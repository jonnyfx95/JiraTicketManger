using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Forms;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            // Cerca un ListView esistente
            var existingListView = FindCommentsListView(commentsTabPage);
            if (existingListView != null)
            {
                // ✅ NUOVO: Configura eventi anche per ListView esistenti
                SetupCommentsEvents(existingListView);
                return existingListView;
            }

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

                // ✅ NUOVO: Configura gli eventi per il doppio click
                SetupCommentsEvents(listView);

                // Aggiungi al TabPage
                commentsTabPage.Controls.Add(listView);

                _logger.LogDebug("ListView commenti creato dinamicamente con eventi configurati");
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

            // NUOVO ORDINE: Visibilità come TERZA colonna
            listView.Columns.Add("👤 Autore", 150);        // 1
            listView.Columns.Add("📅 Data", 120);          // 2
            listView.Columns.Add("👁️ Visibilità", 100);    // 3 ← SPOSTATA QUI
            listView.Columns.Add("💬 Commento", 450);      // 4 ← PIÙ LARGO
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
                var item = new ListViewItem();

                // Colonna 1: Autore con emoji
                var authorText = $"{comment.AvatarEmoji} {comment.AuthorDisplayName ?? comment.Author}";
                item.Text = authorText;

                // Colonna 2: Data formattata
                item.SubItems.Add(comment.FormattedCreated);

                // Colonna 3: Visibilità (CORRETTA POSIZIONE)
                var visibilityText = GetVisibilityDisplayText(comment);
                item.SubItems.Add(visibilityText);

                // Colonna 4: Commento (ORA ULTIMO)
                var bodyText = comment.CleanBody ?? comment.Body ?? "[Commento vuoto]";
                if (bodyText.Length > 200) // Più spazio per colonna commento
                {
                    var truncated = bodyText.Substring(0, 200);
                    var lastSpace = truncated.LastIndexOf(' ');
                    if (lastSpace > 150)
                        truncated = truncated.Substring(0, lastSpace);
                    bodyText = truncated + "...";
                }
                item.SubItems.Add(bodyText);

                // Applica stili
                ApplyAdvancedCommentStyling(item, comment);
                item.Tag = comment;
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
            // SFONDO SEMPRE BIANCO - SOLO COLORE TESTO CAMBIA
            item.BackColor = Color.White;
            item.UseItemStyleForSubItems = true;

            // 🔒 COMMENTI PRIVATI = ROSSO
            if (comment.IsPrivate || comment.IsInternal)
            {
                item.ForeColor = Color.FromArgb(220, 38, 38);    // Rosso evidente
                item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }
            // 👁️ COMMENTI PUBBLICI = BLU SCURO
            else
            {
                item.ForeColor = Color.FromArgb(30, 58, 138);    // Blu navy 
                item.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            }

            // Commenti recenti = grassetto aggiuntivo
            var hoursOld = (DateTime.Now - comment.Created).TotalHours;
            if (hoursOld < 24)
            {
                var currentStyle = item.Font.Style;
                item.Font = new Font("Segoe UI", 9F, currentStyle | FontStyle.Bold);
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



        #region Event Setup

        /// <summary>
        /// Configura gli eventi del ListView per interazioni utente
        /// </summary>
        private void SetupCommentsEvents(ListView listView)
        {
            try
            {
                // DoubleClick per aprire dettaglio commento
                listView.DoubleClick += OnCommentDoubleClick;

                // MouseDown per selezione (opzionale - per feedback visivo)
                listView.MouseDown += OnCommentMouseDown;

                _logger.LogDebug("Eventi ListView commenti configurati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore configurazione eventi ListView", ex);
            }
        }

            /// <summary>
            /// Gestisce il doppio click su un commento per aprire i dettagli
            /// </summary>
            private void OnCommentDoubleClick(object sender, EventArgs e)
        {
            try
            {
                var listView = sender as ListView;
                if (listView?.SelectedItems.Count > 0)
                {
                    var selectedItem = listView.SelectedItems[0];
                    var comment = selectedItem.Tag as JiraComment;

                    if (comment != null)
                    {
                        _logger.LogInfo($"Apertura dettaglio commento di {comment.AuthorDisplayName}");

                        // Ottieni il ticket key dal parent form o da una proprietà
                        var ticketKey = GetCurrentTicketKey();

                        // Apri la form di dettaglio
                        CommentDetailForm.ShowCommentDetail(comment, ticketKey, GetParentForm(listView));

                        _logger.LogDebug("Form dettaglio commento aperta con successo");
                    }
                    else
                    {
                        _logger.LogWarning("Commento non trovato nel Tag dell'item selezionato");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore apertura dettaglio commento", ex);
                MessageBox.Show("Errore nell'apertura dei dettagli del commento.", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // <summary>
        /// Gestisce il click del mouse per feedback visivo (opzionale)
        /// </summary>
        private void OnCommentMouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                var listView = sender as ListView;
                if (listView != null && e.Button == MouseButtons.Left)
                {
                    var hitTest = listView.HitTest(e.Location);
                    if (hitTest.Item != null)
                    {
                        // Opzionale: Aggiungi feedback visivo o tooltip
                        _logger.LogDebug($"Click su commento: {hitTest.Item.Text}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione click mouse", ex);
            }
        }


        #endregion

        #region Helper Methods

        /// <summary>
        /// Ottiene la chiave del ticket corrente dal context
        /// </summary>
        private string GetCurrentTicketKey()
        {
            try
            {
                // OPZIONE 1: Se hai memorizzato il ticket key come proprietà della classe
                // return _currentTicketKey;

                // OPZIONE 2: Ottieni dal parent form se è TicketDetailForm
                var parentForm = GetParentForm(_commentsListView);
                if (parentForm != null)
                {
                    // Cerca una proprietà o field che contiene il ticket key
                    var ticketKeyProperty = parentForm.GetType().GetProperty("CurrentTicketKey")
                                         ?? parentForm.GetType().GetProperty("TicketKey");

                    if (ticketKeyProperty != null)
                    {
                        return ticketKeyProperty.GetValue(parentForm)?.ToString();
                    }

                    // Fallback: estrai dal titolo della finestra
                    var title = parentForm.Text;
                    if (title.Contains("CC-"))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(title, @"CC-\d+");
                        if (match.Success)
                        {
                            return match.Value;
                        }
                    }
                }

                // Fallback finale
                return "CC-UNKNOWN";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ottenimento ticket key", ex);
                return "CC-ERROR";
            }
        }

        /// <summary>
        /// Ottiene il form parent per posizionamento modale
        /// </summary>
        private Form GetParentForm(Control control)
        {
            try
            {
                return control?.FindForm();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ottenimento parent form", ex);
                return null;
            }
        }

        #endregion



    }
}
