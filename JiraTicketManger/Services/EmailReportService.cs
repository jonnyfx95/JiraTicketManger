using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using JiraTicketManager.Models;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per l'invio automatico di report email agli amministratori
    /// Gestisce report settimanali e alert critici
    /// </summary>
    public class EmailReportService
    {
        #region Fields

        private readonly LoggingService _logger;
        private readonly SettingsService _settingsService;
        private readonly AccessLogger _accessLogger;
        private EmailConfiguration _emailConfig;

        #endregion

        #region Constructor

        public EmailReportService()
        {
            _logger = LoggingService.CreateForComponent("EmailReportService", LoggingService.LogArea.System);
            _settingsService = SettingsService.CreateDefault();
            _accessLogger = AccessLogger.Instance;
           
        }

        #endregion

        #region Configuration


        #endregion

        #region Public Methods

        /// <summary>
        /// Controlla se è necessario inviare il report settimanale
        /// </summary>
        public async Task<bool> CheckAndSendWeeklyReportAsync()
        {
            try
            {
                if (!_emailConfig.EnableWeeklyReports || !IsConfigured())
                {
                    _logger.LogDebug("Report settimanali disabilitati o email non configurata");
                    return false;
                }

                if (!ShouldSendWeeklyReport())
                {
                    _logger.LogDebug("Non è ancora il momento per il report settimanale");
                    return false;
                }

                _logger.LogInfo("Invio report settimanale in corso...");
                var success = await SendWeeklyReportAsync();

                if (success)
                {
                    _emailConfig.LastWeeklyReportSent = DateTime.Now;
                    
                    _logger.LogInfo("Report settimanale inviato con successo");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError("CheckAndSendWeeklyReportAsync", ex);
                return false;
            }
        }

        /// <summary>
        /// Invia alert critico immediatamente
        /// </summary>
        public async Task<bool> SendCriticalAlertAsync(string alertType, string details)
        {
            try
            {
                if (!_emailConfig.EnableCriticalAlerts || !IsConfigured())
                {
                    _logger.LogDebug("Alert critici disabilitati o email non configurata");
                    return false;
                }

                if (!ShouldSendCriticalAlert())
                {
                    _logger.LogDebug("Alert critico in cooldown");
                    return false;
                }

                _logger.LogInfo($"Invio alert critico: {alertType}");
                var success = await SendCriticalAlertEmailAsync(alertType, details);

                if (success)
                {
                    _emailConfig.LastCriticalAlertSent = DateTime.Now;
                 
                    _logger.LogInfo("Alert critico inviato con successo");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError("SendCriticalAlertAsync", ex);
                return false;
            }
        }

        /// <summary>
        /// Test della configurazione email
        /// </summary>
        public async Task<bool> TestEmailConfigurationAsync()
        {
            try
            {
                if (!IsConfigured())
                {
                    _logger.LogWarning("Configurazione email non completa per il test");
                    return false;
                }

                _logger.LogInfo("Test configurazione email...");

                var subject = "[Test] Jira Manager - Configurazione Email";
                var body = CreateTestEmailBody();

                var success = await SendEmailAsync(subject, body, false);
                _logger.LogInfo($"Test email completato: {(success ? "Successo" : "Fallito")}");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError("TestEmailConfigurationAsync", ex);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Verifica se la configurazione email è completa
        /// </summary>
        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_emailConfig.SenderEmail) &&
                   !string.IsNullOrEmpty(_emailConfig.SenderPassword_Encrypted) &&
                   _emailConfig.AdminEmails?.Count > 0;
        }

        /// <summary>
        /// Determina se è il momento di inviare il report settimanale
        /// </summary>
        private bool ShouldSendWeeklyReport()
        {
            var now = DateTime.Now;

            // Controlla se è il giorno giusto
            if (now.DayOfWeek != _emailConfig.WeeklyReportDay)
                return false;

            // Controlla se è l'ora giusta (con tolleranza di 1 ora)
            var currentTime = now.TimeOfDay;
            var targetTime = _emailConfig.WeeklyReportTime;
            var timeDiff = Math.Abs((currentTime - targetTime).TotalMinutes);

            if (timeDiff > 60) // Tolleranza di 1 ora
                return false;

            // Controlla se non è già stato inviato questa settimana
            var lastSent = _emailConfig.LastWeeklyReportSent;
            var daysSinceLastReport = (now - lastSent).TotalDays;

            return daysSinceLastReport >= 6; // Almeno 6 giorni dall'ultimo invio
        }

        /// <summary>
        /// Determina se è possibile inviare un alert critico (non in cooldown)
        /// </summary>
        private bool ShouldSendCriticalAlert()
        {
            var now = DateTime.Now;
            var lastAlert = _emailConfig.LastCriticalAlertSent;
            var timeSinceLastAlert = now - lastAlert;

            return timeSinceLastAlert >= _emailConfig.AlertCooldownPeriod;
        }

        #endregion

        #region Email Sending

        /// <summary>
        /// Invia il report settimanale
        /// </summary>
        private async Task<bool> SendWeeklyReportAsync()
        {
            try
            {
                var subject = $"[Jira Manager] Report Settimanale - {Environment.MachineName}";
                var body = CreateWeeklyReportBody();
                var attachmentPath = _accessLogger.GetAccessLogFilePath();

                return await SendEmailAsync(subject, body, true, attachmentPath);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendWeeklyReportAsync", ex);
                return false;
            }
        }

        /// <summary>
        /// Invia alert critico
        /// </summary>
        private async Task<bool> SendCriticalAlertEmailAsync(string alertType, string details)
        {
            try
            {
                var subject = $"[ALERT] Jira Manager - {alertType} - {Environment.MachineName}";
                var body = CreateCriticalAlertBody(alertType, details);

                return await SendEmailAsync(subject, body, false);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendCriticalAlertEmailAsync", ex);
                return false;
            }
        }

        /// <summary>
        /// Metodo core per l'invio email
        /// </summary>
        private async Task<bool> SendEmailAsync(string subject, string body, bool isHtml, string attachmentPath = null)
        {
            try
            {
                using (var client = new SmtpClient(_emailConfig.SmtpServer, _emailConfig.SmtpPort))
                {
                    client.EnableSsl = _emailConfig.EnableSsl;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(
                        _emailConfig.SenderEmail,
                        _emailConfig.SenderPassword_Encrypted // TODO: Decrittare
                    );

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_emailConfig.SenderEmail, _emailConfig.SenderDisplayName);

                        // Aggiungi destinatari
                        foreach (var adminEmail in _emailConfig.AdminEmails)
                        {
                            if (!string.IsNullOrEmpty(adminEmail))
                            {
                                message.To.Add(adminEmail);
                            }
                        }

                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = isHtml;

                        // Aggiungi allegato se specificato
                        if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
                        {
                            var attachment = new Attachment(attachmentPath);
                            message.Attachments.Add(attachment);
                        }

                        await client.SendMailAsync(message);
                        _logger.LogInfo($"Email inviata con successo a {message.To.Count} destinatari");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SendEmailAsync", ex);
                return false;
            }
        }

        #endregion

        #region Email Body Creation

        /// <summary>
        /// Crea il corpo del report settimanale
        /// </summary>
        private string CreateWeeklyReportBody()
        {
            try
            {
                var sb = new StringBuilder();
                var now = DateTime.Now;
                var weekStart = now.AddDays(-(int)now.DayOfWeek + 1).Date; // Lunedì
                var weekEnd = weekStart.AddDays(6); // Domenica

                // Header HTML
                sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
                sb.AppendLine($"<h2>📊 Report Settimanale Jira Manager</h2>");
                sb.AppendLine($"<p><strong>PC:</strong> {Environment.MachineName}</p>");
                sb.AppendLine($"<p><strong>Periodo:</strong> {weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}</p>");
                sb.AppendLine($"<p><strong>Generato:</strong> {now:dd/MM/yyyy HH:mm:ss}</p>");
                sb.AppendLine("<hr>");

                // Analizza log della settimana
                var weeklyStats = AnalyzeWeeklyLogs(weekStart, weekEnd);

                // Sezione utenti
                sb.AppendLine("<h3>👥 Accessi Utenti</h3>");
                if (weeklyStats.UserLogins.Any())
                {
                    sb.AppendLine("<ul>");
                    foreach (var user in weeklyStats.UserLogins)
                    {
                        sb.AppendLine($"<li><strong>{user.Key}:</strong> {user.Value} login</li>");
                    }
                    sb.AppendLine("</ul>");
                }
                else
                {
                    sb.AppendLine("<p>Nessun accesso registrato questa settimana.</p>");
                }

                // Sezione problemi
                sb.AppendLine("<h3>⚠️ Problemi Rilevati</h3>");
                if (weeklyStats.FailedLogins > 0 || weeklyStats.Errors > 0)
                {
                    sb.AppendLine("<ul>");
                    if (weeklyStats.FailedLogins > 0)
                        sb.AppendLine($"<li>Login falliti: {weeklyStats.FailedLogins}</li>");
                    if (weeklyStats.Errors > 0)
                        sb.AppendLine($"<li>Errori applicazione: {weeklyStats.Errors}</li>");
                    sb.AppendLine("</ul>");
                }
                else
                {
                    sb.AppendLine("<p style='color: green;'>✅ Nessun problema rilevato questa settimana.</p>");
                }

                // Statistiche
                sb.AppendLine("<h3>📈 Statistiche</h3>");
                sb.AppendLine("<ul>");
                sb.AppendLine($"<li>Sessioni totali: {weeklyStats.TotalSessions}</li>");
                sb.AppendLine($"<li>Tempo medio sessione: {weeklyStats.AverageSessionDuration:hh\\:mm\\:ss}</li>");
                sb.AppendLine("</ul>");

                // Footer
                sb.AppendLine("<hr>");
                sb.AppendLine("<p><small>📎 File log completo in allegato.</small></p>");
                sb.AppendLine("<p><small>Questo è un report automatico generato da Jira Ticket Manager.</small></p>");
                sb.AppendLine("</body></html>");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateWeeklyReportBody", ex);
                return $"Errore nella generazione del report: {ex.Message}";
            }
        }

        /// <summary>
        /// Crea il corpo dell'alert critico
        /// </summary>
        private string CreateCriticalAlertBody(string alertType, string details)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            sb.AppendLine($"<h2 style='color: red;'>🚨 ALERT CRITICO</h2>");
            sb.AppendLine($"<p><strong>Tipo:</strong> {alertType}</p>");
            sb.AppendLine($"<p><strong>PC:</strong> {Environment.MachineName}</p>");
            sb.AppendLine($"<p><strong>Timestamp:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            sb.AppendLine("<hr>");
            sb.AppendLine("<h3>Dettagli:</h3>");
            sb.AppendLine($"<p>{details}</p>");
            sb.AppendLine("<hr>");
            sb.AppendLine("<p><strong>Azione richiesta:</strong> Verificare i log e investigare il problema.</p>");
            sb.AppendLine("<p><small>Questo è un alert automatico generato da Jira Ticket Manager.</small></p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        /// <summary>
        /// Crea il corpo dell'email di test
        /// </summary>
        private string CreateTestEmailBody()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<html><body style='font-family: Arial, sans-serif;'>");
            sb.AppendLine("<h2>✅ Test Configurazione Email</h2>");
            sb.AppendLine($"<p>La configurazione email è stata testata con successo!</p>");
            sb.AppendLine($"<p><strong>PC:</strong> {Environment.MachineName}</p>");
            sb.AppendLine($"<p><strong>Timestamp:</strong> {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
            sb.AppendLine("<p>Se ricevi questa email, il sistema di notifiche è configurato correttamente.</p>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }

        #endregion

        #region Log Analysis

        /// <summary>
        /// Statistiche settimanali dai log
        /// </summary>
        private class WeeklyStats
        {
            public Dictionary<string, int> UserLogins { get; set; } = new Dictionary<string, int>();
            public int FailedLogins { get; set; }
            public int Errors { get; set; }
            public int TotalSessions { get; set; }
            public TimeSpan AverageSessionDuration { get; set; }
        }

        /// <summary>
        /// Analizza i log per le statistiche settimanali
        /// </summary>
        private WeeklyStats AnalyzeWeeklyLogs(DateTime weekStart, DateTime weekEnd)
        {
            var stats = new WeeklyStats();

            try
            {
                var entries = _accessLogger.GetRecentAccessEntries(500); // Analizza ultime 500 entries

                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry) || !entry.StartsWith("["))
                        continue;

                    // Parse timestamp
                    var timestampEnd = entry.IndexOf(']');
                    if (timestampEnd > 0)
                    {
                        var timestampStr = entry.Substring(1, timestampEnd - 1);
                        if (DateTime.TryParse(timestampStr, out var timestamp))
                        {
                            if (timestamp >= weekStart && timestamp <= weekEnd)
                            {
                                AnalyzeLogEntry(entry, stats);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("AnalyzeWeeklyLogs", ex);
            }

            return stats;
        }

        /// <summary>
        /// Analizza una singola entry di log
        /// </summary>
        private void AnalyzeLogEntry(string entry, WeeklyStats stats)
        {
            try
            {
                if (entry.Contains("LOGIN_SUCCESS"))
                {
                    stats.TotalSessions++;

                    // Estrai email utente
                    var parts = entry.Split('|');
                    if (parts.Length >= 3)
                    {
                        var userEmail = parts[2].Trim();
                        if (!string.IsNullOrEmpty(userEmail) && userEmail != "SYSTEM")
                        {
                            if (stats.UserLogins.ContainsKey(userEmail))
                                stats.UserLogins[userEmail]++;
                            else
                                stats.UserLogins[userEmail] = 1;
                        }
                    }
                }
                else if (entry.Contains("LOGIN_FAILED"))
                {
                    stats.FailedLogins++;
                }
                else if (entry.Contains("AUTH_ERROR"))
                {
                    stats.Errors++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("AnalyzeLogEntry", ex);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Configurazione email corrente
        /// </summary>
        public EmailConfiguration CurrentConfiguration => _emailConfig;

        /// <summary>
        /// Verifica se il sistema email è configurato
        /// </summary>
        public bool IsEmailConfigured => IsConfigured();

        #endregion
    }
}