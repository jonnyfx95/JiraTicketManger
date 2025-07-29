using System;
using System.Text;
using System.Web;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per la generazione di template email per la pianificazione degli interventi.
    /// Gestisce la creazione di contenuti in formato testo, HTML e commenti Jira.
    /// </summary>
    public class EmailTemplateService
    {
        private readonly LoggingService _logger;

        public EmailTemplateService()
        {
            _logger = LoggingService.CreateForComponent("EmailTemplate");
        }

        #region Template Types

        /// <summary>
        /// Tipi di template disponibili per la pianificazione
        /// </summary>
        public enum TemplateType
        {
            /// <summary>Template per singolo intervento con data/ora specifica</summary>
            SingleIntervention = 1,

            /// <summary>Template per lista di interventi multipli</summary>
            MultipleInterventions = 2,

            /// <summary>Template per accordo da definire (senza data specifica)</summary>
            ToBeAgreed = 3
        }

        #endregion

        #region Text Preview Generation

        /// <summary>
        /// Genera anteprima testuale del template selezionato
        /// </summary>
        /// <param name="templateType">Tipo di template da generare</param>
        /// <param name="consultantName">Nome del consulente</param>
        /// <param name="interventionDate">Data intervento formattata</param>
        /// <param name="interventionTime">Ora intervento</param>
        /// <param name="clientPhone">Telefono cliente</param>
        /// <returns>Testo dell'anteprima</returns>
        public string GenerateTextPreview(TemplateType templateType, string consultantName,
            string interventionDate, string interventionTime, string clientPhone)
        {
            try
            {
                _logger.LogInfo($"Generazione anteprima testo - Template: {templateType}");

                // Valida e pulisci i parametri
                var cleanConsultant = ValidateAndCleanParameter(consultantName, "Consulente");
                var cleanDate = ValidateAndCleanParameter(interventionDate, "Data da definire");
                var cleanTime = ValidateAndCleanParameter(interventionTime, "Ora da definire");
                var cleanPhone = ValidateAndCleanParameter(clientPhone, "Telefono");

                string textContent = templateType switch
                {
                    TemplateType.SingleIntervention => GenerateSingleInterventionText(cleanConsultant, cleanDate, cleanTime, cleanPhone),
                    TemplateType.MultipleInterventions => GenerateMultipleInterventionsText(cleanConsultant, cleanDate, cleanPhone),
                    TemplateType.ToBeAgreed => GenerateToBeAgreedText(cleanConsultant, cleanPhone),
                    _ => "Template non valido selezionato."
                };

                _logger.LogInfo($"Anteprima generata, lunghezza: {textContent.Length}");
                return textContent;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generazione anteprima testo", ex);
                return $"Errore nella generazione del template: {ex.Message}";
            }
        }

        #endregion

        #region HTML Content Generation

        /// <summary>
        /// Genera contenuto HTML formattato per l'email
        /// </summary>
        /// <param name="templateType">Tipo di template da generare</param>
        /// <param name="consultantName">Nome del consulente</param>
        /// <param name="interventionDate">Data intervento formattata</param>
        /// <param name="interventionTime">Ora intervento</param>
        /// <param name="clientPhone">Telefono cliente</param>
        /// <returns>Contenuto HTML dell'email</returns>
        public string GenerateHtmlContent(TemplateType templateType, string consultantName,
            string interventionDate, string interventionTime, string clientPhone)
        {
            try
            {
                _logger.LogInfo($"Generazione HTML - Template: {templateType}");

                // Valida e pulisci i parametri
                var cleanConsultant = ValidateAndCleanParameter(consultantName, "Consulente");
                var cleanDate = ValidateAndCleanParameter(interventionDate, "Data da definire");
                var cleanTime = ValidateAndCleanParameter(interventionTime, "Ora da definire");
                var cleanPhone = ValidateAndCleanParameter(clientPhone, "Telefono");

                string htmlContent = templateType switch
                {
                    TemplateType.SingleIntervention => GenerateSingleInterventionHtml(cleanConsultant, cleanDate, cleanTime, cleanPhone),
                    TemplateType.MultipleInterventions => GenerateMultipleInterventionsHtml(cleanConsultant, cleanDate, cleanPhone),
                    TemplateType.ToBeAgreed => GenerateToBeAgreedHtml(cleanConsultant, cleanPhone),
                    _ => "<div style='color:red;'>Template non valido selezionato.</div>"
                };

                _logger.LogInfo($"HTML generato, lunghezza: {htmlContent.Length}");
                return htmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generazione HTML", ex);
                return $"<div style='color:red;'>Errore nella generazione dell'HTML: {ex.Message}</div>";
            }
        }

        #endregion

        #region Jira Comment Generation

        /// <summary>
        /// Genera commento formattato per Jira con dettagli della comunicazione
        /// </summary>
        /// <param name="templateType">Tipo di template utilizzato</param>
        /// <param name="consultantName">Nome del consulente</param>
        /// <param name="interventionDate">Data intervento</param>
        /// <param name="interventionTime">Ora intervento</param>
        /// <param name="clientPhone">Telefono cliente</param>
        /// <param name="responsiblePerson">Responsabile area</param>
        /// <param name="projectManager">Project Manager</param>
        /// <param name="commercial">Commerciale</param>
        /// <param name="reporter">Reporter del ticket</param>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <param name="clientName">Nome cliente</param>
        /// <param name="description">Descrizione ticket</param>
        /// <param name="wbs">Codice WBS</param>
        /// <returns>Commento formattato per Jira</returns>
        public string GenerateJiraComment(TemplateType templateType, string consultantName,
            string interventionDate, string interventionTime, string clientPhone,
            string responsiblePerson, string projectManager, string commercial,
            string reporter, string ticketKey, string clientName, string description, string wbs)
        {
            try
            {
                _logger.LogInfo($"Generazione commento Jira - Template: {templateType}");

                var commentBuilder = new StringBuilder();

                // Header del commento
                commentBuilder.AppendLine("📧 COMUNICAZIONE AUTOMATICA - PIANIFICAZIONE ATTIVITÀ");
                commentBuilder.AppendLine("=" + new string('=', 55));
                commentBuilder.AppendLine();

                // Dettagli invio
                var sendDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                commentBuilder.AppendLine($"Da: DEDAGROUP Schedulazione PA <schedulazione.pa@dedagroup.it>");
                commentBuilder.AppendLine($"Inviato: {sendDate}");

                // Destinatari
                var recipients = BuildRecipientsList(reporter, consultantName, responsiblePerson, projectManager, commercial);
                commentBuilder.AppendLine($"A: {recipients.To}");
                if (!string.IsNullOrWhiteSpace(recipients.Cc))
                {
                    commentBuilder.AppendLine($"Cc: {recipients.Cc}");
                }

                // Oggetto
                var subject = BuildEmailSubject(clientName, ticketKey, description, wbs);
                commentBuilder.AppendLine($"Oggetto: {subject}");
                commentBuilder.AppendLine();

                // Separatore
                commentBuilder.AppendLine("─" + new string('─', 55));
                commentBuilder.AppendLine();

                // Corpo del messaggio (versione testuale)
                var textContent = GenerateTextPreview(templateType, consultantName, interventionDate, interventionTime, clientPhone);
                commentBuilder.AppendLine(textContent);

                commentBuilder.AppendLine();
                commentBuilder.AppendLine("─" + new string('─', 55));
                commentBuilder.AppendLine($"💼 Template utilizzato: {GetTemplateDisplayName(templateType)}");
                commentBuilder.AppendLine($"🕒 Generato automaticamente il {sendDate}");

                var finalComment = commentBuilder.ToString();
                _logger.LogInfo($"Commento Jira generato, lunghezza: {finalComment.Length}");

                return finalComment;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generazione commento Jira", ex);
                return $"Errore nella generazione del commento: {ex.Message}";
            }
        }

        #endregion

        #region Private Template Generators - Text

        private string GenerateSingleInterventionText(string consultant, string date, string time, string phone)
        {
            return $"Gentile cliente,{Environment.NewLine}" +
                   $"la informiamo che in seguito alla sua richiesta è stato programmato un intervento a cura del consulente {consultant} in data {date} con inizio alle ore {time}.{Environment.NewLine}{Environment.NewLine}" +
                   $"Ritenendo la data confermata sin da ora la invitiamo a contattarci tempestivamente se fosse necessaria una ripianificazione.{Environment.NewLine}{Environment.NewLine}" +
                   $"Contatto cliente: {phone}";
        }

        private string GenerateMultipleInterventionsText(string consultant, string date, string phone)
        {
            return $"Gentile Cliente,{Environment.NewLine}" +
                   $"la informiamo che in seguito alla sua richiesta sono stati programmati i seguenti interventi con il consulente {consultant}:{Environment.NewLine}" +
                   $"{date};{Environment.NewLine}" +
                   $"[DATA 2];{Environment.NewLine}" +
                   $"[DATA 3].{Environment.NewLine}{Environment.NewLine}" +
                   $"Ritenendo la data confermata sin da ora la invitiamo a contattarci tempestivamente se fosse necessaria una ripianificazione.{Environment.NewLine}{Environment.NewLine}" +
                   $"Contatto cliente: {phone}";
        }

        private string GenerateToBeAgreedText(string consultant, string phone)
        {
            return $"Gentile Cliente,{Environment.NewLine}" +
                   $"la informiamo che in seguito alla sua richiesta è stato programmato un intervento a cura del consulente {consultant}.{Environment.NewLine}" +
                   $"Concorderete con il consulente data e ora dell'appuntamento.{Environment.NewLine}" +
                   $"Contatto cliente: {phone}";
        }

        #endregion

        #region Private Template Generators - HTML

        private string GenerateSingleInterventionHtml(string consultant, string date, string time, string phone)
        {
            return "<span style='font-family:Segoe UI; font-size:10pt; font-style:italic;'>" +
                   "<i>Gentile cliente,</i><br><br>" +
                   $"La informiamo che in seguito alla sua richiesta è stato programmato un intervento a cura del consulente {EscapeHtml(consultant)} in data {EscapeHtml(date)} con inizio alle ore {EscapeHtml(time)}." +
                   "<br>Ritenendo la data confermata sin da ora, la invitiamo a contattarci tempestivamente se fosse necessaria una ripianificazione." +
                   $"<br><br>Contatto cliente: {EscapeHtml(phone)}</span>";
        }

        private string GenerateMultipleInterventionsHtml(string consultant, string date, string phone)
        {
            return "<div style='font-family:Segoe UI; font-size:10pt; font-style:italic;'>" +
                   "<p>Gentile Cliente,</p>" +
                   $"<p>La informiamo che in seguito alla sua richiesta sono stati programmati i seguenti interventi con il consulente {EscapeHtml(consultant)}:</p>" +
                   "<ul>" +
                   $"    <li>{EscapeHtml(date)}</li>" +
                   "    <li>[INSERISCI DATA 2]</li>" +
                   "    <li>[INSERISCI DATA 3]</li>" +
                   "</ul>" +
                   "<p>Ritenendo la data confermata sin da ora, la invitiamo a contattarci tempestivamente se fosse necessaria una ripianificazione.</p>" +
                   $"<p>Contatto cliente: {EscapeHtml(phone)}</p></div>";
        }

        private string GenerateToBeAgreedHtml(string consultant, string phone)
        {
            return "<div style='font-family:Segoe UI; font-size:10pt; font-style:italic;'>" +
                   "<i>Gentile Cliente," +
                   $"<br><br>La informiamo che in seguito alla sua richiesta è stato programmato un intervento a cura del consulente {EscapeHtml(consultant)}." +
                   "<br>Concorderete con il consulente data e ora dell'appuntamento." +
                   $"<br><br>Contatto cliente: {EscapeHtml(phone)}</i>" +
                   "</div>";
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Valida e pulisce un parametro, sostituendo valori non validi con un default
        /// </summary>
        private string ValidateAndCleanParameter(string value, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value == "[Campo non disponibile]" ||
                value == "Non presente" ||
                value == "[Nessuno]" ||
                value.Contains("*"))
            {
                return defaultValue;
            }

            return value.Trim();
        }

        /// <summary>
        /// Escape HTML per sicurezza
        /// </summary>
        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return HttpUtility.HtmlEncode(text);
        }

        /// <summary>
        /// Costruisce le liste dei destinatari per il commento Jira
        /// </summary>
        private (string To, string Cc) BuildRecipientsList(string reporter, string consultant,
            string responsible, string projectManager, string commercial)
        {
            var toRecipients = new StringBuilder();
            var ccRecipients = new StringBuilder();

            // Destinatario principale (reporter)
            if (!string.IsNullOrWhiteSpace(reporter) && !reporter.Contains("*"))
            {
                toRecipients.Append($"{reporter}");
            }

            // Aggiungi consulente se diverso dal reporter
            if (!string.IsNullOrWhiteSpace(consultant) && !consultant.Contains("*"))
            {
                if (toRecipients.Length > 0) toRecipients.Append("; ");
                toRecipients.Append($"{consultant}");
            }

            // CC automatici
            ccRecipients.Append("schedulazione.pa@dedagroup.it");

            // Aggiungi altri in CC se validi
            foreach (var person in new[] { responsible, projectManager, commercial })
            {
                if (!string.IsNullOrWhiteSpace(person) && !person.Contains("*"))
                {
                    ccRecipients.Append($"; {person}");
                }
            }

            return (toRecipients.ToString(), ccRecipients.ToString());
        }

        /// <summary>
        /// Costruisce l'oggetto dell'email
        /// </summary>
        private string BuildEmailSubject(string clientName, string ticketKey, string description, string wbs)
        {
            var wbsCode = ExtractWbsCode(wbs);
            return $"{clientName}: {ticketKey} - {description} - {wbsCode}";
        }

        /// <summary>
        /// Estrae il codice WBS (prima parte prima del -)
        /// </summary>
        private string ExtractWbsCode(string wbsComplete)
        {
            if (string.IsNullOrWhiteSpace(wbsComplete))
                return "";

            var parts = wbsComplete.Split('-');
            return parts[0].Trim();
        }

        /// <summary>
        /// Ottiene il nome visualizzato del template
        /// </summary>
        private string GetTemplateDisplayName(TemplateType templateType)
        {
            return templateType switch
            {
                TemplateType.SingleIntervention => "Singolo Intervento",
                TemplateType.MultipleInterventions => "Interventi Multipli",
                TemplateType.ToBeAgreed => "Accordo da Definire",
                _ => "Sconosciuto"
            };
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Verifica se un template type è valido
        /// </summary>
        public static bool IsValidTemplateType(int templateIndex)
        {
            return Enum.IsDefined(typeof(TemplateType), templateIndex);
        }

        /// <summary>
        /// Converte un indice di ComboBox in TemplateType
        /// </summary>
        public static TemplateType? GetTemplateTypeFromIndex(int index)
        {
            if (IsValidTemplateType(index))
                return (TemplateType)index;

            return null;
        }

        /// <summary>
        /// Ottiene tutti i template disponibili per popolare ComboBox
        /// </summary>
        public static Dictionary<TemplateType, string> GetAvailableTemplates()
        {
            return new Dictionary<TemplateType, string>
            {
                { TemplateType.SingleIntervention, "Singolo intervento con data/ora" },
                { TemplateType.MultipleInterventions, "Lista interventi multipli" },
                { TemplateType.ToBeAgreed, "Accordo da definire" }
            };
        }

        #endregion
    }
}