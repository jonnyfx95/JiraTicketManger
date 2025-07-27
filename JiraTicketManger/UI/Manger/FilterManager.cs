using JiraTicketManager.Services;
using JiraTicketManager.Data.Models;
using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.UI.Managers
{
    /// <summary>
    /// Manager per coordinare l'interazione tra filtri, ricerca e visualizzazione dati.
    /// Centralizza la logica di filtraggio e orchestrazione tra i componenti UI.
    /// </summary>
    public class FilterManager
    {
        private readonly ComboBoxManager _comboBoxManager;
        private readonly DataGridManager _dataGridManager;
        private readonly LoggingService _logger;

        // State
        private JiraSearchCriteria _lastAppliedCriteria = new();
        private DateTime _lastFilterTime = DateTime.MinValue;
        private readonly TimeSpan _filterDebounceDelay = TimeSpan.FromMilliseconds(500);

        // Events
        public event EventHandler<FilterAppliedEventArgs> FilterApplied;
        public event EventHandler FiltersCleared;
        public event EventHandler<FilterErrorEventArgs> FilterError;

        public FilterManager(ComboBoxManager comboBoxManager, DataGridManager dataGridManager)
        {
            _comboBoxManager = comboBoxManager ?? throw new ArgumentNullException(nameof(comboBoxManager));
            _dataGridManager = dataGridManager ?? throw new ArgumentNullException(nameof(dataGridManager));
            _logger = LoggingService.CreateForComponent("FilterManager");
        }

        #region Public Properties

        /// <summary>
        /// Criteri di ricerca attualmente applicati
        /// </summary>
        public JiraSearchCriteria CurrentCriteria => _lastAppliedCriteria.Clone();

        /// <summary>
        /// Verifica se ci sono filtri attivi
        /// </summary>
        public bool HasActiveFilters => _lastAppliedCriteria.HasActiveFilters();

        #endregion

        #region Public Methods

        /// <summary>
        /// Costruisce i criteri di ricerca correnti dai controlli UI
        /// </summary>
        public JiraSearchCriteria BuildSearchCriteria(
            ComboBox cmbCliente = null,
            ComboBox cmbStato = null,
            ComboBox cmbPriorita = null,
            ComboBox cmbArea = null,
            ComboBox cmbTipo = null,
            ComboBox cmbApplicativo = null,
            ComboBox cmbAssegnatario = null,
            DateTimePicker dtpCreatoDA = null,        
            DateTimePicker dtpCreatoA = null,         
            DateTimePicker dtpCompletatoDA = null,    
            DateTimePicker dtpCompletatoA = null,     
            DateTimePicker dtpDataDa = null,
            DateTimePicker dtpDataA = null,
            TextBox txtRicercaLibera = null)
        {
            var criteria = new JiraSearchCriteria
            {
                Project = "CC" // Default project
            };

            try
            {
                // Estrai valori dai ComboBox (ESISTENTE)
                if (cmbCliente != null)
                    criteria.Organization = _comboBoxManager.GetSelectedOriginalValue(cmbCliente);

                if (cmbStato != null)
                    criteria.Status = _comboBoxManager.GetSelectedOriginalValue(cmbStato);

                if (cmbPriorita != null)
                    criteria.Priority = _comboBoxManager.GetSelectedOriginalValue(cmbPriorita);

                if (cmbArea != null)
                    criteria.Area = _comboBoxManager.GetSelectedOriginalValue(cmbArea);

                if (cmbTipo != null)
                    criteria.IssueType = _comboBoxManager.GetSelectedOriginalValue(cmbTipo);

                if (cmbApplicativo != null)
                    criteria.Application = _comboBoxManager.GetSelectedOriginalValue(cmbApplicativo);

                if (cmbAssegnatario != null)
                    criteria.Assignee = _comboBoxManager.GetSelectedOriginalValue(cmbAssegnatario);

                // *** NUOVO: Estrai date creazione ***
                if (dtpCreatoDA != null && dtpCreatoDA.Checked)
                    criteria.CreatedFrom = dtpCreatoDA.Value.Date;

                if (dtpCreatoA != null && dtpCreatoA.Checked)
                    criteria.CreatedTo = dtpCreatoA.Value.Date.AddDays(1).AddTicks(-1); // Fine giornata

                // *** NUOVO: Estrai date completamento ***
                if (dtpCompletatoDA != null && dtpCompletatoDA.Checked)
                    criteria.CompletedFrom = dtpCompletatoDA.Value.Date;

                if (dtpCompletatoA != null && dtpCompletatoA.Checked)
                    criteria.CompletedTo = dtpCompletatoA.Value.Date.AddDays(1).AddTicks(-1); // Fine giornata

                // Ricerca libera (ESISTENTE)
                if (txtRicercaLibera != null && !string.IsNullOrWhiteSpace(txtRicercaLibera.Text))
                    criteria.FreeText = txtRicercaLibera.Text.Trim();

                _logger.LogDebug($"Criteri costruiti: {SerializeCriteria(criteria)}");
                return criteria;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore costruzione criteri ricerca", ex);
                throw;
            }
        }

        /// <summary>
        /// Applica i criteri di ricerca con debouncing automatico
        /// </summary>
        public async Task ApplyFiltersAsync(JiraSearchCriteria criteria, bool forceImmediate = false)
        {
            try
            {
                _lastFilterTime = DateTime.Now;

                if (!forceImmediate)
                {
                    // Debouncing: aspetta prima di applicare
                    await Task.Delay(_filterDebounceDelay);

                    // Se nel frattempo sono arrivati altri filtri, annulla questo
                    if (DateTime.Now - _lastFilterTime < _filterDebounceDelay)
                        return;
                }

                _logger.LogInfo("Applicazione filtri");

                // Salva criteri
                _lastAppliedCriteria = criteria.Clone();

                // Applica ricerca
                await _dataGridManager.SearchAsync(criteria);

                // Notifica evento
                FilterApplied?.Invoke(this, new FilterAppliedEventArgs(criteria));

                _logger.LogInfo($"Filtri applicati: {CountActiveFilters(criteria)} filtri attivi");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore applicazione filtri", ex);
                FilterError?.Invoke(this, new FilterErrorEventArgs("Errore applicazione filtri", ex));
                throw;
            }
        }

        /// <summary>
        /// Applica una JQL personalizzata
        /// </summary>
        public async Task ApplyCustomJQLAsync(string jql)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jql))
                    throw new ArgumentException("JQL non può essere vuota");

                // Valida JQL di base
                if (!JQLBuilder.IsValidJQL(jql))
                    throw new ArgumentException("JQL non valida");

                _logger.LogInfo($"Applicazione JQL personalizzata: {jql}");

                // Pulisci JQL
                var cleanJql = JQLBuilder.CleanJQL(jql);

                // Applica ricerca
                await _dataGridManager.SearchAsync(cleanJql);

                // Reset criteri strutturati (JQL personalizzata li sovrascrive)
                _lastAppliedCriteria = new JiraSearchCriteria { CustomJQL = cleanJql };

                FilterApplied?.Invoke(this, new FilterAppliedEventArgs(_lastAppliedCriteria));
                _logger.LogInfo("JQL personalizzata applicata");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore JQL personalizzata: {jql}", ex);
                FilterError?.Invoke(this, new FilterErrorEventArgs($"Errore JQL: {ex.Message}", ex));
                throw;
            }
        }

        /// <summary>
        /// Applica un filtro rapido predefinito
        /// </summary>
        public async Task ApplyQuickFilterAsync(QuickFilterType filterType)
        {
            try
            {
                _logger.LogInfo($"Applicazione quick filter: {filterType}");

                var jql = JQLBuilder.CreateDefault().ApplyQuickFilter(filterType).Build();
                await ApplyCustomJQLAsync(jql);

                _logger.LogInfo($"Quick filter {filterType} applicato");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore quick filter {filterType}", ex);
                throw;
            }
        }

        /// <summary>
        /// Pulisce tutti i filtri e torna ai dati iniziali
        /// </summary>
        public async Task ClearAllFiltersAsync()
        {
            try
            {
                _logger.LogInfo("Pulizia tutti i filtri");

                // Reset ComboBox
                _comboBoxManager.ResetAll();

                // Reset criteri
                _lastAppliedCriteria.Reset();

                // Carica dati iniziali
                await _dataGridManager.LoadInitialDataAsync();

                FiltersCleared?.Invoke(this, EventArgs.Empty);
                _logger.LogInfo("Tutti i filtri puliti");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia filtri", ex);
                FilterError?.Invoke(this, new FilterErrorEventArgs("Errore pulizia filtri", ex));
                throw;
            }
        }

        /// <summary>
        /// Salva i filtri correnti come preset
        /// </summary>
        public FilterPreset SaveCurrentFiltersAsPreset(string name)
        {
            try
            {
                var preset = new FilterPreset
                {
                    Name = name,
                    Criteria = _lastAppliedCriteria.Clone(),
                    CreatedDate = DateTime.Now
                };

                _logger.LogInfo($"Preset salvato: {name}");
                return preset;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore salvataggio preset {name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Applica un preset di filtri salvato
        /// </summary>
        public async Task ApplyFilterPresetAsync(FilterPreset preset)
        {
            try
            {
                if (preset?.Criteria == null)
                    throw new ArgumentException("Preset non valido");

                _logger.LogInfo($"Applicazione preset: {preset.Name}");

                await ApplyFiltersAsync(preset.Criteria, forceImmediate: true);

                _logger.LogInfo($"Preset {preset.Name} applicato");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore applicazione preset {preset?.Name}", ex);
                throw;
            }
        }

        /// <summary>
        /// Ottiene statistiche sui filtri correnti
        /// </summary>
        public async Task<FilterStatistics> GetFilterStatisticsAsync()
        {
            try
            {
                var result = _dataGridManager.LastSearchResult;
                if (result == null) return new FilterStatistics();

                return new FilterStatistics
                {
                    TotalResults = result.Total,
                    CurrentPage = result.CurrentPage,
                    TotalPages = result.TotalPages,
                    ActiveFiltersCount = CountActiveFilters(_lastAppliedCriteria),
                    HasCustomJQL = !string.IsNullOrEmpty(_lastAppliedCriteria.CustomJQL),
                    LastAppliedTime = _lastFilterTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore calcolo statistiche filtri", ex);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private int CountActiveFilters(JiraSearchCriteria criteria)
        {
            var count = 0;
            if (!string.IsNullOrEmpty(criteria.Organization)) count++;
            if (!string.IsNullOrEmpty(criteria.Status)) count++;
            if (!string.IsNullOrEmpty(criteria.Priority)) count++;
            if (!string.IsNullOrEmpty(criteria.IssueType)) count++;
            if (!string.IsNullOrEmpty(criteria.Area)) count++;
            if (!string.IsNullOrEmpty(criteria.Application)) count++;
            if (!string.IsNullOrEmpty(criteria.Assignee)) count++;
            if (criteria.CreatedFrom.HasValue) count++;
            if (criteria.CreatedTo.HasValue) count++;
            if (criteria.UpdatedFrom.HasValue) count++;
            if (criteria.UpdatedTo.HasValue) count++;
            if (criteria.CompletedFrom.HasValue) count++;   
            if (criteria.CompletedTo.HasValue) count++;      
            if (!string.IsNullOrEmpty(criteria.FreeText)) count++;
            if (!string.IsNullOrEmpty(criteria.CustomJQL)) count++;
            return count;
        }

        private string SerializeCriteria(JiraSearchCriteria criteria)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(criteria.Organization)) parts.Add($"Org:{criteria.Organization}");
            if (!string.IsNullOrEmpty(criteria.Status)) parts.Add($"Status:{criteria.Status}");
            if (!string.IsNullOrEmpty(criteria.Priority)) parts.Add($"Priority:{criteria.Priority}");
            if (criteria.CreatedFrom.HasValue) parts.Add($"CreatedFrom:{criteria.CreatedFrom:yyyy-MM-dd}");
            if (criteria.CreatedTo.HasValue) parts.Add($"CreatedTo:{criteria.CreatedTo:yyyy-MM-dd}");
            if (criteria.CompletedFrom.HasValue) parts.Add($"CompletedFrom:{criteria.CompletedFrom:yyyy-MM-dd}");  
            if (criteria.CompletedTo.HasValue) parts.Add($"CompletedTo:{criteria.CompletedTo:yyyy-MM-dd}");       
            if (!string.IsNullOrEmpty(criteria.FreeText)) parts.Add($"Text:{criteria.FreeText}");
            return string.Join(", ", parts);
        }

        #endregion

        #region Supporting Classes

        /// <summary>
        /// Preset di filtri salvati
        /// </summary>
        public class FilterPreset
        {
            public string Name { get; set; } = "";
            public JiraSearchCriteria Criteria { get; set; } = new();
            public DateTime CreatedDate { get; set; }
            public string Description { get; set; } = "";
        }

        /// <summary>
        /// Statistiche sui filtri
        /// </summary>
        public class FilterStatistics
        {
            public int TotalResults { get; set; }
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
            public int ActiveFiltersCount { get; set; }
            public bool HasCustomJQL { get; set; }
            public DateTime LastAppliedTime { get; set; }
        }

        /// <summary>
        /// Event args per filtri applicati
        /// </summary>
        public class FilterAppliedEventArgs : EventArgs
        {
            public JiraSearchCriteria Criteria { get; }
            public DateTime AppliedTime { get; }

            public FilterAppliedEventArgs(JiraSearchCriteria criteria)
            {
                Criteria = criteria;
                AppliedTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Event args per errori filtri
        /// </summary>
        public class FilterErrorEventArgs : EventArgs
        {
            public string Message { get; }
            public Exception Exception { get; }

            public FilterErrorEventArgs(string message, Exception exception)
            {
                Message = message;
                Exception = exception;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _logger.LogInfo("FilterManager disposed");
        }

        #endregion
    }
}