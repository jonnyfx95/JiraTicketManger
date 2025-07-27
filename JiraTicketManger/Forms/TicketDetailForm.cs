using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data;
using JiraTicketManager.Services;
using JiraTicketManager.UI.Managers;

namespace JiraTicketManager.Forms
{
    public partial class TicketDetailForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraDataService _dataService;
        private readonly TextBoxManager _textBoxManager;
        private string _currentTicketKey;
        private bool _isLoading = false;

        #endregion

        #region Constructor

        public TicketDetailForm()
        {
            InitializeComponent();

            // Inizializza servizi
            _logger = LoggingService.CreateForComponent("TicketDetailForm");

            // Usa i servizi esistenti della MainForm
            var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _dataService = new JiraDataService(apiService);
            _textBoxManager = new TextBoxManager(_dataService);

            // Setup iniziale
            SetupForm();

            _logger.LogInfo("TicketDetailForm inizializzata");
        }

        #endregion

        #region Public Methods

        /// <summary>
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

                // Imposta stato loading
                SetLoadingState(true);
                _currentTicketKey = ticketKey;

                // Aggiorna titolo form
                this.Text = $"Caricamento Ticket {ticketKey}...";

                // Crea mappatura completa TextBox → Campo Jira
                var fieldMappings = CreateFieldMappings();

                // Popola tutti i campi con una sola chiamata API
                await _textBoxManager.PopulateMultipleTextBoxesAsync(ticketKey, fieldMappings);

                // Aggiorna header con info ticket
                await UpdateHeaderInfo(ticketKey);

                // Aggiorna titolo form
                this.Text = $"Dettaglio Ticket - {ticketKey}";

                _logger.LogInfo($"Ticket {ticketKey} caricato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento ticket {ticketKey}", ex);

                // Mostra errore all'utente
                MessageBox.Show(
                    $"Errore durante il caricamento del ticket {ticketKey}:\n\n{ex.Message}",
                    "Errore Caricamento Ticket",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Aggiorna titolo con errore
                this.Text = $"Errore Caricamento - {ticketKey}";
            }
            finally
            {
                SetLoadingState(false);
            }
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

                _logger.LogDebug("Form setup completato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup form", ex);
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
                [txtCommerciale] = "customfield_10272",    // Commerciale (mail)
                [txtClientePartner] = "customfield_10103", // Cliente Partner

                // === RIGHT PANEL - TEAM PLANNING ===
                [txtPM] = "customfield_10271",             // P.M. (mail)
                [txtResponsabile] = "assignee",            // Responsabile (temporaneo)
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
                // Carica dati ticket per header
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket == null) return;

                // Aggiorna header labels
                lblTicketKey.Text = $"[{ticket.Key}]";
                lblTicketSummary.Text = ticket.Summary ?? "Summary non disponibile";

                // Aggiorna metadata badges
                UpdateStatusBadge(ticket.Status);
                UpdateTypeBadge(ticket.IssueType);
                UpdatePriorityBadge(ticket.Priority);
                UpdateAssigneeBadge(ticket.AssigneeDisplayName);

                _logger.LogDebug($"Header aggiornato per ticket {ticketKey}");
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
            _textBoxManager.ClearAllMappedTextBoxes();

            // Pulisci header
            lblTicketKey.Text = "[TICKET-KEY]";
            lblTicketSummary.Text = "Summary del ticket";
            lblStatus.Text = "Status";
            lblTipo.Text = "Tipo";
            lblPriorita.Text = "Priorità";
            lblAssegnatario.Text = "Assegnatario";
        }

        #endregion

        #region Private Methods - Helpers

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
    }
}