using System;
using System.Drawing;
using System.Windows.Forms;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;

namespace JiraTicketManager.Forms
{
    /// <summary>
    /// Form per visualizzare i dettagli completi di un commento Jira.
    /// Mantiene lo stile consistente con il resto del progetto.
    /// </summary>
    public partial class CommentDetailForm : Form
    {
        private readonly JiraComment _comment;
        private readonly string _ticketKey;
        private readonly LoggingService _logger;

        #region Constructor & Initialization

        /// <summary>
        /// Costruttore che riceve il commento e la chiave del ticket
        /// </summary>
        public CommentDetailForm(JiraComment comment, string ticketKey)
        {
            InitializeComponent();

            _comment = comment ?? throw new ArgumentNullException(nameof(comment));
            _ticketKey = ticketKey ?? throw new ArgumentNullException(nameof(ticketKey));
            _logger = LoggingService.CreateForComponent("CommentDetailForm");

            _logger.LogInfo($"Apertura dettaglio commento per ticket {_ticketKey}");

            LoadCommentData();
            ApplyVisibilityStyle();
            ConfigureFormBehavior();
        }

        #endregion

        #region Data Loading

        /// <summary>
        /// Carica i dati del commento nei controlli UI
        /// </summary>
        private void LoadCommentData()
        {
            try
            {
                // Header - Autore con emoji avatar
                lblAuthor.Text = $"{_comment.AvatarEmoji} {_comment.AuthorDisplayName ?? _comment.Author}";

                // Header - Data formattata
                lblDate.Text = $"📅 {_comment.FormattedCreated}";

                // Header - Info ticket
                lblTicketInfo.Text = $"🎫 Ticket {_ticketKey}";

                // Body - Contenuto commento pulito
                txtCommentBody.Text = _comment.CleanBody ?? _comment.Body ?? "[Commento vuoto]";

                // Footer - Info modifica
                if (_comment.IsEdited)
                {
                    lblModified.Text = $"✏️ Modificato il {_comment.Updated:dd/MM/yyyy HH:mm}";
                    lblModified.Visible = true;
                }
                else
                {
                    lblModified.Visible = false;
                }

                // Titolo finestra con info visibilità
                var visibilityText = _comment.IsPrivate ? "PRIVATO" : "PUBBLICO";
                this.Text = $"💬 Commento di {_comment.AuthorDisplayName ?? _comment.Author} [{visibilityText}]";

                _logger.LogDebug($"Dati commento caricati - Privato: {_comment.IsPrivate}, Modificato: {_comment.IsEdited}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento dati commento", ex);
                MessageBox.Show("Errore nel caricamento dei dati del commento.", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Applica lo stile visivo basato sulla visibilità del commento
        /// </summary>
        private void ApplyVisibilityStyle()
        {
            try
            {
                if (_comment.IsPrivate || _comment.IsInternal)
                {
                    // Stile per commenti PRIVATI
                    lblVisibilityBadge.Text = "🔒 PRIVATO";
                    lblVisibilityBadge.BackColor = Color.FromArgb(220, 38, 38);   // Rosso
                    lblVisibilityBadge.ForeColor = Color.White;

                    // Evidenzia l'header con sfondo leggermente rosso
                    panelHeader.BackColor = Color.FromArgb(254, 242, 242);       // Rosso molto chiaro

                    // Autore in rosso scuro
                    lblAuthor.ForeColor = Color.FromArgb(185, 28, 28);

                    _logger.LogDebug("Applicato stile PRIVATO");
                }
                else
                {
                    // Stile per commenti PUBBLICI
                    lblVisibilityBadge.Text = "👁️ PUBBLICO";
                    lblVisibilityBadge.BackColor = Color.FromArgb(34, 197, 94);   // Verde
                    lblVisibilityBadge.ForeColor = Color.White;

                    // Header con sfondo standard
                    panelHeader.BackColor = Color.FromArgb(248, 249, 250);       // Grigio chiaro standard

                    // Autore in blu scuro
                    lblAuthor.ForeColor = Color.FromArgb(30, 58, 138);

                    _logger.LogDebug("Applicato stile PUBBLICO");
                }

                // Evidenzia commenti recenti (meno di 24 ore)
                var hoursOld = (DateTime.Now - _comment.Created).TotalHours;
                if (hoursOld < 24)
                {
                    lblAuthor.Font = new Font("Segoe UI", 12F, FontStyle.Bold);

                    // Badge NEW per commenti molto recenti (< 2 ore)
                    if (hoursOld < 2)
                    {
                        lblAuthor.Text = "🆕 " + lblAuthor.Text;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore applicazione stile visibilità", ex);
            }
        }

        /// <summary>
        /// Configura il comportamento della form
        /// </summary>
        private void ConfigureFormBehavior()
        {
            try
            {
                // Dimensione ottimale basata sul contenuto
                var contentLength = (_comment.CleanBody ?? _comment.Body ?? "").Length;

                if (contentLength < 200)
                {
                    this.Size = new Size(600, 400);        // Commento corto
                }
                else if (contentLength < 1000)
                {
                    this.Size = new Size(700, 500);        // Commento medio
                }
                else
                {
                    this.Size = new Size(800, 600);        // Commento lungo
                }

                // Focus sul testo per permettere scroll immediato
                txtCommentBody.Focus();
                txtCommentBody.SelectionStart = 0;
                txtCommentBody.SelectionLength = 0;

                // ESC per chiudere
                this.KeyPreview = true;
                this.KeyDown += (s, e) => {
                    if (e.KeyCode == Keys.Escape) this.Close();
                };

                _logger.LogDebug($"Form configurata - Dimensione: {this.Size}, Lunghezza contenuto: {contentLength}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore configurazione form", ex);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Gestisce il click del pulsante Chiudi
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            _logger.LogDebug("Chiusura form dettaglio commento");
            this.Close();
        }

        /// <summary>
        /// Gestisce la chiusura della form
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _logger.LogInfo("Form dettaglio commento chiusa");
            base.OnFormClosed(e);
        }

        #endregion

        #region Public Static Methods

        /// <summary>
        /// Metodo helper per aprire la form in modalità dialog
        /// </summary>
        public static void ShowCommentDetail(JiraComment comment, string ticketKey, IWin32Window parent = null)
        {
            try
            {
                using (var form = new CommentDetailForm(comment, ticketKey))
                {
                    form.ShowDialog(parent);
                }
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("CommentDetailForm");
                logger.LogError("Errore apertura form dettaglio commento", ex);
                MessageBox.Show("Errore nell'apertura del dettaglio commento.", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion
    }
}