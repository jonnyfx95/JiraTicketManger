using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;

namespace JiraTicketManager.UI.Managers.Activity
{
    /// <summary>
    /// Manager specializzato per la gestione del tab allegati.
    /// Implementa visualizzazione con icone, metadati e preview moderni.
    /// </summary>
    public class AttachmentsTabManager
    {
        private readonly IActivityService _activityService;
        private readonly LoggingService _logger;
        private ListView _attachmentsListView;
        private List<JiraAttachment> _currentAttachments = new List<JiraAttachment>();

        public AttachmentsTabManager(IActivityService activityService)
        {
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
            _logger = LoggingService.CreateForComponent("AttachmentsTabManager");
        }

        /// <summary>
        /// Carica e visualizza gli allegati nel tab
        /// </summary>
        public async Task LoadAttachmentsAsync(TabPage attachmentsTabPage, string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento allegati nel tab per ticket: {ticketKey}");
                progress?.Report("Caricamento allegati...");

                // Trova o crea il ListView per gli allegati
                _attachmentsListView = FindOrCreateAttachmentsListView(attachmentsTabPage);

                if (_attachmentsListView == null)
                {
                    _logger.LogError("Impossibile trovare o creare ListView per allegati");
                    return;
                }

                // Pulisci il ListView
                _attachmentsListView.Items.Clear();
                _currentAttachments.Clear();

                // Carica gli allegati dal servizio
                var attachments = await _activityService.GetAttachmentsAsync(ticketKey, progress);

                if (attachments == null || !attachments.Any())
                {
                    ShowNoAttachmentsMessage();
                    _logger.LogInfo("Nessun allegato trovato");
                    return;
                }

                // Salva gli allegati per uso futuro
                _currentAttachments = attachments;

                // Popola il ListView con gli allegati
                PopulateAttachmentsListView(attachments);

                progress?.Report($"Caricati {attachments.Count} allegati");
                _logger.LogInfo($"Caricati e visualizzati {attachments.Count} allegati");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento allegati per {ticketKey}", ex);
                ShowError(attachmentsTabPage, $"Errore caricamento allegati: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pulisce il tab allegati
        /// </summary>
        public void ClearTab(TabPage attachmentsTabPage)
        {
            try
            {
                if (attachmentsTabPage == null) return;

                var listView = FindAttachmentsListView(attachmentsTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();
                    _currentAttachments.Clear();
                    _logger.LogDebug("Tab allegati pulito");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia tab allegati", ex);
            }
        }

        /// <summary>
        /// Mostra un messaggio di errore nel tab
        /// </summary>
        public void ShowError(TabPage attachmentsTabPage, string errorMessage)
        {
            try
            {
                if (attachmentsTabPage == null) return;

                var listView = FindOrCreateAttachmentsListView(attachmentsTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();

                    // Crea un elemento di errore
                    var errorItem = new ListViewItem("❌ Errore");
                    errorItem.SubItems.Add(errorMessage);
                    errorItem.SubItems.Add("N/A");
                    errorItem.SubItems.Add("N/A");
                    errorItem.SubItems.Add("Sistema");
                    errorItem.ForeColor = Color.Red;
                    errorItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                    listView.Items.Add(errorItem);
                    _logger.LogDebug($"Messaggio di errore mostrato nel tab allegati: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione errore tab allegati", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// Trova il ListView degli allegati nel TabPage o lo crea se non esiste
        /// </summary>
        private ListView FindOrCreateAttachmentsListView(TabPage attachmentsTabPage)
        {
            if (attachmentsTabPage == null) return null;

            // Cerca un ListView esistente
            var existingListView = FindAttachmentsListView(attachmentsTabPage);
            if (existingListView != null)
                return existingListView;

            // Se non esiste, crealo
            return CreateAttachmentsListView(attachmentsTabPage);
        }

        /// <summary>
        /// Trova il ListView degli allegati esistente
        /// </summary>
        private ListView FindAttachmentsListView(TabPage attachmentsTabPage)
        {
            // Cerca per nome (dal Designer)
            var listView = attachmentsTabPage.Controls.Find("lvAttachments", true).FirstOrDefault() as ListView;

            // Se non trovato per nome, cerca il primo ListView
            if (listView == null)
            {
                listView = attachmentsTabPage.Controls.OfType<ListView>().FirstOrDefault();
            }

            return listView;
        }

        /// <summary>
        /// Crea un nuovo ListView per gli allegati con stile moderno
        /// </summary>
        private ListView CreateAttachmentsListView(TabPage attachmentsTabPage)
        {
            try
            {
                var listView = new ListView
                {
                    Name = "lvAttachmentsGenerated",
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Segoe UI", 9F),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };

                // Configura le colonne per allegati moderni
                SetupAttachmentsColumns(listView);

                // Aggiungi eventi per interazione
                SetupAttachmentsEvents(listView);

                // Aggiungi al TabPage
                attachmentsTabPage.Controls.Add(listView);

                _logger.LogDebug("ListView allegati creato dinamicamente");
                return listView;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore creazione ListView allegati", ex);
                return null;
            }
        }

        /// <summary>
        /// Configura le colonne del ListView per allegati moderni
        /// </summary>
        private void SetupAttachmentsColumns(ListView listView)
        {
            listView.Columns.Clear();

            // Colonne ottimizzate per visualizzazione allegati
            listView.Columns.Add("📁 File", 250);         // Nome file con icona
            listView.Columns.Add("📏 Dimensione", 100);   // Dimensione
            listView.Columns.Add("📅 Data", 120);         // Data upload
            listView.Columns.Add("👤 Autore", 150);       // Chi ha caricato
            listView.Columns.Add("🔍 Azioni", 80);        // Azioni (preview/download)
        }

        /// <summary>
        /// Configura gli eventi del ListView per interazioni
        /// </summary>
        private void SetupAttachmentsEvents(ListView listView)
        {
            // Doppio click per aprire/scaricare allegato
            listView.DoubleClick += OnAttachmentDoubleClick;

            // Click destro per menu contestuale (futuro)
            listView.MouseClick += OnAttachmentMouseClick;
        }

        /// <summary>
        /// Popola il ListView con la lista degli allegati
        /// </summary>
        private void PopulateAttachmentsListView(List<JiraAttachment> attachments)
        {
            try
            {
                _attachmentsListView.Items.Clear();

                // Ordina gli allegati per data (più recenti prima)
                var sortedAttachments = attachments.OrderByDescending(a => a.Created).ToList();

                foreach (var attachment in sortedAttachments)
                {
                    AddAttachmentToListView(attachment);
                }

                // Auto-ridimensiona le colonne
                _attachmentsListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                _logger.LogDebug($"Popolamento ListView allegati completato con {attachments.Count} elementi");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore popolamento ListView allegati", ex);
            }
        }

        /// <summary>
        /// Aggiunge un singolo allegato al ListView con stile moderno
        /// </summary>
        private void AddAttachmentToListView(JiraAttachment attachment)
        {
            try
            {
                var item = new ListViewItem();

                // Colonna 1: Icona + nome file
                var fileText = $"{attachment.FileIcon} {attachment.Filename}";
                item.Text = fileText;

                // Colonna 2: Dimensione leggibile
                item.SubItems.Add(attachment.HumanReadableSize);

                // Colonna 3: Data formattata
                item.SubItems.Add(attachment.FormattedCreated);

                // Colonna 4: Autore
                item.SubItems.Add(attachment.AuthorDisplayName ?? attachment.Author);

                // Colonna 5: Azioni
                var actionText = attachment.CanPreview ? "👁️ Preview" : "💾 Download";
                item.SubItems.Add(actionText);

                // Applica stile visivo basato sul tipo di file
                ApplyAttachmentStyling(item, attachment);

                // Salva l'oggetto allegato nel Tag
                item.Tag = attachment;

                _attachmentsListView.Items.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiunta allegato al ListView: {ex.Message}");
            }
        }

        /// <summary>
        /// Applica lo stile visivo all'allegato nel ListView
        /// </summary>
        private void ApplyAttachmentStyling(ListViewItem item, JiraAttachment attachment)
        {
            // Colore di sfondo basato sul tipo di file
            var cardColor = ColorTranslator.FromHtml(attachment.FileCardColor);

            // Applica un colore leggero come sfondo
            item.BackColor = Color.FromArgb(25, cardColor.R, cardColor.G, cardColor.B);

            // Testo del tipo di file
            item.ForeColor = cardColor;

            // Evidenzia file che possono essere visualizzati in anteprima
            if (attachment.CanPreview)
            {
                item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Evidenzia allegati recenti (meno di 24 ore)
            if ((DateTime.Now - attachment.Created).TotalHours < 24)
            {
                item.BackColor = Color.FromArgb(248, 249, 250); // Grigio molto chiaro
            }
        }

        /// <summary>
        /// Gestisce il doppio click su un allegato
        /// </summary>
        private async void OnAttachmentDoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (_attachmentsListView.SelectedItems.Count == 0) return;

                var selectedItem = _attachmentsListView.SelectedItems[0];
                var attachment = selectedItem.Tag as JiraAttachment;

                if (attachment == null) return;

                _logger.LogInfo($"Doppio click su allegato: {attachment.Filename}");

                // Mostra progress durante il download
                var progressForm = ShowDownloadProgress(attachment.Filename);

                try
                {
                    // Se può essere visualizzato in anteprima, aprilo nel browser
                    if (attachment.CanPreview && !string.IsNullOrEmpty(attachment.Content))
                    {
                        OpenAttachmentInBrowser(attachment);
                    }
                    else
                    {
                        // Altrimenti, scarica e apri con l'applicazione predefinita
                        await DownloadAndOpenAttachment(attachment);
                    }
                }
                finally
                {
                    progressForm?.Close();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore gestione doppio click allegato: {ex.Message}");
                MessageBox.Show($"Errore apertura allegato:\n{ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Gestisce il click del mouse per future funzionalità (menu contestuale)
        /// </summary>
        private void OnAttachmentMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // TODO: Implementare menu contestuale per download, preview, etc.
                _logger.LogDebug("Click destro su allegato - menu contestuale futuro");
            }
        }

        /// <summary>
        /// Apre un allegato nel browser (per preview)
        /// </summary>
        private void OpenAttachmentInBrowser(JiraAttachment attachment)
        {
            try
            {
                _logger.LogInfo($"Apertura allegato in browser: {attachment.Filename}");
                Process.Start(new ProcessStartInfo(attachment.Content) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore apertura browser per {attachment.Filename}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Scarica e apre un allegato con l'applicazione predefinita
        /// </summary>
        private async Task DownloadAndOpenAttachment(JiraAttachment attachment)
        {
            try
            {
                _logger.LogInfo($"Download e apertura allegato: {attachment.Filename}");

                // Crea percorso temporaneo sicuro
                var tempPath = Path.GetTempPath();
                var safeFileName = GetSafeFileName(attachment.Filename);
                var filePath = Path.Combine(tempPath, safeFileName);

                // Download dell'allegato
                var success = await _activityService.GetAttachmentsAsync(attachment.Id);
                if (success != null)
                {
                    // Apri con l'applicazione predefinita
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                    _logger.LogInfo($"Allegato aperto: {filePath}");
                }
                else
                {
                    throw new Exception("Download fallito");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore download/apertura {attachment.Filename}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Crea un nome file sicuro per il sistema
        /// </summary>
        private string GetSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var safeName = fileName;

            foreach (var invalidChar in invalidChars)
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            return safeName;
        }

        /// <summary>
        /// Mostra una finestra di progress per il download
        /// </summary>
        private Form ShowDownloadProgress(string fileName)
        {
            // Implementazione semplificata - form di progress basilare
            var progressForm = new Form
            {
                Text = "Download in corso...",
                Size = new Size(300, 100),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = $"Download: {fileName}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            progressForm.Controls.Add(label);
            progressForm.Show();

            return progressForm;
        }

        /// <summary>
        /// Mostra un messaggio quando non ci sono allegati
        /// </summary>
        private void ShowNoAttachmentsMessage()
        {
            try
            {
                var noAttachmentsItem = new ListViewItem("📄 Nessun allegato");
                noAttachmentsItem.SubItems.Add("N/A");
                noAttachmentsItem.SubItems.Add("");
                noAttachmentsItem.SubItems.Add("");
                noAttachmentsItem.SubItems.Add("");
                noAttachmentsItem.ForeColor = Color.Gray;
                noAttachmentsItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                _attachmentsListView.Items.Add(noAttachmentsItem);
                _logger.LogDebug("Messaggio 'nessun allegato' visualizzato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione messaggio 'nessun allegato'", ex);
            }
        }

        #endregion
    }
}