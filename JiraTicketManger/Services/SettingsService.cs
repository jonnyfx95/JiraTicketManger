using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using JiraTicketManager.Models;

namespace JiraTicketManager.Services
{
    public class SettingsService
    {
        private readonly LoggingService _logger;
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;
        private static SettingsService _instance;
        private static readonly object _lock = new object();

        #region Constructors & Initialization

        public SettingsService()
        {
            var instanceId = Guid.NewGuid().ToString().Substring(0, 8);
            _logger = LoggingService.CreateForComponent($"SettingsService-{instanceId}");
            _logger.LogInfo($"SettingsService Constructor - NUOVA ISTANZA: {instanceId}");

            _settingsFilePath = Path.Combine(Application.StartupPath, "config", "settings.json");
            // AGGIUNGI QUI ↓
            _logger.LogInfo($"Settings file path: {_settingsFilePath}");

            EnsureConfigDirectory();
            // AGGIUNGI QUI ↓
            _logger.LogInfo("EnsureConfigDirectory completato");

            LoadSettings();
            // AGGIUNGI QUI ↓
            _logger.LogInfo("LoadSettings completato nel constructor");
        }

        private void EnsureConfigDirectory()
        {
            try
            {
                string configDir = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                    _logger.LogInfo($"Cartella config creata: {configDir}");
                }
                else
                {
                    _logger.LogInfo($"Cartella config già esistente: {configDir}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("EnsureConfigDirectory", ex);
            }
        }


        public static SettingsService CreateDefault()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new SettingsService();
                    }
                }
            }
            return _instance;
        }

        private string GetDefaultSettingsFilePath()
        {
            var configDir = Path.Combine(Application.StartupPath, "config");
            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);
            return Path.Combine(configDir, "settings.json");
        }

        #endregion

        #region Settings Management

        public AppSettings GetCurrentSettings()
        {
            return _currentSettings ?? new AppSettings();
        }

        public string GetLastUsername()
        {
            return _currentSettings?.UserSettings?.LastUsername ?? "";
        }

        public void LoadSettings()
        {
            try
            {
                // AGGIUNGI QUI ALL'INIZIO ↓
                _logger.LogInfo("=== INIZIO LOADSETTINGS ===");
                _logger.LogInfo($"LoadSettings() - Ricaricamento da: {_settingsFilePath}");

                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _logger.LogInfo($"JSON letto dal file (primi 200 char): {json.Substring(0, Math.Min(200, json.Length))}");

                    _currentSettings = JsonConvert.DeserializeObject<AppSettings>(json);
                    _logger.LogInfo("Settings deserializzati dal file esistente");

                    // AGGIUNGI QUI ↓
                    _logger.LogInfo($"Dopo LoadSettings - Username_Encrypted length: {_currentSettings?.JiraSettings?.Username_Encrypted?.Length ?? 0}");
                    _logger.LogInfo($"Dopo LoadSettings - Token_Encrypted length: {_currentSettings?.JiraSettings?.Token_Encrypted?.Length ?? 0}");
                    _logger.LogInfo($"Dopo LoadSettings - AuthenticationMethod: '{_currentSettings?.UserSettings?.AuthenticationMethod}'");

                    // Debug del LastUsername appena caricato
                    var lastUsername = _currentSettings?.UserSettings?.LastUsername;
                    _logger.LogInfo($"LastUsername appena caricato dal file: '{lastUsername}'");
                }
            
                else
                {
                    _logger.LogInfo("File settings non trovato - Creazione defaults");
                    _currentSettings = CreateDefaultSettings();
                    SaveSettings();
                }

                // Valida impostazioni caricate
                ValidateSettings();

                _logger.LogInfo($"LoadSettings completato - LastUsername finale: '{_currentSettings?.UserSettings?.LastUsername}'");
            }
            catch (Exception ex)
            {
                _logger.LogError("LoadSettings", ex);
                _logger.LogWarning("Creazione settings di default per errore caricamento");
                _currentSettings = CreateDefaultSettings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                _logger.LogInfo($"SaveSettings - Inizio salvataggio su: {_settingsFilePath}");
                _logger.LogInfo($"SaveSettings - Username_Encrypted da salvare: {_currentSettings.JiraSettings.Username_Encrypted?.Length ?? 0}");
                _logger.LogInfo($"SaveSettings - Token_Encrypted da salvare: {_currentSettings.JiraSettings.Token_Encrypted?.Length ?? 0}");

                string json = JsonConvert.SerializeObject(_currentSettings, Newtonsoft.Json.Formatting.Indented);

                _logger.LogInfo($"SaveSettings - JSON da scrivere (primi 300 char): {json.Substring(0, Math.Min(300, json.Length))}");

                File.WriteAllText(_settingsFilePath, json);

                _logger.LogInfo("Settings salvati con successo");

                // VERIFICA IMMEDIATA
                if (File.Exists(_settingsFilePath))
                {
                    string savedContent = File.ReadAllText(_settingsFilePath);
                    _logger.LogInfo($"SaveSettings - Verifica file salvato (primi 200 char): {savedContent.Substring(0, Math.Min(200, savedContent.Length))}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveSettings", ex);
                throw new InvalidOperationException($"Impossibile salvare le impostazioni: {ex.Message}", ex);
            }
        }

        private AppSettings CreateDefaultSettings()
        {
            _logger.LogInfo("Creazione settings di default");

            return new AppSettings
            {
                UserSettings = new UserSettings
                {
                    LastUsername = "",
                    RememberUsername = true,
                    LastLoginDate = null,
                    AuthenticationMethod = "MicrosoftSSO"
                },
                JiraSettings = new JiraSettings
                {
                    Domain = "https://deda-next.atlassian.net",
                    Username_Encrypted = "",
                    Token_Encrypted = "",
                    Project = "CC",
                    EnableDebugLogging = true,
                    ConnectionTimeout = 10000
                },
                ApplicationSettings = new ApplicationSettings
                {
                    ToastNotifications = true,
                    LogLevel = "Info",
                    AutoSaveCredentials = true,
                    CacheCleanupEnabled = true,
                    LoggingSettings = new LoggingSettings() // ✅ Inizializza logging settings
                },
                EncryptionSettings = new EncryptionSettings
                {
                    UseDPAPI = true,
                    EncryptionVersion = "1.0"
                }
            };
        }

        private void ValidateSettings()
        {
            if (_currentSettings == null)
            {
                _currentSettings = CreateDefaultSettings();
                return;
            }

            // Valida e correggi eventuali null
            if (_currentSettings.UserSettings == null)
                _currentSettings.UserSettings = new UserSettings();

            if (_currentSettings.JiraSettings == null)
                _currentSettings.JiraSettings = new JiraSettings();

            if (_currentSettings.ApplicationSettings == null)
                _currentSettings.ApplicationSettings = new ApplicationSettings();

            if (_currentSettings.EncryptionSettings == null)
                _currentSettings.EncryptionSettings = new EncryptionSettings();

            // ✅ Valida logging settings
            if (_currentSettings.ApplicationSettings.LoggingSettings == null)
                _currentSettings.ApplicationSettings.LoggingSettings = new LoggingSettings();

            _logger.LogInfo("Settings validati");
        }

        #endregion

        #region User Settings Methods

        public void SaveLastUsername(string username)
        {
            try
            {
                _logger.LogInfo($"DEBUG: Prima del salvataggio - username input: '{username}'");
                _currentSettings.UserSettings.LastUsername = username;
                _currentSettings.UserSettings.LastLoginDate = DateTime.Now;
                _logger.LogInfo($"DEBUG: Dopo assegnazione - LastUsername ora è: '{_currentSettings.UserSettings.LastUsername}'");
                SaveSettings();
                _logger.LogInfo($"Username salvato: {username}");
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveLastUsername", ex);
            }
        }

        public void SetAuthenticationMethod(string method)
        {
            try
            {
                _currentSettings.UserSettings.AuthenticationMethod = method;
                SaveSettings();
                _logger.LogInfo($"Metodo autenticazione impostato: {method}");
            }
            catch (Exception ex)
            {
                _logger.LogError("SetAuthenticationMethod", ex);
            }
        }

        #endregion

        #region Jira Settings Methods

        public void SaveJiraCredentials(string domain, string username, string token)
        {
            try
            {
                _logger.LogInfo("=== INIZIO SAVE JIRA CREDENTIALS ===");
                _logger.LogInfo($"Domain: {domain}");
                _logger.LogInfo($"Username input: {username}");
                _logger.LogInfo($"Token input length: {token?.Length ?? 0}");

                _currentSettings.JiraSettings.Domain = domain?.TrimEnd('/') ?? "";

                // USA CRYPTOGRAPHYSERVICE PER CRIPTARE
                string encryptedUsername = "";
                string encryptedToken = "";

                if (!string.IsNullOrWhiteSpace(username))
                {
                    try
                    {
                        var cryptoService = CryptographyService.CreateDefault();
                        encryptedUsername = cryptoService.EncryptString(username);
                        _logger.LogInfo($"Username criptato length: {encryptedUsername.Length}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("SaveJiraCredentials - Errore crittografia username", ex);
                        throw;
                    }
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    try
                    {
                        var cryptoService = CryptographyService.CreateDefault();
                        encryptedToken = cryptoService.EncryptString(token);
                        _logger.LogInfo($"Token criptato length: {encryptedToken.Length}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("SaveJiraCredentials - Errore crittografia token", ex);
                        throw;
                    }
                }

                _currentSettings.JiraSettings.Username_Encrypted = encryptedUsername;
                _currentSettings.JiraSettings.Token_Encrypted = encryptedToken;

                _logger.LogInfo($"Prima di SaveSettings - Username_Encrypted length: {_currentSettings.JiraSettings.Username_Encrypted.Length}");
                _logger.LogInfo($"Prima di SaveSettings - Token_Encrypted length: {_currentSettings.JiraSettings.Token_Encrypted.Length}");

                SaveSettings();
                _logger.LogInfo("Credenziali Jira salvate");
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveJiraCredentials", ex);
                throw;
            }
        }

        public (string domain, string username, string token) GetJiraCredentials()
        {
            try
            {
                var jira = _currentSettings.JiraSettings;

                string domain = jira.Domain ?? "https://deda-next.atlassian.net";
                string encryptedUsername = jira.Username_Encrypted ?? "";
                string encryptedToken = jira.Token_Encrypted ?? "";

                _logger.LogInfo($"GetJiraCredentials - Domain: {domain}");
                _logger.LogInfo($"GetJiraCredentials - Username_Encrypted length: {encryptedUsername.Length}");
                _logger.LogInfo($"GetJiraCredentials - Token_Encrypted length: {encryptedToken.Length}");

                // USA CRYPTOGRAPHYSERVICE PER DECRITTARE
                string decryptedUsername = "";
                string decryptedToken = "";

                if (!string.IsNullOrWhiteSpace(encryptedUsername))
                {
                    try
                    {
                        var cryptoService = CryptographyService.CreateDefault();
                        decryptedUsername = cryptoService.DecryptString(encryptedUsername);
                        _logger.LogInfo($"GetJiraCredentials - Username decriptato: {decryptedUsername}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetJiraCredentials - Errore decrittografia username", ex);
                        decryptedUsername = "";
                    }
                }

                if (!string.IsNullOrWhiteSpace(encryptedToken))
                {
                    try
                    {
                        var cryptoService = CryptographyService.CreateDefault();
                        decryptedToken = cryptoService.DecryptString(encryptedToken);
                        _logger.LogInfo($"GetJiraCredentials - Token decriptato length: {decryptedToken.Length}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetJiraCredentials - Errore decrittografia token", ex);
                        decryptedToken = "";
                    }
                }

                return (domain, decryptedUsername, decryptedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetJiraCredentials", ex);
                return ("https://deda-next.atlassian.net", "", "");
            }
        }



        #endregion

        #region ✅ NUOVI METODI - Logging Configuration

        /// <summary>
        /// Ottieni configurazione logging corrente
        /// </summary>
        public LoggingSettings GetLoggingSettings()
        {
            LoadSettings(); // Assicura che i settings siano freschi
            return _currentSettings?.ApplicationSettings?.LoggingSettings ?? new LoggingSettings();
        }

        /// <summary>
        /// Aggiorna configurazione logging
        /// </summary>
        public void UpdateLoggingSettings(LoggingSettings loggingSettings)
        {
            try
            {
                _logger.LogInfo("Aggiornamento configurazione logging");

                if (_currentSettings.ApplicationSettings == null)
                    _currentSettings.ApplicationSettings = new ApplicationSettings();

                loggingSettings.LastModified = DateTime.Now;
                _currentSettings.ApplicationSettings.LoggingSettings = loggingSettings;

                // Aggiorna anche il LogLevel legacy per compatibilità
                _currentSettings.ApplicationSettings.LogLevel = loggingSettings.LogLevel;

                SaveSettings();
                _logger.LogInfo($"Configurazione logging aggiornata: Preset={loggingSettings.CurrentPreset}");
            }
            catch (Exception ex)
            {
                _logger.LogError("UpdateLoggingSettings", ex);
                throw;
            }
        }

        /// <summary>
        /// Applica un preset di logging
        /// </summary>
        public void ApplyLoggingPreset(string presetName)
        {
            try
            {
                var preset = CreateLoggingPreset(presetName);
                if (preset != null)
                {
                    UpdateLoggingSettings(preset);
                    _logger.LogInfo($"Preset logging '{presetName}' applicato");
                }
                else
                {
                    _logger.LogWarning($"Preset logging '{presetName}' non trovato");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ApplyLoggingPreset", ex);
            }
        }

        /// <summary>
        /// Crea preset di logging predefiniti
        /// </summary>
        private LoggingSettings CreateLoggingPreset(string presetName)
        {
            return presetName.ToLower() switch
            {
                "production" => new LoggingSettings
                {
                    CurrentPreset = "Production",
                    LogLevel = "Warning",
                    EnableDebugOutput = false,
                    EnabledAreas = new List<string> { "Errors", "System" },
                    IsLoggingEnabled = true
                },

                "development" => new LoggingSettings
                {
                    CurrentPreset = "Development",
                    LogLevel = "Debug",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Authentication", "JiraApi", "UI", "Configuration",
                                                     "Export", "Testing", "Errors", "WebView", "Database", "System" },
                    IsLoggingEnabled = true
                },

                "authenticationdebug" => new LoggingSettings
                {
                    CurrentPreset = "AuthenticationDebug",
                    LogLevel = "Debug",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Errors", "System", "Authentication", "WebView" },
                    IsLoggingEnabled = true
                },

                "apidebug" => new LoggingSettings
                {
                    CurrentPreset = "ApiDebug",
                    LogLevel = "Debug",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Errors", "System", "JiraApi", "Configuration" },
                    IsLoggingEnabled = true
                },

                "uidebug" => new LoggingSettings
                {
                    CurrentPreset = "UIDebug",
                    LogLevel = "Info",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Errors", "System", "UI" },
                    IsLoggingEnabled = true
                },

                "exportdebug" => new LoggingSettings
                {
                    CurrentPreset = "ExportDebug",
                    LogLevel = "Debug",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Errors", "System", "Export", "Database" },
                    IsLoggingEnabled = true
                },

                "testingmode" => new LoggingSettings
                {
                    CurrentPreset = "TestingMode",
                    LogLevel = "Info",
                    EnableDebugOutput = true,
                    EnabledAreas = new List<string> { "Errors", "System", "Testing", "JiraApi" },
                    IsLoggingEnabled = true
                },

                "silent" => new LoggingSettings
                {
                    CurrentPreset = "Silent",
                    LogLevel = "Critical",
                    EnableDebugOutput = false,
                    EnabledAreas = new List<string> { "Errors" },
                    IsLoggingEnabled = true
                },

                _ => null
            };
        }

        /// <summary>
        /// Ottieni preset disponibili
        /// </summary>
        public List<string> GetAvailableLoggingPresets()
        {
            return new List<string>
            {
                "Production",
                "Development",
                "AuthenticationDebug",
                "ApiDebug",
                "UIDebug",
                "ExportDebug",
                "TestingMode",
                "Silent"
            };
        }

        /// <summary>
        /// Ottieni descrizione preset
        /// </summary>
        public string GetLoggingPresetDescription(string presetName)
        {
            return presetName.ToLower() switch
            {
                "production" => "Solo errori critici e sistema - Per ambiente di produzione",
                "development" => "Tutto abilitato - Per sviluppo completo",
                "authenticationdebug" => "Focus su autenticazione e WebView - Per debug login",
                "apidebug" => "Focus su API Jira - Per debug connessioni",
                "uidebug" => "Focus su interfaccia utente - Per debug UI",
                "exportdebug" => "Focus su export e file - Per debug Excel/esportazioni",
                "testingmode" => "Modalità test completa - Per validazioni sistema",
                "silent" => "Solo errori critici - Modalità silenziosa",
                _ => "Preset non riconosciuto"
            };
        }

        #endregion

        #region Validation Methods - AGGIUNTI

        //<summary>
        /// Verifica se le credenziali nel settings.json sono valide
       
        /// - MicrosoftSSO: SEMPRE richiede login, ma se login OK → MainForm
        /// - JiraAPI: valida credenziali criptate, se OK → MainForm direttamente
        /// </summary>

        public bool IsConfigurationValid()
        {
            try
            {
                _logger.LogInfo("=== IsConfigurationValid ===");

                // Controlla il metodo di autenticazione
                string authMethod = GetAuthenticationMethod();
                _logger.LogInfo($"Metodo autenticazione: {authMethod}");

                switch (authMethod)
                {
                    case "MicrosoftSSO":
                        _logger.LogInfo("Modalità Microsoft SSO - Richiede login (ma poi avvia MainForm se OK)");
                        return false; // Mostra FrmCredentials per login, ma poi MainForm

                    case "JiraAPI":
                        _logger.LogInfo("Modalità Jira API - Validazione credenziali criptate");
                        bool apiValid = ValidateJiraAPICredentials();
                        if (apiValid)
                        {
                            _logger.LogInfo("Credenziali API valide - Avvio diretto MainForm");
                        }
                        else
                        {
                            _logger.LogInfo("Credenziali API non valide - Mostra FrmCredentials");
                        }
                        return apiValid;

                    default:
                        _logger.LogInfo($"Metodo autenticazione sconosciuto: '{authMethod}' - Default a richiesta login");
                        return false; // Default: mostra FrmCredentials
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("IsConfigurationValid", ex);
                return false; // In caso di errore, mostra FrmCredentials
            }
        }

        /// <summary>
        /// Ottiene il metodo di autenticazione dal settings.json
        /// </summary>
        public string GetAuthenticationMethod()
        {
            return _currentSettings?.UserSettings?.AuthenticationMethod ?? "MicrosoftSSO";
        }

        /// <summary>
        /// Valida le credenziali Jira API nel settings.json
        /// </summary>
        private bool ValidateJiraAPICredentials()
        {
            try
            {
                // Ottieni credenziali DECRITTATE per la validazione
                var (domain, username, token) = GetJiraCredentials();

                _logger.LogInfo($"Validazione Jira API:");
                _logger.LogInfo($"  Domain: '{domain}' (length: {domain?.Length ?? 0})");
                _logger.LogInfo($"  Username: '{username}' (length: {username?.Length ?? 0})");
                _logger.LogInfo($"  Token: {(string.IsNullOrEmpty(token) ? "VUOTO" : "***")} (length: {token?.Length ?? 0})");

                // Verifica che le credenziali decrittate non siano vuote
                if (string.IsNullOrWhiteSpace(domain) ||
                    string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogInfo("ERRORE: Domain, Username o Token vuoti dopo decrittografia");
                    return false;
                }

                // Verifica formato email
                if (!username.Contains("@"))
                {
                    _logger.LogInfo("ERRORE: Username non è un formato email");
                    return false;
                }

                // Verifica lunghezza minima token
                if (token.Length < 10)
                {
                    _logger.LogInfo($"ERRORE: Token troppo corto ({token.Length} caratteri)");
                    return false;
                }

                _logger.LogInfo("✅ Credenziali Jira API valide - Avvio diretto MainForm");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateJiraAPICredentials", ex);
                return false;
            }
        }

        /// <summary>
        /// Valida la configurazione Microsoft SSO nel settings.json
        /// </summary>
        private bool ValidateMicrosoftSSOCredentials()
        {
            try
            {
                _logger.LogInfo("Validazione Microsoft SSO - SEMPRE richiede login");
                return false; // SSO richiede sempre login all'avvio
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateMicrosoftSSOCredentials", ex);
                return false;
            }
        }

        /// <summary>
        /// Ottiene lo stato della configurazione per debug
        /// </summary>
        public string GetConfigurationStatus()
        {
            try
            {
                var authMethod = GetAuthenticationMethod();
                var (domain, username, token) = GetJiraCredentials();
                var lastUsername = GetLastUsername();

                bool hasUsername = !string.IsNullOrWhiteSpace(username);
                bool hasToken = !string.IsNullOrWhiteSpace(token);

                return $"Auth: {authMethod}, Domain: {domain}, LastUser: {lastUsername}, " +
                       $"HasUsername: {hasUsername}, HasToken: {hasToken}";
            }
            catch (Exception ex)
            {
                _logger.LogError("GetConfigurationStatus", ex);
                return "Errore nel recupero dello stato";
            }
        }

        #endregion

    }
}