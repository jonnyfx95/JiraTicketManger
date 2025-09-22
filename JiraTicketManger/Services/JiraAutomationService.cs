using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per l'automazione ticket Jira - Area Demografia
    /// Gestisce lettura configurazione, ricerca ticket, aggiornamento campi e transizioni
    /// </summary>
    public class JiraAutomationService
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraApiService _jiraApiService;
        private readonly JiraTransitionService _transitionService;

        private AutomationConfig _config;
        private string _currentLogFile;
        private string _currentErrorLogFile;

        #endregion

        #region Constructor

        public JiraAutomationService()
        {
            _logger = LoggingService.CreateForComponent("JiraAutomationService");

            // Ottieni servizi esistenti
            var settingsService = SettingsService.CreateDefault();
            _jiraApiService = JiraApiService.CreateFromSettings(settingsService);
            _transitionService = new JiraTransitionService(_jiraApiService);

            _logger.LogInfo("JiraAutomationService inizializzato");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Esegue l'automazione completa per l'area Demografia
        /// </summary>
        /// <param name="cancellationToken">Token per cancellazione</param>
        /// <param name="progressCallback">Callback per aggiornamenti progresso</param>
        /// <param name="ticketCallback">Callback per risultati singoli ticket</param>
        public async Task ExecuteAutomationAsync(
            CancellationToken cancellationToken,
            Action<string, int, int> progressCallback,
            Action<string, bool, string> ticketCallback)
        {
            try
            {
                _logger.LogInfo("=== AVVIO AUTOMAZIONE AREA DEMOGRAFIA ===");

                // Step 1: Inizializzazione
                await InitializeAutomationAsync(cancellationToken);
                progressCallback?.Invoke("Inizializzazione completata", 1, 7);

                // Step 2: Carica configurazione
                await LoadConfigurationAsync(cancellationToken);
                progressCallback?.Invoke("Configurazione caricata", 2, 7);

                // Step 3: Ricerca ticket Demografia
                var tickets = await SearchDemografiaTicketsAsync(cancellationToken);
                progressCallback?.Invoke($"Trovati {tickets.Count} ticket", 3, 7);

                if (tickets.Count == 0)
                {
                    LogToFile("Nessun ticket trovato per l'automazione", false);
                    progressCallback?.Invoke("Automazione completata - nessun ticket", 7, 7);
                    return;
                }

                // Step 4-6: Processa ogni ticket
                LogToFile($"Inizio processamento {tickets.Count} ticket", false);

                for (int i = 0; i < tickets.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var ticket = tickets[i];
                    var success = await ProcessSingleTicketAsync(ticket, cancellationToken);

                    ticketCallback?.Invoke(ticket.Key, success.Success, success.Message);
                    progressCallback?.Invoke($"Processato {i + 1}/{tickets.Count}", 4 + (i * 2 / tickets.Count), 7);
                }

                // Step 7: Completamento
                progressCallback?.Invoke("Automazione completata con successo", 7, 7);
                LogToFile("Automazione completata con successo", false);

                _logger.LogInfo("=== AUTOMAZIONE COMPLETATA ===");
            }
            catch (OperationCanceledException)
            {
                LogToFile("Automazione interrotta dall'utente", true);
                _logger.LogInfo("Automazione interrotta dall'utente");
                throw;
            }
            catch (Exception ex)
            {
                LogToFile($"Errore automazione: {ex.Message}", true);
                _logger.LogError("Errore durante automazione", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - Initialization

        private async Task InitializeAutomationAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Crea file di log con timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var logDirectory = Path.Combine(Application.StartupPath, "logs");

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                _currentLogFile = Path.Combine(logDirectory, $"automation_{timestamp}.log");
                _currentErrorLogFile = Path.Combine(logDirectory, $"automation_errors_{timestamp}.log");

                // Verifica connessione Jira
                if (!await _jiraApiService.TestConnectionAsync())
                {
                    throw new Exception("Connessione Jira non disponibile");
                }

                LogToFile("Automazione inizializzata correttamente", false);
                _logger.LogInfo($"Log files: {_currentLogFile}, {_currentErrorLogFile}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione automazione", ex);
                throw;
            }
        }

        private async Task LoadConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                var configPath = Path.Combine(Application.StartupPath, "config", "automation_config.json");

                if (!File.Exists(configPath))
                {
                    // Crea configurazione di default
                    await CreateDefaultConfigurationAsync(configPath);
                }

                var jsonContent = await File.ReadAllTextAsync(configPath, cancellationToken);
                var configData = JsonConvert.DeserializeObject<Dictionary<string, AutomationAreaConfig>>(jsonContent);

                if (!configData.ContainsKey("Demografia"))
                {
                    throw new Exception("Configurazione per area Demografia non trovata");
                }

                _config = new AutomationConfig
                {
                    Demografia = configData["Demografia"]
                };

                LogToFile($"Configurazione caricata: {_config.Demografia.Keywords.Count} keywords, {_config.Demografia.Assignees.Count} assegnatari", false);
                _logger.LogInfo("Configurazione automazione caricata con successo");
            }
            catch (Exception ex)
            {
                LogToFile($"Errore caricamento configurazione: {ex.Message}", true);
                _logger.LogError("Errore caricamento configurazione", ex);
                throw;
            }
        }

        private async Task CreateDefaultConfigurationAsync(string configPath)
        {
            try
            {
                var defaultConfig = new Dictionary<string, AutomationAreaConfig>
                {
                    ["Demografia"] = new AutomationAreaConfig
                    {
                        Query = "\"area[dropdown]\" = \"Civilia Next - Area Demografia\" AND status IN (Nuovo, \"Preso In Carico\") AND project = CC",
                        Keywords = new Dictionary<string, string>
                        {
                            ["ELETTORE"] = "Civilia Next - Area Demografia -> Elettorale",
                            ["STATO CIVILE"] = "Civilia Next - Area Demografia -> Stato Civile",
                            ["default"] = "Civilia Next - Area Demografia -> Anagrafe"
                        },
                        Assignees = new Dictionary<string, string>
                        {
                            ["Elettorale"] = "Jonathan Felix Da Silva",
                            ["Anagrafe"] = "Jonathan Felix Da Silva",
                            ["Stato Civile"] = "Antonio Carcea"
                        }
                    }
                };

                var configDirectory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                var jsonContent = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                await File.WriteAllTextAsync(configPath, jsonContent);

                _logger.LogInfo($"Configurazione di default creata: {configPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore creazione configurazione default", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - Ticket Processing

        private async Task<List<AutomationTicket>> SearchDemografiaTicketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var query = _config.Demografia.Query;
                _logger.LogInfo($"=== RICERCA TICKET DEMOGRAFIA ===");
                _logger.LogInfo($"Query JQL: {query}");
                LogToFile($"Query JQL: {query}", false);

                // Usa JiraApiService esistente per la ricerca
                var searchResult = await _jiraApiService.SearchIssuesAsync(query, 0, 100); // Max 100 ticket per automazione

                // DEBUG: Log struttura risultato
                _logger.LogInfo($"SearchResult: Total={searchResult.Total}, Issues.Count={searchResult.Issues.Count}");
                LogToFile($"Risultati API: Total={searchResult.Total}, Issues.Count={searchResult.Issues.Count}", false);

                if (searchResult.Issues.Count == 0)
                {
                    LogToFile("NESSUN TICKET TROVATO per la query", false);
                    return new List<AutomationTicket>();
                }

                var tickets = new List<AutomationTicket>();

                foreach (var issue in searchResult.Issues)
                {
                    try
                    {
                        _logger.LogInfo($"=== PROCESSAMENTO ISSUE RAW ===");
                        _logger.LogInfo($"Issue type: {issue.GetType()}");
                        _logger.LogInfo($"Issue content: {issue.ToString().Substring(0, Math.Min(500, issue.ToString().Length))}");

                        var ticketKey = GetSafeStringValue(issue["key"]);
                        _logger.LogInfo($"Step 1: TicketKey = {ticketKey}");

                        if (string.IsNullOrWhiteSpace(ticketKey))
                        {
                            _logger.LogWarning("Ticket key vuoto o null - saltato");
                            continue;
                        }

                        var ticketSummary = GetSafeStringValue(issue["fields"]?["summary"]);
                        _logger.LogInfo($"Step 2: TicketSummary = {ticketSummary}");

                        var ticketStatus = GetSafeStringValue(issue["fields"]?["status"]?["name"]);
                        _logger.LogInfo($"Step 3: TicketStatus = {ticketStatus}");

                        var ticketArea = GetCustomFieldValue(issue, "customfield_10113");
                        _logger.LogInfo($"Step 4: TicketArea = {ticketArea}");

                        var ticketAssignee = GetSafeStringValue(issue["fields"]?["assignee"]?["displayName"]);
                        _logger.LogInfo($"Step 5: TicketAssignee = {ticketAssignee}");

                        var ticketOrganization = GetSafeStringValue(issue["fields"]?["organization"]?["name"]);
                        _logger.LogInfo($"Step 6: TicketOrganization = {ticketOrganization}");

                        _logger.LogInfo($"=== TICKET TROVATO: {ticketKey} ===");
                        _logger.LogInfo($"Summary: {ticketSummary}");
                        _logger.LogInfo($"Status: {ticketStatus}");
                        _logger.LogInfo($"Area: {ticketArea}");
                        _logger.LogInfo($"Assignee: {ticketAssignee}");
                        _logger.LogInfo($"Organization: {ticketOrganization}");

                        // VERIFICA CRITICA: Se assignee non è vuoto, questo ticket non dovrebbe essere qui
                        if (!string.IsNullOrWhiteSpace(ticketAssignee))
                        {
                            _logger.LogWarning($"ANOMALIA: {ticketKey} ha assignee '{ticketAssignee}' ma dovrebbe essere vuoto!");
                            LogToFile($"ANOMALIA: {ticketKey} ha assignee '{ticketAssignee}' ma query richiede assignee = empty", true);
                        }

                        // VERIFICA se questo ticket dovrebbe essere nei risultati
                        LogToFile($"TICKET TROVATO: {ticketKey}", false);
                        LogToFile($"  Status: {ticketStatus}", false);
                        LogToFile($"  Area: {ticketArea}", false);
                        LogToFile($"  Summary: {ticketSummary}", false);
                        LogToFile($"  Assignee: {ticketAssignee}", false);
                        LogToFile($"  Organization: {ticketOrganization}", false);

                        var ticket = new AutomationTicket
                        {
                            Key = ticketKey,
                            Summary = ticketSummary,
                            Description = GetSafeStringValue(issue["fields"]?["description"]),
                            Status = ticketStatus,
                            Assignee = ticketAssignee,
                            Organization = ticketOrganization,
                            Area = ticketArea,
                            Application = GetCustomFieldValue(issue, "customfield_10114"),
                            Customer = GetCustomFieldValue(issue, "customfield_10115"),
                            Category = "SUPPORTO CLIENTE",
                            RawData = issue
                        };

                        tickets.Add(ticket);
                        _logger.LogInfo($"Ticket aggiunto alla lista: {ticket.Key}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore processamento singolo ticket: {ex.Message}");
                        LogToFile($"Errore processamento ticket: {ex.Message}", true);
                        // Continua con il prossimo ticket
                    }
                }

                LogToFile($"TOTALE TICKET PROCESSABILI: {tickets.Count}", false);
                _logger.LogInfo($"=== FINE RICERCA: {tickets.Count} ticket trovati ===");

                return tickets;
            }
            catch (Exception ex)
            {
                LogToFile($"Errore ricerca ticket: {ex.Message}", true);
                _logger.LogError("Errore ricerca ticket Demografia", ex);
                throw;
            }
        }

        private async Task<(bool Success, string Message)> ProcessSingleTicketAsync(AutomationTicket ticket, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo($"Processamento ticket: {ticket.Key}");
                LogToFile($"Inizio processamento ticket: {ticket.Key}", false);

                var updates = new List<string>();

                // Step 1: Determina applicativo (se vuoto)
                if (string.IsNullOrWhiteSpace(ticket.Application))
                {
                    var newApplication = DetermineApplication(ticket);
                    if (!string.IsNullOrWhiteSpace(newApplication))
                    {
                        ticket.Application = newApplication;
                        updates.Add($"Applicativo: {newApplication}");
                    }
                }

                // Step 2: Determina assegnatario
                var assignee = DetermineAssignee(ticket.Application);
                if (!string.IsNullOrWhiteSpace(assignee))
                {
                    ticket.Assignee = assignee;
                    updates.Add($"Assegnatario: {assignee}");
                }

                // Step 3: Determina cliente (se vuoto)
                if (string.IsNullOrWhiteSpace(ticket.Customer) && !string.IsNullOrWhiteSpace(ticket.Organization))
                {
                    var customer = await DetermineCustomerAsync(ticket.Organization, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(customer))
                    {
                        ticket.Customer = customer;
                        updates.Add($"Cliente: {customer}");
                    }
                }

                // Step 4: Imposta categoria
                ticket.Category = "SUPPORTO CLIENTE";
                updates.Add("Categoria: SUPPORTO CLIENTE");

                // Step 5: Aggiorna ticket su Jira
                if (updates.Count > 0)
                {
                    await UpdateTicketFieldsAsync(ticket, cancellationToken);
                    LogToFile($"{ticket.Key}: Campi aggiornati - {string.Join(", ", updates)}", false);
                }

                // Step 6: Esegui transizioni di stato
                await ExecuteStatusTransitionsAsync(ticket, cancellationToken);

                var message = $"Processato con successo - {updates.Count} campi aggiornati";
                LogToFile($"{ticket.Key}: {message}", false);

                return (true, message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Errore processamento: {ex.Message}";
                LogToFile($"{ticket.Key}: {errorMessage}", true);
                _logger.LogError($"Errore processamento ticket {ticket.Key}", ex);

                return (false, errorMessage);
            }
        }

        #endregion

        #region Private Methods - Business Logic

        private string DetermineApplication(AutomationTicket ticket)
        {
            try
            {
                var searchText = $"{ticket.Summary} {ticket.Description}".ToUpper();

                // Cerca keywords CASE-SENSITIVE (convertite in maiuscolo per il confronto)
                foreach (var keyword in _config.Demografia.Keywords)
                {
                    if (keyword.Key != "default" && searchText.Contains(keyword.Key.ToUpper()))
                    {
                        _logger.LogInfo($"{ticket.Key}: Trovata keyword '{keyword.Key}' -> {keyword.Value}");
                        return keyword.Value;
                    }
                }

                // Default se nessuna keyword trovata
                var defaultApp = _config.Demografia.Keywords["default"];
                _logger.LogInfo($"{ticket.Key}: Nessuna keyword trovata -> {defaultApp}");
                return defaultApp;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore determinazione applicativo per {ticket.Key}", ex);
                return _config.Demografia.Keywords.ContainsKey("default") ? _config.Demografia.Keywords["default"] : null;
            }
        }

        private string DetermineAssignee(string application)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(application))
                    return null;

                // Estrae nome applicativo dalla parte dopo " -> "
                var appName = application.Contains(" -> ")
                    ? application.Split(new[] { " -> " }, 2, StringSplitOptions.None)[1].Trim()
                    : application;

                // Cerca assegnatario nel config
                if (_config.Demografia.Assignees.ContainsKey(appName))
                {
                    var assignee = _config.Demografia.Assignees[appName];
                    _logger.LogInfo($"Assegnatario per '{appName}': {assignee}");
                    return assignee;
                }

                _logger.LogWarning($"Nessun assegnatario configurato per applicativo: {appName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore determinazione assegnatario per {application}", ex);
                return null;
            }
        }

        private async Task<string> DetermineCustomerAsync(string organizationName, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(organizationName))
                    return null;

                // Esegui query per trovare ticket con Cliente valorizzato per questa organizzazione
                var query = $"reporter in organizationMembers({organizationName}) AND project = CC AND issuetype is not EMPTY";
                _logger.LogInfo($"Ricerca cliente per organizzazione: {organizationName}");

                var searchResult = await _jiraApiService.SearchIssuesAsync(query, 0, 10); // Primi 10 risultati

                foreach (var issue in searchResult.Issues)
                {
                    var customerValue = GetCustomFieldValue(issue, "customfield_10115"); // Campo Cliente
                    if (!string.IsNullOrWhiteSpace(customerValue))
                    {
                        _logger.LogInfo($"Cliente trovato per {organizationName}: {customerValue}");
                        return customerValue;
                    }
                }

                _logger.LogInfo($"Nessun cliente trovato per organizzazione: {organizationName}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore ricerca cliente per organizzazione {organizationName}", ex);
                return null;
            }
        }

        #endregion

        #region Private Methods - Jira API Operations

        private async Task UpdateTicketFieldsAsync(AutomationTicket ticket, CancellationToken cancellationToken)
        {
            try
            {
                var updateFields = new Dictionary<string, object>();

                // PRIORITA': Assegnatario DEVE essere impostato prima delle transizioni
                if (!string.IsNullOrWhiteSpace(ticket.Assignee))
                {
                    var username = ConvertDisplayNameToUsername(ticket.Assignee);
                    updateFields["assignee"] = new { name = username };
                    _logger.LogInfo($"{ticket.Key}: Impostazione assegnatario: '{ticket.Assignee}' -> username: '{username}'");
                }

                // Prepara altri campi per aggiornamento
                if (!string.IsNullOrWhiteSpace(ticket.Application))
                {
                    updateFields["customfield_10114"] = new { value = ticket.Application };
                }

                if (!string.IsNullOrWhiteSpace(ticket.Customer))
                {
                    updateFields["customfield_10115"] = ticket.Customer;
                }

                if (updateFields.Count == 0)
                {
                    _logger.LogInfo($"{ticket.Key}: Nessun campo da aggiornare");
                    return;
                }

                // Log dettagli update
                _logger.LogInfo($"{ticket.Key}: Aggiornamento {updateFields.Count} campi:");
                foreach (var field in updateFields)
                {
                    _logger.LogInfo($"  {field.Key}: {field.Value}");
                }

                // Esegui aggiornamento via API
                await UpdateTicketViaApiAsync(ticket.Key, updateFields, cancellationToken);

                _logger.LogInfo($"{ticket.Key}: Aggiornati {updateFields.Count} campi con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiornamento campi per {ticket.Key}", ex);
                throw;
            }
        }

        private async Task UpdateTicketViaApiAsync(string ticketKey, Dictionary<string, object> fields, CancellationToken cancellationToken)
        {
            try
            {
                // Usa HttpClient per chiamata PUT diretta (JiraApiService potrebbe non avere metodo update)
                using var httpClient = new System.Net.Http.HttpClient();

                var authHeader = _jiraApiService.GetAuthorizationHeader();
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var updatePayload = new
                {
                    fields = fields
                };

                var jsonContent = JsonConvert.SerializeObject(updatePayload);
                var content = new System.Net.Http.StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}";
                var response = await httpClient.PutAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error {response.StatusCode}: {errorContent}");
                }

                _logger.LogInfo($"{ticketKey}: Campi aggiornati via API");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore chiamata API update per {ticketKey}", ex);
                throw;
            }
        }

        private async Task ExecuteStatusTransitionsAsync(AutomationTicket ticket, CancellationToken cancellationToken)
        {
            try
            {
                var currentStatus = ticket.Status;
                _logger.LogInfo($"{ticket.Key}: Stato attuale: {currentStatus}");

                // VERIFICA CRITICA: Assegnatario deve essere presente per le transizioni
                if (string.IsNullOrWhiteSpace(ticket.Assignee))
                {
                    LogToFile($"{ticket.Key}: ERRORE - Assegnatario mancante, transizioni saltate", true);
                    _logger.LogWarning($"{ticket.Key}: Nessun assegnatario impostato, salto le transizioni");
                    return;
                }

                // Determina sequenza transizioni in base allo stato corrente
                var targetStates = currentStatus switch
                {
                    "Nuovo" => new[] { "Preso In Carico", "Assegnato (Primo Livello)", "Assegnato (Secondo Livello)" },
                    "Preso In Carico" => new[] { "Assegnato (Primo Livello)", "Assegnato (Secondo Livello)" },
                    _ => Array.Empty<string>()
                };

                if (targetStates.Length == 0)
                {
                    _logger.LogInfo($"{ticket.Key}: Nessuna transizione necessaria per stato: {currentStatus}");
                    return;
                }

                LogToFile($"{ticket.Key}: Inizio transizioni: {string.Join(" -> ", targetStates)}", false);

                // Esegui transizioni sequenziali
                foreach (var targetState in targetStates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var result = await _transitionService.TransitionToStatusAsync(ticket.Key, targetState);

                        if (result.Success)
                        {
                            LogToFile($"{ticket.Key}: Transizione {currentStatus} -> {targetState} completata", false);
                            _logger.LogInfo($"{ticket.Key}: Transizione verso {targetState} completata");
                            currentStatus = targetState;
                        }
                        else
                        {
                            LogToFile($"{ticket.Key}: Transizione verso {targetState} fallita: {result.ErrorMessage}", true);
                            _logger.LogWarning($"{ticket.Key}: Transizione verso {targetState} fallita: {result.ErrorMessage}");
                            break; // Interrompi sequenza se una transizione fallisce
                        }

                        // Piccola pausa tra transizioni
                        await Task.Delay(500, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"{ticket.Key}: Errore transizione verso {targetState}: {ex.Message}", true);
                        _logger.LogError($"Errore transizione {ticket.Key} verso {targetState}", ex);
                        break; // Continua con il prossimo ticket
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"{ticket.Key}: Errore generale transizioni: {ex.Message}", true);
                _logger.LogError($"Errore transizioni per {ticket.Key}", ex);
                // Non rilanciare - continua con il prossimo ticket
            }
        }

        #endregion

        #region Private Methods - Helpers

        private string GetSafeStringValue(JToken token)
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return null;

                if (token.Type == JTokenType.String)
                    return token.ToString();

                // Se è un oggetto, prova ad accedere a proprietà comuni
                if (token.Type == JTokenType.Object)
                {
                    var nameValue = token["name"]?.ToString();
                    if (!string.IsNullOrEmpty(nameValue))
                        return nameValue;

                    var displayNameValue = token["displayName"]?.ToString();
                    if (!string.IsNullOrEmpty(displayNameValue))
                        return displayNameValue;

                    var valueValue = token["value"]?.ToString();
                    if (!string.IsNullOrEmpty(valueValue))
                        return valueValue;
                }

                return token.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore GetSafeStringValue: {ex.Message}, Token: {token}");
                return null;
            }
        }

        private string GetCustomFieldValue(JToken issue, string fieldId)
        {
            try
            {
                var field = issue["fields"]?[fieldId];

                if (field == null || field.Type == JTokenType.Null)
                    return null;

                // Gestisce diversi tipi di campi custom
                if (field.Type == JTokenType.String)
                    return field.ToString();

                if (field.Type == JTokenType.Object)
                {
                    // Prova diverse proprietà comuni per custom fields
                    var valueProperty = field["value"];
                    if (valueProperty != null && valueProperty.Type != JTokenType.Null)
                        return valueProperty.ToString();

                    var displayNameProperty = field["displayName"];
                    if (displayNameProperty != null && displayNameProperty.Type != JTokenType.Null)
                        return displayNameProperty.ToString();

                    var nameProperty = field["name"];
                    if (nameProperty != null && nameProperty.Type != JTokenType.Null)
                        return nameProperty.ToString();
                }

                if (field.Type == JTokenType.Array && field.HasValues)
                {
                    var firstElement = field.First;
                    return GetSafeStringValue(firstElement);
                }

                return field.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore lettura campo {fieldId}: {ex.Message}");
                return null;
            }
        }

        private string ConvertDisplayNameToUsername(string displayName)
        {
            try
            {
                // MAPPING SPECIFICO per gli assegnatari Demografia
                var displayNameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Jonathan Felix Da Silva"] = "jonathan.felixdasilva",
                    ["Antonio Carcea"] = "antonio.carcea"
                };

                if (displayNameMappings.ContainsKey(displayName))
                {
                    var username = displayNameMappings[displayName];
                    _logger.LogInfo($"Mapping display name: '{displayName}' -> '{username}'");
                    return username;
                }

                // Se non troviamo mapping specifico, NON convertire
                _logger.LogWarning($"Nessun mapping trovato per display name: '{displayName}' - uso il valore originale");
                return displayName; // Usa il display name originale
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conversione display name: {displayName}", ex);
                return displayName; // Fallback al valore originale
            }
        }

        private void LogToFile(string message, bool isError)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";

                var targetFile = isError ? _currentErrorLogFile : _currentLogFile;

                if (!string.IsNullOrWhiteSpace(targetFile))
                {
                    File.AppendAllText(targetFile, logMessage + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore scrittura log file: {ex.Message}");
            }
        }

        #endregion
    }

    #region Configuration Classes

    /// <summary>
    /// Configurazione completa automazione
    /// </summary>
    public class AutomationConfig
    {
        public AutomationAreaConfig Demografia { get; set; }
    }

    /// <summary>
    /// Configurazione per una singola area
    /// </summary>
    public class AutomationAreaConfig
    {
        public string Query { get; set; }
        public Dictionary<string, string> Keywords { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Assignees { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Modello semplificato ticket Jira per automazione
    /// </summary>
    public class AutomationTicket
    {
        public string Key { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Assignee { get; set; }
        public string Organization { get; set; }
        public string Area { get; set; }
        public string Application { get; set; }
        public string Customer { get; set; }
        public string Category { get; set; }
        public JToken RawData { get; set; }
    }

    #endregion
}