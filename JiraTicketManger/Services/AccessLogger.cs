using System;
using System.IO;
using System.Text;
using System.Globalization;
using JiraTicketManager.Models; // ✅ USA IL TUO ENUM ESISTENTE

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Sistema di logging dedicato per tracciamento accessi utenti
    /// File separato: access_log.txt nella root del progetto
    /// </summary>
    public class AccessLogger
    {
        #region Constants & Fields

        private static readonly string ACCESS_LOG_FILE = Path.Combine(Application.StartupPath, "access_log.txt");
        private static readonly object _lockObject = new object();
        private static AccessLogger _instance;

        #endregion

        #region Singleton Pattern

        /// <summary>
        /// Istanza singleton per evitare conflitti di scrittura
        /// </summary>
        public static AccessLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AccessLogger();
                }
                return _instance;
            }
        }

        private AccessLogger()
        {
            InitializeAccessLog();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Inizializza il file di log degli accessi
        /// </summary>
        private void InitializeAccessLog()
        {
            try
            {
                // Crea header se il file non esiste
                if (!File.Exists(ACCESS_LOG_FILE))
                {
                    CreateAccessLogHeader();
                }
            }
            catch (Exception ex)
            {
                // Log di fallback nel sistema principale
                var logger = LoggingService.CreateForComponent("AccessLogger", LoggingService.LogArea.System);
                logger.LogError("InitializeAccessLog", ex);
            }
        }

        /// <summary>
        /// Crea l'header del file di log degli accessi
        /// </summary>
        private void CreateAccessLogHeader()
        {
            var header = new StringBuilder();
            header.AppendLine("=====================================");
            header.AppendLine("    JIRA TICKET MANAGER - ACCESS LOG");
            header.AppendLine("=====================================");
            header.AppendLine("Formato: [TIMESTAMP] | [TIPO] | [UTENTE] | [METODO_AUTH] | [DETTAGLI]");
            header.AppendLine();

            File.WriteAllText(ACCESS_LOG_FILE, header.ToString(), Encoding.UTF8);
        }

        #endregion

        #region Public Logging Methods

        /// <summary>
        /// Registra un accesso riuscito
        /// </summary>
        /// <param name="userEmail">Email dell'utente</param>
        /// <param name="authMethod">Metodo di autenticazione utilizzato</param>
        /// <param name="additionalInfo">Informazioni aggiuntive opzionali</param>
        public void LogSuccessfulLogin(string userEmail, AuthenticationMethod authMethod, string additionalInfo = "")
        {
            var entry = CreateLogEntry("LOGIN_SUCCESS", userEmail, authMethod, additionalInfo);
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra un tentativo di accesso fallito
        /// </summary>
        /// <param name="userEmail">Email dell'utente (se disponibile)</param>
        /// <param name="authMethod">Metodo di autenticazione tentato</param>
        /// <param name="reason">Motivo del fallimento</param>
        public void LogFailedLogin(string userEmail, AuthenticationMethod authMethod, string reason)
        {
            var entry = CreateLogEntry("LOGIN_FAILED", userEmail ?? "UNKNOWN", authMethod, $"Reason: {reason}");
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra la chiusura di una sessione
        /// </summary>
        /// <param name="userEmail">Email dell'utente</param>
        /// <param name="sessionDuration">Durata della sessione</param>
        public void LogSessionEnd(string userEmail, TimeSpan sessionDuration)
        {
            var durationStr = $"Duration: {sessionDuration.Hours:D2}h {sessionDuration.Minutes:D2}m {sessionDuration.Seconds:D2}s";
            var entry = CreateLogEntry("SESSION_END", userEmail, AuthenticationMethod.MicrosoftSSO, durationStr);
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra un evento di sicurezza
        /// </summary>
        /// <param name="userEmail">Email dell'utente</param>
        /// <param name="securityEvent">Tipo di evento di sicurezza</param>
        /// <param name="details">Dettagli dell'evento</param>
        public void LogSecurityEvent(string userEmail, string securityEvent, string details = "")
        {
            var entry = CreateLogEntry($"SECURITY_{securityEvent.ToUpper()}", userEmail, AuthenticationMethod.MicrosoftSSO, details);
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra un errore di autenticazione
        /// </summary>
        /// <param name="error">Descrizione dell'errore</param>
        /// <param name="context">Contesto in cui è avvenuto l'errore</param>
        public void LogAuthenticationError(string error, string context = "")
        {
            var entry = CreateLogEntry("AUTH_ERROR", "SYSTEM", AuthenticationMethod.MicrosoftSSO, $"{error} | Context: {context}");
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra inizio sessione applicazione
        /// </summary>
        public void LogApplicationStart()
        {
            var entry = CreateLogEntry("APP_START", "SYSTEM", AuthenticationMethod.MicrosoftSSO, $"Version: {GetApplicationVersion()}");
            WriteToAccessLog(entry);
        }

        /// <summary>
        /// Registra chiusura applicazione
        /// </summary>
        public void LogApplicationEnd()
        {
            var entry = CreateLogEntry("APP_END", "SYSTEM", AuthenticationMethod.MicrosoftSSO, "Application shutdown");
            WriteToAccessLog(entry);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Crea una entry di log formattata
        /// </summary>
        private string CreateLogEntry(string eventType, string userEmail, AuthenticationMethod authMethod, string details)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            var authMethodStr = authMethod.ToString().ToUpper();

            // Sanitizza email (nasconde parte per privacy se necessario)
            var sanitizedEmail = SanitizeEmail(userEmail);

            return $"[{timestamp}] | {eventType,-15} | {sanitizedEmail,-30} | {authMethodStr,-12} | {details}";
        }

        /// <summary>
        /// Sanitizza l'email per il logging (opzionale, per privacy)
        /// </summary>
        private string SanitizeEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || email == "SYSTEM" || email == "UNKNOWN")
                return email;

            try
            {
                // Per ora torna l'email completa, ma puoi modificare per nascondere parte
                return email;
            }
            catch
            {
                return "INVALID_EMAIL";
            }
        }

        /// <summary>
        /// Scrive nel file di log con thread safety
        /// </summary>
        private void WriteToAccessLog(string logEntry)
        {
            lock (_lockObject)
            {
                try
                {
                    File.AppendAllText(ACCESS_LOG_FILE, logEntry + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    // Fallback: scrivi nel sistema di logging principale
                    var logger = LoggingService.CreateForComponent("AccessLogger", LoggingService.LogArea.System);
                    logger.LogError($"WriteToAccessLog failed: {ex.Message} | Original entry: {logEntry}");
                }
            }
        }

        /// <summary>
        /// Ottiene la versione dell'applicazione
        /// </summary>
        private string GetApplicationVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Ottieni il percorso del file di log degli accessi
        /// </summary>
        public string GetAccessLogFilePath() => ACCESS_LOG_FILE;

        /// <summary>
        /// Verifica se il file di log degli accessi esiste
        /// </summary>
        public bool AccessLogExists() => File.Exists(ACCESS_LOG_FILE);

        /// <summary>
        /// Ottieni le dimensioni del file di log degli accessi
        /// </summary>
        public long GetAccessLogSize()
        {
            try
            {
                if (File.Exists(ACCESS_LOG_FILE))
                {
                    return new FileInfo(ACCESS_LOG_FILE).Length;
                }
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Ottieni le ultime N righe del log degli accessi
        /// </summary>
        public string[] GetRecentAccessEntries(int count = 10)
        {
            try
            {
                if (!File.Exists(ACCESS_LOG_FILE))
                    return new string[0];

                var lines = File.ReadAllLines(ACCESS_LOG_FILE, Encoding.UTF8);

                // Filtra solo le righe di log (esclude header)
                var logLines = Array.FindAll(lines, line => line.StartsWith("["));

                if (logLines.Length <= count)
                    return logLines;

                // Restituisce le ultime N righe
                var result = new string[count];
                Array.Copy(logLines, logLines.Length - count, result, 0, count);
                return result;
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("AccessLogger", LoggingService.LogArea.System);
                logger.LogError("GetRecentAccessEntries", ex);
                return new string[] { $"Errore lettura log: {ex.Message}" };
            }
        }

        #endregion
    }
}