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
                // Mappa i campi dal formato API a quello del JiraFieldExtractor
                var fieldName = MapJiraFieldToExtractorField(jiraField);

                // Usa il metodo esistente di JiraFieldExtractor
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

        /// <summary>
        /// Mappa i nomi dei campi API ai nomi del JiraFieldExtractor
        /// </summary>
        private string MapJiraFieldToExtractorField(string jiraField)
        {
            return jiraField switch
            {
                "reporter" => "Reporter",
                "assignee" => "Assignee",
                "status" => "Status",
                "priority" => "Priority",
                "issuetype" => "Type",
                "summary" => "Summary",
                "description" => "Description",
                "created" => "Created",
                "updated" => "Updated",
                "resolutiondate" => "ResolutionDate",
                "customfield_10117" => "Cliente",
                "customfield_10113" => "Area",
                "customfield_10114" => "Applicativo",
                "customfield_10103" => "ClientePartner",
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
    }
}