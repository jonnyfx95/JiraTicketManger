using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Data.Models;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per la gestione della rubrica telefonica Jira.
    /// Gestisce caricamento da API, cache CSV e export Excel.
    /// Path: Services/PhoneBookService.cs
    /// 
    /// AGGIORNAMENTI v2:
    /// - Query JQL filtra telefono/cliente nulli direttamente
    /// - Mapping applicativo inline (estrae solo parte dopo "->")
    /// - Rimuove "***" dal campo Cliente
    /// - Logging dettagliato per debug mapping
    /// </summary>
    public class PhoneBookService
    {
        #region Constants

        private const string CACHE_DIRECTORY = "data";
        private const string CACHE_FILENAME = "phonebook_cache.csv";
        private const int API_BATCH_SIZE = 100;

        // ✅ QUERY AGGIORNATA: Filtra telefono/cliente nulli + esclude DEDANEXT
        private const string JQL_QUERY = "project = CC and \"Telefono[Short text]\" IS NOT EMPTY and \"Cliente[Dropdown]\" is not EMPTY and \"Cliente[Dropdown]\" != DEDANEXT ORDER BY updated DESC";

        #endregion

        #region Private Fields

        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;
        private readonly string _cacheFilePath;

        #endregion

        #region Constructor

        public PhoneBookService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("PhoneBookService");

            // Percorso cache nella cartella data/
            _cacheFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CACHE_DIRECTORY, CACHE_FILENAME);

            _logger.LogInfo($"PhoneBookService inizializzato. Cache: {_cacheFilePath}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Carica la rubrica telefonica.
        /// Se esiste cache, carica da file. Altrimenti carica da API.
        /// </summary>
        public async Task<List<PhoneBookEntry>> LoadPhoneBookAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("Caricamento rubrica telefonica");

                // Controlla se esiste cache
                if (File.Exists(_cacheFilePath))
                {
                    _logger.LogInfo("Cache trovata, caricamento da file");
                    progress?.Report("Caricamento da cache...");

                    var entries = await LoadFromCacheAsync();
                    _logger.LogInfo($"Caricati {entries.Count} contatti da cache");

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
                _logger.LogError("Errore caricamento rubrica", ex);
                throw;
            }
        }

        /// <summary>
        /// Forza il refresh dalla API Jira.
        /// Esegue query completa con paginazione e rigenera cache.
        /// </summary>
        public async Task<List<PhoneBookEntry>> RefreshFromApiAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("=== REFRESH DA API JIRA ===");
                _logger.LogInfo($"Query JQL: {JQL_QUERY}");
                progress?.Report("Connessione a Jira API...");

                var allEntries = new List<PhoneBookEntry>();
                var startAt = 0;
                var totalTickets = 0;
                var processedTickets = 0;

                // Loop di paginazione
                while (true)
                {
                    progress?.Report($"Caricamento batch {startAt / API_BATCH_SIZE + 1}...");

                    var searchResult = await _jiraApiService.SearchIssuesAsync(
                        JQL_QUERY,
                        startAt,
                        API_BATCH_SIZE
                    );

                    totalTickets = searchResult.Total;
                    var batchEntries = ExtractEntriesFromTickets(searchResult.Issues);
                    allEntries.AddRange(batchEntries);

                    processedTickets += searchResult.Issues.Count;
                    _logger.LogInfo($"Batch processato: {processedTickets}/{totalTickets} ticket");

                    progress?.Report($"Processati {processedTickets}/{totalTickets} ticket...");

                    // Se abbiamo caricato tutti i ticket, esci
                    if (processedTickets >= totalTickets)
                        break;

                    startAt += API_BATCH_SIZE;
                }

                _logger.LogInfo($"Totale entries estratte: {allEntries.Count}");
                progress?.Report("Rimozione duplicati...");

                // Rimuovi duplicati
                var uniqueEntries = RemoveDuplicates(allEntries);
                _logger.LogInfo($"Entries uniche dopo deduplicazione: {uniqueEntries.Count}");

                // Salva cache
                progress?.Report("Salvataggio cache...");
                await SaveToCacheAsync(uniqueEntries);

                _logger.LogInfo("=== REFRESH COMPLETATO ===");
                return uniqueEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore refresh da API", ex);
                throw;
            }
        }

        /// <summary>
        /// Esporta la rubrica in Excel con raggruppamento per Cliente.
        /// </summary>
        public async Task ExportToExcelAsync(List<PhoneBookEntry> entries, string filePath)
        {
            try
            {
                _logger.LogInfo($"Export Excel: {filePath}");

                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // Raggruppa per Cliente
                        var groupedByCliente = entries
                            .Where(e => !string.IsNullOrWhiteSpace(e.Cliente))
                            .GroupBy(e => e.Cliente)
                            .OrderBy(g => g.Key);

                        foreach (var clienteGroup in groupedByCliente)
                        {
                            var sheetName = SanitizeSheetName(clienteGroup.Key);
                            var worksheet = workbook.Worksheets.Add(sheetName);

                            // Header
                            worksheet.Cell(1, 1).Value = "Nome";
                            worksheet.Cell(1, 2).Value = "Email";
                            worksheet.Cell(1, 3).Value = "Telefono";
                            worksheet.Cell(1, 4).Value = "Applicativo";
                            worksheet.Cell(1, 5).Value = "Area";

                            // Formattazione header
                            var headerRange = worksheet.Range(1, 1, 1, 5);
                            headerRange.Style.Font.Bold = true;
                            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thick;

                            // Dati
                            int row = 2;
                            foreach (var entry in clienteGroup.OrderBy(e => e.Nome))
                            {
                                worksheet.Cell(row, 1).Value = entry.Nome;
                                worksheet.Cell(row, 2).Value = entry.Email;
                                worksheet.Cell(row, 3).Value = entry.Telefono;
                                worksheet.Cell(row, 4).Value = entry.Applicativo;
                                worksheet.Cell(row, 5).Value = entry.Area;
                                row++;
                            }

                            // Auto-fit colonne
                            worksheet.Columns().AdjustToContents();
                        }

                        // Se non ci sono entries con Cliente, crea foglio unico
                        if (workbook.Worksheets.Count == 0)
                        {
                            var worksheet = workbook.Worksheets.Add("Tutti i Contatti");
                            PopulateWorksheet(worksheet, entries);
                        }

                        workbook.SaveAs(filePath);
                    }
                });

                _logger.LogInfo($"Export Excel completato: {entries.Count} entries");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore export Excel", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - API Extraction

        /// <summary>
        /// Estrae PhoneBookEntry da una lista di ticket Jira.
        /// NOTA: La query JQL già filtra telefono/cliente nulli, quindi non serve ricontrollare.
        /// </summary>
        private List<PhoneBookEntry> ExtractEntriesFromTickets(JArray tickets)
        {
            var entries = new List<PhoneBookEntry>();
            int mappingCount = 0;

            foreach (var ticket in tickets)
            {
                try
                {
                    var fields = ticket["fields"];
                    if (fields == null)
                        continue;

                    // Estrai campi raw
                    var clienteRaw = JiraDataConverter.GetCustomFieldValue(fields, "customfield_10117");
                    var applicativoRaw = JiraDataConverter.GetCustomFieldValue(fields, "customfield_10114");
                    var area = JiraDataConverter.GetCustomFieldValue(fields, "customfield_10113");
                    var telefono = JiraDataConverter.GetCustomFieldValue(fields, "customfield_10074");

                    // Reporter fields
                    var reporter = fields["reporter"];
                    var nome = reporter?["displayName"]?.ToString() ?? "";
                    var email = reporter?["emailAddress"]?.ToString() ?? "";

                    // ✅ Applica trasformazioni

                    // 1. Rimuovi "***" dal Cliente
                    var cliente = CleanCliente(clienteRaw);

                    // 2. Applica mapping Applicativo INLINE (estrae solo parte dopo "->")
                    var applicativo = ApplyApplicativoMappingInline(applicativoRaw);

                    // Log mapping se cambia
                    if (applicativo != applicativoRaw)
                    {
                        _logger.LogDebug($"📋 Mapping: '{applicativoRaw}' → '{applicativo}'");
                        mappingCount++;
                    }

                    // ✅ Crea entry
                    var entry = new PhoneBookEntry(
                        cliente ?? "",
                        applicativo ?? "",
                        area ?? "",
                        nome,
                        email,
                        telefono ?? ""
                    );

                    // Aggiungi solo se valida (ha nome o email)
                    if (entry.IsValid())
                    {
                        entries.Add(entry);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Errore estrazione entry da ticket: {ex.Message}");
                    // Continua con il prossimo ticket
                }
            }

            if (mappingCount > 0)
            {
                _logger.LogInfo($"✅ Applicati {mappingCount} mapping applicativi");
            }

            return entries;
        }

        /// <summary>
        /// Pulisce il campo Cliente rimuovendo "***"
        /// </summary>
        private string CleanCliente(string cliente)
        {
            if (string.IsNullOrWhiteSpace(cliente))
                return cliente;

            // Rimuovi "***" ovunque nel testo
            var cleaned = cliente.Replace("***", "").Trim();

            if (cleaned != cliente)
            {
                _logger.LogDebug($"🧹 Cliente pulito: '{cliente}' → '{cleaned}'");
            }

            return cleaned;
        }

        /// <summary>
        /// Applica mapping Applicativo INLINE - Estrae solo la parte dopo " -> "
        /// Questo è il mapping più semplice e diretto, identico a cmbApplicativo.
        /// 
        /// ESEMPI:
        /// "Civilia Next - Area Demografia -> Anagrafe" → "Anagrafe"
        /// "Civilia - GeoNext -> API PDND" → "API PDND"
        /// "Sistema Informativo Territoriale -> CDU" → "CDU"
        /// "Metadatamanager -> MDMGR" → "MDMGR"
        /// "Civilia Next -> Muse" → "Muse"
        /// </summary>
        private string ApplyApplicativoMappingInline(string applicativoRaw)
        {
            if (string.IsNullOrWhiteSpace(applicativoRaw))
                return applicativoRaw;

            try
            {
                // Se contiene " -> ", prendi solo la parte DOPO
                if (applicativoRaw.Contains(" -> "))
                {
                    var parts = applicativoRaw.Split(new[] { " -> " }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length >= 2)
                    {
                        // Prendi l'ULTIMA parte (dopo l'ultimo "->")
                        var mapped = parts[parts.Length - 1].Trim();
                        return mapped;
                    }
                }

                // Se non contiene "->", ritorna invariato
                return applicativoRaw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore mapping applicativo '{applicativoRaw}': {ex.Message}");
                return applicativoRaw; // Fallback: ritorna valore originale
            }
        }

        /// <summary>
        /// Rimuove entries duplicate usando GetUniqueKey()
        /// </summary>
        private List<PhoneBookEntry> RemoveDuplicates(List<PhoneBookEntry> entries)
        {
            var uniqueEntries = entries
                .GroupBy(e => e.GetUniqueKey())
                .Select(g => g.First())
                .OrderBy(e => e.Cliente)
                .ThenBy(e => e.Nome)
                .ToList();

            var duplicatesRemoved = entries.Count - uniqueEntries.Count;
            if (duplicatesRemoved > 0)
            {
                _logger.LogInfo($"Rimossi {duplicatesRemoved} duplicati");
            }

            return uniqueEntries;
        }

        #endregion

        #region Private Methods - Cache Management

        /// <summary>
        /// Salva entries in cache CSV
        /// </summary>
        private async Task SaveToCacheAsync(List<PhoneBookEntry> entries)
        {
            try
            {
                // Assicura che la directory esista
                var directory = Path.GetDirectoryName(_cacheFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInfo($"Creata directory cache: {directory}");
                }

                // Scrivi file CSV
                using (var writer = new StreamWriter(_cacheFilePath, false, Encoding.UTF8))
                {
                    // Header
                    await writer.WriteLineAsync(PhoneBookEntry.GetCsvHeader());

                    // Dati
                    foreach (var entry in entries)
                    {
                        await writer.WriteLineAsync(entry.ToCsvLine());
                    }
                }

                _logger.LogInfo($"Cache salvata: {entries.Count} entries in {_cacheFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio cache", ex);
                throw;
            }
        }

        /// <summary>
        /// Carica entries da cache CSV
        /// </summary>
        private async Task<List<PhoneBookEntry>> LoadFromCacheAsync()
        {
            try
            {
                var entries = new List<PhoneBookEntry>();

                using (var reader = new StreamReader(_cacheFilePath, Encoding.UTF8))
                {
                    // Salta header
                    await reader.ReadLineAsync();

                    // Leggi dati
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var entry = PhoneBookEntry.FromCsvLine(line);
                        if (entry != null && entry.IsValid())
                        {
                            entries.Add(entry);
                        }
                    }
                }

                _logger.LogInfo($"Cache caricata: {entries.Count} entries");
                return entries;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento cache", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - Excel Helpers

        /// <summary>
        /// Popola un worksheet Excel con entries
        /// </summary>
        private void PopulateWorksheet(IXLWorksheet worksheet, List<PhoneBookEntry> entries)
        {
            // Header
            worksheet.Cell(1, 1).Value = "Cliente";
            worksheet.Cell(1, 2).Value = "Applicativo";
            worksheet.Cell(1, 3).Value = "Area";
            worksheet.Cell(1, 4).Value = "Nome";
            worksheet.Cell(1, 5).Value = "Email";
            worksheet.Cell(1, 6).Value = "Telefono";

            // Formattazione header
            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thick;

            // Dati
            int row = 2;
            foreach (var entry in entries.OrderBy(e => e.Cliente).ThenBy(e => e.Nome))
            {
                worksheet.Cell(row, 1).Value = entry.Cliente;
                worksheet.Cell(row, 2).Value = entry.Applicativo;
                worksheet.Cell(row, 3).Value = entry.Area;
                worksheet.Cell(row, 4).Value = entry.Nome;
                worksheet.Cell(row, 5).Value = entry.Email;
                worksheet.Cell(row, 6).Value = entry.Telefono;
                row++;
            }

            // Auto-fit colonne
            worksheet.Columns().AdjustToContents();
        }

        /// <summary>
        /// Sanitizza il nome del foglio Excel (max 31 caratteri, no caratteri speciali)
        /// </summary>
        private string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Sheet1";

            // Rimuovi caratteri non validi per nomi foglio Excel
            var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
            var sanitized = name;

            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), "");
            }

            // Limita a 31 caratteri (limite Excel)
            if (sanitized.Length > 31)
            {
                sanitized = sanitized.Substring(0, 31);
            }

            return sanitized;
        }

        #endregion
    }
}