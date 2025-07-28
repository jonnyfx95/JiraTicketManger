using JiraTicketManager.Business;
using JiraTicketManager.Data;
using JiraTicketManager.Data.Models;
using JiraTicketManager.Helpers;
using JiraTicketManager.Services;
using JiraTicketManager.Services;
using JiraTicketManager.Tools;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.UI.Managers
{
    /// <summary>
    /// Manager generico per gestire il caricamento e la gestione delle ComboBox Jira.
    /// Componente riutilizzabile per qualsiasi campo Jira.
    /// </summary>
    public class ComboBoxManager
    {
        private readonly IJiraDataService _dataService;
        private readonly LoggingService _logger;
        private readonly Dictionary<ComboBox, ComboBoxInfo> _comboBoxInfos = new();
        private readonly Dictionary<ComboBox, Dictionary<string, string>> _valueMappings = new();

        // autocomplete

        private readonly Dictionary<ComboBox, List<string>> _originalDisplayItems = new();
        private readonly Dictionary<ComboBox, System.Windows.Forms.Timer> _filterTimers = new();
        private readonly Dictionary<ComboBox, bool> _autoCompleteEnabled = new();

        public ComboBoxManager(IJiraDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = LoggingService.CreateForComponent("ComboBoxManager");
        }

        #region Public Methods

        /// <summary>
        /// Carica i dati per una ComboBox specifica in modo asincrono
        /// </summary>
        /// <param name="comboBox">ComboBox da popolare</param>
        /// <param name="fieldType">Tipo di campo Jira</param>
        /// <param name="defaultText">Testo di default (es. "Tutti i clienti")</param>
        /// <param name="progress">Report di progresso</param>
        public async Task LoadAsync(ComboBox comboBox, JiraFieldType fieldType,
           string defaultText = null, IProgress<string> progress = null, string ticketKey = null)
        {
            if (comboBox == null) throw new ArgumentNullException(nameof(comboBox));

            try
            {
                _logger.LogInfo($"Caricamento {fieldType} per ComboBox '{comboBox.Name}'");

                // Registra la ComboBox
                RegisterComboBox(comboBox, fieldType, defaultText);

                // Mostra loading
                SetLoadingState(comboBox, true);

                // ✅ MODIFICA: Passa ticketKey al DataService
                var fields = await _dataService.GetFieldValuesAsync(fieldType, progress, ticketKey);

                // Popola la ComboBox
                PopulateComboBox(comboBox, fields, defaultText);

                _logger.LogInfo($"Caricamento completato: {fields.Count} elementi per {fieldType}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento {fieldType}", ex);
                SetErrorState(comboBox, $"Errore caricamento {JiraFieldTypeHelper.GetDisplayName(fieldType)}");
                throw;
            }
            finally
            {
                SetLoadingState(comboBox, false);
            }
        }

        /// <summary>
        /// Carica più ComboBox in parallelo
        /// </summary>
        public async Task LoadMultipleAsync(IEnumerable<ComboBoxLoadRequest> requests,
            IProgress<string> progress = null)
        {
            var requestList = requests.ToList();
            var tasks = requestList.Select(request =>
                LoadAsync(request.ComboBox, request.FieldType, request.DefaultText, progress)
            ).ToArray();

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInfo($"Caricamento parallelo completato per {requestList.Count} ComboBox");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore caricamento parallelo", ex);
                throw;
            }
        }

        /// <summary>
        /// Ricarica i dati per una ComboBox
        /// </summary>
        public async Task RefreshAsync(ComboBox comboBox, IProgress<string> progress = null)
        {
            if (!_comboBoxInfos.TryGetValue(comboBox, out var info))
            {
                throw new InvalidOperationException($"ComboBox '{comboBox.Name}' non registrata");
            }

            await LoadAsync(comboBox, info.FieldType, info.DefaultText, progress);
        }

        /// <summary>
        /// Ottiene il valore originale Jira dal valore visualizzato
        /// </summary>
        public string GetOriginalValue(ComboBox comboBox, string displayValue)
        {
            if (string.IsNullOrEmpty(displayValue)) return "";

            if (_valueMappings.TryGetValue(comboBox, out var mapping) &&
                mapping.TryGetValue(displayValue, out var originalValue))
            {
                return originalValue;
            }

            return displayValue; // Fallback al valore display
        }

        /// <summary>
        /// Ottiene il valore selezionato originale
        /// </summary>
        public string GetSelectedOriginalValue(ComboBox comboBox)
        {
            var selectedText = comboBox.Text;
            return GetOriginalValue(comboBox, selectedText);
        }

        /// <summary>
        /// Imposta il valore selezionato usando il valore originale
        /// </summary>
        public void SetSelectedValue(ComboBox comboBox, string originalValue)
        {
            if (string.IsNullOrEmpty(originalValue))
            {
                comboBox.SelectedIndex = 0; // Default
                return;
            }

            if (_valueMappings.TryGetValue(comboBox, out var mapping))
            {
                var displayValue = mapping.FirstOrDefault(kvp => kvp.Value == originalValue).Key;
                if (!string.IsNullOrEmpty(displayValue))
                {
                    comboBox.Text = displayValue;
                    return;
                }
            }

            // Fallback: cerca direttamente
            var index = comboBox.Items.Cast<string>().ToList().FindIndex(item =>
                string.Equals(item, originalValue, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }
        }

        /// <summary>
        /// Reset di tutte le ComboBox registrate al valore di default
        /// </summary>
        public void ResetAll()
        {
            foreach (var kvp in _comboBoxInfos)
            {
                kvp.Key.SelectedIndex = 0;
            }
            _logger.LogInfo("Reset di tutte le ComboBox completato");
        }

        /// <summary>
        /// Verifica se una ComboBox ha un valore selezionato (diverso dal default)
        /// </summary>
        public bool HasSelectedValue(ComboBox comboBox)
        {
            return comboBox.SelectedIndex > 0 && !string.IsNullOrWhiteSpace(comboBox.Text);
        }


        #region Autocomplete Methods
        // <summary>
        /// Abilita AutoComplete con filtering per una ComboBox
        /// Funziona sia con ComboBox gestite (con mapping) che statiche
        /// </summary>
        public void EnableAutoComplete(ComboBox comboBox)
        {
            try
            {
                if (comboBox == null) return;

                var comboName = comboBox.Name ?? "Unknown";
                _logger.LogDebug($"🔤 Abilitazione AutoComplete per: {comboName}");

                // 1. Converti a DropDown per permettere digitazione
                comboBox.DropDownStyle = ComboBoxStyle.DropDown;

                // 2. Salva items originali (Display Values)
                var originalItems = new List<string>();
                foreach (var item in comboBox.Items)
                {
                    originalItems.Add(item.ToString());
                }
                _originalDisplayItems[comboBox] = originalItems;

                // 3. Crea timer debouncing (300ms)
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 300;
                timer.Tick += (s, e) => OnAutoCompleteFilterTick(comboBox, timer);
                _filterTimers[comboBox] = timer;

                // 4. Aggiungi event handlers
                comboBox.TextChanged += (s, e) => OnAutoCompleteTextChanged(comboBox);
                comboBox.Enter += OnAutoCompleteEnter;
                comboBox.Leave += OnAutoCompleteLeave;

                // 5. Marca come abilitato
                _autoCompleteEnabled[comboBox] = true;

                _logger.LogDebug($"✅ AutoComplete abilitato per {comboName}: {originalItems.Count} items");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore abilitazione AutoComplete per {comboBox?.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Abilita AutoComplete per multiple ComboBox
        /// </summary>
        public void EnableAutoCompleteForAll(params ComboBox[] comboBoxes)
        {
            foreach (var combo in comboBoxes)
            {
                if (combo != null)
                {
                    EnableAutoComplete(combo);
                }
            }
        }

        /// <summary>
        /// Disabilita AutoComplete per una ComboBox
        /// </summary>
        public void DisableAutoComplete(ComboBox comboBox)
        {
            try
            {
                if (comboBox == null || !_autoCompleteEnabled.ContainsKey(comboBox)) return;

                var comboName = comboBox.Name ?? "Unknown";
                _logger.LogDebug($"🚫 Disabilitazione AutoComplete per: {comboName}");

                // Rimuovi event handlers
                comboBox.TextChanged -= (s, e) => OnAutoCompleteTextChanged(comboBox);
                comboBox.Enter -= OnAutoCompleteEnter;
                comboBox.Leave -= OnAutoCompleteLeave;

                // Ferma e disponi timer
                if (_filterTimers.TryGetValue(comboBox, out var timer))
                {
                    timer.Stop();
                    timer.Dispose();
                    _filterTimers.Remove(comboBox);
                }

                // Ripristina DropDownList
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                // Ripristina items originali
                if (_originalDisplayItems.TryGetValue(comboBox, out var originalItems))
                {
                    RestoreAllDisplayItems(comboBox, originalItems);
                    _originalDisplayItems.Remove(comboBox);
                }

                // Cleanup
                _autoCompleteEnabled.Remove(comboBox);

                _logger.LogDebug($"✅ AutoComplete disabilitato per {comboName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore disabilitazione AutoComplete: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Aggiorna items di una ComboBox mantenendo AutoComplete
        /// Chiamato automaticamente quando PopulateComboBox viene eseguito
        /// </summary>
        public void RefreshAutoCompleteItems(ComboBox comboBox)
        {
            try
            {
                if (!_autoCompleteEnabled.ContainsKey(comboBox)) return;

                // Aggiorna lista items originali
                var newItems = new List<string>();
                foreach (var item in comboBox.Items)
                {
                    newItems.Add(item.ToString());
                }
                _originalDisplayItems[comboBox] = newItems;

                _logger.LogDebug($"🔄 AutoComplete items aggiornati per {comboBox.Name}: {newItems.Count} items");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore refresh AutoComplete: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gestisce TextChanged con debouncing
        /// </summary>
        private void OnAutoCompleteTextChanged(ComboBox comboBox)
        {
            try
            {
                if (!_filterTimers.TryGetValue(comboBox, out var timer)) return;

                // Riavvia timer per debouncing
                timer.Stop();
                timer.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore AutoComplete TextChanged per {comboBox.Name}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Esegue il filtering dopo il debouncing
        /// </summary>
        private void OnAutoCompleteFilterTick(ComboBox comboBox, System.Windows.Forms.Timer timer)
        {
            try
            {
                timer.Stop();
                if (!_originalDisplayItems.TryGetValue(comboBox, out var originalItems)) return;

                var searchText = comboBox.Text?.Trim() ?? "";

                // Se testo vuoto, ripristina tutti gli items
                if (string.IsNullOrEmpty(searchText))
                {
                    RestoreAllDisplayItems(comboBox, originalItems);
                    return;
                }

                // 🚀 SMART FILTERING: Diverso comportamento per ComboBox gestite vs statiche
                var isManaged = _valueMappings.ContainsKey(comboBox);
                var filteredItems = new List<string>();

                foreach (var item in originalItems)
                {
                    bool matches = false;

                    if (isManaged)
                    {
                        // 🎯 ComboBox GESTITE (Area, Applicativo, Cliente): Smart filtering
                        matches = MatchesManagedComboBox(item, searchText);
                    }
                    else
                    {
                        // 🎯 ComboBox STATICHE (Tipo, Stato, Priorità, Assegnatario): Filtering normale
                        matches = item.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                    }

                    if (matches)
                    {
                        filteredItems.Add(item);
                    }
                }

                // Aggiorna ComboBox con items filtrati USANDO IL TUO METODO PROTETTO
                UpdateFilteredDisplayItems(comboBox, filteredItems, searchText);

                _logger.LogDebug($"🔍 AutoComplete filtering per {comboBox.Name}: '{searchText}' → {filteredItems.Count} risultati (Smart: {isManaged})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore AutoComplete filtering per {comboBox.Name}: {ex.Message}", ex);
            }
        }

        // <summary>
        /// Logica di matching intelligente per ComboBox gestite (Area, Applicativo, Cliente)
        /// </summary>
        private bool MatchesManagedComboBox(string item, string searchText)
        {
            // 🎯 LOGICA SMART: Cerca nella parte più significativa dell'item

            // Per items che iniziano con "Civilia Next - Area " cerca dopo "Area "
            if (item.StartsWith("Civilia Next - Area "))
            {
                var cleanPart = item.Replace("Civilia Next - Area ", "");
                return cleanPart.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Per items che contengono " -> " cerca nella parte dopo "->"
            if (item.Contains(" -> "))
            {
                var parts = item.Split(new[] { " -> " }, 2, StringSplitOptions.None);
                var appPart = parts[1].Trim();
                return appPart.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Per items che contengono " - " cerca dopo il primo " - "
            if (item.Contains(" - "))
            {
                var dashIndex = item.IndexOf(" - ");
                var afterDash = item.Substring(dashIndex + 3);

                // Se c'è "Area " dopo il dash, cerca dopo "Area "
                if (afterDash.StartsWith("Area "))
                {
                    var cleanPart = afterDash.Replace("Area ", "");
                    return cleanPart.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
                }

                // Altrimenti cerca nella parte dopo " - "
                return afterDash.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // Per tutti gli altri casi: ricerca normale nell'intero testo
            return item.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // <summary>
        /// Aggiorna items della ComboBox mantenendo il testo digitato
        /// </summary>
        private void UpdateFilteredDisplayItems(ComboBox comboBox, List<string> filteredItems, string currentText)
        {
            try
            {
                // 🔧 PROTEZIONE LOOP: Se già stiamo aggiornando, esci
                if (comboBox.Tag?.ToString() == "UPDATING") return;

                // Salva posizione cursore
                var selectionStart = comboBox.SelectionStart;

                // 🔧 MARCA COME "IN AGGIORNAMENTO"
                comboBox.Tag = "UPDATING";

                // 🔧 RIMUOVI TUTTI GLI EVENT HANDLERS
                if (_filterTimers.TryGetValue(comboBox, out var timer))
                {
                    comboBox.TextChanged -= (s, e) => OnAutoCompleteTextChanged(comboBox);
                    comboBox.Enter -= OnAutoCompleteEnter;
                    comboBox.Leave -= OnAutoCompleteLeave;
                }

                // Aggiorna items
                comboBox.Items.Clear();
                foreach (var item in filteredItems)
                {
                    comboBox.Items.Add(item);
                }

                // Ripristina testo e cursore
                comboBox.Text = currentText;
                comboBox.SelectionStart = Math.Min(selectionStart, currentText.Length);
                comboBox.SelectionLength = 0;

                // 🔧 RIABILITA EVENT HANDLERS
                if (_filterTimers.TryGetValue(comboBox, out timer))
                {
                    comboBox.TextChanged += (s, e) => OnAutoCompleteTextChanged(comboBox);
                    comboBox.Enter += OnAutoCompleteEnter;
                    comboBox.Leave += OnAutoCompleteLeave;
                }

                // 🔧 RIMUOVI MARCA "IN AGGIORNAMENTO"
                comboBox.Tag = null;

                // Mostra dropdown se ci sono risultati e NON è già aperto
                if (filteredItems.Count > 0 && filteredItems.Count < 50 && !comboBox.DroppedDown)
                {
                    // 🔧 DELAY per evitare conflitti UI
                    var dropTimer = new System.Windows.Forms.Timer();
                    dropTimer.Interval = 100;
                    dropTimer.Tick += (s, e) =>
                    {
                        dropTimer.Stop();
                        dropTimer.Dispose();
                        try
                        {
                            if (!comboBox.DroppedDown && comboBox.Focused)
                            {
                                comboBox.DroppedDown = true;
                            }
                        }
                        catch { /* Ignora errori dropdown */ }
                    };
                    dropTimer.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiornamento filtered items: {ex.Message}", ex);
                // 🔧 CLEANUP in caso di errore
                comboBox.Tag = null;
            }
        }

        /// <summary>
        /// Ripristina tutti gli items Display originali
        /// </summary>
        private void RestoreAllDisplayItems(ComboBox comboBox, List<string> originalItems)
        {
            try
            {
                var currentText = comboBox.Text;
                var selectionStart = comboBox.SelectionStart;

                // Disabilita eventi
                comboBox.TextChanged -= (s, e) => OnAutoCompleteTextChanged(comboBox);

                comboBox.Items.Clear();
                foreach (var item in originalItems)
                {
                    comboBox.Items.Add(item);
                }

                comboBox.Text = currentText;
                comboBox.SelectionStart = selectionStart;

                // Riabilita eventi
                comboBox.TextChanged += (s, e) => OnAutoCompleteTextChanged(comboBox);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore ripristino items: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gestisce focus enter - ripristina tutti gli items
        /// </summary>
        private void OnAutoCompleteEnter(object sender, EventArgs e)
        {
            try
            {
                if (sender is ComboBox combo && _originalDisplayItems.TryGetValue(combo, out var originalItems))
                {
                    RestoreAllDisplayItems(combo, originalItems);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore AutoComplete Enter: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gestisce focus leave - valida selezione
        /// </summary>
        private void OnAutoCompleteLeave(object sender, EventArgs e)
        {
            try
            {
                if (sender is ComboBox combo && _originalDisplayItems.TryGetValue(combo, out var originalItems))
                {
                    // Se il testo non corrisponde a nessun item originale, seleziona il primo (default)
                    if (!originalItems.Contains(combo.Text) && originalItems.Count > 0)
                    {
                        // Ripristina tutti gli items e seleziona default
                        RestoreAllDisplayItems(combo, originalItems);
                        combo.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore AutoComplete Leave: {ex.Message}", ex);
            }
        }

        #endregion

        #endregion



        #region Private Methods

        private void RegisterComboBox(ComboBox comboBox, JiraFieldType fieldType, string defaultText)
        {
            var info = new ComboBoxInfo
            {
                FieldType = fieldType,
                DefaultText = defaultText ?? GetDefaultText(fieldType),
                LastLoadTime = DateTime.MinValue
            };

            _comboBoxInfos[comboBox] = info;

            // Ensure proper event handling
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void PopulateComboBox(ComboBox comboBox, List<JiraField> fields, string defaultText)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(() => PopulateComboBox(comboBox, fields, defaultText));
                return;
            }
            // Preserva selezione corrente
            var currentSelection = comboBox.Text;
            // Pulisci e crea mapping
            comboBox.Items.Clear();
            var mapping = new Dictionary<string, string>();
            // Aggiungi default
            var defaultDisplayText = defaultText ?? GetDefaultText(_comboBoxInfos[comboBox].FieldType);
            comboBox.Items.Add(defaultDisplayText);
            mapping[defaultDisplayText] = "";
            // Aggiungi valori ordinati
            var sortedFields = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.DisplayValue))
                .OrderBy(f => f.DisplayValue)
                .ToList();
            foreach (var field in sortedFields)
            {
                var displayValue = CleanDisplayValue(field.DisplayValue);
                // Evita duplicati
                if (!mapping.ContainsKey(displayValue))
                {
                    comboBox.Items.Add(displayValue);
                    mapping[displayValue] = field.Value;
                }
            }
            // Salva mapping
            _valueMappings[comboBox] = mapping;
            // Ripristina selezione o seleziona default
            if (!string.IsNullOrEmpty(currentSelection) && comboBox.Items.Contains(currentSelection))
            {
                comboBox.Text = currentSelection;
            }
            else
            {
                comboBox.SelectedIndex = 0;
            }
            // Aggiorna info
            _comboBoxInfos[comboBox].LastLoadTime = DateTime.Now;
            _comboBoxInfos[comboBox].ItemCount = fields.Count;

            // *** NUOVO: Aggiorna AutoComplete se abilitato ***
            RefreshAutoCompleteItems(comboBox);
        }

        private void SetLoadingState(ComboBox comboBox, bool isLoading)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(() => SetLoadingState(comboBox, isLoading));
                return;
            }

            if (isLoading)
            {
                comboBox.Items.Clear();
                comboBox.Items.Add("Caricamento...");
                comboBox.SelectedIndex = 0;
                comboBox.Enabled = false;
            }
            else
            {
                comboBox.Enabled = true;
            }
        }

        private void SetErrorState(ComboBox comboBox, string errorMessage)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(() => SetErrorState(comboBox, errorMessage));
                return;
            }

            comboBox.Items.Clear();
            comboBox.Items.Add($"Errore: {errorMessage}");
            comboBox.SelectedIndex = 0;
            comboBox.Enabled = true;
        }

        private string CleanDisplayValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value.Replace("_", " ")
                       .Replace(".", " ")
                       .Trim();
        }

        private string GetDefaultText(JiraFieldType fieldType)
        {
            return fieldType switch
            {
                JiraFieldType.Organization => "Tutti i clienti",
                JiraFieldType.Status => "Tutti gli stati",
                JiraFieldType.Priority => "Tutte le priorità",
                JiraFieldType.IssueType => "Tutti i tipi",
                JiraFieldType.Area => "Tutte le aree",
                JiraFieldType.Application => "Tutte le applicazioni",
                JiraFieldType.Assignee => "Tutti gli assegnatari",
                JiraFieldType.Consulente => "Tutti i consulenti", 
                _ => $"Tutti i {JiraFieldTypeHelper.GetDisplayName(fieldType).ToLower()}"
            };
        }

        #endregion

        #region Supporting Classes

        /// <summary>
        /// Informazioni su una ComboBox registrata
        /// </summary>
        private class ComboBoxInfo
        {
            public JiraFieldType FieldType { get; set; }
            public string DefaultText { get; set; }
            public DateTime LastLoadTime { get; set; }
            public int ItemCount { get; set; }
        }

        /// <summary>
        /// Richiesta di caricamento per una ComboBox
        /// </summary>
        public class ComboBoxLoadRequest
        {
            public ComboBox ComboBox { get; set; }
            public JiraFieldType FieldType { get; set; }
            public string DefaultText { get; set; }

            public ComboBoxLoadRequest(ComboBox comboBox, JiraFieldType fieldType, string defaultText = null)
            {
                ComboBox = comboBox;
                FieldType = fieldType;
                DefaultText = defaultText;
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _comboBoxInfos.Clear();
            _valueMappings.Clear();
        }

        #endregion


        #region Dependency Management

        /// <summary>
        /// Informazioni su una dipendenza parent-child tra ComboBox
        /// </summary>
        private class ComboBoxDependency
        {
            public ComboBox ParentComboBox { get; set; }
            public ComboBox ChildComboBox { get; set; }
            public JiraFieldType ChildFieldType { get; set; }
            public string ChildDefaultText { get; set; }
            public List<JiraField> AllChildData { get; set; } = new();
        }

        // Dizionario per gestire dipendenze registrate
        private readonly Dictionary<ComboBox, ComboBoxDependency> _dependencies = new();

        /// <summary>
        /// Carica ComboBox Applicativo con dipendenza da Area
        /// </summary>
        public async Task LoadWithAreaDependency(ComboBox cmbArea, ComboBox cmbApplicativo,
            string areaDefaultText = null, string applicativoDefaultText = null,
            IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo("🔗 Caricamento ComboBox con dipendenza Area → Applicativo");

                // 1. Carica Area normalmente
                progress?.Report("Caricamento aree...");
                await LoadAsync(cmbArea, JiraFieldType.Area,
                    areaDefaultText ?? "-- Tutte le Aree --", progress);

                // 2. Carica TUTTI gli applicativi (per avere il dataset completo)
                progress?.Report("Caricamento applicativi...");
                var allApplicativi = await _dataService.GetFieldValuesAsync(JiraFieldType.Application, progress);

                // 3. Registra la dipendenza
                var dependency = new ComboBoxDependency
                {
                    ParentComboBox = cmbArea,
                    ChildComboBox = cmbApplicativo,
                    ChildFieldType = JiraFieldType.Application,
                    ChildDefaultText = applicativoDefaultText ?? "-- Seleziona un'area --",
                    AllChildData = allApplicativi
                };

                _dependencies[cmbArea] = dependency;
                RegisterComboBox(cmbApplicativo, JiraFieldType.Application, dependency.ChildDefaultText);

                // 4. Imposta stato iniziale: Applicativo DISABILITATO
                SetDependencyState(cmbApplicativo, false, dependency.ChildDefaultText);

                // 5. Configura event handler per la dipendenza
                cmbArea.SelectedIndexChanged += OnParentSelectionChanged;

                _logger.LogInfo($"✅ Dipendenza configurata: {allApplicativi.Count} applicativi disponibili");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore configurazione dipendenza Area → Applicativo", ex);
                throw;
            }
        }

        /// <summary>
        /// Event handler per cambi selezione del parent (Area)
        /// </summary>
        private void OnParentSelectionChanged(object sender, EventArgs e)
        {
            try
            {
                var parentCombo = sender as ComboBox;
                if (parentCombo == null || !_dependencies.TryGetValue(parentCombo, out var dependency))
                    return;

                // 🔧 CORREZIONE: Usa VALORE ORIGINALE per il parsing
                var selectedAreaOriginal = GetSelectedOriginalValue(parentCombo);
                var selectedAreaDisplay = GetSelectedDisplayValue(parentCombo);

                _logger.LogDebug($"🔄 [EVENT] Area selezionata (display): '{selectedAreaDisplay}'");
                _logger.LogDebug($"🔄 [EVENT] Area selezionata (original): '{selectedAreaOriginal}'");

                // Se è il valore di default, disabilita il child
                if (string.IsNullOrEmpty(selectedAreaDisplay) || selectedAreaDisplay.StartsWith("--") || selectedAreaDisplay.StartsWith("Tutte"))
                {
                    SetDependencyState(dependency.ChildComboBox, false, dependency.ChildDefaultText);
                    _logger.LogDebug("🔒 Applicativo disabilitato (nessuna area selezionata)");
                    return;
                }

                // 🔧 CORREZIONE: Estrai area pulita dal valore ORIGINALE dell'area
                var selectedAreaClean = ExtractAreaNameFromAreaValue(selectedAreaOriginal);
                _logger.LogDebug($"🔄 [PARSER] Area estratta per filtro: '{selectedAreaClean}'");

                // Filtra applicativi per l'area selezionata usando l'area PULITA
                var applicativiFiltered = FilterApplicativiByArea(dependency.AllChildData, selectedAreaClean);

                // Popola e abilita il child
                PopulateChildComboBox(dependency.ChildComboBox, applicativiFiltered, dependency.ChildDefaultText);
                SetDependencyState(dependency.ChildComboBox, true, null);

                _logger.LogInfo($"✅ Applicativo abilitato: {applicativiFiltered.Count} applicativi per area '{selectedAreaClean}' (da '{selectedAreaOriginal}')");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore gestione dipendenza parent-child", ex);
            }
        }


        /// <summary>
        /// Estrae il nome pulito dell'area dal valore originale dell'area
        /// </summary>
        private string ExtractAreaNameFromAreaValue(string areaValue)
        {
            if (string.IsNullOrEmpty(areaValue))
                return null;

            // Pattern per valori Area:
            // "Civilia Next - Area Demografia" → "Demografia"
            if (areaValue.StartsWith("Civilia Next - Area "))
            {
                return areaValue.Replace("Civilia Next - Area ", "");
            }

            // "Civilia Next Area Comune" → "Area Comune"
            if (areaValue == "Civilia Next Area Comune")
            {
                return "Area Comune";
            }

            // "Sistema Informativo Territoriale" → "Sistema Informativo Territoriale"
            if (areaValue == "Sistema Informativo Territoriale")
            {
                return "Sistema Informativo Territoriale";
            }

            // "Customer Care" → "Customer Care"
            if (areaValue == "Customer Care")
            {
                return "Customer Care";
            }

            // "Civilia Next - Servizi On-Line" → "Servizi On-Line"
            if (areaValue.StartsWith("Civilia Next - "))
            {
                return areaValue.Replace("Civilia Next - ", "");
            }

            // Altri casi: restituisci come è
            return areaValue;
        }



        /// <summary>
        /// Filtra applicativi in base all'area selezionata
        /// </summary>
        /// <summary>
        /// Filtra applicativi in base all'area selezionata
        /// </summary>
        private List<JiraField> FilterApplicativiByArea(List<JiraField> allApplicativi, string selectedArea)
        {
            _logger.LogInfo($"🔍 === DEBUG FILTRO AREA ===");
            _logger.LogInfo($"🔍 Area selezionata: '{selectedArea}'");
            _logger.LogInfo($"🔍 Totale applicativi da filtrare: {allApplicativi.Count}");

            var filtered = new List<JiraField>();
            int matchCount = 0;
            int senzaAreaCount = 0;

            for (int i = 0; i < allApplicativi.Count; i++)
            {
                var app = allApplicativi[i];
                var appArea = AreaApplicativoMappingGenerator.ExtractAreaFromApplicativo(app.Value);

                // Debug primi 10 applicativi
                if (i < 10)
                {
                    _logger.LogInfo($"🔍 [{i + 1:D2}] App: '{app.Value}' → Area: '{appArea}'");
                }

                // Aggiungi se appartiene all'area selezionata
                if (string.Equals(appArea, selectedArea, StringComparison.OrdinalIgnoreCase))
                {
                    var displayName = AreaApplicativoMappingGenerator.ExtractApplicativoDisplayName(app.Value);
                    filtered.Add(new JiraField { Value = app.Value, DisplayValue = displayName });
                    matchCount++;

                    if (matchCount <= 5)
                    {
                        _logger.LogInfo($"🎯 MATCH [{matchCount}]: '{app.Value}' → Display: '{displayName}'");
                    }
                }
                // SEMPRE aggiungi applicativi senza area
                else if (appArea == null)
                {
                    var displayName = AreaApplicativoMappingGenerator.ExtractApplicativoDisplayName(app.Value);
                    filtered.Add(new JiraField { Value = app.Value, DisplayValue = displayName });
                    senzaAreaCount++;

                    if (senzaAreaCount <= 3)
                    {
                        _logger.LogDebug($"🔄 SENZA AREA [{senzaAreaCount}]: '{app.Value}' → Display: '{displayName}'");
                    }
                }
            }

            _logger.LogInfo($"🎯 Risultato filtro: {matchCount} con area + {senzaAreaCount} senza area = {filtered.Count} totali");

            // 🔧 ORDINAMENTO PRIORITARIO: Prima quelli con area, poi quelli senza area
            var result = filtered
                .OrderBy(f => AreaApplicativoMappingGenerator.ExtractAreaFromApplicativo(f.Value) == null ? 1 : 0) // Prima con area
                .ThenBy(f => f.DisplayValue) // Poi alfabetico
                .ToList();

            // Debug primi 10 risultati finali
            _logger.LogInfo($"🔍 Primi 10 applicativi nel risultato finale:");
            for (int i = 0; i < Math.Min(10, result.Count); i++)
            {
                var hasArea = AreaApplicativoMappingGenerator.ExtractAreaFromApplicativo(result[i].Value) != null;
                var areaTag = hasArea ? "🎯" : "🔄";
                _logger.LogInfo($"🔍 Result [{i + 1:D2}]: {areaTag} '{result[i].DisplayValue}' (Value: '{result[i].Value}')");
            }

            return result;
        }

        /// <summary>
        /// Popola ComboBox child con dati filtrati
        /// </summary>
        private void PopulateChildComboBox(ComboBox comboBox, List<JiraField> fields, string defaultText)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(() => PopulateChildComboBox(comboBox, fields, defaultText));
                return;
            }

            // Pulisci e crea mapping
            comboBox.Items.Clear();
            var mapping = new Dictionary<string, string>();

            // Aggiungi default
            var defaultDisplayText = defaultText ?? GetDefaultText(_comboBoxInfos[comboBox].FieldType);
            comboBox.Items.Add(defaultDisplayText);
            mapping[defaultDisplayText] = "";

            // Aggiungi valori ordinati
            var sortedFields = fields
                .Where(f => !string.IsNullOrWhiteSpace(f.DisplayValue))
                .OrderBy(f => f.DisplayValue)
                .ToList();

            foreach (var field in sortedFields)
            {
                var displayValue = CleanDisplayValue(field.DisplayValue);

                // Evita duplicati
                if (!mapping.ContainsKey(displayValue))
                {
                    comboBox.Items.Add(displayValue);
                    mapping[displayValue] = field.Value;
                }
            }

            // Salva mapping
            _valueMappings[comboBox] = mapping;

            // Ripristina selezione o seleziona default
            comboBox.SelectedIndex = 0;

            // Aggiorna info
            _comboBoxInfos[comboBox].LastLoadTime = DateTime.Now;
            _comboBoxInfos[comboBox].ItemCount = fields.Count;

            // *** 🔤 AGGIUNTA: AutoComplete per cmbApplicativo ***
            RefreshAutoCompleteItems(comboBox);
        }

        /// <summary>
        /// Imposta stato abilitato/disabilitato per ComboBox dipendente
        /// </summary>
        private void SetDependencyState(ComboBox comboBox, bool enabled, string disabledText = null)
        {
            if (comboBox.InvokeRequired)
            {
                comboBox.Invoke(() => SetDependencyState(comboBox, enabled, disabledText));
                return;
            }

            if (!enabled && !string.IsNullOrEmpty(disabledText))
            {
                // Stato disabilitato: mostra testo esplicativo
                comboBox.Items.Clear();
                comboBox.Items.Add(disabledText);
                comboBox.SelectedIndex = 0;
                comboBox.Enabled = false;
            }
            else
            {
                // Stato abilitato
                comboBox.Enabled = enabled;
            }
        }

        /// <summary>
        /// Ottiene il valore selezionato display (non originale) da una ComboBox
        /// </summary>
        private string GetSelectedDisplayValue(ComboBox comboBox)
        {
            return comboBox.SelectedItem?.ToString() ?? "";
        }

        #endregion

     

#if DEBUG
        /// <summary>
        /// Debug: Mostra mapping completo di una ComboBox
        /// </summary>
        public void DebugComboBoxMapping(ComboBox comboBox, string comboName)
        {
            _logger.LogInfo($"🔍 === DEBUG MAPPING {comboName} ===");

            if (!_valueMappings.TryGetValue(comboBox, out var mapping))
            {
                _logger.LogError($"❌ Nessun mapping trovato per {comboName}");
                return;
            }

            _logger.LogInfo($"📊 Mapping {comboName}: {mapping.Count} elementi");

            // Mostra primi 10 elementi del mapping
            var counter = 0;
            foreach (var item in mapping)
            {
                if (counter >= 10) break;
                _logger.LogInfo($"🔗 Display: '{item.Key}' → Original: '{item.Value}'");
                counter++;
            }

            var selectedDisplay = comboBox.Text;
            var selectedOriginal = GetSelectedOriginalValue(comboBox);

            _logger.LogInfo($"🎯 SELEZIONATO:");
            _logger.LogInfo($"   Display: '{selectedDisplay}'");
            _logger.LogInfo($"   Original: '{selectedOriginal}'");
            _logger.LogInfo($"🔍 === FINE DEBUG {comboName} ===");
        }
#endif


        // <summary>
        /// Carica ComboBox e imposta il valore corrente dal ticket
        /// </summary>
        public async Task LoadAsyncWithCurrentValue(ComboBox comboBox, JiraFieldType fieldType,
            string defaultText = null, IProgress<string> progress = null, string ticketKey = null)
        {
            if (comboBox == null) throw new ArgumentNullException(nameof(comboBox));

            try
            {
                _logger.LogInfo($"Caricamento {fieldType} con valore corrente per ComboBox '{comboBox.Name}'");

                // 1. Carica tutti i valori (logica esistente)
                await LoadAsync(comboBox, fieldType, defaultText, progress, ticketKey);

                // 2. Imposta il valore corrente se è per Consulente e abbiamo un ticketKey
                if (fieldType == JiraFieldType.Consulente && !string.IsNullOrEmpty(ticketKey))
                {
                    await SetConsulenteCurrentValue(comboBox, ticketKey);
                }

                _logger.LogInfo($"Caricamento con valore corrente completato per {fieldType}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento con valore corrente {fieldType}", ex);
                throw;
            }
        }

        /// <summary>
        /// Imposta il valore corrente del consulente nella ComboBox - CON DEBUG DETTAGLIATO
        /// </summary>
        private async Task SetConsulenteCurrentValue(ComboBox comboBox, string ticketKey)
        {
            try
            {
                _logger.LogInfo($"🔍 === DEBUG CONSULENTE CORRENTE START ===");
                _logger.LogInfo($"🔍 Ticket: {ticketKey}");

                // Carica il ticket corrente
                var ticket = await _dataService.GetTicketAsync(ticketKey);
                if (ticket?.RawData == null)
                {
                    _logger.LogWarning("❌ Ticket o RawData non disponibili");
                    return;
                }

                // Estrai il valore del consulente dal ticket
                var fields = ticket.RawData["fields"];
                var consulteneField = fields?["customfield_10238"];

                if (consulteneField == null || consulteneField.Type == JTokenType.Null)
                {
                    _logger.LogInfo("❌ Campo consulente vuoto nel ticket");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                // Estrai il valore grezzo del consulente
                string consulteneValueRaw = "";
                if (consulteneField.Type == JTokenType.String)
                {
                    consulteneValueRaw = consulteneField.ToString();
                }
                else if (consulteneField.Type == JTokenType.Object)
                {
                    consulteneValueRaw = consulteneField["value"]?.ToString() ??
                                         consulteneField["name"]?.ToString() ??
                                         consulteneField["displayName"]?.ToString() ?? "";
                }

                if (string.IsNullOrEmpty(consulteneValueRaw))
                {
                    _logger.LogWarning("❌ Valore consulente estratto vuoto");
                    comboBox.SelectedIndex = 0;
                    return;
                }

                _logger.LogInfo($"🔍 Valore grezzo dal ticket: '{consulteneValueRaw}'");

                // ✅ LOGICA CORRETTA: Confronta valore grezzo del ticket con valori grezzi della ComboBox
                var items = comboBox.Items.Cast<string>().ToList();

                for (int i = 0; i < items.Count; i++)
                {
                    // Converti l'item della ComboBox INDIETRO al formato grezzo per il confronto
                    var itemDisplayValue = items[i];

                    // Salta il default
                    if (itemDisplayValue.StartsWith("--") || itemDisplayValue.Contains("["))
                        continue;

                    // Converti l'item display INDIETRO al formato grezzo
                    var itemRawValue = ConvertDisplayToRaw(itemDisplayValue);

                    _logger.LogDebug($"🔍 Confronto: ticket='{consulteneValueRaw}' vs combo='{itemRawValue}' (display='{itemDisplayValue}')");

                    // Match con valore grezzo
                    if (itemRawValue.Equals(consulteneValueRaw, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = i;
                        _logger.LogInfo($"✅ MATCH GREZZO: Trovato '{consulteneValueRaw}' → display '{itemDisplayValue}' all'indice {i}");
                        return;
                    }
                }

                // Se non trova match esatto, prova match parziale
                _logger.LogInfo("🔍 Tentativo match parziale...");
                for (int i = 1; i < items.Count; i++)
                {
                    var itemRawValue = ConvertDisplayToRaw(items[i]);

                    if (itemRawValue.Contains(consulteneValueRaw, StringComparison.OrdinalIgnoreCase) ||
                        consulteneValueRaw.Contains(itemRawValue, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedIndex = i;
                        _logger.LogInfo($"⚠️ MATCH PARZIALE: '{consulteneValueRaw}' ≈ '{itemRawValue}' → '{items[i]}' all'indice {i}");
                        return;
                    }
                }

                _logger.LogWarning($"❌ NESSUN MATCH: Consulente '{consulteneValueRaw}' non trovato");
                comboBox.SelectedIndex = 0;

                _logger.LogInfo($"🔍 === DEBUG CONSULENTE CORRENTE END ===");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Errore impostazione consulente corrente: {ex.Message}", ex);
                if (comboBox?.Items.Count > 0)
                    comboBox.SelectedIndex = 0;
            }
        }

        private string ConvertDisplayToRaw(string displayValue)
        {
            try
            {
                if (string.IsNullOrEmpty(displayValue))
                    return "";

                // ✅ CASI SPECIALI INVERSI
                var inverseSpecialCases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NICOLA GIOVANNI LUPO", "NICOLAGIOVANNI.LUPO" },
            { "JONATHAN FELIX DA SILVA", "JONATHAN.FELIXDASILVA" },
            { "FRANCESCA FELICITA MAIELLO", "FRANCESCAFELICITA.MAIELLO" },
            { "GIANNI LORENZO ZULLI", "GIANNILORENZO.ZULLI" },
            { "RAZVAN ALEXANDRU BARABANCEA", "RAZVANALEXANDRU.BARABANCEA" }
        };

                if (inverseSpecialCases.ContainsKey(displayValue))
                {
                    return inverseSpecialCases[displayValue];
                }

                // ✅ LOGICA NORMALE INVERSA: ANDREA ROSSI → andrea.rossi
                return displayValue.Replace(" ", ".").ToLower();
            }
            catch
            {
                return displayValue;
            }
        }

    }
}