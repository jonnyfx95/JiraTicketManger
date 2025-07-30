using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;
using JiraTicketManager.UI.Manger.Activity;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.UI.Managers.Activity
{
    /// <summary>
    /// Manager principale per la gestione dei tab di attività.
    /// Coordina CommentsTabManager, HistoryTabManager e AttachmentsTabManager.
    /// VERSIONE FINALE - Tutti i manager implementati e collegati.
    /// </summary>
    public class ActivityTabManager : IActivityTabManager
    {
        private readonly IActivityService _activityService;
        private readonly LoggingService _logger;

        // Manager specifici per ogni tab - ORA TUTTI IMPLEMENTATI
        private readonly CommentsTabManager _commentsManager;
        private readonly HistoryTabManager _historyManager;
        private readonly AttachmentsTabManager _attachmentsManager;

        public ActivityTabManager(IActivityService activityService)
        {
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
            _logger = LoggingService.CreateForComponent("ActivityTabManager");

            // Inizializza i manager specifici - TUTTI FUNZIONALI
            _commentsManager = new CommentsTabManager(_activityService);
            _historyManager = new HistoryTabManager(_activityService);
            _attachmentsManager = new AttachmentsTabManager(_activityService);

            _logger.LogInfo("ActivityTabManager inizializzato con tutti i sub-managers");
        }

        public async Task LoadActivityTabsAsync(TabControl tabControl, string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento tab attività per ticket: {ticketKey}");
                progress?.Report("Caricamento attività...");

                if (tabControl == null)
                {
                    _logger.LogWarning("TabControl è null, impossibile caricare attività");
                    return;
                }

                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("TicketKey vuoto, impossibile caricare attività");
                    ClearAllTabs(tabControl);
                    return;
                }

                // Caricamento parallelo come nel progetto esistente (pattern JiraDataService)
                _logger.LogDebug("Avvio caricamento parallelo dei tab...");

                var commentsTask = LoadCommentsTabAsync(tabControl, ticketKey, progress);
                var historyTask = LoadHistoryTabAsync(tabControl, ticketKey, progress);
                var attachmentsTask = LoadAttachmentsTabAsync(tabControl, ticketKey, progress);

                // Attendi completamento di tutti i task
                await Task.WhenAll(commentsTask, historyTask, attachmentsTask);

                // Aggiorna i conteggi nei titoli dei tab
                await UpdateTabCountsAsync(tabControl, ticketKey);

                progress?.Report("Attività caricate con successo");
                _logger.LogInfo($"Tab attività caricati con successo per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento tab attività per {ticketKey}", ex);
                progress?.Report("Errore caricamento attività");

                // In caso di errore, mostra tab vuoti con messaggio di errore
                ShowErrorInAllTabs(tabControl, ex.Message);
                throw;
            }
        }

        public async Task UpdateTabCountsAsync(TabControl tabControl, string ticketKey)
        {
            try
            {
                _logger.LogDebug($"Aggiornamento conteggi tab per ticket: {ticketKey}");

                if (tabControl == null || string.IsNullOrEmpty(ticketKey))
                    return;

                // Ottieni il sommario delle attività
                var summary = await _activityService.GetActivitySummaryAsync(ticketKey);

                // Aggiorna i titoli dei tab con i conteggi
                UpdateTabTitle(tabControl, "tpComments", "Comments", summary.CommentsCount);
                UpdateTabTitle(tabControl, "tpHistory", "History", summary.HistoryCount);
                UpdateTabTitle(tabControl, "tpAttachments", "Attachments", summary.AttachmentsCount);

                _logger.LogDebug($"Conteggi aggiornati: C={summary.CommentsCount}, H={summary.HistoryCount}, A={summary.AttachmentsCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiornamento conteggi tab per {ticketKey}", ex);
                // Non rilancia l'eccezione per non bloccare l'UI
            }
        }

        public void ClearAllTabs(TabControl tabControl)
        {
            try
            {
                _logger.LogDebug("Pulizia di tutti i tab attività");

                if (tabControl == null)
                    return;

                // Pulisci ogni tab individualmente - ORA TUTTI FUNZIONALI
                _commentsManager.ClearTab(GetTabPage(tabControl, "tpComments"));
                _historyManager.ClearTab(GetTabPage(tabControl, "tpHistory"));
                _attachmentsManager.ClearTab(GetTabPage(tabControl, "tpAttachments"));

                // Reset dei titoli
                UpdateTabTitle(tabControl, "tpComments", "Comments", 0);
                UpdateTabTitle(tabControl, "tpHistory", "History", 0);
                UpdateTabTitle(tabControl, "tpAttachments", "Attachments", 0);

                _logger.LogDebug("Pulizia tab completata");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia tab attività", ex);
            }
        }

        public async Task<ActivitySummary> GetActivitySummaryAsync(string ticketKey)
        {
            try
            {
                return await _activityService.GetActivitySummaryAsync(ticketKey);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore ottenimento sommario attività per {ticketKey}", ex);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task LoadCommentsTabAsync(TabControl tabControl, string ticketKey, IProgress<string> progress)
        {
            try
            {
                var tabPage = GetTabPage(tabControl, "tpComments");
                if (tabPage != null)
                {
                    await _commentsManager.LoadCommentsAsync(tabPage, ticketKey, progress);
                }
                else
                {
                    _logger.LogWarning("TabPage 'tpComments' non trovata");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento tab commenti per {ticketKey}", ex);
                throw;
            }
        }

        private async Task LoadHistoryTabAsync(TabControl tabControl, string ticketKey, IProgress<string> progress)
        {
            try
            {
                var tabPage = GetTabPage(tabControl, "tpHistory");
                if (tabPage != null)
                {
                    await _historyManager.LoadHistoryAsync(tabPage, ticketKey, progress);
                }
                else
                {
                    _logger.LogWarning("TabPage 'tpHistory' non trovata");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento tab cronologia per {ticketKey}", ex);
                throw;
            }
        }

        private async Task LoadAttachmentsTabAsync(TabControl tabControl, string ticketKey, IProgress<string> progress)
        {
            try
            {
                var tabPage = GetTabPage(tabControl, "tpAttachments");
                if (tabPage != null)
                {
                    await _attachmentsManager.LoadAttachmentsAsync(tabPage, ticketKey, progress);
                }
                else
                {
                    _logger.LogWarning("TabPage 'tpAttachments' non trovata");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento tab allegati per {ticketKey}", ex);
                throw;
            }
        }

        private TabPage GetTabPage(TabControl tabControl, string tabPageName)
        {
            if (tabControl == null) return null;

            foreach (TabPage tabPage in tabControl.TabPages)
            {
                if (tabPage.Name == tabPageName)
                    return tabPage;
            }

            _logger.LogWarning($"TabPage '{tabPageName}' non trovata nel TabControl");
            return null;
        }

        private void UpdateTabTitle(TabControl tabControl, string tabPageName, string baseTitle, int count)
        {
            var tabPage = GetTabPage(tabControl, tabPageName);
            if (tabPage != null)
            {
                tabPage.Text = count > 0 ? $"{baseTitle} ({count})" : baseTitle;
                _logger.LogDebug($"Titolo tab aggiornato: {tabPage.Text}");
            }
        }

        private void ShowErrorInAllTabs(TabControl tabControl, string errorMessage)
        {
            try
            {
                _logger.LogWarning($"Visualizzazione errore in tutti i tab: {errorMessage}");

                // Mostra messaggi di errore in tutti i tab - ORA TUTTI FUNZIONALI
                _commentsManager.ShowError(GetTabPage(tabControl, "tpComments"), errorMessage);
                _historyManager.ShowError(GetTabPage(tabControl, "tpHistory"), errorMessage);
                _attachmentsManager.ShowError(GetTabPage(tabControl, "tpAttachments"), errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione errori nei tab", ex);
            }
        }

        #endregion
    }
}