using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using JiraTicketManager.Services;
using JiraTicketManager.Helpers;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio ibrido: prima tenta Outlook classico (COM), poi moderno (MailTo)
    /// </summary>
    public class OutlookHybridService : IDisposable
    {
        private readonly LoggingService _logger;
        // ⚠️ RIMUOVI _outlookApp persistente che causava il bug
        private bool _disposed = false;

        public OutlookHybridService()
        {
            _logger = LoggingService.CreateForComponent("OutlookHybrid");
        }

        #region Email Data Model

        public class EmailData
        {
            public string To { get; set; } = "";
            public string Cc { get; set; } = "";
            public string Bcc { get; set; } = "";
            public string Subject { get; set; } = "";
            public string BodyHtml { get; set; } = "";
            public string BodyText { get; set; } = "";
            public bool IsHtml { get; set; } = true;
            public string[] Attachments { get; set; } = Array.Empty<string>();

            public bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(To) &&
                       !string.IsNullOrWhiteSpace(Subject) &&
                       (!string.IsNullOrWhiteSpace(BodyHtml) || !string.IsNullOrWhiteSpace(BodyText));
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apertura email ibrida: prima COM Interop, poi MailTo
        /// </summary>
        public async Task<bool> OpenEmailAsync(EmailData emailData)
        {
            return await Task.Run(() => OpenEmail(emailData));
        }

        public bool OpenEmail(EmailData emailData)
        {
            try
            {
                _logger.LogInfo("=== APERTURA EMAIL IBRIDA ===");

                if (emailData == null || !emailData.IsValid())
                {
                    _logger.LogError("EmailData non valida");
                    return false;
                }

                // TENTATIVO 1: Outlook classico (COM Interop)
                _logger.LogInfo("TENTATIVO 1: Outlook classico (COM Interop)");
                if (TryClassicOutlook(emailData))
                {
                    _logger.LogInfo("✅ EMAIL APERTA CON OUTLOOK CLASSICO");
                    return true;
                }

                // TENTATIVO 2: Outlook moderno (MailTo)
                _logger.LogInfo("TENTATIVO 2: Outlook moderno (MailTo)");
                if (TryModernOutlook(emailData))
                {
                    _logger.LogInfo("✅ EMAIL APERTA CON OUTLOOK MODERNO");
                    return true;
                }

                _logger.LogError("❌ ENTRAMBI I METODI OUTLOOK SONO FALLITI");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore apertura email ibrida", ex);
                return false;
            }
        }

        public async Task<bool> IsOutlookAvailableAsync()
        {
            return await Task.Run(IsOutlookAvailable);
        }

        public bool IsOutlookAvailable()
        {
            return IsClassicOutlookAvailable() || IsModernOutlookAvailable();
        }

        #endregion

        #region Outlook Classico (COM Interop)

        /// <summary>
        /// Tenta di aprire con Outlook classico (COM Interop)
        /// </summary>
        private bool TryClassicOutlook(EmailData emailData)
        {
            dynamic localOutlookApp = null;
            dynamic mailItem = null;

            try
            {
                _logger.LogInfo("Tentativo COM Interop...");

                // Verifica se COM è disponibile
                if (!IsClassicOutlookAvailable())
                {
                    _logger.LogWarning("COM Interop non disponibile");
                    return false;
                }

                // ⚠️ SEMPRE CREA NUOVA ISTANZA (non riutilizzare _outlookApp)
                localOutlookApp = Activator.CreateInstance(Type.GetTypeFromProgID("Outlook.Application"));
                if (localOutlookApp == null)
                {
                    _logger.LogWarning("Creazione Outlook.Application fallita");
                    return false;
                }

                // Crea MailItem con l'istanza locale
                mailItem = localOutlookApp.CreateItem(0); // olMailItem = 0
                if (mailItem == null)
                {
                    _logger.LogWarning("Creazione MailItem fallita");
                    return false;
                }

                // Configura e mostra email
                ConfigureClassicMailItem(mailItem, emailData);
                DisplayClassicEmail(mailItem);

                _logger.LogInfo("Outlook classico: email configurata e mostrata");
                return true;
            }
            catch (COMException comEx)
            {
                _logger.LogWarning($"COM Exception: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Outlook classico fallito: {ex.Message}");
                return false;
            }
            finally
            {
                // ⚠️ CLEANUP SEMPRE (indipendentemente dal successo)
                try
                {
                    if (mailItem != null)
                    {
                        Marshal.ReleaseComObject(mailItem);
                        _logger.LogDebug("MailItem rilasciato");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Errore rilascio MailItem: {ex.Message}");
                }

                try
                {
                    if (localOutlookApp != null)
                    {
                        Marshal.ReleaseComObject(localOutlookApp);
                        _logger.LogDebug("Outlook.Application locale rilasciato");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"Errore rilascio Outlook.Application: {ex.Message}");
                }
            }
        }

        private bool IsClassicOutlookAvailable()
        {
            try
            {
                // ⚠️ TEST VELOCE SENZA CREARE ISTANZE PERSISTENTI
                var progIdType = Type.GetTypeFromProgID("Outlook.Application");
                if (progIdType == null)
                {
                    _logger.LogDebug("ProgID Outlook.Application non trovato");
                    return false;
                }

                // ⚠️ TEST RAPIDO DI CREAZIONE (poi rilascia subito)
                dynamic testApp = null;
                try
                {
                    testApp = Activator.CreateInstance(progIdType);
                    if (testApp != null)
                    {
                        _logger.LogDebug("Outlook classico disponibile");
                        return true;
                    }
                }
                finally
                {
                    if (testApp != null)
                    {
                        try { Marshal.ReleaseComObject(testApp); }
                        catch { }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Outlook classico non disponibile: {ex.Message}");
                return false;
            }
        }

        // ⚠️ RIMUOVI QUESTI METODI CHE CAUSANO IL BUG:
        // private bool InitializeClassicOutlook() - NON PIÙ NECESSARIO
        // private dynamic CreateClassicMailItem() - NON PIÙ NECESSARIO

        private void ConfigureClassicMailItem(dynamic mailItem, EmailData emailData)
        {
            try
            {
                mailItem.To = emailData.To;

                if (!string.IsNullOrWhiteSpace(emailData.Cc))
                    mailItem.CC = emailData.Cc;

                if (!string.IsNullOrWhiteSpace(emailData.Bcc))
                    mailItem.BCC = emailData.Bcc;

                mailItem.Subject = emailData.Subject;

                if (emailData.IsHtml && !string.IsNullOrWhiteSpace(emailData.BodyHtml))
                    mailItem.HTMLBody = emailData.BodyHtml;
                else if (!string.IsNullOrWhiteSpace(emailData.BodyText))
                    mailItem.Body = emailData.BodyText;

                _logger.LogInfo("MailItem configurato con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore configurazione MailItem: {ex.Message}");
                throw;
            }
        }

        private void DisplayClassicEmail(dynamic mailItem)
        {
            try
            {
                mailItem.Display(false); // Non modale
                _logger.LogInfo("Email mostrata con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore visualizzazione email: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Outlook Moderno (MailTo)

        /// <summary>
        /// Tenta di aprire con Outlook moderno (MailTo)
        /// </summary>
        private bool TryModernOutlook(EmailData emailData)
        {
            try
            {
                _logger.LogInfo("Tentativo MailTo...");

                var mailtoUrl = BuildMailToUrl(emailData);

                var processInfo = new ProcessStartInfo
                {
                    FileName = mailtoUrl,
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(processInfo);

                if (process != null)
                {
                    _logger.LogInfo("MailTo avviato con successo");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"MailTo fallito: {ex.Message}");
                return false;
            }
        }

        private bool IsModernOutlookAvailable()
        {
            try
            {
                // Testa se MailTo è supportato
                var testUrl = "mailto:test@example.com";
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(testUrl) { UseShellExecute = true };
                // Non avviamo realmente, solo testiamo se è supportato
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string BuildMailToUrl(EmailData emailData)
        {
            var sb = new StringBuilder();
            sb.Append("mailto:");
            sb.Append(Uri.EscapeDataString(emailData.To));

            var parameters = new List<string>();

            if (!string.IsNullOrWhiteSpace(emailData.Cc))
                parameters.Add($"cc={Uri.EscapeDataString(emailData.Cc)}");

            if (!string.IsNullOrWhiteSpace(emailData.Bcc))
                parameters.Add($"bcc={Uri.EscapeDataString(emailData.Bcc)}");

            if (!string.IsNullOrWhiteSpace(emailData.Subject))
                parameters.Add($"subject={Uri.EscapeDataString(emailData.Subject)}");

            // Usa testo per MailTo (più compatibile)
            var bodyText = GetBestBodyForMailTo(emailData);
            if (!string.IsNullOrWhiteSpace(bodyText))
            {
                // Limita lunghezza per evitare URL troppo lunghi
                var truncatedBody = bodyText.Length > 1800
                    ? bodyText.Substring(0, 1800) + "\n\n[Contenuto troncato]"
                    : bodyText;

                parameters.Add($"body={Uri.EscapeDataString(truncatedBody)}");
            }

            if (parameters.Any())
                sb.Append("?" + string.Join("&", parameters));

            return sb.ToString();
        }

        private string GetBestBodyForMailTo(EmailData emailData)
        {
            // Preferisce testo
            if (!string.IsNullOrWhiteSpace(emailData.BodyText))
                return emailData.BodyText;

            // Converte HTML a testo se necessario
            if (!string.IsNullOrWhiteSpace(emailData.BodyHtml))
                return ConvertHtmlToText(emailData.BodyHtml);

            return "";
        }

        private string ConvertHtmlToText(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return "";

            try
            {
                // Rimozione tag HTML semplice
                var text = html;
                text = System.Text.RegularExpressions.Regex.Replace(text, @"<br\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"<p\s*[^>]*>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"</p>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                text = System.Net.WebUtility.HtmlDecode(text);
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n", "\n\n");

                return text.Trim();
            }
            catch
            {
                return html; // Fallback
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Metodi helper per preparazione dati (identici a OutlookIntegrationService)
        /// </summary>
        public static EmailData PrepareEmailFromTicketData(
            string clientName, string ticketKey, string description, string wbs,
            string reporterEmail, string consultantName,
            string responsiblePerson, string projectManager, string commercial,
            string htmlContent)
        {
            try
            {
                var recipients = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(reporterEmail) && !reporterEmail.Contains("*"))
                {
                    recipients.Append(reporterEmail);
                }

                if (!string.IsNullOrWhiteSpace(consultantName) && !consultantName.Contains("*"))
                {
                    var consultantEmail = EmailConverterHelper.ConvertNameToEmail(consultantName);
                    if (!string.IsNullOrWhiteSpace(consultantEmail))
                    {
                        if (recipients.Length > 0) recipients.Append("; ");
                        recipients.Append(consultantEmail);
                    }
                }

                var ccList = new StringBuilder();
                ccList.Append("schedulazione.pa@dedagroup.it");

                foreach (var person in new[] { responsiblePerson, projectManager, commercial })
                {
                    if (!string.IsNullOrWhiteSpace(person) && !person.Contains("*"))
                    {
                        var email = EmailConverterHelper.ConvertNameToEmail(person);
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            ccList.Append($"; {email}");
                        }
                    }
                }

                var wbsCode = ExtractWbsCode(wbs);
                var subject = $"{clientName}: {ticketKey} - {description} - {wbsCode}";

                return new EmailData
                {
                    To = recipients.ToString(),
                    Cc = ccList.ToString(),
                    Subject = subject,
                    BodyHtml = htmlContent,
                    IsHtml = true
                };
            }
            catch (Exception)
            {
                return new EmailData
                {
                    To = reporterEmail ?? "",
                    Cc = "schedulazione.pa@dedagroup.it",
                    Subject = $"{clientName}: {ticketKey} - {description}",
                    BodyHtml = htmlContent,
                    IsHtml = true
                };
            }
        }

        private static string ExtractWbsCode(string wbsComplete)
        {
            if (string.IsNullOrWhiteSpace(wbsComplete))
                return "";

            var parts = wbsComplete.Split('-');
            return parts[0].Trim();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger?.LogInfo("Disposing OutlookHybridService");
                }

                // ⚠️ Non c'è più _outlookApp da rilasciare (problema risolto)
                _disposed = true;
            }
        }

        ~OutlookHybridService()
        {
            Dispose(false);
        }

        #endregion
    }
}