using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Implementazione principale del servizio attività Jira.
    /// Utilizza JiraApiService esistente e segue i pattern del progetto.
    /// Tradotto dalla logica VB.NET LoadCommentsAndHistoryAsync().
    /// </summary>
    public class JiraActivityService : IActivityService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;
        private readonly ICommentsService _commentsService;
        private readonly IHistoryService _historyService;
        private readonly IAttachmentsService _attachmentsService;

        public JiraActivityService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("JiraActivityService");

            // Inizializza i servizi specifici
            _commentsService = new JiraCommentsService(_jiraApiService);
            _historyService = new JiraHistoryService(_jiraApiService);
            _attachmentsService = new JiraAttachmentsService(_jiraApiService);

            _logger.LogInfo("JiraActivityService inizializzato con tutti i sub-services");
        }

        public async Task<List<JiraComment>> GetCommentsAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento commenti per ticket: {ticketKey}");
                progress?.Report("Caricamento commenti...");

                var comments = await _commentsService.GetCommentsAsync(ticketKey, progress);

                _logger.LogInfo($"Caricati {comments.Count} commenti per {ticketKey}");
                progress?.Report($"Caricati {comments.Count} commenti");

                return comments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento commenti per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<List<JiraHistoryItem>> GetHistoryAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento cronologia per ticket: {ticketKey}");
                progress?.Report("Caricamento cronologia...");

                var history = await _historyService.GetHistoryAsync(ticketKey, progress);

                _logger.LogInfo($"Caricati {history.Count} elementi cronologia per {ticketKey}");
                progress?.Report($"Caricati {history.Count} elementi cronologia");

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento cronologia per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<List<JiraAttachment>> GetAttachmentsAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento allegati per ticket: {ticketKey}");
                progress?.Report("Caricamento allegati...");

                var attachments = await _attachmentsService.GetAttachmentsAsync(ticketKey, progress);

                _logger.LogInfo($"Caricati {attachments.Count} allegati per {ticketKey}");
                progress?.Report($"Caricati {attachments.Count} allegati");

                return attachments;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento allegati per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<ActivitySummary> GetActivitySummaryAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento sommario attività per ticket: {ticketKey}");
                progress?.Report("Caricamento sommario attività...");

                // Caricamento parallelo come nel progetto esistente
                var commentsTask = _commentsService.GetCommentsCountAsync(ticketKey);
                var historyTask = _historyService.GetHistoryCountAsync(ticketKey);
                var attachmentsTask = _attachmentsService.GetAttachmentsCountAsync(ticketKey);

                await Task.WhenAll(commentsTask, historyTask, attachmentsTask);

                var summary = new ActivitySummary
                {
                    TicketKey = ticketKey,
                    CommentsCount = await commentsTask,
                    HistoryCount = await historyTask,
                    AttachmentsCount = await attachmentsTask,
                    LastUpdated = DateTime.Now
                };

                progress?.Report("Sommario attività caricato");
                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento sommario attività per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<bool> TestActivityEndpointsAsync(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Test endpoints attività per ticket: {ticketKey}");

                // Test rapido degli endpoint utilizzando timeout ridotto
                var testTasks = new List<Task<bool>>
                {
                    TestEndpointAsync($"/rest/api/2/issue/{ticketKey}/comment"),
                    TestEndpointAsync($"/rest/api/2/issue/{ticketKey}?expand=changelog"),
                    TestEndpointAsync($"/rest/api/2/issue/{ticketKey}?fields=attachment")
                };

                var results = await Task.WhenAll(testTasks);
                var allSuccess = results.All(r => r);

                _logger.LogInfo($"Test endpoints completato: {(allSuccess ? "SUCCESS" : "FAILED")}");
                return allSuccess;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore test endpoints per {ticketKey}", ex);
                return false;
            }
        }

        private async Task<bool> TestEndpointAsync(string endpoint)
        {
            try
            {
                // Utilizza il metodo di test esistente di JiraApiService
                // Implementazione semplificata - da adattare al metodo esistente
                await Task.Delay(100); // Placeholder per test endpoint
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}