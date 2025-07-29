using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;
using JiraTicketManager.Business;
using JiraTicketManager.Data;
using JiraTicketManager.Services;
using JiraTicketManager.UI.Managers;
using JiraTicketManager.Helpers;




namespace JiraTicketManager.Forms
{
    public partial class TicketDetailForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraDataService _dataService;
        private readonly TextBoxManager _textBoxManager;
        private ComboBoxManager _comboBoxManager;

        private string _currentTicketKey;
        private bool _isLoading = false;

        private readonly EmailTemplateService _emailTemplateService;
        private readonly OutlookIntegrationService _outlookService;

        // === STATO PIANIFICAZIONE ===
        private bool _isPlanningEnabled = false;
        private string _currentEmailPreview = "";
        private string _currentHtmlContent = "";

        private readonly WindowsToastService _toastService;

        #endregion

        #region Constructor

        public TicketDetailForm()
        {
            InitializeComponent();

            // Inizializza servizi esistenti
            _logger = LoggingService.CreateForComponent("TicketDetailForm");

            // Usa i servizi esistenti della MainForm
            var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _dataService = new JiraDataService(apiService);
            _textBoxManager = new TextBoxManager(_dataService);

            // === INIZIALIZZA SERVIZI PIANIFICAZIONE (READONLY) ===
            _emailTemplateService = new EmailTemplateService();
            _outlookService = new OutlookIntegrationService();
            _toastService = WindowsToastService.CreateDefault();

            // Setup iniziale
            SetupForm();

            _logger.LogInfo("TicketDetailForm inizializzata con servizi pianificazione");
        }




        #endregion

        #region Public Methods


        // <summary>
        /// Carica e visualizza un ticket specifico
        /// </summary>
        /// <param name="ticketKey">Numero ticket (es: CC-12345)</param>
        public async Task LoadTicketAsync(string ticketKey)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("Tentativo caricamento ticket con key vuota");
                    return;
                }

                _logger.LogInfo($"Caricamento ticket: {ticketKey}");

                SetLoadingState(true);
                _currentTicketKey = ticketKey;
                this.Text = $"Caricamento Ticket {ticketKey}...";

                // Inizializza ComboBox
                await InitializeUserPickerComboBox(cmbConsulente, JiraFieldType.Consulente, "-- Tutti Consulenti --");
                await InitializeUserPickerComboBox(cmbPM, JiraFieldType.PM, "-- Tutti PM --");
                await InitializeUserPickerComboBox(cmbCommerciale, JiraFieldType.Commerciale, "-- Tutti Commerciali --");

                // Popola tutti i controlli (codice esistente)
                var textBoxMappings = CreateTextBoxMappings();
                var labelMappings = CreateLabelMappings();
                await _textBoxManager.PopulateAllControlsAsync(ticketKey, textBoxMappings, labelMappings);

                SetResponsabileFromArea();


                await UpdateHeaderInfo(ticketKey);
                this.Text = $"Dettaglio Ticket - {ticketKey}";

                _logger.LogInfo($"Ticket {ticketKey} caricato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento ticket {ticketKey}", ex);
                MessageBox.Show($"Errore durante il caricamento del ticket {ticketKey}:\n\n{ex.Message}",
                    "Errore Caricamento Ticket", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Text = $"Errore Caricamento - {ticketKey}";
            }
            finally
            {
                SetLoadingState(false);
            }
        }



        /// <summary>
        /// Crea la mappatura completa TextBox → Campo Jira
        /// </summary>

        private Dictionary<TextBox, string> CreateTextBoxMappings()
        {
            return new Dictionary<TextBox, string>
            {
                // === LEFT PANEL - CONTACT (TESTATO E FUNZIONANTE) ===
                [txtRichiedente] = "reporter",              // ✅ "Daniela Paltrinieri"
                [txtEmail] = "reporter.emailAddress",       // ✅ "daniela.paltrinieri@libero.it"  
                [txtTelefono] = "customfield_10074",        // ✅ "0521 344572 - 338 4412342"

                // === LEFT PANEL - TIMELINE ===
                [txtDataCreazione] = "created",             // ✅ Date format
                [txtDataAggiornamento] = "updated",         // ✅ Date format 
                [txtDataCompletamento] = "resolutiondate", // ✅ Date format (può essere null)

                // === RIGHT PANEL - ORGANIZATION (TESTATO) ===
                [txtCliente] = "customfield_10117",         // ✅ "UNIONE PEDEMONTANA PARMENSE"
                [txtArea] = "customfield_10113",            // ✅ "Sistema Informativo Territoriale"
                [txtApplicativo] = "customfield_10114",     // ✅ "Sistema Informativo Territoriale -> NewSed.Net"

                [txtClientePartner] = "customfield_10103",  // ❌ Spesso NULL (facoltativo)

                // === RIGHT PANEL - TEAM PLANNING ===


                [txtWBS] = "customfield_10096",             // ❌ Spesso NULL (facoltativo)

                // === NUOVI CAMPI PIANIFICAZIONE ===
                [txtDataIntervento] = "customfield_10116",  // 🆕 Data Intervento 
                [txtOraIntervento] = "customfield_10133",   // 🆕 Ora Intervento 
                [txtEffort] = "customfield_10089",          // 🆕 Effort (campo corretto!)
                                                            // txtWBS già presente sopra

                // === CENTER PANEL - DESCRIPTION (TESTATO E FUNZIONANTE) ===
                [txtDescrizione] = "description"            // ✅ "Buongiorno dovendo modificare il tracciato..."
            };
        }

        // <summary>
        /// Crea la mappatura Label → Campo Jira ***
        /// </summary>
        private Dictionary<Label, string> CreateLabelMappings()
        {
            return new Dictionary<Label, string>
            {
                // === HEADER METADATA LABELS ===
                [lblStatus] = "status",           // "Inoltrato (Terzo Livello)"
                [lblTipo] = "issuetype",          // "[System] Service request"
                [lblPriorita] = "priority",       // "Media"
                [lblAssegnatario] = "assignee",   // "Rosario Romano"

                // *** NUOVO: Summary nell'header (se esiste lblTicketSummary) ***
                [lblTicketSummary] = "summary"    // "Aci_Vesta"
            };
        }


        /// <summary>
        /// Versione sincrona per compatibilità (sconsigliata)
        /// </summary>
        /// <param name="ticketKey">Numero ticket</param>
        public void LoadTicket(string ticketKey)
        {
            // Avvia caricamento asincrono
            _ = LoadTicketAsync(ticketKey);
        }

        #endregion

        #region Private Methods - Setup

        /// <summary>
        /// Configurazione iniziale della form
        /// </summary>
        private void SetupForm()
        {
            try
            {
                // Imposta stato iniziale
                ClearAllFields();

                // Setup event handlers (se necessari)
                this.Load += OnFormLoad;
                this.FormClosing += OnFormClosing;
                SetupPlanningEventHandlers();




                _logger.LogDebug("Form setup completato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup form", ex);
            }
        }

        private void SetupPlanningEventHandlers()
        {
            try
            {
                // === EVENT HANDLERS PIANIFICAZIONE ===
                btnPianifica.Click += OnPianificaClick;
                cmbTipoPianificazione.SelectedIndexChanged += OnTemplateChanged;

                // === SETUP TEMPLATE COMBOBOX ===
                SetupTemplateComboBox();

                // === VERIFICA OUTLOOK DISPONIBILITÀ ===
                _ = CheckOutlookAvailabilityAsync();

                _logger.LogInfo("Event handlers pianificazione configurati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup planning event handlers", ex);
            }
        }


        /// <summary>
        /// Crea la mappatura completa TextBox → Campo Jira
        /// </summary>
        private Dictionary<TextBox, string> CreateFieldMappings()
        {
            return new Dictionary<TextBox, string>
            {
                // === LEFT PANEL - CONTACT ===
                [txtRichiedente] = "reporter",
                [txtEmail] = "customfield_10136",          // Email Richiedente
                [txtTelefono] = "customfield_10074",       // Telefono

                // === LEFT PANEL - TIMELINE ===
                [txtDataCreazione] = "created",
                [txtDataAggiornamento] = "updated",
                [txtDataCompletamento] = "resolutiondate",

                // === RIGHT PANEL - ORGANIZATION ===
                [txtCliente] = "customfield_10117",        // Cliente
                [txtArea] = "customfield_10113",           // Area
                [txtApplicativo] = "customfield_10114",    // Applicativo

                [txtClientePartner] = "customfield_10103", // Cliente Partner

                // === RIGHT PANEL - TEAM PLANNING ===


                [txtWBS] = "customfield_10096",            // WBS

                // === CENTER PANEL - DESCRIPTION ===
                [txtDescrizione] = "description"
            };
        }

        #endregion

        #region Private Methods - UI Updates

        /// <summary>
        /// Aggiorna le informazioni nell'header
        /// </summary>
        private async Task UpdateHeaderInfo(string ticketKey)
        {
            try
            {
                // Il ticketKey viene già aggiornato dal mapping delle Label
                // Aggiorna solo il lblTicketKey
                if (lblTicketKey != null)
                {
                    lblTicketKey.Text = $"[{ticketKey}]";
                }

                _logger.LogDebug($"Header aggiornato per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiornamento header per {ticketKey}", ex);
            }
        }


        /// <summary>
        /// Aggiorna il badge di status
        /// </summary>
        private void UpdateStatusBadge(string status)
        {
            lblStatus.Text = status ?? "Unknown";

            // Colori dinamici basati su status
            var statusColors = new Dictionary<string, (Color back, Color fore)>
            {
                ["Aperto"] = (Color.FromArgb(220, 53, 69), Color.White),     // Rosso
                ["In Corso"] = (Color.FromArgb(217, 119, 6), Color.White),   // Arancione  
                ["Risolto"] = (Color.FromArgb(40, 167, 69), Color.White),    // Verde
                ["Chiuso"] = (Color.FromArgb(108, 117, 125), Color.White),   // Grigio
                ["In Attesa"] = (Color.FromArgb(23, 162, 184), Color.White)  // Blu
            };

            if (statusColors.TryGetValue(status ?? "", out var colors))
            {
                lblStatus.BackColor = colors.back;
                lblStatus.ForeColor = colors.fore;
            }
        }

        /// <summary>
        /// Aggiorna il badge di tipo
        /// </summary>
        private void UpdateTypeBadge(string issueType)
        {
            var icon = GetIssueTypeIcon(issueType);
            lblTipo.Text = $"{icon} {issueType ?? "Unknown"}";
        }

        /// <summary>
        /// Aggiorna il badge di priorità
        /// </summary>
        private void UpdatePriorityBadge(string priority)
        {
            var icon = GetPriorityIcon(priority);
            lblPriorita.Text = $"{icon} {priority ?? "Normal"}";

            // Colore dinamico priorità
            var priorityColors = new Dictionary<string, Color>
            {
                ["Highest"] = Color.FromArgb(220, 53, 69),   // Rosso scuro
                ["High"] = Color.FromArgb(255, 87, 34),      // Arancione
                ["Medium"] = Color.FromArgb(255, 193, 7),    // Giallo
                ["Low"] = Color.FromArgb(40, 167, 69),       // Verde
                ["Lowest"] = Color.FromArgb(108, 117, 125)   // Grigio
            };

            if (priorityColors.TryGetValue(priority ?? "", out var color))
            {
                lblPriorita.ForeColor = color;
            }
        }

        /// <summary>
        /// Aggiorna il badge assegnatario
        /// </summary>
        private void UpdateAssigneeBadge(string assignee)
        {
            var displayText = string.IsNullOrEmpty(assignee) ? "Non assegnato" : assignee;
            lblAssegnatario.Text = $"👤 {displayText}";

            // Colore diverso se non assegnato
            if (string.IsNullOrEmpty(assignee))
            {
                lblAssegnatario.ForeColor = Color.FromArgb(220, 53, 69); // Rosso
            }
            else
            {
                lblAssegnatario.ForeColor = Color.FromArgb(73, 80, 87);   // Normale
            }
        }

        /// <summary>
        /// Imposta stato di caricamento
        /// </summary>
        private void SetLoadingState(bool isLoading)
        {
            _isLoading = isLoading;

            // Disabilita controlli durante caricamento
            pnlLeft.Enabled = !isLoading;
            pnlCenter.Enabled = !isLoading;
            pnlRight.Enabled = !isLoading;

            // Mostra cursore di attesa
            this.Cursor = isLoading ? Cursors.WaitCursor : Cursors.Default;

            // Aggiorna status bar se presente
            if (tslConnection != null)
            {
                tslConnection.Text = isLoading ? "🔄 Caricamento..." : "🟢 Connesso a Jira";
            }
        }

        /// <summary>
        /// Pulisce tutti i campi
        /// </summary>
        private void ClearAllFields()
        {
            try
            {
                // Pulisce TextBox
                var textBoxMappings = CreateTextBoxMappings();
                foreach (var textBox in textBoxMappings.Keys)
                {
                    if (textBox != null)
                        textBox.Text = "-";
                }

                // *** NUOVO: Pulisce Label ***
                var labelMappings = CreateLabelMappings();
                foreach (var label in labelMappings.Keys)
                {
                    if (label != null)
                        label.Text = "-";
                }

                // Reset header
                if (lblTicketKey != null) lblTicketKey.Text = "[CC-00000]";

                _logger.LogDebug("Tutti i campi puliti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia campi", ex);
            }
        }

        #endregion

        #region Private Methods - Helpers

        // <summary>
        /// Calcola e imposta il responsabile basato sull'area
        /// </summary>
        private void SetResponsabileFromArea()
        {
            try
            {
                var area = txtArea.Text;
                var responsabile = ResponsabileHelper.DeterminaResponsabile(area);

                if (string.IsNullOrWhiteSpace(responsabile))
                    responsabile = "[Campo non disponibile]";

                txtResponsabile.Text = responsabile;

                _logger?.LogDebug($"Responsabile determinato per area '{area}': {responsabile}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore determinazione responsabile: {ex.Message}", ex);
                txtResponsabile.Text = "[Errore calcolo responsabile]";
            }
        }

        /// <summary>
        /// Ottiene icona per tipo issue
        /// </summary>
        private string GetIssueTypeIcon(string issueType)
        {
            return issueType?.ToLower() switch
            {
                "bug" => "🐛",
                "task" => "📋",
                "story" => "📖",
                "epic" => "🎯",
                "incident" => "🚨",
                _ => "📄"
            };
        }

        /// <summary>
        /// Ottiene icona per priorità
        /// </summary>
        private string GetPriorityIcon(string priority)
        {
            return priority?.ToLower() switch
            {
                "highest" => "🔴",
                "high" => "🟠",
                "medium" => "🟡",
                "low" => "🟢",
                "lowest" => "⚪",
                _ => "⚫"
            };
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Event handler caricamento form
        /// </summary>
        private void OnFormLoad(object sender, EventArgs e)
        {
            _logger.LogDebug("TicketDetailForm caricata");
        }

        /// <summary>
        /// Event handler chiusura form
        /// </summary>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            // Cleanup risorse prima della chiusura
            CleanupResources();
            _logger.LogDebug("TicketDetailForm in chiusura");
        }

        #endregion

        #region Private Methods - Cleanup

        /// <summary>
        /// Cleanup risorse custom quando la form si chiude
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _textBoxManager?.Dispose();
                _logger?.LogDebug("Risorse TicketDetailForm rilasciate");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore cleanup risorse", ex);
            }
        }

        #endregion

        #region Popolamento ComboBox

        /// <summary>
        /// Inizializza e popola la ComboBox Consulente usando JiraFieldType.Consulente
        /// </summary>
        private async Task InitializeUserPickerComboBox(ComboBox comboBox, JiraFieldType fieldType, string defaultText)
        {
            try
            {
                if (_comboBoxManager == null)
                {
                    _comboBoxManager = new ComboBoxManager(_dataService);
                }

                // USA IL METODO CHE CARICA E IMPOSTA IL VALORE CORRENTE
                await _comboBoxManager.LoadAsyncWithCurrentValue(
                    comboBox,
                    fieldType,
                    defaultText,
                    progress: null,
                    _currentTicketKey
                );

                _logger?.LogInfo($"✅ {fieldType} inizializzato con successo");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore inizializzazione ComboBox {fieldType}: {ex.Message}", ex);

                // Fallback generico
                if (comboBox != null)
                {
                    comboBox.Items.Clear();
                    comboBox.Items.Add(defaultText);
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        private async Task InitializeAllUserPickerComboBoxes()
        {
            try
            {
                _logger?.LogInfo("🔄 Inizializzazione tutte le ComboBox User Picker...");

                // Definisci tutte le ComboBox user picker
                var userPickerComboBoxes = new[]
                {
            (comboBox: cmbConsulente, fieldType: JiraFieldType.Consulente, defaultText: "-- Tutti Consulenti --"),
            (comboBox: cmbPM, fieldType: JiraFieldType.PM, defaultText: "-- Tutti PM --"),
            (comboBox: cmbCommerciale, fieldType: JiraFieldType.Commerciale, defaultText: "-- Tutti Commerciali --")
        };

                // Carica tutte in parallelo per performance
                var tasks = userPickerComboBoxes.Select(combo =>
                    InitializeUserPickerComboBox(combo.comboBox, combo.fieldType, combo.defaultText)
                );

                await Task.WhenAll(tasks);

                _logger?.LogInfo("✅ Tutte le ComboBox User Picker inizializzate");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore inizializzazione ComboBox User Picker: {ex.Message}", ex);
            }
        }




        /// <summary>
        /// Imposta il consulente dal ticket corrente
        /// </summary>
        private async Task SetConsulenteFromCurrentTicket()
        {
            try
            {
                if (cmbConsulente == null || string.IsNullOrEmpty(_currentTicketKey))
                    return;

                _logger?.LogDebug($"🔍 Impostazione consulente per ticket: {_currentTicketKey}");

                var ticket = await _dataService.GetTicketAsync(_currentTicketKey);
                if (ticket?.RawData == null)
                {
                    cmbConsulente.SelectedIndex = 0;
                    return;
                }

                var consulenteValue = ExtractConsulenteFromTicket(ticket.RawData);

                if (string.IsNullOrEmpty(consulenteValue))
                {
                    cmbConsulente.SelectedIndex = 0;
                    return;
                }

                // Cerca matching nella ComboBox
                var items = cmbConsulente.Items.Cast<string>().ToList();
                var matchingItem = items.FirstOrDefault(item =>
                    item.Equals(consulenteValue, StringComparison.OrdinalIgnoreCase) ||
                    item.Contains(consulenteValue, StringComparison.OrdinalIgnoreCase)
                );

                if (!string.IsNullOrEmpty(matchingItem))
                {
                    cmbConsulente.SelectedItem = matchingItem;
                    _logger?.LogDebug($"✅ Consulente impostato: {matchingItem}");
                }
                else
                {
                    cmbConsulente.SelectedIndex = 0;
                    _logger?.LogDebug($"⚠️ Consulente '{consulenteValue}' non trovato");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore impostazione consulente: {ex.Message}", ex);
                if (cmbConsulente?.Items.Count > 0)
                    cmbConsulente.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Estrae consulente dal ticket
        /// </summary>
        private string ExtractConsulenteFromTicket(Newtonsoft.Json.Linq.JToken rawData)
        {
            try
            {
                var fields = rawData["fields"];
                var consulteneField = fields?["customfield_10238"];

                if (consulteneField == null || consulteneField.Type == Newtonsoft.Json.Linq.JTokenType.Null)
                    return null;

                if (consulteneField.Type == Newtonsoft.Json.Linq.JTokenType.String)
                    return consulteneField.ToString();

                if (consulteneField.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    return consulteneField["value"]?.ToString() ??
                           consulteneField["displayName"]?.ToString() ??
                           consulteneField["emailAddress"]?.ToString();
                }

                return consulteneField.ToString();
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore estrazione consulente: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Fallback ai valori hardcoded
        /// </summary>
        private void UseFallbackValues()
        {
            try
            {
                if (cmbConsulente != null && cmbConsulente.Items.Count <= 1)
                {
                    _logger?.LogInfo("🔄 Fallback a valori hardcoded (ComboBox vuota)...");
                    cmbConsulente.Items.Clear();
                    cmbConsulente.Items.Add("-- Tutti Consulenti --");
                    cmbConsulente.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore fallback: {ex.Message}", ex);
            }
        }

        #endregion

        #region METODI HELPER PIANIFICAZIONE

        /// <summary>
        /// Configura la ComboBox dei template con i tipi disponibili
        /// </summary>
        private void SetupTemplateComboBox()
        {
            try
            {
                _logger.LogInfo("Setup template ComboBox");

                // Pulisci e popola con i template disponibili
                cmbTipoPianificazione.Items.Clear();
                cmbTipoPianificazione.Items.Add("Seleziona tipo pianificazione...");

                var availableTemplates = EmailTemplateService.GetAvailableTemplates();
                foreach (var template in availableTemplates)
                {
                    cmbTipoPianificazione.Items.Add(template.Value);
                }

                cmbTipoPianificazione.SelectedIndex = 0;
                _logger.LogInfo($"Template ComboBox configurata con {availableTemplates.Count} template");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup template ComboBox", ex);
            }
        }

        /// <summary>
        /// Ottiene il tipo di template selezionato
        /// </summary>
        private EmailTemplateService.TemplateType? GetSelectedTemplateType()
        {
            if (cmbTipoPianificazione.SelectedIndex <= 0)
                return null;

            // Mappa indice ComboBox a TemplateType
            return cmbTipoPianificazione.SelectedIndex switch
            {
                1 => EmailTemplateService.TemplateType.SingleIntervention,
                2 => EmailTemplateService.TemplateType.MultipleInterventions,
                3 => EmailTemplateService.TemplateType.ToBeAgreed,
                _ => null
            };
        }

        /// <summary>
        /// Ottiene il valore selezionato da una ComboBox in modo sicuro
        /// </summary>
        private string GetSelectedComboValue(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem == null || comboBox.SelectedIndex <= 0)
                return "";

            return comboBox.SelectedItem.ToString();
        }

        /// <summary>
        /// Mostra messaggio di errore usando il sistema Toast integrato
        /// </summary>
        private void ShowError(string title, string message)
        {
            _toastService.ShowError(title, message);
            _logger.LogError($"Errore pianificazione mostrato: {title} - {message}");
        }

        /// <summary>
        /// Mostra messaggio di successo usando il sistema Toast integrato
        /// </summary>
        private void ShowSuccess(string title, string message)
        {
            _toastService.ShowSuccess(title, message);
            _logger.LogInfo($"Successo pianificazione: {title} - {message}");
        }

        /// <summary>
        /// Mostra messaggio informativo usando il sistema Toast integrato
        /// </summary>
        private void ShowInfo(string title, string message)
        {
            _toastService.ShowInfo(title, message);
            _logger.LogInfo($"Info pianificazione: {title} - {message}");
        }

        /// <summary>
        /// Verifica se un ComboBox ha una selezione valida
        /// </summary>
        private bool HasValidSelection(ComboBox comboBox)
        {
            return comboBox != null && comboBox.SelectedIndex > 0 && comboBox.SelectedItem != null;
        }

        /// <summary>
        /// Pulisce e formatta una stringa di input
        /// </summary>
        private string CleanInputString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            return input.Trim()
                        .Replace("\r\n", " ")
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("  ", " ");
        }

        /// <summary>
        /// Valida se un campo di testo ha contenuto valido
        /// </summary>
        private bool IsValidTextInput(TextBox textBox)
        {
            return textBox != null && !string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text.Trim().Length > 0;
        }

        /// <summary>
        /// Ottiene il testo sicuro da un TextBox
        /// </summary>
        private string GetSafeTextValue(TextBox textBox)
        {
            if (textBox?.Text == null)
                return "";

            return CleanInputString(textBox.Text);
        }

        /// <summary>
        /// Verifica se tutti i campi obbligatori sono compilati
        /// </summary>
        private bool AreRequiredFieldsValid()
        {
            var requiredFields = new[]
            {
        (txtCliente, "Cliente"),
        (txtDescrizione, "Descrizione")
    };

            foreach (var (textBox, fieldName) in requiredFields)
            {
                if (!IsValidTextInput(textBox))
                {
                    ShowError("Campo Obbligatorio", $"Il campo '{fieldName}' è obbligatorio");
                    textBox?.Focus();
                    return false;
                }
            }

            var requiredCombos = new[]
            {
        (cmbConsulente, "Consulente"),
        (cmbTipoPianificazione, "Tipo Pianificazione")
    };

            foreach (var (comboBox, fieldName) in requiredCombos)
            {
                if (!HasValidSelection(comboBox))
                {
                    ShowError("Campo Obbligatorio", $"Selezionare un valore per '{fieldName}'");
                    comboBox?.Focus();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resetta lo stato della pianificazione
        /// </summary>
        private void ResetPlanningState()
        {
            _isPlanningEnabled = false;
            _currentEmailPreview = "";
            _currentHtmlContent = "";

            btnPianifica.Enabled = false;
            btnPianifica.Text = "📅 Pianifica";

            if (cmbTipoPianificazione != null)
                cmbTipoPianificazione.SelectedIndex = 0;
        }

        /// <summary>
        /// Abilita/disabilita i controlli di pianificazione
        /// </summary>
        private void SetPlanningControlsEnabled(bool enabled)
        {
            var planningControls = new Control[]
            {
        cmbTipoPianificazione,
        txtDataIntervento,
        txtOraIntervento,
        txtEffort,
        cmbConsulente,
        cmbPM,
        cmbCommerciale
            };

            foreach (var control in planningControls)
            {
                if (control != null)
                    control.Enabled = enabled;
            }
        }

        /// <summary>
        /// Formatta la data per la visualizzazione
        /// </summary>
        private string FormatDateForDisplay(string dateInput)
        {
            if (string.IsNullOrWhiteSpace(dateInput))
                return "Data da definire";

            // Prova a parsare e riformattare la data
            if (DateTime.TryParse(dateInput, out DateTime parsedDate))
            {
                return parsedDate.ToString("dd/MM/yyyy");
            }

            return CleanInputString(dateInput);
        }

        /// <summary>
        /// Formatta l'ora per la visualizzazione
        /// </summary>
        private string FormatTimeForDisplay(string timeInput)
        {
            if (string.IsNullOrWhiteSpace(timeInput))
                return "Ora da definire";

            // Prova a parsare e riformattare l'ora
            if (TimeSpan.TryParse(timeInput, out TimeSpan parsedTime))
            {
                return parsedTime.ToString(@"hh\:mm");
            }

            return CleanInputString(timeInput);
        }


        /// <summary>
        /// Verifica se Outlook è disponibile nel sistema
        /// </summary>
        private async Task CheckOutlookAvailabilityAsync()
        {
            try
            {
                var isAvailable = await _outlookService.IsOutlookAvailableAsync();
                _logger.LogInfo($"Outlook disponibile: {isAvailable}");

                if (!isAvailable)
                {
                    _logger.LogWarning("Outlook non disponibile - pianificazione limitata");
                    // Potresti voler disabilitare o avvisare l'utente
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore verifica Outlook", ex);
            }
        }

        // <summary>
        /// Event handler per cambio template
        /// </summary>
        private void OnTemplateChanged(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo($"Template cambiato - Index: {cmbTipoPianificazione.SelectedIndex}");

                // Abilita/disabilita pianificazione basato su selezione
                _isPlanningEnabled = cmbTipoPianificazione.SelectedIndex > 0;
                btnPianifica.Enabled = _isPlanningEnabled;

                if (_isPlanningEnabled)
                {
                    // Genera anteprima se i dati sono disponibili
                    UpdateEmailPreview();
                }
                else
                {
                    // Pulisci anteprima
                    _currentEmailPreview = "";
                    _currentHtmlContent = "";
                }

                _logger.LogInfo($"Pianificazione abilitata: {_isPlanningEnabled}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore cambio template", ex);
            }
        }

        /// <summary>
        /// Aggiorna l'anteprima email (se hai un controllo per mostrarla)
        /// </summary>
        private void UpdateEmailPreview()
        {
            try
            {
                if (!_isPlanningEnabled)
                    return;

                var templateType = GetSelectedTemplateType();
                if (templateType == null)
                    return;

                // Genera anteprima con dati correnti
                _currentEmailPreview = _emailTemplateService.GenerateTextPreview(
                    templateType.Value,
                    GetSelectedComboValue(cmbConsulente) ?? "Consulente",
                    txtDataIntervento?.Text ?? "Data da definire",
                    txtOraIntervento?.Text ?? "Ora da definire",
                    txtTelefono?.Text ?? "Telefono"
                );

                _logger.LogInfo($"Anteprima email aggiornata - {_currentEmailPreview.Length} caratteri");

                // Se hai un controllo per mostrare l'anteprima, aggiornalo qui
                // txtAnteprimaEmail.Text = _currentEmailPreview;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento anteprima", ex);
            }
        }

        /// <summary>
        /// Event handler principale per il pulsante Pianifica
        /// </summary>
        private async void OnPianificaClick(object sender, EventArgs e)
        {
            TestOutlookDiagnosticAdvanced();

            try
            {
                _logger.LogInfo("=== INIZIO PIANIFICAZIONE ===");

                // Disabilita pulsante durante elaborazione
                btnPianifica.Enabled = false;
                btnPianifica.Text = "⏳ Elaborazione...";
                this.Cursor = Cursors.WaitCursor;

                // Validazione dati
                if (!ValidatePlanningData())
                {
                    return;
                }

                // Raccoglie dati dai campi UI
                var planningData = CollectPlanningTicketData();  
                if (planningData == null)
                {
                    return;
                }

                // Genera contenuto email
                var emailContent = GenerateEmailContent(planningData);
                if (string.IsNullOrWhiteSpace(emailContent.HtmlBody))
                {
                    //_toastService.ShowError("Errore nella generazione del contenuto email");
                    return;
                }

                // Prepara dati email per Outlook
                var emailData = PrepareEmailData(planningData, emailContent.HtmlBody);

                // Apre Outlook con email precompilata
                var success = await _outlookService.OpenEmailAsync(emailData);

                if (success)
                {
                    _logger.LogInfo("Email Outlook aperta con successo");
                    //_toastService.ShowError("Email di pianificazione preparata e aperta in Outlook");

                    // Genera e aggiunge commento Jira
                    await AddJiraCommentAsync(planningData, emailContent.TextBody);
                }
                else
                {
                   // ShowError("Errore nell'apertura di Outlook. Verificare che Outlook sia installato e funzionante.");
                }

                _logger.LogInfo("=== FINE PIANIFICAZIONE ===");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore durante pianificazione", ex);
                //ShowError($"Errore durante la pianificazione: {ex.Message}");
            }
            finally
            {
                // Ripristina stato UI
                btnPianifica.Enabled = true;
                btnPianifica.Text = "📅 Pianifica";
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Aggiunge commento Jira con dettagli della pianificazione
        /// </summary>
        private async Task AddJiraCommentAsync(PlanningTicketData data, string textContent)
        {
            try
            {
                _logger.LogInfo("Aggiunta commento Jira");

                // Genera commento usando EmailTemplateService
                var comment = _emailTemplateService.GenerateJiraComment(
                    data.TemplateType,
                    data.ConsultantName,
                    data.InterventionDate,
                    data.InterventionTime,
                    data.ClientPhone,
                    data.ResponsiblePerson,
                    data.ProjectManager,
                    data.Commercial,
                    data.ReporterEmail,
                    data.TicketKey,
                    data.ClientName,
                    data.Description,
                    data.WBS
                );

                // Aggiunge commento tramite API (da implementare se necessario)
                // await _dataService.AddCommentAsync(data.TicketKey, comment);

                _logger.LogInfo($"Commento Jira generato - Lunghezza: {comment.Length}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiunta commento Jira", ex);
                // Non bloccare il flusso se il commento fallisce
            }
        }

        /// <summary>
        /// Genera il contenuto email usando EmailTemplateService
        /// </summary>
        private (string HtmlBody, string TextBody) GenerateEmailContent(PlanningTicketData data)
        {
            try
            {
                _logger.LogInfo($"Generazione contenuto email - Template: {data.TemplateType}");

                // Genera HTML per email
                var htmlContent = _emailTemplateService.GenerateHtmlContent(
                    data.TemplateType,
                    data.ConsultantName,
                    data.InterventionDate,
                    data.InterventionTime,
                    data.ClientPhone
                );

                // Genera testo per anteprima/commento
                var textContent = _emailTemplateService.GenerateTextPreview(
                    data.TemplateType,
                    data.ConsultantName,
                    data.InterventionDate,
                    data.InterventionTime,
                    data.ClientPhone
                );

                _logger.LogInfo($"Contenuto generato - HTML: {htmlContent.Length} char, Text: {textContent.Length} char");
                return (htmlContent, textContent);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generazione contenuto", ex);
                return ("", "");
            }
        }

        private OutlookIntegrationService.EmailData PrepareEmailData(PlanningTicketData data, string htmlContent)
        {
            try
            {
                _logger.LogInfo("Preparazione dati email per Outlook");

                // Usa il metodo helper di OutlookIntegrationService
                var emailData = OutlookIntegrationService.PrepareEmailFromTicketData(
                    data.ClientName,
                    data.TicketKey,
                    data.Description,
                    data.WBS,
                    data.ReporterEmail,
                    data.ConsultantName,
                    data.ResponsiblePerson,
                    data.ProjectManager,
                    data.Commercial,
                    htmlContent
                );

                _logger.LogInfo($"Email data preparata - To: {emailData.To}, CC: {emailData.Cc}");
                return emailData;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore preparazione email data", ex);
                throw;
            }
        }

        // <summary>
        /// Raccoglie i dati dai campi UI per la pianificazione
        /// </summary>
        private PlanningTicketData CollectPlanningTicketData()
        {
            try
            {
                _logger.LogInfo("Raccolta dati pianificazione");

                var templateType = GetSelectedTemplateType();
                if (templateType == null)
                {
                    _logger.LogError("Template type non valido");
                    return null;
                }

                var data = new PlanningTicketData
                {
                    TicketKey = _currentTicketKey,
                    TemplateType = templateType.Value,

                    // Dati base ticket
                    ClientName = txtCliente?.Text?.Trim() ?? "",
                    Description = txtDescrizione?.Text?.Trim() ?? "",

                    // Dati intervento
                    ConsultantName = GetSelectedComboValue(cmbConsulente),
                    InterventionDate = txtDataIntervento?.Text?.Trim() ?? "",
                    InterventionTime = txtOraIntervento?.Text?.Trim() ?? "",
                    ClientPhone = txtTelefono?.Text?.Trim() ?? "",

                    // Dati team
                    ProjectManager = GetSelectedComboValue(cmbPM),
                    Commercial = GetSelectedComboValue(cmbCommerciale),
                    ResponsiblePerson = txtResponsabile?.Text?.Trim() ?? "",

                    // Dati organizzazione
                    WBS = txtWBS?.Text?.Trim() ?? "",
                    Area = txtArea?.Text?.Trim() ?? "",
                    ReporterEmail = txtEmail?.Text?.Trim() ?? ""
                };

                _logger.LogInfo($"Dati raccolti per template: {data.TemplateType}");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore raccolta dati", ex);
                return null;
            }
        }

        /// <summary>
        /// Valida i dati necessari per la pianificazione
        /// </summary>
        private bool ValidatePlanningData()
        {
            try
            {
                _logger.LogInfo("Validazione dati pianificazione");

                // Verifica template selezionato
                if (cmbTipoPianificazione.SelectedIndex <= 0)
                {
                    ShowError("Pianificazione", "Selezionare un tipo di pianificazione");
                    return false;
                }

                // Verifica ticket caricato
                if (string.IsNullOrWhiteSpace(_currentTicketKey))
                {
                    ShowError("Pianificazione", "Nessun ticket caricato");
                    return false;
                }

                // Verifica campi obbligatori
                if (string.IsNullOrWhiteSpace(txtCliente?.Text))
                {
                    ShowError("Pianificazione", "Cliente non disponibile");
                    return false;
                }

                // Verifica consulente selezionato
                if (cmbConsulente?.SelectedIndex <= 0)
                {
                    ShowError("Pianificazione", "Selezionare un consulente");
                    return false;
                }

                _logger.LogInfo("Validazione completata con successo");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore validazione dati", ex);
                ShowError("Errore Validazione", "Errore durante la validazione dei dati");
                return false;
            }
        }

        #endregion


        #region MODELLO DATI PIANIFICAZIONE

        /// <summary>
        /// Modello per i dati della pianificazione ticket
        /// </summary>
        private class PlanningTicketData
        {
            public string TicketKey { get; set; } = "";
            public EmailTemplateService.TemplateType TemplateType { get; set; }

            // Dati base ticket
            public string ClientName { get; set; } = "";
            public string Description { get; set; } = "";

            // Dati intervento
            public string ConsultantName { get; set; } = "";
            public string InterventionDate { get; set; } = "";
            public string InterventionTime { get; set; } = "";
            public string ClientPhone { get; set; } = "";

            // Dati team
            public string ProjectManager { get; set; } = "";
            public string Commercial { get; set; } = "";
            public string ResponsiblePerson { get; set; } = "";

            // Dati organizzazione
            public string WBS { get; set; } = "";
            public string Area { get; set; } = "";
            public string ReporterEmail { get; set; } = "";
        }

        #endregion

        /// <summary>
        /// Test diagnostico approfondito per Outlook
        /// AGGIUNGI TEMPORANEAMENTE per debugging
        /// </summary>
        private async void TestOutlookDiagnosticAdvanced()
        {
            try
            {
                _logger.LogInfo("=== DIAGNOSI OUTLOOK AVANZATA ===");

                // Test 1: Verifica registro Windows per Outlook
                try
                {
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                    if (key != null)
                    {
                        var version = key.GetValue("VersionToReport");
                        _logger.LogInfo($"Office ClickToRun Version: {version}");
                    }
                    else
                    {
                        _logger.LogInfo("ClickToRun non trovato, verifica installazione MSI");
                    }
                }
                catch (Exception regEx)
                {
                    _logger.LogWarning($"Errore lettura registro: {regEx.Message}");
                }

                // Test 2: Verifica creazione COM diretta
                try
                {
                    _logger.LogInfo("Test creazione COM diretta...");
                    var comType = Type.GetTypeFromProgID("Outlook.Application");
                    if (comType == null)
                    {
                        _logger.LogError("Outlook.Application ProgID non trovato nel registro");
                        ShowError("Outlook Error", "Outlook non è registrato correttamente nel sistema");
                        return;
                    }

                    _logger.LogInfo($"ProgID trovato: {comType.FullName}");
                }
                catch (Exception comEx)
                {
                    _logger.LogError($"Errore ProgID: {comEx.Message}");
                }

                // Test 3: Verifica assembly Interop
                try
                {
                    _logger.LogInfo("Test assembly Interop...");
                    var interopAssembly = typeof(Microsoft.Office.Interop.Outlook.Application).Assembly;
                    _logger.LogInfo($"Interop Assembly: {interopAssembly.FullName}");
                    _logger.LogInfo($"Interop Location: {interopAssembly.Location}");
                }
                catch (Exception interopEx)
                {
                    _logger.LogError($"Errore Interop Assembly: {interopEx.Message}");
                }

                // Test 4: Tentativo creazione step-by-step
                Microsoft.Office.Interop.Outlook.Application outlookApp = null;
                try
                {
                    _logger.LogInfo("Tentativo creazione Outlook.Application...");
                    outlookApp = new Microsoft.Office.Interop.Outlook.Application();

                    _logger.LogInfo("Outlook.Application creata con successo!");

                    // Test versione
                    var version = outlookApp.Version;
                    _logger.LogInfo($"Outlook Version: {version}");

                    // Test namespace
                    var nameSpace = outlookApp.GetNamespace("MAPI");
                    _logger.LogInfo("MAPI Namespace ottenuto con successo");

                    // Test creazione MailItem
                    var mailItem = (Microsoft.Office.Interop.Outlook.MailItem)outlookApp.CreateItem(
                        Microsoft.Office.Interop.Outlook.OlItemType.olMailItem);
                    _logger.LogInfo("MailItem creato con successo");

                    // Test impostazione proprietà
                    mailItem.Subject = "Test Pianificazione JiraTicketManager";
                    mailItem.Body = "Test di integrazione Outlook";
                    _logger.LogInfo("Proprietà MailItem impostate con successo");

                    // Cleanup
                    if (mailItem != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(mailItem);
                        _logger.LogInfo("MailItem rilasciato");
                    }

                    if (nameSpace != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(nameSpace);
                        _logger.LogInfo("Namespace rilasciato");
                    }

                    ShowSuccess("Test Outlook", "Outlook funziona correttamente! Il problema dovrebbe essere risolto.");
                }
                catch (System.Runtime.InteropServices.COMException comException)
                {
                    _logger.LogError($"COM Exception: {comException.Message}");
                    _logger.LogError($"HRESULT: 0x{comException.HResult:X8}");
                    _logger.LogError($"Source: {comException.Source}");

                    var errorMessage = comException.HResult switch
                    {
                        unchecked((int)0x80040154) => "Outlook non è registrato correttamente (REGDB_E_CLASSNOTREG)",
                        unchecked((int)0x800401F0) => "COM non inizializzato (CO_E_NOTINITIALIZED)",
                        unchecked((int)0x80070005) => "Accesso negato a Outlook (E_ACCESSDENIED)",
                        _ => $"Errore COM non riconosciuto: 0x{comException.HResult:X8}"
                    };

                    ShowError("COM Error", errorMessage);
                }
                finally
                {
                    if (outlookApp != null)
                    {
                        try
                        {
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(outlookApp);
                            _logger.LogInfo("Outlook.Application rilasciato");
                        }
                        catch (Exception releaseEx)
                        {
                            _logger.LogError($"Errore rilascio COM: {releaseEx.Message}");
                        }
                    }
                }

                _logger.LogInfo("=== FINE DIAGNOSI OUTLOOK ===");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generale test Outlook", ex);
                ShowError("Test Error", $"Errore durante il test: {ex.Message}");
            }
        }

    }
}