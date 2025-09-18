using System;
using System.Drawing;
using System.Windows.Forms;
using JiraTicketManager.Services;

namespace JiraTicketManager.Forms
{
    /// <summary>
    /// Dialog di conferma per l'anteprima del commento Jira prima dell'invio.
    /// Mantiene lo stile coerente con il resto del progetto.
    /// </summary>
    public partial class CommentPreviewDialog : Form
    {
        private readonly LoggingService _logger;
        private readonly string _commentContent;
        private readonly string _ticketKey;

        #region Controls

        private Panel panelHeader;
        private Label lblTitle;
        private Label lblTicketInfo;
        private Panel panelBody;
        private TextBox txtCommentPreview;
        private Panel panelFooter;
        private Button btnInvia;
        private Button btnAnnulla;
        private Label lblCharacterCount;

        #endregion

        #region Constructor & Initialization

        /// <summary>
        /// Costruttore del dialog di anteprima commento
        /// </summary>
        /// <param name="commentContent">Contenuto del commento da mostrare</param>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <param name="templateType">Tipo di template utilizzato</param>
        public CommentPreviewDialog(string commentContent, string ticketKey, string templateType = "")
        {
            _logger = LoggingService.CreateForComponent("CommentPreviewDialog");
            _commentContent = commentContent ?? "";
            _ticketKey = ticketKey ?? "";

            _logger.LogInfo($"Apertura dialog anteprima commento per ticket {_ticketKey}");

            InitializeComponent();
            LoadCommentData(templateType);
            ConfigureFormBehavior();
        }

        /// <summary>
        /// Inizializzazione dei controlli UI
        /// </summary>
   

        /// <summary>
        /// Carica i dati del commento nel dialog
        /// </summary>
        private void LoadCommentData(string templateType)
        {
            try
            {
                // Aggiorna titolo con tipo template se disponibile
                if (!string.IsNullOrWhiteSpace(templateType))
                {
                    lblTicketInfo.Text = $"🎫 Ticket: {_ticketKey} | 📋 Template: {templateType}";
                }

                // Aggiorna conteggio caratteri
                lblCharacterCount.Text = $"📏 Lunghezza: {_commentContent.Length} caratteri";

                // Focus sul textbox per permettere scroll/selezione
                txtCommentPreview.Focus();

                _logger.LogInfo($"Dati commento caricati - {_commentContent.Length} caratteri");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento dati commento: {ex.Message}");
            }
        }

        /// <summary>
        /// Configura il comportamento del form
        /// </summary>
        private void ConfigureFormBehavior()
        {
            try
            {
                // Accept/Cancel buttons
                this.AcceptButton = btnInvia;
                this.CancelButton = btnAnnulla;

                // Event handlers
                btnInvia.Click += BtnInvia_Click;
                btnAnnulla.Click += BtnAnnulla_Click;
                this.KeyDown += CommentPreviewDialog_KeyDown;

                // Abilita key events
                this.KeyPreview = true;

                _logger.LogInfo("Comportamento form configurato");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore configurazione form: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handler per il pulsante Invia
        /// </summary>
        private void BtnInvia_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo($"Confermato invio commento per ticket {_ticketKey}");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore click Invia: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler per il pulsante Annulla
        /// </summary>
        private void BtnAnnulla_Click(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo($"Annullato invio commento per ticket {_ticketKey}");
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore click Annulla: {ex.Message}");
            }
        }

        /// <summary>
        /// Handler per i tasti di scelta rapida
        /// </summary>
        private void CommentPreviewDialog_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Ctrl+Enter = Invia
                if (e.Control && e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    btnInvia.PerformClick();
                }
                // Escape = Annulla
                else if (e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    btnAnnulla.PerformClick();
                }
                // Ctrl+A = Seleziona tutto nel textbox
                else if (e.Control && e.KeyCode == Keys.A && txtCommentPreview.Focused)
                {
                    e.Handled = true;
                    txtCommentPreview.SelectAll();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore gestione tasti: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Mostra il dialog e restituisce il risultato
        /// </summary>
        /// <param name="parent">Form parent</param>
        /// <returns>DialogResult</returns>
        public static DialogResult ShowCommentPreview(IWin32Window parent, string commentContent, string ticketKey, string templateType = "")
        {
            try
            {
                using (var dialog = new CommentPreviewDialog(commentContent, ticketKey, templateType))
                {
                    return dialog.ShowDialog(parent);
                }
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("CommentPreviewDialog");
                logger.LogError($"Errore mostra dialog anteprima: {ex.Message}");
                return DialogResult.Cancel;
            }
        }

        #endregion

        #region Dispose

        

        #endregion
    }
}