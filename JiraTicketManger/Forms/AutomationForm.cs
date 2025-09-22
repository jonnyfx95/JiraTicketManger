using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Services;

namespace JiraTicketManager.Forms
{
    /// <summary>
    /// Form per l'automazione Jira - Area Demografia
    /// Interfaccia moderna con console log in tempo reale
    /// </summary>
    public partial class AutomationForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraAutomationService _automationService;
        private CancellationTokenSource _cancellationTokenSource;

        // Stato dell'automazione
        private bool _isRunning = false;
        private DateTime _startTime;
        private string _currentLogFile = "";
        private string _currentErrorLogFile = "";

        // Contatori
        private int _ticketsFound = 0;
        private int _ticketsProcessed = 0;
        private int _ticketsSuccess = 0;
        private int _ticketsErrors = 0;

        #endregion

        #region Constructor

        public AutomationForm()
        {
            _logger = LoggingService.CreateForComponent("AutomationForm");

            InitializeComponent();
            InitializeForm();

            // Inizializza il servizio di automazione
            _automationService = new JiraAutomationService();
        }

        #endregion

        #region Form Initialization

        private void InitializeForm()
        {
            try
            {
                _logger.LogInfo("Inizializzazione AutomationForm");

                // Configura form properties
                ConfigureFormProperties();

                // Setup event handlers
                SetupEventHandlers();

                // Inizializza stato UI
                InitializeUIState();

                // Crea directory logs se non esiste
                EnsureLogDirectoryExists();

                // Log iniziale nella console
                LogToConsole("[Sistema] AutomationForm inizializzata correttamente", LogLevel.Info);
                LogToConsole("[Info] Pronta per avviare automazione Area Demografia", LogLevel.Info);

                _logger.LogInfo("AutomationForm inizializzata con successo");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione AutomationForm", ex);
                MessageBox.Show($"Errore inizializzazione: {ex.Message}", "Errore",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigureFormProperties()
        {
            // Configura proprietà form
            this.Icon = SystemIcons.Application; // TODO: Add custom icon
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ShowInTaskbar = true;
        }

        private void SetupEventHandlers()
        {
            // Event handlers per i pulsanti
            btnStartAutomation.Click += OnStartAutomationClick;
            btnStopAutomation.Click += OnStopAutomationClick;
            btnClearLog.Click += OnClearLogClick;
            btnSaveLog.Click += OnSaveLogClick;

            // Event handler per chiusura form
            this.FormClosing += OnFormClosing;
        }

        private void InitializeUIState()
        {
            // Imposta stato iniziale UI
            SetAutomationState(false);
            ResetCounters();
            UpdateStatusIndicator("Pronto", Color.FromArgb(40, 167, 69));
            UpdateProgressInfo("In attesa di avviare l'automazione...");
        }

        private void EnsureLogDirectoryExists()
        {
            var logDirectory = Path.Combine(Application.StartupPath, "logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
                _logger.LogInfo($"Cartella logs creata: {logDirectory}");
            }
        }

        #endregion

        #region Event Handlers

        private async void OnStartAutomationClick(object sender, EventArgs e)
        {
            try
            {
                if (_isRunning)
                {
                    LogToConsole("[Warning] Automazione già in corso", LogLevel.Warning);
                    return;
                }

                await StartAutomationAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore avvio automazione", ex);
                LogToConsole($"[Errore] Impossibile avviare automazione: {ex.Message}", LogLevel.Error);
                SetAutomationState(false);
            }
        }

        private void OnStopAutomationClick(object sender, EventArgs e)
        {
            try
            {
                if (!_isRunning)
                {
                    LogToConsole("[Warning] Nessuna automazione in corso", LogLevel.Warning);
                    return;
                }

                StopAutomation();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore stop automazione", ex);
                LogToConsole($"[Errore] Errore durante stop: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnClearLogClick(object sender, EventArgs e)
        {
            try
            {
                txtConsoleLog.Clear();
                LogToConsole("[Sistema] Console pulita", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia log", ex);
            }
        }

        private void OnSaveLogClick(object sender, EventArgs e)
        {
            try
            {
                SaveConsoleLog();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio log", ex);
                LogToConsole($"[Errore] Impossibile salvare log: {ex.Message}", LogLevel.Error);
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_isRunning)
                {
                    var result = MessageBox.Show(
                        "Automazione in corso. Interrompere e chiudere?",
                        "Conferma Chiusura",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }

                    StopAutomation();
                }

                _logger.LogInfo("AutomationForm chiusa");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore chiusura form", ex);
            }
        }

        #endregion

        #region Automation Control

        private async Task StartAutomationAsync()
        {
            try
            {
                _logger.LogInfo("=== AVVIO AUTOMAZIONE JIRA ===");

                // Prepara stato per nuova esecuzione
                PrepareNewAutomationRun();

                // Imposta UI in stato "running"
                SetAutomationState(true);
                UpdateStatusIndicator("In Esecuzione", Color.FromArgb(255, 193, 7));

                LogToConsole("", LogLevel.Info);
                LogToConsole("=".PadRight(60, '='), LogLevel.Info);
                LogToConsole("[Sistema] AVVIO AUTOMAZIONE AREA DEMOGRAFIA", LogLevel.Info);
                LogToConsole($"[Info] Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", LogLevel.Info);
                LogToConsole("=".PadRight(60, '='), LogLevel.Info);

                // Crea cancellation token per permettere lo stop
                _cancellationTokenSource = new CancellationTokenSource();

                // Esegui automazione tramite servizio
                await _automationService.ExecuteAutomationAsync(
                    _cancellationTokenSource.Token,
                    OnProgressUpdate,
                    OnTicketProcessed);

                // Automazione completata
                OnAutomationCompleted();
            }
            catch (OperationCanceledException)
            {
                LogToConsole("[Sistema] Automazione interrotta dall'utente", LogLevel.Warning);
                OnAutomationStopped();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore durante automazione", ex);
                LogToConsole($"[Errore] Automazione fallita: {ex.Message}", LogLevel.Error);
                OnAutomationError(ex);
            }
        }

        private void StopAutomation()
        {
            try
            {
                LogToConsole("[Sistema] Richiesta interruzione automazione...", LogLevel.Warning);

                _cancellationTokenSource?.Cancel();

                // Aggiorna UI immediatamente
                SetAutomationState(false);
                UpdateStatusIndicator("Interrotto", Color.FromArgb(220, 53, 69));
                UpdateProgressInfo("Automazione interrotta dall'utente");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore interruzione automazione", ex);
            }
        }

        private void PrepareNewAutomationRun()
        {
            // Reset contatori
            ResetCounters();

            // Crea nomi file di log con timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _currentLogFile = Path.Combine(Application.StartupPath, "logs", $"automation_{timestamp}.log");
            _currentErrorLogFile = Path.Combine(Application.StartupPath, "logs", $"automation_errors_{timestamp}.log");

            // Aggiorna footer con info log files
            lblFooterInfo.Text = $"🕒 Avvio: {DateTime.Now:HH:mm:ss} | 📂 Log: {Path.GetFileName(_currentLogFile)}";

            _startTime = DateTime.Now;
        }

        #endregion

        #region Automation Callbacks

        private void OnProgressUpdate(string message, int current, int total)
        {
            try
            {
                // Assicurati che l'update UI avvenga nel thread principale
                if (InvokeRequired)
                {
                    Invoke(new Action(() => OnProgressUpdate(message, current, total)));
                    return;
                }

                // Aggiorna progress bar
                if (total > 0)
                {
                    progressMain.Maximum = total;
                    progressMain.Value = Math.Min(current, total);
                }

                // Aggiorna messaggio di progresso
                UpdateProgressInfo(message);

                // Log alla console
                LogToConsole($"[Progress] {message} ({current}/{total})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore update progresso", ex);
            }
        }

        private void OnTicketProcessed(string ticketKey, bool success, string details)
        {
            try
            {
                // Assicurati che l'update UI avvenga nel thread principale
                if (InvokeRequired)
                {
                    Invoke(new Action(() => OnTicketProcessed(ticketKey, success, details)));
                    return;
                }

                // Aggiorna contatori
                _ticketsProcessed++;

                if (success)
                {
                    _ticketsSuccess++;
                    LogToConsole($"[Successo] {ticketKey}: {details}", LogLevel.Success);
                }
                else
                {
                    _ticketsErrors++;
                    LogToConsole($"[Errore] {ticketKey}: {details}", LogLevel.Error);
                }

                // Aggiorna UI contatori
                UpdateCounters();
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore processing ticket callback", ex);
            }
        }

        private void OnAutomationCompleted()
        {
            try
            {
                var duration = DateTime.Now - _startTime;

                LogToConsole("", LogLevel.Info);
                LogToConsole("=".PadRight(60, '='), LogLevel.Success);
                LogToConsole("[Sistema] AUTOMAZIONE COMPLETATA CON SUCCESSO", LogLevel.Success);
                LogToConsole($"[Info] Durata: {duration:mm\\:ss}", LogLevel.Info);
                LogToConsole($"[Info] Ticket processati: {_ticketsSuccess}/{_ticketsProcessed}", LogLevel.Info);
                LogToConsole("=".PadRight(60, '='), LogLevel.Success);

                SetAutomationState(false);
                UpdateStatusIndicator("Completato", Color.FromArgb(40, 167, 69));
                UpdateProgressInfo($"Automazione completata in {duration:mm\\:ss}");

                // Aggiorna footer
                lblFooterInfo.Text = $"🕒 Completato: {DateTime.Now:HH:mm:ss} | ✅ Successi: {_ticketsSuccess} | ❌ Errori: {_ticketsErrors}";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore completion callback", ex);
            }
        }

        private void OnAutomationStopped()
        {
            var duration = DateTime.Now - _startTime;

            LogToConsole("", LogLevel.Warning);
            LogToConsole("[Sistema] AUTOMAZIONE INTERROTTA", LogLevel.Warning);
            LogToConsole($"[Info] Durata parziale: {duration:mm\\:ss}", LogLevel.Info);

            SetAutomationState(false);
            UpdateStatusIndicator("Interrotto", Color.FromArgb(220, 53, 69));
            UpdateProgressInfo("Automazione interrotta dall'utente");
        }

        private void OnAutomationError(Exception ex)
        {
            LogToConsole("", LogLevel.Error);
            LogToConsole("[Sistema] AUTOMAZIONE FALLITA", LogLevel.Error);
            LogToConsole($"[Errore] {ex.Message}", LogLevel.Error);

            SetAutomationState(false);
            UpdateStatusIndicator("Errore", Color.FromArgb(220, 53, 69));
            UpdateProgressInfo($"Automazione fallita: {ex.Message}");
        }

        #endregion

        #region UI Updates

        private void SetAutomationState(bool isRunning)
        {
            _isRunning = isRunning;

            // Abilita/disabilita pulsanti
            btnStartAutomation.Enabled = !isRunning;
            btnStopAutomation.Enabled = isRunning;

            // Cambia colori pulsanti
            if (isRunning)
            {
                btnStartAutomation.BackColor = Color.FromArgb(108, 117, 125);
                btnStopAutomation.BackColor = Color.FromArgb(220, 53, 69);
            }
            else
            {
                btnStartAutomation.BackColor = Color.FromArgb(40, 167, 69);
                btnStopAutomation.BackColor = Color.FromArgb(108, 117, 125);
            }
        }

        private void UpdateStatusIndicator(string status, Color color)
        {
            lblStatusValue.Text = status;
            lblStatusValue.ForeColor = color;
            picStatus.BackColor = color;
        }

        private void UpdateProgressInfo(string message)
        {
            lblProgress.Text = message;
        }

        private void ResetCounters()
        {
            _ticketsFound = 0;
            _ticketsProcessed = 0;
            _ticketsSuccess = 0;
            _ticketsErrors = 0;

            UpdateCounters();
        }

        private void UpdateCounters()
        {
            lblFoundValue.Text = _ticketsFound.ToString();
            lblProcessedValue.Text = _ticketsProcessed.ToString();
            lblSuccessValue.Text = _ticketsSuccess.ToString();
            lblErrorsValue.Text = _ticketsErrors.ToString();
        }

        public void SetTicketsFound(int count)
        {
            _ticketsFound = count;
            UpdateCounters();
        }

        #endregion

        #region Console Logging

        private enum LogLevel
        {
            Info,
            Success,
            Warning,
            Error
        }

        private void LogToConsole(string message, LogLevel level)
        {
            try
            {
                // Assicurati che l'update UI avvenga nel thread principale
                if (InvokeRequired)
                {
                    Invoke(new Action(() => LogToConsole(message, level)));
                    return;
                }

                // Timestamp
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var formattedMessage = $"[{timestamp}] {message}";

                // Colore basato sul livello
                Color textColor = level switch
                {
                    LogLevel.Success => Color.FromArgb(40, 167, 69),
                    LogLevel.Warning => Color.FromArgb(255, 193, 7),
                    LogLevel.Error => Color.FromArgb(220, 53, 69),
                    _ => Color.FromArgb(248, 249, 250)
                };

                // Aggiungi al RichTextBox con colore
                txtConsoleLog.SelectionStart = txtConsoleLog.TextLength;
                txtConsoleLog.SelectionLength = 0;
                txtConsoleLog.SelectionColor = textColor;
                txtConsoleLog.AppendText(formattedMessage + Environment.NewLine);

                // Scroll automatico
                txtConsoleLog.SelectionStart = txtConsoleLog.TextLength;
                txtConsoleLog.ScrollToCaret();

                // Limita righe per performance (mantieni solo ultime 1000 righe)
                if (txtConsoleLog.Lines.Length > 1000)
                {
                    var lines = txtConsoleLog.Lines;
                    var newLines = new string[500];
                    Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                    txtConsoleLog.Lines = newLines;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore logging console", ex);
            }
        }

        private void SaveConsoleLog()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Title = "Salva Log Console",
                    Filter = "File di testo (*.txt)|*.txt|Tutti i file (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"console_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, txtConsoleLog.Text);
                    LogToConsole($"[Sistema] Log salvato: {Path.GetFileName(saveDialog.FileName)}", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore salvataggio console log", ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Metodo pubblico per log da servizi esterni
        /// </summary>
        public void LogMessage(string message, bool isError = false)
        {
            LogToConsole(message, isError ? LogLevel.Error : LogLevel.Info);
        }

        /// <summary>
        /// Metodo pubblico per aggiornare progresso da servizi esterni
        /// </summary>
        public void UpdateProgress(string message, int current = 0, int total = 0)
        {
            OnProgressUpdate(message, current, total);
        }

        #endregion
    }
}