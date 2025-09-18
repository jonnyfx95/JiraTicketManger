using System;
using System.Text;
using System.Web;
using JiraTicketManager.Services;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

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

        private static string _cachedSignature = null;
        private static DateTime _lastSignatureCheck = DateTime.MinValue;
        private static readonly TimeSpan SignatureCacheTime = TimeSpan.FromMinutes(10);

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

                // Genera il contenuto base del template
                string templateContent = templateType switch
                {
                    TemplateType.SingleIntervention => GenerateSingleInterventionHtml(cleanConsultant, cleanDate, cleanTime, cleanPhone),
                    TemplateType.MultipleInterventions => GenerateMultipleInterventionsHtml(cleanConsultant, cleanDate, cleanPhone),
                    TemplateType.ToBeAgreed => GenerateToBeAgreedHtml(cleanConsultant, cleanPhone),
                    _ => "<div style='color:red;'>Template non valido selezionato.</div>"
                };

                // 🔥 NUOVA FUNZIONALITÀ: Aggiunge la firma Outlook
                var htmlWithSignature = AppendOutlookSignatureToHtml(templateContent);

                _logger.LogInfo($"HTML generato con firma, lunghezza: {htmlWithSignature.Length}");
                return htmlWithSignature;
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

        #region Outlook Signature Integration

        /// <summary>
        /// Aggiunge la firma Outlook al contenuto HTML esistente
        /// </summary>
        /// <param name="existingHtml">HTML del template esistente</param>
        /// <returns>HTML completo con firma</returns>
        private string AppendOutlookSignatureToHtml(string existingHtml)
        {
            try
            {
                _logger.LogInfo("🔍 Tentativo aggiunta firma Outlook all'HTML");

                // Ottieni la firma HTML
                var signature = GetOutlookHtmlSignature();

                if (string.IsNullOrWhiteSpace(signature))
                {
                    _logger.LogWarning("❌ Nessuna firma Outlook trovata - uso HTML senza firma");
                    return WrapInCompleteHtml(existingHtml);
                }

                _logger.LogInfo($"✅ Firma Outlook trovata ({signature.Length} caratteri)");

                // Combina template + firma
                var combinedContent = CombineTemplateAndSignature(existingHtml, signature);

                return WrapInCompleteHtml(combinedContent);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiunta firma", ex);
                return WrapInCompleteHtml(existingHtml);
            }
        }

        /// <summary>
        /// Ottiene la firma HTML di Outlook con cache intelligente
        /// </summary>
        /// <returns>HTML della firma o stringa vuota</returns>
        private string GetOutlookHtmlSignature()
        {
            try
            {
                // Cache per evitare letture multiple
                if (_cachedSignature != null &&
                    DateTime.Now - _lastSignatureCheck < SignatureCacheTime)
                {
                    _logger.LogDebug("Uso firma dalla cache");
                    return _cachedSignature;
                }

                _logger.LogInfo("=== RICERCA FIRMA OUTLOOK HTML ===");

                // Strategia 1: Registry (firma default dell'account)
                var signatureFromRegistry = GetSignatureFromRegistry();
                if (!string.IsNullOrWhiteSpace(signatureFromRegistry))
                {
                    _logger.LogInfo("✅ Firma trovata dal Registry");
                    CacheSignature(signatureFromRegistry);
                    return signatureFromRegistry;
                }

                // Strategia 2: Cartella Signatures (prima firma trovata)
                var signatureFromFolder = GetSignatureFromFolder();
                if (!string.IsNullOrWhiteSpace(signatureFromFolder))
                {
                    _logger.LogInfo("✅ Firma trovata dalla cartella Signatures");
                    CacheSignature(signatureFromFolder);
                    return signatureFromFolder;
                }

                _logger.LogWarning("❌ Nessuna firma HTML trovata");
                CacheSignature(""); // Cache anche il risultato vuoto
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca firma", ex);
                CacheSignature("");
                return "";
            }
        }

        /// <summary>
        /// Legge la firma di default dal Registry
        /// </summary>
        private string GetSignatureFromRegistry()
        {
            try
            {
                var officeVersions = new[] { "16.0", "15.0", "14.0", "12.0" };

                foreach (var version in officeVersions)
                {
                    _logger.LogDebug($"Testando Office {version}...");

                    var registryPath = $@"HKEY_CURRENT_USER\Software\Microsoft\Office\{version}\Common\MailSettings";

                    try
                    {
                        var newSignatureName = Registry.GetValue(registryPath, "NewSignature", null) as string;
                        var replySignatureName = Registry.GetValue(registryPath, "ReplySignature", null) as string;

                        var signatureName = newSignatureName ?? replySignatureName;

                        if (!string.IsNullOrWhiteSpace(signatureName))
                        {
                            _logger.LogInfo($"✅ Trovata firma dal Registry Office {version}: {signatureName}");

                            var signatureContent = ReadSignatureFile(signatureName);
                            if (!string.IsNullOrWhiteSpace(signatureContent))
                                return signatureContent;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug($"Registry Office {version}: {ex.Message}");
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore lettura firma da Registry", ex);
                return "";
            }
        }

        /// <summary>
        /// Legge la prima firma trovata nella cartella Signatures
        /// </summary>
        private string GetSignatureFromFolder()
        {
            try
            {
                var signaturesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Signatures");

                if (!Directory.Exists(signaturesPath))
                {
                    _logger.LogWarning($"Cartella firme non trovata: {signaturesPath}");
                    return "";
                }

                _logger.LogInfo($"🔍 Ricerca in: {signaturesPath}");

                var htmlFiles = Directory.GetFiles(signaturesPath, "*.htm");

                _logger.LogInfo($"File .htm trovati: {htmlFiles.Length}");
                foreach (var file in htmlFiles)
                {
                    _logger.LogInfo($"  - {Path.GetFileName(file)}");
                }

                if (htmlFiles.Length > 0)
                {
                    var firstSignature = htmlFiles[0];
                    _logger.LogInfo($"PRIMA FIRMA TROVATA: {Path.GetFileName(firstSignature)}");

                    return ReadSignatureFile(Path.GetFileNameWithoutExtension(firstSignature));
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore lettura cartella firme", ex);
                return "";
            }
        }

        /// <summary>
        /// Legge il contenuto di un file firma
        /// </summary>
        private string ReadSignatureFile(string signatureName)
        {
            try
            {
                var signaturesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Signatures");

                var signatureFile = Path.Combine(signaturesPath, $"{signatureName}.htm");

                if (!File.Exists(signatureFile))
                {
                    _logger.LogWarning($"File firma non trovato: {signatureFile}");
                    return "";
                }

                var content = File.ReadAllText(signatureFile, Encoding.UTF8);
                _logger.LogInfo($"Firma letta: {content.Length} caratteri");

                // 🔥 NUOVA FUNZIONALITÀ: Processa le immagini prima di restituire
                var processedContent = ProcessSignatureImages(content, signatureName);

                // Log del contenuto finale per debug (solo le prime 300 caratteri)
                var preview = processedContent.Length > 300 ? processedContent.Substring(0, 300) + "..." : processedContent;
                _logger.LogDebug($"Anteprima firma processata: {preview}");

                return processedContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore lettura file firma '{signatureName}'", ex);
                return "";
            }
        }

        /// <summary>
        /// Combina il contenuto del template con la firma
        /// </summary>
        private string CombineTemplateAndSignature(string templateHtml, string signatureHtml)
        {
            try
            {
                // Pulisci la firma da eventuali tag html/body completi
                var cleanSignature = CleanSignatureHtml(signatureHtml);

                // Usa solo un singolo <br> per evitare doppi a capo
                return $@"{templateHtml}

<br>
{cleanSignature}";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore combinazione template e firma", ex);
                return templateHtml; // Fallback senza firma
            }
        }

        /// <summary>
        /// Pulisce l'HTML della firma rimuovendo tag html/body se presenti
        /// </summary>
        private string CleanSignatureHtml(string signatureHtml)
        {
            if (string.IsNullOrWhiteSpace(signatureHtml))
                return "";

            try
            {
                var cleaned = signatureHtml;

                // Estrai solo il contenuto del body se presente, ma preserva gli stili inline
                var bodyStart = cleaned.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                var bodyEnd = cleaned.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);

                if (bodyStart >= 0 && bodyEnd >= 0)
                {
                    // Trova la fine del tag body di apertura
                    var bodyTagEnd = cleaned.IndexOf(">", bodyStart) + 1;
                    if (bodyTagEnd > bodyStart)
                    {
                        cleaned = cleaned.Substring(bodyTagEnd, bodyEnd - bodyTagEnd);
                    }
                }

                // Estrai gli stili CSS critici dall'header e convertili in stili inline
                cleaned = PreserveEssentialStyles(signatureHtml, cleaned);

                // Rimuovi solo gli elementi non necessari per l'email
                cleaned = RemoveUnnecessaryElements(cleaned);

                return cleaned.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia firma HTML", ex);
                return signatureHtml; // Fallback
            }
        }

        /// <summary>
        /// Preserva gli stili CSS essenziali convertendoli in stili inline
        /// </summary>
        private string PreserveEssentialStyles(string originalHtml, string bodyContent)
        {
            try
            {
                // Estrai gli stili MsoNormal dal CSS originale
                var msoNormalStyle = ExtractMsoNormalStyle(originalHtml);

                if (!string.IsNullOrEmpty(msoNormalStyle))
                {
                    // Applica gli stili inline ai paragrafi che avevano class="MsoNormal"
                    var msoNormalPattern = @"<p\s+class=([""']?)MsoNormal\1[^>]*>";
                    bodyContent = System.Text.RegularExpressions.Regex.Replace(
                        bodyContent,
                        msoNormalPattern,
                        match =>
                        {
                            var existingTag = match.Value;

                            // Estrai eventuali stili esistenti
                            var styleMatch = System.Text.RegularExpressions.Regex.Match(existingTag, @"style=([""'])([^""']*)\1");
                            var existingStyles = styleMatch.Success ? styleMatch.Groups[2].Value : "";

                            // Combina stili esistenti con MsoNormal
                            var combinedStyles = string.IsNullOrEmpty(existingStyles)
                                ? msoNormalStyle
                                : $"{existingStyles};{msoNormalStyle}";

                            // Ricostruisci il tag senza class ma con stili inline
                            var cleanedTag = System.Text.RegularExpressions.Regex.Replace(existingTag, @"\s+class=([""']?)MsoNormal\1", "");
                            cleanedTag = System.Text.RegularExpressions.Regex.Replace(cleanedTag, @"style=([""'])[^""']*\1", "");

                            return cleanedTag.Replace("<p", $"<p style=\"{combinedStyles}\"").Replace(" >", ">");
                        },
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                }

                return bodyContent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore preservazione stili: {ex.Message}");
                return bodyContent;
            }
        }

        private string ExtractMsoNormalStyle(string html)
        {
            try
            {
                // Cerca la definizione di .MsoNormal nel CSS
                var cssPattern = @"p\.MsoNormal[^{]*\{([^}]*)\}";
                var match = System.Text.RegularExpressions.Regex.Match(html, cssPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var cssRules = match.Groups[1].Value;

                    // Converte le regole CSS più importanti per la spaziatura
                    var inlineStyles = new List<string>();

                    // Margin
                    var marginMatch = System.Text.RegularExpressions.Regex.Match(cssRules, @"margin:\s*([^;]+)");
                    if (marginMatch.Success)
                        inlineStyles.Add($"margin:{marginMatch.Groups[1].Value.Trim()}");

                    // Margin-bottom (più specifico)
                    var marginBottomMatch = System.Text.RegularExpressions.Regex.Match(cssRules, @"margin-bottom:\s*([^;]+)");
                    if (marginBottomMatch.Success)
                        inlineStyles.Add($"margin-bottom:{marginBottomMatch.Groups[1].Value.Trim()}");

                    // Font-size
                    var fontSizeMatch = System.Text.RegularExpressions.Regex.Match(cssRules, @"font-size:\s*([^;]+)");
                    if (fontSizeMatch.Success)
                        inlineStyles.Add($"font-size:{fontSizeMatch.Groups[1].Value.Trim()}");

                    // Font-family
                    var fontFamilyMatch = System.Text.RegularExpressions.Regex.Match(cssRules, @"font-family:\s*([^;]+)");
                    if (fontFamilyMatch.Success)
                        inlineStyles.Add($"font-family:{fontFamilyMatch.Groups[1].Value.Trim()}");

                    return string.Join(";", inlineStyles);
                }

                // Fallback: stili di base simili a MsoNormal
                return "margin:0cm;font-size:12.0pt;font-family:Aptos,sans-serif";
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore estrazione stili MsoNormal: {ex.Message}");
                return "margin:0cm;font-size:12.0pt;font-family:Aptos,sans-serif";
            }
        }

        /// <summary>
        /// Rimuove elementi non necessari ma preserva la struttura essenziale
        /// </summary>
        private string RemoveUnnecessaryElements(string html)
        {
            try
            {
                var cleaned = html;

                // Rimuovi commenti HTML ma mantieni tutto il resto
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<!--.*?-->", "", System.Text.RegularExpressions.RegexOptions.Singleline);

                // Rimuovi tag XML Office specifici ma mantieni contenuto
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<\?xml[^>]*>", "");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"</?o:[^>]*>", "");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"</?w:[^>]*>", "");
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"</?v:[^>]*>", "");

                // Mantieni div.WordSection1 ma rimuovi solo la classe
                cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"<div\s+class=([""']?)WordSection1\1[^>]*>", "<div>");

                return cleaned;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore rimozione elementi non necessari: {ex.Message}");
                return html;
            }
        }

   




        /// <summary>
        /// Avvolge il contenuto in HTML completo
        /// </summary>
        private string WrapInCompleteHtml(string content)
        {
            return $@"<!DOCTYPE html>
                            <html>
                            <head>
                                <meta charset=""UTF-8"">
                                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                            </head>
                            <body style=""font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; line-height: 1.4;"">
                            {content}
                            </body>
                            </html>";
        }

        /// <summary>
        /// Salva la firma nella cache
        /// </summary>
        private void CacheSignature(string signature)
        {
            _cachedSignature = signature;
            _lastSignatureCheck = DateTime.Now;
        }

        /// <summary>
        /// Processa le immagini nella firma HTML convertendole in Base64
        /// </summary>
        /// <param name="htmlContent">HTML della firma con immagini linked</param>
        /// <param name="signatureName">Nome del file firma (senza estensione)</param>
        /// <returns>HTML con immagini convertite in Base64</returns>
        private string ProcessSignatureImages(string htmlContent, string signatureName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(htmlContent))
                    return htmlContent;

                _logger.LogInfo("🖼️ Inizio processamento immagini firma");

                // Usa Regex per trovare tutti i tag img
                var imgPattern = @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*>";
                var regex = new System.Text.RegularExpressions.Regex(imgPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                var processedHtml = htmlContent;
                var matches = regex.Matches(htmlContent);

                _logger.LogInfo($"🔍 Trovati {matches.Count} tag <img>");

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var fullImgTag = match.Value;
                    var srcValue = match.Groups[1].Value;

                    _logger.LogInfo($"📸 Processamento immagine: {srcValue}");

                    // Verifica se è un percorso locale (non già base64 o URL)
                    if (IsLocalImagePath(srcValue))
                    {
                        var base64Image = ConvertImageToBase64(srcValue, signatureName);
                        if (!string.IsNullOrEmpty(base64Image))
                        {
                            // Sostituisci il src nell'HTML
                            var newImgTag = fullImgTag.Replace($"src=\"{srcValue}\"", $"src=\"{base64Image}\"")
                                                     .Replace($"src='{srcValue}'", $"src='{base64Image}'");

                            processedHtml = processedHtml.Replace(fullImgTag, newImgTag);
                            _logger.LogInfo($"✅ Immagine convertita: {srcValue} -> Base64 ({base64Image.Length} caratteri)");
                        }
                        else
                        {
                            _logger.LogWarning($"❌ Impossibile convertire: {srcValue}");
                        }
                    }
                    else
                    {
                        _logger.LogInfo($"⏭️ Saltata (già processata o URL remota): {srcValue}");
                    }
                }

                _logger.LogInfo($"🎉 Processamento completato. HTML finale: {processedHtml.Length} caratteri");
                return processedHtml;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore processamento immagini firma", ex);
                return htmlContent; // Fallback: ritorna HTML originale
            }
        }

        /// <summary>
        /// Verifica se il percorso dell'immagine è locale (non base64 o URL)
        /// </summary>
        /// <param name="srcValue">Valore dell'attributo src</param>
        /// <returns>True se è un percorso locale da convertire</returns>
        private bool IsLocalImagePath(string srcValue)
        {
            if (string.IsNullOrWhiteSpace(srcValue))
                return false;

            // Salta se già base64
            if (srcValue.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                return false;

            // Salta se URL remota
            if (srcValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                srcValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            // Salta se CID (Content-ID per email)
            if (srcValue.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
                return false;

            // È un percorso locale se contiene riferimenti ai file della firma
            return srcValue.Contains("_file/") || srcValue.Contains("_files/");
        }

        /// <summary>
        /// Converte un'immagine locale in stringa Base64
        /// </summary>
        /// <param name="imagePath">Percorso relativo dell'immagine</param>
        /// <param name="signatureName">Nome del file firma</param>
        /// <returns>Stringa data:image/... o stringa vuota se errore</returns>
        private string ConvertImageToBase64(string imagePath, string signatureName)
        {
            try
            {
                // Costruisci il percorso completo del file immagine
                var signaturesPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft", "Signatures");

                // Decodifica il percorso URL (es: %20 -> spazio)
                var decodedPath = System.Web.HttpUtility.UrlDecode(imagePath);

                // Il percorso nell'HTML è relativo alla cartella della firma
                var fullImagePath = Path.Combine(signaturesPath, decodedPath);

                _logger.LogInfo($"🔍 Ricerca immagine: {fullImagePath}");

                if (!File.Exists(fullImagePath))
                {
                    _logger.LogWarning($"❌ File immagine non trovato: {fullImagePath}");
                    return "";
                }

                // Leggi il file come byte array
                var imageBytes = File.ReadAllBytes(fullImagePath);

                // Determina il tipo MIME
                var mimeType = GetImageMimeType(fullImagePath);

                // Converti in Base64
                var base64String = Convert.ToBase64String(imageBytes);
                var result = $"data:{mimeType};base64,{base64String}";

                _logger.LogInfo($"✅ Immagine convertita: {imageBytes.Length} bytes -> {base64String.Length} caratteri Base64");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore conversione immagine '{imagePath}'", ex);
                return "";
            }
        }

        /// <summary>
        /// Determina il tipo MIME di un file immagine basandosi sull'estensione
        /// </summary>
        /// <param name="filePath">Percorso del file</param>
        /// <returns>Tipo MIME (es: image/png)</returns>
        private string GetImageMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "image/png" // Default fallback
            };
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

        /// <summary>
        /// Metodo pubblico per testare la lettura della firma (chiamabile dai DevelopmentTests)
        /// </summary>
        public string DebugGetOutlookSignature()
        {
            _logger.LogInfo("🧪 DEBUG: Lettura firma Outlook CON processamento immagini");

            // Forza il refresh della cache
            _cachedSignature = null;

            var signature = GetOutlookHtmlSignature();

            _logger.LogInfo($"🧪 DEBUG: Firma trovata: {!string.IsNullOrEmpty(signature)}");
            if (!string.IsNullOrEmpty(signature))
            {
                _logger.LogInfo($"🧪 DEBUG: Lunghezza firma: {signature.Length}");

                // Conta le immagini Base64 nella firma
                var base64Count = CountBase64Images(signature);
                _logger.LogInfo($"🧪 DEBUG: Immagini Base64 trovate: {base64Count}");

                // Verifica dimensioni approssimative
                if (signature.Length > 50000) // Firma con immagini dovrebbe essere più grande
                {
                    _logger.LogInfo("🧪 DEBUG: Dimensioni compatibili con immagini incluse");
                }
                else
                {
                    _logger.LogWarning("🧪 DEBUG: Dimensioni sospette - potrebbero mancare immagini");
                }
            }

            return signature;
        }

        /// <summary>
        /// Conta le immagini Base64 nell'HTML
        /// </summary>
        private int CountBase64Images(string htmlContent)
        {
            if (string.IsNullOrEmpty(htmlContent))
                return 0;

            var base64Pattern = @"src\s*=\s*[""']data:image/[^""']*[""']";
            var matches = Regex.Matches(htmlContent, base64Pattern, RegexOptions.IgnoreCase);
            return matches.Count;
        }
    }
}