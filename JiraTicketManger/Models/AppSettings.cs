using System;
using System.Collections.Generic;

namespace JiraTicketManager.Models
{
    public class AppSettings
    {
        public UserSettings UserSettings { get; set; } = new UserSettings();
        public JiraSettings JiraSettings { get; set; } = new JiraSettings();
        public ApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();
        public EncryptionSettings EncryptionSettings { get; set; } = new EncryptionSettings();
    }

    public class UserSettings
    {
        public string LastUsername { get; set; } = "";
        public bool RememberUsername { get; set; } = true;
        public DateTime? LastLoginDate { get; set; }
        public string AuthenticationMethod { get; set; } = "MicrosoftSSO";
    }

    public class JiraSettings
    {
        public string Domain { get; set; } = "https://deda-next.atlassian.net";
        public string Username_Encrypted { get; set; } = "";
        public string Token_Encrypted { get; set; } = "";
        public string Project { get; set; } = "CC";
        public bool EnableDebugLogging { get; set; } = true;
        public int ConnectionTimeout { get; set; } = 10000;
    }

    // ✅ SOLUZIONE: Aggiungi partial qui per permettere estensioni
    public partial class ApplicationSettings
    {
        public bool ToastNotifications { get; set; } = true;
        public string LogLevel { get; set; } = "Info";
        public bool AutoSaveCredentials { get; set; } = true;
        public bool CacheCleanupEnabled { get; set; } = true;
    }

    public class EncryptionSettings
    {
        public bool UseDPAPI { get; set; } = true;
        public string EncryptionVersion { get; set; } = "1.0";
    }

    // ✅ NUOVA CLASSE - Configurazione logging granulare
    public class LoggingSettings
    {
        public string CurrentPreset { get; set; } = "Production";
        public string LogLevel { get; set; } = "Info";
        public bool EnableDebugOutput { get; set; } = false;
        public List<string> EnabledAreas { get; set; } = new List<string> { "Errors", "System" };
        public bool IsLoggingEnabled { get; set; } = true;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}

// ✅ ESTENSIONE PARTIAL - In un file separato o nella stessa unità
namespace JiraTicketManager.Models
{
    public partial class ApplicationSettings
    {
        // ✅ NUOVA PROPRIETÀ - Configurazione logging avanzata
        public LoggingSettings LoggingSettings { get; set; } = new LoggingSettings();
    }
}