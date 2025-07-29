using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using JiraTicketManager.Services;
using JiraTicketManager.Helpers;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per l'integrazione con Microsoft Outlook tramite COM Interop tipizzato.
    /// Utilizza Microsoft.Office.Interop.Outlook per tipizzazione forte e migliore debugging.
    /// Sostituisce il pattern bridge esterno con integrazione diretta .NET 8.
    /// </summary>
    public class OutlookIntegrationService : IDisposable
    {
        private readonly LoggingService _logger;
        private Outlook.Application _outlookApp;
        private bool _disposed = false;

        public OutlookIntegrationService()
        {
            _logger = LoggingService.CreateForComponent("OutlookIntegration");
        }

        #region Email Data Model

        /// <summary>
        /// Modello per i dati dell'email da inviare
        /// </summary>
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

            /// <summary>
            /// Valida i dati essenziali dell'email
            /// </summary>
            public bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(To) &&
                       !string.IsNullOrWhiteSpace(Subject) &&
                       (!string.IsNullOrWhiteSpace(BodyHtml) || !string.IsNullOrWhiteSpace(BodyText));
            }

            /// <summary>
            /// Ottiene il corpo dell'email appropriato (HTML o testo)
            /// </summary>
            public string GetBody()
            {
                return IsHtml && !string.IsNullOrWhiteSpace(BodyHtml) ? BodyHtml : BodyText;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apre Outlook con una nuova email precompilata con i dati forniti
        /// </summary>
        /// <param name="emailData">Dati dell'email da preparare</param>
        /// <returns>True se l'operazione è riuscita</returns>
        public async Task<bool> OpenEmailAsync(EmailData emailData)
        {
            return await Task.Run(() => OpenEmail(emailData));
        }

        /// <summary>
        /// Apre Outlook con una nuova email precompilata (versione sincrona)
        /// </summary>
        /// <param name="emailData">Dati dell'email da preparare</param>
        /// <returns>True se l'operazione è riuscita</returns>
        public bool OpenEmail(EmailData emailData)
        {
            try
            {
                _logger.LogInfo("Apertura email Outlook - Inizio processo");

                // Validazione dati
                if (emailData == null)
                {
                    _logger.LogError("EmailData è null");
                    return false;
                }

                if (!emailData.IsValid())
                {
                    _logger.LogError("EmailData non valida - campi obbligatori mancanti");
                    return false;
                }

                // Pulizia indirizzi email
                var cleanEmailData = CleanEmailData(emailData);
                _logger.LogInfo($"Email pulita - To: {cleanEmailData.To.Length} char, Cc: {cleanEmailData.Cc.Length} char");

                // Inizializza Outlook
                if (!InitializeOutlook())
                {
                    return false;
                }

                // Crea e configura l'email
                Outlook.MailItem mailItem = CreateMailItem();
                if (mailItem == null)
                {
                    return false;
                }

                try
                {
                    ConfigureMailItem(mailItem, cleanEmailData);
                    DisplayEmail(mailItem);
                    _logger.LogInfo("Email Outlook aperta con successo");
                    return true;
                }
                finally
                {
                    // Rilascia il MailItem COM object
                    if (mailItem != null)
                    {
                        Marshal.ReleaseComObject(mailItem);
                    }
                }
            }
            catch (COMException comEx)
            {
                _logger.LogError($"Errore COM Outlook: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})", comEx);
                return HandleOutlookComError(comEx);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore apertura email Outlook", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifica se Outlook è disponibile nel sistema
        /// </summary>
        /// <returns>True se Outlook è disponibile</returns>
        public async Task<bool> IsOutlookAvailableAsync()
        {
            return await Task.Run(IsOutlookAvailable);
        }

        /// <summary>
        /// Verifica se Outlook è disponibile nel sistema (versione sincrona)
        /// </summary>
        /// <returns>True se Outlook è disponibile</returns>
        public bool IsOutlookAvailable()
        {
            try
            {
                _logger.LogInfo("Verifica disponibilità Outlook");

                // Prova a creare l'oggetto Outlook
                var testApp = new Outlook.Application();
                if (testApp != null)
                {
                    // Verifica che Outlook sia effettivamente funzionante
                    var version = testApp.Version;
                    _logger.LogInfo($"Outlook disponibile - Versione: {version}");

                    Marshal.ReleaseComObject(testApp);
                    return true;
                }

                _logger.LogWarning("Outlook non disponibile - CreateInstance fallito");
                return false;
            }
            catch (COMException comEx)
            {
                _logger.LogWarning($"Outlook non disponibile - COM Error: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Outlook non disponibile - Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ottiene informazioni sulla versione di Outlook installata
        /// </summary>
        /// <returns>Versione di Outlook o null se non disponibile</returns>
        public string GetOutlookVersion()
        {
            Outlook.Application tempApp = null;
            try
            {
                tempApp = new Outlook.Application();
                var version = tempApp.Version;
                _logger.LogInfo($"Versione Outlook rilevata: {version}");
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Impossibile rilevare versione Outlook: {ex.Message}");
                return null;
            }
            finally
            {
                // Rilascia manualmente COM object
                if (tempApp != null)
                {
                    Marshal.ReleaseComObject(tempApp);
                }
            }
        }

        #endregion

        #region Private Methods - Outlook Initialization

        /// <summary>
        /// Inizializza l'applicazione Outlook
        /// </summary>
        private bool InitializeOutlook()
        {
            try
            {
                if (_outlookApp == null)
                {
                    _logger.LogInfo("Inizializzazione applicazione Outlook");
                    _outlookApp = new Outlook.Application();

                    if (_outlookApp == null)
                    {
                        _logger.LogError("Impossibile creare istanza Outlook.Application");
                        return false;
                    }

                    // Verifica che l'applicazione sia funzionante
                    var version = _outlookApp.Version;
                    _logger.LogInfo($"Outlook.Application inizializzata - Versione: {version}");
                }

                return true;
            }
            catch (COMException comEx)
            {
                _logger.LogError($"Errore inizializzazione Outlook COM: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})", comEx);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione Outlook", ex);
                return false;
            }
        }

        /// <summary>
        /// Crea un nuovo elemento mail
        /// </summary>
        private Outlook.MailItem CreateMailItem()
        {
            try
            {
                _logger.LogInfo("Creazione nuovo MailItem");

                var mailItem = (Outlook.MailItem)_outlookApp.CreateItem(Outlook.OlItemType.olMailItem);

                if (mailItem == null)
                {
                    _logger.LogError("Impossibile creare MailItem");
                    return null;
                }

                _logger.LogInfo("MailItem creato con successo");
                return mailItem;
            }
            catch (COMException comEx)
            {
                _logger.LogError($"Errore creazione MailItem COM: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})", comEx);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore creazione MailItem", ex);
                return null;
            }
        }

        /// <summary>
        /// Configura il MailItem con i dati forniti
        /// </summary>
        private void ConfigureMailItem(Outlook.MailItem mailItem, EmailData emailData)
        {
            try
            {
                _logger.LogInfo("Configurazione MailItem");

                // Destinatari principali
                mailItem.To = emailData.To;
                _logger.LogInfo($"To impostato: {emailData.To}");

                // Destinatari in copia
                if (!string.IsNullOrWhiteSpace(emailData.Cc))
                {
                    mailItem.CC = emailData.Cc;
                    _logger.LogInfo($"CC impostato: {emailData.Cc}");
                }

                // Destinatari in copia nascosta
                if (!string.IsNullOrWhiteSpace(emailData.Bcc))
                {
                    mailItem.BCC = emailData.Bcc;
                    _logger.LogInfo($"BCC impostato: {emailData.Bcc}");
                }

                // Oggetto
                mailItem.Subject = emailData.Subject;
                _logger.LogInfo($"Subject impostato: {emailData.Subject}");

                // Corpo dell'email
                if (emailData.IsHtml && !string.IsNullOrWhiteSpace(emailData.BodyHtml))
                {
                    mailItem.HTMLBody = emailData.BodyHtml;
                    _logger.LogInfo($"HTMLBody impostato: {emailData.BodyHtml.Length} caratteri");
                }
                else if (!string.IsNullOrWhiteSpace(emailData.BodyText))
                {
                    mailItem.Body = emailData.BodyText;
                    _logger.LogInfo($"Body impostato: {emailData.BodyText.Length} caratteri");
                }

                // Allegati (se presenti)
                if (emailData.Attachments != null && emailData.Attachments.Length > 0)
                {
                    AddAttachments(mailItem, emailData.Attachments);
                }

                // Imposta priorità normale
                mailItem.Importance = Outlook.OlImportance.olImportanceNormal;

                _logger.LogInfo("MailItem configurato completamente");
            }
            catch (COMException comEx)
            {
                _logger.LogError($"Errore configurazione MailItem COM: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})", comEx);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore configurazione MailItem", ex);
                throw;
            }
        }

        /// <summary>
        /// Aggiunge allegati al MailItem
        /// </summary>
        private void AddAttachments(Outlook.MailItem mailItem, string[] attachments)
        {
            try
            {
                _logger.LogInfo($"Aggiunta {attachments.Length} allegati");

                foreach (var attachment in attachments)
                {
                    if (!string.IsNullOrWhiteSpace(attachment) && File.Exists(attachment))
                    {
                        mailItem.Attachments.Add(attachment, Outlook.OlAttachmentType.olByValue);
                        _logger.LogInfo($"Allegato aggiunto: {attachment}");
                    }
                    else
                    {
                        _logger.LogWarning($"Allegato non trovato o non valido: {attachment}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiunta allegati", ex);
                // Non rilanciare l'eccezione - gli allegati sono opzionali
            }
        }

        /// <summary>
        /// Mostra l'email all'utente
        /// </summary>
        private void DisplayEmail(Outlook.MailItem mailItem)
        {
            try
            {
                _logger.LogInfo("Visualizzazione email");

                // Mostra l'email senza inviarla automaticamente
                mailItem.Display(false); // false = non modale

                _logger.LogInfo("Email visualizzata con successo");
            }
            catch (COMException comEx)
            {
                _logger.LogError($"Errore visualizzazione email COM: {comEx.Message} (HRESULT: 0x{comEx.HResult:X8})", comEx);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione email", ex);
                throw;
            }
        }

        #endregion

        #region Private Methods - Error Handling

        /// <summary>
        /// Gestisce errori COM specifici di Outlook
        /// </summary>
        private bool HandleOutlookComError(COMException comEx)
        {
            // Codici di errore COM comuni di Outlook
            switch ((uint)comEx.HResult)
            {
                case 0x80040154: // REGDB_E_CLASSNOTREG
                    _logger.LogError("Outlook non è registrato correttamente nel sistema");
                    break;
                case 0x800401F0: // CO_E_NOTINITIALIZED
                    _logger.LogError("COM non inizializzato correttamente");
                    break;
                case 0x80070005: // E_ACCESSDENIED
                    _logger.LogError("Accesso negato a Outlook - verificare permessi");
                    break;
                default:
                    _logger.LogError($"Errore COM non riconosciuto: 0x{comEx.HResult:X8}");
                    break;
            }
            return false;
        }

        #endregion

        #region Private Methods - Data Cleaning

        /// <summary>
        /// Pulisce e valida i dati dell'email
        /// </summary>
        private EmailData CleanEmailData(EmailData emailData)
        {
            return new EmailData
            {
                To = CleanEmailAddresses(emailData.To),
                Cc = CleanEmailAddresses(emailData.Cc),
                Bcc = CleanEmailAddresses(emailData.Bcc),
                Subject = CleanSubject(emailData.Subject),
                BodyHtml = emailData.BodyHtml?.Trim() ?? "",
                BodyText = emailData.BodyText?.Trim() ?? "",
                IsHtml = emailData.IsHtml,
                Attachments = emailData.Attachments ?? Array.Empty<string>()
            };
        }

        /// <summary>
        /// Pulisce gli indirizzi email rimuovendo spazi extra e caratteri non validi
        /// </summary>
        private string CleanEmailAddresses(string emailString)
        {
            if (string.IsNullOrWhiteSpace(emailString))
                return "";

            try
            {
                // Rimuovi spazi extra e caratteri non validi
                var cleaned = emailString.Trim();

                // Dividi per punto e virgola e pulisci ogni indirizzo
                var addresses = cleaned.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(addr => addr.Trim())
                                      .Where(addr => !string.IsNullOrWhiteSpace(addr))
                                      .Where(addr => IsValidEmailFormat(addr));

                // Ricostruisci la stringa
                var result = string.Join("; ", addresses);
                _logger.LogInfo($"Email pulite: '{emailString}' → '{result}'");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore pulizia indirizzi email: {ex.Message}");
                return emailString; // Ritorna l'originale se c'è un errore
            }
        }

        /// <summary>
        /// Valida il formato di base di un indirizzo email
        /// </summary>
        private bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Usa System.Net.Mail.MailAddress per validazione più robusta
                var mailAddress = new System.Net.Mail.MailAddress(email);
                return mailAddress.Address == email.Trim();
            }
            catch
            {
                // Fallback a controllo base se MailAddress fallisce
                var atIndex = email.IndexOf('@');
                if (atIndex <= 0 || atIndex == email.Length - 1)
                    return false;

                var dotIndex = email.LastIndexOf('.');
                return dotIndex > atIndex && dotIndex < email.Length - 1;
            }
        }

        /// <summary>
        /// Pulisce l'oggetto dell'email
        /// </summary>
        private string CleanSubject(string subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
                return "";

            // Rimuovi caratteri di controllo e spazi extra
            return subject.Trim()
                          .Replace('\r', ' ')
                          .Replace('\n', ' ')
                          .Replace('\t', ' ')
                          .Replace("  ", " ");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Crea EmailData da parametri di pianificazione
        /// </summary>
        public static EmailData CreateFromPlanningData(string to, string cc, string subject,
            string htmlBody, string textBody = "")
        {
            return new EmailData
            {
                To = to ?? "",
                Cc = cc ?? "",
                Subject = subject ?? "",
                BodyHtml = htmlBody ?? "",
                BodyText = textBody ?? "",
                IsHtml = !string.IsNullOrWhiteSpace(htmlBody)
            };
        }

        /// <summary>
        /// Prepara i dati email per la pianificazione utilizzando gli helper esistenti
        /// </summary>
        public static EmailData PrepareEmailFromTicketData(
            string clientName, string ticketKey, string description, string wbs,
            string reporterEmail, string consultantName,
            string responsiblePerson, string projectManager, string commercial,
            string htmlContent)
        {
            try
            {
                // Costruisce destinatari utilizzando EmailConverterHelper esistente
                var recipients = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(reporterEmail) && !reporterEmail.Contains("*"))
                {
                    recipients.Append(reporterEmail);
                }

                // Aggiungi consulente se disponibile
                if (!string.IsNullOrWhiteSpace(consultantName) && !consultantName.Contains("*"))
                {
                    var consultantEmail = EmailConverterHelper.ConvertNameToEmail(consultantName);
                    if (!string.IsNullOrWhiteSpace(consultantEmail))
                    {
                        if (recipients.Length > 0) recipients.Append("; ");
                        recipients.Append(consultantEmail);
                    }
                }

                // Costruisce CC utilizzando ResponsabileHelper esistente
                var ccList = new StringBuilder();
                ccList.Append("schedulazione.pa@dedagroup.it");

                // Aggiungi responsabile, PM e commerciale se validi
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

                // Costruisce oggetto
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
                // Fallback in caso di errore
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

        /// <summary>
        /// Estrae il codice WBS (prima parte prima del -)
        /// </summary>
        private static string ExtractWbsCode(string wbsComplete)
        {
            if (string.IsNullOrWhiteSpace(wbsComplete))
                return "";

            var parts = wbsComplete.Split('-');
            return parts[0].Trim();
        }

        #endregion

        #region IDisposable Implementation

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
                    // Pulisci risorse gestite
                    _logger?.LogInfo("Disposing OutlookIntegrationService");
                }

                // Pulisci risorse COM
                if (_outlookApp != null)
                {
                    try
                    {
                        Marshal.ReleaseComObject(_outlookApp);
                        _outlookApp = null;
                        _logger?.LogInfo("COM Object Outlook rilasciato");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError("Errore rilascio COM Object", ex);
                    }
                }

                _disposed = true;
            }
        }

        ~OutlookIntegrationService()
        {
            Dispose(false);
        }

        #endregion
    }
}