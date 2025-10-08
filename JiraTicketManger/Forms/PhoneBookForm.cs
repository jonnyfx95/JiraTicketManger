using JiraTicketManager.Data.Models;
using JiraTicketManager.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    /// <summary>
    /// Form per la gestione della rubrica telefonica Jira.
    /// Estrae contatti dai ticket e permette filtro, cache e export.
    /// STILE IDENTICO A MainForm (DataGridView + StatusStrip)
    /// Path: Forms/PhoneBookForm.cs
    /// </summary>
    public partial class PhoneBookForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly PhoneBookService _phoneBookService;
        private readonly WindowsToastService _toastService;

        // State
        private bool _isLoading = false;
        private List<PhoneBookEntry> _allContacts;
        private List<PhoneBookEntry> _filteredContacts;

        #endregion

        #region Constructor

        public PhoneBookForm()
        {
            // Inizializza componenti Designer
            InitializeComponent();

            // Inizializza logger
            _logger = LoggingService.CreateForComponent("PhoneBookForm");
            _logger.LogInfo("PhoneBookForm inizializzata");

            // Inizializza servizi
            var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _phoneBookService = new PhoneBookService(apiService);
            _toastService = WindowsToastService.CreateDefault();

            // Setup eventi
            SetupEventHandlers();

            _logger.LogInfo("Servizi inizializzati e pronti");
        }

        #endregion

        #region Event Handlers Setup

        private void SetupEventHandlers()
        {
            // Form events
            this.Load += OnFormLoad;
            this.Shown += OnFormShown;

            // Toolbar events
            txtFilter.TextChanged += OnFilterTextChanged;
            btnRefresh.Click += OnRefreshClick;
            btnExport.Click += OnExportClick;
            btnClear.Click += OnClearClick;

            // DataGridView events
            dgvPhoneBook.CellDoubleClick += OnCellDoubleClick;
            dgvPhoneBook.SelectionChanged += OnSelectionChanged;
        }

        #endregion

        #region Form Events

        private async void OnFormLoad(object sender, EventArgs e)
        {
            _logger.LogInfo("Form Load - Inizio caricamento rubrica");

            // Carica rubrica (da cache o API)
            await LoadPhoneBookAsync();
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            _logger.LogInfo("Form Shown");

            // Focus sulla casella di ricerca
            txtFilter.Focus();
        }

        #endregion

        #region Toolbar Events

        private void OnFilterTextChanged(object sender, EventArgs e)
        {
            _logger.LogDebug($"Filtro cambiato: {txtFilter.Text}");

            // Applica filtro in tempo reale
            ApplyFilter(txtFilter.Text);
        }

        private async void OnRefreshClick(object sender, EventArgs e)
        {
            _logger.LogInfo("Refresh richiesto dall'utente");

            if (_isLoading)
            {
                MessageBox.Show(
                    "Caricamento in corso, attendere...",
                    "Attendere",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Conferma refresh
            var result = MessageBox.Show(
                "Questo rigenererà la cache dalla API Jira.\nL'operazione potrebbe richiedere diversi minuti.\n\nContinuare?",
                "Conferma Aggiornamento",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _logger.LogInfo("Refresh confermato dall'utente");
                await RefreshFromApiAsync();
            }
        }

        private async void OnExportClick(object sender, EventArgs e)
        {
            _logger.LogInfo("Export Excel richiesto");

            if (_allContacts == null || _allContacts.Count == 0)
            {
                MessageBox.Show(
                    "Nessun contatto da esportare.",
                    "Attenzione",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                saveDialog.FileName = $"RubricaJira_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                saveDialog.Title = "Esporta Rubrica in Excel";
                saveDialog.DefaultExt = "xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    await ExportToExcelAsync(saveDialog.FileName);
                }
            }
        }

        private void OnClearClick(object sender, EventArgs e)
        {
            _logger.LogInfo("Pulizia filtro");

            txtFilter.Clear();
            txtFilter.Focus();
        }

        #endregion

        #region DataGridView Events

        private void OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            _logger.LogDebug($"Double click su riga {e.RowIndex}");

            try
            {
                // Recupera il numero di telefono dalla cella

                var telefono = "3347632044";
              // var telefono = dgvPhoneBook.Rows[e.RowIndex].Cells["colTelefono"].Value?.ToString();

                if (!string.IsNullOrWhiteSpace(telefono))
                {
                    telefono = telefono.Replace(" ", "").Replace("-", "");
                    if (!telefono.StartsWith("+"))
                    {
                        telefono = "+39" + telefono;
                    }
                    if (chkTeams.Checked)
                    {
                        string teamsUri = $"tel:{telefono}";
                        Process.Start(new ProcessStartInfo(teamsUri) { UseShellExecute = true });
                    }
                    Clipboard.SetText(telefono);
                    _toastService.ShowInfo("Copiato", $"Numero copiato: {telefono}");
                    _logger.LogInfo($"Numero copiato in clipboard: {telefono}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione doppio click telefono", ex);
            }
        }


        private void OnSelectionChanged(object sender, EventArgs e)
        {
            // Opzionale: mostrare dettagli del contatto selezionato in futuro
        }

        #endregion

        #region Private Methods - Data Loading

        /// <summary>
        /// Carica la rubrica telefonica (da cache o API)
        /// </summary>
        private async Task LoadPhoneBookAsync()
        {
            try
            {
                ShowLoading(true, "Caricamento rubrica...");
                _logger.LogInfo("=== INIZIO CARICAMENTO RUBRICA ===");

                _allContacts = await _phoneBookService.LoadPhoneBookAsync(
                    new Progress<string>(status => UpdateStatus(status))
                );

                DisplayContacts(_allContacts);
                UpdateStatusLabels();

                _logger.LogInfo($"✅ Rubrica caricata: {_allContacts.Count} contatti");
                //_toastService.ShowSuccess("Rubrica", $"Caricati {_allContacts.Count} contatti");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore caricamento rubrica", ex);
                MessageBox.Show(
                    $"Errore durante il caricamento della rubrica:\n\n{ex.Message}",
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // Inizializza lista vuota per evitare null reference
                _allContacts = new List<PhoneBookEntry>();
                DisplayContacts(_allContacts);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Forza refresh dalla API Jira
        /// </summary>
        private async Task RefreshFromApiAsync()
        {
            try
            {
                ShowLoading(true, "Aggiornamento da Jira API...");
                _logger.LogInfo("=== INIZIO REFRESH DA API ===");

                _allContacts = await _phoneBookService.RefreshFromApiAsync(
                    new Progress<string>(status => UpdateStatus(status))
                );

                // Pulisci filtro
                txtFilter.Clear();
                _filteredContacts = null;

                DisplayContacts(_allContacts);
                UpdateStatusLabels();

                _logger.LogInfo($"✅ Refresh completato: {_allContacts.Count} contatti");
                
               _toastService.ShowSuccess("Aggiornamento", $"Rubrica aggiornata: {_allContacts.Count} contatti");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore refresh da API", ex);
                MessageBox.Show(
                    $"Errore durante l'aggiornamento:\n\n{ex.Message}",
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                ShowLoading(false);
            }
        }

        /// <summary>
        /// Esporta in Excel
        /// </summary>
        private async Task ExportToExcelAsync(string filePath)
        {
            try
            {
                ShowLoading(true, "Esportazione in Excel...");
                _logger.LogInfo($"=== EXPORT EXCEL: {filePath} ===");

                // Esporta contatti filtrati se presente filtro, altrimenti tutti
                var contactsToExport = _filteredContacts ?? _allContacts;

                await _phoneBookService.ExportToExcelAsync(contactsToExport, filePath);

                _logger.LogInfo($"✅ Export completato: {contactsToExport.Count} contatti");

                // Chiedi se aprire il file
                var result = MessageBox.Show(
                    $"Export completato!\n\n{contactsToExport.Count} contatti esportati in:\n{filePath}\n\nVuoi aprire il file?",
                    "Export Completato",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore export Excel", ex);
                MessageBox.Show(
                    $"Errore durante l'esportazione:\n\n{ex.Message}",
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                ShowLoading(false);
            }
        }

        #endregion

        #region Private Methods - Filtering

        /// <summary>
        /// Applica filtro in tempo reale sulla DataGridView
        /// </summary>
        private void ApplyFilter(string filterText)
        {
            if (_allContacts == null)
                return;

            if (string.IsNullOrWhiteSpace(filterText))
            {
                // Nessun filtro: mostra tutti
                _filteredContacts = null;
                DisplayContacts(_allContacts);
                tslResults.Text = $"📊 {_allContacts.Count} contatti";
                return;
            }

            // Split per virgola per filtri multipli
            var keywords = filterText.Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => !string.IsNullOrEmpty(k))
                .ToArray();

            // Filtra contatti (cerca in tutti i campi)
            _filteredContacts = _allContacts.Where(contact =>
            {
                var searchableText = $"{contact.Cliente} {contact.Applicativo} {contact.Area} {contact.Nome} {contact.Email} {contact.Telefono}".ToLowerInvariant();

                // Tutti i keyword devono essere presenti (AND logic)
                return keywords.All(keyword => searchableText.Contains(keyword));
            }).ToList();

            DisplayContacts(_filteredContacts);
            tslResults.Text = $"🔍 {_filteredContacts.Count} filtrati su {_allContacts.Count} totali";

            _logger.LogDebug($"Filtro applicato: '{filterText}' → {_filteredContacts.Count} risultati");
        }

        #endregion

        #region Private Methods - UI Updates

        /// <summary>
        /// Visualizza contatti nella DataGridView
        /// </summary>
        private void DisplayContacts(List<PhoneBookEntry> contacts)
        {
            try
            {
                dgvPhoneBook.DataSource = null;
                dgvPhoneBook.DataSource = contacts;
                dgvPhoneBook.Refresh();

                _logger.LogDebug($"Visualizzati {contacts?.Count ?? 0} contatti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione contatti", ex);
            }
        }

        /// <summary>
        /// Aggiorna le label di stato
        /// </summary>
        private void UpdateStatusLabels()
        {
            if (_allContacts != null)
            {
                tslResults.Text = $"📊 {_allContacts.Count} contatti";
                tslLastUpdate.Text = $"⏱️ Ultimo agg: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }

        /// <summary>
        /// Mostra/nasconde loading
        /// </summary>
        private void ShowLoading(bool show, string message = "")
        {
            _isLoading = show;
            prgLoading.Visible = show;

            btnRefresh.Enabled = !show;
            btnExport.Enabled = !show;
            btnClear.Enabled = !show;
            txtFilter.Enabled = !show;

            if (show)
            {
                Cursor = Cursors.WaitCursor;
                tslResults.Text = $"⏳ {message}";
            }
            else
            {
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Aggiorna messaggio di stato (thread-safe)
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }

            tslResults.Text = $"⏳ {message}";
            Application.DoEvents(); // Forza refresh UI
        }

        #endregion

       
    }
}