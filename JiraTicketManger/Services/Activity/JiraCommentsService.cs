using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Servizio specializzato per la gestione dei commenti Jira.
    /// Tradotto direttamente da FrmDettaglio.vb LoadCommentsAsync().
    /// </summary>
    public class JiraCommentsService : ICommentsService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;

        public JiraCommentsService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("JiraCommentsService");
        }

        public async Task<List<JiraComment>> GetCommentsAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento commenti per ticket: {ticketKey}");
                progress?.Report("Caricamento commenti...");

                // TRADUZIONE DIRETTA da VB.NET: URL con expand per visibility
                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}/comment?expand=comments.visibility";
                _logger.LogDebug($"URL commenti: {url}");

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Accept = "application/json";
                request.Timeout = 15000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = await reader.ReadToEndAsync();
                    _logger.LogDebug($"Risposta API ricevuta, lunghezza: {json.Length}");

                    var commentsData = JObject.Parse(json);
                    var comments = ParseCommentsFromJson(commentsData["comments"]);

                    _logger.LogInfo($"Caricati {comments.Count} commenti per {ticketKey}");
                    return comments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento commenti per {ticketKey}", ex);

                // TRADUZIONE DIRETTA da VB.NET: Fallback endpoint
                _logger.LogInfo("Tentativo con endpoint alternativo...");
                return await GetCommentsFromFallbackEndpointAsync(ticketKey, progress);
            }
        }

        public async Task<List<JiraComment>> GetCommentsWithVisibilityAsync(string ticketKey, IProgress<string> progress = null)
        {
            // Questo metodo è già incluso nella implementazione principale
            return await GetCommentsAsync(ticketKey, progress);
        }

        public async Task<int> GetCommentsCountAsync(string ticketKey)
        {
            try
            {
                var comments = await GetCommentsAsync(ticketKey);
                return comments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conteggio commenti per {ticketKey}", ex);
                return 0;
            }
        }

        private async Task<List<JiraComment>> GetCommentsFromFallbackEndpointAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("Usando endpoint separato per commenti...");

                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}/comment";
                _logger.LogDebug($"URL fallback: {url}");

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Accept = "application/json";
                request.Timeout = 15000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = await reader.ReadToEndAsync();
                    var commentsData = JObject.Parse(json);

                    if (commentsData["comments"] != null)
                    {
                        _logger.LogInfo($"Numero commenti da endpoint alternativo: {commentsData["comments"].Count()}");
                        return ParseCommentsFromJson(commentsData["comments"]);
                    }
                    else
                    {
                        _logger.LogWarning("Campo 'comments' non trovato nella risposta alternativa");
                        return new List<JiraComment>();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore endpoint alternativo commenti per {ticketKey}", ex);
                throw;
            }
        }

        private List<JiraComment> ParseCommentsFromJson(JToken commentsToken)
        {
            var comments = new List<JiraComment>();

            if (commentsToken == null || !commentsToken.Any())
            {
                _logger.LogInfo("Nessun commento da processare");
                return comments;
            }

            // TRADUZIONE DIRETTA da VB.NET: Ordina per data decrescente
            var sortedComments = commentsToken.OrderByDescending(c => DateTime.Parse(c["created"].ToString()));

            foreach (var commentToken in sortedComments)
            {
                try
                {
                    var comment = JiraComment.FromJToken(commentToken);
                    comments.Add(comment);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore parsing commento: {ex.Message}");
                    // Continua con il prossimo commento
                }
            }

            return comments;
        }

        private string GetAuthorizationHeader()
        {
            // Utilizza il metodo pubblico esistente di JiraApiService
            return _jiraApiService.GetAuthorizationHeader();
        }
    }
}
