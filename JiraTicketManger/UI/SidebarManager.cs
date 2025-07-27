using System;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager.UI
{
    public class SidebarManager
    {
        #region Fields
        private readonly Panel _sidebarPanel;
        private readonly Panel _sidebarContent;
        private readonly Button _toggleButton;

        private bool _isExpanded = false;
        private System.Windows.Forms.Timer _animationTimer;
        private int _targetWidth;

        private const int COLLAPSED_WIDTH = 40;
        private const int EXPANDED_WIDTH = 280;
        private const int ANIMATION_STEP = 15;
        #endregion

        #region Events
        public event EventHandler<string> QuickFilterClicked;
        public event EventHandler SidebarToggled;
        #endregion

        #region Constructor
        public SidebarManager(Panel sidebarPanel, Panel sidebarContent, Button toggleButton)
        {
            _sidebarPanel = sidebarPanel ?? throw new ArgumentNullException(nameof(sidebarPanel));
            _sidebarContent = sidebarContent ?? throw new ArgumentNullException(nameof(sidebarContent));
            _toggleButton = toggleButton ?? throw new ArgumentNullException(nameof(toggleButton));

            Initialize();
        }
        #endregion

        #region Public Methods
        public void Toggle()
        {
            _isExpanded = !_isExpanded;
            _targetWidth = _isExpanded ? EXPANDED_WIDTH : COLLAPSED_WIDTH;

            UpdateToggleButton();
            StartAnimation();

            SidebarToggled?.Invoke(this, EventArgs.Empty);
        }

        public void SetExpanded(bool expanded)
        {
            if (_isExpanded == expanded) return;

            Toggle();
        }

        public bool IsExpanded => _isExpanded;

        public void RefreshStatistics(int totalTickets, int myTickets, int urgentToday, int unassigned, int modifiedToday, int nearDeadlines, int newTickets)
        {
            // Aggiorna i badge numerici nei filtri rapidi
            UpdateQuickFilterBadges(totalTickets, myTickets, urgentToday, unassigned, modifiedToday, nearDeadlines, newTickets);
        }

        public void RefreshStatistics(int openedToday, int inProgress, int completed, int overdue, int total)
        {
            // Aggiorna le statistiche nella sezione dedicata
            UpdateStatisticsValues(openedToday, inProgress, completed, overdue, total);
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            InitializeAnimation();
            SetupEventHandlers();
            CreateSidebarContent();

            // Stato iniziale
            _isExpanded = false;
            _targetWidth = COLLAPSED_WIDTH;
            _sidebarContent.Visible = false;
            UpdateToggleButton();
        }

        private void InitializeAnimation()
        {
            _animationTimer = new System.Windows.Forms.Timer();
            _animationTimer.Interval = 16; // ~60 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        private void SetupEventHandlers()
        {
            _toggleButton.Click += (s, e) => Toggle();
        }

        private void StartAnimation()
        {
            // Gestisci visibilità del contenuto
            if (_isExpanded)
            {
                _sidebarContent.Visible = true;
            }

            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            int currentWidth = _sidebarPanel.Width;
            int difference = _targetWidth - currentWidth;

            if (Math.Abs(difference) <= ANIMATION_STEP)
            {
                // Animazione completata
                _sidebarPanel.Width = _targetWidth;
                _animationTimer.Stop();

                // Nascondi contenuto se collassato
                if (!_isExpanded)
                {
                    _sidebarContent.Visible = false;
                }
            }
            else
            {
                // Continua animazione
                int step = difference > 0 ? ANIMATION_STEP : -ANIMATION_STEP;
                _sidebarPanel.Width = currentWidth + step;
            }
        }

        private void UpdateToggleButton()
        {
            _toggleButton.Text = _isExpanded ? "◀" : "≡";
            _toggleButton.Font = new Font("Segoe UI", _isExpanded ? 10F : 12F);
        }

        private void CreateSidebarContent()
        {
            _sidebarContent.Controls.Clear();

            CreateQuickFiltersSection();
            CreateStatisticsSection();
        }

        private void CreateQuickFiltersSection()
        {
            GroupBox grpQuickFilters = new GroupBox();
            grpQuickFilters.Text = "FILTRI RAPIDI";
            grpQuickFilters.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grpQuickFilters.ForeColor = Color.FromArgb(108, 117, 125);
            grpQuickFilters.Location = new Point(5, 5);
            grpQuickFilters.Size = new Size(265, 300);
            grpQuickFilters.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var quickFilters = new[]
            {
                new { Text = "📊 Tutti i Ticket", Count = 234, Filter = "all" },
                new { Text = "👤 Miei Ticket", Count = 23, Filter = "mine" },
                new { Text = "⚡ Urgenti Oggi", Count = 7, Filter = "urgent_today" },
                new { Text = "📋 Da Assegnare", Count = 12, Filter = "unassigned" },
                new { Text = "🔄 Modificati Oggi", Count = 5, Filter = "modified_today" },
                new { Text = "📅 Scadenze Vicine", Count = 3, Filter = "near_deadlines" },
                new { Text = "🆕 Nuovi", Count = 8, Filter = "new" }
            };

            for (int i = 0; i < quickFilters.Length; i++)
            {
                var filter = quickFilters[i];
                Button btnFilter = CreateQuickFilterButton(filter.Text, filter.Count, filter.Filter, i);
                grpQuickFilters.Controls.Add(btnFilter);
            }

            _sidebarContent.Controls.Add(grpQuickFilters);
        }

        private Button CreateQuickFilterButton(string text, int count, string filter, int index)
        {
            Button btn = new Button();
            btn.Text = $"{text} ({count})";
            btn.Size = new Size(250, 30);
            btn.Location = new Point(10, 25 + (index * 32));
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.FromArgb(73, 80, 87);
            btn.Font = new Font("Segoe UI", 8F);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Cursor = Cursors.Hand;
            btn.Tag = filter; // Memorizza il tipo di filtro

            // Effetti hover
            btn.MouseEnter += (s, e) => {
                btn.BackColor = Color.FromArgb(233, 246, 255);
                btn.ForeColor = Color.FromArgb(0, 120, 212);
            };
            btn.MouseLeave += (s, e) => {
                btn.BackColor = Color.Transparent;
                btn.ForeColor = Color.FromArgb(73, 80, 87);
            };

            // Click event
            btn.Click += (s, e) => {
                QuickFilterClicked?.Invoke(this, filter);
            };

            return btn;
        }

        private void CreateStatisticsSection()
        {
            GroupBox grpStatistics = new GroupBox();
            grpStatistics.Text = "STATISTICHE";
            grpStatistics.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grpStatistics.ForeColor = Color.FromArgb(108, 117, 125);
            grpStatistics.Location = new Point(5, 315);
            grpStatistics.Size = new Size(265, 200);
            grpStatistics.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var statistics = new[]
            {
                new { Label = "Aperti oggi", Value = 12, Key = "opened_today" },
                new { Label = "In corso", Value = 34, Key = "in_progress" },
                new { Label = "Completati", Value = 156, Key = "completed" },
                new { Label = "Overdue", Value = 8, Key = "overdue" },
                new { Label = "Totali", Value = 234, Key = "total" }
            };

            for (int i = 0; i < statistics.Length; i++)
            {
                var stat = statistics[i];
                Panel pnlStat = CreateStatisticPanel(stat.Label, stat.Value, stat.Key, i);
                grpStatistics.Controls.Add(pnlStat);
            }

            _sidebarContent.Controls.Add(grpStatistics);
        }

        private Panel CreateStatisticPanel(string label, int value, string key, int index)
        {
            Panel pnlStat = new Panel();
            pnlStat.Size = new Size(250, 25);
            pnlStat.Location = new Point(10, 25 + (index * 27));
            pnlStat.BackColor = Color.Transparent;
            pnlStat.Tag = key; // Per identificare il pannello durante gli aggiornamenti

            Label lblLabel = new Label();
            lblLabel.Text = label;
            lblLabel.Font = new Font("Segoe UI", 8F);
            lblLabel.ForeColor = Color.FromArgb(73, 80, 87);
            lblLabel.Location = new Point(0, 5);
            lblLabel.AutoSize = true;

            Label lblValue = new Label();
            lblValue.Text = value.ToString();
            lblValue.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblValue.ForeColor = Color.FromArgb(73, 80, 87);
            lblValue.BackColor = Color.FromArgb(222, 226, 230);
            lblValue.Location = new Point(200, 2);
            lblValue.Size = new Size(30, 20);
            lblValue.TextAlign = ContentAlignment.MiddleCenter;
            lblValue.Name = "value"; // Per trovarlo facilmente

            pnlStat.Controls.Add(lblLabel);
            pnlStat.Controls.Add(lblValue);

            return pnlStat;
        }

        private void UpdateQuickFilterBadges(int totalTickets, int myTickets, int urgentToday, int unassigned, int modifiedToday, int nearDeadlines, int newTickets)
        {
            // Implementa l'aggiornamento dei badge nei filtri rapidi
            // (Cerca i pulsanti e aggiorna il testo con i nuovi numeri)
        }

        private void UpdateStatisticsValues(int openedToday, int inProgress, int completed, int overdue, int total)
        {
            // Implementa l'aggiornamento delle statistiche
            // (Cerca i pannelli per Tag e aggiorna i valori)
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
        }
        #endregion
    }
}