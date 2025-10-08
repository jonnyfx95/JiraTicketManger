using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json.Linq;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per la connessione e l'autenticazione con Jira API.
    /// Supporta dual authentication: Jira API Token e Microsoft SSO con credenziali hardcoded.
    /// Migrato da JiraApiService.vb mantenendo compatibilità.
    /// </summary>
    public class JiraApiService : IDisposable
    {
        private readonly LoggingService _logger;
        private readonly HttpClient _httpClient;
        private readonly CryptographyService _cryptographyService;
        private bool _disposed = false;

        /// <summary>
        /// MODIFICA CREDENZIALI JIRA SSO
        /// </summary>
        // Credenziali per modalità Microsoft SSO (hardcoded come richiesto)
        private const string SSO_USERNAME = "USERNAME";
        private const string SSO_TOKEN = "TOKEN";

        public string Domain { get; private set; }
        public string Username { get; private set; }
        public string Token { get; private set; }
        public bool IsSSOModeActive { get; private set; }

        #region Constructors

        /// <summary>
        /// Constructor per modalità API Token esplicita
        /// </summary>
        public JiraApiService(string domain, string username, string token)
        {
            _logger = LoggingService.CreateForComponent("JiraApiService");
            _cryptographyService = CryptographyService.CreateDefault();

            Domain = domain?.TrimEnd('/') ?? "";
            Username = username ?? "";
            Token = token ?? "";
            IsSSOModeActive = false;

            _httpClient = CreateHttpClient();

            _logger.LogInfo($"JiraApiService inizializzato - Modalità API Token (Domain: {Domain}, Username: {Username})");
        }

        /// <summary>
        /// Constructor per modalità Microsoft SSO con credenziali hardcoded
        /// </summary>
        public JiraApiService(string domain, bool useSSOCredentials = true)
        {
            _logger = LoggingService.CreateForComponent("JiraApiService");
            _cryptographyService = CryptographyService.CreateDefault();

            Domain = domain?.TrimEnd('/') ?? "https://deda-next.atlassian.net";

            if (useSSOCredentials)
            {
                Username = SSO_USERNAME;
                Token = SSO_TOKEN;
                IsSSOModeActive = true;
                _logger.LogInfo($"JiraApiService inizializzato - Modalità Microsoft SSO (Domain: {Domain})");
            }
            else
            {
                Username = "";
                Token = "";
                IsSSOModeActive = false;
                _logger.LogInfo($"JiraApiService inizializzato - Modalità vuota (Domain: {Domain})");
            }

            _httpClient = CreateHttpClient();
        }

        /// <summary>
        /// Constructor da SettingsService con gestione automatica dual-auth
        /// </summary>
        public JiraApiService(SettingsService settingsService)
        {
            _logger = LoggingService.CreateForComponent("JiraApiService");
            _cryptographyService = CryptographyService.CreateDefault();

            try
            {
                var settings = settingsService.GetCurrentSettings();  // ← AGGIUNGI QUESTA RIGA
                Domain = settings.JiraSettings.Domain;

                // Determina modalità autenticazione
                string authMethod = settings.UserSettings.AuthenticationMethod;
                _logger.LogInfo($"JiraApiService - AuthenticationMethod dal settings: '{authMethod}'");

                if (authMethod == "MicrosoftSSO")
                {
                    _logger.LogInfo("JiraApiService - USANDO CREDENZIALI HARDCODED SSO");
                    Username = SSO_USERNAME;
                    Token = SSO_TOKEN;
                    IsSSOModeActive = true;
                }
                else if (authMethod == "JiraAPI")
                {
                    _logger.LogInfo("JiraApiService - USANDO CREDENZIALI DAL FILE");
                    var (domain, username, token) = settingsService.GetJiraCredentials();

                    Domain = domain;
                    Username = _cryptographyService.EnsureDecrypted(username);
                    Token = _cryptographyService.EnsureDecrypted(token);
                    IsSSOModeActive = false;
                }
                else
                {
                    _logger.LogWarning($"JiraApiService - AuthenticationMethod sconosciuto/vuoto: '{authMethod}' - Default a SSO");
                    Username = SSO_USERNAME;  // ← PROBABILMENTE ENTRA QUI!
                    Token = SSO_TOKEN;
                    IsSSOModeActive = true;
                }

                _httpClient = CreateHttpClient();
                _logger.LogInfo($"JiraApiService inizializzato - Modalità: {(IsSSOModeActive ? "SSO" : "API")} (Username: {Username})");
            }
            catch (Exception ex)
            {
                _logger.LogError("JiraApiService Constructor da SettingsService", ex);
                throw;
            }
        }

        #endregion

        #region HTTP Client Setup

        private HttpClient CreateHttpClient()
        {
            try
            {
                var client = new HttpClient();

                // Timeout configurabile (default 30 secondi)
                client.Timeout = TimeSpan.FromSeconds(30);

                // Headers comuni
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("User-Agent", "JiraTicketManager/2.0");

                // Autenticazione Basic se credenziali disponibili
                if (!string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Token))
                {
                    string authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Token}"));
                    client.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");
                    _logger.LogDebug("HttpClient configurato con Basic Authentication");
                }
                else
                {
                    _logger.LogWarning("HttpClient configurato senza credenziali");
                }

                return client;
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateHttpClient", ex);
                throw;
            }
        }

        #endregion

        #region Connection Testing

        /// <summary>
        /// Testa la connessione a Jira API restituendo solo OK/KO.
        /// Usa l'endpoint /rest/api/2/myself per un test veloce e leggero.
        /// </summary>
        /// <returns>True se connessione OK, False se KO</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInfo("TestConnectionAsync - Inizio test connessione");

                if (string.IsNullOrEmpty(Domain))
                {
                    _logger.LogError("TestConnectionAsync - Domain vuoto");
                    return false;
                }

                if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Token))
                {
                    _logger.LogError("TestConnectionAsync - Credenziali mancanti");
                    return false;
                }

                // Lista di endpoint da testare (in ordine di preferenza)
                string[] testEndpoints = {
                       "/rest/api/3/myself",           // ✅ NUOVO - API v3 preferita
                       "/rest/api/2/myself",           // Fallback API v2
                       "/rest/api/3/serverInfo",       // ✅ NUOVO - Info server v3
                       "/rest/api/2/serverInfo",       // Fallback info server
                       "/rest/api/2/user?accountId=me" // User info alternativo
                };

                foreach (string endpoint in testEndpoints)
                {
                    string testUrl = $"{Domain}{endpoint}";
                    _logger.LogDebug($"TestConnectionAsync - Provo endpoint: {testUrl}");

                    try
                    {
                        using (var response = await _httpClient.GetAsync(testUrl))
                        {
                            _logger.LogInfo($"TestConnectionAsync - Endpoint {endpoint}: StatusCode {response.StatusCode}");

                            if (response.IsSuccessStatusCode)
                            {
                                _logger.LogInfo($"TestConnectionAsync - Successo con endpoint: {endpoint}");

                                // Log informazioni utente per debug (senza credenziali)
                                try
                                {
                                    string content = await response.Content.ReadAsStringAsync();
                                    var userInfo = JObject.Parse(content);
                                    string displayName = userInfo["displayName"]?.ToString() ?? "Unknown";
                                    string emailAddress = userInfo["emailAddress"]?.ToString() ?? userInfo["name"]?.ToString() ?? "Unknown";

                                    _logger.LogInfo($"TestConnectionAsync - Utente autenticato: {displayName} ({emailAddress})");
                                }
                                catch (Exception parseEx)
                                {
                                    _logger.LogWarning($"TestConnectionAsync - Errore parsing risposta: {parseEx.Message}");
                                }

                                return true;
                            }
                            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                            {
                                _logger.LogError($"TestConnectionAsync - Credenziali non valide per {endpoint}");
                                // Continua con l'endpoint successivo
                            }
                            else
                            {
                                _logger.LogWarning($"TestConnectionAsync - Endpoint {endpoint} non disponibile: {response.StatusCode}");
                                // Continua con l'endpoint successivo
                            }
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        _logger.LogWarning($"TestConnectionAsync - Errore HTTP per {endpoint}: {httpEx.Message}");
                        // Continua con l'endpoint successivo
                    }
                }

                // Se tutti gli endpoint falliscono
                _logger.LogError("TestConnectionAsync - Tutti gli endpoint hanno fallito");
                return false;
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError($"TestConnectionAsync - Timeout: {timeoutEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("TestConnectionAsync - Exception generica", ex);
                return false;
            }
        }

        /// <summary>
        /// Test connessione sincrono (wrapper per compatibilità)
        /// </summary>
        /// <returns>True se connessione OK, False se KO</returns>
        public bool TestConnection()
        {
            try
            {
                return TestConnectionAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError("TestConnection (sync)", ex);
                return false;
            }
        }

        #endregion

        #region Configuration Management

        /// <summary>
        /// Aggiorna le credenziali del servizio
        /// </summary>
        public void UpdateCredentials(string domain, string username, string token)
        {
            try
            {
                _logger.LogInfo("UpdateCredentials - Aggiornamento credenziali");

                Domain = domain?.TrimEnd('/') ?? "";
                Username = username ?? "";
                Token = token ?? "";
                IsSSOModeActive = false;

                // Ricrea HttpClient con nuove credenziali
                _httpClient?.Dispose();
                var newClient = CreateHttpClient();

                // Sostituisci il client (thread-safe per singoli assignment)
                var oldClient = _httpClient;
                // _httpClient = newClient; // Non possiamo modificare readonly field

                _logger.LogInfo($"UpdateCredentials - Credenziali aggiornate (Domain: {Domain}, Username: {Username})");
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateCredentials", ex);
                throw;
            }
        }

        /// <summary>
        /// Imposta modalità Microsoft SSO
        /// </summary>
        public void SetSSOMode(string domain = null)
        {
            try
            {
                _logger.LogInfo("SetSSOMode - Impostazione modalità Microsoft SSO");

                if (!string.IsNullOrEmpty(domain))
                {
                    Domain = domain.TrimEnd('/');
                }

                Username = SSO_USERNAME;
                Token = SSO_TOKEN;
                IsSSOModeActive = true;

                // Ricrea HttpClient
                _httpClient?.Dispose();
                var newClient = CreateHttpClient();

                _logger.LogInfo($"SetSSOMode - Modalità SSO attivata (Domain: {Domain})");
            }
            catch (Exception ex)
            {
                _logger.LogError("SetSSOMode", ex);
                throw;
            }
        }

        /// <summary>
        /// Verifica se le credenziali sono configurate
        /// </summary>
        public bool HasValidCredentials()
        {
            return !string.IsNullOrEmpty(Domain) &&
                   !string.IsNullOrEmpty(Username) &&
                   !string.IsNullOrEmpty(Token);
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Crea istanza per modalità Microsoft SSO
        /// </summary>
        public static JiraApiService CreateForSSO(string domain = "https://deda-next.atlassian.net")
        {
            return new JiraApiService(domain, useSSOCredentials: true);
        }

        /// <summary>
        /// Crea istanza per modalità API Token
        /// </summary>
        public static JiraApiService CreateForAPI(string domain, string username, string token)
        {
            return new JiraApiService(domain, username, token);
        }

        /// <summary>
        /// Crea istanza da SettingsService con auto-detection modalità
        /// </summary>
        public static JiraApiService CreateFromSettings(SettingsService settingsService)
        {
            return new JiraApiService(settingsService);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _httpClient?.Dispose();
                    _logger.LogDebug("JiraApiService disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Dispose", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        ~JiraApiService()
        {
            Dispose(false);
        }

        #endregion

        #region Search Methods

        /// <summary>
        /// Esegue una ricerca di ticket Jira usando JQL.
        /// Migrato da JiraApiService.vb con stessa logica.
        /// </summary>
        /// <param name="jql">Query JQL da eseguire</param>
        /// <param name="startAt">Indice di partenza (paginazione)</param>
        /// <param name="maxResults">Numero massimo di risultati</param>
        /// <param name="progress">Progress reporter opzionale</param>
        /// <returns>Risultati della ricerca</returns>
        public async Task<JiraSearchResult> SearchIssuesAsync(
    string jql,
    int startAt,
    int maxResults,
    IProgress<int>? progress = null,
    string? nextPageToken = null)
        {
            try
            {
                _logger.LogInfo($"SearchIssuesAsync - Avvio ricerca Jira: JQL='{jql}', StartAt={startAt}, MaxResults={maxResults}, NextPageToken={(nextPageToken != null ? nextPageToken.Substring(0, Math.Min(20, nextPageToken.Length)) : "null")}");

                // Encode JQL per URL
                var encodedJql = Uri.EscapeDataString(jql);

                // Costruisci URL API v3
                string url = $"{Domain}/rest/api/3/search/jql?jql={encodedJql}&maxResults={maxResults}" +
                             "&fields=key,summary,status,priority,assignee,reporter,issuetype,created,updated,description,resolutiondate," +
                             "customfield_10117,customfield_10113,customfield_10114,customfield_10172," +
                             "customfield_10136,customfield_10074,customfield_10103,customfield_10271," +
                             "customfield_10272,customfield_10238,customfield_10096," +
                             "customfield_10116,customfield_10133,customfield_10089";

                // Aggiungi nextPageToken se presente
                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    url += $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
                    _logger.LogInfo($"NextPageToken applicato: {nextPageToken}");
                }

                _logger.LogDebug($"SearchIssuesAsync - URL completo: {url}");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Headers.Add("Accept", "application/json");

                using var response = await _httpClient.SendAsync(request);

                _logger.LogInfo($"SearchIssuesAsync - Risposta ricevuta. Status code: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"SearchIssuesAsync - Errore API Jira. Status: {response.StatusCode}, Content: {errorContent}");
                    throw new HttpRequestException($"Errore API Jira: {response.StatusCode} - {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug($"SearchIssuesAsync - Contenuto JSON ricevuto (primi 500 caratteri): {jsonContent.Substring(0, Math.Min(500, jsonContent.Length))}");

                JObject jsonObject;
                try
                {
                    jsonObject = JObject.Parse(jsonContent);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SearchIssuesAsync - Eccezione durante il parsing JSON: {ex.Message}");
                    _logger.LogError($"Contenuto JSON completo: {jsonContent}");
                    throw;
                }

                var issuesToken = jsonObject["issues"];
                if (issuesToken == null)
                {
                    _logger.LogWarning("SearchIssuesAsync - Campo 'issues' non presente nella risposta.");
                    issuesToken = new JArray();
                }
                else if (issuesToken.Type != JTokenType.Array)
                {
                    _logger.LogWarning($"SearchIssuesAsync - Campo 'issues' non è un array ma di tipo {issuesToken.Type}. Viene usato array vuoto.");
                    issuesToken = new JArray();
                }

                var totalTickets = jsonObject["total"]?.Value<int>() ?? 0;
                var returnedCount = ((JArray)issuesToken).Count;
                var maxResultsValue = jsonObject["maxResults"]?.Value<int>() ?? maxResults;
                var nextToken = jsonObject["nextPageToken"]?.ToString();

                _logger.LogInfo($"SearchIssuesAsync - Totale da API: {totalTickets}, Ticket restituiti: {returnedCount}, maxResults: {maxResultsValue}, NextPageToken: {(string.IsNullOrEmpty(nextToken) ? "null" : nextToken)}");

                progress?.Report(returnedCount);

                return new JiraSearchResult
                {
                    Issues = (JArray)issuesToken,
                    Total = totalTickets,
                    StartAt = startAt,
                    MaxResults = maxResultsValue,
                    NextPageToken = nextToken
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("SearchIssuesAsync - Errore HTTP durante richiesta API", ex);
                throw new Exception($"Errore di connessione API Jira v3: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("SearchIssuesAsync - Errore generico durante ricerca", ex);
                throw new Exception($"Errore durante ricerca Jira API v3: {ex.Message}", ex);
            }
        }



        /// <summary>
        /// Esegue una ricerca completa senza paginazione per export.
        /// Migrato da JiraApiService.vb.
        /// </summary>
        public async Task<JArray> SearchAllIssuesForExportAsync(string jql, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("=== INIZIO EXPORT COMPLETO ===");

                var allIssues = new List<JToken>();
                int startAt = 0;
                const int batchSize = 100;
                bool hasMoreData = true;

                // Prima chiamata per ottenere il totale
                progress?.Report("🔍 Analisi query...");
                var firstResult = await SearchIssuesAsync(jql, 0, 1);
                int totalRecords = firstResult.Total;

                _logger.LogInfo($"Totale record da scaricare: {totalRecords}");

                if (totalRecords == 0)
                {
                    progress?.Report("❌ Nessun record trovato");
                    return new JArray();
                }

                // Ciclo di download con progress dettagliato
                while (hasMoreData)
                {
                    int currentBatch = (startAt / batchSize) + 1;
                    int totalBatches = (int)Math.Ceiling((double)totalRecords / batchSize);

                    progress?.Report($"📥 Download batch {currentBatch}/{totalBatches} ({allIssues.Count}/{totalRecords} record)");

                    _logger.LogDebug($"Richiesta batch: startAt={startAt}, maxResults={batchSize}");

                    var batchResult = await SearchIssuesAsync(jql, startAt, batchSize);

                    _logger.LogDebug($"Ricevuto batch: {batchResult.Issues.Count} issues");

                    // Aggiungi i risultati
                    allIssues.AddRange(batchResult.Issues);

                    // Controlla se ci sono altri dati
                    hasMoreData = batchResult.Issues.Count >= batchSize && allIssues.Count < totalRecords;
                    startAt += batchSize;

                    // Safety check per evitare loop infiniti
                    if (currentBatch > 100) // Max 10.000 record
                    {
                        _logger.LogWarning("Raggiunto limite sicurezza di 100 batch");
                        break;
                    }
                }

                _logger.LogInfo($"Export completato: {allIssues.Count} record totali");
                progress?.Report($"✅ Export completato: {allIssues.Count} record");

                return new JArray(allIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError("SearchAllIssuesForExportAsync", ex);
                throw;
            }
        }

        #endregion

        #region Comments API

        /// <summary>
        /// Aggiunge un commento a un issue Jira utilizzando API v3 con formato ADF
        /// </summary>
        /// <param name="issueKey">Chiave del ticket (es. ISSUE-123)</param>
        /// <param name="commentText">Testo del commento da aggiungere</param>
        /// <returns>True se il commento è stato aggiunto con successo</returns>
        public async Task<bool> AddCommentToIssueAsync(string issueKey, string commentText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(issueKey))
                {
                    _logger.LogError("IssueKey non può essere vuoto");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(commentText))
                {
                    _logger.LogError("CommentText non può essere vuoto");
                    return false;
                }

                _logger.LogInfo($"Aggiunta commento a issue: {issueKey}");

                // Costruisci URL API v3
                var url = $"{Domain}/rest/api/3/issue/{issueKey}/comment";
                _logger.LogDebug($"URL commento: {url}");

                // Converti testo in formato ADF (Atlassian Document Format)
                var adfBody = ConvertTextToADF(commentText);

                // Crea il payload JSON
                var payload = new JObject
                {
                    ["body"] = adfBody
                };

                var jsonContent = payload.ToString();
                _logger.LogDebug($"Payload commento: {jsonContent.Length} caratteri");

                // Crea la richiesta HTTP
                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    request.Headers.Add("Authorization", GetAuthorizationHeader());
                    request.Headers.Add("Accept", "application/json");
                    request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Invia la richiesta
                    using (var response = await _httpClient.SendAsync(request))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInfo($"Commento aggiunto con successo a {issueKey}");
                            _logger.LogDebug($"Risposta API: {responseContent}");
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"Errore aggiunta commento a {issueKey}. Status: {response.StatusCode}");
                            _logger.LogError($"Risposta errore: {responseContent}");
                            return false;
                        }
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"Errore HTTP aggiunta commento a {issueKey}: {httpEx.Message}");
                return false;
            }
            catch (TaskCanceledException tcEx)
            {
                _logger.LogError($"Timeout aggiunta commento a {issueKey}: {tcEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore generale aggiunta commento a {issueKey}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Converte testo normale in formato ADF (Atlassian Document Format)
        /// Ogni riga del testo diventa un paragrafo separato in ADF
        /// </summary>
        /// <param name="text">Testo da convertire</param>
        /// <returns>JObject in formato ADF</returns>
        private JObject ConvertTextToADF(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return CreateEmptyADFDocument();
                }

                // Dividi il testo in righe
                var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                var contentArray = new JArray();

                // Raggruppa righe consecutive non vuote in un singolo paragrafo
                var currentParagraphLines = new List<string>();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        // Riga vuota: chiudi il paragrafo corrente e aggiungi spazio
                        if (currentParagraphLines.Count > 0)
                        {
                            AddParagraphToContent(contentArray, currentParagraphLines);
                            currentParagraphLines.Clear();
                        }
                        // Aggiungi un paragrafo vuoto per lo spazio
                        AddEmptyParagraph(contentArray);
                    }
                    else
                    {
                        currentParagraphLines.Add(line);
                    }
                }

                // Aggiungi l'ultimo paragrafo se presente
                if (currentParagraphLines.Count > 0)
                {
                    AddParagraphToContent(contentArray, currentParagraphLines);
                }

                // Crea il documento ADF completo
                var adfDocument = new JObject
                {
                    ["type"] = "doc",
                    ["version"] = 1,
                    ["content"] = contentArray
                };

                _logger.LogDebug($"Testo convertito in ADF: {lines.Length} righe → {contentArray.Count} paragrafi");
                return adfDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conversione testo in ADF: {ex.Message}");
                return CreateEmptyADFDocument();
            }
        }

        private void AddEmptyParagraph(JArray contentArray)
        {
            contentArray.Add(new JObject
            {
                ["type"] = "paragraph",
                ["content"] = new JArray()
            });
        }

        // <summary>
        /// Aggiunge un paragrafo con righe multiple al contenuto ADF
        /// </summary>
        private void AddParagraphToContent(JArray contentArray, List<string> lines)
        {
            if (lines.Count == 0) return;

            var paragraph = new JObject
            {
                ["type"] = "paragraph",
                ["content"] = new JArray()
            };

            var paragraphContent = (JArray)paragraph["content"];

            for (int i = 0; i < lines.Count; i++)
            {
                // Aggiungi il testo della riga
                paragraphContent.Add(new JObject
                {
                    ["type"] = "text",
                    ["text"] = lines[i]
                });

                // Aggiungi hard break tra le righe (tranne l'ultima)
                if (i < lines.Count - 1)
                {
                    paragraphContent.Add(new JObject
                    {
                        ["type"] = "hardBreak"
                    });
                }
            }

            contentArray.Add(paragraph);
        }

        /// <summary>
        /// Crea un documento ADF vuoto come fallback
        /// </summary>
        /// <returns>Documento ADF vuoto</returns>
        private JObject CreateEmptyADFDocument()
        {
            return new JObject
            {
                ["type"] = "doc",
                ["version"] = 1,
                ["content"] = new JArray
        {
            new JObject
            {
                ["type"] = "paragraph",
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = "[Commento vuoto]"
                    }
                }
            }
        }
            };
        }


        /// <summary>
        /// Metodo helper pubblico per ottenere l'header di autorizzazione
        /// (già esistente nel progetto, ma lo rendo pubblico se necessario)
        /// </summary>


        #endregion


       

        #region Update Helper Methods

        /// <summary>
        /// Aggiorna un campo di testo semplice
        /// </summary>
        public async Task<bool> UpdateTextFieldAsync(string issueKey, string fieldName, string value)
        {
            var updateData = new
            {
                fields = new Dictionary<string, object>
                {
                    [fieldName] = value
                }
            };

            return await UpdateIssueAsync(issueKey, updateData);
        }

        /// <summary>
        /// Aggiorna un campo option (come Status, Priority)
        /// </summary>
        public async Task<bool> UpdateOptionFieldAsync(string issueKey, string fieldName, string optionValue)
        {
            var updateData = new
            {
                fields = new Dictionary<string, object>
                {
                    [fieldName] = new { value = optionValue }
                }
            };

            return await UpdateIssueAsync(issueKey, updateData);
        }

        /// <summary>
        /// Aggiorna un campo workspace object (CMDB)
        /// </summary>
        public async Task<bool> UpdateWorkspaceFieldAsync(string issueKey, string fieldName,
            string workspaceId, string objectId)
        {
            var updateData = new
            {
                fields = new Dictionary<string, object>
                {
                    [fieldName] = new[]
                    {
                new
                {
                    workspaceId = workspaceId,
                    id = $"{workspaceId}:{objectId}",
                    objectId = objectId
                }
            }
                }
            };

            return await UpdateIssueAsync(issueKey, updateData);
        }

        /// <summary>
        /// Aggiorna campo dal valore di una TextBox
        /// </summary>
        public async Task<bool> UpdateFromTextBoxAsync(string issueKey, string fieldName, TextBox textBox)
        {
            if (textBox == null || string.IsNullOrEmpty(textBox.Text))
            {
                _logger.LogWarning($"TextBox vuota per campo {fieldName}");
                return false;
            }

            return await UpdateTextFieldAsync(issueKey, fieldName, textBox.Text);
        }

        /// <summary>
        /// Aggiorna campo dal valore di una ComboBox
        /// </summary>
        public async Task<bool> UpdateFromComboBoxAsync(string issueKey, string fieldName, ComboBox comboBox)
        {
            if (comboBox == null || comboBox.SelectedItem == null)
            {
                _logger.LogWarning($"ComboBox vuota per campo {fieldName}");
                return false;
            }

            var selectedValue = comboBox.SelectedItem.ToString();
            return await UpdateOptionFieldAsync(issueKey, fieldName, selectedValue);
        }

        /// <summary>
        /// Aggiorna multipli campi da controlli UI
        /// </summary>
        public async Task<bool> UpdateFromControlsAsync(string issueKey,
            Dictionary<string, Control> controlMappings)
        {
            try
            {
                var fields = new Dictionary<string, object>();

                foreach (var mapping in controlMappings)
                {
                    var fieldName = mapping.Key;
                    var control = mapping.Value;

                    switch (control)
                    {
                        case TextBox textBox when !string.IsNullOrEmpty(textBox.Text):
                            fields[fieldName] = textBox.Text;
                            _logger.LogDebug($"Campo {fieldName}: '{textBox.Text}' (TextBox)");
                            break;

                        case ComboBox comboBox when comboBox.SelectedItem != null:
                            fields[fieldName] = new { value = comboBox.SelectedItem.ToString() };
                            _logger.LogDebug($"Campo {fieldName}: '{comboBox.SelectedItem}' (ComboBox)");
                            break;

                        case Label label when !string.IsNullOrEmpty(label.Text):
                            fields[fieldName] = label.Text;
                            _logger.LogDebug($"Campo {fieldName}: '{label.Text}' (Label)");
                            break;

                        default:
                            _logger.LogDebug($"Campo {fieldName}: controllo vuoto o non supportato");
                            break;
                    }
                }

                if (fields.Count == 0)
                {
                    _logger.LogWarning($"Nessun campo da aggiornare per {issueKey}");
                    return false;
                }

                _logger.LogInfo($"Aggiornamento {fields.Count} campi per {issueKey}");
                var updateData = new { fields = fields };
                return await UpdateIssueAsync(issueKey, updateData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore update da controlli per {issueKey}", ex);
                return false;
            }
        }

        /// <summary>
        /// Aggiorna multipli campi workspace objects
        /// </summary>
        public async Task<bool> UpdateMultipleWorkspaceFieldsAsync(string issueKey,
            Dictionary<string, (string workspaceId, string objectId)> workspaceFields)
        {
            try
            {
                var fields = new Dictionary<string, object>();

                foreach (var field in workspaceFields)
                {
                    var fieldName = field.Key;
                    var (workspaceId, objectId) = field.Value;

                    fields[fieldName] = new[]
                    {
                new
                {
                    workspaceId = workspaceId,
                    id = $"{workspaceId}:{objectId}",
                    objectId = objectId
                }
            };

                    _logger.LogDebug($"Campo workspace {fieldName}: objectId {objectId}");
                }

                if (fields.Count == 0)
                {
                    _logger.LogWarning($"Nessun campo workspace da aggiornare per {issueKey}");
                    return false;
                }

                _logger.LogInfo($"Aggiornamento {fields.Count} campi workspace per {issueKey}");
                var updateData = new { fields = fields };
                return await UpdateIssueAsync(issueKey, updateData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore update campi workspace per {issueKey}", ex);
                return false;
            }
        }

        /// <summary>
        /// Aggiorna i campi di un issue Jira
        /// </summary>
        /// <param name="issueKey">Chiave del ticket (es: CC-12345)</param>
        /// <param name="updateData">Dati da aggiornare nel formato { fields: { ... } }</param>
        /// <returns>True se l'aggiornamento è avvenuto con successo</returns>
        public async Task<bool> UpdateIssueAsync(string issueKey, object updateData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(issueKey))
                {
                    _logger.LogError("IssueKey non può essere vuoto");
                    return false;
                }

                if (updateData == null)
                {
                    _logger.LogError("UpdateData non può essere null");
                    return false;
                }

                _logger.LogInfo($"Aggiornamento issue: {issueKey}");

                // Costruisci URL API v3
                var url = $"{Domain}/rest/api/3/issue/{issueKey}";
                _logger.LogDebug($"URL update: {url}");

                // Serializza i dati in JSON
                var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(updateData, Newtonsoft.Json.Formatting.Indented);
                _logger.LogDebug($"Payload update ({jsonContent.Length} chars): {jsonContent}");

                // Crea la richiesta HTTP PUT
                using (var request = new HttpRequestMessage(HttpMethod.Put, url))
                {
                    request.Headers.Add("Authorization", GetAuthorizationHeader());
                    request.Headers.Add("Accept", "application/json");
                    request.Content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                    // Invia la richiesta
                    using (var response = await _httpClient.SendAsync(request))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            _logger.LogInfo($"✅ Issue {issueKey} aggiornato con successo");
                            _logger.LogDebug($"Risposta API: {responseContent}");
                            return true;
                        }
                        else
                        {
                            _logger.LogError($"❌ Errore aggiornamento {issueKey} - Status: {response.StatusCode}");
                            _logger.LogError($"Risposta API: {responseContent}");

                            // Tenta di parsare errore specifico da Jira
                            try
                            {
                                var errorJson = JObject.Parse(responseContent);
                                var errorMessages = errorJson["errorMessages"]?.ToObject<string[]>();
                                var errors = errorJson["errors"]?.ToObject<Dictionary<string, string>>();

                                if (errorMessages?.Length > 0)
                                {
                                    _logger.LogError($"Errori Jira: {string.Join(", ", errorMessages)}");
                                }

                                if (errors?.Count > 0)
                                {
                                    foreach (var error in errors)
                                    {
                                        _logger.LogError($"Campo {error.Key}: {error.Value}");
                                    }
                                }
                            }
                            catch
                            {
                                // Se non riesce a parsare l'errore, ignora
                            }

                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore generale aggiornamento {issueKey}: {ex.Message}", ex);
                return false;
            }
        }
        #endregion



        /// <summary>
        /// Risultato di una ricerca Jira
        /// </summary>
        public class JiraSearchResult
        {
            public JArray Issues { get; set; }
            public int Total { get; set; }
            public int StartAt { get; set; }
            public int MaxResults { get; set; }
            public string NextPageToken { get; set; } // ✅ NUOVO per API v3
        }



        /// <summary>
        /// Ottiene l'header di autorizzazione per le chiamate API
        /// </summary>
        public string GetAuthorizationHeader()
        {
            if (IsSSOModeActive)
            {
                // Modalità SSO con credenziali hardcoded
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{SSO_USERNAME}:{SSO_TOKEN}"));
                return $"Basic {credentials}";
            }
            else
            {
                // Modalità normale con credenziali utente
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Username}:{Token}"));
                return $"Basic {credentials}";
            }
        }

    }
}