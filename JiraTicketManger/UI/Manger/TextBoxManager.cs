using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.UI.Managers
{
    /// <summary>
    /// Manager per popolare TextBox con dati Jira usando API esistenti
    /// Riutilizza JiraDataService e JiraFieldExtractor
    /// </summary>
    public class TextBoxManager : IDisposable
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraDataService _dataService;
        private readonly Dictionary<TextBox, string> _textBoxMappings;
        private bool _disposed = false;

        #endregion

        #region Constructor

        public TextBoxManager(JiraDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = LoggingService.CreateForComponent("TextBoxManager");
            _textBoxMappings = new Dictionary<TextBox, string>();

            _logger.LogInfo("TextBoxManager inizializzato");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Popola una singola TextBox con dato da ticket Jira
        /// </summary>
        /// <param name="textBox">TextBox da popolare</param>
        /// <param name="ticketKey">Numero ticket (es: CC-12345)</param>
        /// <param name="jiraField">Nome campo Jira (es: "reporter", "customfield_10136")</param>
        public async Task PopulateTextBoxAsync(TextBox textBox, string ticketKey, string jiraField)
        {
            try
            {
                if (textBox == null)
                {
                    _logger.LogWarning($"TextBox null per campo {jiraField}");
                    return;
                }

                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning($"TicketKey vuoto per campo {jiraField}");
                    ClearTextBox(textBox);
                    return;
                }

                _logger.LogDebug($"Popolamento TextBox per ticket {ticketKey}, campo {jiraField}");

                // 1. Ottieni i dati del ticket usando API esistente
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket {ticketKey} non trovato");
                    SetTextBoxValue(textBox, "Ticket non trovato");
                    return;
                }

                // 2. Estrai il valore del campo usando JiraFieldExtractor esistente
                var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);

                // 3. Popola la TextBox
                SetTextBoxValue(textBox, fieldValue);

                // 4. Salva mapping per future operazioni
                _textBoxMappings[textBox] = jiraField;

                _logger.LogDebug($"TextBox popolata: {jiraField} = '{fieldValue}'");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore popolamento TextBox per {ticketKey}.{jiraField}", ex);
                SetTextBoxValue(textBox, $"Errore: {ex.Message}");
            }
        }

        /// <summary>
        /// Popola multiple TextBox con un singolo caricamento ticket
        /// </summary>
        /// <param name="ticketKey">Numero ticket</param>
        /// <param name="textBoxFieldMappings">Dizionario TextBox → Campo Jira</param>
        public async Task PopulateMultipleTextBoxesAsync(string ticketKey, Dictionary<TextBox, string> textBoxFieldMappings)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("TicketKey vuoto per popolamento multiplo");
                    ClearAllTextBoxes(textBoxFieldMappings.Keys);
                    return;
                }

                _logger.LogInfo($"Popolamento multiplo per ticket {ticketKey} - {textBoxFieldMappings.Count} campi");

                // 1. Carica ticket una sola volta (ottimizzazione)
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket {ticketKey} non trovato");
                    SetAllTextBoxesValue(textBoxFieldMappings.Keys, "Ticket non trovato");
                    return;
                }

                // 2. Popola tutti i campi
                foreach (var mapping in textBoxFieldMappings)
                {
                    var textBox = mapping.Key;
                    var jiraField = mapping.Value;

                    try
                    {
                        var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);
                        SetTextBoxValue(textBox, fieldValue);
                        _textBoxMappings[textBox] = jiraField;

                        _logger.LogDebug($"Campo popolato: {jiraField} = '{fieldValue}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore popolamento campo {jiraField}", ex);
                        SetTextBoxValue(textBox, $"Errore campo");
                    }
                }

                _logger.LogInfo($"Popolamento multiplo completato per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore popolamento multiplo per {ticketKey}", ex);
                SetAllTextBoxesValue(textBoxFieldMappings.Keys, $"Errore caricamento");
            }
        }

        /// <summary>
        /// Pulisce tutte le TextBox mappate
        /// </summary>
        public void ClearAllMappedTextBoxes()
        {
            foreach (var textBox in _textBoxMappings.Keys)
            {
                ClearTextBox(textBox);
            }
            _textBoxMappings.Clear();
            _logger.LogDebug("Tutte le TextBox mappate pulite");
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Estrae il valore di un campo usando JiraFieldExtractor esistente
        /// </summary>
        private string ExtractFieldValue(JToken rawData, string jiraField)
        {
            try
            {
                // === GESTIONE SPECIALE PER REPORTER EMAIL ===
                if (jiraField == "reporter.emailAddress")
                {
                    var reporterField = rawData["fields"]?["reporter"];
                    if (reporterField != null && reporterField.Type != JTokenType.Null)
                    {
                        var emailAddress = reporterField["emailAddress"]?.ToString();
                        return !string.IsNullOrEmpty(emailAddress) ? emailAddress : "-";
                    }
                    return "-";
                }

                // === GESTIONE SPECIALE PER CUSTOM FIELD TELEFONO ===
                if (jiraField == "customfield_10074")
                {
                    var phoneField = rawData["fields"]?["customfield_10074"];
                    if (phoneField != null && phoneField.Type != JTokenType.Null)
                    {
                        var phoneValue = phoneField.ToString();
                        return !string.IsNullOrEmpty(phoneValue) ? phoneValue : "-";
                    }
                    return "-";
                }

                // === GESTIONE SPECIALE PER DESCRIZIONE ===
                if (jiraField == "description")
                {
                    var descriptionField = rawData["fields"]?["description"];
                    if (descriptionField != null && descriptionField.Type != JTokenType.Null)
                    {
                        var description = descriptionField.ToString();
                        if (!string.IsNullOrEmpty(description))
                        {
                            // Formatta la descrizione per migliorare la leggibilità
                            return FormatDescription(description);
                        }
                    }
                    return "-";
                }

                // === GESTIONE SPECIALE PER CLIENTE PARTNER (ARRAY WORKSPACE) ===
                if (jiraField == "customfield_10103")
                {
                    var clientePartnerField = rawData["fields"]?["customfield_10103"];
                    if (clientePartnerField != null && clientePartnerField.Type == JTokenType.Array)
                    {
                        var array = clientePartnerField as JArray;
                        if (array?.Count > 0)
                        {
                            var firstItem = array[0];
                            if (firstItem?.Type == JTokenType.Object)
                            {
                                // Gestione workspace object
                                var objectId = firstItem["objectId"]?.ToString();
                                var id = firstItem["id"]?.ToString();

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
                        }
                    }
                    return "-";
                }

                // === GESTIONE STANDARD CON JIRA FIELD EXTRACTOR ===
                var fieldName = MapJiraFieldToExtractorField(jiraField);
                var value = JiraFieldExtractor.ExtractField(rawData, fieldName);

                // Gestisci casi speciali
                if (value == null || value == DBNull.Value)
                    return "-";

                if (value is DateTime dateTime)
                    return dateTime.ToString("dd/MM/yyyy HH:mm");

                return value.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore estrazione campo {jiraField}", ex);
                return "Errore estrazione";
            }
        }

        // <summary>
        /// Formatta la descrizione per una migliore leggibilità
        /// </summary>
        private string FormatDescription(string description)
        {
            try
            {
                if (string.IsNullOrEmpty(description))
                    return "-";

                // 1. Normalizza i caratteri di fine riga
                description = description
                    .Replace("\r\n", "\n")  // Windows → Unix
                    .Replace("\r", "\n");   // Mac → Unix

                // 2. Sostituisce sequenze multiple di spazi con spazi singoli
                while (description.Contains("  "))
                {
                    description = description.Replace("  ", " ");
                }

                // 3. Aggiunge spazi dopo la punteggiatura se mancano
                description = description
                    .Replace(".", ". ")
                    .Replace(",", ", ")
                    .Replace(":", ": ")
                    .Replace(";", "; ");

                // 4. Rimuove spazi doppi creati dal punto precedente
                while (description.Contains("  "))
                {
                    description = description.Replace("  ", " ");
                }

                // 5. Divide il testo in paragrafi logici (dopo punto e a capo)
                var lines = description.Split('\n');
                var formattedLines = new List<string>();

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        // Capitalizza la prima lettera se necessario
                        if (char.IsLower(trimmedLine[0]))
                        {
                            trimmedLine = char.ToUpper(trimmedLine[0]) + trimmedLine.Substring(1);
                        }
                        formattedLines.Add(trimmedLine);
                    }
                }

                // 6. Unisce con doppio a capo per paragrafi separati
                var formatted = string.Join("\r\n\r\n", formattedLines);

                // 7. Limita la lunghezza se troppo lungo (opzionale)
                if (formatted.Length > 1000)
                {
                    formatted = formatted.Substring(0, 997) + "...";
                }

                return formatted;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore formattazione descrizione", ex);
                return description; // Ritorna il testo originale in caso di errore
            }
        }


        /// <summary>
        /// Mappa i nomi dei campi API ai nomi del JiraFieldExtractor
        /// AGGIORNATO con mapping reporter corretto
        /// </summary>
        private string MapJiraFieldToExtractorField(string jiraField)
        {
            return jiraField switch
            {
                // === CAMPI REPORTER CORRETTI ===
                "reporter" => "Reporter",                    // reporter.displayName
                "reporter.emailAddress" => "ReporterEmail",  //  reporter.emailAddress  

                // === CAMPI STANDARD ===
                "assignee" => "Assignee",
                "status" => "Status",
                "priority" => "Priority",
                "issuetype" => "Type",
                "summary" => "Summary",
                "description" => "Description",
                "created" => "Created",
                "updated" => "Updated",
                "resolutiondate" => "ResolutionDate",

                // === CUSTOM FIELDS ===
                "customfield_10117" => "Cliente",
                "customfield_10113" => "Area",
                "customfield_10114" => "Applicativo",
                "customfield_10103" => "ClientePartner",
                "customfield_10074" => "Telefono",          // Telefono funziona

                // === CAMPI TEAM (anche se spesso vuoti) ===
                "customfield_10271" => "PMEmail",           // P.M. (mail)
                "customfield_10272" => "CommercialeEmail",  // Commerciale (mail)  
                "customfield_10238" => "ConsulenteEmail",   // Consulente (mail)
                "customfield_10096" => "WBS",               // WBS

                _ => jiraField // Se non mappato, usa il nome originale
            };
        }

        /// <summary>
        /// Imposta valore in TextBox thread-safe
        /// </summary>
        private void SetTextBoxValue(TextBox textBox, string value)
        {
            if (textBox.InvokeRequired)
            {
                textBox.Invoke(() => SetTextBoxValue(textBox, value));
                return;
            }

            textBox.Text = value ?? "-";
        }

        /// <summary>
        /// Pulisce una TextBox
        /// </summary>
        private void ClearTextBox(TextBox textBox)
        {
            SetTextBoxValue(textBox, "-");
        }

        /// <summary>
        /// Pulisce multiple TextBox
        /// </summary>
        private void ClearAllTextBoxes(IEnumerable<TextBox> textBoxes)
        {
            foreach (var textBox in textBoxes)
            {
                ClearTextBox(textBox);
            }
        }

        /// <summary>
        /// Imposta stesso valore in multiple TextBox
        /// </summary>
        private void SetAllTextBoxesValue(IEnumerable<TextBox> textBoxes, string value)
        {
            foreach (var textBox in textBoxes)
            {
                SetTextBoxValue(textBox, value);
            }
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
                    _textBoxMappings?.Clear();
                    _logger.LogDebug("TextBoxManager disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Errore dispose TextBoxManager", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion

        

        #region Label Support Methods

        /// <summary>
        /// Popola una Label con valore del campo Jira
        /// </summary>
        /// <param name="label">Label da popolare</param>
        /// <param name="ticketKey">Numero ticket</param>
        /// <param name="jiraField">Nome campo Jira</param>
        public async Task PopulateLabelAsync(Label label, string ticketKey, string jiraField)
        {
            try
            {
                if (label == null || string.IsNullOrEmpty(ticketKey) || string.IsNullOrEmpty(jiraField))
                {
                    _logger.LogWarning("Parametri non validi per popolamento Label");
                    return;
                }

                _logger.LogDebug($"Popolamento Label per {ticketKey}.{jiraField}");

                // 1. Carica dati ticket
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket {ticketKey} non trovato");
                    SetLabelValue(label, "Ticket non trovato");
                    return;
                }

                // 2. Estrai il valore del campo
                var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);

                // 3. Popola la Label
                SetLabelValue(label, fieldValue);

                _logger.LogDebug($"Label popolata: {jiraField} = '{fieldValue}'");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore popolamento Label per {ticketKey}.{jiraField}", ex);
                SetLabelValue(label, $"Errore: {ex.Message}");
            }
        }

        /// <summary>
        /// Popola multiple Label con un singolo caricamento ticket
        /// </summary>
        /// <param name="ticketKey">Numero ticket</param>
        /// <param name="labelFieldMappings">Dizionario Label → Campo Jira</param>
        public async Task PopulateMultipleLabelsAsync(string ticketKey, Dictionary<Label, string> labelFieldMappings)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("TicketKey vuoto per popolamento multiplo Label");
                    ClearAllLabels(labelFieldMappings.Keys);
                    return;
                }

                _logger.LogInfo($"Popolamento multiplo Label per ticket {ticketKey} - {labelFieldMappings.Count} campi");

                // 1. Carica ticket una sola volta (ottimizzazione)
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket {ticketKey} non trovato");
                    SetAllLabelsValue(labelFieldMappings.Keys, "Ticket non trovato");
                    return;
                }

                // 2. Popola tutte le Label
                foreach (var mapping in labelFieldMappings)
                {
                    var label = mapping.Key;
                    var jiraField = mapping.Value;

                    try
                    {
                        var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);
                        SetLabelValue(label, fieldValue);

                        _logger.LogDebug($"Label popolata: {jiraField} = '{fieldValue}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore popolamento Label {jiraField}", ex);
                        SetLabelValue(label, $"Errore campo");
                    }
                }

                _logger.LogInfo($"Popolamento multiplo Label completato per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore popolamento multiplo Label per {ticketKey}", ex);
                SetAllLabelsValue(labelFieldMappings.Keys, $"Errore caricamento");
            }
        }

        /// <summary>
        /// Popola sia TextBox che Label in un'unica operazione
        /// </summary>
        /// <param name="ticketKey">Numero ticket</param>
        /// <param name="textBoxMappings">Dizionario TextBox → Campo Jira</param>
        /// <param name="labelMappings">Dizionario Label → Campo Jira</param>
        public async Task PopulateAllControlsAsync(string ticketKey,
            Dictionary<TextBox, string> textBoxMappings,
            Dictionary<Label, string> labelMappings)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("TicketKey vuoto per popolamento completo");
                    ClearAllTextBoxes(textBoxMappings.Keys);
                    ClearAllLabels(labelMappings.Keys);
                    return;
                }

                _logger.LogInfo($"Popolamento completo per ticket {ticketKey} - TextBox: {textBoxMappings.Count}, Label: {labelMappings.Count}");

                // 1. Carica ticket UNA SOLA VOLTA
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket {ticketKey} non trovato");
                    SetAllTextBoxesValue(textBoxMappings.Keys, "Ticket non trovato");
                    SetAllLabelsValue(labelMappings.Keys, "Ticket non trovato");
                    return;
                }

                // 2. Popola tutte le TextBox
                foreach (var mapping in textBoxMappings)
                {
                    var textBox = mapping.Key;
                    var jiraField = mapping.Value;

                    try
                    {
                        var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);
                        SetTextBoxValue(textBox, fieldValue);
                        _textBoxMappings[textBox] = jiraField;

                        _logger.LogDebug($"TextBox popolata: {jiraField} = '{fieldValue}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore popolamento TextBox {jiraField}", ex);
                        SetTextBoxValue(textBox, $"Errore campo");
                    }
                }

                // 3. Popola tutte le Label
                foreach (var mapping in labelMappings)
                {
                    var label = mapping.Key;
                    var jiraField = mapping.Value;

                    try
                    {
                        var fieldValue = ExtractFieldValue(ticket.RawData, jiraField);
                        SetLabelValue(label, fieldValue);

                        _logger.LogDebug($"Label popolata: {jiraField} = '{fieldValue}'");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Errore popolamento Label {jiraField}", ex);
                        SetLabelValue(label, $"Errore campo");
                    }
                }

                _logger.LogInfo($"Popolamento completo completato per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore popolamento completo per {ticketKey}", ex);
                SetAllTextBoxesValue(textBoxMappings.Keys, $"Errore caricamento");
                SetAllLabelsValue(labelMappings.Keys, $"Errore caricamento");
            }
        }

        #endregion

        #region Private Helper Methods for Labels

        /// <summary>
        /// Imposta valore in Label thread-safe
        /// </summary>
        private void SetLabelValue(Label label, string value)
        {
            if (label.InvokeRequired)
            {
                label.Invoke(() => SetLabelValue(label, value));
                return;
            }

            label.Text = value ?? "-";
        }

        /// <summary>
        /// Pulisce una Label
        /// </summary>
        private void ClearLabel(Label label)
        {
            SetLabelValue(label, "-");
        }

        /// <summary>
        /// Pulisce multiple Label
        /// </summary>
        private void ClearAllLabels(IEnumerable<Label> labels)
        {
            foreach (var label in labels)
            {
                ClearLabel(label);
            }
        }

        /// <summary>
        /// Imposta stesso valore in multiple Label
        /// </summary>
        private void SetAllLabelsValue(IEnumerable<Label> labels, string value)
        {
            foreach (var label in labels)
            {
                SetLabelValue(label, value);
            }
        }

        #endregion


    }
}