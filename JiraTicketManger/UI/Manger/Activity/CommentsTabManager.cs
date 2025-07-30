using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;

namespace JiraTicketManager.UI.Managers.Activity
{
    /// <summary>
    /// Manager specializzato per la gestione del tab commenti.
    /// Implementa uno stile chat moderno invece del ListView tradizionale.
    /// </summary>
    public class CommentsTabManager
    {
        private readonly IActivityService _activityService;
        private readonly LoggingService _logger;
        private ListView _commentsListView;

        public CommentsTabManager(IActivityService activityService)
        {
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
            _logger = LoggingService.CreateForComponent("CommentsTabManager");
        }

        /// <summary>
        /// Carica e visualizza i commenti nel tab
        /// </summary>
        public async Task LoadCommentsAsync(TabPage commentsTabPage, string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento commenti nel tab per ticket: {ticketKey}");
                progress?.Report("Caricamento commenti...");

                // Trova o crea il ListView per i commenti
                _commentsListView = FindOrCreateCommentsListView(commentsTabPage);

                if (_commentsListView == null)
                {
                    _logger.LogError("Impossibile trovare o creare ListView per commenti");
                    return;
                }

                // Pulisci il ListView
                _commentsListView.Items.Clear();

                // Carica i commenti dal servizio
                var comments = await _activityService.GetCommentsAsync(ticketKey, progress);

                if (comments == null || !comments.Any())
                {
                    ShowNoCommentsMessage();
                    _logger.LogInfo("Nessun commento trovato");
                    return;
                }

                // Popola il ListView con i commenti
                PopulateCommentsListView(comments);

                progress?.Report($"Caricati {comments.Count} commenti");
                _logger.LogInfo($"Caricati e visualizzati {comments.Count} commenti");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento commenti per {ticketKey}", ex);
                ShowError(commentsTabPage, $"Errore caricamento commenti: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pulisce il tab commenti
        /// </summary>
        public void ClearTab(TabPage commentsTabPage)
        {
            try
            {
                if (commentsTabPage == null) return;

                var listView = FindCommentsListView(commentsTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();
                    _logger.LogDebug("Tab commenti pulito");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia tab commenti", ex);
            }
        }

        /// <summary>
        /// Mostra un messaggio di errore nel tab
        /// </summary>
        public void ShowError(TabPage commentsTabPage, string errorMessage)
        {
            try
            {
                if (commentsTabPage == null) return;

                var listView = FindOrCreateCommentsListView(commentsTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();

                    // Crea un elemento di errore
                    var errorItem = new ListViewItem("❌ Errore");
                    errorItem.SubItems.Add(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    errorItem.SubItems.Add(errorMessage);
                    errorItem.SubItems.Add("Sistema");
                    errorItem.ForeColor = Color.Red;
                    errorItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                    listView.Items.Add(errorItem);
                    _logger.LogDebug($"Messaggio di errore mostrato nel tab commenti: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione errore tab commenti", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// Trova il ListView dei commenti nel TabPage o lo crea se non esiste
        /// </summary>
        private ListView FindOrCreateCommentsListView(TabPage commentsTabPage)
        {
            if (commentsTabPage == null) return null;

            // Cerca un ListView esistente
            var existingListView = FindCommentsListView(commentsTabPage);
            if (existingListView != null)
                return existingListView;

            // Se non esiste, crealo
            return CreateCommentsListView(commentsTabPage);
        }

        /// <summary>
        /// Trova il ListView dei commenti esistente
        /// </summary>
        private ListView FindCommentsListView(TabPage commentsTabPage)
        {
            // Cerca per nome (dal Designer)
            var listView = commentsTabPage.Controls.Find("lvComments", true).FirstOrDefault() as ListView;

            // Se non trovato per nome, cerca il primo ListView
            if (listView == null)
            {
                listView = commentsTabPage.Controls.OfType<ListView>().FirstOrDefault();
            }

            return listView;
        }

        /// <summary>
        /// Crea un nuovo ListView per i commenti con stile moderno
        /// </summary>
        private ListView CreateCommentsListView(TabPage commentsTabPage)
        {
            try
            {
                var listView = new ListView
                {
                    Name = "lvCommentsGenerated",
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Segoe UI", 9F),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };

                // Configura le colonne per stile chat
                SetupCommentsColumns(listView);

                // Aggiungi al TabPage
                commentsTabPage.Controls.Add(listView);

                _logger.LogDebug("ListView commenti creato dinamicamente");
                return listView;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore creazione ListView commenti", ex);
                return null;
            }
        }

        /// <summary>
        /// Configura le colonne del ListView per uno stile chat moderno
        /// </summary>
        private void SetupCommentsColumns(ListView listView)
        {
            listView.Columns.Clear();

            // Colonne ottimizzate per visualizzazione chat-style
            listView.Columns.Add("👤 Autore", 150);      // Autore con emoji
            listView.Columns.Add("📅 Data", 120);        // Data
            listView.Columns.Add("💬 Commento", 400);    // Corpo del commento
            listView.Columns.Add("👁️ Visibilità", 100); // Visibilità
        }

        /// <summary>
        /// Popola il ListView con la lista dei commenti
        /// </summary>
        private void PopulateCommentsListView(List<JiraComment> comments)
        {
            try
            {
                _commentsListView.Items.Clear();

                // Ordina i commenti per data (più recenti prima) 
                var sortedComments = comments.OrderByDescending(c => c.Created).ToList();

                foreach (var comment in sortedComments)
                {
                    AddCommentToListView(comment);
                }

                // Auto-ridimensiona le colonne
                _commentsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                _logger.LogDebug($"Popolamento ListView completato con {comments.Count} commenti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore popolamento ListView commenti", ex);
            }
        }

        /// <summary>
        /// Aggiunge un singolo commento al ListView con stile moderno
        /// </summary>
        private void AddCommentToListView(JiraComment comment)
        {
            try
            {
                // Crea l'elemento ListView
                var item = new ListViewItem();

                // Colonna 1: Autore con emoji avatar MIGLIORATO
                var authorText = $"{comment.AvatarEmoji} {comment.AuthorDisplayName ?? comment.Author}";
                item.Text = authorText;

                // Colonna 2: Data formattata
                item.SubItems.Add(comment.FormattedCreated);

                // Colonna 3: Corpo del commento (pulito e troncato intelligentemente)
                var bodyText = comment.CleanBody ?? comment.Body ?? "[Commento vuoto]";
                // Tronca in modo intelligente mantenendo parole intere
                if (bodyText.Length > 150)
                {
                    var truncated = bodyText.Substring(0, 150);
                    var lastSpace = truncated.LastIndexOf(' ');
                    if (lastSpace > 100) // Solo se ha senso
                        truncated = truncated.Substring(0, lastSpace);
                    bodyText = truncated + "...";
                }
                item.SubItems.Add(bodyText);

                // Colonna 4: Visibilità con emoji e descrizione
                var visibilityText = GetVisibilityDisplayText(comment);
                item.SubItems.Add(visibilityText);

                // NUOVO: Stile visivo avanzato basato su proprietà del commento
                ApplyAdvancedCommentStyling(item, comment);

                // Salva l'oggetto commento nel Tag per uso futuro
                item.Tag = comment;

                // Aggiungi al ListView
                _commentsListView.Items.Add(item);

                _logger.LogDebug($"Commento aggiunto con emoji: {authorText}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiunta commento al ListView: {ex.Message}");
            }
        }

        // <summary>
        /// Ottiene il testo di visualizzazione per la visibilità del commento
        /// </summary>
        private string GetVisibilityDisplayText(JiraComment comment)
        {
            if (comment.IsInternal)
                return "🔒 Interno";

            if (!string.IsNullOrEmpty(comment.VisibilityType))
            {
                return comment.VisibilityType.ToLower() switch
                {
                    "group" => $"👥 {comment.VisibilityValue ?? "Gruppo"}",
                    "role" => $"🎭 {comment.VisibilityValue ?? "Ruolo"}",
                    _ => $"🔐 {comment.VisibilityValue ?? comment.VisibilityType}"
                };
            }

            return "👁️ Pubblico";
        }

        // <summary>
        /// Applica stile visivo avanzato al commento nel ListView
        /// </summary>
        private void ApplyAdvancedCommentStyling(ListViewItem item, JiraComment comment)
        {
            // COLORI DI BASE
            var defaultColor = Color.FromArgb(33, 37, 41);      // Nero scuro moderno
            var defaultBackColor = Color.White;

            // Stile per commenti privati/interni - MIGLIORATO
            if (comment.IsPrivate)
            {
                item.BackColor = Color.FromArgb(255, 248, 225); // Giallo molto chiaro
                item.ForeColor = Color.FromArgb(133, 100, 4);   // Marrone dorato
                item.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

                // Aggiungi un bordo visivo simulato con il primo carattere
                item.Text = "🔒 " + item.Text;
            }
            else
            {
                item.BackColor = defaultBackColor;
                item.ForeColor = defaultColor;
            }

            // Evidenzia commenti modificati
            if (comment.IsEdited)
            {
                item.ForeColor = Color.FromArgb(108, 117, 125); // Grigio
                                                                // Aggiungi indicatore di modifica
                var dateSubItem = item.SubItems[1];
                dateSubItem.Text += " ✏️";
            }

            // Evidenzia commenti recenti (meno di 24 ore) - MIGLIORATO
            var hoursOld = (DateTime.Now - comment.Created).TotalHours;
            if (hoursOld < 24)
            {
                item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                // Gradazione di "freschezza"
                if (hoursOld < 1)
                {
                    item.BackColor = Color.FromArgb(225, 248, 255); // Azzurro molto chiaro
                    item.Text = "🆕 " + item.Text;
                }
                else if (hoursOld < 6)
                {
                    item.BackColor = Color.FromArgb(240, 248, 255); // Azzurro leggerissimo
                }
            }

            // Stile per commenti lunghi
            var bodySubItem = item.SubItems[2];
            if (bodySubItem.Text.Contains("..."))
            {
                bodySubItem.Font = new Font("Segoe UI", 8.5F); // Leggermente più piccolo
                bodySubItem.ForeColor = Color.FromArgb(73, 80, 87);
            }
        }


        /// <summary>
        /// Applica lo stile visivo al commento nel ListView
        /// </summary>
        private void ApplyCommentStyling(ListViewItem item, JiraComment comment)
        {
            // Stile per commenti privati/interni
            if (comment.IsPrivate)
            {
                item.BackColor = Color.FromArgb(255, 248, 225); // Giallo chiaro
                item.ForeColor = Color.FromArgb(133, 100, 4);   // Marrone scuro
                item.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            }

            // Stile per commenti modificati
            if (comment.IsEdited)
            {
                item.ForeColor = Color.FromArgb(108, 117, 125); // Grigio
            }

            // Evidenzia commenti recenti (meno di 24 ore)
            if ((DateTime.Now - comment.Created).TotalHours < 24)
            {
                item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
        }

        /// <summary>
        /// Mostra un messaggio quando non ci sono commenti
        /// </summary>
        private void ShowNoCommentsMessage()
        {
            try
            {
                var noCommentsItem = new ListViewItem("📝 Nessun commento");
                noCommentsItem.SubItems.Add("");
                noCommentsItem.SubItems.Add("Non ci sono commenti per questo ticket");
                noCommentsItem.SubItems.Add("Pubblico");
                noCommentsItem.ForeColor = Color.Gray;
                noCommentsItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                _commentsListView.Items.Add(noCommentsItem);
                _logger.LogDebug("Messaggio 'nessun commento' visualizzato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione messaggio 'nessun commento'", ex);
            }
        }

        #endregion
    }
}