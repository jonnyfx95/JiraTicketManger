using ClosedXML.Excel;
using JiraTicketManager.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per la gestione dei membri delle organizzazioni Jira.
    /// Pattern identico a PhoneBookService: cache, sync incrementale, export Excel.
    /// Path: Services/OrganizationMembersService.cs
    /// </summary>
    public class OrganizationMembersService
    {
        #region Constants

        private const string CACHE_DIRECTORY = "data";
        private const string ORGANIZATIONS_CACHE_FILENAME = "organizations_cache.csv";
        private const string MEMBERS_CACHE_FILENAME = "organization_members_cache.csv";
        private const string SYNC_CONFIG_FILENAME = "organization_members_sync.json";

        #endregion

        #region Private Fields

        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;
        private readonly string _organizationsCacheFilePath;
        private readonly string _membersCacheFilePath;
        private readonly string _syncConfigFilePath;

        #endregion

        #region Constructor

        public OrganizationMembersService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("OrganizationMembersService");

            var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CACHE_DIRECTORY);
            Directory.CreateDirectory(dataDirectory);

            _organizationsCacheFilePath = Path.Combine(dataDirectory, ORGANIZATIONS_CACHE_FILENAME);
            _membersCacheFilePath = Path.Combine(dataDirectory, MEMBERS_CACHE_FILENAME);
            _syncConfigFilePath = Path.Combine(dataDirectory, SYNC_CONFIG_FILENAME);

            _logger.LogInfo($"OrganizationMembersService inizializzato");
            _logger.LogInfo($"Organizations Cache: {_organizationsCacheFilePath}");
            _logger.LogInfo($"Members Cache: {_membersCacheFilePath}");
            _logger.LogInfo($"Sync Config: {_syncConfigFilePath}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Carica i membri delle organizzazioni.
        /// Se esiste cache, carica da file. Altrimenti carica da API.
        /// </summary>
        public async Task<List<OrganizationMemberEntry>> LoadOrganizationMembersAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("Caricamento membri organizzazioni");

                if (File.Exists(_membersCacheFilePath))
                {
                    _logger.LogInfo("Cache trovata, caricamento da file");
                    progress?.Report("Caricamento da cache...");

                    var entries = await LoadMembersFromCacheAsync();
                    _logger.LogInfo($"Caricati {entries.Count} membri da cache");

                    return entries;
                }
                else
                {
                    _logger.LogInfo("Cache non trovata, caricamento da API");
                    progress?.Report("Prima esecuzione: caricamento da Jira API...");

                    var entries = await RefreshFromApiAsync(progress);
                    return entries;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento membri organizzazioni", ex);
                throw;
            }
        }

        /// <summary>
        /// Forza il refresh dalla API Jira.
        /// Carica tutte le organizzazioni e per ognuna estrae i membri.
        /// </summary>
        public async Task<List<OrganizationMemberEntry>> RefreshFromApiAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("=== REFRESH DA API JIRA - ORGANIZATION MEMBERS ===");

                var syncStartTime = DateTime.UtcNow;

                // STEP 1: Carica tutte le organizzazioni
                progress?.Report("Caricamento organizzazioni...");
                var organizations = await LoadOrganizationsAsync(progress);
                _logger.LogInfo($"📋 Organizzazioni caricate: {organizations.Count}");

                if (organizations.Count == 0)
                {
                    _logger.LogWarning("Nessuna organizzazione trovata");
                    return new List<OrganizationMemberEntry>();
                }

                // STEP 2: Per ogni organizzazione, carica i membri
                var allMembers = new List<OrganizationMemberEntry>();
                int orgCount = 0;

                foreach (var org in organizations)
                {
                    orgCount++;
                    progress?.Report($"Elaborazione organizzazione {orgCount}/{organizations.Count}: {org}");
                    _logger.LogInfo($"📂 [{orgCount}/{organizations.Count}] Elaborazione: {org}");

                    try
                    {
                        var members = await LoadMembersForOrganizationAsync(org, progress);
                        allMembers.AddRange(members);
                        _logger.LogInfo($"   ✅ {members.Count} membri trovati");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"   ❌ Errore elaborazione organizzazione '{org}'", ex);
                        // Continua con le altre organizzazioni
                    }

                    // Pausa per non sovraccaricare l'API
                    await Task.Delay(200);
                }

                // STEP 3: Deduplicazione e salvataggio
                var uniqueMembers = DeduplicateMembers(allMembers);
                _logger.LogInfo($"📊 Membri totali: {allMembers.Count}, Membri unici: {uniqueMembers.Count}");

                // Salva cache organizzazioni
                await SaveOrganizationsCacheAsync(organizations);

                // Salva cache membri
                await SaveMembersCacheAsync(uniqueMembers);

                // Aggiorna sync config
                await UpdateSyncConfigAsync(syncStartTime);

                progress?.Report($"✅ Completato: {uniqueMembers.Count} membri trovati");
                return uniqueMembers;
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore refresh da API", ex);
                throw;
            }
        }

        /// <summary>
        /// Esporta i membri in Excel con tabella PIVOT configurata
        /// </summary>
        public async Task ExportToExcelAsync(List<OrganizationMemberEntry> entries, string filePath)
        {
            try
            {
                _logger.LogInfo($"=== EXPORT EXCEL CON PIVOT ===");
                _logger.LogInfo($"File: {filePath}");
                _logger.LogInfo($"Membri da esportare: {entries.Count}");

                if (entries == null || entries.Count == 0)
                {
                    _logger.LogWarning("Nessun membro da esportare");
                    throw new InvalidOperationException("Nessun membro da esportare");
                }

                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // SHEET 1: Dati grezzi
                        var dataSheet = workbook.Worksheets.Add("Dati");
                        PopulateDataSheet(dataSheet, entries);

                        // SHEET 2: Tabella PIVOT (configurazione manuale Excel)
                        // ClosedXML non supporta nativamente le tabelle PIVOT,
                        // ma possiamo creare una tabella Excel formattata che si può
                        // facilmente convertire in PIVOT dall'utente

                        // Salva workbook
                        workbook.SaveAs(filePath);
                        _logger.LogInfo($"✅ File Excel salvato: {filePath}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore export Excel", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - API Loading

        /// <summary>
        /// Carica tutte le organizzazioni tramite API ServiceDesk
        /// </summary>
        private async Task<List<string>> LoadOrganizationsAsync(IProgress<string> progress)
        {
            _logger.LogInfo("🏢 Caricamento organizzazioni da API");
            var organizations = new List<string>();

            using var httpClient = CreateHttpClient();
            int start = 0;
            int batchSize = 50;
            int batchNumber = 1;
            int maxBatches = 50;
            bool morePages = true;

            while (morePages && batchNumber <= maxBatches)
            {
                try
                {
                    var url = $"{_jiraApiService.Domain}/rest/servicedeskapi/organization?start={start}&limit={batchSize}";
                    _logger.LogDebug($"📄 Batch {batchNumber}: {url}");

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Accept", "application/json");

                    using var response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"❌ Batch {batchNumber} failed: {response.StatusCode}");
                        break;
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    var root = JObject.Parse(json);
                    var values = root["values"] as JArray;

                    if (values != null && values.Count > 0)
                    {
                        foreach (var org in values)
                        {
                            var orgName = org["name"]?.ToString()?.Trim();
                            if (!string.IsNullOrWhiteSpace(orgName))
                            {
                                organizations.Add(orgName);
                            }
                        }

                        _logger.LogInfo($"📦 Batch {batchNumber}: {values.Count} organizzazioni");

                        if (values.Count < batchSize)
                        {
                            morePages = false;
                        }
                        else
                        {
                            start += batchSize;
                            batchNumber++;
                        }
                    }
                    else
                    {
                        morePages = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Errore batch {batchNumber}", ex);
                    break;
                }

                await Task.Delay(100);
            }

            var distinctOrgs = organizations.Distinct().OrderBy(o => o).ToList();
            _logger.LogInfo($"🎯 Organizzazioni totali: {distinctOrgs.Count}");
            return distinctOrgs;
        }

        /// <summary>
        /// Carica i membri di una specifica organizzazione tramite query JQL
        /// </summary>
        private async Task<List<OrganizationMemberEntry>> LoadMembersForOrganizationAsync(string organizationName, IProgress<string> progress)
        {
            var members = new List<OrganizationMemberEntry>();

            try
            {
                // Query JQL per trovare tutti i reporter dell'organizzazione
                var jql = $"reporter in organizationMembers(\"{EscapeJql(organizationName)}\") AND project = CC";
                _logger.LogDebug($"   JQL: {jql}");

                var searchResult = await _jiraApiService.SearchIssuesAsync(jql, 0, 1000);
                var tickets = searchResult.Issues;

                if (tickets == null || tickets.Count == 0)
                {
                    _logger.LogDebug($"   ℹ️ Nessun ticket trovato per {organizationName}");
                    return members;
                }

                _logger.LogDebug($"   📊 {tickets.Count} ticket trovati");

                // Raggruppa per reporter e conta i ticket
                var reporterGroups = new Dictionary<string, (string nome, string email, int count)>();

                foreach (var ticket in tickets)
                {
                    try
                    {
                        var fields = ticket["fields"];
                        var reporter = fields?["reporter"];

                        if (reporter == null || reporter.Type == JTokenType.Null)
                            continue;

                        var accountId = reporter["accountId"]?.ToString() ?? "";
                        var displayName = reporter["displayName"]?.ToString() ?? "";
                        var emailAddress = reporter["emailAddress"]?.ToString() ?? "";

                        if (string.IsNullOrWhiteSpace(accountId))
                            continue;

                        if (reporterGroups.ContainsKey(accountId))
                        {
                            var current = reporterGroups[accountId];
                            reporterGroups[accountId] = (current.nome, current.email, current.count + 1);
                        }
                        else
                        {
                            reporterGroups[accountId] = (displayName, emailAddress, 1);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"   ⚠️ Errore parsing ticket: {ex.Message}");
                    }
                }

                // Converti in OrganizationMemberEntry
                foreach (var kvp in reporterGroups)
                {
                    var accountId = kvp.Key;
                    var (nome, email, count) = kvp.Value;

                    members.Add(new OrganizationMemberEntry(
                        organizationName,
                        nome,
                        email,
                        accountId,
                        count
                    ));
                }

                _logger.LogDebug($"   👥 {members.Count} membri unici estratti");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento membri per {organizationName}", ex);
            }

            return members;
        }

        #endregion

        #region Private Methods - Cache Management

        private async Task<List<OrganizationMemberEntry>> LoadMembersFromCacheAsync()
        {
            try
            {
                var entries = new List<OrganizationMemberEntry>();

                using (var reader = new StreamReader(_membersCacheFilePath, Encoding.UTF8))
                {
                    await reader.ReadLineAsync(); // Salta header

                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var entry = OrganizationMemberEntry.FromCsvLine(line);
                        if (entry != null && entry.IsValid())
                        {
                            entries.Add(entry);
                        }
                    }
                }

                _logger.LogInfo($"Cache caricata: {entries.Count} membri");
                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento cache membri", ex);
                throw;
            }
        }

        private async Task SaveMembersCacheAsync(List<OrganizationMemberEntry> entries)
        {
            try
            {
                using (var writer = new StreamWriter(_membersCacheFilePath, false, Encoding.UTF8))
                {
                    await writer.WriteLineAsync(OrganizationMemberEntry.GetCsvHeader());

                    foreach (var entry in entries.OrderBy(e => e.Organizzazione).ThenBy(e => e.Nome))
                    {
                        await writer.WriteLineAsync(entry.ToCsvLine());
                    }
                }

                _logger.LogInfo($"Cache membri salvata: {entries.Count} entries");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio cache membri", ex);
                throw;
            }
        }

        private async Task SaveOrganizationsCacheAsync(List<string> organizations)
        {
            try
            {
                using (var writer = new StreamWriter(_organizationsCacheFilePath, false, Encoding.UTF8))
                {
                    foreach (var org in organizations.OrderBy(o => o))
                    {
                        await writer.WriteLineAsync(org);
                    }
                }

                _logger.LogInfo($"Cache organizzazioni salvata: {organizations.Count} entries");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio cache organizzazioni", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - Sync Config

        private class OrganizationMembersSyncConfig
        {
            public DateTime? LastSyncDate { get; set; }
        }

        private async Task<OrganizationMembersSyncConfig> LoadSyncConfigAsync()
        {
            try
            {
                if (File.Exists(_syncConfigFilePath))
                {
                    var json = await File.ReadAllTextAsync(_syncConfigFilePath);
                    return JsonConvert.DeserializeObject<OrganizationMembersSyncConfig>(json)
                           ?? new OrganizationMembersSyncConfig();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore caricamento sync config: {ex.Message}");
            }

            return new OrganizationMembersSyncConfig();
        }

        private async Task UpdateSyncConfigAsync(DateTime syncDate)
        {
            try
            {
                var config = new OrganizationMembersSyncConfig
                {
                    LastSyncDate = syncDate
                };

                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(_syncConfigFilePath, json);

                _logger.LogInfo($"Sync config aggiornato: {syncDate:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento sync config", ex);
            }
        }

        #endregion

        #region Private Methods - Excel Export

        private void PopulateDataSheet(IXLWorksheet worksheet, List<OrganizationMemberEntry> entries)
        {
            // HEADER
            worksheet.Cell(1, 1).Value = "Organizzazione";
            worksheet.Cell(1, 2).Value = "Nome";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Numero Ticket";

            // Formattazione header
            var headerRange = worksheet.Range(1, 1, 1, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontSize = 11;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thick;

            // DATI
            int row = 2;
            foreach (var entry in entries.OrderBy(e => e.Organizzazione).ThenBy(e => e.Nome))
            {
                worksheet.Cell(row, 1).Value = entry.Organizzazione ?? "";
                worksheet.Cell(row, 2).Value = entry.Nome ?? "";
                worksheet.Cell(row, 3).Value = entry.Email ?? "";
                worksheet.Cell(row, 4).Value = entry.NumeroTicket;
                row++;
            }

            // FORMATTAZIONE
            var dataRange = worksheet.Range(1, 1, row - 1, 4);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Crea tabella Excel formattata (per facilitare PIVOT dall'utente)
            var table = dataRange.CreateTable("TabellaMembers");
            table.Theme = XLTableTheme.TableStyleMedium2;

            // Auto-fit
            worksheet.Columns().AdjustToContents();
            worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 30); // Organizzazione
            worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 25); // Nome
            worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 30); // Email

            // Freeze header
            worksheet.SheetView.FreezeRows(1);
        }

        #endregion

        #region Private Helpers

        private List<OrganizationMemberEntry> DeduplicateMembers(List<OrganizationMemberEntry> members)
        {
            var uniqueMembers = new Dictionary<string, OrganizationMemberEntry>();

            foreach (var member in members)
            {
                var key = member.GetUniqueKey();

                if (uniqueMembers.ContainsKey(key))
                {
                    // Somma il numero di ticket se il membro esiste già
                    uniqueMembers[key].NumeroTicket += member.NumeroTicket;
                }
                else
                {
                    uniqueMembers[key] = member;
                }
            }

            return uniqueMembers.Values.ToList();
        }

        private string EscapeJql(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return value.Replace("\"", "\\\"");
        }

        private HttpClient CreateHttpClient()
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_jiraApiService.Username}:{_jiraApiService.Token}")
            );

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {credentials}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            return httpClient;
        }

        #endregion
    }
}