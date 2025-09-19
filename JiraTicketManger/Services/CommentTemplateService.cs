using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio dedicato per la generazione di template commenti Jira.
    /// Gestisce diversi formati di commento per diverse esigenze del progetto.
    /// Separato da EmailTemplateService per mantenere responsabilità distinte.
    /// </summary>
    public class CommentTemplateService
    {
        private readonly LoggingService _logger;

        public CommentTemplateService()
        {
            _logger = LoggingService.CreateForComponent("CommentTemplate");
        }

        #region Comment Types

        /// <summary>
        /// Tipi di commento supportati
        /// </summary>
        public enum CommentType
        {
            /// <summary>Commento formato email inoltrata per pianificazione</summary>
            ForwardedEmail,
            
            /// <summary>Commento semplice di testo</summary>
            Simple,
            
            /// <summary>Commento automatico di sistema</summary>
            System
        }

        #endregion

        #region Main Comment Generation

        /// <summary>
        /// Genera commento formato "email inoltrata" per Jira
        /// Simula una comunicazione email inoltrata con intestazioni complete
        /// </summary>
        /// <param name="data">Dati del ticket per la pianificazione</param>
        /// <returns>Testo formattato come email inoltrata</returns>
        public string GenerateForwardedEmailComment(CommentData data)
        {
            try
            {
                _logger.LogInfo($"Generazione commento email inoltrata - Template: {data.TemplateType}, Ticket: {data.TicketKey}");

                // Valida i dati in ingresso
                if (!ValidateCommentData(data))
                {
                    return GenerateErrorComment("Dati di input non validi per la generazione del commento");
                }

                // Costruisci il commento completo
                var comment = new StringBuilder();

                // === INTESTAZIONE EMAIL INOLTRATA ===
                AppendEmailHeader(comment, data);

                // === INFORMAZIONI SISTEMA ===
                AppendSystemInfo(comment, data);

                // === CONTENUTO TEMPLATE ===
                AppendTemplateContent(comment, data);

                var result = comment.ToString();
                _logger.LogInfo($"Commento email inoltrata generato - Lunghezza: {result.Length} caratteri");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore generazione commento email inoltrata: {ex.Message}");
                return GenerateErrorComment($"Errore nella generazione: {ex.Message}");
            }
        }

        /// <summary>
        /// Genera commento semplice di testo
        /// </summary>
        /// <param name="text">Testo del commento</param>
        /// <param name="author">Autore del commento (opzionale)</param>
        /// <returns>Commento formattato</returns>
        public string GenerateSimpleComment(string text, string author = "")
        {
            try
            {
                _logger.LogInfo("Generazione commento semplice");

                var comment = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(author))
                {
                    comment.AppendLine($"👤 {author}");
                    comment.AppendLine($"🕒 {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    comment.AppendLine();
                }

                comment.Append(text?.Trim() ?? "[Commento vuoto]");

                return comment.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore generazione commento semplice: {ex.Message}");
                return GenerateErrorComment($"Errore commento semplice: {ex.Message}");
            }
        }

        #endregion

        #region Email Header Construction

        /// <summary>
        /// Costruisce l'intestazione email inoltrata
        /// </summary>
        private void AppendEmailHeader(StringBuilder comment, CommentData data)
        {
            comment.AppendLine("📧 COMUNICAZIONE AUTOMATICA - PIANIFICAZIONE ATTIVITÀ");
            comment.AppendLine("========================================================");
            comment.AppendLine("Da: DEDAGROUP Schedulazione PA <schedulazione.pa@dedagroup.it>");
            comment.AppendLine($"Inviato: {DateTime.Now:dd/MM/yyyy HH:mm}");

            // Destinatari
            var (toRecipients, ccRecipients) = BuildEmailRecipients(data);

            if (!string.IsNullOrWhiteSpace(toRecipients))
            {
                comment.AppendLine($"A: {toRecipients}");
            }

            if (!string.IsNullOrWhiteSpace(ccRecipients))
            {
                comment.AppendLine($"Cc: {ccRecipients}");
            }

            // Oggetto (formato: [CLIENTE]: [TICKET] - [SUMMARY] - [WBS])
            var subject = BuildEmailSubject(data);
            comment.AppendLine($"Oggetto: {subject}");

            // Separatore
            comment.AppendLine("────────────────────────────────────────────────────────");
        }

        /// <summary>
        /// Costruisce la sezione informazioni sistema
        /// </summary>
        private void AppendSystemInfo(StringBuilder comment, CommentData data)
        {
            comment.AppendLine("ℹ️ INFORMAZIONI SISTEMA:");
            comment.AppendLine($"- Ticket: {data.TicketKey}: {data.TicketSummary}");
            comment.AppendLine($"- Cliente: {data.ClientName}");
            comment.AppendLine($"- Telefono: {data.ClientPhone}");
            comment.AppendLine($"- Template utilizzato: {GetTemplateDisplayName(data.TemplateType)}");
            comment.AppendLine($"- Generato automaticamente il: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            comment.AppendLine();
        }

        /// <summary>
        /// Aggiunge il contenuto del template
        /// </summary>
        private void AppendTemplateContent(StringBuilder comment, CommentData data)
        {
            var templateContent = GenerateTemplateTextContent(data);
            comment.AppendLine(templateContent);
        }

        #endregion

        #region Template Content Generation

        /// <summary>
        /// Genera il contenuto testuale del template
        /// Riutilizza la logica di EmailTemplateService ma per testo puro
        /// </summary>
        private string GenerateTemplateTextContent(CommentData data)
        {
            return data.TemplateType switch
            {
                EmailTemplateService.TemplateType.SingleIntervention => 
                    GenerateSingleInterventionText(data),
                
                EmailTemplateService.TemplateType.MultipleInterventions => 
                    GenerateMultipleInterventionsText(data),
                
                EmailTemplateService.TemplateType.ToBeAgreed => 
                    GenerateToBeAgreedText(data),
                
                _ => "Template non riconosciuto."
            };
        }

        /// <summary>
        /// Template per singolo intervento
        /// </summary>
        private string GenerateSingleInterventionText(CommentData data)
        {
            var content = new StringBuilder();

            content.AppendLine("È stato pianificato un intervento di assistenza tecnica.");
            content.AppendLine();
            content.AppendLine($"📅 Data: {data.InterventionDate}");
            content.AppendLine($"🕐 Orario: {data.InterventionTime}");
            content.AppendLine($"👨‍💼 Consulente: {data.ConsultantName}");
            content.AppendLine();

            
            try
            {
                var emailTemplateService = new EmailTemplateService();
                var dynamicContent = emailTemplateService.GenerateTextPreview(
                    EmailTemplateService.TemplateType.SingleIntervention,
                    data.ConsultantName,
                    data.InterventionDate,
                    data.InterventionTime,
                    data.ClientPhone
                );

                content.AppendLine(dynamicContent);
            }
            catch (Exception ex)
            {
                // Fallback alla stringa fissa se il template dinamico fallisce
                content.AppendLine("Il consulente sarà disponibile all'orario concordato per fornire il supporto necessario.");
                content.AppendLine($"Per eventuali modifiche o chiarimenti, contattare: {data.ClientPhone}");
            }

            return content.ToString();
        }

        /// <summary>
        /// Template per interventi multipli
        /// </summary>
        private string GenerateMultipleInterventionsText(CommentData data)
        {
            var content = new StringBuilder();
            
            content.AppendLine("Sono stati pianificati interventi di assistenza tecnica multipli.");
            content.AppendLine();
            content.AppendLine($"📅 Data iniziale: {data.InterventionDate}");
            content.AppendLine($"👨‍💼 Consulente di riferimento: {data.ConsultantName}");
            content.AppendLine();
            content.AppendLine("Il consulente contatterà il cliente per definire:");
            content.AppendLine("• Date e orari specifici per ogni intervento");
            content.AppendLine("• Modalità di esecuzione (remoto/in presenza)");
            content.AppendLine("• Priorità degli interventi da svolgere");
            content.AppendLine();
            content.AppendLine($"Contatto cliente: {data.ClientPhone}");

            return content.ToString();
        }

        /// <summary>
        /// Template per accordi da concordare
        /// </summary>
        private string GenerateToBeAgreedText(CommentData data)
        {
            var content = new StringBuilder();
            
            content.AppendLine("L'intervento è in fase di pianificazione.");
            content.AppendLine();
            content.AppendLine($"👨‍💼 Consulente assegnato: {data.ConsultantName}");
            content.AppendLine();
            content.AppendLine("Il consulente contatterà il cliente per concordare:");
            content.AppendLine("• Data e ora dell'appuntamento");
            content.AppendLine("• Modalità di intervento più adatta");
            content.AppendLine("• Dettagli tecnici specifici");
            content.AppendLine();
            content.AppendLine("Concorderete con il consulente data e ora dell'appuntamento.");
            content.AppendLine($"Contatto cliente: {data.ClientPhone}");

            return content.ToString();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Costruisce i destinatari email simulati con formato completo
        /// Segue la logica di OutlookHybridService.PrepareEmailFromTicketData
        /// </summary>
        private (string To, string Cc) BuildEmailRecipients(CommentData data)
        {
            try
            {
                var toList = new List<string>();
                var ccList = new List<string>();

                // TO: Commerciale (principale destinatario) - usa il nome se email vuota
                if (!string.IsNullOrWhiteSpace(data.CommercialEmail))
                {
                    toList.Add(data.CommercialEmail);
                }
                else if (!string.IsNullOrWhiteSpace(data.ConsultantName))
                {
                    // Fallback: simula email dal nome consulente se commerciale non disponibile
                    var simulatedEmail = SimulateEmailFromName(data.ConsultantName);
                    if (!string.IsNullOrWhiteSpace(simulatedEmail))
                        toList.Add(simulatedEmail);
                }
                
                // Ulteriore fallback: Reporter
                if (!toList.Any() && !string.IsNullOrWhiteSpace(data.ReporterEmail))
                {
                    toList.Add(data.ReporterEmail);
                }

                // CC: PM dal ticket reale
                if (!string.IsNullOrWhiteSpace(data.ProjectManagerEmail))
                {
                    ccList.Add(data.ProjectManagerEmail);
                }
                // Se PM email vuota ma abbiamo il nome PM dal ticket, simulalo
                else if (!string.IsNullOrWhiteSpace(data.ProjectManagerName))
                {
                    var simulatedPMEmail = SimulateEmailFromName(data.ProjectManagerName);
                    if (!string.IsNullOrWhiteSpace(simulatedPMEmail))
                        ccList.Add(simulatedPMEmail);
                }

                // CC: Consulente
                if (!string.IsNullOrWhiteSpace(data.ConsultantEmail))
                {
                    ccList.Add(data.ConsultantEmail);
                }
                else if (!string.IsNullOrWhiteSpace(data.ConsultantName))
                {
                    // Simula consulente email dal nome
                    var simulatedConsultant = SimulateEmailFromName(data.ConsultantName);
                    if (!string.IsNullOrWhiteSpace(simulatedConsultant))
                        ccList.Add(simulatedConsultant);
                }

                // Sempre in CC: DEDAGROUP Schedulazione PA
                ccList.Add("DEDAGROUP Schedulazione PA <schedulazione.pa@dedagroup.it>");

                var to = toList.Any() ? string.Join("; ", toList) : data.ReporterEmail ?? "destinatario@cliente.it";
                var cc = ccList.Any() ? string.Join("; ", ccList) : "DEDAGROUP Schedulazione PA <schedulazione.pa@dedagroup.it>";

                _logger.LogDebug($"Destinatari costruiti - TO: {to}, CC: {cc}");
                return (to, cc);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore costruzione destinatari: {ex.Message}");
                return ("", "DEDAGROUP Schedulazione PA <schedulazione.pa@dedagroup.it>");
            }
        }

        /// <summary>
        /// Simula un'email dal nome per scopi dimostrativi nel commento
        /// </summary>
        private string SimulateEmailFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            try
            {
                // Converti nome in formato email simulato
                var cleanName = name.Trim();
                
                // Se contiene spazi, prova a creare nome.cognome
                if (cleanName.Contains(" "))
                {
                    var parts = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var firstName = parts[0].ToLowerInvariant();
                        var lastName = parts[1].ToLowerInvariant();
                        var emailAddress = $"{firstName}.{lastName}@dedagroup.it";
                        return $"{cleanName} <{emailAddress}>";
                    }
                }
                
                // Fallback: usa nome completo
                var fallbackEmail = $"{cleanName.Replace(" ", ".").ToLowerInvariant()}@dedagroup.it";
                return $"{cleanName} <{fallbackEmail}>";
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore simulazione email per '{name}': {ex.Message}");
                return $"{name} <{name.Replace(" ", ".").ToLowerInvariant()}@dedagroup.it>";
            }
        }

        /// <summary>
        /// Costruisce l'oggetto email simulato
        /// </summary>
        private string BuildEmailSubject(CommentData data)
        {
            var wbsCode = ExtractWbsCode(data.WBS);
            return $"{data.ClientName}: {data.TicketKey} - {data.TicketSummary} - {wbsCode}";
        }

        /// <summary>
        /// Estrae il codice WBS (prima parte prima del -)
        /// </summary>
        private string ExtractWbsCode(string wbsComplete)
        {
            if (string.IsNullOrWhiteSpace(wbsComplete))
                return "WBS-000";

            var parts = wbsComplete.Split('-');
            return parts[0].Trim();
        }

        /// <summary>
        /// Ottiene il nome visualizzato del template
        /// </summary>
        private string GetTemplateDisplayName(EmailTemplateService.TemplateType templateType)
        {
            return templateType switch
            {
                EmailTemplateService.TemplateType.SingleIntervention => "Singolo Intervento",
                EmailTemplateService.TemplateType.MultipleInterventions => "Interventi Multipli",
                EmailTemplateService.TemplateType.ToBeAgreed => "Accordo da Definire",
                _ => "Sconosciuto"
            };
        }

        /// <summary>
        /// Valida i dati per la generazione del commento
        /// </summary>
        private bool ValidateCommentData(CommentData data)
        {
            if (data == null)
            {
                _logger.LogError("CommentData è null");
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.TicketKey))
            {
                _logger.LogError("TicketKey è obbligatorio");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Genera un commento di errore standard
        /// </summary>
        private string GenerateErrorComment(string errorMessage)
        {
            return $"❌ ERRORE GENERAZIONE COMMENTO\n" +
                   $"Timestamp: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                   $"Dettaglio: {errorMessage}\n\n" +
                   $"Contattare l'amministratore di sistema.";
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Verifica se un tipo di commento è supportato
        /// </summary>
        public bool IsCommentTypeSupported(CommentType commentType)
        {
            return Enum.IsDefined(typeof(CommentType), commentType);
        }

        /// <summary>
        /// Ottiene tutti i tipi di commento disponibili
        /// </summary>
        public Dictionary<CommentType, string> GetAvailableCommentTypes()
        {
            return new Dictionary<CommentType, string>
            {
                { CommentType.ForwardedEmail, "Email inoltrata di pianificazione" },
                { CommentType.Simple, "Commento semplice" },
                { CommentType.System, "Commento automatico di sistema" }
            };
        }

        #endregion

        #region Data Model

        /// <summary>
        /// Modello dati per la generazione dei commenti
        /// </summary>
        public class CommentData
        {
            // Dati ticket base
            public string TicketKey { get; set; } = "";
            public string TicketSummary { get; set; } = "";
            public string ClientName { get; set; } = "";
            public string WBS { get; set; } = "";

            // Dati pianificazione
            public EmailTemplateService.TemplateType TemplateType { get; set; }
            public string ConsultantName { get; set; } = "";
            public string InterventionDate { get; set; } = "";
            public string InterventionTime { get; set; } = "";
            public string ClientPhone { get; set; } = "";

            // Nomi delle persone (per simulazione email)
            public string ProjectManagerName { get; set; } = "";
            public string CommercialName { get; set; } = "";

            // Email destinatari
            public string ReporterEmail { get; set; } = "";
            public string ConsultantEmail { get; set; } = "";
            public string ProjectManagerEmail { get; set; } = "";
            public string CommercialEmail { get; set; } = "";

            /// <summary>
            /// Valida che i dati minimi siano presenti
            /// </summary>
            public bool IsValid()
            {
                return !string.IsNullOrWhiteSpace(TicketKey) &&
                       !string.IsNullOrWhiteSpace(ClientName);
            }

            /// <summary>
            /// Crea CommentData da PlanningTicketData esistente
            /// </summary>
            public static CommentData FromPlanningData(object planningData, string ticketSummary)
            {
                // Questo metodo sarà implementato nello STEP 3
                // quando avremo accesso alla classe PlanningTicketData
                throw new NotImplementedException("Sarà implementato nello STEP 3");
            }
        }

        #endregion
    }
}