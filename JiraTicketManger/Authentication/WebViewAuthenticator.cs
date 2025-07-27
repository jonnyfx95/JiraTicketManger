using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using JiraTicketManager.Models;
using JiraTicketManager.UI.Templates;
using JiraTicketManager.Services;
using JiraTicketManager.Helpers;

namespace JiraTicketManager.Authentication
{
    public class WebViewAuthenticator
    {
        private readonly LoggingService _logger;
        private string _tempUserDataPath;
        private string _savedUsername;
        private readonly WindowsToastService _toastService;
        private readonly SettingsService _settingsService;

        public WebViewAuthenticator(string logFilePath = null)
        {
            _logger = string.IsNullOrEmpty(logFilePath)
                ? LoggingService.CreateForComponent("WebViewAuth")
                : new LoggingService("WebViewAuth", logFilePath);

            _toastService = WindowsToastService.CreateDefault();
            _settingsService = SettingsService.CreateDefault();

            // FORZA RICARICAMENTO FRESCO DAL FILE
            _logger.LogInfo("Forzando ricaricamento settings dal file...");
            _settingsService.LoadSettings();

            LoadSavedUsername();
        }

        public async Task<AuthenticationResult> AuthenticateAsync(Form parentForm)
        {
            _logger.LogSession("INIZIO AUTENTICAZIONE MICROSOFT SSO SICURA");

            try
            {
                // Crea cartella temporanea unica per questa sessione
                CreateTemporaryUserDataFolder();

                var loginForm = CreateLoginForm(parentForm);
                var webView2 = CreateWebView2();

                loginForm.Controls.Add(webView2);
                _logger.LogInfo("Form WebView2 creato con cache temporanea");

                AuthenticationResult result = null;

                loginForm.Load += async (s, e) =>
                {
                    try
                    {
                        _logger.LogInfo("Inizializzazione WebView2 con ambiente sicuro");

                        // Crea ambiente WebView2 con cartella temporanea
                        var environment = await CoreWebView2Environment.CreateAsync(null, _tempUserDataPath);
                        await webView2.EnsureCoreWebView2Async(environment);

                        // Mostra pagina loading
                        string loadingHtml = LoadingPageGenerator.GenerateLoadingPage(
                            "Connessione Sicura in Corso",
                            "Inizializzazione ambiente protetto...");
                        webView2.NavigateToString(loadingHtml);
                        _logger.LogInfo("Pagina loading sicura mostrata");

                        // Avvia autenticazione con navigazione reale
                        result = await PerformSecureAuthentication(webView2);

                        // Mostra risultato
                        if (result.IsSuccess)
                        {
                            // Salva username per prossima volta (solo username!)
                            SaveUsername(result.UserEmail);

                            string successHtml = LoadingPageGenerator.GenerateSuccessPage(result.UserEmail);
                            webView2.NavigateToString(successHtml);
                            await Task.Delay(1500);
                        }
                        else
                        {
                            string errorHtml = LoadingPageGenerator.GenerateErrorPage(result.ErrorMessage);
                            webView2.NavigateToString(errorHtml);
                            await Task.Delay(2000);
                        }

                        // Pulizia sicurezza SEMPRE
                        await CleanupSecurityData(webView2);

                        loginForm.DialogResult = result.IsSuccess ? DialogResult.OK : DialogResult.Cancel;
                        loginForm.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Load Event", ex);
                        await CleanupSecurityData(webView2);
                        result = AuthenticationResult.Failure($"Errore: {ex.Message}", AuthenticationMethod.MicrosoftSSO);
                        loginForm.Close();
                    }
                };

                // Gestione chiusura form per pulizia
                loginForm.FormClosed += async (s, e) =>
                {
                    await CleanupSecurityData(webView2);
                };

                _logger.LogInfo("Apertura form autenticazione sicura");
                loginForm.ShowDialog(parentForm);

                return result ?? AuthenticationResult.Failure("Operazione annullata", AuthenticationMethod.MicrosoftSSO);
            }
            catch (Exception ex)
            {
                _logger.LogError("AuthenticateAsync", ex);
                await CleanupSecurityData(null);
                return AuthenticationResult.Failure($"Errore: {ex.Message}", AuthenticationMethod.MicrosoftSSO);
            }
        }

        private void CreateTemporaryUserDataFolder()
        {
            try
            {
                // Crea cartella temporanea unica per questa sessione
                _tempUserDataPath = Path.Combine(Path.GetTempPath(), "JiraTM_" + Guid.NewGuid().ToString("N")[..8]);
                Directory.CreateDirectory(_tempUserDataPath);
                _logger.LogInfo($"Cartella temporanea creata: {_tempUserDataPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError("CreateTemporaryUserDataFolder", ex);
                _tempUserDataPath = Path.GetTempPath();
            }
        }

        private async Task<AuthenticationResult> PerformSecureAuthentication(WebView2 webView2)
        {
            _logger.LogInfo("=== AUTENTICAZIONE SICURA OTTIMIZZATA ===");

            try
            {
                bool loginCompleted = false;
                bool emailExtracted = false;
                string extractedEmail = null;
                string homePageUrl = null;

                // Handler ottimizzato
                webView2.CoreWebView2.NavigationCompleted += async (s, e) =>
                {
                    if (!e.IsSuccess || loginCompleted) return;

                    string currentUrl = webView2.CoreWebView2.Source;
                    _logger.LogInfo($"URL: {currentUrl}");

                    try
                    {
                        // Se login Microsoft, mostra WebView
                        if (currentUrl.Contains("login.microsoftonline.com") && !webView2.Visible)
                        {
                            _logger.LogInfo("🔐 Mostra pagina login");
                            webView2.Visible = true;

                            // Pre-compila username
                            if (!string.IsNullOrEmpty(_savedUsername))
                            {
                                await Task.Delay(2000);
                                await PreFillUsername(webView2, _savedUsername);
                            }
                        }

                        // Se raggiungiamo MyTime home
                        if (currentUrl.Contains("mytime.dedagroup.it/home") && !loginCompleted)
                        {
                            _logger.LogInfo("🎉 Login completato - ESTRAZIONE IMMEDIATA");
                            loginCompleted = true; // BLOCCA subito altri eventi
                            homePageUrl = currentUrl;

                            // PRIMA estrai email (WebView ancora visibile)
                            _logger.LogInfo("Estrazione email con WebView visibile");
                            await Task.Delay(3000); // Attesa caricamento completo

                            _logger.LogInfo("=== PRIMA DI CHIAMARE ExtractUserEmail ===");
                            try
                            {
                                extractedEmail = await ExtractUserEmail(webView2);
                                _logger.LogInfo($"=== DOPO ExtractUserEmail - Risultato: '{extractedEmail}' ===");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("ERRORE ExtractUserEmail", ex);
                                extractedEmail = null;
                            }

                            if (string.IsNullOrEmpty(extractedEmail))
                            {
                                _logger.LogInfo("Primo tentativo fallito - Secondo tentativo");
                                await Task.Delay(2000);
                                extractedEmail = await ExtractUserEmail(webView2);
                            }

                            _logger.LogInfo($"Email estratta: '{extractedEmail}'");

                            // POI nascondi WebView
                            _logger.LogInfo("NASCONDO WebView dopo estrazione");
                            webView2.Visible = false;

                            // Mostra loading solo se email estratta
                            if (!string.IsNullOrEmpty(extractedEmail))
                            {
                                string successHtml = LoadingPageGenerator.GenerateLoadingPage(
                                    "Accesso Autorizzato ✓",
                                    $"Benvenuto {extractedEmail}");
                                webView2.NavigateToString(successHtml);
                                webView2.Visible = true;
                                _logger.LogInfo("Mostrato messaggio successo FINALE");
                            }
                            else
                            {
                                string errorHtml = LoadingPageGenerator.GenerateLoadingPage(
                                    "Errore Estrazione ❌",
                                    "Impossibile rilevare email utente");
                                webView2.NavigateToString(errorHtml);
                                webView2.Visible = true;
                                _logger.LogInfo("Mostrato messaggio errore");
                            }

                            emailExtracted = true; // Segnala completamento
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("NavigationCompleted Handler", ex);
                        emailExtracted = true; // Sblocca in caso di errore
                    }
                };

                // Inizia nascosto
                webView2.Visible = false;

                // Naviga a MyTime
                _logger.LogInfo("Navigazione a MyTime...");
                webView2.CoreWebView2.Navigate("https://mytime.dedagroup.it/home");

                // Attendi completamento
                int waitTime = 0;
                const int maxWaitTime = 300000; // 5 minuti

                while (!emailExtracted && waitTime < maxWaitTime)
                {
                    await Task.Delay(1000);
                    waitTime += 1000;

                    if (waitTime % 15000 == 0)
                    {
                        _logger.LogInfo($"Attesa: {waitTime / 1000}s / {maxWaitTime / 1000}s");
                    }
                }

                // Elabora risultato
                if (!string.IsNullOrEmpty(extractedEmail))
                {
                    if (extractedEmail.EndsWith("@dedagroup.it", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInfo($"✅ SUCCESSO FINALE: {extractedEmail}");
                        _toastService.ShowAuthenticationSuccess(extractedEmail);
                        return AuthenticationResult.Success(extractedEmail, AuthenticationMethod.MicrosoftSSO);
                    }
                    else
                    {
                        _logger.LogWarning($"❌ Dominio non valido: {extractedEmail}");
                        _toastService.ShowAuthenticationError($"Email non autorizzata: {extractedEmail}");
                        return AuthenticationResult.Failure($"Email non autorizzata: {extractedEmail}", AuthenticationMethod.MicrosoftSSO);
                    }
                }
                else
                {
                    _logger.LogWarning("❌ Email non estratta");
                    return AuthenticationResult.Failure("Impossibile estrarre email utente", AuthenticationMethod.MicrosoftSSO);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("PerformSecureAuthentication", ex);
                return AuthenticationResult.Failure($"Errore: {ex.Message}", AuthenticationMethod.MicrosoftSSO);
            }
        }

        private async Task PreFillUsername(WebView2 webView2, string username)
        {
            try
            {
                _logger.LogInfo($"Pre-compilazione username: {username}");

                // ✅ CONTROLLO SICUREZZA AGGIUNTO
                if (webView2?.CoreWebView2 == null)
                {
                    _logger.LogWarning("CoreWebView2 non disponibile per pre-compilazione username");
                    return;
                }

                string script = $@"
            setTimeout(function() {{
                var emailField = document.querySelector('input[type=""email""]') || 
                               document.querySelector('input[name*=""email""]') ||
                               document.querySelector('input[name*=""username""]') ||
                               document.querySelector('input[placeholder*=""email""]');
                
                if (emailField) {{
                    emailField.value = '{username}';
                    emailField.dispatchEvent(new Event('input', {{ bubbles: true }}));
                    console.log('Username pre-compilato: {username}');
                }}
            }}, 1000);
        ";

                await webView2.CoreWebView2.ExecuteScriptAsync(script);
                _logger.LogInfo("Script pre-compilazione username eseguito");
            }
            catch (Exception ex)
            {
                _logger.LogError("PreFillUsername", ex);
            }
        }


        private async Task<string> ExtractUserEmail(WebView2 webView2)
        {
            try
            {
                _logger.LogInfo("Estrazione nome utente e conversione automatica");

                // ✅ CONTROLLO SICUREZZA AGGIUNTO
                if (webView2?.CoreWebView2 == null)
                {
                    _logger.LogWarning("CoreWebView2 non disponibile per estrazione email");
                    return null;
                }

                string script = @"
            (function() {
                return document.body.innerText;
            })();
        ";

                string result = await webView2.CoreWebView2.ExecuteScriptAsync(script);
                string pageText = result?.Trim('"') ?? "";

                _logger.LogInfo($"Testo pagina estratto (primi 300 char): {pageText.Substring(0, Math.Min(300, pageText.Length))}");

                // Resto del codice...
                string convertedEmail = EmailConverterHelper.ExtractAndConvertFirstName(pageText);

                if (!string.IsNullOrEmpty(convertedEmail))
                {
                    _logger.LogInfo($"✅ Email convertita con successo: '{convertedEmail}'");
                    return convertedEmail;
                }
                else
                {
                    _logger.LogWarning("❌ Impossibile estrarre e convertire nome in email");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("ExtractUserEmail", ex);
                return null;
            }
        }

        private async Task CleanupSecurityData(WebView2 webView2)
        {
            try
            {
                _logger.LogInfo("=== PULIZIA DATI DI SICUREZZA ===");

                // ✅ CONTROLLO SICUREZZA AGGIUNTO
                if (webView2?.CoreWebView2 != null)
                {
                    try
                    {
                        _logger.LogInfo("Pulizia dati browser...");

                        await webView2.CoreWebView2.ExecuteScriptAsync(@"
                    if (typeof Storage !== 'undefined') {
                        localStorage.clear();
                        sessionStorage.clear();
                    }
                    
                    document.cookie.split(';').forEach(function(c) { 
                        document.cookie = c.replace(/^ +/, '').replace(/=.*/, '=;expires=' + new Date().toUTCString() + ';path=/'); 
                    });
                    
                    console.log('Dati browser puliti via JavaScript');
                ");

                        _logger.LogInfo("Dati browser puliti via JavaScript");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Pulizia JavaScript fallita: {ex.Message}");
                    }

                    try
                    {
                        webView2.CoreWebView2.Navigate("about:blank");
                        await Task.Delay(500);
                        _logger.LogInfo("WebView2 navigato a pagina vuota");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Navigazione a blank fallita: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogWarning("CoreWebView2 già null - Skipping cleanup JavaScript");
                }

                // Resto del cleanup delle cartelle temporanee...
            }
            catch (Exception ex)
            {
                _logger.LogError("CleanupSecurityData", ex);
            }
        }

        private void DeleteDirectoryRecursive(string path)
        {
            try
            {
                // Rimuovi attributi readonly dai file
                foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }

                Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore cancellazione ricorsiva: {ex.Message}");
                throw;
            }
        }




        private void LoadSavedUsername()
        {
            try
            {
                _logger.LogInfo("=== LOAD SAVED USERNAME DEBUG ===");

                // Forza ricaricamento fresco dal file
                _settingsService.LoadSettings();
                _logger.LogInfo("Settings ricaricati dal file");

                _savedUsername = _settingsService.GetLastUsername();

                _logger.LogInfo($"Username letto dal SettingsService: '{_savedUsername}'");
                _logger.LogInfo($"Username salvato in _savedUsername: '{_savedUsername}'");

                if (!string.IsNullOrEmpty(_savedUsername))
                {
                    _logger.LogInfo($"✅ Username salvato caricato correttamente: {_savedUsername}");
                }
                else
                {
                    _logger.LogWarning("❌ Nessun username trovato nei settings");
                }

                // Debug del file settings diretto
                var settings = _settingsService.GetCurrentSettings();
                _logger.LogInfo($"Settings.UserSettings.LastUsername: '{settings?.UserSettings?.LastUsername}'");

            }
            catch (Exception ex)
            {
                _logger.LogError("LoadSavedUsername", ex);
            }
        }

        private void SaveUsername(string email)
        {
            try
            {
                _logger.LogInfo($"DEBUG SaveUsername chiamato con email: '{email}'");
                _settingsService.SaveLastUsername(email);
                _settingsService.SetAuthenticationMethod("MicrosoftSSO");
                _savedUsername = email;
                _logger.LogInfo($"Username salvato nel sistema settings: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveUsername", ex);
            }
        }

        private Form CreateLoginForm(Form parentForm)
        {
            return new Form
            {
                Text = "Dedagroup - Autenticazione Sicura",
                Size = new Size(900, 650),
                StartPosition = FormStartPosition.CenterScreen, 
                Icon = parentForm?.Icon,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };
        }

        private WebView2 CreateWebView2()
        {
            return new WebView2 { Dock = DockStyle.Fill };
        }
    }
}