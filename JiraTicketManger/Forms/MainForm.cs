using JiraTicketManager.Business;
using JiraTicketManager.Data;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Data.Models; 
using JiraTicketManager.Services;
using JiraTicketManager.UI;
using JiraTicketManager.UI.Managers;
using JiraTicketManager.Utilities;
using JiraTicketManager.Testing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.DirectoryServices;
using System.Drawing;
using JiraTicketManager.Tools;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Forms;

namespace JiraTicketManager
{
    /// <summary>
    /// Form principale dell'applicazione Jira Ticket Manager.
    /// Integrato completamente con MainForm.Designer.cs esistente.
    /// Migrato da MainForm.vb con architettura moderna C#.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraApiService _apiService;
        private readonly ConfigService _configService;
        private readonly JiraDataService _dataService;
        private readonly WindowsToastService _toastService;

        private readonly Dictionary<ComboBox, List<string>> _comboBoxOriginalItems = new();
        private readonly Dictionary<ComboBox, System.Windows.Forms.Timer> _filterTimers = new();

#if DEBUG
        private DevelopmentTests _devTests;
#endif


        private bool _allowAutoSearch = false;


        private readonly IProgressService _progressService;

        private ComboBoxManager _comboBoxManager;

        // UI Managers (già implementati)
        private SidebarManager _sidebarManager;
        private ToolbarManager _toolbarManager;


        // State Management
        private bool _isInitialized = false;
        private bool _isLoading = false;
        private DataTable _currentData;
        private string _currentJQL = "";
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalRecords = 0;

        // Filter state
        private bool _isBasicMode = true;
        private Dictionary<string, object> _activeFilters = new Dictionary<string, object>();

        #endregion

        #region Constructor

        public MainForm()
        {
            // Initialize services first
            _logger = LoggingService.CreateForComponent("MainForm");
            _configService = new ConfigService();
            _apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _dataService = new JiraDataService(_apiService);
            _toastService = WindowsToastService.CreateDefault();


            _progressService = ProgressService.CreateStandalone(_toastService);
            _comboBoxManager = new ComboBoxManager(_dataService);

            _logger.LogInfo("MainForm constructor - Inizializzazione componenti...");

            // Initialize designer components
            InitializeComponent();

            // Setup UI
            InitializeForm();
        }

        #endregion

        #region Search Logic

       

        private async Task SearchTicketsAsync()
        {
            if (_isLoading) return;

            if (!_isInitialized)
            {
                _logger.LogDebug("Ricerca ignorata - applicazione non ancora inizializzata");
                return;
            }

            try
            {
                _isLoading = true;
                // 🔧 NON disabilitare ancora i controlli - dobbiamo leggere i valori prima!
                // SetControlsEnabled(false);  // ❌ SPOSTATO PIÙ SOTTO

                ShowProgress("🔍 Ricerca ticket in corso...");
                _logger.LogInfo("=== INIZIO RICERCA INTELLIGENTE ===");

                // *** DEBUG E COSTRUZIONE FILTRI PRIMA DI DISABILITARE ***
                string jql;

                if (rbJQLMode != null && rbJQLMode.Checked)
                {
                    // Modalità JQL
                    jql = txtJQLQuery?.Text?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(jql))
                    {
                        _toastService.ShowError("Errore", "Inserire una query JQL valida");
                        return;
                    }
                    _logger.LogInfo($"🔍 Modalità JQL: {jql}");
                }
                else if (_isBasicMode || (rbDate != null && rbDate.Checked))
                {
                    // Modalità Base o Date (entrambe usano filtri)
                    _logger.LogInfo($"🔍 Modalità filtri attiva");

#if DEBUG
                    // 🔧 DEBUG: Verifica stato ComboBox prima della ricerca
                    DebugComboBoxStates();
#endif

                    // 1. PRIORITÀ ASSOLUTA: Numero ticket
                    var ticketNumber = txtSearch?.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(ticketNumber) &&
                        !ticketNumber.StartsWith("Cerca ticket"))
                    {
                        var ticketKey = JQLBuilder.ParseTicketKey(ticketNumber);
                        if (!string.IsNullOrEmpty(ticketKey))
                        {
                            _logger.LogInfo($"Ricerca per numero ticket: {ticketKey}");
                            jql = $"key = \"{ticketKey}\"";
                        }
                        else
                        {
                            _logger.LogInfo($"Formato ticket non valido '{ticketNumber}' - usando JQL base");
                            jql = "project = CC AND statuscategory != \"Done\" ORDER BY updated DESC";
                        }
                    }
                    else
                    {
                        // *** RICERCA CON FILTRI CONDIZIONALI ***
                        var filters = BuildFiltersFromControls();
                        var hasCliente = filters.ContainsKey("Cliente");
                        var hasDateFilters = filters.Keys.Any(k => k.Contains("Creato") || k.Contains("Completato"));

                        _logger.LogInfo($"Filtri costruiti: {filters.Count}, Cliente: {hasCliente}, Date: {hasDateFilters}");

                        if (filters.Count == 0)
                        {
                            _logger.LogInfo("Nessun filtro attivo - usando JQL base");
                            jql = "project = CC AND statuscategory != \"Done\" ORDER BY updated DESC";
                        }
                        else
                        {
                            _logger.LogInfo($"Applicando {filters.Count} filtri alla ricerca");

                            // Log dettagliato dei filtri
                            foreach (var filter in filters)
                            {
                                _logger.LogInfo($"   Filtro: {filter.Key} = '{filter.Value}'");
                            }

                            var criteria = ConvertToSearchCriteria(filters);
                            jql = JQLBuilder.FromCriteria(criteria).Build();

                            _logger.LogInfo($"🔍 JQL generata dai filtri: {jql}");
                        }
                    }
                }
                else
                {
                    // Fallback
                    _logger.LogWarning("Modalità non riconosciuta - usando JQL base");
                    jql = "project = CC AND statuscategory != \"Done\" ORDER BY updated DESC";
                }

                // 🔧 ORA disabilita i controlli DOPO aver costruito i filtri
                SetControlsEnabled(false);

                // Execute search
                _currentJQL = jql;
                _logger.LogInfo($"Ricerca con JQL: {jql}");

                var startAt = (_currentPage - 1) * _pageSize;
                var searchResult = await _apiService.SearchIssuesAsync(jql, startAt, _pageSize);

                _logger.LogInfo("🔄 Conversione con JiraDataConverter...");
                _currentData = JiraDataConverter.ConvertToDataTable(searchResult.Issues, _logger);
                _totalRecords = searchResult.Total;

                // Converti e aggiorna DataGrid
                _currentData = JiraDataConverter.ConvertToDataTable(searchResult.Issues, _logger);
                dgvTickets.DataSource = _currentData;
                ConfigureDataGridColumns();

                // Aggiorna UI
                UpdateResultsInfo();
                UpdateNavigationButtons();

                _logger.LogInfo($"Navigazione a pagina {_currentPage} completata");
                UpdateStatusMessage($"✅ Trovati {_totalRecords} ticket - Pagina {_currentPage}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca ticket", ex);
                // ... resto gestione errori ...
            }
            finally
            {
                _isLoading = false;
                SetControlsEnabled(true);  // Riabilita alla fine
                HideProgress();
            }
        }

        /// <summary>
        /// Ricerca con filtri condizionali (Cliente deve essere selezionato per altri filtri)
        /// </summary>
        private async Task SearchWithConditionalFilters()
        {
            var filters = BuildFiltersFromControls();
            var hasCliente = filters.ContainsKey("Cliente");

            _logger.LogInfo($"Filtri costruiti: {filters.Count}, Cliente selezionato: {hasCliente}");

            // Se NON c'è cliente selezionato, usa solo JQL base
            if (!hasCliente)
            {
                _logger.LogInfo("Nessun cliente selezionato - usando JQL base");
                await ExecuteSearchWithJQL("project = CC ORDER BY updated DESC");
                return;
            }

            // Cliente selezionato - applica tutti i filtri
            var criteria = ConvertToSearchCriteria(filters);
            var jql = JQLBuilder.FromCriteria(criteria).Build();
            await ExecuteSearchWithJQL(jql);
        }

        /// <summary>
        /// Esegue ricerca con JQL specifica (riutilizza logica esistente)
        /// </summary>
        private async Task ExecuteSearchWithJQL(string jql)
        {
            _logger.LogInfo($"Esecuzione JQL: {jql}");

            _currentJQL = jql;
            var startAt = (_currentPage - 1) * _pageSize;
            var searchResult = await _apiService.SearchIssuesAsync(jql, startAt, _pageSize);

            _currentData = JiraDataConverter.ConvertToDataTable(searchResult.Issues, _logger);
            _totalRecords = searchResult.Total;

            dgvTickets.DataSource = _currentData;
            UpdateResultsInfo();
            UpdateNavigationButtons();

            _logger.LogInfo($"Ricerca completata: {searchResult.Issues.Count} di {_totalRecords}");
        }

        #endregion


        #region Form Initialization

        private void InitializeForm()
        {
            try
            {
                _logger.LogInfo("=== INIZIALIZZAZIONE MAINFORM ===");

                // *** NUOVO: Disabilita controlli durante setup ***
                SetControlsEnabled(false);

                // Configure form
                ConfigureFormProperties();

                // Initialize UI components
                InitializeDataGrid();
                InitializeFilters();
                InitializeStatusBar();

                // Setup UI Managers
                InitializeUIManagers();

                // Connect event handlers
                SetupEventHandlers();

                // 🔧 AGGIUNTO: Inizializza sistema filtri automatici
                InitializeFilterDebouncing();

                // Set initial state
                SetFilterMode(true); // Start in Basic mode
                _isInitialized = true;

                var delayTimer = new System.Windows.Forms.Timer();
                delayTimer.Interval = 2000; // 2 secondi
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    _allowAutoSearch = true;
                    _logger.LogInfo("Auto-search abilitato dopo inizializzazione");
                };
                delayTimer.Start();

                _logger.LogInfo("Auto-search abilitato dopo inizializzazione");

                // *** NUOVO: Abilita controlli dopo setup completo ***
                SetControlsEnabled(true);

                _logger.LogInfo("MainForm inizializzata con successo");

#if DEBUG
                // Inizializza test di sviluppo
                _devTests = new DevelopmentTests(_logger, this);
                _logger.LogInfo("🧪 Test di sviluppo inizializzati");

                // *** NUOVO: Reset DateTimePicker all'avvio ***
                if (dtpCreatoDA != null) dtpCreatoDA.Checked = false;
                if (dtpCreatoA != null) dtpCreatoA.Checked = false;
                if (dtpCompletatoDA != null) dtpCompletatoDA.Checked = false;
                if (dtpCompletatoA != null) dtpCompletatoA.Checked = false;
                _logger.LogInfo("DateTimePicker inizializzati come non selezionati");
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione MainForm", ex);
                _toastService.ShowError("Errore", $"Errore inizializzazione: {ex.Message}");
            }
        }

        private void ConfigureFormProperties()
        {
            // Form properties (integra con Designer)
            this.Text = "Jira Ticket Manager - Deda Group";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.Icon = SystemIcons.Application; // TODO: Add custom icon

            _logger.LogInfo("Proprietà form configurate");
        }

        private void InitializeDataGrid()
        {
            // Configure DataGridView (usa dgvTickets dal Designer)
            dgvTickets.AllowUserToAddRows = false;
            dgvTickets.AllowUserToDeleteRows = false;
            dgvTickets.ReadOnly = true;
            dgvTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTickets.MultiSelect = false;
            dgvTickets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTickets.RowHeadersVisible = false;

            // Style configuration
            dgvTickets.BackgroundColor = Color.White;
            dgvTickets.BorderStyle = BorderStyle.None;
            dgvTickets.DefaultCellStyle.SelectionBackColor = Color.FromArgb(0, 120, 212);
            dgvTickets.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvTickets.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dgvTickets.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvTickets.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgvTickets.ColumnHeadersHeight = 35;
            dgvTickets.RowTemplate.Height = 30;

            // Event handlers
            dgvTickets.CellDoubleClick += OnTicketDoubleClick;
            dgvTickets.SelectionChanged += OnTicketSelectionChanged;

            _logger.LogInfo("DataGrid configurato");
        }

        private void InitializeFilters()
        {
            try
            {
                _logger.LogInfo("🎛️ Inizializzazione sistema filtri");

                // 🔤 AUTOCOMPLETE: Lista delle ComboBox da configurare
                var filterCombos = new[] { cmbCliente, cmbArea, cmbApplicativo, cmbTipo, cmbStato, cmbPriorita, cmbAssegnatario };

                foreach (var combo in filterCombos)
                {
                    if (combo != null)
                    {
                        // *** CONFIGURAZIONE BASE ***
                        combo.Font = new Font("Segoe UI", 9F);

                        // 🔤 AUTOCOMPLETE: Cambio da DropDownList a DropDown
                        combo.DropDownStyle = ComboBoxStyle.DropDown;

                        // 🔤 AUTOCOMPLETE: Configurazione nativa Windows
                        combo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                        combo.AutoCompleteSource = AutoCompleteSource.ListItems;

                        // *** EVENT HANDLERS ESISTENTI ***
                        // ⚠️ IMPORTANTE: NON aggiungere SelectedIndexChanged per cmbArea
                        // È già gestito da LoadWithAreaDependency() nel ComboBoxManager
                        if (combo != cmbArea)
                        {
                            combo.SelectedIndexChanged += OnFilterChanged;
                            _logger.LogDebug($"✅ Event handler aggiunto per: {combo.Name}");
                        }
                        else
                        {
                            _logger.LogDebug($"⚠️ Event handler ESCLUSO per: {combo.Name} (gestito da dipendenza)");
                        }

                        // 🔤 AUTOCOMPLETE: Log di conferma
                        _logger.LogDebug($"🔤 AutoComplete abilitato per: {combo.Name}");
                    }
                }

                // ⭐ NUOVO: Configure DateTimePicker controls (se esistono)
                InitializeDateFilters();

                // Configure search box
                if (txtSearch != null)
                {
                    txtSearch.KeyPress += OnSearchKeyPress;
                    txtSearch.PlaceholderText = "Cerca per numero ticket o testo...";
                }

                // Configure radio buttons for filter mode
                if (rbBasicMode != null)
                {
                    rbBasicMode.CheckedChanged += (s, e) => {
                        if (rbBasicMode.Checked) SetFilterMode(true);
                    };
                }

                if (rbJQLMode != null)
                {
                    rbJQLMode.CheckedChanged += (s, e) => {
                        if (rbJQLMode.Checked) SetFilterMode(false);
                    };
                }

                // Set initial mode
                SetFilterMode(true); // Start in basic mode

                _logger.LogInfo("✅ Sistema filtri inizializzato con AutoComplete e dipendenza Area → Applicativo");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione filtri", ex);
            }
        }



        /// <summary>
        /// Inizializza controlli DateTimePicker per filtri data
        /// </summary>
        private void InitializeDateFilters()
        {
            try
            {
                // ✅ AGGIORNATO: Usa controlli esistenti dal Designer
                var dateControls = new[] { dtpCreatoDA, dtpCreatoA, dtpCompletatoDA, dtpCompletatoA };
                var dateNames = new[] { "dtpCreatoDA", "dtpCreatoA", "dtpCompletatoDA", "dtpCompletatoA" };

                for (int i = 0; i < dateControls.Length; i++)
                {
                    var dtp = dateControls[i];
                    if (dtp != null)
                    {
                        // Configura DateTimePicker (già configurati nel Designer ma assicuriamoci)
                        dtp.Format = DateTimePickerFormat.Short;
                        dtp.ShowCheckBox = true;
                        dtp.Checked = false; // Non attivo inizialmente

                        // ⚠️ IMPORTANTE: NON aggiungere ValueChanged se vogliamo evitare ricerca automatica
                        // dtp.ValueChanged += OnFilterChanged;

                        _logger.LogDebug($"✅ DateTimePicker configurato: {dateNames[i]}");
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ DateTimePicker non trovato: {dateNames[i]}");
                    }
                }

                _logger.LogInfo("🗓️ DateTimePicker inizializzati correttamente");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione filtri data", ex);
            }
        }


        private void InitializeStatusBar()
        {
            // Configure StatusStrip (usa statusStrip1 dal Designer)
            if (statusStrip1 != null)
            {
                statusStrip1.Items.Clear();

                // Add status labels (compatibile con tslConnection, tslResults, tslLastUpdate dal Designer)
                var tslConnection = new ToolStripStatusLabel("●")
                {
                    ForeColor = Color.Green,
                    ToolTipText = "Connesso a Jira"
                };

                var tslResults = new ToolStripStatusLabel("Pronto")
                {
                    Spring = true,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var tslLastUpdate = new ToolStripStatusLabel($"Ultimo aggiornamento: {DateTime.Now:HH:mm:ss}")
                {
                    TextAlign = ContentAlignment.MiddleRight
                };

                statusStrip1.Items.AddRange(new ToolStripItem[] { tslConnection, tslResults, tslLastUpdate });
            }

            _logger.LogInfo("StatusBar configurata");
        }

        private void InitializeUIManagers()
        {
            // Initialize Sidebar Manager (usa pnlSidebar dal Designer)
            if (pnlSidebar != null && btnSidebarToggle != null)
            {
                var sidebarContent = pnlSidebarContent ?? pnlSidebar;
                _sidebarManager = new SidebarManager(pnlSidebar, sidebarContent, btnSidebarToggle);
                _sidebarManager.QuickFilterClicked += OnQuickFilterClicked;
            }

            // Initialize Toolbar Manager (usa pnlToolbar e relativi controlli dal Designer)  
            if (pnlToolbar != null)
            {
                _toolbarManager = new ToolbarManager(pnlToolbar);
                _toolbarManager.SearchRequested += OnSearchRequested;
                _toolbarManager.RefreshRequested += OnRefreshRequested;
                _toolbarManager.ExportRequested += OnExportRequested;
                _toolbarManager.DashboardRequested += OnDashboardRequested;
                _toolbarManager.AutomationRequested += OnAutomationRequested;
                _toolbarManager.TestRequested += OnTestRequested;
                _toolbarManager.ConfigRequested += OnConfigRequested;
            }

            _logger.LogInfo("UI Managers inizializzati");
        }

        private void SetupEventHandlers()
        {
            try
            {
                _logger.LogInfo("🔄 Setup event handlers...");

                // Form events
                this.Load += OnFormLoad;
                this.Resize += OnFormResize;
                this.FormClosing += OnFormClosing;

                // Filter mode events (usa rbBasicMode, rbJQLMode, rbDate dal Designer)
                if (rbBasicMode != null)
                    rbBasicMode.CheckedChanged += (s, e) => { if (rbBasicMode.Checked && _isInitialized) SetFilterMode(true); };

                if (rbJQLMode != null)
                    rbJQLMode.CheckedChanged += (s, e) => { if (rbJQLMode.Checked && _isInitialized) SetFilterMode(false); };

                if (rbDate != null)
                    rbDate.CheckedChanged += (s, e) => { if (rbDate.Checked && _isInitialized) SetDateFilterMode(); };

                // Button events (usa controlli dal Designer)
                if (btnSearch != null)
                    btnSearch.Click += OnSearchClick;

                if (btnSearchFilter != null)
                    btnSearchFilter.Click += OnSearchClick;

                if (btnRefresh != null)
                    btnRefresh.Click += OnRefreshClick;

                if (btnExportExcel != null)
                    btnExportExcel.Click += OnExportClick;

                if (btnPulisci != null)
                    btnPulisci.Click += OnPulisciClick;

                // Navigation events
                SetupNavigationEvents();

                // 🔧 EVENTI COMBOBOX PER RICERCA AUTOMATICA
                if (cmbCliente != null)
                {
                    cmbCliente.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbCliente.SelectedIndexChanged");
                }

                // 🔧 AGGIUNTO: cmbArea per ricerca automatica
                if (cmbArea != null)
                {
                    cmbArea.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbArea.SelectedIndexChanged");
                    _logger.LogDebug("ℹ️ cmbArea: Ha DOPPIO event handler (dipendenza + ricerca automatica)");
                }

                if (cmbApplicativo != null)
                {
                    cmbApplicativo.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbApplicativo.SelectedIndexChanged");
                }

                if (cmbTipo != null)
                {
                    cmbTipo.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbTipo.SelectedIndexChanged");
                }

                if (cmbStato != null)
                {
                    cmbStato.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbStato.SelectedIndexChanged");
                }

                if (cmbPriorita != null)
                {
                    cmbPriorita.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbPriorita.SelectedIndexChanged");
                }

                if (cmbAssegnatario != null)
                {
                    cmbAssegnatario.SelectedIndexChanged += OnSmartFilterChanged;
                    _logger.LogDebug("✅ Event handler aggiunto: cmbAssegnatario.SelectedIndexChanged");
                }

                // Text search events (CONTROLLI ESISTENTI)
                if (txtSearch != null)
                {
                    txtSearch.KeyPress += OnSearchKeyPress;
                    _logger.LogDebug("✅ Event handler aggiunto: txtSearch.KeyPress");
                }

                if (txtJQLQuery != null)
                {
                    txtJQLQuery.KeyPress += OnJQLKeyPress;
                    _logger.LogDebug("✅ Event handler aggiunto: txtJQLQuery.KeyPress");
                }

                _logger.LogInfo("✅ Tutti gli event handlers configurati");
            }
            catch (Exception ex)
            {
                _logger.LogError("❌ Errore setup event handlers", ex);
                throw;
            }
        }

        private void OnPulisciClick(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo("🧹 Inizio pulizia filtri");

                // *** Reset tutti i DateTimePicker ***
                if (dtpCreatoDA != null)
                {
                    dtpCreatoDA.Checked = false;
                    _logger.LogDebug("DateTimePicker CreatoDA resettato");
                }
                if (dtpCreatoA != null)
                {
                    dtpCreatoA.Checked = false;
                    _logger.LogDebug("DateTimePicker CreatoA resettato");
                }
                if (dtpCompletatoDA != null)
                {
                    dtpCompletatoDA.Checked = false;
                    _logger.LogDebug("DateTimePicker CompletatoDA resettato");
                }
                if (dtpCompletatoA != null)
                {
                    dtpCompletatoA.Checked = false;
                    _logger.LogDebug("DateTimePicker CompletatoA resettato");
                }

                // *** Reset ComboBox SEMPRE (sia in modalità Date che Base) ***
                ResetAllComboBoxes();

                _logger.LogInfo("✅ Filtri puliti completamente");

                // Toast notification
                _toastService?.ShowSuccess("Filtri", "Tutti i filtri sono stati puliti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia filtri", ex);
                _toastService?.ShowError("Errore", "Errore durante pulizia filtri");
            }
        }

        /// <summary>
        /// Reset di tutte le ComboBox ai valori di default
        /// </summary>
        private void ResetAllComboBoxes()
        {
            try
            {
                _logger.LogInfo("🔄 Inizio reset ComboBox");

                var comboBoxes = new[] { cmbCliente, cmbArea, cmbApplicativo, cmbTipo, cmbStato, cmbPriorita, cmbAssegnatario };
                int resetCount = 0;

                // 🔧 CORREZIONE: Disabilita TUTTI gli eventi prima di iniziare
                foreach (var combo in comboBoxes)
                {
                    if (combo != null)
                    {
                        combo.SelectedIndexChanged -= OnSmartFilterChanged;
                    }
                }

                // Reset delle ComboBox
                foreach (var combo in comboBoxes)
                {
                    if (combo != null && combo.Items.Count > 0)
                    {
                        try
                        {
                            combo.SelectedIndex = 0;
                            combo.Refresh();
                            resetCount++;
                            _logger.LogDebug($"{combo.Name} → '{combo.Text}'");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Errore reset {combo.Name}: {ex.Message}");
                        }
                    }
                }

                // 🔧 CORREZIONE: Riabilita eventi DOPO che tutto è stato resettato
                foreach (var combo in comboBoxes)
                {
                    if (combo != null)
                    {
                        combo.SelectedIndexChanged += OnSmartFilterChanged;
                    }
                }

                _logger.LogInfo($"✅ ComboBox resettate: {resetCount}/7");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore reset ComboBox", ex);

                // 🔧 EMERGENZA: Riabilita eventi anche in caso di errore
                var comboBoxes = new[] { cmbCliente, cmbArea, cmbApplicativo, cmbTipo, cmbStato, cmbPriorita, cmbAssegnatario };
                foreach (var combo in comboBoxes)
                {
                    if (combo != null)
                    {
                        try
                        {
                            combo.SelectedIndexChanged += OnSmartFilterChanged;
                        }
                        catch { /* Ignora errori di riabilitazione */ }
                    }
                }
            }
        }


        private void SetDateFilterMode()
        {
            try
            {
                // *** CORREZIONE: Modalità Date È modalità Basic estesa ***
                _isBasicMode = true; // ← CAMBIA da false a true

                // Usa il metodo ShowDateFilters che abbiamo creato
                ShowDateFilters();

                // Update radio button colors
                UpdateFilterModeColors();

                _logger.LogInfo("Modalità Date attivata (considerata Basic Mode esteso)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore modalità Date", ex);
            }
        }

        private void SetupNavigationEvents()
        {
            try
            {
                if (btnFirstPage != null)
                {
                    btnFirstPage.Click += async (s, e) => await GoToPage(1); // ✅ async event handler
                    _logger.LogDebug("Event handler btnFirstPage collegato");
                }

                if (btnPreviousPage != null)
                {
                    btnPreviousPage.Click += async (s, e) => GoToPreviousPage(); // ✅ async void va bene qui
                    _logger.LogDebug("Event handler btnPreviousPage collegato");
                }

                if (btnNextPage != null)
                {
                    btnNextPage.Click += async (s, e) => GoToNextPage(); // ✅ async void va bene qui
                    _logger.LogDebug("Event handler btnNextPage collegato");
                }

                if (btnLastPage != null)
                {
                    btnLastPage.Click += async (s, e) => GoToLastPage(); // ✅ async void va bene qui
                    _logger.LogDebug("Event handler btnLastPage collegato");
                }

                _logger.LogInfo("Event handler navigazione configurati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup navigation events", ex);
            }
        }

        #endregion

        #region Filter Management

        /// <summary>
        /// Ottiene il valore selezionato da una ComboBox (esclude "Tutti..." e valori con "--")
        /// </summary>
        private string GetSelectedComboValue(ComboBox combo)
        {
            if (combo?.SelectedItem == null) return null;

            var value = combo.SelectedItem.ToString();

            // Escludi tutti i valori "Tutti" e con "--"
            if (value.StartsWith("Tutti") ||
                value.StartsWith("--") ||
                value.Contains("Tutti"))
            {
                return null;
            }

            return value;
        }

        private void SetFilterMode(bool isBasicMode)
        {
            try
            {
                _isBasicMode = isBasicMode;

                ShowBasicFilters(isBasicMode);

                if (txtJQLQuery != null)
                    txtJQLQuery.Visible = !isBasicMode;

                // *** POSIZIONE STANDARD anche per modalità JQL ***
                if (!isBasicMode)
                {
                    if (pnlDate != null)
                    {
                        pnlDate.Visible = false;
                        pnlDate.Location = new Point(1497, 35);
                    }

                    if (btnSearchFilter != null)
                    {
                        btnSearchFilter.Visible = true;
                        btnSearchFilter.Location = new Point(1497, 35); // ← POSIZIONE STANDARD
                        _logger.LogDebug("Modalità JQL: btnSearchFilter a posizione standard (1497, 35)");
                    }
                }

                UpdateFilterModeColors();

                _logger.LogInfo($"Modalità filtro cambiata: {(isBasicMode ? "Base" : "JQL")}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore cambio modalità filtro", ex);
            }
        }

        private void ShowBasicFilters(bool show)
        {
            // Show/hide filter ComboBoxes
            var filterControls = new Control[] {
        cmbCliente, cmbArea, cmbApplicativo, cmbTipo,
        cmbStato, cmbPriorita, cmbAssegnatario
    };

            foreach (var control in filterControls)
            {
                if (control != null)
                    control.Visible = show;
            }

            // Nascondi pannello date in modalità Basic
            if (pnlDate != null && show)
            {
                pnlDate.Visible = false;
                pnlDate.Location = new Point(1497, 35); // Posizione originale
            }

            // *** POSIZIONE STANDARD per modalità Basic ***
            if (show && btnSearchFilter != null)
            {
                btnSearchFilter.Visible = true;
                btnSearchFilter.Location = new Point(1497, 35); // ← POSIZIONE STANDARD
                _logger.LogDebug("Modalità Base: btnSearchFilter a posizione standard (1497, 35)");
            }
        }


        /// <summary>
        /// Mostra filtri ComboBox + pannello date insieme
        /// </summary>
        private void ShowDateFilters()
        {
            // Mostra tutti i filtri basic
            ShowBasicFilters(true);

            // Mostra e riposiziona pannello date
            if (pnlDate != null)
            {
                pnlDate.Visible = true;
                pnlDate.Location = new Point(1497, 5); // Pannello in alto
            }

            // Nascondi JQL
            if (txtJQLQuery != null)
                txtJQLQuery.Visible = false;

            // *** UNICO CASO: Posizione speciale per modalità Date ***
            if (btnSearchFilter != null)
            {
                btnSearchFilter.Visible = true;
                btnSearchFilter.Location = new Point(1721, 35); // ← POSIZIONE SPECIALE
                _logger.LogDebug("Modalità Date: pnlDate a (1497, 5), btnSearchFilter a (1721, 35)");
            }
        }



        private void UpdateFilterModeColors()
        {
            Color activeColor = Color.FromArgb(0, 120, 212);
            Color inactiveColor = Color.FromArgb(108, 117, 125);

            if (rbBasicMode != null)
                rbBasicMode.ForeColor = rbBasicMode.Checked ? activeColor : inactiveColor;

            if (rbJQLMode != null)
                rbJQLMode.ForeColor = rbJQLMode.Checked ? activeColor : inactiveColor;

            // *** NUOVO: Support per rbDate ***
            if (rbDate != null)
                rbDate.ForeColor = rbDate.Checked ? activeColor : inactiveColor;
        }

        private Dictionary<string, object> BuildFiltersFromControls()
        {
            var filters = new Dictionary<string, object>();

            try
            {
                if (_isBasicMode)
                {
                    // 🔧 CORREZIONE: Usa ComboBoxManager per ottenere valori ORIGINALI
                    // Build from ComboBox controls (usa controlli dal Designer)
                    AddFilterIfNotEmptyOriginal(filters, "Cliente", cmbCliente);
                    AddFilterIfNotEmptyOriginal(filters, "Area", cmbArea);
                    AddFilterIfNotEmptyOriginal(filters, "Applicativo", cmbApplicativo);
                    AddFilterIfNotEmptyOriginal(filters, "Tipo", cmbTipo);
                    AddFilterIfNotEmptyOriginal(filters, "Stato", cmbStato);
                    AddFilterIfNotEmptyOriginal(filters, "Priority", cmbPriorita);
                    AddFilterIfNotEmptyOriginal(filters, "Assegnatario", cmbAssegnatario);

                    // Add search text (usa ancora il testo diretto)
                    AddFilterIfNotEmpty(filters, "TicketNumber", txtSearch?.Text);

                    // *** NUOVO: Gestione Date ***
                    AddDateFilter(filters, "CreatoDA", dtpCreatoDA);
                    AddDateFilter(filters, "CreatoA", dtpCreatoA);
                    AddDateFilter(filters, "CompletatoDA", dtpCompletatoDA);
                    AddDateFilter(filters, "CompletatoA", dtpCompletatoA);

                    // 🔧 LOGGING DEBUG per vedere valori estratti
                    if (filters.Count > 0)
                    {
                        _logger.LogInfo($"🔍 Filtri costruiti con valori ORIGINALI:");
                        foreach (var filter in filters)
                        {
                            _logger.LogInfo($"   {filter.Key}: '{filter.Value}'");
                        }
                    }
                }

                _activeFilters = filters;
                return filters;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore costruzione filtri", ex);
                return new Dictionary<string, object>();
            }
        }


        // CORREZIONE: SOSTITUIRE il metodo AddFilterIfNotEmpty() in MainForm.cs
        // PROBLEMA: Il placeholder "Cerca ticket..." viene trattato come testo di ricerca

        private void AddFilterIfNotEmpty(Dictionary<string, object> filters, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value == "-- Tutti --" ||
                value.StartsWith("Tutti"))
                return;

            // 🔧 CORREZIONE: Esclude placeholder txtSearch
            if (key == "TicketNumber")
            {
                // Escludi placeholder e testi di default
                if (value.StartsWith("Cerca ticket") ||
                    value.StartsWith("Cerca per numero") ||
                    value.Contains("placeholder"))
                {
                    _logger.LogDebug($"🚫 Placeholder ignorato: '{value}'");
                    return;
                }

                // Verifica se è un numero ticket (formato CC-XXXXX o simile)
                bool isTicketNumber = System.Text.RegularExpressions.Regex.IsMatch(
                    value.Trim(), @"^[A-Z]+-\d+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (isTicketNumber)
                {
                    _logger.LogDebug($"🎯 Numero ticket rilevato: '{value}' - NON aggiunto ai filtri");
                    return; // NON aggiungere ai filtri - verrà gestito separatamente
                }
            }

            filters[key] = value.Trim();
            _logger.LogDebug($"✅ Filtro aggiunto: {key} = '{value.Trim()}'");
        }

        // Estrae valori ORIGINALI dalle ComboBox
        private void AddFilterIfNotEmptyOriginal(Dictionary<string, object> filters, string key, ComboBox comboBox)
        {
            if (comboBox == null)
                return;

            // 🔧 CORREZIONE: NON controllare Enabled - le ComboBox sono disabilitate durante la ricerca!
            // OLD: if (comboBox == null || !comboBox.Enabled)
            //     return;

            try
            {
                // Usa ComboBoxManager per ottenere il valore ORIGINALE
                var originalValue = _comboBoxManager?.GetSelectedOriginalValue(comboBox);

                if (!string.IsNullOrWhiteSpace(originalValue) &&
                    originalValue != "-- Tutti --" &&
                    !originalValue.StartsWith("Tutti") &&
                    !originalValue.StartsWith("--"))
                {
                    filters[key] = originalValue.Trim();
                    _logger.LogDebug($"🔗 Filtro {key}: Display='{comboBox.Text}' → Original='{originalValue}'");
                }
                else
                {
                    _logger.LogDebug($"🚫 Filtro {key} saltato: valore='{originalValue}' (non valido)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore ottenimento valore originale per {key}: {ex.Message}");
                // Fallback al valore display
                AddFilterIfNotEmpty(filters, key, comboBox.Text);
            }
        }


        #region Automatic Filter System

        private System.Windows.Forms.Timer _filterDebounceTimer;
        private readonly int _debounceDelay = 750; // 750ms come concordato

        /// <summary>
        /// Inizializza il sistema di debouncing per i filtri automatici
        /// </summary>
        private void InitializeFilterDebouncing()
        {
            try
            {
                // 🔧 LOGGING per debug
                _logger.LogInfo("🔄 Inizio inizializzazione timer debouncing...");

                // Disponi del timer esistente se presente
                if (_filterDebounceTimer != null)
                {
                    _logger.LogInfo("   Disposing timer esistente...");
                    _filterDebounceTimer.Stop();
                    _filterDebounceTimer.Dispose();
                    _filterDebounceTimer = null;
                }

                // Crea nuovo timer
                _logger.LogInfo($"   Creando nuovo timer con interval {_debounceDelay}ms...");
                _filterDebounceTimer = new System.Windows.Forms.Timer();
                _filterDebounceTimer.Interval = _debounceDelay;
                _filterDebounceTimer.Tick += OnFilterDebounceTimeout;

                _logger.LogInfo($"✅ Timer debouncing inizializzato con successo: Interval={_debounceDelay}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ ERRORE inizializzazione timer debouncing: {ex.Message}", ex);
                // Non fare throw - l'app deve continuare a funzionare
            }
        }


        /// <summary>
        /// Gestisce il timeout del debouncing - esegue la ricerca automatica
        /// </summary>
        private async void OnFilterDebounceTimeout(object sender, EventArgs e)
        {
            try
            {
                _filterDebounceTimer?.Stop();
                _logger.LogInfo("🎯 DEBOUNCE TIMEOUT - Esecuzione ricerca automatica");

                await SearchTicketsAsync();

                _logger.LogInfo("✅ Ricerca automatica completata");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore ricerca automatica debounce: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Gestisce i cambi delle ComboBox con logica condizionale e debouncing
        /// </summary>
        private void OnSmartFilterChanged(object sender, EventArgs e)
        {
            try
            {
                // 🔧 CONTROLLI STATO APPLICAZIONE
                if (!_isInitialized || _isLoading || !_allowAutoSearch)
                {
                    _logger.LogDebug("Filtro ignorato - applicazione non pronta");
                    return;
                }

                // 🔧 CONTROLLI NULL SAFETY
                if (!(sender is ComboBox combo))
                {
                    _logger.LogDebug("OnSmartFilterChanged: sender non è ComboBox");
                    return;
                }

                if (combo == null)
                {
                    _logger.LogDebug("OnSmartFilterChanged: combo è null");
                    return;
                }

                // 🔧 CONTROLLO STATO CARICAMENTO
                if (combo.Text == "Caricamento...")
                {
                    _logger.LogDebug("Filtro ignorato - ComboBox in caricamento");
                    return;
                }

                var comboName = combo.Name ?? "Unknown";

                // 🔧 GESTIONE SICURA DEL VALORE
                var selectedValue = "";
                try
                {
                    selectedValue = combo.SelectedItem?.ToString() ?? combo.Text ?? "";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Errore lettura valore {comboName}: {ex.Message}");
                    selectedValue = "";
                }

                // 🔧 DEBUG DETTAGLIATO
                _logger.LogInfo($"🔄 FILTRO CAMBIATO: {comboName} = '{selectedValue}'");
                _logger.LogInfo($"   SelectedIndex: {combo.SelectedIndex}");
                _logger.LogInfo($"   Stato app: Initialized={_isInitialized}, Loading={_isLoading}, AutoSearch={_allowAutoSearch}");

                // 🔧 VERIFICA SE DEVE ATTIVARE RICERCA AUTOMATICA
                bool shouldTrigger = ShouldTriggerAutoSearch(combo, selectedValue);
                _logger.LogInfo($"   ShouldTriggerAutoSearch: {shouldTrigger}");

                if (shouldTrigger)
                {
                    _logger.LogInfo($"🚀 RICERCA AUTOMATICA ATTIVATA per: {comboName}");

                    // 🔧 VERIFICA TIMER
                    if (_filterDebounceTimer != null)
                    {
                        _logger.LogInfo($"   Timer: Attivo, Interval={_filterDebounceTimer.Interval}ms");
                        _filterDebounceTimer.Stop();
                        _filterDebounceTimer.Start();
                        _logger.LogInfo("   Timer riavviato");
                    }
                    else
                    {
                        _logger.LogError("❌ _filterDebounceTimer è NULL!");
                        // 🔧 RICERCA IMMEDIATA se timer è null
                        Task.Run(async () => {
                            try
                            {
                                _logger.LogInfo("Esecuzione ricerca immediata (timer null)");
                                await SearchTicketsAsync();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Errore ricerca immediata: {ex.Message}", ex);
                            }
                        });
                    }
                }
                else
                {
                    _logger.LogDebug($"🚫 Ricerca automatica NON attivata per: {comboName}");

                    // 🔧 MESSAGGIO SPECIFICO
                    if (comboName != "cmbCliente")
                    {
                        UpdateStatusMessage("Seleziona Cliente per attivare filtri automatici");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ ERRORE in OnSmartFilterChanged: {ex.Message}", ex);
                // Non ri-lanciare l'eccezione per evitare crash dell'UI
            }
        }
        /// <summary>
        /// Determina se il cambio di filtro deve scatenare ricerca automatica
        /// </summary>
        private bool ShouldTriggerAutoSearch(ComboBox changedCombo, string selectedValue)
        {
            try
            {
                // 🔧 BASE: Ignora valori vuoti o di default
                if (string.IsNullOrEmpty(selectedValue) ||
                    selectedValue.StartsWith("--") ||
                    selectedValue.StartsWith("Tutti"))
                {
                    return false;
                }

                // 🔧 CLIENTE: Sempre attiva ricerca automatica
                if (changedCombo?.Name == "cmbCliente")
                {
                    _logger.LogDebug("Auto-search: Cliente cambiato → SÌ");
                    return true;
                }

                // 🔧 LOGICA FLESSIBILE PER ALTRI FILTRI:
                // Attiva ricerca automatica se:
                // 1. C'è già un Cliente selezionato (logica originale) OPPURE
                // 2. Ci sono già altri filtri selezionati (logica nuova)

                // Controlla se Cliente è selezionato
                var clienteValue = cmbCliente?.SelectedItem?.ToString() ?? "";
                var hasClienteSelected = !string.IsNullOrEmpty(clienteValue) &&
                                       !clienteValue.StartsWith("--") &&
                                       !clienteValue.StartsWith("Tutti");

                if (hasClienteSelected)
                {
                    _logger.LogDebug($"Auto-search: {changedCombo?.Name} cambiato + Cliente presente → SÌ");
                    return true;
                }

                // 🔧 NUOVA LOGICA: Controlla se ci sono altri filtri già selezionati
                var otherFiltersSelected = CountSelectedFilters() > 0;

                if (otherFiltersSelected)
                {
                    _logger.LogDebug($"Auto-search: {changedCombo?.Name} cambiato + Altri filtri presenti → SÌ");
                    return true;
                }

                // 🔧 CASO SPECIALE: Se è il primo filtro selezionato e non è Cliente,
                // permettiamo comunque la ricerca automatica per maggiore flessibilità
                _logger.LogDebug($"Auto-search: {changedCombo?.Name} è il primo filtro → SÌ");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore in ShouldTriggerAutoSearch: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Conta quanti filtri sono già selezionati (escluso quello che è appena cambiato)
        /// </summary>
        private int CountSelectedFilters()
        {
            try
            {
                int count = 0;

                // Lista delle ComboBox da controllare
                var combos = new[] { cmbCliente, cmbArea, cmbApplicativo, cmbTipo, cmbStato, cmbPriorita, cmbAssegnatario };

                foreach (var combo in combos)
                {
                    if (combo != null)
                    {
                        var value = combo.SelectedItem?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(value) &&
                            !value.StartsWith("--") &&
                            !value.StartsWith("Tutti"))
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore in CountSelectedFilters: {ex.Message}", ex);
                return 0;
            }
        }


        #endregion


        #endregion

        #region Enable /Disable Controls

        /// <summary>
        /// Abilita/disabilita i controlli di ricerca e filtri
        /// </summary>
        private void SetControlsEnabled(bool enabled)
        {
            try
            {
                // TextBox ricerca
                if (txtSearch != null)
                    txtSearch.Enabled = enabled;

                // Pulsanti ricerca
                if (btnSearch != null)
                    btnSearch.Enabled = enabled;
                if (btnSearchFilter != null)
                    btnSearchFilter.Enabled = enabled;

                // ComboBox filtri
                if (cmbCliente != null) cmbCliente.Enabled = enabled;
                if (cmbArea != null) cmbArea.Enabled = enabled;
                if (cmbApplicativo != null) cmbApplicativo.Enabled = enabled;
                if (cmbTipo != null) cmbTipo.Enabled = enabled;
                if (cmbStato != null) cmbStato.Enabled = enabled;
                if (cmbPriorita != null) cmbPriorita.Enabled = enabled;
                if (cmbAssegnatario != null) cmbAssegnatario.Enabled = enabled;

                // Altri pulsanti
                if (btnRefresh != null) btnRefresh.Enabled = enabled;
                if (btnExportExcel != null) btnExportExcel.Enabled = enabled;

                _logger.LogDebug($"Controlli {(enabled ? "abilitati" : "disabilitati")}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore controllo stati", ex);
            }
        }


        #endregion

        #region Search and Data Loading


        // SOSTITUIRE il metodo ConvertToSearchCriteria() in MainForm.cs

        private JiraSearchCriteria ConvertToSearchCriteria(Dictionary<string, object> filters)
        {
            var criteria = new JiraSearchCriteria
            {
                Project = "CC", // Default project
                Organization = filters.ContainsKey("Cliente") ? filters["Cliente"].ToString() : null,
                Area = filters.ContainsKey("Area") ? filters["Area"].ToString() : null,
                Application = filters.ContainsKey("Applicativo") ? filters["Applicativo"].ToString() : null,
                IssueType = filters.ContainsKey("Tipo") ? filters["Tipo"].ToString() : null,
                Status = filters.ContainsKey("Stato") ? filters["Stato"].ToString() : null,
                Priority = filters.ContainsKey("Priority") ? filters["Priority"].ToString() : null,
                Assignee = filters.ContainsKey("Assegnatario") ? filters["Assegnatario"].ToString() : null,
                FreeText = filters.ContainsKey("TicketNumber") ? filters["TicketNumber"].ToString() : null
            };

            // 🔧 LOGGING DEBUG per verificare i valori nei criteri finali
            _logger.LogInfo($"🎯 JiraSearchCriteria costruiti:");
            _logger.LogInfo($"   Project: {criteria.Project}");
            if (!string.IsNullOrEmpty(criteria.Organization))
                _logger.LogInfo($"   Organization: '{criteria.Organization}'");
            if (!string.IsNullOrEmpty(criteria.Area))
                _logger.LogInfo($"   Area: '{criteria.Area}'");
            if (!string.IsNullOrEmpty(criteria.Application))
                _logger.LogInfo($"   Application: '{criteria.Application}'");
            if (!string.IsNullOrEmpty(criteria.Status))
                _logger.LogInfo($"   Status: '{criteria.Status}'");
            if (!string.IsNullOrEmpty(criteria.Priority))
                _logger.LogInfo($"   Priority: '{criteria.Priority}'");
            if (!string.IsNullOrEmpty(criteria.Assignee))
                _logger.LogInfo($"   Assignee: '{criteria.Assignee}'");
            if (!string.IsNullOrEmpty(criteria.FreeText))
                _logger.LogInfo($"   FreeText: '{criteria.FreeText}'");

            return criteria;
        }

        #endregion


        #region Export

        private async Task ExportToExcelAsync()
        {
            if (_currentData == null || _currentData.Rows.Count == 0)
            {
                _toastService.ShowWarning("Export", "Nessun dato da esportare");
                return;
            }

            try
            {
                ShowProgress("📊 Preparazione export Excel...");

                // Get all data for export (not just current page)
                var allIssues = await _apiService.SearchAllIssuesForExportAsync(_currentJQL);
                var exportData = JiraDataConverter.ConvertToDataTable(allIssues);

                // TODO: Implement Excel export using existing ExportManager
                // var success = ExportManager.ExportDataTable(exportData, ExportManager.ExportFormat.Excel);

                _toastService.ShowSuccess("Export", $"Export completato: {exportData.Rows.Count} record");
                _logger.LogInfo($"Export Excel completato: {exportData.Rows.Count} record");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore export Excel", ex);
                _toastService.ShowError("Export", $"Errore durante l'export: {ex.Message}");
            }
            finally
            {
                HideProgress();
            }
        }

        #endregion

        #region Navigation

        private async Task GoToPage(int pageNumber)
        {
            if (_isLoading) return;

            if (pageNumber < 1) pageNumber = 1;

            var maxPage = (int)Math.Ceiling((double)_totalRecords / _pageSize);
            if (pageNumber > maxPage) pageNumber = maxPage;

            if (pageNumber != _currentPage)
            {
                _currentPage = pageNumber;

                // ✅ USA LA STESSA JQL DEL CARICAMENTO INIZIALE
                try
                {
                    _isLoading = true;
                    ShowProgress($"Caricamento pagina {_currentPage}...");

                    var startAt = (_currentPage - 1) * _pageSize;

                    // ✅ USA _currentJQL INVECE DI RICOSTRUIRE CON FILTRI
                    _logger.LogInfo($"Navigazione pagina {_currentPage} con JQL: {_currentJQL}");

                    var searchResult = await _apiService.SearchIssuesAsync(_currentJQL, startAt, _pageSize);

                    // Converti e aggiorna DataGrid
                    _currentData = JiraDataConverter.ConvertToDataTable(searchResult.Issues, _logger);
                    dgvTickets.DataSource = _currentData;

                    // Aggiorna UI
                    UpdateResultsInfo();
                    UpdateNavigationButtons();

                    _logger.LogInfo($"Navigazione a pagina {_currentPage} completata");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Errore navigazione pagina {_currentPage}", ex);
                    _toastService.ShowError("Errore", $"Errore caricamento pagina: {ex.Message}");
                }
                finally
                {
                    _isLoading = false;
                    HideProgress();
                }
            }
        }


        private async void GoToPreviousPage()
        {
            if (_currentPage > 1)
                await GoToPage(_currentPage - 1); // ✅ Con await
        }

        private async void GoToNextPage()
        {
            var maxPage = (int)Math.Ceiling((double)_totalRecords / _pageSize);
            if (_currentPage < maxPage)
                await GoToPage(_currentPage + 1); // ✅ Con await
        }


        private async void GoToLastPage()
        {
            var maxPage = (int)Math.Ceiling((double)_totalRecords / _pageSize);
            await GoToPage(maxPage); // ✅ Con await
        }

        private void UpdateNavigationButtons()
        {
            var maxPage = (int)Math.Ceiling((double)_totalRecords / _pageSize);

            // Update navigation button states (if they exist in Designer)
            var btnFirstPage = this.Controls.Find("btnFirstPage", true).FirstOrDefault() as Button;
            var btnPreviousPage = this.Controls.Find("btnPreviousPage", true).FirstOrDefault() as Button;
            var btnNextPage = this.Controls.Find("btnNextPage", true).FirstOrDefault() as Button;
            var btnLastPage = this.Controls.Find("btnLastPage", true).FirstOrDefault() as Button;

            if (btnFirstPage != null) btnFirstPage.Enabled = _currentPage > 1;
            if (btnPreviousPage != null) btnPreviousPage.Enabled = _currentPage > 1;
            if (btnNextPage != null) btnNextPage.Enabled = _currentPage < maxPage;
            if (btnLastPage != null) btnLastPage.Enabled = _currentPage < maxPage;
        }

        private void UpdateResultsInfo()
        {
            try
            {
                var startRecord = (_currentPage - 1) * _pageSize + 1;
                var endRecord = Math.Min(_currentPage * _pageSize, _totalRecords);
                var resultsText = $"Record {startRecord}-{endRecord} di {_totalRecords}";

                // Update results label (if exists in Designer)
                var lblResults = this.Controls.Find("lblResults", true).FirstOrDefault() as Label;
                if (lblResults != null)
                    lblResults.Text = resultsText;

                // Update status bar
                if (statusStrip1?.Items.Count > 1)
                    statusStrip1.Items[1].Text = resultsText;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore aggiornamento info risultati: {ex.Message}");
            }
        }

        #endregion

        #region Progress and Status

        private void ShowProgress(string message = "")
        {
            _logger.LogDebug("Mostra progress");

            if (!string.IsNullOrEmpty(message))
                UpdateStatusMessage(message);

            // Disable controls during operation
            if (btnSearch != null) btnSearch.Enabled = false;
            if (btnRefresh != null) btnRefresh.Enabled = false;
            if (btnExportExcel != null) btnExportExcel.Enabled = false;

            Application.DoEvents();
        }

        private void HideProgress()
        {
            _logger.LogDebug("Nascondi progress");

            // Re-enable controls
            if (btnSearch != null) btnSearch.Enabled = true;
            if (btnRefresh != null) btnRefresh.Enabled = true;
            if (btnExportExcel != null) btnExportExcel.Enabled = (_currentData?.Rows.Count > 0);

            UpdateStatusMessage("Pronto");
            Application.DoEvents();
        }

        private void UpdateStatusMessage(string message)
        {
            try
            {
                // Update status bar (usa statusStrip1 dal Designer)
                if (statusStrip1?.Items.Count > 1)
                {
                    statusStrip1.Items[1].Text = $"{DateTime.Now:HH:mm:ss} - {message}";
                }

                // Update last update time
                if (statusStrip1?.Items.Count > 2)
                {
                    statusStrip1.Items[2].Text = $"Ultimo aggiornamento: {DateTime.Now:HH:mm:ss}";
                }

                _logger.LogDebug($"Status: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore aggiornamento status: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        // Form Events
        private async void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                _logger.LogInfo("MainForm Load - Caricamento iniziale");

                // Load initial data
                await LoadInitialDataAsync();

                // Set focus
                if (txtSearch != null)
                    txtSearch.Focus();

                UpdateStatusMessage("Pronto");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore OnFormLoad", ex);
                _toastService.ShowError("Errore", $"Errore caricamento form: {ex.Message}");
            }
        }

        private void OnFormResize(object sender, EventArgs e)
        {
            // Handle form resize if needed
            UpdateNavigationButtons();
        }


        // Search Events (sezione esistente - AGGIUNGERE questo)
        private async void OnSearchFilterClick(object sender, EventArgs e)
        {
            await SearchTicketsAsync();
        }





        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isLoading)
            {
                var result = MessageBox.Show(
                    "Un'operazione è in corso. Vuoi davvero chiudere l'applicazione?",
                    "Conferma chiusura",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _logger.LogInfo("MainForm chiusura");
        }

        // Search Events
        private async void OnSearchClick(object sender, EventArgs e)
        {
            await SearchTicketsAsync();

        }

        private async void OnRefreshClick(object sender, EventArgs e)
        {
            await SearchTicketsAsync();

        }

        private async void OnSearchKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                await SearchTicketsAsync(); // ← Questo dovrebbe già chiamare la logica intelligente
            }
        }

        private async void OnJQLKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter && Control.ModifierKeys == Keys.Control)
            {
                e.Handled = true;
                await SearchTicketsAsync();
            }
        }

        // Filter Events
        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (_isInitialized && !_isLoading)
            {
                UpdateStatusMessage("Filtro modificato - Clicca Cerca per aggiornare");
            }
        }

        // Navigation Events
        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            if (_isInitialized && sender is ComboBox cmb)
            {
                if (int.TryParse(cmb.SelectedItem?.ToString(), out int newPageSize))
                {
                    _pageSize = newPageSize;
                    _currentPage = 1;
                    _ = SearchTicketsAsync();
                }
            }
        }

        // Grid Events
        // <summary>
        /// Gestisce il double click su una cella del DataGridView
        /// </summary>
        private async void OnTicketDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex < 0) return; // Click su header

                // Estrai ticket key dalla riga
                var ticketKey = GetTicketKeyFromRow(e.RowIndex);
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("Ticket key non trovato nella riga selezionata");
                    return;
                }

                _logger.LogInfo($"Double-click su ticket: {ticketKey}");

                // Apri form dettaglio
                await OpenTicketDetailAsync(ticketKey);
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione double-click ticket", ex);
                _toastService?.ShowError("Errore", $"Errore apertura dettaglio: {ex.Message}");
            }
        }

        /// <summary>
        /// Estrae la chiave del ticket dalla riga del DataGridView
        /// </summary>
        private string GetTicketKeyFromRow(int rowIndex)
        {
            try
            {
                if (rowIndex < 0 || rowIndex >= dgvTickets.Rows.Count)
                    return null;

                var row = dgvTickets.Rows[rowIndex];

                // Metodo 1: DataBoundItem (più affidabile)
                if (row.DataBoundItem is DataRowView dataRow)
                {
                    var key = dataRow["Key"]?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        _logger.LogDebug($"Ticket key estratto da DataBoundItem: {key}");
                        return key;
                    }
                }

                // Metodo 2: Cerca colonna "Key"
                if (dgvTickets.Columns.Contains("Key"))
                {
                    var key = row.Cells["Key"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        _logger.LogDebug($"Ticket key estratto da colonna Key: {key}");
                        return key;
                    }
                }

                // Metodo 3: Prima colonna se contiene formato ticket
                var firstCellValue = row.Cells[0]?.Value?.ToString();
                if (!string.IsNullOrEmpty(firstCellValue) && firstCellValue.Contains("-"))
                {
                    _logger.LogDebug($"Ticket key estratto da prima colonna: {firstCellValue}");
                    return firstCellValue;
                }

                _logger.LogWarning($"Impossibile estrarre ticket key dalla riga {rowIndex}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore estrazione ticket key dalla riga {rowIndex}", ex);
                return null;
            }
        }

        /// <summary>
        /// Apre la form di dettaglio ticket
        /// </summary>
        private async Task OpenTicketDetailAsync(string ticketKey)
        {
            try
            {
                if (string.IsNullOrEmpty(ticketKey))
                {
                    _logger.LogWarning("Tentativo apertura dettaglio con ticket key vuoto");
                    return;
                }

                _logger.LogInfo($"Apertura form dettaglio per ticket: {ticketKey}");

                // Crea nuova istanza della form
                var detailForm = new TicketDetailForm();

                // Configura la form
                ConfigureDetailForm(detailForm, ticketKey);

                // Mostra la form (non bloccante)
                detailForm.Show();

                // Carica i dati del ticket (asincrono)
                await detailForm.LoadTicketAsync(ticketKey);

                _logger.LogInfo($"Form dettaglio aperta per ticket: {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore apertura form dettaglio per {ticketKey}", ex);

                // Mostra errore all'utente
                _toastService?.ShowError(
                    "Errore Apertura Dettaglio",
                    $"Impossibile aprire il dettaglio del ticket {ticketKey}.\n\nErrore: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// Centra la form sullo schermo
        /// </summary>
        private void CenterFormOnScreen(Form form)
        {
            try
            {
                // Ottieni lo schermo che contiene la MainForm
                var screen = Screen.FromControl(this);
                var workingArea = screen.WorkingArea;

                // Calcola posizione centrata
                var x = workingArea.X + (workingArea.Width - form.Width) / 2;
                var y = workingArea.Y + (workingArea.Height - form.Height) / 2;

                // Assicurati che sia dentro i bounds dello schermo
                x = Math.Max(workingArea.X, Math.Min(x, workingArea.Right - form.Width));
                y = Math.Max(workingArea.Y, Math.Min(y, workingArea.Bottom - form.Height));

                // Imposta posizione
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(x, y);

                _logger.LogDebug($"Form centrata a posizione: {x}, {y}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore centratura form", ex);

                // Fallback: centra sullo schermo principale
                form.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        /// <summary>
        /// Apre dettaglio ticket da tastiera (Enter su riga selezionata)
        /// </summary>
        private async void OnDataGridKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter && dgvTickets.CurrentRow != null)
                {
                    var ticketKey = GetTicketKeyFromRow(dgvTickets.CurrentRow.Index);
                    if (!string.IsNullOrEmpty(ticketKey))
                    {
                        e.Handled = true; // Previeni comportamento default
                        await OpenTicketDetailAsync(ticketKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione tasto Enter su DataGrid", ex);
            }
        }

        /// <summary>
        /// Setup event handlers aggiuntivi (OPZIONALE - chiama in InitializeDataGrid)
        /// </summary>
        private void SetupAdditionalDataGridEvents()
        {
            try
            {
                // Aggiungi event handler per Enter key (opzionale)
                dgvTickets.KeyDown += OnDataGridKeyDown;

                _logger.LogDebug("Event handlers aggiuntivi DataGrid configurati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup event handlers DataGrid", ex);
            }
        }


        /// <summary>
        /// Crea context menu per click destro su riga (OPZIONALE)
        /// </summary>
        private void SetupDataGridContextMenu()
        {
            try
            {
                var contextMenu = new ContextMenuStrip();

                // Menu item "Apri Dettaglio"
                var openDetailItem = new ToolStripMenuItem("Apri Dettaglio", null, async (s, e) => {
                    if (dgvTickets.CurrentRow != null)
                    {
                        var ticketKey = GetTicketKeyFromRow(dgvTickets.CurrentRow.Index);
                        if (!string.IsNullOrEmpty(ticketKey))
                        {
                            await OpenTicketDetailAsync(ticketKey);
                        }
                    }
                });

                // Menu item "Copia Ticket Key"
                var copyKeyItem = new ToolStripMenuItem("Copia Numero Ticket", null, (s, e) => {
                    if (dgvTickets.CurrentRow != null)
                    {
                        var ticketKey = GetTicketKeyFromRow(dgvTickets.CurrentRow.Index);
                        if (!string.IsNullOrEmpty(ticketKey))
                        {
                            Clipboard.SetText(ticketKey);
                            _toastService?.ShowSuccess("Copiato", $"Numero ticket {ticketKey} copiato negli appunti");
                        }
                    }
                });

                contextMenu.Items.AddRange(new ToolStripItem[] { openDetailItem, copyKeyItem });
                dgvTickets.ContextMenuStrip = contextMenu;

                _logger.LogDebug("Context menu DataGrid configurato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore setup context menu DataGrid", ex);
            }
        }


        // <summary>
        /// Configura la form di dettaglio prima dell'apertura
        /// </summary>
        private void ConfigureDetailForm(TicketDetailForm detailForm, string ticketKey)
        {
            try
            {
                // Titolo della finestra
                detailForm.Text = $"Dettaglio Ticket - {ticketKey}";

                // Dimensioni ottimali per desktop FullHD
                detailForm.Size = new Size(1620, 1055);
                detailForm.MinimumSize = new Size(1400, 900);

                // Centra sullo schermo
                CenterFormOnScreen(detailForm);

                // Icona (se disponibile)
                if (this.Icon != null)
                {
                    detailForm.Icon = this.Icon;
                }

                _logger.LogDebug($"Form dettaglio configurata per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore configurazione form dettaglio per {ticketKey}", ex);
            }
        }


        private void OnTicketSelectionChanged(object sender, EventArgs e)
        {
            // Handle ticket selection change if needed
            var selectedCount = dgvTickets.SelectedRows.Count;
            _logger.LogDebug($"Ticket selezionati: {selectedCount}");
        }

        // Export Events
        private async void OnExportClick(object sender, EventArgs e)
        {
            await ExportToExcelAsync();
        }

        // UI Manager Events
        private void OnQuickFilterClicked(object sender, string filterType)
        {
            _logger.LogInfo($"Quick filter selezionato: {filterType}");
            // TODO: Apply quick filter
        }

        private async void OnSearchRequested(object sender, SearchEventArgs e)
        {
            if (txtSearch != null)
                txtSearch.Text = e.SearchTerm;
            await SearchTicketsAsync();
        }

        private async void OnRefreshRequested(object sender, EventArgs e)
        {
            await SearchTicketsAsync();
        }

        private async void OnExportRequested(object sender, EventArgs e)
        {
            await ExportToExcelAsync();
        }

        private void OnDashboardRequested(object sender, EventArgs e)
        {
            _logger.LogInfo("Dashboard richiesto");
            _toastService.ShowInfo("Dashboard", "Funzionalità in sviluppo");
        }

        private void OnAutomationRequested(object sender, EventArgs e)
        {
            _logger.LogInfo("Automazione richiesta");
            _toastService.ShowInfo("Automazione", "Funzionalità in sviluppo");
        }

        private void OnTestRequested(object sender, EventArgs e)
        {
            _logger.LogInfo("Test richiesto");
            _toastService.ShowInfo("Test", "Funzionalità in sviluppo");
        }

        private void OnConfigRequested(object sender, EventArgs e)
        {
            _logger.LogInfo("Configurazione richiesta");
            _toastService.ShowInfo("Configurazione", "Funzionalità in sviluppo");
        }

        #endregion

        #region Helper Methods

        private async Task LoadInitialDataAsync()
        {
            var operationId = "initial_data_load";

            try
            {
                _logger.LogInfo("🚀 Avvio caricamento dati iniziali");

                // ✅ AGGIORNATO: 7 step totali (1 dipendenza Area→Applicativo + 4 combobox singole + 1 ricerca)
                _progressService.StartOperation(operationId, "Caricamento dati iniziali", totalSteps: 7);

                // Crea progress reporter che si integra con il ProgressService
                var progress = new Progress<string>(message =>
                    _progressService.UpdateMessage(operationId, message));

                // ⭐ NUOVO: Step 1-2: Carica ComboBox Area e Applicativo con DIPENDENZA
                _progressService.UpdateProgress(operationId, 1, "Caricamento aree e applicativi...");
                _logger.LogInfo("🔗 Caricamento ComboBox Area → Applicativo con dipendenza");
                await _comboBoxManager.LoadWithAreaDependency(
                    cmbArea,
                    cmbApplicativo,
                    "-- Tutte le Aree --",
                    "-- Seleziona un'area --",
                    progress);

                // Step 3: Carica ComboBox Cliente (operazione più lunga)
                _progressService.UpdateProgress(operationId, 3, "Caricamento organizzazioni...");
                _logger.LogInfo("🧪 Caricamento ComboBox Cliente");
                await _comboBoxManager.LoadAsync(cmbCliente, JiraFieldType.Organization, "-- Tutti i Clienti --", progress);

                // Step 4: Carica ComboBox Stato
                _progressService.UpdateProgress(operationId, 4, "Caricamento stati...");
                _logger.LogInfo("🧪 Caricamento ComboBox Stato");
                await _comboBoxManager.LoadAsync(cmbStato, JiraFieldType.Status, "-- Tutti gli Stati --", progress);

                // Step 5: Carica ComboBox Priorità
                _progressService.UpdateProgress(operationId, 5, "Caricamento priorità...");
                _logger.LogInfo("🧪 Caricamento ComboBox Priorità");
                await _comboBoxManager.LoadAsync(cmbPriorita, JiraFieldType.Priority, "-- Tutte le Priorità --", progress);

                // Step 6: Carica ComboBox Tipo
                _progressService.UpdateProgress(operationId, 6, "Caricamento tipi ticket...");
                _logger.LogInfo("🧪 Caricamento ComboBox Tipo");
                await _comboBoxManager.LoadAsync(cmbTipo, JiraFieldType.IssueType, "-- Tutti i Tipi --", progress);

                // Step 7: Carica ComboBox Assegnatario
                _progressService.UpdateProgress(operationId, 7, "Caricamento assegnatari...");
                _logger.LogInfo("🧪 Caricamento ComboBox Assegnatario");
                await _comboBoxManager.LoadAsync(cmbAssegnatario, JiraFieldType.Assignee, "-- Tutti gli Assegnatari --", progress);

                // Resto del caricamento rimane come nel codice esistente...
                _progressService.UpdateProgress(operationId, 7, "Caricamento ticket iniziali...");
                _logger.LogInfo("🔍 Caricamento ticket iniziali");

                // Resetta filtri e usa JQL di base
                _activeFilters.Clear();
                _currentPage = 1;

                // JQL semplice senza filtri delle combobox
                _currentJQL = "project = CC AND statuscategory = \"In Progress\" ORDER BY updated DESC";

                _logger.LogInfo($"🔍 JQL iniziale: {_currentJQL}");

                // Esegui ricerca iniziale (usa il codice esistente)
                var startAt = (_currentPage - 1) * _pageSize;
                var searchResult = await _apiService.SearchIssuesAsync(_currentJQL, startAt, _pageSize);

                if (searchResult?.Issues != null)
                {
                    // ✅ CORRETTO: ConvertToDataTable accetta JArray e logger
                    _currentData = JiraDataConverter.ConvertToDataTable(searchResult.Issues, _logger);
                    _totalRecords = searchResult.Total;

                    dgvTickets.DataSource = _currentData;
                    UpdateResultsInfo();
                    UpdateNavigationButtons();
                }

                _progressService.CompleteOperation(operationId, "Caricamento completato");
                _logger.LogInfo("✅ Caricamento dati iniziali completato");

                // Mostra toast di successo
                _toastService.ShowSuccess("Sistema pronto", $"Caricati {_totalRecords} ticket");
            }
            catch (Exception ex)
            {
                _progressService.CompleteOperation(operationId, "Errore caricamento");
                _logger.LogError("Errore caricamento dati iniziali", ex);
                _toastService.ShowError("Errore", "Errore durante il caricamento dei dati");
                throw;
            }
        }


        /// <summary>
        /// Crea un DataTable vuoto per evitare errori nella DataGridView
        /// </summary>
        private void CreateEmptyDataTable()
        {
            try
            {
                _logger.LogInfo("📋 === CREAZIONE DATATABLE VUOTO ===");

                // Crea JArray vuoto per JiraDataConverter
                var emptyJArray = new Newtonsoft.Json.Linq.JArray();
                _currentData = JiraDataConverter.ConvertToDataTable(emptyJArray, _logger);

                // Aggiungi riga placeholder se necessario
                if (_currentData.Rows.Count == 0)
                {
                    var row = _currentData.NewRow();
                    row["Key"] = "Nessun ticket trovato";
                    row["Descrizione"] = "Nessun risultato per i criteri di ricerca correnti";
                    row["Stato"] = "";
                    row["Assegnatario"] = "";
                    row["Area"] = "";
                    row["Applicativo"] = "";
                    row["Cliente"] = "";
                    row["Creato"] = "";
                    row["Completato"] = "";
                    _currentData.Rows.Add(row);
                }

                dgvTickets.DataSource = _currentData;
                ConfigureDataGridColumns();
                _logger.LogInfo("✅ DataTable vuoto creato con JiraDataConverter");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ ERRORE creazione DataTable vuoto: {ex.Message}");

                // Fallback
                try
                {
                    dgvTickets.DataSource = null;
                    _logger.LogInfo("🔄 DataGridView pulita come fallback");
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError($"❌ ERRORE cleanup: {cleanupEx.Message}");
                }
            }
        }

        private void OpenTicketDetails(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Apertura dettagli ticket: {ticketKey}");

                // TODO: Open ticket details form
                // var detailsForm = new FrmDettaglio(ticketKey);
                // detailsForm.Show();

                _toastService.ShowInfo("Dettagli", $"Apertura dettagli per {ticketKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore apertura dettagli {ticketKey}", ex);
                _toastService.ShowError("Errore", $"Errore apertura dettagli: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiunge un filtro data se il DateTimePicker è abilitato e selezionato
        /// </summary>
        private void AddDateFilter(Dictionary<string, object> filters, string key, DateTimePicker dateTimePicker)
{
    if (dateTimePicker == null || !dateTimePicker.Checked)
        return;

    try
    {
        var dateValue = dateTimePicker.Value.ToString("yyyy-MM-dd");
        filters[key] = dateValue;
        _logger.LogDebug($"📅 Filtro data {key}: {dateValue}");
    }
    catch (Exception ex)
    {
        _logger.LogWarning($"Errore ottenimento data per {key}: {ex.Message}");
    }
}

        #endregion



        #region DatagridView Confing

        /// <summary>
        /// Configura le colonne del DataGridView per mostrare solo quelle richieste
        /// </summary>
        private void ConfigureDataGridColumns()
        {
            try
            {
                if (dgvTickets.DataSource == null) return;

                // Disabilita temporaneamente AutoSizeColumnsMode
                var originalMode = dgvTickets.AutoSizeColumnsMode;
                dgvTickets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

                // Prima nascondi tutte le colonne
                foreach (DataGridViewColumn column in dgvTickets.Columns)
                {
                    column.Visible = false;
                }

                // Configura colonne
                var visibleColumns = new Dictionary<string, (string DisplayName, int Width)>
       {
           { "Key", ("Key", 80) },
           { "Descrizione", ("Descrizione", 300) },  
           { "Stato", ("Stato", 120) },
           { "Cliente", ("Cliente", 200) },
           { "Assegnatario", ("Assegnatario", 150) },
           { "Area", ("Area", 200) },
           { "Applicativo", ("Applicativo", 200) },
           { "Creato", ("Creato", 120) },
           { "Completato", ("Completato", 120) }
       };

                int displayIndex = 0;
                foreach (var col in visibleColumns)
                {
                    if (dgvTickets.Columns.Contains(col.Key))
                    {
                        var column = dgvTickets.Columns[col.Key];
                        column.Visible = true;
                        column.HeaderText = col.Value.DisplayName;
                        column.FillWeight = col.Value.Width;
                        column.DisplayIndex = displayIndex++;
                    }
                }

                // Riabilita AutoSizeColumnsMode
                dgvTickets.AutoSizeColumnsMode = originalMode;

                _logger.LogInfo($"📊 Configurate {visibleColumns.Count} colonne visibili");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore configurazione colonne DataGrid", ex);
            }
        }

        #endregion

#if DEBUG
        /// <summary>
        /// Esegue i test di sviluppo - F9 per test rapido
        /// </summary>
        public async Task RunDevTestsAsync()
        {
            try
            {
                if (_devTests != null)
                {
                    await _devTests.RunAllAsync();
                }
                else
                {
                    _logger.LogWarning("🧪 Test di sviluppo non inizializzati");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore esecuzione test sviluppo", ex);
            }
        }

        /// <summary>
        /// Gestisce tasti rapidi per test - F9 per eseguire test
        /// </summary>
#if DEBUG
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // F9 - Esegui tutti i test di sviluppo
            if (keyData == Keys.F9)
            {
                _ = RunDevTestsAsync();
                return true;
            }

            // F10 - Analisi JSON ticket selezionato
            if (keyData == Keys.F10)
            {
                _ = _devTests?.TestRealTicketJSONAnalysis();
                return true;
            }

            // *** NUOVO: F11 - Debug campo Reporter ***
            if (keyData == Keys.F11)
            {
                _ = _devTests?.TestReporterFieldOnMultipleTickets();
                return true;
            }

            // F12 - Genera mappatura Area-Applicativo
            //if (keyData == Keys.F12)
            //{
            //    GenerateAreaApplicativoMapping();
            //    return true;
            //}

            // F12 - Test API dedicata Cliente Partner  
            if (keyData == Keys.F12)
            {
                _ = _devTests?.TestClientePartnerDedicatedAPI();
                return true;
            }



            return base.ProcessCmdKey(ref msg, keyData);
        }
#endif


#endif


#if DEBUG
        /// <summary>
        /// Genera file mappatura Area-Applicativo - F12
        /// </summary>
        private void GenerateAreaApplicativoMapping()
        {
            try
            {
                _logger.LogInfo("🔧 Generazione mappatura Area-Applicativo...");

                // Dati completi dal tuo log (sostituisci con il log completo)
                var logData = @"[JiraDataService] 🔍 Custom field customfield_10114 valori: Civilia - Fattura Elettronica -> WebApp, Civilia - GeoNext -> API PDND, Civilia - GeoNext -> CDU, Civilia - GeoNext -> CEN, Civilia - GeoNext -> Data Catalogue, Civilia - GeoNext -> Editor Web, Civilia - GeoNext -> Metadati, Civilia Next - Area Affari Generali -> Albo Pretorio, Civilia Next - Area Affari Generali -> Amministrazione Trasparente, Civilia Next - Area Affari Generali -> Atti Formali, Civilia Next - Area Affari Generali -> Consultazione Commissioni, Civilia Next - Area Affari Generali -> Procedimenti Amministrativi, Civilia Next - Area Affari Generali -> Procedimenti Web, Civilia Next - Area Affari Generali -> Protocollo Informatico, Civilia Next - Area Affari Generali -> Ufficio Legale, Civilia Next - Area Appalti e Contratti -> BDAP, Civilia Next - Area Appalti e Contratti -> CNED, Civilia Next - Area Appalti e Contratti -> E-Procurement, Civilia Next - Area Appalti e Contratti -> Gestione Contratti, Civilia Next - Area Appalti e Contratti -> Gestione Manutenzioni, Civilia Next - Area Appalti e Contratti -> Opere, Civilia Next - Area Appalti e Contratti -> Progettazione e Direzioni Lavori, Civilia Next - Area Appalti e Contratti -> Programmazione, Civilia Next - Area Appalti e Contratti -> SOA, Civilia Next - Area Demografia -> Anagrafe, Civilia Next - Area Demografia -> Elettorale, Civilia Next - Area Demografia -> Gestione Cimiteriale, Civilia Next - Area Demografia -> Risultati Elettorali, Civilia Next - Area Demografia -> Stato Civile, Civilia Next - Area Gare e Contratti -> Gare e Contratti, Civilia Next - Area Gestione Entrate -> Accertamento Imposta di Soggiorno, Civilia Next - Area Gestione Entrate -> Banche Dati, Civilia Next - Area Gestione Entrate -> Cup, Civilia Next - Area Gestione Entrate -> Icp, Civilia Next - Area Gestione Entrate -> Imposta di Soggiorno, Civilia Next - Area Gestione Entrate -> Osap, Civilia Next - Area Gestione Entrate -> Simulatore, Civilia Next - Area Gestione Entrate -> Tributi Maggiori, Civilia Next - Area Gestione Entrate -> Tributi Vari, Civilia Next - Area Risorse Economiche -> Centri Responsabilità, Civilia Next - Area Risorse Economiche -> Contabilità, Civilia Next - Area Risorse Economiche -> Controllo di Gestione, Civilia Next - Area Risorse Economiche -> Economato, Civilia Next - Area Risorse Economiche -> Gestione Performance, Civilia Next - Area Risorse Economiche -> Mutui, Civilia Next - Area Risorse Economiche -> Ordini e Magazzino, Civilia Next - Area Risorse Economiche -> Patrimonio, Civilia Next - Area Risorse Economiche -> Relazioni di Mandato, Civilia Next - Area Risorse Umane -> Buoni Pasto, Civilia Next - Area Risorse Umane -> Dotazione Organica, Civilia Next - Area Risorse Umane -> Gestione Economica, Civilia Next - Area Risorse Umane -> Gestione Giuridica, Civilia Next - Area Risorse Umane -> PIAO, Civilia Next - Area Risorse Umane -> Portale del Dipendente, Civilia Next - Area Risorse Umane -> Rilevazione Presenze, Civilia Next - Area Tecnica -> Catasto Termico, Civilia Next - Area Tecnica -> Commercio, Civilia Next - Area Tecnica -> Geo Next, Civilia Next - Area Tecnica -> Mercati e Fiere, Civilia Next - Area Tecnica -> Pratiche Edilizie, Civilia Next - Area Tecnica -> Pratiche Edilizie (SUE), Civilia Next - Area Tecnica -> SUAP, Civilia Next - Area Tecnica -> Toponomastica, Civilia Next - Dup (Open) -> Dup, Civilia Next - Servizi On-Line -> AAGG - Alberatura Trasparenza, Civilia Next - Servizi On-Line -> AAGG - Albo Pretorio OnLine, Civilia Next - Servizi On-Line -> AAGG - Amministrazione Aperta, Civilia Next - Servizi On-Line -> AAGG - Avanzamento Pratiche, Civilia Next - Servizi On-Line -> Comunicazione, Civilia Next - Servizi On-Line -> Demografici OnLine, Civilia Next - Servizi On-Line -> Istanze - Caricamento Pratiche, Civilia Next - Servizi On-Line -> Istanze - Modulistica OnLine, Civilia Next - Servizi On-Line -> NewsLetter, Civilia Next - Servizi On-Line -> Portale, Civilia Next - Servizi On-Line -> Servizi - Appuntamenti OnLine, Civilia Next - Servizi On-Line -> Servizi - IMU/TASI OnLine, Civilia Next - Servizi On-Line -> Servizi - Pagamenti OnLine, Civilia Next - Servizi On-Line -> Sito Istituzionale, Civilia Next - Welfare e Scuola -> Gestione Strutture Sportive, Civilia Next - Welfare e Scuola -> Servizi a Domanda Individuale, Civilia Next - Welfare e Scuola -> Servizi Sociali, Civilia Next -> GeoNext, Civilia Next -> Muse, Civilia Next Area Comune -> Archivio Generale, Civilia Next Area Comune -> Comunicazioni Istituzionali, Civilia Next Area Comune -> Gestione Individui, Civilia Next Area Comune -> Next BI, Civilia Next Area Comune -> Organigramma, Civilia Next Area Comune -> Platform, Civilia Next Area Comune -> Scrivania Virtuale, Civlia Web -> Area Affari Generali, Civlia Web -> Area Contabilità PMI, Civlia Web -> Controllo di Gestione, Customer Care - Sistema di Ticketing, Folium -> Affari Generali, Metadatamanager -> MDMGR, Sistema Informativo Territoriale -> C2C, Sistema Informativo Territoriale -> Editor PRG, Base, Reti, Sistema Informativo Territoriale -> GeoPat, Sistema Informativo Territoriale -> GeoView.Net, Sistema Informativo Territoriale -> GPCad Pro, Sistema Informativo Territoriale -> Load Catasto, Sistema Informativo Territoriale -> Load Siatel, Sistema Informativo Territoriale -> Metasfera, Sistema Informativo Territoriale -> NewSed.Net, Sistema Informativo Territoriale -> Normalizzatore, Sistema Informativo Territoriale -> SincroCat, Sistema Informativo Territoriale -> SIT, Sistema Informativo Territoriale -> Vesta";

                AreaApplicativoMappingGenerator.GenerateMappingFile(logData);

                _logger.LogInfo("✅ Mappatura generata con successo");
                _toastService?.ShowSuccess("Mappatura", "File Area-Applicativo generato!");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore generazione mappatura", ex);
                _toastService?.ShowError("Errore", $"Errore generazione mappatura: {ex.Message}");
            }
        }
#endif

#if DEBUG
        /// <summary>
        /// Debug: Verifica stato delle ComboBox prima della ricerca
        /// </summary>
        private void DebugComboBoxStates()
        {
            _logger.LogInfo($"🔍 === DEBUG STATO COMBOBOX ===");

            DebugSingleComboBox("Cliente", cmbCliente);
            DebugSingleComboBox("Area", cmbArea);
            DebugSingleComboBox("Applicativo", cmbApplicativo);
            DebugSingleComboBox("Tipo", cmbTipo);
            DebugSingleComboBox("Stato", cmbStato);
            DebugSingleComboBox("Priority", cmbPriorita);
            DebugSingleComboBox("Assegnatario", cmbAssegnatario);

            _logger.LogInfo($"🔍 === FINE DEBUG COMBOBOX ===");
        }

        private void DebugSingleComboBox(string name, ComboBox comboBox)
        {
            if (comboBox == null)
            {
                _logger.LogInfo($"🔍 {name}: NULL");
                return;
            }

            var enabled = comboBox.Enabled;
            var displayValue = comboBox.Text;
            var selectedIndex = comboBox.SelectedIndex;
            var itemCount = comboBox.Items.Count;

            string originalValue = "N/A";
            try
            {
                originalValue = _comboBoxManager?.GetSelectedOriginalValue(comboBox) ?? "NULL";
            }
            catch (Exception ex)
            {
                originalValue = $"ERROR: {ex.Message}";
            }

            _logger.LogInfo($"🔍 {name}: Enabled={enabled}, Items={itemCount}, Index={selectedIndex}");
            _logger.LogInfo($"    Display: '{displayValue}'");
            _logger.LogInfo($"    Original: '{originalValue}'");
        }
#endif


        


    }
}