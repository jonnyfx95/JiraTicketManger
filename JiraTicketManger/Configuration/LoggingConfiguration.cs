using System;
using System.Collections.Generic;
using JiraTicketManager.Services;
using JiraTicketManager.Models;
using static JiraTicketManager.Services.LoggingService;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Configurazione avanzata del sistema di logging
    /// INTEGRATA con SettingsService esistente - NESSUN FILE AGGIUNTIVO
    /// </summary>
    public class LoggingConfiguration
    {
        #region Integration with existing SettingsService

        private static SettingsService _settingsService;

        /// <summary>
        /// Usa il SettingsService esistente per persistenza
        /// </summary>
        private static SettingsService Settings
        {
            get
            {
                if (_settingsService == null)
                {
                    _settingsService = SettingsService.CreateDefault();
                }
                return _settingsService;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Inizializza il sistema di logging con la configurazione salvata nel SettingsService
        /// </summary>
        public static void Initialize()
        {
            try
            {
                var loggingSettings = Settings.GetLoggingSettings();
                ApplySettingsToLoggingService(loggingSettings);

                // Log di inizializzazione
                var logger = LoggingService.CreateForComponent("LogConfig", LogArea.System);
                logger.LogInfo($"Sistema logging inizializzato da SettingsService - Preset: {loggingSettings.CurrentPreset}");
                logger.LogInfo($"Configurazione: {LoggingService.GetCurrentConfiguration()}");
            }
            catch (Exception ex)
            {
                // Fallback su configurazione di produzione
                ApplyPreset("Production");
                Console.WriteLine($"Errore inizializzazione logging config: {ex.Message}");
            }
        }

        /// <summary>
        /// Applica un preset specifico usando SettingsService
        /// </summary>
        public static bool ApplyPreset(string presetName)
        {
            try
            {
                Settings.ApplyLoggingPreset(presetName);
                var loggingSettings = Settings.GetLoggingSettings();
                ApplySettingsToLoggingService(loggingSettings);

                Console.WriteLine($"✅ Preset '{presetName}' applicato e salvato automaticamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore applicazione preset '{presetName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un preset personalizzato (salvato nel SettingsService)
        /// </summary>
        public static void CreateCustomPreset(string name, string description, LogArea areas, LogLevel level, bool debugOutput = true)
        {
            try
            {
                var customSettings = new LoggingSettings
                {
                    CurrentPreset = name,
                    LogLevel = level.ToString(),
                    EnableDebugOutput = debugOutput,
                    EnabledAreas = ConvertLogAreasToStringList(areas),
                    IsLoggingEnabled = true
                };

                Settings.UpdateLoggingSettings(customSettings);
                Console.WriteLine($"✅ Preset personalizzato '{name}' creato e salvato");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore creazione preset personalizzato: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottieni lista dei preset disponibili dal SettingsService
        /// </summary>
        public static List<string> GetAvailablePresets()
        {
            return Settings.GetAvailableLoggingPresets();
        }

        /// <summary>
        /// Ottieni descrizione di un preset dal SettingsService
        /// </summary>
        public static string GetPresetDescription(string presetName)
        {
            return Settings.GetLoggingPresetDescription(presetName);
        }

        /// <summary>
        /// Abilita temporaneamente una modalità debug specifica
        /// (per sessione corrente, non salva nel SettingsService)
        /// </summary>
        public static void EnableTemporaryDebug(LogArea area, string reason = "")
        {
            LoggingService.EnableAreas(area);
            LoggingService.MinimumLevel = LogLevel.Debug;
            LoggingService.WriteToDebugOutput = true;

            var logger = LoggingService.CreateForComponent("LogConfig", LogArea.System);
            logger.LogInfo($"🔧 Debug temporaneo abilitato per {area}" +
                          (string.IsNullOrEmpty(reason) ? "" : $" - Motivo: {reason}"));
        }

        /// <summary>
        /// Disabilita completamente il logging (solo per emergenze)
        /// </summary>
        public static void EmergencyDisable()
        {
            LoggingService.IsLoggingEnabled = false;
            Console.WriteLine("🚨 LOGGING DISABILITATO - MODALITÀ EMERGENZA");
        }

        /// <summary>
        /// Ricarica configurazione dal SettingsService
        /// </summary>
        public static void ReloadFromSettings()
        {
            try
            {
                Settings.LoadSettings();
                var loggingSettings = Settings.GetLoggingSettings();
                ApplySettingsToLoggingService(loggingSettings);

                var logger = LoggingService.CreateForComponent("LogConfig", LogArea.System);
                logger.LogInfo("🔄 Configurazione logging ricaricata da SettingsService");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore ricaricamento configurazione: {ex.Message}");
            }
        }

        #endregion

        #region Configuration Helpers

        /// <summary>
        /// Converte settings del SettingsService in configurazione LoggingService
        /// </summary>
        private static void ApplySettingsToLoggingService(LoggingSettings settings)
        {
            // Converti LogLevel
            LoggingService.MinimumLevel = settings.LogLevel.ToLower() switch
            {
                "debug" => LogLevel.Debug,
                "info" => LogLevel.Info,
                "warning" => LogLevel.Warning,
                "error" => LogLevel.Error,
                "critical" => LogLevel.Critical,
                _ => LogLevel.Info
            };

            // Converti aree abilitate
            LoggingService.EnabledAreas = LogArea.System; // Base sempre abilitata

            foreach (var area in settings.EnabledAreas)
            {
                if (Enum.TryParse<LogArea>(area, true, out var logArea))
                {
                    LoggingService.EnabledAreas |= logArea;
                }
            }

            // Altre impostazioni
            LoggingService.IsLoggingEnabled = settings.IsLoggingEnabled;
            LoggingService.WriteToDebugOutput = settings.EnableDebugOutput;
        }

        /// <summary>
        /// Converte LogArea flags in lista di stringhe per SettingsService
        /// </summary>
        private static List<string> ConvertLogAreasToStringList(LogArea areas)
        {
            var result = new List<string>();

            foreach (LogArea area in Enum.GetValues<LogArea>())
            {
                if (areas.HasFlag(area))
                {
                    result.Add(area.ToString());
                }
            }

            return result;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Genera un report della configurazione corrente
        /// </summary>
        public static string GenerateConfigurationReport()
        {
            try
            {
                var settings = Settings.GetLoggingSettings();
                var report = new System.Text.StringBuilder();

                report.AppendLine("=== CONFIGURAZIONE LOGGING ===");
                report.AppendLine($"Preset Corrente: {settings.CurrentPreset}");
                report.AppendLine($"Ultima Modifica: {settings.LastModified:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Stato: {LoggingService.GetCurrentConfiguration()}");
                report.AppendLine();

                report.AppendLine("Aree Abilitate:");
                foreach (var area in settings.EnabledAreas)
                {
                    report.AppendLine($"  ✅ {area}");
                }
                report.AppendLine();

                report.AppendLine("Preset Disponibili:");
                foreach (var preset in GetAvailablePresets())
                {
                    var marker = preset == settings.CurrentPreset ? "► " : "  ";
                    report.AppendLine($"{marker}{preset}: {GetPresetDescription(preset)}");
                }

                report.AppendLine();
                report.AppendLine($"File Settings: {Settings.GetType().Name}");
                report.AppendLine($"Integrazione: SettingsService esistente (nessun file aggiuntivo)");

                return report.ToString();
            }
            catch (Exception ex)
            {
                return $"Errore generazione report: {ex.Message}";
            }
        }

        /// <summary>
        /// Mostra tutte le configurazioni disponibili
        /// </summary>
        public static void ShowAvailableConfigurations()
        {
            Console.WriteLine("📋 CONFIGURAZIONI LOGGING DISPONIBILI:");
            Console.WriteLine();

            foreach (var preset in GetAvailablePresets())
            {
                Console.WriteLine($"🎛️  {preset}");
                Console.WriteLine($"   {GetPresetDescription(preset)}");
                Console.WriteLine();
            }

            Console.WriteLine("💡 USO:");
            Console.WriteLine("   LoggingConfiguration.ApplyPreset(\"AuthenticationDebug\");");
            Console.WriteLine("   LoggingConfiguration.EnableTemporaryDebug(LogArea.WebView, \"Test login\");");
        }

        #endregion
    }

    /// <summary>
    /// Helper class per configurazioni rapide
    /// </summary>
    public static class LoggingHelper
    {
        /// <summary>
        /// Modalità per debug problemi di autenticazione
        /// </summary>
        public static void EnableAuthDebug(string reason = "Debug autenticazione")
        {
            LoggingConfiguration.ApplyPreset("AuthenticationDebug");
            Console.WriteLine($"🔐 Debug autenticazione abilitato: {reason}");
        }

        /// <summary>
        /// Modalità per debug problemi API Jira
        /// </summary>
        public static void EnableApiDebug(string reason = "Debug API Jira")
        {
            LoggingConfiguration.ApplyPreset("ApiDebug");
            Console.WriteLine($"🔌 Debug API abilitato: {reason}");
        }

        /// <summary>
        /// Modalità per debug UI (sidebar, toolbar, etc.)
        /// </summary>
        public static void EnableUIDebug(string reason = "Debug interfaccia utente")
        {
            LoggingConfiguration.ApplyPreset("UIDebug");
            Console.WriteLine($"🎨 Debug UI abilitato: {reason}");
        }

        /// <summary>
        /// Modalità produzione (log puliti)
        /// </summary>
        public static void EnableProductionMode()
        {
            LoggingConfiguration.ApplyPreset("Production");
            Console.WriteLine("🏭 Modalità produzione abilitata");
        }

        /// <summary>
        /// Modalità sviluppo completo
        /// </summary>
        public static void EnableDevelopmentMode()
        {
            LoggingConfiguration.ApplyPreset("Development");
            Console.WriteLine("🔧 Modalità sviluppo completa abilitata");
        }

        /// <summary>
        /// Debug temporaneo per area specifica (non salva)
        /// </summary>
        public static void TemporaryDebug(LogArea area, string reason)
        {
            LoggingConfiguration.EnableTemporaryDebug(area, reason);
        }

        /// <summary>
        /// Mostra configurazione corrente
        /// </summary>
        public static void ShowCurrentConfig()
        {
            Console.WriteLine(LoggingConfiguration.GenerateConfigurationReport());
        }
    }
}