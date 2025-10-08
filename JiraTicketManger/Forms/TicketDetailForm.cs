using JiraTicketManager.Business;
using JiraTicketManager.Data;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Helpers;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;
using JiraTicketManager.Testing;
using JiraTicketManager.UI.Managers;
using JiraTicketManager.UI.Managers.Activity;
using JiraTicketManager.UI.Manger.Activity;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.MonthCalendar;
using OutlookInterop = Microsoft.Office.Interop.Outlook;




namespace JiraTicketManager.Forms
{
    public partial class TicketDetailForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraDataService _dataService;
        private readonly TextBoxManager _textBoxManager;
        readonly ConfigService _configService;
        private ComboBoxManager _comboBoxManager;
        private DevelopmentTests _devTests; 

        private IActivityTabManager _activityTabManager;


        private string _currentTicketKey;
        private bool _isLoading = false;

        private readonly EmailTemplateService _emailTemplateService;
        private readonly OutlookHybridService _outlookService;

        // === STATO PIANIFICAZIONE ===
        private bool _isPlanningEnabled = false;
        private string _currentEmailPreview = "";
        private string _currentHtmlContent = "";

        // === STATO COMMENTO ===

        private static int _commentCallCount = 0;
        private static bool _commentInProgress = false;

        private readonly WindowsToastService _toastService;

        #endregion

        #region Constructor

        public TicketDetailForm()
        {
            InitializeComponent();

            SetupActivityTabListViews();

            //InitializeCommentFunctionality();

            InitializeStatusStrip();

            // Inizializza servizi esistenti
            _logger = LoggingService.CreateForComponent("TicketDetailForm");

            // Usa i servizi esistenti della MainForm
            var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _dataService = new JiraDataService(apiService);
            _textBoxManager = new TextBoxManager(_dataService);

            // === INIZIALIZZA SERVIZI PIANIFICAZIONE (READONLY) ===
            _emailTemplateService = new EmailTemplateService();
            _outlookService = new OutlookHybridService();
            _toastService = WindowsToastService.CreateDefault();

            // Setup iniziale
            SetupForm();

            InitializeCommentFunctionality();
            InitializeDevelopmentTests();

            



            _logger.LogInfo("TicketDetailForm inizializzata con servizi pianificazione");
        }

        /// <summary>
        /// AGGIUNGERE nel costruttore di TicketDetailForm (dopo le inizializzazioni esistenti)
        /// </summary>
        private void InitializeCommentFunctionality()
        {
            try
            {
                // Collega event handler per btnCommento
                ConnectCommentButtonHandler();

                // Inizializza stato pulsante
                if (btnCommento != null)
                {
                    btnCommento.Enabled = true;
                    btnCommento.Text = "💬 Commento";
                }

                _logger.LogInfo("Funzionalità commento inizializzata");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore inizializzazione funzionalità commento: {ex.Message}");
            }
        }

        #region Event Handler Registration

        /// <summary>
        /// Collegamento dell'event handler al pulsante (aggiungere nel costruttore o InitializeComponent)
        /// </summary>
        private void ConnectCommentButtonHandler()
        {
            try
            {
                btnCommento.Click -= OnCommentoClick;
                btnCommento.Click += OnCommentoClick;

                _logger.LogInfo("Event handler btnCommento collegato");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore collegamento event handler btnCommento: {ex.Message}");
            }
        }


        #endregion


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

                // ✅ AGGIUNGI QUESTE RIGHE QUI:
                txtDataIntervento.Text = JiraDataConverter.FormatDateForDisplay(txtDataIntervento.Text);
                txtDataCompletamento.Text = JiraDataConverter.FormatDateForDisplay(txtDataCompletamento.Text);

                SetResponsabileFromArea();

                UpdateConnectionStatus(false, "🔄 Caricamento...");
                ShowStatusMessage("⏳ Caricamento ticket in corso...", Color.FromArgb(59, 130, 246));


                await UpdateHeaderInfo(ticketKey);
                this.Text = $"Dettaglio Ticket - {ticketKey}";

                _logger.LogInfo($"Ticket {ticketKey} caricato con successo");

                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var activityService = ActivityServiceFactory.Create(apiService);

                var comments = await activityService.GetCommentsAsync(ticketKey);
                var attachments = await activityService.GetAttachmentsAsync(ticketKey);



                var activityTabManager = ActivityTabManagerFactory.CreateFromApiService(apiService);
                if (tcActivity != null)
                {
                    await activityTabManager.LoadActivityTabsAsync(tcActivity, ticketKey,
                        new Progress<string>(s => _logger.LogInfo($"Progress: {s}")));

                    // ✅ AGGIORNA LA STATUSSTRIP CON I CONTEGGI:
                    var summary = await activityService.GetActivitySummaryAsync(ticketKey);
                    UpdateCommentsCount(summary.CommentsCount, summary.HistoryCount, summary.AttachmentsCount);
                }

                // ✅ AGGIORNA STATO FINALE:
                UpdateConnectionStatus(true);
                UpdateLastUpdateTime();
                ShowStatusMessage("✅ Ticket caricato con successo!", Color.FromArgb(34, 197, 94), 2000);
            }


            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento ticket {ticketKey}", ex);

                UpdateConnectionStatus(false, "❌ Errore connessione");
                ShowStatusMessage("❌ Errore caricamento ticket", Color.FromArgb(220, 38, 38), 5000);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        public void SetTextBoxValue(TextBox textBox, string value)
        {
            if (textBox.Name == "txtDataIntervento" || textBox.Name == "txtDataCompletamento")
            {
                textBox.Text = JiraDataConverter.FormatDateForDisplay(value);
            }
            else
            {
                textBox.Text = value ?? "";
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

        public ConfigService ConfigService { get; set; }

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


        private void SetupActivityTabListViews()
        {
            // Setup ListView Comments - CON MIGLIORAMENTI
            if (lvComments != null)
            {
                lvComments.Columns.Clear();
                lvComments.Columns.Add("👤 Autore", 180);        // Più largo per emoji
                lvComments.Columns.Add("📅 Data", 130);          // Leggermente più largo
                lvComments.Columns.Add("💬 Commento", 250);      // Un po' più stretto
                lvComments.Columns.Add("👁️ Visibilità", 120);    // NUOVA COLONNA per visibilità

                // Migliora l'aspetto generale
                lvComments.FullRowSelect = true;
                lvComments.GridLines = true;
                lvComments.View = View.Details;
                lvComments.Font = new Font("Segoe UI", 9F);
                lvComments.BackColor = Color.White;

                //_logger?.LogInfo("ListView Comments configurato con emoji e visibilità");
            }

            // Setup ListView History - CON ICONE MIGLIORATE
            if (lvHistory != null)
            {
                lvHistory.Columns.Clear();
                lvHistory.Columns.Add("⚡ Azione", 140);         // Più largo per icone
                lvHistory.Columns.Add("📅 Data", 130);
                lvHistory.Columns.Add("🔄 Modifiche", 320);     // Più largo per "Da → A"
                lvHistory.Columns.Add("👤 Autore", 160);        // Più largo per nomi completi

                // Migliora l'aspetto
                lvHistory.FullRowSelect = true;
                lvHistory.GridLines = true;
                lvHistory.View = View.Details;
                lvHistory.Font = new Font("Segoe UI", 9F);
                lvHistory.BackColor = Color.White;

                //_logger.LogInfo("ListView History configurato con icone migliorate");
            }

            // Setup ListView Attachments - CON ICONE FILE
            if (lvAttachments != null)
            {
                lvAttachments.Columns.Clear();
                lvAttachments.Columns.Add("📁 File", 280);       // Più largo per icone + nome
                lvAttachments.Columns.Add("📏 Dimensione", 100);
                lvAttachments.Columns.Add("📅 Data", 130);
                lvAttachments.Columns.Add("👤 Autore", 150);
                lvAttachments.Columns.Add("🔍 Azioni", 100);     // NUOVA COLONNA per azioni

                // Migliora l'aspetto
                lvAttachments.FullRowSelect = true;
                lvAttachments.GridLines = true;
                lvAttachments.View = View.Details;
                lvAttachments.Font = new Font("Segoe UI", 9F);
                lvAttachments.BackColor = Color.White;

                // _logger.LogInfo("ListView Attachments configurato con icone file");
            }

            // _logger.LogInfo("✨ Tutti i ListView configurati con stili moderni");
        }

        #endregion

        #region Private Methods - Helpers


        // <summary>
        /// Estensione per altri tipi di chiusura (futuro)
        /// </summary>
        public static class FutureClosureTypes
        {
            /// <summary>
            /// Campi per chiusura intervento (esempio futuro)
            /// </summary>
            public static class Intervento
            {
                // TODO: Definire campi per chiusura interventi
                // public static readonly (string fieldId, string displayName, string value)
                //     TipoIntervento = ("customfield_10XXX", "Tipo Intervento", "Risolto");
            }

            /// <summary>
            /// Aggiorna campi per diversi tipi di chiusura
            /// </summary>
            public static async Task<bool> UpdateClosureFieldsByTypeAsync(
                JiraApiService apiService, string ticketKey, string closureType, LoggingService logger = null)
            {
                return closureType.ToLower() switch
                {
                    "planning" or "pianificazione" =>
                        await ClosureFieldsConfig.UpdatePlanningClosureFieldsAsync(apiService, ticketKey, logger),

                    // "intervento" => 
                    //     await UpdateInterventoClosureFieldsAsync(apiService, ticketKey, logger),

                    _ => throw new ArgumentException($"Tipo chiusura non supportato: {closureType}")
                };
            }
        }
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
                    ShowError("Errore Generazione", "Errore nella generazione del contenuto email");
                    return;
                }

                // Prepara dati email per Outlook
                var emailData = PrepareEmailData(planningData, emailContent.HtmlBody);

                // ⚠️ TENTATIVO OUTLOOK CON FALLBACK AUTOMATICO
                bool outlookSuccess = false;

                try
                {
                    // Tenta di aprire Outlook
                    outlookSuccess = await _outlookService.OpenEmailAsync(emailData);

                    if (outlookSuccess)
                    {
                        _logger.LogInfo("Email Outlook aperta con successo");
                        ShowSuccess("Pianificazione Completata", "Email di pianificazione preparata e aperta in Outlook");
                    }
                }
                catch (Exception outlookEx)
                {
                    _logger.LogWarning($"Outlook non disponibile: {outlookEx.Message}");
                    outlookSuccess = false;
                }

                // ⚠️ FALLBACK SE OUTLOOK NON FUNZIONA
                if (!outlookSuccess)
                {
                    _logger.LogInfo("Outlook non disponibile - attivo fallback");

                    // Mostra finestra con dati email
                    ShowEmailDataWindow(emailData, emailContent.TextBody);

                    // Prova metodi alternativi
                    TryAlternativeEmailMethods(emailData);

                    ShowSuccess("Pianificazione Completata",
                        "Email preparata! Outlook non disponibile - utilizza i dati mostrati per invio manuale.");
                }

                // Genera commento Jira (sempre)


                _logger.LogInfo("=== FINE PIANIFICAZIONE ===");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore durante pianificazione", ex);
                ShowError("Errore Pianificazione", $"Errore durante la pianificazione: {ex.Message}");
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

                // Genera commento usando EmailTemplateService con template dinamico
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

                var jiraApiService = new JiraApiService(SettingsService.CreateDefault());
                await jiraApiService.AddCommentToIssueAsync(data.TicketKey, comment);

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

        private OutlookHybridService.EmailData PrepareEmailData(PlanningTicketData data, string htmlContent)
        {
            try
            {
                _logger.LogInfo("Preparazione dati email per Outlook");

                // Usa il metodo helper di OutlookIntegrationService
                var emailData = OutlookHybridService.PrepareEmailFromTicketData(
                    data.ClientName,
                    data.TicketKey,
                    data.Summary,
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
                    Summary = lblTicketSummary.Text?.Trim() ?? "",

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
            public string Summary { get; set; } = "";

            // Dati intervento
            public string ConsultantName { get; set; } = "";
            public string InterventionDate { get; set; } = "";
            public string InterventionTime { get; set; } = "";
            public string ClientPhone { get; set; } = "";
            public string Description { get; set; } = "";

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


        #region Comment Functionality



        /// <summary>
        /// Event handler per il pulsante Commento - genera commento formato "email inoltrata" + TRANSIZIONE
        /// 
        /// </summary>
        private async void OnCommentoClick(object sender, EventArgs e)
        {
            var callNumber = ++_commentCallCount;
            _logger?.LogInfo($"*** OnCommentoClick CHIAMATA #{callNumber} ***");

            var operationId = Guid.NewGuid().ToString("N")[..8]; // ID univoco per questa operazione

            try
            {
                _logger?.LogInfo($"[{operationId}] === INIZIO OnCommentoClick ===");

                // 🛑 PROTEZIONE GLOBALE ANTI-DUPLICAZIONE
                if (_commentInProgress)
                {
                    _logger?.LogWarning($"[{operationId}] BLOCCATO: Operazione commento già in corso");
                    return;
                }

                // Protezione locale del bottone
                if (!btnCommento.Enabled)
                {
                    _logger?.LogWarning($"[{operationId}] BLOCCATO: Bottone già disabilitato");
                    return;
                }

                // ATTIVA PROTEZIONI
                _commentInProgress = true;
                btnCommento.Enabled = false;
                btnCommento.Text = "⏳ Elaborazione...";
                this.Cursor = Cursors.WaitCursor;

                _logger?.LogInfo($"[{operationId}] Protezioni attivate, inizio elaborazione");

                // Validazione dati
                if (!ValidatePlanningData())
                {
                    _logger?.LogInfo($"[{operationId}] Validazione fallita, uscita");
                    return;
                }

                var planningData = CollectPlanningTicketData();
                if (planningData == null)
                {
                    _logger?.LogError($"[{operationId}] Planning data null, uscita");
                    _toastService?.ShowError("Errore Dati", "Impossibile raccogliere i dati dalla UI");
                    return;
                }

                // ✅ USA CommentTemplateService (non più funzione locale duplicata)
                var commentTemplateService = new CommentTemplateService();
                var commentData = CreateCommentDataFromPlanning(planningData);
                var commentContent = commentTemplateService.GenerateForwardedEmailComment(commentData);

                if (string.IsNullOrWhiteSpace(commentContent))
                {
                    _logger?.LogError($"[{operationId}] Comment content vuoto, uscita");
                    _toastService?.ShowError("Errore Generazione", "Errore nella generazione del commento");
                    return;
                }

                _logger?.LogInfo($"[{operationId}] Commento generato - Lunghezza: {commentContent.Length}");

                // Dialog conferma
                var templateDisplayName = GetTemplateDisplayName(planningData.TemplateType);
                var confirmResult = CommentPreviewDialog.ShowCommentPreview(
                    this, commentContent, planningData.TicketKey, templateDisplayName);

                if (confirmResult != DialogResult.OK)
                {
                    _logger?.LogInfo($"[{operationId}] Operazione annullata dall'utente");
                    _toastService?.ShowInfo("Operazione Annullata", "Invio commento annullato");
                    return;
                }

                // 🎯 FASE 1: Invio commento (CON TRACKING)
                _logger?.LogInfo($"[{operationId}] === FASE 1: INVIO COMMENTO ===");
                _logger?.LogInfo($"[{operationId}] Target ticket: {planningData.TicketKey}");

                bool commentSuccess = await SendCommentToJiraWithDebug(planningData.TicketKey, commentContent, operationId);

                if (!commentSuccess)
                {
                    _logger?.LogError($"[{operationId}] FASE 1 FALLITA: Errore invio commento");
                    _toastService?.ShowError("Errore Invio", "Errore durante l'invio del commento a Jira");
                    return;
                }

                _logger?.LogInfo($"[{operationId}] === FASE 1 COMPLETATA ===");

                // 🎯 FASE 2: Transizione
                _logger?.LogInfo($"[{operationId}] === FASE 2: TRANSIZIONE ===");

                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var transitionService = new JiraTransitionService(apiService);
                var transitionResult = await transitionService.TransitionToPlanningCompleteAsync(planningData.TicketKey);

                _logger?.LogInfo($"[{operationId}] === FASE 2 COMPLETATA ===");

                // 🎯 FASE 3: Gestione risultato
                _logger?.LogInfo($"[{operationId}] === FASE 3: GESTIONE RISULTATO ===");
                await HandleCommentAndTransitionResult(planningData.TicketKey, commentSuccess, transitionResult);



                // FASE 4: COMPILA CAMPI CHIUSURA (VERSIONE FINALE)
                _logger?.LogInfo($"[{operationId}] === FASE 4: COMPILA CAMPI CHIUSURA ===");

                try
                {
                    var closureSuccess = await ClosureFieldsConfig.UpdatePlanningClosureFieldsAsync(
                        apiService, planningData.TicketKey, _logger);

                    if (closureSuccess)
                    {
                        _logger?.LogInfo($"[{operationId}] ✅ Campi chiusura completati con successo");
                        _toastService?.ShowSuccess("Chiusura Completata", $"Ticket {planningData.TicketKey} aggiornato con campi di chiusura");
                    }
                    else
                    {
                        _logger?.LogWarning($"[{operationId}] ⚠️ Alcuni campi chiusura falliti (ticket comunque pianificato)");
                        _toastService?.ShowWarning("Chiusura Parziale", "Ticket pianificato ma alcuni campi chiusura falliti");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"[{operationId}] ERRORE FASE 4: {ex.Message}");
                    _toastService?.ShowWarning("Campi Chiusura", "Errore aggiornamento campi (ticket già pianificato)");
                }

                _logger?.LogInfo($"[{operationId}] === OPERAZIONE COMPLETATA ===");
            }

            
            catch (Exception ex)
            {
                _logger?.LogError($"[{operationId}] ERRORE GENERALE: {ex.Message}");
                _toastService?.ShowError("Errore Commento", $"Errore durante la generazione del commento: {ex.Message}");
            }
            finally
            {
                // DISATTIVA PROTEZIONI SEMPRE
                _commentInProgress = false;
                btnCommento.Enabled = true;
                btnCommento.Text = "💬 Commento";
                this.Cursor = Cursors.Default;

                _logger?.LogInfo($"[{operationId}] === FINE OnCommentoClick (CLEANUP ESEGUITO) ===");
            }
        }

        /// <summary>
        /// 
        /// Aggiorna i campi di pianificazione
        /// 
        /// </summary>
        /// 



        /// <summary>
        /// Wrapper per SendCommentToJira con debug avanzato
        /// </summary>
        private async Task<bool> SendCommentToJiraWithDebug(string ticketKey, string commentContent, string operationId)
        {
            _logger?.LogInfo($"[{operationId}] SendCommentToJiraWithDebug INIZIO");
            _logger?.LogInfo($"[{operationId}] Ticket: {ticketKey}, Content Length: {commentContent.Length}");

            try
            {
                // Verifica che non ci siano chiamate multiple
                var commentId = $"{ticketKey}_{DateTime.Now:HHmmss}";
                _logger?.LogInfo($"[{operationId}] Comment ID univoco: {commentId}");

                var result = await SendCommentToJiraWithRetry(ticketKey, commentContent, maxRetries: 1);

                _logger?.LogInfo($"[{operationId}] SendCommentToJiraWithDebug FINE - Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"[{operationId}] SendCommentToJiraWithDebug ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gestisce il risultato combinato di commento + transizione
        /// per il nuovo TransitionResult
        /// </summary>
        private async Task HandleCommentAndTransitionResult(string ticketKey, bool commentSuccess, JiraTransitionService.TransitionResult transitionResult)
        {
            try
            {
                if (commentSuccess && transitionResult.Success)
                {
                    // Successo completo
                    _logger.LogInfo($"Operazione completa per {ticketKey}: commento inviato e stato transizionato");

                    string statusMessage = !string.IsNullOrEmpty(transitionResult.NewStatus) && transitionResult.CurrentStatus != transitionResult.NewStatus
                        ? $"Stato cambiato da '{transitionResult.CurrentStatus}' a '{transitionResult.NewStatus}'"
                        : $"Stato confermato: '{transitionResult.NewStatus}'";

                    _toastService?.ShowSuccess("Operazione Completata",
                        $"Commento aggiunto al ticket {ticketKey}\n{statusMessage}");

                    // Refresh dei commenti e stato
                    await RefreshCommentsTab();
                    await RefreshTicketStatusDisplayIfNeeded(ticketKey, transitionResult.NewStatus);
                }
                else if (commentSuccess && !transitionResult.Success)
                {
                    // Successo parziale - commento ok, transizione fallita
                    _logger.LogWarning($"Successo parziale per {ticketKey}: commento ok, transizione fallita");
                    _logger.LogInfo($"Dettagli transizione: {transitionResult.ErrorMessage}");

                    var warningMessage = $"Commento aggiunto al ticket {ticketKey}\n" +
                                        $"Transizione automatica non riuscita: {transitionResult.ErrorMessage}\n";

                    // Se ci sono transizioni alternative, suggeriscile
                    if (transitionResult.AvailableTransitions?.Any() == true)
                    {
                        var suggestions = string.Join(", ", transitionResult.AvailableTransitions.Take(3));
                        warningMessage += $"Transizioni disponibili: {suggestions}";
                    }
                    else
                    {
                        warningMessage += "Completa manualmente il workflow in Jira";
                    }

                    _toastService?.ShowWarning("Operazione Parziale", warningMessage);

                    // Refresh solo commenti - usa il metodo senza parametri per questo caso
                    await RefreshCommentsTab();
                    await RefreshTicketStatusDisplayIfNeeded(ticketKey);
                }
                else
                {
                    // Commento fallito - non dovrebbe succedere perché è già controllato sopra
                    _logger.LogError($"Stato inconsistente per {ticketKey}");
                    _toastService?.ShowError("Errore", "Stato inconsistente dell'operazione");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore gestione risultato per {ticketKey}: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiorna la visualizzazione dello stato del ticket nella UI se presente
        /// </summary>
        private async Task RefreshTicketStatusDisplayIfNeeded(string ticketKey, string newStatus = null)
        {
            try
            {
                _logger.LogInfo($"🔄 Tentativo aggiornamento display stato per {ticketKey}");

                // Se c'è una label che mostra lo stato del ticket, aggiornala
                // Cerca controlli che potrebbero mostrare lo stato
                var statusControls = new[]
                {
            this.Controls.Find("lblStatus", true).FirstOrDefault(),
            this.Controls.Find("lblTicketStatus", true).FirstOrDefault(),
            this.Controls.Find("txtStatus", true).FirstOrDefault()
        };

                var statusControl = statusControls.FirstOrDefault(c => c != null);

                if (statusControl != null)
                {
                    // Aggiorna il testo dello stato
                    if (statusControl is Label label)
                    {
                        label.Text = "Attività pianifica";
                        label.ForeColor = Color.DarkGreen;
                        _logger.LogInfo("✅ Status label aggiornata");
                    }
                    else if (statusControl is TextBox textBox)
                    {
                        textBox.Text = "Attività pianifica";
                        textBox.BackColor = Color.LightGreen;
                        _logger.LogInfo("✅ Status textbox aggiornata");
                    }
                }
                else
                {
                    _logger.LogDebug("📋 Nessun controllo stato trovato da aggiornare");
                }

                // Placeholder per refresh più completo se necessario
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore aggiornamento display stato: {ex.Message}");
                // Non bloccare il flusso per questo errore
            }
        }


        /// <summary>
        /// Converte PlanningTicketData in CommentData per CommentTemplateService
        /// </summary>
        /// <param name="planningData">Dati della pianificazione</param>
        /// <returns>CommentData per la generazione del commento</returns>
        private CommentTemplateService.CommentData CreateCommentDataFromPlanning(PlanningTicketData planningData)
        {
            try
            {
                // Ottiene summary del ticket dalla UI
                var ticketSummary = lblTicketSummary?.Text?.Trim() ?? planningData.Summary;

                // ✅ NUOVO: Ottieni reporter display name dalla UI
                var reporterDisplayName = txtRichiedente?.Text?.Trim() ?? "";

                return new CommentTemplateService.CommentData
                {
                    // Dati ticket base
                    TicketKey = planningData.TicketKey,
                    TicketSummary = ticketSummary,
                    ClientName = planningData.ClientName,
                    WBS = planningData.WBS,

                    // Dati pianificazione
                    TemplateType = planningData.TemplateType,
                    ConsultantName = planningData.ConsultantName,
                    InterventionDate = planningData.InterventionDate,
                    InterventionTime = planningData.InterventionTime,
                    ClientPhone = planningData.ClientPhone,

                    // Nomi delle persone per simulazione email
                    ProjectManagerName = planningData.ProjectManager,
                    CommercialName = planningData.Commercial,

                    // ✅ NUOVO: Reporter display name (non ClientName!)
                    ReporterDisplayName = reporterDisplayName,

                    // Email destinatari
                    ReporterEmail = planningData.ReporterEmail,
                    ConsultantEmail = EmailConverterHelper.ConvertNameToEmail(planningData.ConsultantName),
                    ProjectManagerEmail = EmailConverterHelper.ConvertNameToEmail(planningData.ProjectManager),
                    CommercialEmail = EmailConverterHelper.ConvertNameToEmail(planningData.Commercial)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore conversione planning data: {ex.Message}");
                throw;
            }
        }


        private string GetConsultantEmailFromName(string consultantName) =>
    EmailConverterHelper.ConvertNameToEmail(consultantName);

        private string GetPMEmailFromName(string pmName) =>
            EmailConverterHelper.ConvertNameToEmail(pmName);

        private string GetCommercialEmailFromName(string commercialName) =>
            EmailConverterHelper.ConvertNameToEmail(commercialName);

        /// <summary>
        /// Converte nome persona in email aziendale con formato "Nome Cognome <email@dedagroup.it>"
        /// Riutilizza la logica esistente di EmailConverterHelper.ConvertNameToEmail
        /// </summary>
        /// <param name="personName">Nome della persona</param>
        /// <returns>Email formattata con nome e indirizzo</returns>
        private string ConvertNameToEmail(string personName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personName))
                    return "";

                // Usa EmailConverterHelper che hai già implementato con casi speciali
                var emailAddress = EmailConverterHelper.ConvertNameToEmail(personName);

                if (string.IsNullOrWhiteSpace(emailAddress))
                {
                    _logger.LogDebug($"Conversione nome->email fallita per: '{personName}'");
                    return "";
                }

                // Formatta il nome con prima lettera maiuscola e resto minuscolo
                var formattedName = FormatNameForDisplay(personName);

                // Formatta come "Nome Cognome <email@dedagroup.it>"
                var formattedEmail = $"{formattedName} <{emailAddress}>";

                _logger.LogDebug($"Conversione nome->email: '{personName}' -> '{formattedEmail}'");
                return formattedEmail;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore conversione nome->email per '{personName}': {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Formatta il nome con prima lettera maiuscola e resto minuscolo
        /// Es: "NICOLA GIOVANNI LUPO" -> "Nicola Giovanni Lupo"
        /// </summary>
        /// <param name="fullName">Nome completo</param>
        /// <returns>Nome formattato</returns>
        private string FormatNameForDisplay(string fullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return "";

                // Dividi in parole
                var words = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var formattedWords = new List<string>();

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;

                    // Prima lettera maiuscola, resto minuscolo
                    var formattedWord = char.ToUpperInvariant(word[0]) + word.Substring(1).ToLowerInvariant();
                    formattedWords.Add(formattedWord);
                }

                return string.Join(" ", formattedWords);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore formattazione nome '{fullName}': {ex.Message}");
                return fullName; // Fallback al nome originale
            }
        }


        /// <summary>
        /// Invia il commento a Jira tramite JiraApiService
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <param name="commentContent">Contenuto del commento</param>
        /// <returns>True se invio riuscito</returns>
        private async Task<bool> SendCommentToJira(string ticketKey, string commentContent)
        {
            try
            {
                _logger.LogInfo($"Invio commento a Jira - Ticket: {ticketKey}");

                // Crea JiraApiService dalla configurazione esistente (stesso pattern di MainForm)
                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());

                var success = await apiService.AddCommentToIssueAsync(ticketKey, commentContent);

                if (success)
                {
                    _logger.LogInfo($"Commento inviato con successo al ticket {ticketKey}");
                }
                else
                {
                    _logger.LogError($"Fallimento invio commento al ticket {ticketKey}");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore invio commento a Jira: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Refresh della tab commenti dopo aggiunta nuovo commento
        /// </summary>
        private async Task RefreshCommentsTab()
        {
            try
            {
                _logger.LogInfo("Refresh tab commenti");

                // TODO: Implementare refresh dei commenti
                // Questo dovrebbe ricaricare i commenti nella ListView dei commenti
                // per mostrare il nuovo commento appena aggiunto

                // Se hai un manager per i commenti, chiamalo
                // await _commentsManager?.RefreshCommentsAsync(_currentTicketKey);

                _logger.LogInfo("Refresh commenti completato");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore refresh commenti: {ex.Message}");
                // Non è critico, quindi non bloccare il flusso
            }
        }


        /// <summary>
        /// Ottiene il nome visualizzato del template
        /// </summary>
        private string GetTemplateDisplayName(EmailTemplateService.TemplateType templateType)
        {
            return templateType switch
            {
                EmailTemplateService.TemplateType.SingleIntervention => "Singolo Intervento",
                EmailTemplateService.TemplateType.MultipleInterventions => "Interventi Multipli",
                EmailTemplateService.TemplateType.ToBeAgreed => "Accordo da Definire",
                _ => "Sconosciuto"
            };
        }


        #endregion



        #region Final Integration Updates




        /// <summary>
        /// Gestisce lo stato del pulsante commento
        /// </summary>
        private void SetCommentButtonState(bool enabled, string text)
        {
            try
            {
                if (btnCommento != null)
                {
                    btnCommento.Enabled = enabled;
                    btnCommento.Text = text;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore aggiornamento stato pulsante commento: {ex.Message}");
            }
        }

        /// <summary>
        /// Invia commento a Jira con retry automatico
        /// </summary>
        private async Task<bool> SendCommentToJiraWithRetry(string ticketKey, string commentContent, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInfo($"Tentativo {attempt}/{maxRetries} - Invio commento a Jira per ticket {ticketKey}");

                    var success = await SendCommentToJira(ticketKey, commentContent);

                    if (success)
                    {
                        _logger.LogInfo($"Commento inviato con successo al tentativo {attempt}");
                        return true;
                    }

                    _logger.LogWarning($"Tentativo {attempt} fallito, ritento...");

                    // Attesa progressiva tra i tentativi
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(1000 * attempt); // 1s, 2s, 3s
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore durante tentativo {attempt}: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        throw; // Re-throw sull'ultimo tentativo
                    }

                    await Task.Delay(1000 * attempt);
                }
            }

            return false;
        }

        /// <summary>
        /// Gestisce il risultato dell'invio commento con feedback dettagliato
        /// </summary>
        private async Task HandleCommentResult(bool success, string ticketKey, int commentLength)
        {
            try
            {
                if (success)
                {
                    _logger.LogInfo($"✅ Commento inviato con successo - Ticket: {ticketKey}, Lunghezza: {commentLength}");

                    // Feedback positivo con dettagli
                    ShowSuccess("Commento Inviato",
                        $"Commento di pianificazione aggiunto al ticket {ticketKey}\n" +
                        $"📏 Lunghezza: {commentLength} caratteri\n" +
                        $"🕒 Inviato: {DateTime.Now:HH:mm:ss}");

                    // Refresh automatico dei commenti
                    await RefreshCommentsTabSafe();

                    // Aggiorna status bar se presente
                    UpdateStatusBarAfterComment(ticketKey, true);
                }
                else
                {
                    _logger.LogError($"❌ Invio commento fallito - Ticket: {ticketKey}");

                    // Feedback errore con opzioni
                    var result = MessageBox.Show(
                        $"Errore durante l'invio del commento al ticket {ticketKey}.\n\n" +
                        $"Il commento è stato generato correttamente ma non è stato possibile inviarlo.\n\n" +
                        $"Vuoi copiare il contenuto negli appunti per un invio manuale?",
                        "Errore Invio Commento",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        await CopyCommentToClipboard(ticketKey);
                    }

                    UpdateStatusBarAfterComment(ticketKey, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore gestione risultato commento: {ex.Message}");
            }
        }

        /// <summary>
        /// Gestisce errori critici durante la generazione commento
        /// </summary>
        private void HandleCommentError(Exception ex)
        {
            try
            {
                var errorMessage = "Si è verificato un errore durante la generazione del commento.";

                // Errori specifici con messaggi più chiari
                if (ex is ArgumentNullException)
                {
                    errorMessage = "Dati mancanti per la generazione del commento.";
                }
                else if (ex is InvalidOperationException)
                {
                    errorMessage = "Operazione non valida. Verificare i dati inseriti.";
                }
                else if (ex.Message.Contains("connection") || ex.Message.Contains("network"))
                {
                    errorMessage = "Errore di connessione. Verificare la connessione internet.";
                }

                ShowError("Errore Commento", $"{errorMessage}\n\nDettaglio tecnico: {ex.Message}");

                // Log dettagliato per debug
                _logger.LogError($"Stack trace errore commento: {ex.StackTrace}");
            }
            catch (Exception logEx)
            {
                // Fallback se anche la gestione errori fallisce
                MessageBox.Show("Errore critico nell'applicazione", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _logger.LogError($"Errore gestione errori commento: {logEx.Message}");
            }
        }

        /// <summary>
        /// Refresh sicuro della tab commenti (non blocca se fallisce)
        /// </summary>
        private async Task RefreshCommentsTabSafe()
        {
            try
            {
                _logger.LogInfo("Refresh tab commenti dopo invio commento");

                // TODO: Implementare refresh specifico basato sull'architettura esistente
                // Esempi di possibili implementazioni:

                // OPZIONE 1: Se hai CommentsTabManager
                // if (_commentsManager != null)
                // {
                //     await _commentsManager.RefreshCommentsAsync(_currentTicketKey);
                // }

                // OPZIONE 2: Se hai ActivityTabManager
                // if (_activityTabManager != null)
                // {
                //     await _activityTabManager.RefreshActivityAsync(_currentTicketKey);
                // }

                // OPZIONE 3: Refresh generico
                // await LoadTicketActivity(_currentTicketKey);

                _logger.LogInfo("Refresh commenti completato");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Refresh commenti fallito (non critico): {ex.Message}");
                // Non propagare l'errore - il refresh non è critico
            }
        }

        /// <summary>
        /// Aggiorna la status bar dopo l'invio del commento
        /// </summary>
        private void UpdateStatusBarAfterComment(string ticketKey, bool success)
        {
            try
            {
                // Aggiorna status bar se presente
                if (statusStrip1 != null)
                {
                    var statusText = success
                        ? $"✅ Commento inviato a {ticketKey} - {DateTime.Now:HH:mm:ss}"
                        : $"❌ Errore invio commento a {ticketKey} - {DateTime.Now:HH:mm:ss}";

                    // Trova e aggiorna label appropriata nella status bar
                    foreach (ToolStripItem item in statusStrip1.Items)
                    {
                        if (item is ToolStripStatusLabel label && label.Name.Contains("LastUpdate"))
                        {
                            label.Text = statusText;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore aggiornamento status bar: {ex.Message}");
            }
        }

        /// <summary>
        /// Copia il commento negli appunti per invio manuale
        /// </summary>
        private async Task CopyCommentToClipboard(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Copia commento negli appunti per ticket {ticketKey}");

                // Raccoglie i dati dalla UI
                var planningData = CollectPlanningTicketData();
                if (planningData == null)
                {
                    _logger.LogError("Planning data null - impossibile generare commento");
                    ShowError("Errore", "Impossibile raccogliere i dati necessari per il commento");
                    return;
                }

                // Usa CommentTemplateService
                var commentTemplateService = new CommentTemplateService();
                var commentData = CreateCommentDataFromPlanning(planningData);
                var commentContent = commentTemplateService.GenerateForwardedEmailComment(commentData);

                if (string.IsNullOrWhiteSpace(commentContent))
                {
                    _logger.LogError("Comment content vuoto dopo generazione");
                    ShowError("Errore", "Errore nella generazione del commento");
                    return;
                }

                // Copia negli appunti
                Clipboard.SetText(commentContent);

                ShowInfo("Copiato negli Appunti",
                    $"Commento copiato negli appunti.\n" +
                    $"Puoi incollarlo manualmente in Jira per il ticket {ticketKey}\n\n" +
                    $"Lunghezza: {commentContent.Length} caratteri");

                _logger.LogInfo($"Commento copiato negli appunti con successo - {commentContent.Length} caratteri");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore copia negli appunti: {ex.Message}");
                ShowError("Errore", "Impossibile copiare il commento negli appunti");
            }
        }



        #endregion

        #region Validation Enhancements

        /// <summary>
        /// Validazione specifica per i dati del commento (estende ValidatePlanningData)
        /// </summary>
        private bool ValidateCommentData()
        {
            try
            {
                // Validazione base (riusa quella esistente)
                if (!ValidatePlanningData())
                {
                    return false;
                }

                // Validazioni specifiche per commento
                if (string.IsNullOrWhiteSpace(_currentTicketKey))
                {
                    ShowError("Validazione", "Nessun ticket selezionato per il commento");
                    return false;
                }

                // Verifica che il ticket key sia in formato valido
                if (!IsValidTicketKey(_currentTicketKey))
                {
                    ShowError("Validazione", $"Formato ticket key non valido: {_currentTicketKey}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore validazione dati commento: {ex.Message}");
                ShowError("Errore Validazione", "Errore durante la validazione dei dati");
                return false;
            }
        }

        /// <summary>
        /// Verifica se il ticket key è in formato valido
        /// </summary>
        private bool IsValidTicketKey(string ticketKey)
        {
            if (string.IsNullOrWhiteSpace(ticketKey))
                return false;

            // Pattern base: PROJECT-NUMBER (es: CC-1234, PROJ-567)
            return System.Text.RegularExpressions.Regex.IsMatch(
                ticketKey,
                @"^[A-Z]+-\d+$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        #endregion





        #region Fallback Email Methods

        /// <summary>
        /// Mostra finestra con i dati email quando Outlook non è disponibile
        /// </summary>
        private void ShowEmailDataWindow(OutlookHybridService.EmailData emailData, string textContent)
        {
            try
            {
                var emailForm = new Form
                {
                    Text = "📧 Dati Email Pianificazione - Outlook Non Disponibile",
                    Size = new Size(900, 700),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    MinimumSize = new Size(700, 500),
                    MaximizeBox = true,
                    ShowIcon = true
                };

                // Panel principale
                var mainPanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3,
                    Padding = new Padding(10)
                };
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                // Header con istruzioni
                var lblHeader = new Label
                {
                    Text = "⚠️ Outlook non disponibile. Utilizza questi dati per inviare l'email manualmente:",
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = Color.DarkOrange,
                    AutoSize = true,
                    Margin = new Padding(0, 0, 0, 10)
                };

                // TextBox con dati email
                var txtEmailData = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 9),
                    BackColor = Color.White,
                    Text = BuildEmailDisplayText(emailData, textContent)
                };

                // Panel pulsanti
                var buttonPanel = new FlowLayoutPanel
                {
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                    Dock = DockStyle.Fill
                };

                // Pulsante Copia Tutto
                var btnCopyAll = new Button
                {
                    Text = "📋 Copia Tutto",
                    Size = new Size(120, 35),
                    UseVisualStyleBackColor = true
                };
                btnCopyAll.Click += (s, e) =>
                {
                    Clipboard.SetText(txtEmailData.Text);
                    ShowSuccess("Copiato", "Tutti i dati email copiati negli appunti");
                };

                // Pulsante Copia Solo Email
                var btnCopyEmails = new Button
                {
                    Text = "📧 Copia Email",
                    Size = new Size(120, 35),
                    UseVisualStyleBackColor = true
                };
                btnCopyEmails.Click += (s, e) =>
                {
                    var emails = $"A: {emailData.To}\nCC: {emailData.Cc}";
                    Clipboard.SetText(emails);
                    ShowSuccess("Copiato", "Indirizzi email copiati negli appunti");
                };

                // Pulsante Salva File
                var btnSave = new Button
                {
                    Text = "💾 Salva File",
                    Size = new Size(120, 35),
                    UseVisualStyleBackColor = true
                };
                btnSave.Click += (s, e) => SaveEmailToFile(emailData, textContent);

                // Pulsante Mailto (alternativo)
                var btnMailto = new Button
                {
                    Text = "🔗 Apri MailTo",
                    Size = new Size(120, 35),
                    UseVisualStyleBackColor = true
                };
                btnMailto.Click += (s, e) => TryMailToUrl(emailData);

                // Pulsante Chiudi
                var btnClose = new Button
                {
                    Text = "✖️ Chiudi",
                    Size = new Size(120, 35),
                    UseVisualStyleBackColor = true
                };
                btnClose.Click += (s, e) => emailForm.Close();

                // Assembla tutto
                buttonPanel.Controls.AddRange(new Control[] { btnClose, btnMailto, btnSave, btnCopyEmails, btnCopyAll });
                mainPanel.Controls.Add(lblHeader, 0, 0);
                mainPanel.Controls.Add(txtEmailData, 0, 1);
                mainPanel.Controls.Add(buttonPanel, 0, 2);
                emailForm.Controls.Add(mainPanel);

                // Mostra la finestra
                emailForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione dati email", ex);
            }
        }

        /// <summary>
        /// Costruisce il testo da visualizzare nella finestra
        /// </summary>
        private string BuildEmailDisplayText(OutlookHybridService.EmailData emailData, string textContent)
        {
            var sb = new StringBuilder();

            sb.AppendLine("📧 DATI EMAIL PIANIFICAZIONE");
            sb.AppendLine("=" + new string('=', 60));
            sb.AppendLine();
            sb.AppendLine($"A: {emailData.To}");
            sb.AppendLine($"CC: {emailData.Cc}");
            if (!string.IsNullOrWhiteSpace(emailData.Bcc))
                sb.AppendLine($"BCC: {emailData.Bcc}");
            sb.AppendLine($"Oggetto: {emailData.Subject}");
            sb.AppendLine();
            sb.AppendLine("CONTENUTO TESTO:");
            sb.AppendLine("-" + new string('-', 59));
            sb.AppendLine(textContent);
            sb.AppendLine();
            sb.AppendLine("CONTENUTO HTML:");
            sb.AppendLine("-" + new string('-', 59));
            sb.AppendLine(emailData.BodyHtml);
            sb.AppendLine();
            sb.AppendLine("📋 ISTRUZIONI:");
            sb.AppendLine("1. Copia gli indirizzi email (A e CC)");
            sb.AppendLine("2. Apri il tuo client email preferito");
            sb.AppendLine("3. Incolla destinatari e oggetto");
            sb.AppendLine("4. Copia e incolla il contenuto");
            sb.AppendLine();
            sb.AppendLine($"Generato il: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("Da: JiraTicketManager - Sistema Pianificazione");

            return sb.ToString();
        }

        /// <summary>
        /// Salva i dati email in un file
        /// </summary>
        private void SaveEmailToFile(OutlookHybridService.EmailData emailData, string textContent)
        {
            try
            {
                using var saveDialog = new SaveFileDialog
                {
                    Filter = "File Email (*.eml)|*.eml|File Testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"Email_Pianificazione_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    var content = BuildEmailDisplayText(emailData, textContent);
                    File.WriteAllText(saveDialog.FileName, content, Encoding.UTF8);

                    ShowSuccess("File Salvato", $"Email salvata in:\n{saveDialog.FileName}");
                    _logger.LogInfo($"Email fallback salvata: {saveDialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio email", ex);
                ShowError("Errore Salvataggio", $"Errore nel salvataggio: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta metodi alternativi per aprire email
        /// </summary>
        private void TryAlternativeEmailMethods(OutlookHybridService.EmailData emailData)
        {
            try
            {
                // Metodo 1: MailTo URL (funziona con qualsiasi client email)
                TryMailToUrl(emailData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Metodi alternativi falliti: {ex.Message}");
            }
        }

        /// <summary>
        /// Tenta di aprire email con MailTo URL
        /// </summary>
        private bool TryMailToUrl(OutlookHybridService.EmailData emailData)
        {
            try
            {
                _logger.LogInfo("Tentativo apertura MailTo URL");

                var mailtoUrl = $"mailto:{Uri.EscapeDataString(emailData.To)}";
                var parameters = new List<string>();

                if (!string.IsNullOrWhiteSpace(emailData.Cc))
                    parameters.Add($"cc={Uri.EscapeDataString(emailData.Cc)}");

                if (!string.IsNullOrWhiteSpace(emailData.Subject))
                    parameters.Add($"subject={Uri.EscapeDataString(emailData.Subject)}");

                // Converti HTML a testo per mailto
                if (!string.IsNullOrWhiteSpace(emailData.BodyText))
                {
                    var textBody = emailData.BodyText.Length > 1000
                        ? emailData.BodyText.Substring(0, 1000) + "..."
                        : emailData.BodyText;
                    parameters.Add($"body={Uri.EscapeDataString(textBody)}");
                }

                if (parameters.Any())
                    mailtoUrl += "?" + string.Join("&", parameters);

                Process.Start(new ProcessStartInfo(mailtoUrl) { UseShellExecute = true });

                _logger.LogInfo("MailTo URL aperto con successo");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"MailTo URL fallito: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region StatusStrip Management

        /// <summary>
        /// Configura e inizializza la StatusStrip con i componenti necessari
        /// </summary>
        private void InitializeStatusStrip()
        {
            try
            {
                // Pulisci eventuali item esistenti
                statusStrip1.Items.Clear();

                // 1. STATO CONNESSIONE (sinistra)
                var connectionStatusLabel = new ToolStripStatusLabel
                {
                    Name = "lblConnectionStatus",
                    Text = "🔴 Non connesso",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(220, 38, 38), // Rosso iniziale
                    AutoSize = true,
                    Margin = new Padding(5, 2, 15, 2)
                };
                statusStrip1.Items.Add(connectionStatusLabel);

                // 2. NUMERO COMMENTI (centro)
                var commentsCountLabel = new ToolStripStatusLabel
                {
                    Name = "lblCommentsCount",
                    Text = "Numero Commenti: --",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(59, 130, 246), // Blu
                    AutoSize = true,
                    Margin = new Padding(15, 2, 15, 2)
                };
                statusStrip1.Items.Add(commentsCountLabel);

                // 3. SPRING (per spingere l'ultimo item a destra)
                var springLabel = new ToolStripStatusLabel
                {
                    Spring = true,
                    Text = ""
                };
                statusStrip1.Items.Add(springLabel);

                // 4. ULTIMO AGGIORNAMENTO (destra)
                var lastUpdateLabel = new ToolStripStatusLabel
                {
                    Name = "lblLastUpdate",
                    Text = "⏰ Ultimo aggiornamento: --",
                    Font = new Font("Segoe UI", 9F),
                    ForeColor = Color.FromArgb(107, 114, 128), // Grigio
                    AutoSize = true,
                    Margin = new Padding(15, 2, 5, 2)
                };
                statusStrip1.Items.Add(lastUpdateLabel);

                //_logger.LogInfo("StatusStrip inizializzata con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione StatusStrip", ex);
            }
        }

        /// <summary>
        /// Aggiorna lo stato della connessione nella StatusStrip
        /// </summary>
        private void UpdateConnectionStatus(bool isConnected, string message = null)
        {
            try
            {
                var connectionLabel = statusStrip1.Items["lblConnectionStatus"] as ToolStripStatusLabel;
                if (connectionLabel != null)
                {
                    if (isConnected)
                    {
                        connectionLabel.Text = "🟢 Connesso a Jira";
                        connectionLabel.ForeColor = Color.FromArgb(34, 197, 94); // Verde
                    }
                    else
                    {
                        connectionLabel.Text = message ?? "🔴 Non connesso";
                        connectionLabel.ForeColor = Color.FromArgb(220, 38, 38); // Rosso
                    }
                }

                _logger.LogDebug($"Stato connessione aggiornato: {isConnected}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento stato connessione", ex);
            }
        }

        /// <summary>
        /// Aggiorna il contatore dei commenti nella StatusStrip
        /// </summary>
        private void UpdateCommentsCount(int commentsCount, int historyCount = 0, int attachmentsCount = 0)
        {
            try
            {
                var commentsLabel = statusStrip1.Items["lblCommentsCount"] as ToolStripStatusLabel;
                if (commentsLabel != null)
                {
                    if (historyCount > 0 || attachmentsCount > 0)
                    {
                        // Mostra statistiche complete
                        commentsLabel.Text = $"💬 Commenti: {commentsCount} | 📜 Cronologia: {historyCount} | 📎 Allegati: {attachmentsCount}";
                    }
                    else
                    {
                        // Solo commenti
                        commentsLabel.Text = $"Numero Commenti: {commentsCount}";
                    }

                    // Colore basato su presenza di contenuto
                    if (commentsCount > 0)
                    {
                        commentsLabel.ForeColor = Color.FromArgb(59, 130, 246); // Blu
                    }
                    else
                    {
                        commentsLabel.ForeColor = Color.FromArgb(107, 114, 128); // Grigio
                    }
                }

                _logger.LogDebug($"Contatore commenti aggiornato: {commentsCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento contatore commenti", ex);
            }
        }

        /// <summary>
        /// Aggiorna il timestamp dell'ultimo aggiornamento
        /// </summary>
        private void UpdateLastUpdateTime(DateTime? updateTime = null)
        {
            try
            {
                var lastUpdateLabel = statusStrip1.Items["lblLastUpdate"] as ToolStripStatusLabel;
                if (lastUpdateLabel != null)
                {
                    var timestamp = updateTime ?? DateTime.Now;
                    lastUpdateLabel.Text = $"⏰ Ultimo aggiornamento: {timestamp:dd/MM/yyyy HH:mm:ss}";
                    lastUpdateLabel.ForeColor = Color.FromArgb(107, 114, 128); // Grigio
                }

                _logger.LogDebug($"Timestamp ultimo aggiornamento: {updateTime ?? DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento timestamp", ex);
            }
        }

        /// <summary>
        /// Mostra un messaggio temporaneo nella StatusStrip
        /// </summary>
        private void ShowStatusMessage(string message, Color? textColor = null, int durationMs = 3000)
        {
            try
            {
                var tempLabel = statusStrip1.Items["lblTempMessage"] as ToolStripStatusLabel;

                if (tempLabel == null)
                {
                    // Crea label temporaneo se non esiste
                    tempLabel = new ToolStripStatusLabel
                    {
                        Name = "lblTempMessage",
                        Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                        AutoSize = true,
                        Margin = new Padding(5, 2, 5, 2)
                    };
                    statusStrip1.Items.Insert(0, tempLabel); // Inserisci all'inizio
                }

                tempLabel.Text = message;
                tempLabel.ForeColor = textColor ?? Color.FromArgb(59, 130, 246);
                tempLabel.Visible = true;

                // Timer per nascondere il messaggio
                var timer = new System.Windows.Forms.Timer
                {
                    Interval = durationMs
                };
                timer.Tick += (s, e) =>
                {
                    tempLabel.Visible = false;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();

                _logger.LogDebug($"Messaggio temporaneo mostrato: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione messaggio temporaneo", ex);
            }
        }

        #endregion


#if DEBUG
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
#if DEBUG
            try
            {
                // F2 = Test Cliente Partner
                if (keyData == Keys.F2)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger?.LogInfo("🧪 F2: Avvio test Cliente Partner");

                            if (_devTests != null)
                            {
                                var result = await _devTests.TestClientePartnerResolution();
                                _logger?.LogInfo($"🧪 F2: Test Cliente Partner {(result ? "✅ SUPERATO" : "❌ FALLITO")}");

                                // Mostra toast con risultato
                                var toastService = WindowsToastService.CreateDefault();
                                if (result)
                                    toastService.ShowSuccess("Test F2", "Cliente Partner test superato!");
                                else
                                    toastService.ShowError("Test F2", "Cliente Partner test fallito!");
                            }
                            else
                            {
                                _logger?.LogWarning("❌ DevelopmentTests non inizializzato");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Errore F2: {ex.Message}");
                        }
                    });
                    return true;
                }

                // F3 = Test Firma Outlook
                if (keyData == Keys.F3)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger?.LogInfo("🧪 F3: Avvio test Firma Outlook");

                            if (_devTests != null)
                            {
                                var result = await _devTests.TestOutlookSignatureDebug();
                                _logger?.LogInfo($"🧪 F3: Test Firma {(result ? "✅ SUPERATO" : "❌ FALLITO")}");

                                var toastService = WindowsToastService.CreateDefault();
                                if (result)
                                    toastService.ShowSuccess("Test F3", "Firma Outlook test superato!");
                                else
                                    toastService.ShowError("Test F3", "Firma Outlook test fallito!");
                            }
                            else
                            {
                                _logger?.LogWarning("❌ DevelopmentTests non inizializzato");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Errore F3: {ex.Message}");
                        }
                    });
                    return true;
                }

                // Ctrl+F3 = Test Completo
                if (keyData == (Keys.Control | Keys.F3))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger?.LogInfo("🧪 Ctrl+F3: Avvio test completo");

                            if (_devTests != null)
                            {
                                var clienteResult = await _devTests.TestClientePartnerResolution();
                                var firmaResult = await _devTests.TestOutlookSignatureDebug();

                                var message = $"Cliente Partner: {(clienteResult ? "✅" : "❌")}, Firma: {(firmaResult ? "✅" : "❌")}";
                                _logger?.LogInfo($"🧪 Ctrl+F3: {message}");

                                var toastService = WindowsToastService.CreateDefault();
                                if (clienteResult && firmaResult)
                                    toastService.ShowSuccess("Test Completo", "Tutti i test superati!");
                                else
                                    toastService.ShowWarning("Test Completo", $"Risultati: {message}");
                            }
                            else
                            {
                                _logger?.LogWarning("❌ DevelopmentTests non inizializzato");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Errore Ctrl+F3: {ex.Message}");
                        }
                    });
                    return true;
                }

                // F4 = Tutti i test di sviluppo
                if (keyData == Keys.F4)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            _logger?.LogInfo("🧪 F4: Avvio tutti i test di sviluppo");

                            if (_devTests != null)
                            {
                                await _devTests.RunAllAsync();
                                _logger?.LogInfo("🧪 F4: Tutti i test completati");

                                var toastService = WindowsToastService.CreateDefault();
                                toastService.ShowInfo("Test F4", "Tutti i test completati! Controlla i log.");
                            }
                            else
                            {
                                _logger?.LogWarning("❌ DevelopmentTests non inizializzato");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError($"Errore F4: {ex.Message}");
                        }
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore ProcessCmdKey: {ex.Message}");
            }
#endif

            return base.ProcessCmdKey(ref msg, keyData);
        }
#endif


#if DEBUG
        /// <summary>
        /// Inizializza i test di sviluppo solo in modalità DEBUG
        /// </summary>
        private void InitializeDevelopmentTests()
        {
            try
            {
                _logger?.LogInfo("Inizializzazione DevelopmentTests...");
                _devTests = new DevelopmentTests(_logger, this);
                _logger?.LogInfo("✅ DevelopmentTests inizializzati con successo");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"❌ Errore inizializzazione DevelopmentTests: {ex.Message}");
                _devTests = null; // Assicurati che sia null in caso di errore
            }
        }
#endif



    }

}