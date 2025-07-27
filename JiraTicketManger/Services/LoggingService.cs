using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Sistema di logging migliorato con gestione consolidata dei file
    /// MIGLIORAMENTI:
    /// - File consolidati invece di timestamp per sessione
    /// - Rotazione automatica e cleanup
    /// - Configurazione dinamica runtime
    /// - Mantiene compatibilità con codice esistente
    /// </summary>
    public class LoggingService
    {
        #region Configurazione Logging

        /// <summary>
        /// Aree di logging disponibili - aggiungere qui nuove aree
        /// </summary>
        [Flags]
        public enum LogArea
        {
            [Description("Autenticazione e credenziali")]
            Authentication = 1,

            [Description("API Jira e comunicazione")]
            JiraApi = 2,

            [Description("Interfaccia utente")]
            UI = 4,

            [Description("Gestione configurazione")]
            Configuration = 8,

            [Description("Export Excel e file")]
            Export = 16,

            [Description("Test e validazioni")]
            Testing = 32,

            [Description("Gestione errori")]
            Errors = 64,

            [Description("WebView2 e browser")]
            WebView = 128,

            [Description("Database e storage")]
            Database = 256,

            [Description("Sistema generale")]
            System = 512
        }

        /// <summary>
        /// Livelli di logging
        /// </summary>
        public enum LogLevel
        {
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Critical = 5
        }

        /// <summary>
        /// Configurazione corrente del logging
        /// Usa combinazioni di aree (es: LogArea.Errors | LogArea.Authentication)
        /// </summary>
        public static LogArea EnabledAreas { get; set; } =
            LogArea.Errors | LogArea.Authentication | LogArea.System; // Default: solo errori, auth e sistema

        /// <summary>
        /// Livello minimo di logging
        /// </summary>
        public static LogLevel MinimumLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// Abilita/disabilita completamente il logging
        /// </summary>
        public static bool IsLoggingEnabled { get; set; } = true;

        /// <summary>
        /// Abilita scrittura nel debug output di Visual Studio
        /// </summary>
        public static bool WriteToDebugOutput { get; set; } = true;

        /// <summary>
        /// Modalità debug temporaneo per sessioni di troubleshooting
        /// </summary>
        public static bool IsDebugSession { get; set; } = false;

        /// <summary>
        /// Giorni di ritenzione log prima del cleanup automatico
        /// </summary>
        public static int MaxLogDays { get; set; } = 7;

        #endregion

        #region File Consolidati (NUOVO)

        private static readonly string LOGS_DIR = Path.Combine(Application.StartupPath, "logs");
        private static readonly string APP_LOG = Path.Combine(LOGS_DIR, "application.log");
        private static readonly string DEBUG_LOG = Path.Combine(LOGS_DIR, "debug.log");
        private static readonly object _fileLock = new object();

        #endregion

        #region Proprietà Instance

        private readonly string _logFilePath;
        private readonly string _componentName;
        private readonly LogArea _componentArea;

        #endregion

        #region Costruttori (Mantenuti per compatibilità)

        public LoggingService(string componentName, LogArea area = LogArea.System)
        {
            _componentName = componentName;
            _componentArea = area;

            // MIGLIORAMENTO: Usa file consolidato invece di timestamp
            _logFilePath = GetConsolidatedLogFile(area);
        }

        public LoggingService(string componentName, LogArea area, string customLogPath)
        {
            _componentName = componentName;
            _componentArea = area;
            _logFilePath = customLogPath; // Mantiene comportamento legacy se specificato
        }

        /// <summary>
        /// Costruttore legacy per compatibilità con codice esistente
        /// </summary>
        public LoggingService(string componentName, string customLogPath)
        {
            _componentName = componentName;
            _componentArea = LogArea.System; // Default area per legacy
            _logFilePath = customLogPath;
        }

        #endregion

        #region Metodi di Logging Principali (Mantenuti)

        public void LogDebug(string message)
        {
            WriteLog(LogLevel.Debug, message);
        }

        public void LogInfo(string message)
        {
            WriteLog(LogLevel.Info, message);
        }

        public void LogWarning(string message)
        {
            WriteLog(LogLevel.Warning, message);
        }

        public void LogError(string message)
        {
            WriteLog(LogLevel.Error, message);
        }

        public void LogError(string method, Exception ex)
        {
            WriteLog(LogLevel.Error, $"{method}: {ex.Message}");
            WriteLog(LogLevel.Debug, $"StackTrace: {ex.StackTrace ?? "No stack trace"}");
        }

        public void LogCritical(string message)
        {
            WriteLog(LogLevel.Critical, message);
        }

        public void LogSession(string sessionName)
        {
            WriteLog(LogLevel.Info, $"=== {sessionName} ===");
        }

        /// <summary>
        /// Log condizionale - scrive solo se la condizione è vera
        /// </summary>
        public void LogIf(bool condition, LogLevel level, string message)
        {
            if (condition)
                WriteLog(level, message);
        }

        /// <summary>
        /// Log temporaneo - per debug di sviluppo, sempre visibile
        /// </summary>
        public void LogTemp(string message)
        {
            // Ignora configurazioni e scrive sempre (per debug temporaneo)
            string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [TEMP] [{_componentName}] {message}";
            WriteToFile(logEntry);

            if (WriteToDebugOutput)
                System.Diagnostics.Debug.WriteLine($"🔥 TEMP: {logEntry}");
        }

        #endregion

        #region Nuovi Metodi Statici per Gestione Consolidata

        /// <summary>
        /// Inizio sessione - logga nel file principale
        /// </summary>
        public static void StartSession(string sessionType = "APPLICATION")
        {
            WriteConsolidatedLog(LogLevel.Info, "SESSION", $"=== START {sessionType} SESSION ===");
            WriteConsolidatedLog(LogLevel.Info, "SYSTEM", $"Version: {Application.ProductVersion}");
            WriteConsolidatedLog(LogLevel.Info, "SYSTEM", $"OS: {Environment.OSVersion}");
        }

        /// <summary>
        /// Fine sessione
        /// </summary>
        public static void EndSession()
        {
            WriteConsolidatedLog(LogLevel.Info, "SESSION", "=== END SESSION ===");
        }

        /// <summary>
        /// Avvia sessione debug temporanea
        /// </summary>
        public static void StartDebugSession(string reason)
        {
            IsDebugSession = true;
            MinimumLevel = LogLevel.Debug;
            WriteConsolidatedLog(LogLevel.Debug, "DEBUG", $"🔧 DEBUG SESSION STARTED: {reason}");
        }

        /// <summary>
        /// Termina sessione debug
        /// </summary>
        public static void EndDebugSession()
        {
            WriteConsolidatedLog(LogLevel.Debug, "DEBUG", "🔧 DEBUG SESSION ENDED");
            IsDebugSession = false;
            MinimumLevel = LogLevel.Info;
        }

        /// <summary>
        /// Modalità produzione (solo warning/error)
        /// </summary>
        public static void SetProductionMode()
        {
            MinimumLevel = LogLevel.Warning;
            IsDebugSession = false;
            WriteConsolidatedLog(LogLevel.Info, "CONFIG", "🏭 PRODUCTION MODE ENABLED");
        }

        /// <summary>
        /// Modalità sviluppo (tutto abilitato)
        /// </summary>
        public static void SetDevelopmentMode()
        {
            MinimumLevel = LogLevel.Debug;
            IsDebugSession = true;
            WriteConsolidatedLog(LogLevel.Info, "CONFIG", "🛠️ DEVELOPMENT MODE ENABLED");
        }

        #endregion

        #region Implementazione Core Migliorata

        private void WriteLog(LogLevel level, string message)
        {
            // Controlli preliminari
            if (!IsLoggingEnabled) return;
            if (level < MinimumLevel && !IsDebugSession) return;
            if (!EnabledAreas.HasFlag(_componentArea) && level < LogLevel.Error && !IsDebugSession) return;

            // Crea entry di log
            string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{_componentArea}] [{_componentName}] {message}";

            // Scrivi su file (thread-safe)
            WriteToFile(logEntry);

            // Scrivi su debug output se abilitato
            if (WriteToDebugOutput)
            {
                string emoji = GetLevelEmoji(level);
                System.Diagnostics.Debug.WriteLine($"{emoji} {logEntry}");
            }

            // Cleanup periodico automatico
            if (DateTime.Now.Minute % 10 == 0 && DateTime.Now.Second < 5)
            {
                CleanupOldLogs();
            }
        }

        private void WriteToFile(string logEntry)
        {
            try
            {
                lock (_fileLock)
                {
                    EnsureLogDirectory();
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Ignora errori di scrittura per evitare crash
            }
        }

        private static void WriteConsolidatedLog(LogLevel level, string component, string message)
        {
            try
            {
                lock (_fileLock)
                {
                    EnsureLogDirectory();

                    string logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] [{level}] [{component}] {message}";
                    string targetFile = level >= LogLevel.Warning ? APP_LOG :
                                       (IsDebugSession ? DEBUG_LOG : APP_LOG);

                    File.AppendAllText(targetFile, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Ignora errori
            }
        }

        #endregion

        #region File Management (NUOVO)

        private static void EnsureLogDirectory()
        {
            if (!Directory.Exists(LOGS_DIR))
                Directory.CreateDirectory(LOGS_DIR);
        }

        private string GetConsolidatedLogFile(LogArea area)
        {
            EnsureLogDirectory();

            // Debug sempre in debug.log se sessione debug attiva
            if (IsDebugSession)
                return DEBUG_LOG;

            // File consolidato principale
            return APP_LOG;
        }

        private static void CleanupOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(LOGS_DIR, "*.log");
                var cutoffDate = DateTime.Now.AddDays(-MaxLogDays);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffDate)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // Ignora errori di cleanup
            }
        }

        /// <summary>
        /// Pulisce manualmente i file di log legacy con timestamp
        /// </summary>
        public static void CleanupLegacyLogs()
        {
            try
            {
                if (!Directory.Exists(LOGS_DIR)) return;

                var legacyFiles = Directory.GetFiles(LOGS_DIR, "*_202*.txt"); // File con timestamp
                int cleaned = 0;

                foreach (var file in legacyFiles)
                {
                    try
                    {
                        File.Delete(file);
                        cleaned++;
                    }
                    catch
                    {
                        // Ignora errori su singoli file
                    }
                }

                WriteConsolidatedLog(LogLevel.Info, "CLEANUP", $"Rimossi {cleaned} file di log legacy");
            }
            catch (Exception ex)
            {
                WriteConsolidatedLog(LogLevel.Error, "CLEANUP", $"Errore cleanup legacy: {ex.Message}");
            }
        }

        #endregion

        #region Utilities e Metodi Factory (Mantenuti)

        /// <summary>
        /// Ottieni informazioni sui file di log correnti
        /// </summary>
        public static string GetLogSummary()
        {
            try
            {
                var summary = new StringBuilder();
                summary.AppendLine("📊 LOG SUMMARY:");
                summary.AppendLine($"   Directory: {LOGS_DIR}");
                summary.AppendLine($"   Current Level: {MinimumLevel}");
                summary.AppendLine($"   Debug Session: {IsDebugSession}");
                summary.AppendLine($"   Files:");

                if (File.Exists(APP_LOG))
                    summary.AppendLine($"      • application.log ({new FileInfo(APP_LOG).Length / 1024} KB)");

                if (File.Exists(DEBUG_LOG))
                    summary.AppendLine($"      • debug.log ({new FileInfo(DEBUG_LOG).Length / 1024} KB)");

                return summary.ToString();
            }
            catch
            {
                return "❌ Error reading log summary";
            }
        }

        private static string GetLevelEmoji(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "🔍",
                LogLevel.Info => "ℹ️",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🚨",
                _ => "📝"
            };
        }

        /// <summary>
        /// Abilita specifiche aree di logging
        /// </summary>
        public static void EnableAreas(LogArea areas)
        {
            EnabledAreas |= areas;
        }

        /// <summary>
        /// Disabilita specifiche aree di logging
        /// </summary>
        public static void DisableAreas(LogArea areas)
        {
            EnabledAreas &= ~areas;
        }

        /// <summary>
        /// Configurazione di debug completa
        /// </summary>
        public static void EnableFullDebug(string reason = "Debug completo")
        {
            EnabledAreas = (LogArea)(-1); // Tutte le aree
            MinimumLevel = LogLevel.Debug;
            IsDebugSession = true;
            WriteConsolidatedLog(LogLevel.Debug, "CONFIG", $"🔧 DEBUG COMPLETO: {reason}");
        }

        /// <summary>
        /// Reset alla configurazione di default
        /// </summary>
        public static void ResetToDefault()
        {
            EnabledAreas = LogArea.Errors | LogArea.Authentication | LogArea.System;
            MinimumLevel = LogLevel.Info;
            IsDebugSession = false;
            WriteConsolidatedLog(LogLevel.Info, "CONFIG", "🔄 Configurazione reset a default");
        }

        public static string GetCurrentConfiguration()
        {
            var areas = new List<string>();
            foreach (LogArea area in Enum.GetValues<LogArea>())
            {
                if (EnabledAreas.HasFlag(area))
                    areas.Add(area.ToString());
            }

            return $"Enabled: {(IsLoggingEnabled ? "ON" : "OFF")} | " +
                   $"Level: {MinimumLevel} | " +
                   $"Areas: [{string.Join(", ", areas)}] | " +
                   $"Debug Output: {WriteToDebugOutput}";
        }

        #endregion

        #region Metodi Factory (Mantenuti per compatibilità)

        public static LoggingService CreateForComponent(string componentName, LogArea area = LogArea.System)
        {
            return new LoggingService(componentName, area);
        }

        public static LoggingService CreateForAuthentication(string componentName = "Auth")
        {
            return new LoggingService(componentName, LogArea.Authentication);
        }

        public static LoggingService CreateForJiraApi(string componentName = "JiraAPI")
        {
            return new LoggingService(componentName, LogArea.JiraApi);
        }

        public static LoggingService CreateForUI(string componentName = "UI")
        {
            return new LoggingService(componentName, LogArea.UI);
        }

        public static LoggingService CreateForWebView(string componentName = "WebView")
        {
            return new LoggingService(componentName, LogArea.WebView);
        }

        /// <summary>
        /// Factory legacy per compatibilità con codice esistente
        /// </summary>
        public static LoggingService CreateLegacy(string componentName, string customLogPath)
        {
            return new LoggingService(componentName, customLogPath);
        }

        #endregion

        #region Proprietà Pubbliche

        public string GetLogFilePath() => _logFilePath;
        public LogArea ComponentArea => _componentArea;
        public string ComponentName => _componentName;

        #endregion
    }
}