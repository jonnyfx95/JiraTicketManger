using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JiraTicketManager.UI
{
    public class ToolbarManager
    {
        #region Events
        public event EventHandler<SearchEventArgs> SearchRequested;
        public event EventHandler RefreshRequested;
        public event EventHandler<ExportEventArgs> ExportRequested;
        public event EventHandler DashboardRequested;
        public event EventHandler AutomationRequested;
        public event EventHandler TestRequested;
        public event EventHandler ConfigRequested;
        #endregion

        #region Fields
        private readonly Panel _toolbarPanel;
        private TextBox _searchTextBox;
        private Button _searchButton;
        private Button _refreshButton;
        private Button _exportButton;
        private Button _dashboardButton;
        private Button _automationButton;
        private Button _testButton;
        private Button _configButton;

        private readonly List<string> _searchHistory;
        private bool _isSearchActive = false;
        private DateTime _lastSearchTime = DateTime.MinValue;
        private const int SEARCH_DEBOUNCE_MS = 500;
        #endregion

        #region Constructor
        public ToolbarManager(Panel toolbarPanel)
        {
            _toolbarPanel = toolbarPanel ?? throw new ArgumentNullException(nameof(toolbarPanel));
            _searchHistory = new List<string>();

            // Trova i controlli nel panel toolbar
            FindToolbarControls();

            // Configura la toolbar
            InitializeToolbar();
            SetupEventHandlers();
            ConfigureSearchFeatures();
        }
        #endregion

        #region Initialization
        private void FindToolbarControls()
        {
            // Cerca i controlli nella toolbar tramite ricerca dinamica
            _searchTextBox = FindControl<TextBox>("txtSearch");
            _searchButton = FindControl<Button>("btnSearch");
            _refreshButton = FindControl<Button>("btnRefresh");
            _exportButton = FindControl<Button>("btnExportExcel");
            _dashboardButton = FindControl<Button>("btnDashboard");
            _automationButton = FindControl<Button>("btnJiraAutomation");
            _testButton = FindControl<Button>("btnTest");
            _configButton = FindControl<Button>("btnConfig");
        }

        private T FindControl<T>(string name) where T : Control
        {
            return _toolbarPanel.Controls.Find(name, true).FirstOrDefault() as T;
        }

        private void InitializeToolbar()
        {
            // Configura aspetto iniziale dei pulsanti
            ConfigureActionButtons();
            ConfigureSearchSection();
        }

        private void ConfigureActionButtons()
        {
            // Configura ogni pulsante con i suoi colori e stati
            if (_exportButton != null)
                ConfigureButton(_exportButton, Color.FromArgb(22, 163, 74), "Export dei ticket selezionati in Excel");

            if (_dashboardButton != null)
                ConfigureButton(_dashboardButton, Color.FromArgb(8, 145, 178), "Apri dashboard analytics");

            if (_automationButton != null)
                ConfigureButton(_automationButton, Color.FromArgb(124, 58, 237), "Gestione automazioni Jira");

            if (_testButton != null)
                ConfigureButton(_testButton, Color.FromArgb(217, 119, 6), "Strumenti di test e debug");

            if (_configButton != null)
                ConfigureButton(_configButton, Color.FromArgb(108, 117, 125), "Configurazioni sistema");

            if (_refreshButton != null)
                ConfigureButton(_refreshButton, Color.FromArgb(0, 120, 212), "Aggiorna dati");

            if (_searchButton != null)
                ConfigureButton(_searchButton, Color.FromArgb(0, 120, 212), "Cerca nei ticket");
        }

        private void ConfigureButton(Button button, Color baseColor, string tooltip)
        {
            // Configurazione base
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = baseColor;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.Font = new Font("Segoe UI", 8F, FontStyle.Bold);

            // Tooltip
            var toolTip = new ToolTip();
            toolTip.SetToolTip(button, tooltip);

            // Effetti hover
            Color hoverColor = ControlPaint.Light(baseColor, 0.1f);
            Color pressedColor = ControlPaint.Dark(baseColor, 0.1f);

            button.MouseEnter += (s, e) => {
                button.BackColor = hoverColor;
                button.FlatAppearance.MouseOverBackColor = hoverColor;
            };

            button.MouseLeave += (s, e) => {
                button.BackColor = baseColor;
            };

            button.MouseDown += (s, e) => {
                button.BackColor = pressedColor;
            };

            button.MouseUp += (s, e) => {
                button.BackColor = hoverColor;
            };
        }

        private void ConfigureSearchSection()
        {
            if (_searchTextBox != null)
            {
                _searchTextBox.Font = new Font("Segoe UI", 9F);
                _searchTextBox.ForeColor = Color.FromArgb(73, 80, 87);

                // Placeholder text simulation
                if (string.IsNullOrEmpty(_searchTextBox.Text))
                {
                    SetPlaceholderText("Cerca ticket...");
                }
            }
        }

        private void SetPlaceholderText(string placeholder)
        {
            if (_searchTextBox == null) return;

            _searchTextBox.Text = placeholder;
            _searchTextBox.ForeColor = Color.Gray;
            _searchTextBox.Tag = "placeholder";
        }

        private void ConfigureSearchFeatures()
        {
            // Configurazione funzionalità avanzate di ricerca
            SetupSearchAutoComplete();
        }

        private void SetupSearchAutoComplete()
        {
            if (_searchTextBox == null) return;

            // Configura AutoComplete con storia ricerche
            var autoComplete = new AutoCompleteStringCollection();
            _searchTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            _searchTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            _searchTextBox.AutoCompleteCustomSource = autoComplete;
        }
        #endregion

        #region Event Handlers Setup
        private void SetupEventHandlers()
        {
            // Search events
            if (_searchTextBox != null)
            {
                _searchTextBox.KeyPress += OnSearchKeyPress;
                _searchTextBox.Enter += OnSearchFocusEnter;
                _searchTextBox.Leave += OnSearchFocusLeave;
                _searchTextBox.TextChanged += OnSearchTextChanged;
            }

            if (_searchButton != null)
                _searchButton.Click += OnSearchButtonClick;

            // Action button events
            if (_refreshButton != null)
                _refreshButton.Click += OnRefreshClick;

            if (_exportButton != null)
                _exportButton.Click += OnExportClick;

            if (_dashboardButton != null)
                _dashboardButton.Click += OnDashboardClick;

            if (_automationButton != null)
                _automationButton.Click += OnAutomationClick;

            if (_testButton != null)
                _testButton.Click += OnTestClick;

            if (_configButton != null)
                _configButton.Click += OnConfigClick;
        }
        #endregion

        #region Search Event Handlers
        private void OnSearchKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                PerformSearch();
            }
        }

        private void OnSearchFocusEnter(object sender, EventArgs e)
        {
            if (_searchTextBox.Tag?.ToString() == "placeholder")
            {
                _searchTextBox.Text = "";
                _searchTextBox.ForeColor = Color.FromArgb(73, 80, 87);
                _searchTextBox.Tag = "";
            }
        }

        private void OnSearchFocusLeave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_searchTextBox.Text))
            {
                SetPlaceholderText("Cerca ticket...");
            }
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            // Debounce search per evitare troppe ricerche
            _lastSearchTime = DateTime.Now;

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = SEARCH_DEBOUNCE_MS;
            timer.Tick += (s, args) =>
            {
                if (DateTime.Now.Subtract(_lastSearchTime).TotalMilliseconds >= SEARCH_DEBOUNCE_MS)
                {
                    // Auto-search per testi lunghi
                    if (_searchTextBox.Text.Length >= 3 && _searchTextBox.Tag?.ToString() != "placeholder")
                    {
                        PerformSearch(isAutoSearch: true);
                    }
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void OnSearchButtonClick(object sender, EventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch(bool isAutoSearch = false)
        {
            if (_searchTextBox?.Tag?.ToString() == "placeholder") return;

            string searchTerm = _searchTextBox?.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(searchTerm) && !isAutoSearch)
            {
                // Ricerca vuota = mostra tutti
                searchTerm = "";
            }

            // Aggiungi alla storia se non è auto-search
            if (!isAutoSearch && !string.IsNullOrEmpty(searchTerm))
            {
                AddToSearchHistory(searchTerm);
            }

            // Aggiorna stato ricerca
            _isSearchActive = !string.IsNullOrEmpty(searchTerm);
            UpdateSearchButtonState();

            // Lancia evento
            SearchRequested?.Invoke(this, new SearchEventArgs
            {
                SearchTerm = searchTerm,
                IsAutoSearch = isAutoSearch,
                SearchType = DetermineSearchType(searchTerm)
            });
        }

        private void AddToSearchHistory(string searchTerm)
        {
            if (!_searchHistory.Contains(searchTerm))
            {
                _searchHistory.Insert(0, searchTerm);

                // Mantieni solo ultime 10 ricerche
                if (_searchHistory.Count > 10)
                {
                    _searchHistory.RemoveAt(_searchHistory.Count - 1);
                }

                // Aggiorna AutoComplete
                UpdateAutoComplete();
            }
        }

        private void UpdateAutoComplete()
        {
            if (_searchTextBox?.AutoCompleteCustomSource == null) return;

            _searchTextBox.AutoCompleteCustomSource.Clear();
            _searchTextBox.AutoCompleteCustomSource.AddRange(_searchHistory.ToArray());
        }

        private SearchType DetermineSearchType(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm)) return SearchType.All;

            // Pattern per Jira Key (es: ABC-123)
            if (System.Text.RegularExpressions.Regex.IsMatch(searchTerm, @"^[A-Z]+-\d+$"))
                return SearchType.TicketKey;

            // Pattern per email
            if (searchTerm.Contains("@") && searchTerm.Contains("."))
                return SearchType.Email;

            // Pattern per data (vari formati)
            if (System.Text.RegularExpressions.Regex.IsMatch(searchTerm, @"\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4}"))
                return SearchType.Date;

            return SearchType.Text;
        }

        private void UpdateSearchButtonState()
        {
            if (_searchButton == null) return;

            if (_isSearchActive)
            {
                _searchButton.Text = "🔍 Cerca";
                _searchButton.BackColor = Color.FromArgb(220, 53, 69); // Rosso per "clear search"
            }
            else
            {
                _searchButton.Text = "🔍 Cerca";
                _searchButton.BackColor = Color.FromArgb(0, 120, 212); // Blu normale
            }
        }
        #endregion

        #region Action Button Event Handlers
        private void OnRefreshClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Refresh requested");
            RefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Export requested");

            // Determina tipo di export basato su contesto
            var exportArgs = new ExportEventArgs
            {
                ExportType = ExportType.Excel,
                IncludeFilters = _isSearchActive,
                SearchTerm = _isSearchActive ? _searchTextBox?.Text?.Trim() : null
            };

            ExportRequested?.Invoke(this, exportArgs);
        }

        private void OnDashboardClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Dashboard requested");
            DashboardRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnAutomationClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Automation requested");
            AutomationRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnTestClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Test requested");
            TestRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnConfigClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ToolbarManager: Config requested");
            ConfigRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Public Methods
        public void SetButtonEnabled(ToolbarButton button, bool enabled)
        {
            var targetButton = GetButtonByType(button);
            if (targetButton != null)
            {
                targetButton.Enabled = enabled;
                targetButton.BackColor = enabled ?
                    GetButtonOriginalColor(button) :
                    Color.FromArgb(173, 181, 189); // Grigio disabilitato
            }
        }

        public void SetSearchText(string text)
        {
            if (_searchTextBox != null)
            {
                _searchTextBox.Text = text;
                _searchTextBox.ForeColor = Color.FromArgb(73, 80, 87);
                _searchTextBox.Tag = "";
            }
        }

        public void ClearSearch()
        {
            if (_searchTextBox != null)
            {
                SetPlaceholderText("Cerca ticket...");
                _isSearchActive = false;
                UpdateSearchButtonState();
            }
        }

        public string GetCurrentSearchTerm()
        {
            if (_searchTextBox?.Tag?.ToString() == "placeholder")
                return "";

            return _searchTextBox?.Text?.Trim() ?? "";
        }

        public List<string> GetSearchHistory()
        {
            return new List<string>(_searchHistory);
        }

        public void ShowProgress(ToolbarButton button, bool show)
        {
            var targetButton = GetButtonByType(button);
            if (targetButton != null)
            {
                if (show)
                {
                    targetButton.Text = "⏳ " + GetButtonText(button);
                    targetButton.Enabled = false;
                }
                else
                {
                    targetButton.Text = GetButtonOriginalText(button);
                    targetButton.Enabled = true;
                }
            }
        }
        #endregion

        #region Helper Methods
        private Button GetButtonByType(ToolbarButton buttonType)
        {
            return buttonType switch
            {
                ToolbarButton.Search => _searchButton,
                ToolbarButton.Refresh => _refreshButton,
                ToolbarButton.Export => _exportButton,
                ToolbarButton.Dashboard => _dashboardButton,
                ToolbarButton.Automation => _automationButton,
                ToolbarButton.Test => _testButton,
                ToolbarButton.Config => _configButton,
                _ => null
            };
        }

        private Color GetButtonOriginalColor(ToolbarButton buttonType)
        {
            return buttonType switch
            {
                ToolbarButton.Export => Color.FromArgb(22, 163, 74),
                ToolbarButton.Dashboard => Color.FromArgb(8, 145, 178),
                ToolbarButton.Automation => Color.FromArgb(124, 58, 237),
                ToolbarButton.Test => Color.FromArgb(217, 119, 6),
                ToolbarButton.Config => Color.FromArgb(108, 117, 125),
                ToolbarButton.Refresh => Color.FromArgb(0, 120, 212),
                ToolbarButton.Search => Color.FromArgb(0, 120, 212),
                _ => Color.Gray
            };
        }

        private string GetButtonText(ToolbarButton buttonType)
        {
            return buttonType switch
            {
                ToolbarButton.Export => "Export Excel",
                ToolbarButton.Dashboard => "Dashboard",
                ToolbarButton.Automation => "Automation",
                ToolbarButton.Test => "Test",
                ToolbarButton.Config => "Config",
                ToolbarButton.Refresh => "Aggiorna",
                ToolbarButton.Search => "Cerca",
                _ => "Button"
            };
        }

        private string GetButtonOriginalText(ToolbarButton buttonType)
        {
            return buttonType switch
            {
                ToolbarButton.Export => "📊 Export Excel",
                ToolbarButton.Dashboard => "📈 Dashboard",
                ToolbarButton.Automation => "🤖 Automation",
                ToolbarButton.Test => "🔧 Test",
                ToolbarButton.Config => "⚙️ Config",
                ToolbarButton.Refresh => "🔄 Aggiorna",
                ToolbarButton.Search => "🔍 Cerca",
                _ => "Button"
            };
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            // Cleanup se necessario
            _searchHistory?.Clear();
        }
        #endregion
    }

    #region Event Args Classes
    public class SearchEventArgs : EventArgs
    {
        public string SearchTerm { get; set; }
        public bool IsAutoSearch { get; set; }
        public SearchType SearchType { get; set; }
    }

    public class ExportEventArgs : EventArgs
    {
        public ExportType ExportType { get; set; }
        public bool IncludeFilters { get; set; }
        public string SearchTerm { get; set; }
    }
    #endregion

    #region Enums
    public enum ToolbarButton
    {
        Search,
        Refresh,
        Export,
        Dashboard,
        Automation,
        Test,
        Config
    }

    public enum SearchType
    {
        All,
        Text,
        TicketKey,
        Email,
        Date
    }

    public enum ExportType
    {
        Excel,
        CSV,
        PDF
    }
    #endregion
}