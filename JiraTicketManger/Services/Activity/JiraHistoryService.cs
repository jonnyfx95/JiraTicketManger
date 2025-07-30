using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Servizio specializzato per la gestione della cronologia Jira.
    /// Tradotto direttamente da FrmDettaglio.vb LoadHistoryAsync().
    /// </summary>
    public class JiraHistoryService : IHistoryService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;

        public JiraHistoryService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("JiraHistoryService");
        }

        public async Task<List<JiraHistoryItem>> GetHistoryAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento cronologia per ticket: {ticketKey}");
                progress?.Report("Caricamento cronologia...");

                // TRADUZIONE DIRETTA da VB.NET LoadHistoryAsync
                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}?expand=changelog";

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Accept = "application/json";
                request.Timeout = 15000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = await reader.ReadToEndAsync();
                    var historyData = JObject.Parse(json);

                    var history = ParseHistoryFromJson(historyData["changelog"]);

                    _logger.LogInfo($"Caricati {history.Count} elementi cronologia per {ticketKey}");
                    return history;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento cronologia per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<List<JiraHistoryItem>> GetHistoryWithChangelogAsync(string ticketKey, IProgress<string> progress = null)
        {
            // Questo metodo è già incluso nella implementazione principale
            return await GetHistoryAsync(ticketKey, progress);
        }

        public async Task<int> GetHistoryCountAsync(string ticketKey)
        {
            try
            {
                var history = await GetHistoryAsync(ticketKey);
                return history.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conteggio cronologia per {ticketKey}", ex);
                return 0;
            }
        }

        private List<JiraHistoryItem> ParseHistoryFromJson(JToken changelogToken)
        {
            var historyItems = new List<JiraHistoryItem>();

            if (changelogToken?["histories"] == null)
            {
                _logger.LogInfo("Nessuna cronologia da processare");
                return historyItems;
            }

            foreach (var historyToken in changelogToken["histories"])
            {
                try
                {
                    var historyItem = JiraHistoryItem.FromJToken(historyToken);
                    historyItems.Add(historyItem);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore parsing elemento cronologia: {ex.Message}");
                    // Continua con il prossimo elemento
                }
            }

            return historyItems;
        }

        private string GetAuthorizationHeader()
        {
            // Utilizza il metodo pubblico esistente di JiraApiService
            return _jiraApiService.GetAuthorizationHeader();
        }
    }
}