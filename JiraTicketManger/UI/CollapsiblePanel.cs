using System;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager.UI
{
    /// <summary>
    /// Pannello collassabile personalizzato per Windows Forms
    /// Replica il comportamento degli accordion nel mockup HTML
    /// </summary>
    public class CollapsiblePanel : Panel
    {
        #region Private Fields

        private bool _isExpanded = true;
        private int _collapsedHeight = 40;
        private int _expandedHeight = 200;
        private Panel _headerPanel;
        private Panel _contentPanel;
        private Label _titleLabel;
        private Button _toggleButton;
        private string _title = "";
        private string _titleIcon = "";

        #endregion

        #region Public Properties

        /// <summary>
        /// Titolo del pannello collassabile
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                UpdateTitleDisplay();
            }
        }

        /// <summary>
        /// Icona da mostrare nel titolo (emoji o carattere)
        /// </summary>
        public string TitleIcon
        {
            get => _titleIcon;
            set
            {
                _titleIcon = value;
                UpdateTitleDisplay();
            }
        }

        /// <summary>
        /// Indica se il pannello è espanso o collassato
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    TogglePanel();
                    OnExpandedChanged();
                }
            }
        }

        /// <summary>
        /// Altezza del pannello quando è collassato
        /// </summary>
        public int CollapsedHeight
        {
            get => _collapsedHeight;
            set
            {
                _collapsedHeight = value;
                if (!_isExpanded)
                    this.Height = _collapsedHeight;
            }
        }

        /// <summary>
        /// Altezza del pannello quando è espanso
        /// </summary>
        public int ExpandedHeight
        {
            get => _expandedHeight;
            set
            {
                _expandedHeight = value;
                if (_isExpanded)
                    this.Height = _expandedHeight;
            }
        }

        /// <summary>
        /// Pannello contenente i controlli del contenuto
        /// </summary>
        public Panel ContentPanel => _contentPanel;

        #endregion

        #region Events

        /// <summary>
        /// Evento scatenato quando lo stato espanso/collassato cambia
        /// </summary>
        public event EventHandler ExpandedChanged;

        #endregion

        #region Constructor

        public CollapsiblePanel()
        {
            InitializeComponent();
            SetupStyling();
            SetupEventHandlers();
        }

        #endregion

        #region Initialization Methods

        private void InitializeComponent()
        {
            // Configurazione pannello principale
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = Color.White;
            this.Size = new Size(300, _expandedHeight);

            // Creazione header panel
            _headerPanel = new Panel
            {
                Height = _collapsedHeight,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(52, 73, 94),
                Cursor = Cursors.Hand
            };

            // Creazione title label
            _titleLabel = new Label
            {
                Text = _titleIcon + " " + _title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(16, 0),
                Size = new Size(200, _collapsedHeight),
                Cursor = Cursors.Hand
            };

            // Creazione toggle button
            _toggleButton = new Button
            {
                Text = "▼",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Size = new Size(24, 24),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _toggleButton.FlatAppearance.BorderSize = 0;
            _toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 90, 110);

            // Creazione content panel
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                AutoScroll = true
            };

            // Aggiunta controlli alla gerarchia
            _headerPanel.Controls.Add(_titleLabel);
            _headerPanel.Controls.Add(_toggleButton);
            this.Controls.Add(_contentPanel);
            this.Controls.Add(_headerPanel);

            // Posizionamento toggle button
            _toggleButton.Location = new Point(_headerPanel.Width - _toggleButton.Width - 8,
                (_headerPanel.Height - _toggleButton.Height) / 2);
        }

        private void SetupStyling()
        {
            // Styling coerente con MainForm palette
            _headerPanel.BackColor = Color.FromArgb(52, 73, 94);
            _contentPanel.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;

            // Hover effects per migliorare UX
            _headerPanel.MouseEnter += (s, e) => _headerPanel.BackColor = Color.FromArgb(62, 83, 104);
            _headerPanel.MouseLeave += (s, e) => _headerPanel.BackColor = Color.FromArgb(52, 73, 94);

            _titleLabel.MouseEnter += (s, e) => _headerPanel.BackColor = Color.FromArgb(62, 83, 104);
            _titleLabel.MouseLeave += (s, e) => _headerPanel.BackColor = Color.FromArgb(52, 73, 94);
        }

        private void SetupEventHandlers()
        {
            // Click handlers per toggle
            _headerPanel.Click += ToggleButton_Click;
            _titleLabel.Click += ToggleButton_Click;
            _toggleButton.Click += ToggleButton_Click;

            // Resize handler per riposizionare toggle button
            this.Resize += CollapsiblePanel_Resize;
            _headerPanel.Resize += (s, e) => RepositionToggleButton();
        }

        #endregion

        #region Event Handlers

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            IsExpanded = !IsExpanded;
        }

        private void CollapsiblePanel_Resize(object sender, EventArgs e)
        {
            // Salva l'altezza solo se espanso
            if (_isExpanded && this.Height > _collapsedHeight)
            {
                _expandedHeight = this.Height;
            }

            RepositionToggleButton();
        }

        #endregion

        #region Private Methods

        private void TogglePanel()
        {
            // Animazione semplice del toggle
            if (_isExpanded)
            {
                this.Height = _expandedHeight;
                _contentPanel.Visible = true;
                _toggleButton.Text = "▼";
            }
            else
            {
                this.Height = _collapsedHeight;
                _contentPanel.Visible = false;
                _toggleButton.Text = "►";
            }

            // Refresh del parent per layout corretto
            this.Parent?.PerformLayout();
        }

        private void UpdateTitleDisplay()
        {
            if (_titleLabel != null)
            {
                _titleLabel.Text = string.IsNullOrEmpty(_titleIcon)
                    ? _title
                    : $"{_titleIcon} {_title}";
            }
        }

        private void RepositionToggleButton()
        {
            if (_toggleButton != null && _headerPanel != null)
            {
                _toggleButton.Location = new Point(
                    _headerPanel.Width - _toggleButton.Width - 8,
                    (_headerPanel.Height - _toggleButton.Height) / 2);
            }
        }

        private void OnExpandedChanged()
        {
            ExpandedChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Aggiunge un controllo al pannello del contenuto
        /// </summary>
        /// <param name="control">Controllo da aggiungere</param>
        public void AddContent(Control control)
        {
            _contentPanel.Controls.Add(control);
        }

        /// <summary>
        /// Rimuove tutti i controlli dal pannello del contenuto
        /// </summary>
        public void ClearContent()
        {
            _contentPanel.Controls.Clear();
        }

        /// <summary>
        /// Espande il pannello
        /// </summary>
        public void Expand()
        {
            IsExpanded = true;
        }

        /// <summary>
        /// Collassa il pannello
        /// </summary>
        public void Collapse()
        {
            IsExpanded = false;
        }

        #endregion

        #region Designer Support

        /// <summary>
        /// Supporto per Visual Studio Designer
        /// </summary>
        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            // Limita l'altezza minima al collapsed height
            if (height < _collapsedHeight)
                height = _collapsedHeight;

            // Se espanso, salva la nuova altezza
            if (_isExpanded && height > _collapsedHeight)
                _expandedHeight = height;

            base.SetBoundsCore(x, y, width, height, specified);
        }

        #endregion
    }
}

// ===================================================================
// === MANAGER PER GESTIONE ACCORDION ===
// ===================================================================

namespace JiraTicketManager.UI
{
    /// <summary>
    /// Manager per gestire il comportamento accordion dei CollapsiblePanel
    /// Permette di avere solo un pannello espanso alla volta
    /// </summary>
    public class CollapsiblePanelManager
    {
        #region Private Fields

        private readonly List<CollapsiblePanel> _panels;
        private bool _autoCollapseOthers;

        #endregion

        #region Public Properties

        /// <summary>
        /// Se true, espandendo un pannello gli altri si collassano automaticamente
        /// </summary>
        public bool AutoCollapseOthers
        {
            get => _autoCollapseOthers;
            set => _autoCollapseOthers = value;
        }

        #endregion

        #region Constructor

        public CollapsiblePanelManager(bool autoCollapseOthers = true)
        {
            _panels = new List<CollapsiblePanel>();
            _autoCollapseOthers = autoCollapseOthers;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registra un pannello per la gestione accordion
        /// </summary>
        /// <param name="panel">Pannello da registrare</param>
        public void RegisterPanel(CollapsiblePanel panel)
        {
            if (!_panels.Contains(panel))
            {
                _panels.Add(panel);
                panel.ExpandedChanged += Panel_ExpandedChanged;
            }
        }

        /// <summary>
        /// Rimuove un pannello dalla gestione
        /// </summary>
        /// <param name="panel">Pannello da rimuovere</param>
        public void UnregisterPanel(CollapsiblePanel panel)
        {
            if (_panels.Contains(panel))
            {
                _panels.Remove(panel);
                panel.ExpandedChanged -= Panel_ExpandedChanged;
            }
        }

        /// <summary>
        /// Espande tutti i pannelli
        /// </summary>
        public void ExpandAll()
        {
            _autoCollapseOthers = false;
            foreach (var panel in _panels)
            {
                panel.IsExpanded = true;
            }
            _autoCollapseOthers = true;
        }

        /// <summary>
        /// Collassa tutti i pannelli
        /// </summary>
        public void CollapseAll()
        {
            foreach (var panel in _panels)
            {
                panel.IsExpanded = false;
            }
        }

        /// <summary>
        /// Espande solo il pannello con il titolo specificato
        /// </summary>
        /// <param name="title">Titolo del pannello da espandere</param>
        public void ExpandPanelByTitle(string title)
        {
            foreach (var panel in _panels)
            {
                panel.IsExpanded = panel.Title.Contains(title);
            }
        }

        #endregion

        #region Private Methods

        private void Panel_ExpandedChanged(object sender, EventArgs e)
        {
            if (!_autoCollapseOthers) return;

            var expandedPanel = sender as CollapsiblePanel;
            if (expandedPanel?.IsExpanded == true)
            {
                // Collassa tutti gli altri pannelli
                foreach (var panel in _panels)
                {
                    if (panel != expandedPanel && panel.IsExpanded)
                    {
                        panel.IsExpanded = false;
                    }
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            foreach (var panel in _panels.ToList())
            {
                UnregisterPanel(panel);
            }
            _panels.Clear();
        }

        #endregion
    }
}