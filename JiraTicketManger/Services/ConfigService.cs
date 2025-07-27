using System;
using System.IO;
using System.Threading.Tasks;
using JiraTicketManager.Models;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio wrapper per la gestione della configurazione e validazione.
    /// Centralizza la logica di controllo credenziali e decide se mostrare FrmCredentials.
    /// Migrato da Config.vb con architettura moderna.
    /// </summary>
    public class ConfigService
    {
        private readonly LoggingService _logger;
        private readonly SettingsService _settingsService;
        private readonly CryptographyService _cryptographyService;

        public ConfigService()
        {
            _logger = LoggingService.CreateForComponent("ConfigService");
            _settingsService = SettingsService.CreateDefault();
            _cryptographyService = CryptographyService.CreateDefault();

            _logger.LogInfo("ConfigService inizializzato");
        }

        public ConfigService(SettingsService settingsService)
        {
            _logger = LoggingService.CreateForComponent("ConfigService");
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _cryptographyService = CryptographyService.CreateDefault();

            _logger.LogInfo("ConfigService inizializzato con SettingsService personalizzato");
        }

        #region Configuration Validation

        /// <summary>
        /// Verifica se la configurazione è valida e completa.
        /// Determina se l'applicazione può procedere direttamente alla MainForm.
        /// </summary>
        /// <returns>True se configurazione valida, False se serve FrmCredentials</returns>
        public bool IsConfigurationValid()
        {
            try
            {
                _logger.LogInfo("=== DEBUG IsConfigurationValid ===");

                var settings = _settingsService.GetCurrentSettings();
                if (settings == null)
                {
                    _logger.LogWarning("IsConfigurationValid - Settings null");
                    return false;
                }

                _logger.LogInfo($"DEBUG - Settings object OK");
                _logger.LogInfo($"DEBUG - UserSettings: {settings.UserSettings != null}");
                _logger.LogInfo($"DEBUG - JiraSettings: {settings.JiraSettings != null}");

                // Verifica configurazione base
                if (!ValidateBasicSettings(settings))
                {
                    _logger.LogWarning("DEBUG - ValidateBasicSettings FAILED");
                    return false;
                }
                _logger.LogInfo("DEBUG - ValidateBasicSettings OK");

                // Verifica in base al metodo di autenticazione
                string authMethod = settings.UserSettings.AuthenticationMethod;
                _logger.LogInfo($"DEBUG - AuthenticationMethod RAW: '{authMethod}'");
                _logger.LogInfo($"DEBUG - AuthenticationMethod IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(authMethod)}");

                // CORREZIONE: Se authenticationMethod è vuoto o null, NON è valido
                if (string.IsNullOrWhiteSpace(authMethod))
                {
                    _logger.LogInfo("DEBUG - authenticationMethod vuoto - Configurazione NON valida - RETURN FALSE");
                    return false;
                }

                _logger.LogInfo($"DEBUG - AuthenticationMethod valido: '{authMethod}'");

                switch (authMethod)
                {
                    case "MicrosoftSSO":
                        _logger.LogInfo("DEBUG - Entro in case MicrosoftSSO");
                        bool ssoResult = ValidateSSOConfiguration(settings);
                        _logger.LogInfo($"DEBUG - SSO validation result: {ssoResult}");
                        return ssoResult;

                    case "JiraAPI":
                        _logger.LogInfo("DEBUG - Entro in case JiraAPI");
                        bool apiResult = ValidateAPIConfiguration(settings);
                        _logger.LogInfo($"DEBUG - API validation result: {apiResult}");
                        return apiResult;

                    default:
                        _logger.LogWarning($"DEBUG - Entro in default con AuthenticationMethod: '{authMethod}' - RETURN FALSE");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("IsConfigurationValid - EXCEPTION", ex);
                return false;
            }
        }

        /// <summary>
        /// Valida le impostazioni di base comuni a tutte le modalità
        /// </summary>
        private bool ValidateBasicSettings(AppSettings settings)
        {
            try
            {
                // Verifica dominio Jira
                if (string.IsNullOrWhiteSpace(settings.JiraSettings.Domain))
                {
                    _logger.LogWarning("ValidateBasicSettings - Domain Jira mancante");
                    return false;
                }

                // Verifica formato URL
                if (!Uri.TryCreate(settings.JiraSettings.Domain, UriKind.Absolute, out Uri domainUri))
                {
                    _logger.LogWarning($"ValidateBasicSettings - Domain Jira non valido: {settings.JiraSettings.Domain}");
                    return false;
                }

                // Verifica che sia HTTPS per sicurezza
                if (domainUri.Scheme != "https")
                {
                    _logger.LogWarning($"ValidateBasicSettings - Domain deve essere HTTPS: {settings.JiraSettings.Domain}");
                    return false;
                }

                // Verifica progetto
                if (string.IsNullOrWhiteSpace(settings.JiraSettings.Project))
                {
                    _logger.LogWarning("ValidateBasicSettings - Progetto Jira mancante");
                    return false;
                }

                _logger.LogDebug("ValidateBasicSettings - Configurazione base valida");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateBasicSettings", ex);
                return false;
            }
        }

        /// <summary>
        /// Valida configurazione per modalità Microsoft SSO
        /// </summary>
        private bool ValidateSSOConfiguration(AppSettings settings)
        {
            try
            {
                _logger.LogDebug("ValidateSSOConfiguration - Modalità Microsoft SSO");

                // Verifica che il dominio sia quello di Dedagroup
                string domain = settings.JiraSettings.Domain.ToLowerInvariant();
                if (!domain.Contains("deda-next.atlassian.net"))
                {
                    _logger.LogWarning($"ValidateSSOConfiguration - Domain non compatibile con SSO: {domain}");
                    return false;
                }

                // CRITICO: Verifica che ci sia un LastUsername valido (SSO completato)
                string lastUsername = settings.UserSettings.LastUsername;
                if (string.IsNullOrWhiteSpace(lastUsername))
                {
                    _logger.LogInfo("ValidateSSOConfiguration - Nessun LastUsername trovato - SSO NON completato");
                    return false;
                }

                if (!IsValidDedagroupEmail(lastUsername))
                {
                    _logger.LogWarning($"ValidateSSOConfiguration - LastUsername non valido per SSO: {lastUsername}");
                    return false;
                }

                // Verifica che ci sia una data di ultimo login
                if (settings.UserSettings.LastLoginDate == null)
                {
                    _logger.LogInfo("ValidateSSOConfiguration - Nessuna data ultimo login - SSO NON completato");
                    return false;
                }

                _logger.LogInfo($"ValidateSSOConfiguration - Configurazione SSO valida per {lastUsername}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateSSOConfiguration", ex);
                return false;
            }
        }

        /// <summary>
        /// Valida configurazione per modalità Jira API
        /// </summary>
        private bool ValidateAPIConfiguration(AppSettings settings)
        {
            try
            {
                _logger.LogDebug("ValidateAPIConfiguration - Modalità Jira API");

                // Verifica presenza credenziali criptate
                string encryptedUsername = settings.JiraSettings.Username_Encrypted;
                string encryptedToken = settings.JiraSettings.Token_Encrypted;

                if (string.IsNullOrWhiteSpace(encryptedUsername))
                {
                    _logger.LogWarning("ValidateAPIConfiguration - Username criptato mancante");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(encryptedToken))
                {
                    _logger.LogWarning("ValidateAPIConfiguration - Token criptato mancante");
                    return false;
                }

                // Verifica che le credenziali siano decrittabili
                try
                {
                    string username = _cryptographyService.EnsureDecrypted(encryptedUsername);
                    string token = _cryptographyService.EnsureDecrypted(encryptedToken);

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        _logger.LogWarning("ValidateAPIConfiguration - Username decrittato vuoto");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        _logger.LogWarning("ValidateAPIConfiguration - Token decrittato vuoto");
                        return false;
                    }

                    // Verifica formato email username
                    if (!IsValidEmail(username))
                    {
                        _logger.LogWarning($"ValidateAPIConfiguration - Username non è email valida");
                        return false;
                    }

                    // Verifica lunghezza token (token Jira sono tipicamente 24+ caratteri)
                    if (token.Length < 20)
                    {
                        _logger.LogWarning("ValidateAPIConfiguration - Token troppo corto");
                        return false;
                    }

                    _logger.LogInfo("ValidateAPIConfiguration - Configurazione API valida");
                    return true;
                }
                catch (Exception decryptEx)
                {
                    _logger.LogError("ValidateAPIConfiguration - Errore decrittografia", decryptEx);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ValidateAPIConfiguration", ex);
                return false;
            }
        }

        #endregion

        #region Credential Management

        /// <summary>
        /// Salva credenziali Jira API con crittografia automatica
        /// </summary>
        public void SaveJiraCredentials(string domain, string username, string token)
        {
            try
            {
                _logger.LogInfo("SaveJiraCredentials - Salvataggio credenziali Jira");

                // Valida input
                if (string.IsNullOrWhiteSpace(domain))
                    throw new ArgumentException("Domain è richiesto", nameof(domain));

                if (string.IsNullOrWhiteSpace(username))
                    throw new ArgumentException("Username è richiesto", nameof(username));

                if (string.IsNullOrWhiteSpace(token))
                    throw new ArgumentException("Token è richiesto", nameof(token));

                // Cripta credenziali
                string encryptedUsername = _cryptographyService.EncryptString(username);
                string encryptedToken = _cryptographyService.EncryptString(token);

                // Salva tramite SettingsService
                _settingsService.SaveJiraCredentials(domain, encryptedUsername, encryptedToken);

                // Aggiorna metodo autenticazione
                _settingsService.SetAuthenticationMethod("JiraAPI");

                _logger.LogInfo("SaveJiraCredentials - Credenziali salvate e criptate");
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveJiraCredentials", ex);
                throw new InvalidOperationException($"Errore salvataggio credenziali: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Imposta modalità Microsoft SSO
        /// </summary>
        public void SetSSOMode(string domain = null)
        {
            try
            {
                _logger.LogInfo("SetSSOMode - Impostazione modalità Microsoft SSO");

                if (!string.IsNullOrEmpty(domain))
                {
                    // Salva solo il domain, credenziali sono hardcoded
                    _settingsService.SaveJiraCredentials(domain, "", "");
                }

                _settingsService.SetAuthenticationMethod("MicrosoftSSO");

                _logger.LogInfo("SetSSOMode - Modalità SSO configurata");
            }
            catch (Exception ex)
            {
                _logger.LogError("SetSSOMode", ex);
                throw;
            }
        }

        /// <summary>
        /// Ottiene credenziali decrittate per uso interno
        /// </summary>
        public (string domain, string username, string token) GetDecryptedCredentials()
        {
            try
            {
                var (domain, encryptedUsername, encryptedToken) = _settingsService.GetJiraCredentials();

                string username = _cryptographyService.EnsureDecrypted(encryptedUsername);
                string token = _cryptographyService.EnsureDecrypted(encryptedToken);

                return (domain, username, token);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetDecryptedCredentials", ex);
                return ("", "", "");
            }
        }

        #endregion

        #region Connection Testing

        /// <summary>
        /// Testa la connessione con le credenziali attuali
        /// </summary>
        public async Task<bool> TestCurrentConnectionAsync()
        {
            try
            {
                _logger.LogInfo("TestCurrentConnectionAsync - Test connessione configurazione attuale");

                using var jiraService = JiraApiService.CreateFromSettings(_settingsService);

                if (!jiraService.HasValidCredentials())
                {
                    _logger.LogWarning("TestCurrentConnectionAsync - Credenziali non valide");
                    return false;
                }

                bool result = await jiraService.TestConnectionAsync();
                _logger.LogInfo($"TestCurrentConnectionAsync - Risultato: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("TestCurrentConnectionAsync", ex);
                return false;
            }
        }

        /// <summary>
        /// Testa credenziali specifiche senza salvarle
        /// </summary>
        public async Task<bool> TestCredentialsAsync(string domain, string username, string token)
        {
            try
            {
                _logger.LogInfo("TestCredentialsAsync - Test credenziali temporanee");

                using var jiraService = JiraApiService.CreateForAPI(domain, username, token);

                bool result = await jiraService.TestConnectionAsync();
                _logger.LogInfo($"TestCredentialsAsync - Risultato: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("TestCredentialsAsync", ex);
                return false;
            }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Verifica se un'email è valida per Dedagroup (@dedagroup.com)
        /// </summary>
        private bool IsValidDedagroupEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return email.ToLowerInvariant().EndsWith("@dedagroup.com") && IsValidEmail(email);
        }

        /// <summary>
        /// Verifica formato email generico
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ottiene informazioni di stato della configurazione per debug
        /// </summary>
        public string GetConfigurationStatus()
        {
            try
            {
                var settings = _settingsService.GetCurrentSettings();
                string authMethod = settings.UserSettings.AuthenticationMethod;
                string domain = settings.JiraSettings.Domain;
                string lastUsername = settings.UserSettings.LastUsername;

                bool hasEncryptedUsername = !string.IsNullOrEmpty(settings.JiraSettings.Username_Encrypted);
                bool hasEncryptedToken = !string.IsNullOrEmpty(settings.JiraSettings.Token_Encrypted);

                return $"Auth: {authMethod}, Domain: {domain}, LastUser: {lastUsername}, " +
                       $"HasUsername: {hasEncryptedUsername}, HasToken: {hasEncryptedToken}";
            }
            catch (Exception ex)
            {
                return $"Errore lettura configurazione: {ex.Message}";
            }
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Crea istanza ConfigService con configurazione di default
        /// </summary>
        public static ConfigService CreateDefault()
        {
            return new ConfigService();
        }

        /// <summary>
        /// Crea istanza ConfigService con SettingsService personalizzato
        /// </summary>
        public static ConfigService CreateWithSettings(SettingsService settingsService)
        {
            return new ConfigService(settingsService);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Accesso al SettingsService sottostante
        /// </summary>
        public SettingsService Settings => _settingsService;

        /// <summary>
        /// Verifica se è in modalità Microsoft SSO
        /// </summary>
        public bool IsSSOMode => _settingsService.GetCurrentSettings().UserSettings.AuthenticationMethod == "MicrosoftSSO";

        /// <summary>
        /// Verifica se è in modalità Jira API
        /// </summary>
        public bool IsAPIMode => _settingsService.GetCurrentSettings().UserSettings.AuthenticationMethod == "JiraAPI";

        #endregion


        public bool IsConfigurationValiddeb()
        {
            try
            {
                _logger.LogInfo("=== DEBUG IsConfigurationValid ===");

                var settings = _settingsService.GetCurrentSettings();
                if (settings == null)
                {
                    _logger.LogWarning("IsConfigurationValid - Settings null");
                    return false;
                }

                _logger.LogInfo($"DEBUG - Settings object OK");
                _logger.LogInfo($"DEBUG - UserSettings: {settings.UserSettings != null}");
                _logger.LogInfo($"DEBUG - JiraSettings: {settings.JiraSettings != null}");

                // Verifica configurazione base
                if (!ValidateBasicSettings(settings))
                {
                    _logger.LogWarning("DEBUG - ValidateBasicSettings FAILED");
                    return false;
                }
                _logger.LogInfo("DEBUG - ValidateBasicSettings OK");

                // Verifica in base al metodo di autenticazione
                string authMethod = settings.UserSettings.AuthenticationMethod;
                _logger.LogInfo($"DEBUG - AuthenticationMethod RAW: '{authMethod}'");
                _logger.LogInfo($"DEBUG - AuthenticationMethod IsNullOrWhiteSpace: {string.IsNullOrWhiteSpace(authMethod)}");

                // CORREZIONE: Se authenticationMethod è vuoto o null, NON è valido
                if (string.IsNullOrWhiteSpace(authMethod))
                {
                    _logger.LogInfo("DEBUG - authenticationMethod vuoto - Configurazione NON valida - RETURN FALSE");
                    return false;
                }

                _logger.LogInfo($"DEBUG - AuthenticationMethod valido: '{authMethod}'");

                switch (authMethod)
                {
                    case "MicrosoftSSO":
                        _logger.LogInfo("DEBUG - Entro in case MicrosoftSSO");
                        bool ssoResult = ValidateSSOConfiguration(settings);
                        _logger.LogInfo($"DEBUG - SSO validation result: {ssoResult}");
                        return ssoResult;

                    case "JiraAPI":
                        _logger.LogInfo("DEBUG - Entro in case JiraAPI");
                        bool apiResult = ValidateAPIConfiguration(settings);
                        _logger.LogInfo($"DEBUG - API validation result: {apiResult}");
                        return apiResult;

                    default:
                        _logger.LogWarning($"DEBUG - Entro in default con AuthenticationMethod: '{authMethod}' - RETURN FALSE");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("IsConfigurationValid - EXCEPTION", ex);
                return false;
            }
        }


    }
}