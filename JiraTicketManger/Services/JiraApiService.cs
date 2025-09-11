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
        private const string SSO_USERNAME = "jonathan.felixdasilva@dedagroup.it";
        private const string SSO_TOKEN = "ATATT3xFfGF0Ore44aYniWdWcF1c5p-R_WhsmnjXElNbLU_DlDKYmrIBnblAMYaRJmUAKPJXbG97sZt4hBVL_ZBSKqHFlkR8H21XkVPc5UAbA3sgtWMwbG2-XjMO8_kM9RjUN_q61ciFiQEwnJfZ2pdNhQnffN7CUn_D5nmibFazwYfWoMfe3J4=90AF50DB";

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
        public async Task<JiraSearchResult> SearchIssuesAsync(string jql, int startAt, int maxResults, IProgress<int> progress = null, string nextPageToken = null)
        {
            try
            {
                _logger.LogInfo($"SearchIssuesAsync - JQL: {jql}, StartAt: {startAt}, MaxResults: {maxResults}, NextPageToken: {nextPageToken?.Substring(0, 20) ?? "null"}");

                // Encode JQL per URL
                var encodedJql = Uri.EscapeDataString(jql);

                // ✅ USA SEMPRE API v3 - Gestione paginazione con nextPageToken
                string url = $"{Domain}/rest/api/3/search/jql?jql={encodedJql}&maxResults={maxResults}" +
                             "&fields=key,summary,status,priority,assignee,reporter,issuetype,created,updated,description,resolutiondate," +
                             "customfield_10117,customfield_10113,customfield_10114,customfield_10172," +
                             "customfield_10136,customfield_10074,customfield_10103,customfield_10271," +
                             "customfield_10272,customfield_10238,customfield_10096," +
                             "customfield_10116,customfield_10133,customfield_10089";

                // ✅ NUOVO: Aggiungi nextPageToken se fornito
                if (!string.IsNullOrEmpty(nextPageToken))
                {
                    url += $"&nextPageToken={Uri.EscapeDataString(nextPageToken)}";
                }

                _logger.LogDebug($"API v3 URL: {url}");

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Headers.Add("Accept", "application/json");

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API v3 Error - Status: {response.StatusCode}, Content: {errorContent}");
                    throw new HttpRequestException($"API v3 Error: {response.StatusCode} - {errorContent}");
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonContent);

                // ✅ GESTIONE SICURA DEI CAMPI - Alcuni potrebbero essere null nell'API v3
                var result = new JiraSearchResult
                {
                    Issues = (JArray)(jsonObject["issues"] ?? new JArray()),
                    Total = (int)(jsonObject["total"] ?? 0),
                    StartAt = startAt, // Usiamo il valore passato come parametro
                    MaxResults = (int)(jsonObject["maxResults"] ?? maxResults),
                    NextPageToken = jsonObject["nextPageToken"]?.ToString() // ✅ NUOVO
                };

                _logger.LogInfo($"API v3 Search completed - Found: {result.Total} tickets, Returned: {result.Issues.Count}, HasNextPage: {!string.IsNullOrEmpty(result.NextPageToken)}");

                // Report progress se fornito
                progress?.Report(result.Issues.Count);
                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("SearchIssuesAsync - HTTP Error", ex);
                throw new Exception($"Errore di connessione API v3: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError("SearchIssuesAsync - Generic Error", ex);
                throw new Exception($"Errore nella ricerca JIRA v3: {ex.Message}", ex);
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