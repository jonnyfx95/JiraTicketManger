using JiraTicketManager.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per gestire l'autenticazione Jira API
    /// </summary>
    public class JiraAuthenticator
    {
        private readonly LoggingService _logger;
        private readonly SettingsService _settingsService;
        private readonly WindowsToastService _toastService;

        public JiraAuthenticator()
        {
            _logger = LoggingService.CreateForComponent("JiraAuth");
            _settingsService = SettingsService.CreateDefault();
            _toastService = WindowsToastService.CreateDefault();
        }

        /// <summary>
        /// Autentica e salva le credenziali Jira
        /// </summary>
        public async Task<AuthenticationResult> AuthenticateAsync(string server, string username, string token)
        {
            _logger.LogInfo("=== INIZIO AUTENTICAZIONE JIRA API ===");

            try
            {
                // Valida i parametri di input
                var validation = ValidateCredentials(server, username, token);
                if (!validation.IsValid)
                {
                    _logger.LogWarning($"Validazione fallita: {validation.ErrorMessage}");
                    return AuthenticationResult.Failure(validation.ErrorMessage, AuthenticationMethod.JiraAPI);
                }

                _logger.LogInfo($"Validazione input OK - Server: {server}, Username: {username}");

                // Test connessione alle API Jira
                bool connectionTest = await TestJiraConnectionAsync(server, username, token);

                if (!connectionTest)
                {
                    string errorMsg = "Impossibile connettersi a Jira. Verifica Server, Email e Token API.";
                    _logger.LogWarning(errorMsg);
                    return AuthenticationResult.Failure(errorMsg, AuthenticationMethod.JiraAPI);
                }

                _logger.LogInfo("✅ Test connessione Jira riuscito");

                // Salva le credenziali
                SaveCredentials(server, username, token);

                _logger.LogInfo("✅ Credenziali Jira salvate con successo");

                // Mostra notifica di successo
                _toastService.ShowSuccess("Jira API", "Credenziali salvate correttamente!");

                return AuthenticationResult.Success(username, AuthenticationMethod.JiraAPI);
            }
            catch (Exception ex)
            {
                _logger.LogError("AuthenticateAsync", ex);
                return AuthenticationResult.Failure($"Errore durante l'autenticazione: {ex.Message}", AuthenticationMethod.JiraAPI);
            }
        }

        /// <summary>
        /// Valida i parametri di input delle credenziali Jira
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateCredentials(string server, string username, string token)
        {
            if (string.IsNullOrWhiteSpace(server))
                return (false, "Il campo Server è obbligatorio");

            if (string.IsNullOrWhiteSpace(username))
                return (false, "Il campo Email è obbligatorio");

            if (string.IsNullOrWhiteSpace(token))
                return (false, "Il campo Token API è obbligatorio");

            if (!username.Contains("@"))
                return (false, "L'Email deve essere in formato valido (es: nome@dominio.com)");

            if (token.Length < 10)
                return (false, "Il Token API deve essere di almeno 10 caratteri");

            if (!server.StartsWith("http"))
                return (false, "Il Server deve iniziare con http:// o https://");

            return (true, "");
        }

        /// <summary>
        /// Testa la connessione alle API Jira
        /// </summary>
        private async Task<bool> TestJiraConnectionAsync(string server, string username, string token)
        {
            try
            {
                _logger.LogInfo("Test connessione API Jira...");

                // Costruisce l'URL per il test (endpoint /rest/api/2/myself)
                string testUrl = $"{server.TrimEnd('/')}/rest/api/2/myself";
                _logger.LogInfo($"URL test: {testUrl}");

                // Crea l'header di autenticazione Basic
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{token}"));
                string authHeader = $"Basic {credentials}";

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(15);
                    httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                    _logger.LogInfo("Invio richiesta di test...");
                    var response = await httpClient.GetAsync(testUrl);

                    _logger.LogInfo($"Risposta ricevuta: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        _logger.LogInfo($"Test riuscito - Response: {responseBody.Substring(0, Math.Min(200, responseBody.Length))}...");
                        return true;
                    }
                    else
                    {
                        string errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Test fallito - Status: {response.StatusCode}, Body: {errorBody}");
                        return false;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("TestJiraConnectionAsync - HTTP Error", ex);
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError("TestJiraConnectionAsync - Timeout", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("TestJiraConnectionAsync - Generic Error", ex);
                return false;
            }
        }

        /// <summary>
        /// Salva le credenziali Jira nel sistema di configurazione
        /// </summary>
        private void SaveCredentials(string server, string username, string token)
        {
            try
            {
                _logger.LogInfo("Salvataggio credenziali...");

                // Imposta il metodo di autenticazione su Jira API
                _settingsService.SetAuthenticationMethod("JiraAPI");

                // Salva le credenziali Jira
                _settingsService.SaveJiraCredentials(server, username, token);

                // Salva anche l'ultimo username per comodità futura
                _settingsService.SaveLastUsername(username);

                _logger.LogInfo("Credenziali salvate nel settings.json");
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveCredentials", ex);
                throw;
            }
        }

        /// <summary>
        /// Crea un'istanza di default del JiraAuthenticator
        /// </summary>
        public static JiraAuthenticator CreateDefault()
        {
            return new JiraAuthenticator();
        }
    }
}