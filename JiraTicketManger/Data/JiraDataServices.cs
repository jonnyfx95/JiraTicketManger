using JiraTicketManager.Business;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Data.Models;
using JiraTicketManager.Helpers;
using JiraTicketManager.Services;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JiraTicketManager.Data
{
    /// <summary>
    /// Implementazione del servizio dati Jira che utilizza JiraApiService esistente.
    /// Fornisce un'interfaccia moderna e tipizzata per l'accesso ai dati Jira.
    /// </summary>
    public class JiraDataService : IJiraDataService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;
        private readonly JiraDataServiceConfig _config;
        private readonly Dictionary<JiraFieldType, List<JiraField>> _fieldCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;

        // Events
        public event EventHandler<JiraErrorEventArgs> ConnectionError;
        public event EventHandler<ProgressEventArgs> ProgressUpdated;

        public JiraDataService(JiraApiService jiraApiService, JiraDataServiceConfig config = null)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("JiraDataService");
            _config = config ?? CreateDefaultConfig();
        }

        #region Properties

        public bool IsConfigured => !string.IsNullOrEmpty(_jiraApiService.Domain);
        public string Domain => _jiraApiService.Domain;

        #endregion

        #region Connection & Authentication

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                _logger.LogInfo("Test connessione Jira API");
                var result = await _jiraApiService.TestConnectionAsync();
                _logger.LogInfo($"Test connessione risultato: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Test connessione fallito", ex);
                ConnectionError?.Invoke(this, new JiraErrorEventArgs("Test connessione fallito", ex, "TestConnection"));
                return false;
            }
        }

        #endregion

        #region Ticket Search & Retrieval

        public async Task<JiraSearchResult> SearchTicketsAsync(JiraSearchCriteria criteria, PaginationConfig pagination)
        {
            try
            {
                var jql = JQLBuilder.FromCriteria(criteria).Build();
                return await SearchTicketsAsync(jql, pagination.StartAt, pagination.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca ticket con criteri", ex);
                throw;
            }
        }

        public async Task<JiraSearchResult> SearchTicketsAsync(string jql, int startAt = 0, int maxResults = 50)
        {
            try
            {
                _logger.LogInfo($"Ricerca ticket JQL: {jql}");
                ProgressUpdated?.Invoke(this, new ProgressEventArgs("Ricerca ticket...", 0, "Search"));

                // Utilizza il metodo esistente di JiraApiService
                var searchResult = await _jiraApiService.SearchIssuesAsync(jql, startAt, maxResults);

                // Converte il risultato nel nuovo formato tipizzato
                var result = ConvertToJiraSearchResult(searchResult);

                _logger.LogInfo($"Ricerca completata: {result.Total} ticket, pagina {result.CurrentPage}/{result.TotalPages}");
                ProgressUpdated?.Invoke(this, new ProgressEventArgs("Ricerca completata", 100, "Search"));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca ticket", ex);
                ConnectionError?.Invoke(this, new JiraErrorEventArgs("Errore ricerca ticket", ex, "SearchTickets"));
                throw;
            }
        }

        public async Task<JiraTicket> GetTicketAsync(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Caricamento ticket: {ticketKey}");

                // Utilizza il metodo di ricerca esistente con JQL specifica
                var jql = $"key = {ticketKey}";
                var result = await SearchTicketsAsync(jql, 0, 1);

                return result.Issues.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento ticket {ticketKey}", ex);
                throw;
            }
        }

        public async Task<JiraSearchResult> LoadInitialDataAsync(int pageSize = 50)
        {
            try
            {
                _logger.LogInfo("Caricamento dati iniziali");
                var baseJql = _config.BaseJQL;
                return await SearchTicketsAsync(baseJql, 0, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento dati iniziali", ex);
                throw;
            }
        }

        #endregion

        #region Field Values Loading

        public async Task<List<JiraField>> GetCustomFieldValuesAsync(string fieldId, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento custom field: {fieldId}");

                var fieldType = GetFieldTypeFromCustomFieldId(fieldId);

                // Usa il sistema esistente di GetFieldValuesAsync
                return await GetFieldValuesAsync(fieldType, progress);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento custom field {fieldId}", ex);
                throw;
            }
        }

        public async Task<List<JiraField>> GetFieldValuesAsync(JiraFieldType fieldType, IProgress<string> progress = null, string ticketKey = null)
        {
            try
            {
                _logger.LogInfo($"🔄 Inizio caricamento campo: {fieldType}");

                // Cache check
                if (_config.EnableCaching && IsCacheValid(fieldType))
                {
                    _logger.LogDebug($"💾 Utilizzando cache per {fieldType}");
                    return _fieldCache[fieldType];
                }

                _logger.LogInfo($"📊 Caricamento valori campo: {fieldType}");
                progress?.Report($"Caricamento {JiraFieldTypeHelper.GetDisplayName(fieldType)}...");

                // 1. 🧠 AUTO-STRATEGY DETECTION
                var strategy = DetermineLoadingStrategy(fieldType);
                _logger.LogDebug($"🎯 Strategia selezionata per {fieldType}: {strategy}");

                // 2. 🔄 ESECUZIONE STRATEGIA
                List<string> rawValues = strategy switch
                {
                    LoadingStrategy.DirectAPI => await LoadFromDirectAPI(fieldType, progress, ticketKey), // ✅ MODIFICA: Aggiunto ticketKey
                    LoadingStrategy.JQLSearch => await LoadFromJQLSearch(fieldType, progress),
                    LoadingStrategy.ServiceDeskAPI => await LoadFromServiceDeskAPI(fieldType, progress),
                    LoadingStrategy.Hybrid => await LoadFromHybridStrategy(fieldType, progress),
                    _ => throw new NotSupportedException($"Strategia {strategy} non supportata per {fieldType}")
                };

                // 3. 🎯 CONVERSIONE A JIRAFIELD
                var results = ConvertToJiraFields(rawValues, fieldType);

                // 4. 💾 SALVATAGGIO CACHE
                if (_config.EnableCaching)
                {
                    _fieldCache[fieldType] = results;
                    _lastCacheUpdate = DateTime.Now;
                    _logger.LogDebug($"💾 Cache aggiornata per {fieldType}: {results.Count} elementi");
                }

                _logger.LogInfo($"✅ Caricamento completato: {results.Count} valori per {fieldType}");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore caricamento {fieldType}", ex);
                throw;
            }
        }

        #region Loading Strategy Detection & Enums

        /// <summary>
        /// Strategie di caricamento per diversi tipi di campi
        /// </summary>
        private enum LoadingStrategy
        {
            DirectAPI,        // Stati, Priorità, Tipi → /rest/api/2/status
            JQLSearch,        // Custom Fields, Assegnatari → JQL search  
            ServiceDeskAPI,   // Organizzazioni → /rest/servicedeskapi/organization
            Hybrid           // Organizzazioni con fallback API + JQL
        }

        /// <summary>
        /// Determina la strategia di caricamento ottimale per un tipo di campo
        /// </summary>
        private LoadingStrategy DetermineLoadingStrategy(JiraFieldType fieldType)
        {
            try
            {
                _logger.LogDebug($"🧠 Determinazione strategia per {fieldType}");

                // ✅ MODIFICA: Organizations usa DirectAPI con paginazione
                if (fieldType == JiraFieldType.Organization)
                {
                    _logger.LogDebug($"🏢 {fieldType} → Strategia DirectAPI (con paginazione)");
                    return LoadingStrategy.DirectAPI;
                }

                // Custom Fields → DirectAPI con CreateMeta
                if (JiraFieldTypeHelper.IsCustomField(fieldType))
                {
                    _logger.LogDebug($"🔧 {fieldType} → Strategia DirectAPI (CreateMeta per custom field)");
                    return LoadingStrategy.DirectAPI;
                }

                // Assegnatari: DirectAPI
                if (fieldType == JiraFieldType.Assignee)
                {
                    _logger.LogDebug($"👤 {fieldType} → Strategia DirectAPI (endpoint assignable)");
                    return LoadingStrategy.DirectAPI;
                }

                // Campi con endpoint API diretti
                if (JiraFieldTypeHelper.HasDirectApiEndpoint(fieldType))
                {
                    _logger.LogDebug($"🔗 {fieldType} → Strategia DirectAPI (endpoint: {JiraFieldTypeHelper.GetApiEndpoint(fieldType)})");
                    return LoadingStrategy.DirectAPI;
                }

                // Fallback
                _logger.LogWarning($"⚠️ {fieldType} → Fallback a DirectAPI (strategia non determinata)");
                return LoadingStrategy.DirectAPI;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore determinazione strategia per {fieldType}", ex);
                return LoadingStrategy.DirectAPI;
            }
        }

        #endregion



        #endregion

        #region Batch Operations

        public async Task<Dictionary<JiraFieldType, List<JiraField>>> LoadAllFieldValuesAsync(IProgress<string> progress = null)
        {
            var fieldTypes = new[]
            {
                JiraFieldType.Organization,
                JiraFieldType.Status,
                JiraFieldType.Priority,
                JiraFieldType.IssueType,
                JiraFieldType.Area,
                JiraFieldType.Application,
                JiraFieldType.Assignee
            };

            return await LoadFieldValuesAsync(fieldTypes, progress);
        }

        public async Task<Dictionary<JiraFieldType, List<JiraField>>> LoadFieldValuesAsync(
            IEnumerable<JiraFieldType> fieldTypes, IProgress<string> progress = null)
        {
            var result = new Dictionary<JiraFieldType, List<JiraField>>();
            var fieldTypesList = fieldTypes.ToList();

            for (int i = 0; i < fieldTypesList.Count; i++)
            {
                var fieldType = fieldTypesList[i];
                var progressMessage = $"Caricamento {JiraFieldTypeHelper.GetDisplayName(fieldType)} ({i + 1}/{fieldTypesList.Count})";
                progress?.Report(progressMessage);

                try
                {
                    var values = await GetFieldValuesAsync(fieldType, progress);
                    result[fieldType] = values;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore caricamento {fieldType} in batch", ex);
                    result[fieldType] = new List<JiraField>();
                }
            }

            return result;
        }

        #endregion

        #region Loading Strategy Implementation


        /// <summary>
        /// Carica valori da API dirette (Stati, Priorità, Tipi)
        /// </summary>

        private async Task<List<string>> LoadFromDirectAPI(JiraFieldType fieldType, IProgress<string> progress, string ticketKey = null)
        {
            try
            {
                _logger.LogInfo($"🚀 Caricamento {fieldType} con strategia DirectAPI");

                return fieldType switch
                {
                    JiraFieldType.Organization => await LoadOrganizationsWithPagination(progress),
                    JiraFieldType.Consulente or JiraFieldType.PM or JiraFieldType.Commerciale =>
                        await LoadUserPickerFieldValues(fieldType, progress, ticketKey),
                    _ when JiraFieldTypeHelper.IsCustomField(fieldType) =>
                        await LoadCustomFieldValues(fieldType, progress),
                    _ => await LoadStandardFieldValues(fieldType, progress)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore LoadFromDirectAPI per {fieldType}", ex);
                throw;
            }
        }




        /// <summary>
        /// Estrae allowedValues dal JSON CreateMeta per custom field
        /// </summary>
        private List<string> ExtractAllowedValuesFromCreateMeta(JObject createMeta, string customFieldId)
        {
            try
            {
                var values = new List<string>();

                var projects = createMeta["projects"] as JArray;
                if (projects == null)
                {
                    _logger.LogWarning("Nessun progetto trovato in CreateMeta");
                    return values;
                }

                foreach (var project in projects)
                {
                    var issuetypes = project["issuetypes"] as JArray;
                    if (issuetypes == null) continue;

                    foreach (var issuetype in issuetypes)
                    {
                        var fields = issuetype["fields"];
                        var field = fields?[customFieldId];
                        var allowedValues = field?["allowedValues"] as JArray;

                        if (allowedValues != null)
                        {
                            foreach (var allowedValue in allowedValues)
                            {
                                var value = allowedValue["value"]?.ToString();
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    values.Add(value.Trim());
                                }
                            }
                        }
                    }
                }

                return values.Distinct().OrderBy(v => v).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore estrazione allowedValues per {customFieldId}", ex);
                return new List<string>();
            }
        }

        /// <summary>
        /// Pulisce i nomi dei tipi di ticket rimuovendo prefissi come [System]
        /// </summary>
        private string CleanIssueTypeName(string issueTypeName)
        {
            if (string.IsNullOrWhiteSpace(issueTypeName))
                return issueTypeName;

            // Rimuovi [System] e altri pattern simili
            var cleaned = issueTypeName
                .Replace("[System]", "", StringComparison.OrdinalIgnoreCase)
                .Replace("[system]", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            return cleaned;
        }

        /// <summary>
        /// Carica valori tramite ricerca JQL (Custom Fields, Assegnatari)
        /// </summary>
        private async Task<List<string>> LoadFromJQLSearch(JiraFieldType fieldType, IProgress<string> progress)
        {
            try
            {
                _logger.LogInfo($"🔍 Caricamento tramite JQL search per {fieldType}");

                string jql;
                string extractionField;

                if (JiraFieldTypeHelper.IsCustomField(fieldType))
                {
                    var fieldId = JiraFieldTypeHelper.GetCustomFieldId(fieldType);
                    jql = $"project = {_config.BaseJQL.Split(' ').FirstOrDefault(x => x != "project" && x != "=")} AND {fieldId} IS NOT EMPTY ORDER BY updated DESC";
                    extractionField = fieldId;

                    _logger.LogDebug($"🔧 Custom field JQL: {jql}");
                }
                else if (fieldType == JiraFieldType.Assignee)
                {
                    jql = $"project = {_config.BaseJQL.Split(' ').FirstOrDefault(x => x != "project" && x != "=")} AND assignee IS NOT EMPTY ORDER BY updated DESC";
                    extractionField = "assignee";

                    _logger.LogDebug($"👤 Assignee JQL: {jql}");
                }
                else
                {
                    throw new NotSupportedException($"JQL search non supportata per {fieldType}");
                }

                progress?.Report($"Ricerca valori {JiraFieldTypeHelper.GetDisplayName(fieldType)}...");

                // Usa JiraApiService esistente per la ricerca
                var searchResult = await _jiraApiService.SearchIssuesAsync(jql, 0, 1000);
                _logger.LogInfo($"📊 Ricerca JQL completata: {searchResult.Total} ticket analizzati");

                // Estrai valori univoci usando JiraDataConverter esistente
                var uniqueValues = new HashSet<string>();

                foreach (var issue in searchResult.Issues)
                {
                    var fields = issue["fields"];
                    string extractedValue;

                    if (JiraFieldTypeHelper.IsCustomField(fieldType))
                    {
                        // Usa JiraDataConverter esistente per custom fields
                        extractedValue = JiraDataConverter.GetCustomFieldValue(fields, extractionField);
                    }
                    else
                    {
                        // Usa JiraDataConverter esistente per campi standard
                        extractedValue = JiraDataConverter.GetSafeStringValue(fields?[extractionField]);
                    }

                    if (!string.IsNullOrWhiteSpace(extractedValue) && extractedValue != "[Oggetto complesso]")
                    {
                        uniqueValues.Add(extractedValue.Trim());
                    }
                }

                var result = uniqueValues.OrderBy(x => x).ToList();
                _logger.LogInfo($"✅ JQL search completata: {result.Count} valori univoci per {fieldType}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore JQL search per {fieldType}", ex);
                throw;
            }
        }

        /// <summary>
        /// Carica valori da ServiceDesk API (Organizzazioni)
        /// </summary>
        private async Task<List<string>> LoadFromServiceDeskAPI(JiraFieldType fieldType, IProgress<string> progress)
        {
            try
            {
                _logger.LogInfo($"🏢 Caricamento da ServiceDesk API per {fieldType}");
                progress?.Report("Caricamento organizzazioni...");

                // TODO: Implementare chiamata ServiceDesk API reale
                // Per ora placeholder
                await Task.Delay(1000);

                var mockOrganizations = new List<string>
        {
            "Deda Group", "Cliente A", "Cliente B", "Partner XYZ"
        };

                _logger.LogInfo($"✅ ServiceDesk API completata: {mockOrganizations.Count} organizzazioni");
                return mockOrganizations;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore ServiceDesk API per {fieldType}", ex);
                throw;
            }
        }

        /// <summary>
        /// Strategia ibrida: prova API, poi fallback a JQL
        /// </summary>
        private async Task<List<string>> LoadFromHybridStrategy(JiraFieldType fieldType, IProgress<string> progress)
        {
            try
            {
                _logger.LogInfo($"🔄 Strategia ibrida per {fieldType}");

                // Prova prima ServiceDesk API
                try
                {
                    progress?.Report("Tentativo caricamento da API...");
                    var apiResult = await LoadFromServiceDeskAPI(fieldType, progress);

                    if (apiResult.Count >= 200) // Soglia di successo
                    {
                        _logger.LogInfo($"✅ API principale riuscita: {apiResult.Count} elementi");
                        return apiResult;
                    }

                    _logger.LogWarning($"⚠️ API principale insufficiente: {apiResult.Count} elementi, provo fallback");
                }
                catch (Exception apiEx)
                {
                    _logger.LogWarning($"⚠️ API principale fallita: {apiEx.Message}, provo fallback");
                }

                // Fallback a JQL search
                progress?.Report("Fallback a ricerca nei ticket...");
                var jqlResult = await LoadFromJQLSearch(fieldType, progress);

                _logger.LogInfo($"✅ Fallback JQL completato: {jqlResult.Count} elementi");
                return jqlResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore strategia ibrida per {fieldType}", ex);
                throw;
            }
        }

        /// <summary>
        /// Converte lista di stringhe in JiraField tipizzati
        /// </summary>
        private List<JiraField> ConvertToJiraFields(List<string> rawValues, JiraFieldType fieldType)
        {
            try
            {
                _logger.LogDebug($"🎯 Conversione {rawValues.Count} valori in JiraField per {fieldType}");

                var fieldId = JiraFieldTypeHelper.IsCustomField(fieldType)
                    ? JiraFieldTypeHelper.GetCustomFieldId(fieldType)
                    : fieldType.ToString().ToLower();

                var displayName = JiraFieldTypeHelper.GetDisplayName(fieldType);

                var results = rawValues
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => JiraField.Create(fieldId, displayName, value, fieldType))
                    .ToList();

                _logger.LogDebug($"✅ Conversione completata: {results.Count} JiraField creati");
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore conversione per {fieldType}", ex);
                throw;
            }
        }

        #endregion

        #region Statistics & Analysis

        public async Task<JiraStatistics> GetStatisticsAsync(JiraSearchCriteria criteria = null)
        {
            try
            {
                var jql = criteria != null ? JQLBuilder.FromCriteria(criteria).Build() : _config.BaseJQL;
                var result = await SearchTicketsAsync(jql, 0, 1000); // Carica più ticket per statistiche

                var statistics = new JiraStatistics
                {
                    TotalTickets = result.Total,
                    TicketsByStatus = result.Issues.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count()),
                    TicketsByPriority = result.Issues.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count()),
                    TicketsByType = result.Issues.GroupBy(t => t.IssueType).ToDictionary(g => g.Key, g => g.Count()),
                    TicketsByAssignee = result.Issues.GroupBy(t => t.AssigneeDisplayName).ToDictionary(g => g.Key, g => g.Count()),
                    OldestTicket = result.Issues.Any() ? result.Issues.Min(t => t.Created) : null,
                    NewestTicket = result.Issues.Any() ? result.Issues.Max(t => t.Created) : null
                };

                if (result.Issues.Any())
                {
                    var avgTicks = result.Issues.Average(t => (DateTime.Now - t.Created).Ticks);
                    statistics.AverageAge = new TimeSpan((long)avgTicks);
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore calcolo statistiche", ex);
                throw;
            }
        }

        public async Task<int> CountTicketsAsync(JiraSearchCriteria criteria)
        {
            try
            {
                var jql = JQLBuilder.FromCriteria(criteria).Build();
                var result = await SearchTicketsAsync(jql, 0, 1);
                return result.Total;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore conteggio ticket", ex);
                throw;
            }
        }

        #endregion

        #region Private Helper Methods

        private JiraSearchResult ConvertToJiraSearchResult(dynamic oldSearchResult)
        {
            // Converte il risultato del VB.NET JiraApiService nel nuovo formato
            // Questo dovrebbe adattarsi alla struttura esistente
            var result = new JiraSearchResult
            {
                StartAt = oldSearchResult.StartAt,
                MaxResults = oldSearchResult.MaxResults,
                Total = oldSearchResult.Total
            };

            if (oldSearchResult.Issues != null)
            {
                foreach (var issue in oldSearchResult.Issues)
                {
                    var ticket = JiraTicket.FromJiraJson(issue);
                    result.Issues.Add(ticket);
                }
            }

            return result;
        }


        /// <summary>
        /// Ottiene il JiraFieldType dal Custom Field ID
        /// </summary>
        private JiraFieldType GetFieldTypeFromCustomFieldId(string fieldId)
        {
            return fieldId switch
            {
                "customfield_10113" => JiraFieldType.Area,
                "customfield_10114" => JiraFieldType.Application,
                "customfield_10238" => JiraFieldType.Consulente,
                "customfield_10271" => JiraFieldType.PM,          
                "customfield_10272" => JiraFieldType.Commerciale,
                "customfield_10117" => JiraFieldType.Cliente,
                _ => JiraFieldType.CustomField
            };
        }

        private bool IsCacheValid(JiraFieldType fieldType)
        {
            return _fieldCache.ContainsKey(fieldType) &&
                   DateTime.Now - _lastCacheUpdate < _config.CacheExpiry;
        }

        private static JiraDataServiceConfig CreateDefaultConfig()
        {
            return new JiraDataServiceConfig
            {
                BaseJQL = "project = CC AND statuscategory = \"In Progress\" ORDER BY updated DESC",
                TimeoutSeconds = 30,
                MaxRetries = 3,
                EnableCaching = true,
                CacheExpiry = TimeSpan.FromMinutes(15)
            };
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Crea un JiraDataService dalle impostazioni correnti
        /// </summary>
        public static JiraDataService CreateFromSettings()
        {
            var settingsService = SettingsService.CreateDefault();
            var jiraApiService = new JiraApiService(settingsService);
            return new JiraDataService(jiraApiService);
        }

        /// <summary>
        /// Crea un JiraDataService con configurazione personalizzata
        /// </summary>
        public static JiraDataService CreateWithConfig(JiraDataServiceConfig config)
        {
            var settingsService = SettingsService.CreateDefault();
            var jiraApiService = new JiraApiService(settingsService);
            return new JiraDataService(jiraApiService, config);
        }

        #endregion

        #region Organizations Loading

        /// <summary>
        /// Carica le organizzazioni con paginazione tramite ServiceDesk API
        /// </summary>
        private async Task<List<string>> LoadOrganizationsWithPagination(IProgress<string> progress)
        {
            _logger.LogInfo($"🏢 Caricamento organizzazioni con paginazione");
            progress?.Report("Caricamento organizzazioni...");

            using var httpClient = CreateHttpClient();
            var allOrganizations = new List<string>();
            var start = 0;
            var batchSize = 50;
            var batchNumber = 1;
            var maxBatches = 25;
            bool morePages = true;

            while (morePages && batchNumber <= maxBatches)
            {
                progress?.Report($"Caricamento organizzazioni... (batch {batchNumber}, trovate {allOrganizations.Count})");

                var batch = await LoadOrganizationBatch(httpClient, start, batchSize, batchNumber);

                if (batch.Count > 0)
                {
                    allOrganizations.AddRange(batch);
                    _logger.LogInfo($"📦 Batch {batchNumber}: {batch.Count} organizzazioni, totale: {allOrganizations.Count}");

                    if (batch.Count < batchSize)
                    {
                        _logger.LogInfo($"🏁 Fine paginazione: ricevute {batch.Count} < {batchSize}");
                        morePages = false;
                    }
                    else
                    {
                        start += batchSize;
                        batchNumber++;
                        await Task.Delay(100); // Pausa per non sovraccaricare l'API
                    }
                }
                else
                {
                    _logger.LogInfo($"📭 Batch {batchNumber}: nessun risultato, fine paginazione");
                    morePages = false;
                }
            }

            var distinctOrganizations = allOrganizations.Distinct().OrderBy(n => n).ToList();
            _logger.LogInfo($"🎯 Organizations caricate: {distinctOrganizations.Count} (batch processati: {batchNumber - 1})");
            return distinctOrganizations;
        }

        /// <summary>
        /// Carica un singolo batch di organizzazioni
        /// </summary>
        private async Task<List<string>> LoadOrganizationBatch(HttpClient httpClient, int start, int batchSize, int batchNumber)
        {
            var organizations = new List<string>();

            try
            {
                var paginatedUrl = $"{Domain}/rest/servicedeskapi/organization?start={start}&limit={batchSize}";
                _logger.LogInfo($"📄 Batch {batchNumber}: {paginatedUrl}");

                using var orgRequest = new HttpRequestMessage(HttpMethod.Get, paginatedUrl);
                orgRequest.Headers.Add("Accept", "application/json");

                using var orgResponse = await httpClient.SendAsync(orgRequest);

                if (!orgResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Organizations API Error batch {batchNumber}: {orgResponse.StatusCode}");
                    return organizations; // Restituisce lista vuota
                }

                var orgJson = await orgResponse.Content.ReadAsStringAsync();
                var orgRoot = JObject.Parse(orgJson);
                var orgValues = orgRoot["values"] as JArray;

                if (orgValues != null)
                {
                    foreach (var org in orgValues)
                    {
                        var orgName = org["name"]?.ToString()?.Trim();
                        if (!string.IsNullOrWhiteSpace(orgName))
                        {
                            organizations.Add(orgName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore batch {batchNumber} organizzazioni", ex);
            }

            return organizations;
        }

        #endregion

        #region User Picker Fields Loading

        /// <summary>
        /// Carica valori per campi user picker (Consulente, PM, Commerciale)
        /// </summary>
        private async Task<List<string>> LoadUserPickerFieldValues(JiraFieldType fieldType, IProgress<string> progress, string ticketKey)
        {
            var customFieldId = JiraFieldTypeHelper.GetCustomFieldId(fieldType);
            _logger.LogInfo($"👤 Caricamento {fieldType} tramite EditMeta (campo: {customFieldId})");
            progress?.Report($"Caricamento {JiraFieldTypeHelper.GetDisplayName(fieldType)}...");

            using var httpClient = CreateHttpClient();

            var fallbackTickets = GetFallbackTickets(ticketKey);

            foreach (var testTicketKey in fallbackTickets)
            {
                var values = await TryLoadUserPickerFromTicket(httpClient, fieldType, customFieldId, testTicketKey);
                if (values.Count > 0)
                {
                    _logger.LogInfo($"✅ Successo con ticket {testTicketKey}: {values.Count} valori per {fieldType}");
                    return values.OrderBy(c => c).ToList();
                }
            }

            // Fallback finale a JQL
            _logger.LogInfo($"🔄 Fallback a strategia JQL per {fieldType}");
            try
            {
                return await LoadFromJQLSearch(fieldType, progress);
            }
            catch (Exception jqlEx)
            {
                _logger.LogError($"❌ Anche la strategia JQL è fallita per {fieldType}: {jqlEx.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Genera lista di ticket fallback per l'EditMeta
        /// </summary>
        private string[] GetFallbackTickets(string ticketKey)
        {
            return new[] { ticketKey, "CC-1", "CC-2", "CC-10", "CC-100", "CC-1000" }
                .Where(tk => !string.IsNullOrEmpty(tk))
                .Distinct()
                .ToArray();
        }

        /// <summary>
        /// Tenta di caricare valori user picker da un ticket specifico
        /// </summary>
        private async Task<List<string>> TryLoadUserPickerFromTicket(HttpClient httpClient, JiraFieldType fieldType, string customFieldId, string testTicketKey)
        {
            try
            {
                _logger.LogInfo($"🎯 Tentativo con ticket: {testTicketKey}");

                var editMetaUrl = $"{Domain}/rest/api/2/issue/{testTicketKey}/editmeta";
                using var editMetaRequest = new HttpRequestMessage(HttpMethod.Get, editMetaUrl);
                editMetaRequest.Headers.Add("Accept", "application/json");

                using var editMetaResponse = await httpClient.SendAsync(editMetaRequest);

                if (!editMetaResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"⚠️ EditMeta fallito per {testTicketKey}: {editMetaResponse.StatusCode}");
                    return new List<string>();
                }

                var editMetaJson = await editMetaResponse.Content.ReadAsStringAsync();
                return ExtractUserPickerValues(editMetaJson, customFieldId, testTicketKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"⚠️ Errore con ticket {testTicketKey}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Estrae i valori user picker dal JSON EditMeta
        /// </summary>
        private List<string> ExtractUserPickerValues(string editMetaJson, string customFieldId, string testTicketKey)
        {
            var userPickerValues = new List<string>();
            var editMetaObj = JObject.Parse(editMetaJson);

            var fields = editMetaObj["fields"];
            if (fields != null && fields[customFieldId] != null)
            {
                var allowedValues = fields[customFieldId]["allowedValues"];
                if (allowedValues != null && allowedValues.Any())
                {
                    foreach (var allowedValue in allowedValues)
                    {
                        var value = allowedValue["value"]?.ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            var displayValue = EmailConverterHelper.FormatUsernameForDisplay(value);
                            userPickerValues.Add(displayValue);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"⚠️ Campo {customFieldId} trovato ma senza allowedValues in {testTicketKey}");
                }
            }
            else
            {
                _logger.LogWarning($"⚠️ Campo {customFieldId} non trovato in {testTicketKey}");
            }

            return userPickerValues;
        }

        #endregion

        #region Custom Fields Loading

        /// <summary>
        /// Carica valori per custom fields tramite CreateMeta API
        /// </summary>
        private async Task<List<string>> LoadCustomFieldValues(JiraFieldType fieldType, IProgress<string> progress)
        {
            var customFieldId = JiraFieldTypeHelper.GetCustomFieldId(fieldType);
            var createMetaUrl = $"{Domain}/rest/api/2/issue/createmeta?projectKeys=CC&expand=projects.issuetypes.fields.{customFieldId}.allowedValues";

            _logger.LogInfo($"🔧 Custom field CreateMeta: {createMetaUrl}");
            progress?.Report($"Caricamento {JiraFieldTypeHelper.GetDisplayName(fieldType)} via CreateMeta...");

            using var httpClient = CreateHttpClient();
            using var createMetaRequest = new HttpRequestMessage(HttpMethod.Get, createMetaUrl);
            createMetaRequest.Headers.Add("Accept", "application/json");

            using var createMetaResponse = await httpClient.SendAsync(createMetaRequest);

            if (!createMetaResponse.IsSuccessStatusCode)
            {
                var errorContent = await createMetaResponse.Content.ReadAsStringAsync();
                throw new HttpRequestException($"CreateMeta Error: {createMetaResponse.StatusCode} - {errorContent}");
            }

            var createMetaJson = await createMetaResponse.Content.ReadAsStringAsync();
            var createMeta = JObject.Parse(createMetaJson);
            var customFieldValues = ExtractAllowedValuesFromCreateMeta(createMeta, customFieldId);

            _logger.LogInfo($"🔍 Custom field {customFieldId} valori: {string.Join(", ", customFieldValues)}");
            return customFieldValues;
        }

        #endregion

        #region Standard Fields Loading

        /// <summary>
        /// Carica valori per campi standard (Stati, Priorità, Tipi, Assegnatari)
        /// </summary>
        private async Task<List<string>> LoadStandardFieldValues(JiraFieldType fieldType, IProgress<string> progress)
        {
            var endpoint = JiraFieldTypeHelper.GetApiEndpoint(fieldType);
            var url = $"{Domain}{endpoint}";

            _logger.LogInfo($"🔗 API diretta: {url}");
            progress?.Report($"Chiamata API {JiraFieldTypeHelper.GetDisplayName(fieldType)}...");

            using var httpClient = CreateHttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "application/json");

            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API Error: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var jsonArray = JArray.Parse(jsonContent);

            return ProcessStandardFieldResponse(fieldType, jsonArray);
        }

        /// <summary>
        /// Processa la risposta JSON per campi standard
        /// </summary>
        private List<string> ProcessStandardFieldResponse(JiraFieldType fieldType, JArray jsonArray)
        {
            List<string> finalFieldValues = fieldType switch
            {
                JiraFieldType.Status => ExtractStatusCategories(jsonArray),
                JiraFieldType.Assignee => ExtractAndFilterAssignees(jsonArray),
                _ => ExtractGenericFieldNames(jsonArray)
            };

            // Applica filtri specifici
            finalFieldValues = ApplyFieldSpecificFilters(fieldType, finalFieldValues);

            return finalFieldValues;
        }

        /// <summary>
        /// Estrae le categorie di stato
        /// </summary>
        private List<string> ExtractStatusCategories(JArray jsonArray)
        {
            var statusCategoryNames = jsonArray
                .Where(item => item["statusCategory"]?["name"] != null)
                .Select(item => item["statusCategory"]["name"].ToString().Trim())
                .Where(categoryName => !string.IsNullOrEmpty(categoryName))
                .Distinct()
                .OrderBy(categoryName => categoryName)
                .ToList();

            _logger.LogInfo($"🔍 StatusCategory trovate: {string.Join(", ", statusCategoryNames)}");
            return statusCategoryNames;
        }

        /// <summary>
        /// Estrae e filtra gli assegnatari
        /// </summary>
        private List<string> ExtractAndFilterAssignees(JArray jsonArray)
        {
            var assigneeNames = jsonArray
                .Where(item => item["displayName"] != null)
                .Select(item => item["displayName"].ToString().Trim())
                .Where(displayName => !string.IsNullOrEmpty(displayName))
                .Distinct()
                .OrderBy(displayName => displayName)
                .ToList();

            var excludedAssignees = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Dania Zaccara", "Daniela La Duca", "Edoardo Lembo",
        "Francisca Gori", "Giovanni Piccolo", "Valerio Vocale", "Francesco Dembech"
    };

            var filteredAssignees = assigneeNames.Where(assignee => !excludedAssignees.Contains(assignee)).ToList();
            _logger.LogInfo($"🔍 Assegnatari filtrati: {string.Join(", ", filteredAssignees)}");

            return filteredAssignees;
        }

        /// <summary>
        /// Estrae nomi generici dai campi
        /// </summary>
        private List<string> ExtractGenericFieldNames(JArray jsonArray)
        {
            return jsonArray
                .Where(item => item["name"] != null)
                .Select(item => item["name"].ToString().Trim())
                .Where(fieldName => !string.IsNullOrEmpty(fieldName))
                .Distinct()
                .OrderBy(fieldName => fieldName)
                .ToList();
        }

        /// <summary>
        /// Applica filtri specifici per tipo di campo
        /// </summary>
        private List<string> ApplyFieldSpecificFilters(JiraFieldType fieldType, List<string> values)
        {
            return fieldType switch
            {
                JiraFieldType.Priority => FilterPriorities(values),
                JiraFieldType.IssueType => FilterIssueTypes(values),
                _ => values
            };
        }

        /// <summary>
        /// Filtra le priorità consentite
        /// </summary>
        private List<string> FilterPriorities(List<string> priorities)
        {
            var allowedPriorities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Alta", "Media", "Bassa"
    };

            var filtered = priorities.Where(priority => allowedPriorities.Contains(priority)).ToList();
            _logger.LogInfo($"🔍 Priorità filtrate: {string.Join(", ", filtered)}");
            return filtered;
        }

        /// <summary>
        /// Filtra e pulisce i tipi di issue
        /// </summary>
        private List<string> FilterIssueTypes(List<string> issueTypes)
        {
            var cleanedIssueTypes = issueTypes.Select(issueType => CleanIssueTypeName(issueType)).ToList();
            var filteredIssueTypes = cleanedIssueTypes
                .Where(issueType => !issueType.Equals("Sottotask", StringComparison.OrdinalIgnoreCase))
                .ToList();

            _logger.LogInfo($"🔍 Tipi puliti: {string.Join(", ", filteredIssueTypes)}");
            return filteredIssueTypes;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Crea un HttpClient configurato con credenziali
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_jiraApiService.Username}:{_jiraApiService.Token}"));

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }

        

        #endregion


    }
}