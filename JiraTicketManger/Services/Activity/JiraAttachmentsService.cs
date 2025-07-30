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
    /// Servizio specializzato per la gestione degli allegati Jira.
    /// Tradotto direttamente da FrmDettaglio.vb LoadAttachmentsAsync().
    /// </summary>
    public class JiraAttachmentsService : IAttachmentsService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;

        public JiraAttachmentsService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("JiraAttachmentsService");
        }

        public async Task<List<JiraAttachment>> GetAttachmentsAsync(string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento allegati per ticket: {ticketKey}");
                progress?.Report("Caricamento allegati...");

                // TRADUZIONE DIRETTA da VB.NET LoadAttachmentsAsync
                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}?fields=attachment";

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Accept = "application/json";
                request.Timeout = 15000;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    var json = await reader.ReadToEndAsync();
                    var ticketData = JObject.Parse(json);

                    var attachments = ParseAttachmentsFromJson(ticketData["fields"]?["attachment"]);

                    _logger.LogInfo($"Caricati {attachments.Count} allegati per {ticketKey}");
                    return attachments;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento allegati per {ticketKey}", ex);
                throw;
            }
        }

        public async Task<byte[]> DownloadAttachmentAsync(JiraAttachment attachment, IProgress<int> downloadProgress = null)
        {
            try
            {
                _logger.LogInfo($"Download allegato: {attachment.Filename}");

                var request = (HttpWebRequest)WebRequest.Create(attachment.Content);
                request.Method = "GET";
                request.Headers.Add("Authorization", GetAuthorizationHeader());
                request.Timeout = 60000; // 60 secondi per download

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    var totalBytes = response.ContentLength;
                    var downloadedBytes = 0L;

                    using (var responseStream = response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        var buffer = new byte[8192];
                        int bytesRead;

                        while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await memoryStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;

                            // Report progress se fornito
                            if (downloadProgress != null && totalBytes > 0)
                            {
                                var percentComplete = (int)((downloadedBytes * 100) / totalBytes);
                                downloadProgress.Report(percentComplete);
                            }
                        }

                        _logger.LogInfo($"Download completato per: {attachment.Filename}");
                        return memoryStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore download allegato {attachment.Filename}", ex);
                throw;
            }
        }

        public async Task<bool> SaveAttachmentAsync(JiraAttachment attachment, string filePath, IProgress<int> downloadProgress = null)
        {
            try
            {
                var data = await DownloadAttachmentAsync(attachment, downloadProgress);
                await File.WriteAllBytesAsync(filePath, data);

                _logger.LogInfo($"Allegato salvato: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore salvataggio allegato {attachment.Filename}", ex);
                return false;
            }
        }

        public async Task<int> GetAttachmentsCountAsync(string ticketKey)
        {
            try
            {
                var attachments = await GetAttachmentsAsync(ticketKey);
                return attachments.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conteggio allegati per {ticketKey}", ex);
                return 0;
            }
        }

        private List<JiraAttachment> ParseAttachmentsFromJson(JToken attachmentsToken)
        {
            var attachments = new List<JiraAttachment>();

            if (attachmentsToken == null || !attachmentsToken.Any())
            {
                _logger.LogInfo("Nessun allegato da processare");
                return attachments;
            }

            _logger.LogDebug($"Trovati {attachmentsToken.Count()} allegati da processare");

            foreach (var attachmentToken in attachmentsToken)
            {
                try
                {
                    var attachment = JiraAttachment.FromJToken(attachmentToken);
                    attachments.Add(attachment);

                    _logger.LogDebug($"Allegato processato: {attachment.Filename} - {attachment.HumanReadableSize}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore parsing allegato: {ex.Message}");
                    // Continua con il prossimo allegato
                }
            }

            return attachments;
        }

        private string GetAuthorizationHeader()
        {
            // Utilizza il metodo esistente di JiraApiService per l'autenticazione
            return _jiraApiService.GetAuthorizationHeader();
        }
    }
}