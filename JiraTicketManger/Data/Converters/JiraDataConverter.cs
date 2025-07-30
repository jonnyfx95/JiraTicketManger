using System;
using System.Data;
using System.Linq;
using Newtonsoft.Json.Linq;
using JiraTicketManager.Services;

namespace JiraTicketManager.Data.Converters
{
    /// <summary>
    /// Convertitore specializzato per la conversione di dati Jira in DataTable.
    /// Gestisce in modo sicuro tutti i tipi di campi Jira (oggetti, array, valori semplici).
    /// Ricostruito completamente basandosi sulla logica funzionante di CreateManualDataTable.
    /// </summary>
    public static class JiraDataConverter
    {
        #region Public Methods

        /// <summary>
        /// Converte un JArray di issues Jira in un DataTable per il binding alla UI.
        /// Gestisce in modo robusto tutti i tipi di errori e campi mancanti.
        /// </summary>
        /// <param name="issues">Array JSON delle issues Jira</param>
        /// <param name="logger">Logger per tracking errori e debug</param>
        /// <returns>DataTable pronto per il binding a DataGridView</returns>
        public static DataTable ConvertToDataTable(JArray issues, LoggingService logger = null)
        {
            var table = new DataTable();

            try
            {
                logger?.LogInfo($"🔄 Inizio conversione di {issues.Count} issues");

                // Define columns con tipi appropriati
                DefineTableColumns(table);
                logger?.LogDebug($"📋 Definite {table.Columns.Count} colonne");

                // Fill rows con gestione errori per issue singole
                FillTableRows(table, issues, logger);

                logger?.LogInfo($"✅ Conversione completata: {table.Rows.Count} record su {issues.Count} issues");
                return table;
            }
            catch (Exception ex)
            {
                logger?.LogError("❌ Errore grave in ConvertToDataTable", ex);
                return CreateErrorTable(ex);
            }
        }

        /// <summary>
        /// Converte una singola issue Jira in un DataRow.
        /// Metodo pubblico per conversioni singole.
        /// </summary>
        /// <param name="issue">Issue Jira in formato JSON</param>
        /// <param name="table">DataTable di destinazione</param>
        /// <param name="logger">Logger opzionale</param>
        /// <returns>DataRow popolato o null se errore</returns>
        public static DataRow ConvertSingleIssue(JToken issue, DataTable table, LoggingService logger = null)
        {
            try
            {
                var row = table.NewRow();
                PopulateDataRow(row, issue, logger);
                return row;
            }
            catch (Exception ex)
            {
                var issueKey = GetSafeStringValue(issue?["key"]) ?? "UNKNOWN";
                logger?.LogWarning($"⚠️ Errore conversione issue {issueKey}: {ex.Message}");
                return CreateErrorRow(table, issueKey, ex);
            }
        }

        #endregion

        #region Private Methods - Table Structure

        /// <summary>
        /// Definisce la struttura del DataTable con tutte le colonne necessarie.
        /// Basato sulla struttura di CreateManualDataTable che funziona.
        /// </summary>
        private static void DefineTableColumns(DataTable table)
        {
            // 9 colonne con Summary inclusa
            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Descrizione", typeof(string));        
            table.Columns.Add("Stato", typeof(string));
            table.Columns.Add("Assegnatario", typeof(string));
            table.Columns.Add("Area", typeof(string));
            table.Columns.Add("Applicativo", typeof(string));
            table.Columns.Add("Cliente", typeof(string));
            table.Columns.Add("Creato", typeof(string));
            table.Columns.Add("Completato", typeof(string));
        }

        /// <summary>
        /// Riempie le righe del DataTable processando tutte le issues.
        /// Logica identica a CreateManualDataTable ma con gestione errori avanzata.
        /// </summary>
        private static void FillTableRows(DataTable table, JArray issues, LoggingService logger)
        {
            int processedCount = 0;
            int errorCount = 0;

            foreach (JToken issue in issues)
            {
                try
                {
                    var row = table.NewRow();
                    PopulateDataRow(row, issue, logger);
                    table.Rows.Add(row);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    var issueKey = GetSafeStringValue(issue?["key"]) ?? $"ISSUE_{errorCount + 1}";
                    logger?.LogWarning($"⚠️ Errore conversione issue {issueKey}: {ex.Message}");

                    // Aggiungi riga di errore per mantenere il conteggio
                    var errorRow = CreateErrorRow(table, issueKey, ex);
                    table.Rows.Add(errorRow);
                    errorCount++;
                }
            }

            logger?.LogInfo($"📊 Elaborazione completata: {processedCount} successi, {errorCount} errori su {issues.Count} totali");
        }

        #endregion

        #region Private Methods - Row Population

        /// <summary>
        /// Popola una DataRow con i dati di una issue Jira.
        /// Logica IDENTICA a CreateManualDataTable che funziona perfettamente.
        /// </summary>
        private static void PopulateDataRow(DataRow row, JToken issue, LoggingService logger)
        {
            var fields = issue["fields"];

            // 9 colonne inclusa Summary
            row["Key"] = GetSafeStringValue(issue["key"]);
            row["Descrizione"] = GetSafeStringValue(fields?["summary"]);

            try
            {
                var statoValue = GetSafeStringValue(fields?["status"]?["name"]);
                row["Stato"] = !string.IsNullOrEmpty(statoValue) ? statoValue : "Sconosciuto";
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Errore lettura stato: {ex.Message}");
                row["Stato"] = "Errore lettura stato";
            }

            // *** NUOVO: Assignee con controllo null esplicito ***
            try
            {
                var assigneeField = fields?["assignee"];
                string assigneeValue = "";

                if (assigneeField != null && assigneeField.Type != JTokenType.Null)
                {
                    assigneeValue = GetSafeStringValue(assigneeField["displayName"]) ?? "";
                }

                row["Assegnatario"] = !string.IsNullOrEmpty(assigneeValue) ? assigneeValue : "Non assegnato";
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Errore lettura assegnatario: {ex.Message}");
                row["Assegnatario"] = "Non assegnato";
            }

            row["Area"] = GetCustomFieldValue(fields, "customfield_10113");
            row["Applicativo"] = GetCustomFieldValue(fields, "customfield_10114");
            row["Cliente"] = GetOrganizationValue(fields);
            row["Creato"] = GetSafeStringValue(fields?["created"]);
            row["Completato"] = GetCustomFieldValue(fields, "customfield_10172");
            
        }

        /// <summary>
        /// Crea una riga di errore quando la conversione fallisce
        /// </summary>
        private static DataRow CreateErrorRow(DataTable table, string issueKey, Exception ex)
        {
            var errorRow = table.NewRow();

            // Popola SOLO le 8 colonne richieste
            errorRow["Key"] = issueKey ?? "ERROR";
            errorRow["Stato"] = "Errore";
            errorRow["Assegnatario"] = "";
            errorRow["Area"] = "";
            errorRow["Applicativo"] = "";
            errorRow["Cliente"] = "";
            errorRow["Creato"] = "";
            errorRow["Completato"] = $"Errore: {ex.Message}";

            return errorRow;
        }

        #endregion

        #region Private Methods - Safe Value Extraction



        /// <summary>
        /// Estrae in modo sicuro un custom field value.
        /// Versione avanzata che gestisce oggetti complessi, array e workspace objects.
        /// </summary>
        private static string GetSafeCustomFieldValue(JToken fields, string fieldId, LoggingService logger = null)
        {
            try
            {
                if (fields == null) return "";

                var customField = fields[fieldId];
                if (customField == null || customField.Type == JTokenType.Null)
                    return "";

                // Usa la logica avanzata per formattazione
                return FormatTokenEnhanced(customField, logger);
            }
            catch (Exception ex)
            {
                logger?.LogDebug($"Errore estrazione custom field {fieldId}: {ex.Message}");
                return "";
            }
        }


        private static string FormatTokenEnhanced(JToken token, LoggingService logger = null)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return "";
            }

            try
            {
                switch (token.Type)
                {
                    case JTokenType.String:
                    case JTokenType.Integer:
                    case JTokenType.Boolean:
                        return token.ToString();

                    case JTokenType.Date:
                        if (token is JValue jValue && jValue.Value is DateTime dateValue)
                        {
                            return dateValue.ToString("dd/MM/yyyy");
                        }
                        return token.ToString();

                    case JTokenType.Object:
                        var obj = token as JObject;

                        // === GESTIONE SPECIALE WORKSPACE/REFERENCE (Cliente Partner) ===
                        if (obj?["workspaceId"] != null && obj?["objectId"] != null)
                        {
                            var objectId = obj["objectId"]?.ToString();
                            var id = obj["id"]?.ToString();

                            if (!string.IsNullOrEmpty(objectId))
                            {
                                return $"ID: {objectId}";
                            }
                            else if (!string.IsNullOrEmpty(id) && id.Contains(":"))
                            {
                                var parts = id.Split(':');
                                return $"Ref: {parts[parts.Length - 1]}";
                            }
                            else
                            {
                                return "[Riferimento oggetto]";
                            }
                        }
                        else
                        {
                            // === GESTIONE NORMALE OGGETTI ===
                            var possibleFields = new[] { "displayName", "name", "value", "emailAddress", "key" };

                            foreach (var field in possibleFields)
                            {
                                if (obj?[field] != null)
                                {
                                    var value = obj[field].ToString();
                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        return value;
                                    }
                                }
                            }

                            // Fallback su logica esistente
                            return GetSafeStringValue(token);
                        }

                    case JTokenType.Array:
                        var arr = token as JArray;
                        if (arr?.Count == 0)
                        {
                            return "";
                        }
                        else if (arr?.Count == 1)
                        {
                            // Array con singolo elemento
                            return FormatTokenEnhanced(arr[0], logger);
                        }
                        else
                        {
                            // Array multipli - concatena i valori
                            var values = new List<string>();
                            foreach (var item in arr ?? Enumerable.Empty<JToken>())
                            {
                                var formattedValue = FormatTokenEnhanced(item, logger);
                                if (!string.IsNullOrEmpty(formattedValue))
                                {
                                    values.Add(formattedValue);
                                }
                            }
                            return string.Join(", ", values);
                        }

                    default:
                        return token.ToString();
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug($"Errore FormatTokenEnhanced: {ex.Message}");
                return "[Errore formattazione]";
            }
        }



        /// <summary>
        /// Estrae in modo sicuro un valore stringa da un JToken.
        /// Gestisce tutti i tipi di JToken senza generare eccezioni.
        /// </summary>
        public static string GetSafeStringValue(JToken token)
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return "";

                // Gestisce tutti i tipi di JToken
                return token.Type switch
                {
                    JTokenType.String => token.ToString(),
                    JTokenType.Integer => token.ToString(),
                    JTokenType.Float => token.ToString(),
                    JTokenType.Boolean => token.ToString(),
                    JTokenType.Date => token.ToString(),
                    JTokenType.Object => token.ToString(), // Serializza l'oggetto
                    JTokenType.Array => string.Join(", ", token.Select(t => t.ToString())),
                    _ => token.ToString()
                };
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Ottiene valore custom field con gestione robusta.
        /// Logica IDENTICA a CreateManualDataTable.
        /// </summary>
        public static string GetCustomFieldValue(JToken fields, string fieldId)
        {
            try
            {
                var field = fields?[fieldId];
                if (field == null) return "";

                // Se è un oggetto con "value"
                if (field is JObject obj && obj["value"] != null)
                    return obj["value"].ToString();

                // Se è stringa diretta
                if (field is JValue val)
                    return val.ToString();

                return field.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Ottiene valore Organization con fallback multipli.
        /// Logica IDENTICA a CreateManualDataTable.
        /// </summary>
        private static string GetOrganizationValue(JToken fields)
        {
            try
            {
                // Organization può essere in diversi posti
                var org = fields?["customfield_10002"]?.FirstOrDefault()?["name"]?.ToString();
                if (!string.IsNullOrEmpty(org)) return org;

                org = fields?["organization"]?["name"]?.ToString();
                if (!string.IsNullOrEmpty(org)) return org;

                // Fallback su customfield_10117 (campo cliente principale)
                org = GetCustomFieldValue(fields, "customfield_10117");
                if (!string.IsNullOrEmpty(org)) return org;

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Tronca la descrizione per evitare problemi di performance nella UI
        /// </summary>
        private static string TruncateDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";

            const int maxLength = 200;
            if (description.Length <= maxLength) return description;

            return description.Substring(0, maxLength) + "...";
        }

        #endregion

        #region Private Methods - Error Handling

        /// <summary>
        /// Crea una tabella di errore quando la conversione fallisce completamente
        /// </summary>
        private static DataTable CreateErrorTable(Exception ex)
        {
            var table = new DataTable();

            table.Columns.Add("Key", typeof(string));
            table.Columns.Add("Errore", typeof(string));
            table.Columns.Add("Dettagli", typeof(string));

            var errorRow = table.NewRow();
            errorRow["Key"] = "SYSTEM_ERROR";
            errorRow["Errore"] = $"Errore sistema: {ex.Message}";
            errorRow["Dettagli"] = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0)) ?? "";
            table.Rows.Add(errorRow);

            return table;
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Valida se un JArray contiene dati validi per la conversione
        /// </summary>
        public static bool IsValidJiraData(JArray issues)
        {
            if (issues == null || issues.Count == 0)
                return false;

            // Verifica che almeno il primo elemento abbia la struttura base
            var firstIssue = issues.First;
            return firstIssue?["key"] != null && firstIssue?["fields"] != null;
        }

        /// <summary>
        /// Conta quanti record sono stati convertiti con successo
        /// </summary>
        public static int CountSuccessfulConversions(DataTable table)
        {
            if (table == null) return 0;

            // Conta le righe che non sono errori
            return table.AsEnumerable()
                .Count(row => !row["Summary"].ToString().StartsWith("❌ Errore"));
        }

        /// <summary>
        /// Ottiene statistiche sulla conversione
        /// </summary>
        public static ConversionStatistics GetConversionStatistics(DataTable table, int originalCount)
        {
            return new ConversionStatistics
            {
                OriginalCount = originalCount,
                ConvertedCount = table?.Rows.Count ?? 0,
                SuccessCount = CountSuccessfulConversions(table),
                ErrorCount = (table?.Rows.Count ?? 0) - CountSuccessfulConversions(table),
                SuccessRate = originalCount > 0 ? (double)CountSuccessfulConversions(table) / originalCount : 0.0
            };
        }

        #endregion

        #region DataConver

        /// <summary>
        /// Formatta una data per la visualizzazione in formato italiano (dd/MM/yyyy)
        /// </summary>
        /// <param name="dateValue">Valore data da formattare (può essere string, DateTime, o object)</param>
        /// <returns>Data formattata come dd/MM/yyyy o stringa vuota se non valida</returns>
        public static string FormatDateForDisplay(object dateValue)
        {
            try
            {
                if (dateValue == null || dateValue == DBNull.Value)
                    return "";

                // Se è già un DateTime
                if (dateValue is DateTime dateTime)
                {
                    return dateTime.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                }
                // Se è una stringa, prova a parsarla
                else if (dateValue is string dateString)
                {
                    if (string.IsNullOrWhiteSpace(dateString))
                        return "";

                    // Prova prima con i formati Jira comuni
                    var formats = new[]
                    {
                "yyyy-MM-dd",                       // Formato più comune da Jira  
                "yyyy-MM-ddTHH:mm:ss.fffzzz",      // ISO completo con timezone
                "yyyy-MM-ddTHH:mm:ss.fff",         // ISO senza timezone
                "yyyy-MM-ddTHH:mm:sszzz",          // ISO con timezone senza millisecondi
                "yyyy-MM-ddTHH:mm:ss",             // ISO base
                "yyyy-MM-dd HH:mm:ss"              // Formato standard
            };

                    foreach (var format in formats)
                    {
                        if (DateTime.TryParseExact(dateString, format,
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            return parsedDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }

                    // Fallback: parsing automatico
                    if (DateTime.TryParse(dateString, out DateTime fallbackDate))
                    {
                        return fallbackDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }

                    return "";
                }
                else
                {
                    // Prova conversione generica
                    if (DateTime.TryParse(dateValue.ToString(), out DateTime genericDate))
                    {
                        return genericDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    return "";
                }


            }
            catch (Exception)
            {
                return "";
            }
        }

        #endregion

    }

    #region Supporting Classes

    /// <summary>
    /// Statistiche sulla conversione dei dati
    /// </summary>
    public class ConversionStatistics
    {
        public int OriginalCount { get; set; }
        public int ConvertedCount { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public double SuccessRate { get; set; }

        public override string ToString()
        {
            return $"Conversione: {SuccessCount}/{OriginalCount} successi ({SuccessRate:P1}), {ErrorCount} errori";
        }
    }

    #endregion
}