using JiraTicketManager.Authentication;
using JiraTicketManager.Services;
using JiraTicketManager;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    public partial class FrmCredentials : Form
    {
        // ✅ NUOVO: Logger consolidato invece di _logFilePath
        private readonly LoggingService _logger;

        // Servizi integrati (mantenuti)
        private readonly ConfigService _configService;
        private readonly WindowsToastService _toastService;
        private WebViewAuthenticator _webViewAuthenticator;
        private bool _isTestingConnection = false;

        #region Constructor

        public FrmCredentials()
        {
            InitializeComponent();

            // ✅ NUOVO: Inizializza logger consolidato
            _logger = InitializeLogging();

            // Inizializza nuovi servizi
            _configService = ConfigService.CreateDefault();
            _toastService = new WindowsToastService();

            InitializeForm();

            // Aggiungi evento FormClosing per cleanup
            this.FormClosing += (s, e) => OnFormClosing();
        }

        #endregion

        #region Logging System (AGGIORNATO)

        /// <summary>
        /// ✅ NUOVO: Inizializza sistema logging consolidato usando metodi esistenti
        /// </summary>
        private static LoggingService InitializeLogging()
        {
            try
            {
                // ✅ NUOVO: Carica configurazione dal settings.json
                LoggingConfiguration.Initialize();

                var logger = LoggingService.CreateForComponent("Program", LoggingService.LogArea.System);
                logger.LogDebug($"Logging Configuration: {LoggingService.GetCurrentConfiguration()}");

                return logger;
            }
            catch (Exception ex)
            {
                // Fallback se inizializzazione logging fallisce
                System.Diagnostics.Debug.WriteLine($"❌ Errore inizializzazione logging: {ex.Message}");

                // Fallback manuale
                LoggingService.EnableAreas(LoggingService.LogArea.Errors |
                                         LoggingService.LogArea.Authentication |
                                         LoggingService.LogArea.System);
                LoggingService.MinimumLevel = LoggingService.LogLevel.Info;

                return LoggingService.CreateForComponent("Program-Fallback", LoggingService.LogArea.System);
            }
        }

        /// <summary>
        /// ✅ SOSTITUISCE: WriteDebugLog() - ora usa logger consolidato
        /// </summary>
        private void WriteDebugLog(string message)
        {
            try
            {
                // Usa il logger invece di scrivere su file separato
                _logger.LogDebug(message);

                // Mantieni anche Debug.WriteLine per compatibilità sviluppo
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CREDENTIALS: {message}");
            }
            catch
            {
                // Fallback silenzioso se logger non funziona
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CREDENTIALS: {message}");
            }
        }

        /// <summary>
        /// ✅ SOSTITUISCE: WriteErrorLog() - ora usa logger consolidato
        /// </summary>
        private void WriteErrorLog(string method, Exception ex)
        {
            try
            {
                // Usa il metodo esistente del logger
                _logger.LogError(method, ex);
            }
            catch
            {
                // Fallback silenzioso
                System.Diagnostics.Debug.WriteLine($"❌ ERRORE {method}: {ex.Message}");
            }
        }

        #endregion

        #region Form Initialization (MANTENUTA - stessa logica)

        private void InitializeForm()
        {
            WriteDebugLog("InitializeForm() - Inizializzazione form");

            try
            {
                CenterToScreen();
                SetupEventHandlers();
                LoadDefaultValues();
                SetDefaultMode();
                LoadSavedSettings(); // Nuovo: carica settings esistenti
                WriteDebugLog("InitializeForm completata");
            }
            catch (Exception ex)
            {
                WriteErrorLog("InitializeForm", ex);
                throw;
            }
        }

        private void SetupEventHandlers()
        {
            WriteDebugLog("SetupEventHandlers() - Configurazione event handlers");

            try
            {
                // Radio button events
                rbJiraApi.CheckedChanged += OnAuthModeChanged;
                rbMicrosoftSSO.CheckedChanged += OnAuthModeChanged;
                WriteDebugLog("Event handlers radio button configurati");

                // Button events
                btnTest.Click += OnTestConnection;
                btnSave.Click += OnSaveCredentials;
                btnCancel.Click += OnCancel;
                btnMicrosoftLogin.Click += OnMicrosoftLogin;
                WriteDebugLog("Event handlers button configurati");

                // Text events
                txtServer.TextChanged += OnFieldChanged;
                txtUsername.TextChanged += OnFieldChanged;
                txtToken.TextChanged += OnFieldChanged; // Aggiunto Token
                WriteDebugLog("Event handlers textbox configurati");

                WriteDebugLog("Tutti gli event handlers configurati correttamente");
            }
            catch (Exception ex)
            {
                WriteErrorLog("SetupEventHandlers", ex);
                throw;
            }
        }

        private void LoadDefaultValues()
        {
            WriteDebugLog("LoadDefaultValues() - Caricamento valori default");

            try
            {
                txtServer.Text = "https://deda-next.atlassian.net";
                OnFieldChanged(txtServer, EventArgs.Empty);
                WriteDebugLog("Valori default caricati - Server: https://deda-next.atlassian.net");
            }
            catch (Exception ex)
            {
                WriteErrorLog("LoadDefaultValues", ex);
            }
        }

        private void SetDefaultMode()
        {
            WriteDebugLog("SetDefaultMode() - Impostazione modalità default");

            try
            {
                rbMicrosoftSSO.Checked = true;
                WriteDebugLog("Modalità default: Microsoft SSO");
                SwitchToMicrosoftMode();
                WriteDebugLog("Switch a modalità Microsoft completato");
            }
            catch (Exception ex)
            {
                WriteErrorLog("SetDefaultMode", ex);
            }
        }

        private void LoadSavedSettings()
        {
            WriteDebugLog("LoadSavedSettings - Caricamento impostazioni salvate");

            try
            {
                // Implementazione futura per caricare settings salvati
                WriteDebugLog("LoadSavedSettings completato");
            }
            catch (Exception ex)
            {
                WriteErrorLog("LoadSavedSettings", ex);
            }
        }

        #endregion

        #region Event Handlers (MANTENUTI - stessa logica)

        private void OnAuthModeChanged(object sender, EventArgs e)
        {
            WriteDebugLog("OnAuthModeChanged() - Cambio modalità autenticazione");

            try
            {
                if (rbJiraApi.Checked)
                {
                    SwitchToJiraApiMode();
                }
                else if (rbMicrosoftSSO.Checked)
                {
                    SwitchToMicrosoftMode();
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnAuthModeChanged", ex);
            }
        }

        private void OnFieldChanged(object sender, EventArgs e)
        {
            WriteDebugLog("OnFieldChanged() - Sender: " + sender.GetType().Name);

            try
            {
                if (rbJiraApi.Checked)
                {
                    ValidateJiraFields();
                }
                // Microsoft mode non ha campi da validare
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnFieldChanged", ex);
            }
        }

        private async void OnTestConnection(object sender, EventArgs e)
        {
            WriteDebugLog("OnTestConnection() - Test connessione");

            try
            {
                if (rbJiraApi.Checked)
                {
                    await TestJiraApiConnectionAsync();
                }
                else
                {
                    WriteDebugLog("Test connessione non disponibile per modalità SSO");
                    _toastService.ShowInfo("Info", "Test connessione disponibile solo per modalità Jira API");
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnTestConnection", ex);
            }
        }

        private async void OnSaveCredentials(object sender, EventArgs e)
        {
            WriteDebugLog("OnSaveCredentials() - Avvio salvataggio credenziali");

            try
            {
                if (rbJiraApi.Checked)
                {
                    // ✅ CORREZIONE: Usa HandleJiraAuthentication che già imposta DialogResult.OK
                    HandleJiraAuthentication();
                }
                else
                {
                    // Modalità Microsoft SSO già gestita in OnMicrosoftLogin
                    WriteDebugLog("Modalità SSO - configurazione già salvata");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }

                
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnSaveCredentials", ex);
                _toastService.ShowError("Errore", "Errore durante il salvataggio della configurazione");
                UpdateStatus("Errore salvataggio", true);
            }
        }

        private async void OnMicrosoftLogin(object sender, EventArgs e)
        {
            WriteDebugLog("OnMicrosoftLogin() - Avvio autenticazione Microsoft SSO");

            try
            {
                SetMicrosoftLoginState(true);
                UpdateStatus("Avvio autenticazione Microsoft...");

                // WebViewAuthenticator senza parametro _logFilePath
                if (_webViewAuthenticator == null)
                {
                    _webViewAuthenticator = new WebViewAuthenticator();
                }

                // Mostra il processo di autenticazione
                var result = await _webViewAuthenticator.AuthenticateAsync(this);

                if (result.IsSuccess)
                {
                    WriteDebugLog($"Autenticazione SSO riuscita per: {result.UserEmail}");

                    // Salva configurazione SSO
                    _configService.SetSSOMode(txtServer.Text.Trim());
                    _configService.Settings.SaveLastUsername(result.UserEmail);

                    // Mostra successo
                    _toastService.ShowSuccess("Autenticazione riuscita", $"Benvenuto {result.UserEmail}");
                    UpdateStatus($"Autenticato come: {result.UserEmail} - Avvio applicazione...");

                    // ✅ CORREZIONE: Imposta DialogResult.OK invece di OpenMainFormAndClose
                    await Task.Delay(1500); // Breve pausa per mostrare il messaggio

                    WriteDebugLog("Impostazione DialogResult.OK per avvio MainForm");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    WriteDebugLog($"Autenticazione SSO fallita: {result.ErrorMessage}");
                    _toastService.ShowError("Autenticazione fallita", result.ErrorMessage ?? "Errore sconosciuto");
                    UpdateStatus("Autenticazione fallita", true);
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnMicrosoftLogin", ex);
                _toastService.ShowError("Errore", "Errore durante l'autenticazione Microsoft SSO");
                UpdateStatus("Errore autenticazione", true);
            }
            finally
            {
                SetMicrosoftLoginState(false);
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            WriteDebugLog("OnCancel() - Annullamento form");
            this.DialogResult = DialogResult.Cancel;
            Application.Exit(); // Modifica: esci dall'applicazione invece di chiudere solo il form
        }

        #endregion

        #region Authentication Methods (MANTENUTI con correzioni)

        private async void HandleJiraAuthentication()
        {
            try
            {
                // Disabilita controlli durante elaborazione
                SetJiraControlsEnabled(false);
                UpdateStatus("🔄 Verifica credenziali in corso...");

                // Leggi i valori dal form
                string server = txtServer?.Text?.Trim() ?? "";
                string username = txtUsername?.Text?.Trim() ?? "";
                string token = txtToken?.Text?.Trim() ?? "";

                WriteDebugLog($"HandleJiraAuthentication - Server: {server}, Username: {username}");

                // Usa il servizio di autenticazione Jira
                var jiraAuth = JiraAuthenticator.CreateDefault();
                var result = await jiraAuth.AuthenticateAsync(server, username, token);

                if (result.IsSuccess)
                {
                    WriteDebugLog("Autenticazione Jira API riuscita");
                    UpdateStatus("✅ Credenziali salvate correttamente");

                    // ✅ CORREZIONE: Usa Windows Toast invece di MessageBox
                    _toastService.ShowSuccess(
                        "🎉 Jira API",
                        "Credenziali salvate correttamente!\nAvvio applicazione..."
                    );

                    // Breve pausa per mostrare il toast
                    await Task.Delay(1500);

                    // ✅ CORREZIONE: Imposta DialogResult.OK
                    WriteDebugLog("Impostazione DialogResult.OK per avvio MainForm");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    WriteDebugLog($"Autenticazione Jira API fallita: {result.ErrorMessage}");
                    UpdateStatus("❌ Errore nelle credenziali", true);

                    // ✅ CORREZIONE: Usa Windows Toast invece di MessageBox
                    _toastService.ShowError(
                        "❌ Errore Credenziali",
                        $"Autenticazione fallita:\n{result.ErrorMessage}"
                    );

                    // Riabilita controlli per correggere i dati
                    SetJiraControlsEnabled(true);
                }
            }
            catch (Exception ex)
            {
                WriteErrorLog("HandleJiraAuthentication", ex);
                UpdateStatus("❌ Errore di sistema", true);

                // ✅ CORREZIONE: Usa Windows Toast invece di MessageBox
                _toastService.ShowError(
                    "❌ Errore Sistema",
                    $"Errore imprevisto:\n{ex.Message}"
                );

                // Riabilita controlli
                SetJiraControlsEnabled(true);
            }
        }

        #endregion

        #region Mode Switching (MANTENUTI - stessa logica)

        private void SwitchToJiraApiMode()
        {
            WriteDebugLog("SwitchToJiraApiMode() - Passaggio a modalità Jira API");

            try
            {
                // UI visibility - nomi corretti dal Designer
                pnlJiraAuth.Visible = true;
                pnlMicrosoftAuth.Visible = false;

                // Validation
                ValidateJiraFields();

                WriteDebugLog("Switch a modalità API completato");
            }
            catch (Exception ex)
            {
                WriteErrorLog("SwitchToJiraApiMode", ex);
            }
        }

        private void SwitchToMicrosoftMode()
        {
            WriteDebugLog("SwitchToMicrosoftMode() - Passaggio a modalità Microsoft SSO");

            try
            {
                // UI visibility - nomi corretti dal Designer
                pnlJiraAuth.Visible = false;
                pnlMicrosoftAuth.Visible = true;

                // Reset button states
                btnSave.Enabled = true;

                WriteDebugLog("Switch a modalità Microsoft completato");
            }
            catch (Exception ex)
            {
                WriteErrorLog("SwitchToMicrosoftMode", ex);
            }
        }

        #endregion

        #region Validation (MANTENUTA - stessa logica)

        private void ValidateJiraFields()
        {
            try
            {
                bool isValid = IsJiraFormValid();
                btnSave.Enabled = isValid;
                btnTest.Enabled = isValid;
            }
            catch (Exception ex)
            {
                WriteErrorLog("ValidateJiraFields", ex);
            }
        }

        private bool IsJiraFormValid()
        {
            return !string.IsNullOrWhiteSpace(txtServer?.Text) &&
                   !string.IsNullOrWhiteSpace(txtUsername?.Text) &&
                   !string.IsNullOrWhiteSpace(txtToken?.Text) &&
                   (txtUsername?.Text?.Contains("@") ?? false);
        }

        private bool ValidateJiraApiInputs()
        {
            if (!IsJiraFormValid())
            {
                _toastService.ShowError("Validazione", "Compilare tutti i campi richiesti");
                return false;
            }

            return true;
        }

        #endregion

        #region Connection Testing (MANTENUTO - stessa logica)

        private async Task<bool> TestJiraApiConnectionAsync(bool showToast = true)
        {
            if (_isTestingConnection)
            {
                WriteDebugLog("Test connessione già in corso, ignoro richiesta");
                return false;
            }

            try
            {
                _isTestingConnection = true;
                WriteDebugLog("TestJiraApiConnectionAsync - Inizio test connessione Jira API");

                // Valida input
                if (!ValidateJiraApiInputs())
                {
                    return false;
                }

                // UI feedback
                btnTest.Enabled = false;
                btnSave.Enabled = false;
                UpdateStatus("Test connessione in corso...");
                this.Cursor = Cursors.WaitCursor;

                // Test connessione
                bool result = await _configService.TestCredentialsAsync(
                    txtServer.Text.Trim(),
                    txtUsername.Text.Trim(),
                    txtToken.Text.Trim()
                );

                // Aggiorna UI
                if (result)
                {
                    WriteDebugLog("Test connessione Jira API riuscito");
                    UpdateStatus("✅ Connessione riuscita!");

                    if (showToast)
                    {
                        _toastService.ShowSuccess("Test riuscito", "Connessione a Jira stabilita correttamente");
                    }
                }
                else
                {
                    WriteDebugLog("Test connessione Jira API fallito");
                    UpdateStatus("❌ Connessione fallita", true);

                    if (showToast)
                    {
                        _toastService.ShowError("Test fallito", "Impossibile connettersi a Jira. Verifica le credenziali.");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                WriteErrorLog("TestJiraApiConnectionAsync", ex);
                UpdateStatus("❌ Errore durante il test", true);

                if (showToast)
                {
                    _toastService.ShowError("Errore", "Errore durante il test di connessione");
                }

                return false;
            }
            finally
            {
                _isTestingConnection = false;
                ValidateJiraFields(); // Ripristina stato bottoni
                this.Cursor = Cursors.Default;
            }
        }

        #endregion

        #region UI State Management (MANTENUTI - stessa logica)

        private void UpdateStatus(string message, bool isError = false)
        {
            try
            {
                lblStatus.Text = message;
                lblStatus.ForeColor = isError ?
                    Color.FromArgb(192, 57, 43) :
                    Color.FromArgb(120, 120, 120);

                WriteDebugLog($"Status aggiornato: {message} (Error: {isError})");
            }
            catch (Exception ex)
            {
                WriteErrorLog("UpdateStatus", ex);
            }
        }

        private void SetMicrosoftLoginState(bool logging)
        {
            try
            {
                btnMicrosoftLogin.Text = logging ?
                    "🔄 Connessione..." : "🔗 Accedi con Microsoft";
                btnSave.Enabled = !logging;
                btnCancel.Enabled = !logging;

                rbJiraApi.Enabled = !logging;
                rbMicrosoftSSO.Enabled = !logging;

                this.Cursor = logging ? Cursors.WaitCursor : Cursors.Default;
            }
            catch (Exception ex)
            {
                WriteErrorLog("SetMicrosoftLoginState", ex);
            }
        }

        private void SetJiraControlsEnabled(bool enabled)
        {
            try
            {
                txtServer.Enabled = enabled;
                txtUsername.Enabled = enabled;
                txtToken.Enabled = enabled;
                btnSave.Enabled = enabled && IsJiraFormValid();
                btnTest.Enabled = enabled && IsJiraFormValid();

                this.Cursor = enabled ? Cursors.Default : Cursors.WaitCursor;
            }
            catch (Exception ex)
            {
                WriteErrorLog("SetJiraControlsEnabled", ex);
            }
        }

        #endregion

        #region Form Cleanup (MANTENUTO con miglioramenti)

        private void OnFormClosing()
        {
            try
            {
                WriteDebugLog("Form in chiusura - Cleanup risorse");

                // Cleanup WebViewAuthenticator
                if (_webViewAuthenticator != null)
                {
                    WriteDebugLog("Cleanup WebViewAuthenticator");
                    _webViewAuthenticator = null;
                }

                // ✅ NUOVO: Log fine sessione
                _logger.LogSession("FRMCREDENTIALS SESSION END");
            }
            catch (Exception ex)
            {
                WriteErrorLog("OnFormClosing", ex);
            }
        }

        #endregion

        #region Public Properties (MANTENUTI)

        public string ServerUrl => txtServer.Text.Trim();
        public string UserEmail => txtUsername.Text.Trim();
        public string ApiToken => txtToken.Text.Trim();
        public bool IsJiraApiMode => rbJiraApi.Checked;
        public bool IsMicrosoftSSOMode => rbMicrosoftSSO.Checked;

        // ✅ MODIFICATO: Non espone più _logFilePath ma info logger
        public string LogInfo => _logger.GetLogFilePath();

        #endregion
    }
}