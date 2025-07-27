using JiraTicketManager.Services;
using JiraTicketManager.Data;
using JiraTicketManager.Data.Models;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.UI.Managers
{
    /// <summary>
    /// Manager specializzato per gestire la DataGridView dei ticket Jira.
    /// Gestisce caricamento, formattazione, filtering e paginazione.
    /// </summary>
    public class DataGridManager
    {
        private readonly IJiraDataService _dataService;
        private readonly DataGridView _dataGridView;
        private readonly LoggingService _logger;

        private DataTable _currentDataTable;
        private List<JiraTicket> _currentTickets = new();
        private JiraSearchResult _lastSearchResult;
        private PaginationConfig _pagination = new();
        private JiraSearchCriteria _lastCriteria = new();

        // Events
        public event EventHandler<TicketSelectedEventArgs> TicketSelected;
        public event EventHandler<DataLoadedEventArgs> DataLoaded;
        public event EventHandler<ErrorEventArgs> LoadError;

        public DataGridManager(IJiraDataService dataService, DataGridView dataGridView)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _dataGridView = dataGridView ?? throw new ArgumentNullException(nameof(dataGridView));
            _logger = LoggingService.CreateForComponent("DataGridManager");

            InitializeDataGrid();
            SetupEventHandlers();
        }

        #region Public Properties

        /// <summary>
        /// Configurazione di paginazione corrente
        /// </summary>
        public PaginationConfig Pagination => _pagination;

        /// <summary>
        /// Risultato dell'ultima ricerca
        /// </summary>
        public JiraSearchResult LastSearchResult => _lastSearchResult;

        /// <summary>
        /// Lista dei ticket attualmente visualizzati
        /// </summary>
        public IReadOnlyList<JiraTicket> CurrentTickets => _currentTickets.AsReadOnly();

        /// <summary>
        /// Ticket selezionato correntemente
        /// </summary>
        public JiraTicket SelectedTicket
        {
            get
            {
                if (_dataGridView.SelectedRows.Count > 0)
                {
                    var selectedIndex = _dataGridView.SelectedRows[0].Index;
                    if (selectedIndex >= 0 && selectedIndex < _currentTickets.Count)
                    {
                        return _currentTickets[selectedIndex];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Verifica se ci sono dati caricati
        /// </summary>
        public bool HasData => _currentTickets.Count > 0;

        #endregion

        #region Public Methods

        /// <summary>
        /// Carica i dati iniziali con JQL di base
        /// </summary>
        public async Task LoadInitialDataAsync(IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("Caricamento dati iniziali");
                progress?.Report("Caricamento ticket iniziali...");

                _pagination.Reset();
                var result = await _dataService.LoadInitialDataAsync(_pagination.PageSize);

                await ProcessSearchResult(result);

                _logger.LogInfo($"Dati iniziali caricati: {result.Total} ticket trovati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento dati iniziali", ex);
                LoadError?.Invoke(this, new ErrorEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// Ricerca ticket usando criteri strutturati
        /// </summary>
        public async Task SearchAsync(JiraSearchCriteria criteria, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("Ricerca ticket con criteri strutturati");
                progress?.Report("Ricerca ticket in corso...");

                _lastCriteria = criteria.Clone();
                _pagination.Reset();

                var result = await _dataService.SearchTicketsAsync(criteria, _pagination);
                await ProcessSearchResult(result);

                _logger.LogInfo($"Ricerca completata: {result.Total} ticket trovati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca ticket", ex);
                LoadError?.Invoke(this, new ErrorEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// Ricerca ticket usando JQL personalizzata
        /// </summary>
        public async Task SearchAsync(string jql, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Ricerca con JQL: {jql}");
                progress?.Report("Esecuzione query JQL...");

                _pagination.Reset();
                var result = await _dataService.SearchTicketsAsync(jql, _pagination.StartAt, _pagination.PageSize);
                await ProcessSearchResult(result);

                _logger.LogInfo($"Ricerca JQL completata: {result.Total} ticket trovati");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ricerca JQL", ex);
                LoadError?.Invoke(this, new ErrorEventArgs(ex));
                throw;
            }
        }

        /// <summary>
        /// Naviga alla pagina successiva
        /// </summary>
        public async Task GoToNextPageAsync(IProgress<string> progress = null)
        {
            if (_lastSearchResult?.IsLast == false)
            {
                _pagination.NextPage();
                await RefreshCurrentSearchAsync(progress);
            }
        }

        /// <summary>
        /// Naviga alla pagina precedente
        /// </summary>
        public async Task GoToPreviousPageAsync(IProgress<string> progress = null)
        {
            if (_pagination.CurrentPage > 1)
            {
                _pagination.PreviousPage();
                await RefreshCurrentSearchAsync(progress);
            }
        }

        /// <summary>
        /// Naviga a una pagina specifica
        /// </summary>
        public async Task GoToPageAsync(int page, IProgress<string> progress = null)
        {
            if (page > 0 && page <= _lastSearchResult?.TotalPages)
            {
                _pagination.GoToPage(page);
                await RefreshCurrentSearchAsync(progress);
            }
        }

        /// <summary>
        /// Cambia la dimensione della pagina
        /// </summary>
        public async Task ChangePageSizeAsync(int newPageSize, IProgress<string> progress = null)
        {
            _pagination.PageSize = newPageSize;
            _pagination.Reset();
            await RefreshCurrentSearchAsync(progress);
        }

        /// <summary>
        /// Ricarica i dati con gli ultimi criteri di ricerca
        /// </summary>
        public async Task RefreshAsync(IProgress<string> progress = null)
        {
            await RefreshCurrentSearchAsync(progress);
        }

        /// <summary>
        /// Applica un filtro rapido ai dati correnti
        /// </summary>
        public void ApplyQuickFilter(Func<JiraTicket, bool> filter)
        {
            if (_currentTickets.Count == 0) return;

            var filteredTickets = _currentTickets.Where(filter).ToList();
            var filteredDataTable = ConvertTicketsToDataTable(filteredTickets);

            _dataGridView.DataSource = filteredDataTable;
            _logger.LogInfo($"Filtro rapido applicato: {filteredTickets.Count}/{_currentTickets.Count} ticket");
        }

        /// <summary>
        /// Rimuove tutti i filtri rapidi
        /// </summary>
        public void ClearQuickFilters()
        {
            _dataGridView.DataSource = _currentDataTable;
            _logger.LogInfo("Filtri rapidi rimossi");
        }

        /// <summary>
        /// Seleziona un ticket per chiave
        /// </summary>
        public void SelectTicket(string ticketKey)
        {
            if (string.IsNullOrEmpty(ticketKey)) return;

            for (int i = 0; i < _dataGridView.Rows.Count; i++)
            {
                var keyValue = _dataGridView.Rows[i].Cells["Key"]?.Value?.ToString();
                if (string.Equals(keyValue, ticketKey, StringComparison.OrdinalIgnoreCase))
                {
                    _dataGridView.ClearSelection();
                    _dataGridView.Rows[i].Selected = true;
                    _dataGridView.FirstDisplayedScrollingRowIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Esporta i dati correnti in DataTable per elaborazioni esterne
        /// </summary>
        public DataTable ExportCurrentData()
        {
            return _currentDataTable?.Copy();
        }

        #endregion

        #region Private Methods

        private void InitializeDataGrid()
        {
            _dataGridView.AllowUserToAddRows = false;
            _dataGridView.AllowUserToDeleteRows = false;
            _dataGridView.ReadOnly = true;
            _dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _dataGridView.MultiSelect = false;
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            _dataGridView.RowHeadersVisible = false;
            _dataGridView.BackgroundColor = Color.White;
            _dataGridView.BorderStyle = BorderStyle.None;
            _dataGridView.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            _dataGridView.GridColor = Color.FromArgb(230, 230, 230);

            // Header styling
            _dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 152, 219);
            _dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _dataGridView.ColumnHeadersHeight = 35;

            // Row styling
            _dataGridView.DefaultCellStyle.BackColor = Color.White;
            _dataGridView.DefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
            _dataGridView.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            _dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(41, 128, 185);
            _dataGridView.DefaultCellStyle.SelectionForeColor = Color.White;
            _dataGridView.RowTemplate.Height = 28;

            // Alternating row colors
            _dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
        }

        private void SetupEventHandlers()
        {
            _dataGridView.SelectionChanged += OnSelectionChanged;
            _dataGridView.CellDoubleClick += OnCellDoubleClick;
            _dataGridView.DataBindingComplete += OnDataBindingComplete;
        }

        private async Task ProcessSearchResult(JiraSearchResult result)
        {
            _lastSearchResult = result;
            _currentTickets = result.Issues;

            // Converti in DataTable per binding
            _currentDataTable = ConvertTicketsToDataTable(_currentTickets);

            // Aggiorna UI sul thread principale
            if (_dataGridView.InvokeRequired)
            {
                _dataGridView.Invoke(() => UpdateDataGridUI());
            }
            else
            {
                UpdateDataGridUI();
            }

            // Notifica evento
            DataLoaded?.Invoke(this, new DataLoadedEventArgs(result));
        }

        private void UpdateDataGridUI()
        {
            _dataGridView.DataSource = _currentDataTable;
            ConfigureColumns();
            ApplyConditionalFormatting();
        }

        private void ConfigureColumns()
        {
            if (_dataGridView.Columns.Count == 0) return;

            // Configura colonne esistenti
            var columnConfigs = new Dictionary<string, (int width, string header)>
            {
                ["Key"] = (100, "Chiave"),
                ["Summary"] = (350, "Titolo"),
                ["Status"] = (120, "Stato"),
                ["Priority"] = (100, "Priorità"),
                ["IssueType"] = (100, "Tipo"),
                ["AssigneeDisplayName"] = (150, "Assegnatario"),
                ["Organization"] = (200, "Cliente"),
                ["Area"] = (120, "Area"),
                ["Application"] = (120, "Applicativo"),
                ["Created"] = (100, "Creato"),
                ["Updated"] = (100, "Aggiornato")
            };

            foreach (DataGridViewColumn column in _dataGridView.Columns)
            {
                if (columnConfigs.TryGetValue(column.Name, out var config))
                {
                    column.Width = config.width;
                    column.HeaderText = config.header;
                    column.SortMode = DataGridViewColumnSortMode.Automatic;
                }

                // Formattazione speciale per le date
                if (column.Name.Contains("Created") || column.Name.Contains("Updated"))
                {
                    column.DefaultCellStyle.Format = "dd/MM/yyyy";
                    column.Width = 100;
                }
            }

            // Nascondi colonne non necessarie
            var columnsToHide = new[] { "Description", "Reporter", "ResolutionDate", "RawData" };
            foreach (var columnName in columnsToHide)
            {
                if (_dataGridView.Columns[columnName] != null)
                {
                    _dataGridView.Columns[columnName].Visible = false;
                }
            }
        }

        private void ApplyConditionalFormatting()
        {
            // Applica formattazione condizionale basata su priorità e stato
            foreach (DataGridViewRow row in _dataGridView.Rows)
            {
                if (row.IsNewRow) continue;

                var priority = row.Cells["Priority"]?.Value?.ToString() ?? "";
                var status = row.Cells["Status"]?.Value?.ToString() ?? "";

                // Colori per priorità
                switch (priority.ToLower())
                {
                    case "highest":
                    case "altissima":
                        row.Cells["Priority"].Style.BackColor = Color.FromArgb(231, 76, 60);
                        row.Cells["Priority"].Style.ForeColor = Color.White;
                        break;
                    case "high":
                    case "alta":
                        row.Cells["Priority"].Style.BackColor = Color.FromArgb(241, 196, 15);
                        break;
                    case "low":
                    case "bassa":
                        row.Cells["Priority"].Style.BackColor = Color.FromArgb(46, 204, 113);
                        row.Cells["Priority"].Style.ForeColor = Color.White;
                        break;
                }

                // Colori per stato
                switch (status.ToLower())
                {
                    case "done":
                    case "closed":
                    case "resolved":
                    case "chiuso":
                    case "risolto":
                        row.Cells["Status"].Style.BackColor = Color.FromArgb(39, 174, 96);
                        row.Cells["Status"].Style.ForeColor = Color.White;
                        break;
                    case "in progress":
                    case "in corso":
                        row.Cells["Status"].Style.BackColor = Color.FromArgb(52, 152, 219);
                        row.Cells["Status"].Style.ForeColor = Color.White;
                        break;
                    case "open":
                    case "aperto":
                    case "new":
                    case "nuovo":
                        row.Cells["Status"].Style.BackColor = Color.FromArgb(241, 196, 15);
                        break;
                }
            }
        }

        private async Task RefreshCurrentSearchAsync(IProgress<string> progress = null)
        {
            if (_lastCriteria.HasActiveFilters())
            {
                await SearchAsync(_lastCriteria, progress);
            }
            else
            {
                await LoadInitialDataAsync(progress);
            }
        }

        private DataTable ConvertTicketsToDataTable(List<JiraTicket> tickets)
        {
            var dataTable = new DataTable();

            // Aggiungi colonne
            dataTable.Columns.Add("Key", typeof(string));
            dataTable.Columns.Add("Summary", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("Priority", typeof(string));
            dataTable.Columns.Add("IssueType", typeof(string));
            dataTable.Columns.Add("AssigneeDisplayName", typeof(string));
            dataTable.Columns.Add("Organization", typeof(string));
            dataTable.Columns.Add("Area", typeof(string));
            dataTable.Columns.Add("Application", typeof(string));
            dataTable.Columns.Add("Created", typeof(DateTime));
            dataTable.Columns.Add("Updated", typeof(DateTime));
            dataTable.Columns.Add("ResolutionDate", typeof(DateTime));

            // Aggiungi righe
            foreach (var ticket in tickets)
            {
                var row = dataTable.NewRow();
                row["Key"] = ticket.Key;
                row["Summary"] = ticket.Summary;
                row["Description"] = ticket.Description;
                row["Status"] = ticket.Status;
                row["Priority"] = ticket.Priority;
                row["IssueType"] = ticket.IssueType;
                row["AssigneeDisplayName"] = ticket.AssigneeDisplayName;
                row["Organization"] = ticket.Organization;
                row["Area"] = ticket.Area;
                row["Application"] = ticket.Application;
                row["Created"] = ticket.Created != DateTime.MinValue ? ticket.Created : (object)DBNull.Value;
                row["Updated"] = ticket.Updated != DateTime.MinValue ? ticket.Updated : (object)DBNull.Value;
                row["ResolutionDate"] = ticket.ResolutionDate.HasValue ? ticket.ResolutionDate.Value : (object)DBNull.Value;

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        #endregion

        #region Event Handlers

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            var selectedTicket = SelectedTicket;
            if (selectedTicket != null)
            {
                TicketSelected?.Invoke(this, new TicketSelectedEventArgs(selectedTicket));
            }
        }

        private void OnCellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var selectedTicket = SelectedTicket;
                if (selectedTicket != null)
                {
                    // Event per aprire dettaglio ticket
                    TicketSelected?.Invoke(this, new TicketSelectedEventArgs(selectedTicket, true));
                }
            }
        }

        private void OnDataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            _logger.LogDebug($"Data binding completato: {_dataGridView.Rows.Count} righe visualizzate");
        }

        #endregion

        #region Event Args

        public class TicketSelectedEventArgs : EventArgs
        {
            public JiraTicket Ticket { get; }
            public bool IsDoubleClick { get; }

            public TicketSelectedEventArgs(JiraTicket ticket, bool isDoubleClick = false)
            {
                Ticket = ticket;
                IsDoubleClick = isDoubleClick;
            }
        }

        public class DataLoadedEventArgs : EventArgs
        {
            public JiraSearchResult Result { get; }

            public DataLoadedEventArgs(JiraSearchResult result)
            {
                Result = result;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _dataGridView.SelectionChanged -= OnSelectionChanged;
            _dataGridView.CellDoubleClick -= OnCellDoubleClick;
            _dataGridView.DataBindingComplete -= OnDataBindingComplete;
        }

        #endregion
    }
}