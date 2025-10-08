using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using JiraTicketManager.Services;
using JiraTicketManager.Data.Models;
using JiraTicketManager.Data.Converters;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per l'automazione ticket Jira - Area Demografia
    /// Utilizza classi esistenti per parsing e aggiornamento campi
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

        // Configurazione workspace CMDB per categoria  
        private const string WORKSPACE_ID = "c541ca01-a3a4-400b-a389-573d1f19899a";
        private const string CATEGORIA_SUPPORTO_OBJECT_ID = "956"; // CORREZIONE: Prova 956 invece di 957

        #endregion

        #region Constructor

        public JiraAutomationService()
        {
            _logger = LoggingService.CreateForComponent("JiraAutomationService");

            // Usa servizi esistenti
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
        /// <summary>
        /// Esegue l'automazione completa per l'area Demografia
        /// VERSIONE SEMPLICE - SOLO CANCELLATION TOKEN
        /// </summary>
        public async Task<AutomationResult> ExecuteAutomationAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo("=== AVVIO AUTOMAZIONE AREA DEMOGRAFIA ===");

                // Inizializzazione
                await InitializeAutomationAsync(cancellationToken);
                await LoadConfigurationAsync(cancellationToken);

                // Ricerca ticket
                var tickets = await SearchDemografiaTicketsAsync(cancellationToken);

                if (tickets.Count == 0)
                {
                    LogToFile("Nessun ticket trovato per l'automazione", false);
                    return new AutomationResult { Success = true, ProcessedCount = 0 };
                }

                // Processa ogni ticket
                var processedCount = 0;
                var successCount = 0;

                LogToFile($"Inizio processamento {tickets.Count} ticket", false);

                foreach (var ticket in tickets)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var result = await ProcessSingleTicketAsync(ticket, cancellationToken);

                    processedCount++;
                    if (result.Success) successCount++;
                }

                // Risultato finale
                LogToFile($"Automazione completata: {successCount}/{processedCount} ticket processati con successo", false);
                _logger.LogInfo("=== AUTOMAZIONE COMPLETATA ===");

                return new AutomationResult
                {
                    Success = true,
                    ProcessedCount = processedCount,
                    SuccessCount = successCount
                };
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
                var connectionResult = await _jiraApiService.TestConnectionAsync();
                if (!connectionResult)
                {
                    throw new Exception("Connessione Jira fallita - verificare credenziali e connessione di rete");
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
                    await CreateDefaultConfigurationAsync(configPath);
                }

                var jsonContent = await File.ReadAllTextAsync(configPath, cancellationToken);
                var configData = JsonConvert.DeserializeObject<Dictionary<string, AutomationAreaConfig>>(jsonContent);

                if (!configData.ContainsKey("Demografia"))
                {
                    throw new Exception("Configurazione per area Demografia non trovata");
                }

                _config = new AutomationConfig { Demografia = configData["Demografia"] };

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

        private async Task<List<JiraTicket>> SearchDemografiaTicketsAsync(CancellationToken cancellationToken)
        {
            try
            {
                // USA LA QUERY DIRETTAMENTE DAL CONFIG
                var query = _config.Demografia.Query;
                LogToFile($"Query JQL: {query}", false);

                // USA JiraApiService esistente per la ricerca
                var searchResult = await _jiraApiService.SearchIssuesAsync(query, 0, 100);

                if (searchResult?.Issues == null || !searchResult.Issues.Any())
                {
                    LogToFile("Nessun ticket trovato per l'automazione", false);
                    return new List<JiraTicket>();
                }

                LogToFile($"API Search Result: Found={searchResult.Total}, Returned={searchResult.Issues.Count}", false);

                var tickets = new List<JiraTicket>();

                foreach (var issue in searchResult.Issues)
                {
                    try
                    {
                        LogToFile("=== DEBUG PARSING TICKET ===", false);

                        // DEBUG: Log del tipo di issue
                        LogToFile($"Issue type: {issue?.GetType()?.Name}", false);

                        // Test accesso key
                        var ticketKey = "UNKNOWN";
                        try
                        {
                            ticketKey = issue["key"]?.ToString();
                            LogToFile($"✓ Key estratto: {ticketKey}", false);
                        }
                        catch (Exception keyEx)
                        {
                            LogToFile($"✗ Errore estrazione key: {keyEx.Message}", true);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(ticketKey))
                        {
                            LogToFile("Key vuoto - saltato", true);
                            continue;
                        }

                        // Test accesso fields
                        var fields = issue["fields"];
                        if (fields == null)
                        {
                            LogToFile($"{ticketKey}: Fields è null", true);
                            continue;
                        }

                        LogToFile($"✓ Fields accessibili per {ticketKey}", false);

                        // Estrazione sicura campo per campo con debug
                        var ticket = new JiraTicket { Key = ticketKey, RawData = issue };

                        // Summary
                        try
                        {
                            ticket.Summary = fields["summary"]?.ToString() ?? "";
                            LogToFile($"✓ Summary: {ticket.Summary.Substring(0, Math.Min(50, ticket.Summary.Length))}...", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Summary: {ex.Message}", true);
                            ticket.Summary = "";
                        }

                        // Status
                        try
                        {
                            var statusObj = fields["status"];
                            if (statusObj != null && statusObj.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                ticket.Status = statusObj["name"]?.ToString() ?? "";
                            }
                            else
                            {
                                ticket.Status = statusObj?.ToString() ?? "";
                            }
                            LogToFile($"✓ Status: {ticket.Status}", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Status: {ex.Message}", true);
                            ticket.Status = "";
                        }

                        // Area (customfield_10113)
                        try
                        {
                            var areaField = fields["customfield_10113"];
                            if (areaField != null && areaField.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                            {
                                if (areaField.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                                {
                                    ticket.Area = areaField["value"]?.ToString() ?? areaField["displayName"]?.ToString() ?? "";
                                }
                                else
                                {
                                    ticket.Area = areaField.ToString();
                                }
                            }
                            else
                            {
                                ticket.Area = "";
                            }
                            LogToFile($"✓ Area: {ticket.Area}", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Area: {ex.Message}", true);
                            ticket.Area = "";
                        }

                        // Application (customfield_10114)
                        try
                        {
                            var appField = fields["customfield_10114"];
                            if (appField != null && appField.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                            {
                                if (appField.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                                {
                                    ticket.Application = appField["value"]?.ToString() ?? appField["displayName"]?.ToString() ?? "";
                                }
                                else
                                {
                                    ticket.Application = appField.ToString();
                                }
                            }
                            else
                            {
                                ticket.Application = "";
                            }
                            LogToFile($"✓ Application: {ticket.Application}", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Application: {ex.Message}", true);
                            ticket.Application = "";
                        }

                        // Assignee
                        try
                        {
                            var assigneeObj = fields["assignee"];
                            if (assigneeObj != null && assigneeObj.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                ticket.Assignee = assigneeObj["name"]?.ToString() ?? "";
                                ticket.AssigneeDisplayName = assigneeObj["displayName"]?.ToString() ?? "";
                            }
                            else
                            {
                                ticket.Assignee = "";
                                ticket.AssigneeDisplayName = "";
                            }
                            LogToFile($"✓ Assignee: {ticket.AssigneeDisplayName}", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Assignee: {ex.Message}", true);
                            ticket.Assignee = "";
                            ticket.AssigneeDisplayName = "";
                        }

                        // Organization
                        try
                        {
                            var orgObj = fields["organization"];
                            if (orgObj != null && orgObj.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                ticket.Organization = orgObj["name"]?.ToString() ?? "";
                            }
                            else
                            {
                                ticket.Organization = orgObj?.ToString() ?? "";
                            }
                            LogToFile($"✓ Organization: {ticket.Organization}", false);
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"✗ Errore Organization: {ex.Message}", true);
                            ticket.Organization = "";
                        }

                        // Campi data placeholder
                        ticket.Created = DateTime.Now;
                        ticket.Updated = DateTime.Now;
                        ticket.Description = "";

                        tickets.Add(ticket);
                        LogToFile($"✅ TICKET PROCESSATO: {ticket.Key} - {ticket.Status} - Area: {ticket.Area}", false);
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"✗ ERRORE GENERALE parsing ticket: {ex.Message}", true);
                        LogToFile($"✗ StackTrace: {ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}", true);
                        // Continua con il prossimo ticket
                    }
                }

                LogToFile($"=== TOTALE TICKET PROCESSABILI: {tickets.Count} ===", false);
                return tickets;
            }
            catch (Exception ex)
            {
                LogToFile($"Errore ricerca ticket: {ex.Message}", true);
                _logger.LogError("Errore ricerca ticket Demografia", ex);
                throw;
            }
        }

        private async Task<(bool Success, string Message)> ProcessSingleTicketAsync(JiraTicket ticket, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInfo($"Processamento ticket: {ticket.Key}");
                LogToFile($"Inizio processamento ticket: {ticket.Key}", false);

                var updatedFields = new List<string>();
                var failedFields = new List<string>();

                // 1. CATEGORIA - Sempre "SUPPORTO CLIENTE"
                await UpdateCategoriaAsync(ticket.Key, updatedFields, failedFields);

                // 2. APPLICATIVO - Solo se vuoto
                if (string.IsNullOrWhiteSpace(ticket.Application))
                {
                    await UpdateApplicativoAsync(ticket, updatedFields, failedFields);
                }

                // 3. ASSEGNATARIO - Basato sull'applicativo
                await UpdateAssegnatarioAsync(ticket, updatedFields, failedFields);

                // 4. CLIENTE - Solo se vuoto e organizzazione presente
                if (string.IsNullOrWhiteSpace(GetCustomerFromTicket(ticket)) && !string.IsNullOrWhiteSpace(ticket.Organization))
                {
                    await UpdateClienteAsync(ticket, updatedFields, failedFields, cancellationToken);
                }

                // 5. TRANSIZIONI DI STATO
                await ExecuteStatusTransitionsAsync(ticket, cancellationToken);

                // Summary risultati
                var totalUpdated = updatedFields.Count;
                var totalFailed = failedFields.Count;

                if (totalUpdated > 0)
                {
                    LogToFile($"{ticket.Key}: SUCCESSO - {totalUpdated} campi aggiornati: {string.Join(", ", updatedFields)}", false);
                }

                if (totalFailed > 0)
                {
                    LogToFile($"{ticket.Key}: FALLIMENTI - {totalFailed} campi falliti: {string.Join(", ", failedFields)}", true);
                }

                var message = $"Processato - {totalUpdated} successi, {totalFailed} fallimenti";
                return (totalUpdated > 0, message);
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

        #region Private Methods - Field Updates (Using Existing Services)

        private async Task UpdateCategoriaAsync(string ticketKey, List<string> updatedFields, List<string> failedFields)
        {
            try
            {
                // USA UpdateWorkspaceFieldAsync esistente
                var success = await _jiraApiService.UpdateWorkspaceFieldAsync(
                    ticketKey,
                    "customfield_10095",
                    WORKSPACE_ID,
                    CATEGORIA_SUPPORTO_OBJECT_ID);

                if (success)
                {
                    updatedFields.Add("Categoria: SUPPORTO CLIENTE");
                    LogToFile($"{ticketKey}: ✓ Categoria aggiornata: SUPPORTO CLIENTE", false);
                }
                else
                {
                    failedFields.Add("Categoria");
                    LogToFile($"{ticketKey}: ✗ Errore aggiornamento categoria", true);
                }
            }
            catch (Exception ex)
            {
                failedFields.Add("Categoria");
                LogToFile($"{ticketKey}: ✗ Eccezione categoria: {ex.Message}", true);
                _logger.LogError($"Errore categoria per {ticketKey}", ex);
            }
        }

        private async Task UpdateApplicativoAsync(JiraTicket ticket, List<string> updatedFields, List<string> failedFields)
        {
            try
            {
                var newApplication = DetermineApplication(ticket);
                if (!string.IsNullOrWhiteSpace(newApplication))
                {
                    // USA UpdateOptionFieldAsync esistente
                    var success = await _jiraApiService.UpdateOptionFieldAsync(
                        ticket.Key, "customfield_10114", newApplication);

                    if (success)
                    {
                        updatedFields.Add($"Applicativo: {newApplication}");
                        LogToFile($"{ticket.Key}: ✓ Applicativo aggiornato: {newApplication}", false);
                    }
                    else
                    {
                        failedFields.Add("Applicativo");
                        LogToFile($"{ticket.Key}: ✗ Errore aggiornamento applicativo", true);
                    }
                }
            }
            catch (Exception ex)
            {
                failedFields.Add("Applicativo");
                LogToFile($"{ticket.Key}: ✗ Eccezione applicativo: {ex.Message}", true);
                _logger.LogError($"Errore applicativo per {ticket.Key}", ex);
            }
        }

        private async Task UpdateAssegnatarioAsync(JiraTicket ticket, List<string> updatedFields, List<string> failedFields)
        {
            try
            {
                var assignee = DetermineAssignee(ticket.Application);
                if (!string.IsNullOrWhiteSpace(assignee))
                {
                    var username = ConvertDisplayNameToUsername(assignee);

                    // USA UpdateIssueAsync esistente con formato corretto
                    var assigneeFields = new Dictionary<string, object>
                    {
                        ["assignee"] = new { name = username }
                    };

                    var success = await _jiraApiService.UpdateIssueAsync(
                        ticket.Key, new { fields = assigneeFields });

                    if (success)
                    {
                        updatedFields.Add($"Assegnatario: {assignee} ({username})");
                        LogToFile($"{ticket.Key}: ✓ Assegnatario aggiornato: {assignee} -> {username}", false);
                    }
                    else
                    {
                        failedFields.Add("Assegnatario");
                        LogToFile($"{ticket.Key}: ✗ Errore aggiornamento assegnatario", true);
                    }
                }
                else
                {
                    failedFields.Add("Assegnatario");
                    LogToFile($"{ticket.Key}: ✗ Assegnatario non determinabile", true);
                }
            }
            catch (Exception ex)
            {
                failedFields.Add("Assegnatario");
                LogToFile($"{ticket.Key}: ✗ Eccezione assegnatario: {ex.Message}", true);
                _logger.LogError($"Errore assegnatario per {ticket.Key}", ex);
            }
        }

        private async Task UpdateClienteAsync(JiraTicket ticket, List<string> updatedFields, List<string> failedFields, CancellationToken cancellationToken)
        {
            try
            {
                var customer = await DetermineCustomerAsync(ticket.Organization, cancellationToken);
                if (!string.IsNullOrWhiteSpace(customer))
                {
                    // USA UpdateTextFieldAsync esistente
                    var success = await _jiraApiService.UpdateTextFieldAsync(
                        ticket.Key, "customfield_10115", customer);

                    if (success)
                    {
                        updatedFields.Add($"Cliente: {customer}");
                        LogToFile($"{ticket.Key}: ✓ Cliente aggiornato: {customer}", false);
                    }
                    else
                    {
                        failedFields.Add("Cliente");
                        LogToFile($"{ticket.Key}: ✗ Errore aggiornamento cliente", true);
                    }
                }
            }
            catch (Exception ex)
            {
                failedFields.Add("Cliente");
                LogToFile($"{ticket.Key}: ✗ Eccezione cliente: {ex.Message}", true);
                _logger.LogError($"Errore cliente per {ticket.Key}", ex);
            }
        }

        #endregion

        #region Private Methods - Business Logic

        private string DetermineApplication(JiraTicket ticket)
        {
            try
            {
                var searchText = $"{ticket.Summary} {ticket.Description}".ToUpper();

                // Cerca keywords CASE-SENSITIVE
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
                if (string.IsNullOrWhiteSpace(application)) return null;

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
                if (string.IsNullOrWhiteSpace(organizationName)) return null;

                // Esegui query per trovare ticket con Cliente valorizzato
                var query = $"reporter in organizationMembers({organizationName}) AND project = CC AND issuetype is not EMPTY";
                _logger.LogInfo($"Ricerca cliente per organizzazione: {organizationName}");

                var searchResult = await _jiraApiService.SearchIssuesAsync(query, 0, 10);

                foreach (var issue in searchResult.Issues)
                {
                    // USA il metodo esistente per estrazione sicura
                    var ticket = JiraTicket.FromJiraJson(issue);
                    var customerValue = GetCustomerFromTicket(ticket);

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

        private async Task ExecuteStatusTransitionsAsync(JiraTicket ticket, CancellationToken cancellationToken)
        {
            try
            {
                var currentStatus = ticket.Status;
                _logger.LogInfo($"{ticket.Key}: Stato attuale: {currentStatus}");

                // Determina sequenza transizioni
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

                // USA JiraTransitionService esistente
                foreach (var targetState in targetStates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var result = await _transitionService.TransitionToStatusAsync(ticket.Key, targetState);

                        if (result.Success)
                        {
                            LogToFile($"{ticket.Key}: Transizione {currentStatus} -> {targetState} completata", false);
                            currentStatus = targetState;
                        }
                        else
                        {
                            LogToFile($"{ticket.Key}: Transizione verso {targetState} fallita: {result.ErrorMessage}", true);
                            break;
                        }

                        await Task.Delay(500, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"{ticket.Key}: Errore transizione verso {targetState}: {ex.Message}", true);
                        _logger.LogError($"Errore transizione {ticket.Key} verso {targetState}", ex);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogToFile($"{ticket.Key}: Errore generale transizioni: {ex.Message}", true);
                _logger.LogError($"Errore transizioni per {ticket.Key}", ex);
            }
        }

        #endregion

        #region Private Methods - Helpers

        private string GetCustomerFromTicket(JiraTicket ticket)
        {
            try
            {
                // Prova ad estrarre il cliente dai raw data se disponibili
                if (ticket.RawData != null)
                {
                    var customerField = ticket.RawData["fields"]?["customfield_10115"];
                    if (customerField != null && customerField.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                    {
                        return customerField.ToString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore estrazione cliente: {ex.Message}");
                return null;
            }
        }

        private string ConvertDisplayNameToUsername(string displayName)
        {
            try
            {
                // Mapping specifico per Demografia
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

                _logger.LogWarning($"Nessun mapping trovato per display name: '{displayName}' - uso il valore originale");
                return displayName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conversione display name: {displayName}", ex);
                return displayName;
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
    /// Risultato dell'automazione
    /// </summary>
    public class AutomationResult
    {
        public bool Success { get; set; }
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}