using JiraTicketManager.Data.Models;
using JiraTicketManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    /// <summary>
    /// Form per la gestione dei membri delle organizzazioni Jira.
    /// Estrae i membri delle organizzazioni e permette filtro, cache e export.
    /// STILE IDENTICO a PhoneBookForm.
    /// Path: Forms/OrganizationMembersForm.cs
    /// </summary>
    public partial class OrganizationMembersForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly OrganizationMembersService _organizationMembersService;
        private readonly WindowsToastService _toastService;

        // State
        private bool _isLoading = false;
        private List<OrganizationMemberEntry> _allMembers;
        private List<OrganizationMemberEntry> _filteredMembers;

        #endregion

        #region Constructor

        public OrganizationMembersForm()
        {
            InitializeComponent();

            _logger = LoggingService.CreateForComponent("OrganizationMembersForm");
            _logger.LogInfo("OrganizationMembersForm inizializzata");

            var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _organizationMembersService = new OrganizationMembersService(apiService);
            _toastService = WindowsToastService.CreateDefault();

            SetupEventHandlers();

            _logger.LogInfo("Servizi inizializzati e pronti");
        }

        #endregion

        #region Event Handlers Setup

        private void SetupEventHandlers()
        {
            this.Load += OnFormLoad;
            this.Shown += OnFormShown;

            txtFilter.TextChanged += OnFilterTextChanged;
            btnRefresh.Click += OnRefreshClick;
            btnExport.Click += OnExportClick;
            btnClear.Click += OnClearClick;

            dgvOrganizationMembers.CellDoubleClick += OnCellDoubleClick;
            dgvOrganizationMembers.SelectionChanged += OnSelectionChanged;
        }

        #endregion

        #region Form Events

        private async void OnFormLoad(object sender, EventArgs e)
        {
            _logger.LogInfo("Form Load - Inizio caricamento membri organizzazioni");
            await LoadOrganizationMembersAsync();
        }

        private void OnFormShown(object sender, EventArgs e)
        {
            _logger.LogInfo("Form Shown");
            txtFilter.Focus();
        }

        #endregion

        #region Toolbar Events

        private void OnFilterTextChanged(object sender, EventArgs e)
        {
            _logger.LogDebug($"Filtro cambiato: {txtFilter.Text}");
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

            var result = MessageBox.Show(
                "Questo rigenererà la cache dalla API Jira.\n" +
                "L'operazione potrebbe richiedere MOLTI minuti (deve scaricare tutte le organizzazioni e i loro membri).\n\n" +
                "Continuare?",
                "Conferma Aggiornamento",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                _logger.LogInfo("Refresh confermato dall'utente");
                await HandleRefreshButtonAsync();
            }
        }

        private async void OnExportClick(object sender, EventArgs e)
        {
            _logger.LogInfo("Export Excel richiesto");

            if (_allMembers == null || _allMembers.Count == 0)
            {
                MessageBox.Show(
                    "Nessun membro da esportare.\n\nCarica prima i dati con il pulsante Aggiorna.",
                    "Nessun Dato",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
                saveDialog.FileName = $"MembriOrganizzazioni_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                saveDialog.Title = "Esporta Membri Organizzazioni in Excel";
                saveDialog.DefaultExt = "xlsx";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    await HandleExportToExcelAsync(saveDialog.FileName);
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

            try
            {
                var row = dgvOrganizationMembers.Rows[e.RowIndex];
                var columnName = dgvOrganizationMembers.Columns[e.ColumnIndex].Name;

                // Doppio click su Email → Copia email
                if (columnName == "colEmail")
                {
                    var email = row.Cells["colEmail"].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        System.Windows.Forms.Clipboard.SetText(email);
                        _toastService.ShowInfo("Copiato", $"Email copiata: {email}");
                        _logger.LogInfo($"Email copiata in clipboard: {email}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione doppio click", ex);
            }
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            // Placeholder per future funzionalità
        }

        #endregion

        #region Private Methods - Data Loading

        private async Task LoadOrganizationMembersAsync()
        {
            try
            {
                ShowLoading(true, "Caricamento membri organizzazioni...");
                _logger.LogInfo("=== INIZIO CARICAMENTO MEMBRI ORGANIZZAZIONI ===");

                _allMembers = await _organizationMembersService.LoadOrganizationMembersAsync(
                    new Progress<string>(status => UpdateStatus(status))
                );

                DisplayMembers(_allMembers);
                UpdateStatusLabels();

                _logger.LogInfo($"✅ Membri caricati: {_allMembers.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore caricamento membri", ex);
                MessageBox.Show(
                    $"Errore durante il caricamento dei membri:\n\n{ex.Message}",
                    "Errore",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                _allMembers = new List<OrganizationMemberEntry>();
                DisplayMembers(_allMembers);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task HandleRefreshButtonAsync()
        {
            try
            {
                ShowLoading(true, "Aggiornamento da Jira API...");
                _logger.LogInfo("=== INIZIO REFRESH DA API ===");

                _allMembers = await _organizationMembersService.RefreshFromApiAsync(
                    new Progress<string>(status => UpdateStatus(status))
                );

                txtFilter.Clear();
                _filteredMembers = null;

                DisplayMembers(_allMembers);
                UpdateStatusLabels();

                _logger.LogInfo($"✅ Refresh completato: {_allMembers.Count} membri");

                _toastService.ShowSuccess("Aggiornamento", $"Dati aggiornati: {_allMembers.Count} membri");
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

        private async Task HandleExportToExcelAsync(string filePath)
        {
            try
            {
                ShowLoading(true, "Esportazione in Excel...");
                _logger.LogInfo($"=== EXPORT EXCEL: {filePath} ===");

                _logger.LogInfo($"📊 _allMembers: {_allMembers?.Count ?? 0} membri");
                _logger.LogInfo($"📊 _filteredMembers: {_filteredMembers?.Count.ToString() ?? "null"}");

                List<OrganizationMemberEntry> membersToExport;

                bool hasActiveFilter = _filteredMembers != null && _filteredMembers.Count < (_allMembers?.Count ?? 0);

                if (hasActiveFilter)
                {
                    _logger.LogInfo($"🔍 Filtro attivo: esporto {_filteredMembers.Count} membri filtrati");
                    membersToExport = _filteredMembers;
                }
                else
                {
                    _logger.LogInfo($"📋 Nessun filtro: esporto tutti i {_allMembers?.Count ?? 0} membri");
                    membersToExport = _allMembers;
                }

                if (membersToExport == null || membersToExport.Count == 0)
                {
                    _logger.LogWarning("⚠️ Nessun membro da esportare");
                    MessageBox.Show(
                        "Nessun membro da esportare!\n\nVerifica che i dati siano stati caricati correttamente.",
                        "Nessun Dato",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    ShowLoading(false);
                    return;
                }

                _logger.LogInfo($"🚀 Avvio export: {membersToExport.Count} membri");

                await _organizationMembersService.ExportToExcelAsync(membersToExport, filePath);

                _logger.LogInfo($"✅ Export completato: {membersToExport.Count} membri esportati");

                var result = MessageBox.Show(
                    $"✅ Export completato!\n\n" +
                    $"📊 {membersToExport.Count} membri esportati in un unico foglio\n" +
                    $"📁 {filePath}\n\n" +
                    $"Vuoi aprire il file?",
                    "Export Completato",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information
                );

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception openEx)
                    {
                        _logger.LogError("Errore apertura file Excel", openEx);
                        MessageBox.Show(
                            $"File esportato correttamente!\n\n" +
                            $"Impossibile aprirlo automaticamente.\n" +
                            $"Percorso: {filePath}",
                            "Avviso",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore export Excel", ex);
                MessageBox.Show(
                    $"Errore durante l'esportazione:\n\n{ex.Message}\n\n" +
                    $"Verifica i log per maggiori dettagli.",
                    "Errore Export",
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

        #region Private Methods - Display & Filter

        private void DisplayMembers(List<OrganizationMemberEntry> members)
        {
            try
            {
                dgvOrganizationMembers.DataSource = null;
                dgvOrganizationMembers.DataSource = members;
                dgvOrganizationMembers.Refresh();

                _logger.LogDebug($"DataGridView aggiornata: {members?.Count ?? 0} righe");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore display membri", ex);
            }
        }

        private void ApplyFilter(string filterText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filterText))
                {
                    _filteredMembers = null;
                    DisplayMembers(_allMembers);
                    UpdateStatusLabels();
                    return;
                }

                var keywords = filterText.ToLowerInvariant()
                    .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                _filteredMembers = _allMembers.Where(m =>
                    keywords.All(keyword =>
                        (m.Organizzazione?.ToLowerInvariant().Contains(keyword) ?? false) ||
                        (m.Nome?.ToLowerInvariant().Contains(keyword) ?? false) ||
                        (m.Email?.ToLowerInvariant().Contains(keyword) ?? false)
                    )
                ).ToList();

                DisplayMembers(_filteredMembers);
                UpdateStatusLabels();

                _logger.LogDebug($"Filtro applicato: {_filteredMembers.Count} risultati su {_allMembers.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore applicazione filtro", ex);
            }
        }

        #endregion

        #region Private Methods - UI Updates

        private void UpdateStatusLabels()
        {
            try
            {
                int totalCount = _allMembers?.Count ?? 0;
                int filteredCount = _filteredMembers?.Count ?? totalCount;

                //lblTotalCount.Text = $"Totali: {totalCount}";
                //lblFilteredCount.Text = $"Mostrati: {filteredCount}";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore aggiornamento status labels", ex);
            }
        }

        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateStatus), message);
                return;
            }

            //lblStatus.Text = message;
            statusStrip1.Refresh();
        }

        private void ShowLoading(bool show, string message = "")
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, string>(ShowLoading), show, message);
                return;
            }

            _isLoading = show;
            prgLoading.Visible = show;

            btnRefresh.Enabled = !show;
            btnExport.Enabled = !show;
            txtFilter.Enabled = !show;

            if (show && !string.IsNullOrEmpty(message))
            {
                UpdateStatus(message);
            }
            else if (!show)
            {
                UpdateStatus("Pronto");
            }
        }

        #endregion
    }
}