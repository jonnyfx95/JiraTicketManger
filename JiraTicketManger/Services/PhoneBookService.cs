using ClosedXML.Excel;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Data.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private const string SYNC_CONFIG_FILENAME = "phonebook_sync.json"; // ← NUOVO
        private const int API_BATCH_SIZE = 100;
        private const int MAX_BATCHES = 500;

        // Query BASE (senza filtro temporale)
        private const string JQL_QUERY_BASE = "project = CC and \"Telefono[Short text]\" IS NOT EMPTY and \"Cliente[Dropdown]\" is not EMPTY and \"Cliente[Dropdown]\" != DEDANEXT";

        #endregion


        #region Private Fields

        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;
        private readonly string _syncConfigFilePath;
        private readonly string _cacheFilePath;


        #endregion

        #region Constructor

        public PhoneBookService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("PhoneBookService");

            // Percorso cache nella cartella data/
            var dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CACHE_DIRECTORY);
            _cacheFilePath = Path.Combine(dataDirectory, CACHE_FILENAME);
            _syncConfigFilePath = Path.Combine(dataDirectory, SYNC_CONFIG_FILENAME); // ← NUOVO

            _logger.LogInfo($"PhoneBookService inizializzato. Cache: {_cacheFilePath}, SyncConfig: {_syncConfigFilePath}");
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
        /// Forza il refresh dalla API Jira con caricamento incrementale.
        /// - Prima sync: carica tutti i ticket
        /// - Sync successive: carica solo ticket aggiornati dall'ultima sync
        /// </summary>
        public async Task<List<PhoneBookEntry>> RefreshFromApiAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("=== REFRESH DA API JIRA ===");

                // ✅ CARICA CONFIG SYNC
                var syncConfig = await LoadSyncConfigAsync();
                var isFirstSync = !syncConfig.LastSyncDate.HasValue;
                var syncStartTime = DateTime.UtcNow; // Salva l'ora di inizio sync

                if (isFirstSync)
                {
                    _logger.LogInfo("🆕 PRIMA SINCRONIZZAZIONE - Caricamento completo");
                    progress?.Report("Prima sincronizzazione: caricamento completo da Jira...");
                }
                else
                {
                    _logger.LogInfo($"🔄 SINCRONIZZAZIONE INCREMENTALE - Solo ticket aggiornati dopo {syncConfig.LastSyncDate:yyyy-MM-dd HH:mm:ss}");
                    progress?.Report($"Caricamento ticket aggiornati dopo {syncConfig.LastSyncDate:yyyy-MM-dd HH:mm}...");
                }

                // ✅ COSTRUISCI QUERY DINAMICA
                var jqlQuery = BuildJqlQuery(syncConfig.LastSyncDate);
                _logger.LogInfo($"Query JQL: {jqlQuery}");

                // ✅ CARICA CACHE ESISTENTE (se sync incrementale)
                List<PhoneBookEntry> existingEntries = new List<PhoneBookEntry>();
                if (!isFirstSync && File.Exists(_cacheFilePath))
                {
                    existingEntries = await LoadFromCacheAsync();
                    _logger.LogInfo($"📂 Cache esistente caricata: {existingEntries.Count} entries");
                }

                // ✅ CARICA NUOVI/AGGIORNATI TICKET
                var newEntries = await FetchTicketsFromApiAsync(jqlQuery, progress);

                _logger.LogInfo($"📥 Nuove entries estratte: {newEntries.Count}");

                // ✅ MERGE: Combina vecchie + nuove entries
                List<PhoneBookEntry> allEntries;

                if (isFirstSync)
                {
                    // Prima sync: usa solo le nuove
                    allEntries = newEntries;
                }
                else
                {
                    // Sync incrementale: merge intelligente
                    _logger.LogInfo("🔀 Merge entries esistenti + nuove...");
                    allEntries = MergeEntries(existingEntries, newEntries);
                }

                progress?.Report("Rimozione duplicati...");

                // ✅ DEDUPLICAZIONE FINALE
                var uniqueEntries = RemoveDuplicates(allEntries);
                _logger.LogInfo($"✅ Entries uniche dopo deduplicazione: {uniqueEntries.Count}");

                // ✅ SALVA CACHE
                progress?.Report("Salvataggio cache...");
                await SaveToCacheAsync(uniqueEntries);

                // ✅ SALVA DATA SYNC
                await SaveSyncConfigAsync(syncStartTime, uniqueEntries.Count);

                _logger.LogInfo("=== REFRESH COMPLETATO ===");
                progress?.Report($"✅ Rubrica aggiornata: {uniqueEntries.Count} contatti totali ({newEntries.Count} nuovi/aggiornati)");

                return uniqueEntries;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore refresh da API", ex);
                throw;
            }
        }

        /// <summary>
        /// Scarica i ticket da Jira API con paginazione NextPageToken
        /// </summary>
        private async Task<List<PhoneBookEntry>> FetchTicketsFromApiAsync(string jqlQuery, IProgress<string> progress)
        {
            var allEntries = new List<PhoneBookEntry>();
            var seenTicketKeys = new HashSet<string>();

            string nextPageToken = null;
            var batchNumber = 1;
            var totalTicketsProcessed = 0;

            while (batchNumber <= MAX_BATCHES)
            {
                _logger.LogInfo($"📦 === BATCH {batchNumber}/{MAX_BATCHES} ===");

                progress?.Report($"Caricamento batch {batchNumber}...");

                var searchResult = await _jiraApiService.SearchIssuesAsync(
                    jqlQuery,
                    0,
                    API_BATCH_SIZE,
                    null,
                    nextPageToken
                );

                if (searchResult.Issues.Count == 0)
                {
                    _logger.LogInfo($"🏁 Nessun ticket ricevuto - fine paginazione");
                    break;
                }

                // Conta nuovi ticket
                var newTicketsInBatch = 0;
                foreach (var issue in searchResult.Issues)
                {
                    var ticketKey = issue["key"]?.ToString();
                    if (!string.IsNullOrEmpty(ticketKey) && seenTicketKeys.Add(ticketKey))
                    {
                        newTicketsInBatch++;
                    }
                }

                totalTicketsProcessed += searchResult.Issues.Count;
                _logger.LogInfo($"   ✅ Ricevuti: {searchResult.Issues.Count} ticket ({newTicketsInBatch} nuovi)");
                _logger.LogInfo($"   📊 Totale: {totalTicketsProcessed} ticket processati");

                // Estrai entries
                var batchEntries = ExtractEntriesFromTickets(searchResult.Issues);
                allEntries.AddRange(batchEntries);

                progress?.Report($"Processati {totalTicketsProcessed} ticket, {allEntries.Count} contatti estratti...");

                // Prepara prossimo batch
                nextPageToken = searchResult.NextPageToken;

                if (string.IsNullOrEmpty(nextPageToken))
                {
                    _logger.LogInfo($"🏁 NextPageToken vuoto - fine paginazione");
                    break;
                }

                batchNumber++;
                await Task.Delay(50);
            }

            _logger.LogInfo($"📊 Fetch completato: {totalTicketsProcessed} ticket, {allEntries.Count} entries estratte");
            return allEntries;
        }

        /// <summary>
/// Merge intelligente: combina entries esistenti con quelle nuove/aggiornate
/// Se un contatto esiste già (stesso unique key), viene sostituito con la versione aggiornata
/// </summary>
private List<PhoneBookEntry> MergeEntries(List<PhoneBookEntry> existingEntries, List<PhoneBookEntry> newEntries)
{
    try
    {
        // Usa dizionario per merge efficiente (key = GetUniqueKey())
        var merged = new Dictionary<string, PhoneBookEntry>();

        // Aggiungi tutte le entries esistenti
        foreach (var entry in existingEntries)
        {
            var key = entry.GetUniqueKey();
            if (!string.IsNullOrEmpty(key))
            {
                merged[key] = entry;
            }
        }

        _logger.LogInfo($"   📂 Entries esistenti nel dizionario: {merged.Count}");

        // Sovrascrivi/aggiungi con le nuove (più recenti)
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var entry in newEntries)
        {
            var key = entry.GetUniqueKey();
            if (!string.IsNullOrEmpty(key))
            {
                if (merged.ContainsKey(key))
                {
                    updatedCount++;
                }
                else
                {
                    addedCount++;
                }
                
                merged[key] = entry; // Sovrascrivi o aggiungi
            }
        }

        _logger.LogInfo($"   ✅ Merge completato: {addedCount} aggiunte, {updatedCount} aggiornate");
        _logger.LogInfo($"   📊 Totale entries dopo merge: {merged.Count}");

        return merged.Values.ToList();
    }
    catch (Exception ex)
    {
        _logger.LogError("Errore merge entries", ex);
        // Fallback: ritorna solo le nuove
        return newEntries;
    }
}


        /// <summary>
        /// Esporta i contatti in Excel in UN SOLO FOGLIO.
        /// Esporta esattamente la lista passata, senza raggruppamenti.
        /// </summary>
        public async Task ExportToExcelAsync(List<PhoneBookEntry> entries, string filePath)
        {
            try
            {
                _logger.LogInfo($"Export Excel: {filePath}");
                _logger.LogInfo($"📊 Contatti da esportare: {entries.Count}");

                if (entries == null || entries.Count == 0)
                {
                    throw new InvalidOperationException("Nessun contatto da esportare");
                }

                await Task.Run(() =>
                {
                    using (var workbook = new XLWorkbook())
                    {
                        // ✅ UN SOLO FOGLIO
                        var worksheet = workbook.Worksheets.Add("Rubrica");

                        // ✅ HEADER
                        worksheet.Cell(1, 1).Value = "Cliente";
                        worksheet.Cell(1, 2).Value = "Area";
                        worksheet.Cell(1, 3).Value = "Applicativo";
                        worksheet.Cell(1, 4).Value = "Nome";
                        worksheet.Cell(1, 5).Value = "Email";
                        worksheet.Cell(1, 6).Value = "Telefono";

                        // Formattazione header
                        var headerRange = worksheet.Range(1, 1, 1, 6);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                        // ✅ DATI - Esattamente come arrivano (senza ordinamenti o raggruppamenti)
                        int row = 2;
                        foreach (var entry in entries)
                        {
                            worksheet.Cell(row, 1).Value = entry.Cliente ?? "";
                            worksheet.Cell(row, 2).Value = entry.Area ?? "";
                            worksheet.Cell(row, 3).Value = entry.Applicativo ?? "";
                            worksheet.Cell(row, 4).Value = entry.Nome ?? "";
                            worksheet.Cell(row, 5).Value = entry.Email ?? "";
                            worksheet.Cell(row, 6).Value = entry.Telefono ?? "";
                            row++;
                        }

                        // Auto-fit colonne
                        worksheet.Columns().AdjustToContents();

                        // Congela header
                        worksheet.SheetView.FreezeRows(1);

                        // Filtri automatici
                        worksheet.RangeUsed().SetAutoFilter();

                        workbook.SaveAs(filePath);
                    }
                });

                _logger.LogInfo($"✅ Export completato: {entries.Count} contatti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore export Excel", ex);
                throw;
            }
        }




        /// <summary>
        /// Ottiene un nome foglio Excel unico, sanitizzato e valido.
        /// Gestisce caratteri speciali, troncamento e duplicati.
        /// </summary>
        private string GetUniqueSheetName(string originalName, HashSet<string> usedNames)
        {
            if (string.IsNullOrWhiteSpace(originalName))
                return "Cliente Sconosciuto";

            try
            {
                // ✅ STEP 1: Sanitizza caratteri speciali e invalidi
                var sanitized = SanitizeSheetName(originalName);

                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    sanitized = "Cliente";
                }

                // ✅ STEP 2: Tronca a 31 caratteri (limite Excel)
                const int MAX_LENGTH = 31;

                if (sanitized.Length > MAX_LENGTH)
                {
                    sanitized = sanitized.Substring(0, MAX_LENGTH).TrimEnd();
                }

                // ✅ STEP 3: Gestisci duplicati aggiungendo numero progressivo
                var finalName = sanitized;
                var counter = 1;

                while (usedNames.Contains(finalName))
                {
                    // Aggiungi suffisso numerico: "Nome Cliente (2)"
                    var suffix = $" ({counter})";
                    var maxBaseLength = MAX_LENGTH - suffix.Length;

                    var baseName = sanitized.Length > maxBaseLength
                        ? sanitized.Substring(0, maxBaseLength).TrimEnd()
                        : sanitized;

                    finalName = baseName + suffix;
                    counter++;

                    // Safety: evita loop infiniti
                    if (counter > 1000)
                    {
                        _logger.LogError($"Troppi duplicati per '{originalName}' - usando nome generico");
                        finalName = $"Cliente_{Guid.NewGuid().ToString().Substring(0, 8)}";
                        break;
                    }
                }

                // ✅ STEP 4: Registra come usato
                usedNames.Add(finalName);

                // Log se nome modificato
                if (finalName != originalName)
                {
                    _logger.LogDebug($"📝 Nome foglio: '{originalName}' → '{finalName}'");
                }

                return finalName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore generazione nome foglio per '{originalName}'", ex);
                return $"Cliente_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        /// <summary>
        /// Sanitizza il nome del foglio Excel rimuovendo/sostituendo caratteri non validi.
        /// </summary>
        private string SanitizeSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Sheet1";

            try
            {
                // ✅ STEP 1: Rimuovi caratteri COMPLETAMENTE INVALIDI per Excel
                // Excel non permette: : \ / ? * [ ]
                var invalidChars = new[] { ':', '\\', '/', '?', '*', '[', ']' };
                var sanitized = name;

                foreach (var c in invalidChars)
                {
                    sanitized = sanitized.Replace(c.ToString(), "");
                }

                // ✅ STEP 2: Sostituisci caratteri SPECIALI problematici
                // Accenti e caratteri unicode
                sanitized = sanitized
                    .Replace('à', 'a').Replace('è', 'e').Replace('é', 'e').Replace('ì', 'i')
                    .Replace('ò', 'o').Replace('ù', 'u').Replace('á', 'a').Replace('í', 'i')
                    .Replace('ó', 'o').Replace('ú', 'u').Replace('ä', 'a').Replace('ë', 'e')
                    .Replace('ï', 'i').Replace('ö', 'o').Replace('ü', 'u').Replace('â', 'a')
                    .Replace('ê', 'e').Replace('î', 'i').Replace('ô', 'o').Replace('û', 'u')
                    .Replace('À', 'A').Replace('È', 'E').Replace('É', 'E').Replace('Ì', 'I')
                    .Replace('Ò', 'O').Replace('Ù', 'U').Replace('Á', 'A').Replace('Í', 'I')
                    .Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ä', 'A').Replace('Ë', 'E')
                    .Replace('Ï', 'I').Replace('Ö', 'O').Replace('Ü', 'U').Replace('Â', 'A')
                    .Replace('Ê', 'E').Replace('Î', 'I').Replace('Ô', 'O').Replace('Û', 'U');

                // ✅ STEP 3: Rimuovi caratteri speciali comuni
                sanitized = sanitized
                             .Replace('`', '\'')  // Backtick → apostrofo normale
                             .Replace('‘', '\'')  // Smart quote sinistra → apostrofo normale
                             .Replace('’', '\'')  // Smart quote destra → apostrofo normale
                             .Replace('“', '\"')  // Virgolette alte sinistra → doppio apice
                             .Replace('”', '\"')  // Virgolette alte destra → doppio apice
                             .Replace('–', '-')   // En dash → trattino
                             .Replace('—', '-')   // Em dash → trattino
                            
                             .Replace('°', ' ')
                             .Replace('§', 'S')
                             .Replace('©', 'C')
                             .Replace('®', 'R')
                             .Replace("™", "TM")  // Trademark → TM
                             .Replace("€", "EUR") // Euro → EUR
                             .Replace("£", "GBP") // Sterlina → GBP
                             .Replace("$", "USD");// Dollaro → USD
                // ✅ STEP 4: Rimuovi spazi multipli e trim
                while (sanitized.Contains("  "))
                {
                    sanitized = sanitized.Replace("  ", " ");
                }

                sanitized = sanitized.Trim();

                // ✅ STEP 5: Se dopo la sanitizzazione è vuoto, usa default
                if (string.IsNullOrWhiteSpace(sanitized))
                {
                    sanitized = "Cliente";
                }

                return sanitized;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore sanitizzazione nome '{name}'", ex);
                return "Cliente";
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
            worksheet.Cell(1, 2).Value = "Area";
            worksheet.Cell(1, 3).Value = "Applicativo";
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
                worksheet.Cell(row, 2).Value = entry.Area;
                worksheet.Cell(row, 3).Value = entry.Applicativo;
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
      

        #endregion

        #region Sync Configuration

        /// <summary>
        /// Configurazione per il sync incrementale
        /// </summary>
        private class PhoneBookSyncConfig
        {
            /// <summary>
            /// Data dell'ultima sincronizzazione con Jira
            /// </summary>
            public DateTime? LastSyncDate { get; set; }

            /// <summary>
            /// Numero totale di entries nell'ultima sync
            /// </summary>
            public int TotalEntries { get; set; }
        }

        /// <summary>
        /// Carica la configurazione di sync dal file JSON
        /// </summary>
        private async Task<PhoneBookSyncConfig> LoadSyncConfigAsync()
        {
            try
            {
                if (!File.Exists(_syncConfigFilePath))
                {
                    _logger.LogInfo("Nessun file sync trovato - prima sincronizzazione");
                    return new PhoneBookSyncConfig { LastSyncDate = null, TotalEntries = 0 };
                }

                var jsonContent = await File.ReadAllTextAsync(_syncConfigFilePath);
                var config = JsonConvert.DeserializeObject<PhoneBookSyncConfig>(jsonContent);

                _logger.LogInfo($"Sync config caricata: LastSyncDate={config.LastSyncDate:yyyy-MM-dd HH:mm:ss}, TotalEntries={config.TotalEntries}");
                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento sync config", ex);
                return new PhoneBookSyncConfig { LastSyncDate = null, TotalEntries = 0 };
            }
        }

        /// <summary>
        /// Salva la configurazione di sync nel file JSON
        /// </summary>
        private async Task SaveSyncConfigAsync(DateTime syncDate, int totalEntries)
        {
            try
            {
                var config = new PhoneBookSyncConfig
                {
                    LastSyncDate = syncDate,
                    TotalEntries = totalEntries
                };

                // Assicura che la directory esista
                var directory = Path.GetDirectoryName(_syncConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var jsonContent = JsonConvert.SerializeObject(config, Formatting.Indented);
                await File.WriteAllTextAsync(_syncConfigFilePath, jsonContent);

                _logger.LogInfo($"Sync config salvata: LastSyncDate={syncDate:yyyy-MM-dd HH:mm:ss}, TotalEntries={totalEntries}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio sync config", ex);
            }
        }

        /// <summary>
        /// Costruisce la query JQL con filtro temporale opzionale
        /// </summary>
        private string BuildJqlQuery(DateTime? lastSyncDate)
        {
            if (lastSyncDate.HasValue)
            {
                // Sync incrementale: solo ticket aggiornati dopo l'ultima sync
                // Formato Jira: "2025-01-09 14:30"
                var jiraDateFormat = lastSyncDate.Value.ToString("yyyy-MM-dd HH:mm");
                var query = $"{JQL_QUERY_BASE} AND updated >= \"{jiraDateFormat}\" ORDER BY updated DESC";

                _logger.LogInfo($"🔄 SYNC INCREMENTALE: aggiornati dopo {jiraDateFormat}");
                return query;
            }
            else
            {
                // Prima sync: carica tutto
                var query = $"{JQL_QUERY_BASE} ORDER BY updated DESC";

                _logger.LogInfo("📦 PRIMA SYNC: caricamento completo");
                return query;
            }
        }

        #endregion
    }
}