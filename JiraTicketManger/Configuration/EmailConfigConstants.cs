using System;
using System.Collections.Generic;

namespace JiraTicketManager.Configuration
{
    /// <summary>
    /// Configurazione email hardcoded nel codice
    /// NON VISIBILE agli utenti finali
    /// </summary>
    internal static class EmailConfigConstants
    {
        #region Gmail Configuration

        /// <summary>
        /// Account Gmail dedicato per l'invio automatico dei log
        /// </summary>
        public const string SENDER_EMAIL = "jiramanager-logs@gmail.com";

        /// <summary>
        /// Password dell'app Gmail (NON la password normale!)
        /// ISTRUZIONI per ottenere password app:
        /// 1. Vai su https://myaccount.google.com/security
        /// 2. Abilita autenticazione a 2 fattori
        /// 3. Vai su "Password per le app"
        /// 4. Genera password per "Jira Manager"
        /// 5. Sostituisci "your-gmail-app-password" con la password generata
        /// </summary>
        public const string SENDER_PASSWORD = "your-gmail-app-password";

        /// <summary>
        /// Nome visualizzato come mittente
        /// </summary>
        public const string SENDER_DISPLAY_NAME = "Jira Manager System";

        #endregion

        #region SMTP Configuration

        /// <summary>
        /// Server SMTP Gmail
        /// </summary>
        public const string SMTP_SERVER = "smtp.gmail.com";

        /// <summary>
        /// Porta SMTP Gmail con TLS
        /// </summary>
        public const int SMTP_PORT = 587;

        /// <summary>
        /// Abilita SSL per Gmail
        /// </summary>
        public const bool ENABLE_SSL = true;

        #endregion

        #region Admin Recipients

        /// <summary>
        /// Lista degli amministratori che riceveranno i report
        /// MODIFICA QUI per aggiungere/rimuovere amministratori
        /// </summary>
        public static readonly List<string> ADMIN_EMAILS = new List<string>
        {
            "admin@dedagroup.com",           // Admin principale
            "it.support@dedagroup.com",      // Supporto IT
            // "backup.admin@dedagroup.com", // Admin backup (decommentare se necessario)
        };

        #endregion

        #region Report Settings

        /// <summary>
        /// Abilita invio report settimanali automatici
        /// </summary>
        public const bool ENABLE_WEEKLY_REPORTS = true;

        /// <summary>
        /// Giorno della settimana per invio report
        /// </summary>
        public const DayOfWeek WEEKLY_REPORT_DAY = DayOfWeek.Monday;

        /// <summary>
        /// Ora di invio report settimanali (08:00)
        /// </summary>
        public static readonly TimeSpan WEEKLY_REPORT_TIME = new TimeSpan(8, 0, 0);

        #endregion

        #region Alert Settings

        /// <summary>
        /// Abilita alert critici immediati
        /// </summary>
        public const bool ENABLE_CRITICAL_ALERTS = true;

        /// <summary>
        /// Soglia login falliti per attivare alert
        /// </summary>
        public const int CRITICAL_FAILED_LOGINS_THRESHOLD = 3;

        /// <summary>
        /// Periodo di cooldown tra alert (2 ore)
        /// Evita spam di notifiche
        /// </summary>
        public static readonly TimeSpan ALERT_COOLDOWN_PERIOD = TimeSpan.FromHours(2);

        #endregion

        #region Company Settings

        /// <summary>
        /// Nome dell'azienda per i report
        /// </summary>
        public const string COMPANY_NAME = "Dedagroup";

        /// <summary>
        /// Dominio email aziendale autorizzato
        /// </summary>
        public const string AUTHORIZED_EMAIL_DOMAIN = "@dedagroup.com";

        /// <summary>
        /// Ambiente di distribuzione
        /// </summary>
        public const string ENVIRONMENT = "Production"; // "Development" per test

        #endregion

        #region Security Settings

        /// <summary>
        /// Abilita crittografia delle password (futuro)
        /// </summary>
        public const bool ENCRYPT_PASSWORDS = false; // TODO: Implementare crittografia

        /// <summary>
        /// Mantieni solo le ultime N entries nei log per privacy
        /// </summary>
        public const int MAX_LOG_ENTRIES_TO_KEEP = 1000;

        #endregion

        

        /// <summary>
        /// Verifica se la configurazione è valida
        /// </summary>
        public static bool IsConfigurationValid()
        {
            return !string.IsNullOrEmpty(SENDER_EMAIL) &&
                   !string.IsNullOrEmpty(SENDER_PASSWORD) &&
                   SENDER_PASSWORD != "your-gmail-app-password" && // Password deve essere cambiata
                   ADMIN_EMAILS.Count > 0;
        }

        /// <summary>
        /// Ottieni messaggio di errore se configurazione non valida
        /// </summary>
        public static string GetConfigurationError()
        {
            if (string.IsNullOrEmpty(SENDER_EMAIL))
                return "SENDER_EMAIL non configurato";

            if (string.IsNullOrEmpty(SENDER_PASSWORD) || SENDER_PASSWORD == "your-gmail-app-password")
                return "SENDER_PASSWORD non configurato - Sostituire con password app Gmail";

            if (ADMIN_EMAILS.Count == 0)
                return "Nessun amministratore configurato in ADMIN_EMAILS";

            return "Configurazione valida";
        }
    }
}