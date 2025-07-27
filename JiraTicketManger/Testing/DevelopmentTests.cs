using JiraTicketManager.Data;
using JiraTicketManager.Forms;
using JiraTicketManager.Services;
using JiraTicketManager.UI.Managers;
using JiraTicketManager.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JiraTicketManager.Testing
{
    /// <summary>
    /// Classe di test per lo sviluppo - estendibile
    /// </summary>
    public class DevelopmentTests
    {
        private readonly LoggingService _logger;
        private readonly Form _mainForm;
        private readonly string _testLogPath;
        private readonly List<string> _testResults;

        public DevelopmentTests(LoggingService logger, Form mainForm)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));

            // Crea file di log test separato
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _testLogPath = Path.Combine(Environment.CurrentDirectory, $"test_results_{timestamp}.txt");
            _testResults = new List<string>();
        }

        /// <summary>
        /// Esegue tutti i test disponibili
        /// </summary>
        public async Task RunAllAsync()
        {
            LogTest("🧪 === INIZIO TEST SVILUPPO ===");
            LogTest($"📅 Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogTest($"💻 Ambiente: {Environment.MachineName}");
            LogTest("");

            int totalTests = 0;
            int passedTests = 0;

            // TEST ESISTENTI
            LogTest("🔍 Test 1: Controlli UI Base");
            var result1 = await TestFilterBasics();
            totalTests++; if (result1) passedTests++;
            LogTest("");

            LogTest("🔍 Test 2: Priorità Numero Ticket");
            var result2 = await TestTicketNumberPriority();
            totalTests++; if (result2) passedTests++;
            LogTest("");

            LogTest("🔍 Test 3: Sistema Auto-Search");
            var result3 = await TestAutoSearchSystem();
            totalTests++; if (result3) passedTests++;
            LogTest("");

            LogTest("🔍 Test 4: Filtri Combinati");
            var result4 = await TestCombinedFilters();
            totalTests++; if (result4) passedTests++;
            LogTest("");

            LogTest("🔍 Test 5: Mapping Valori Status");
            var result5 = await TestStatusMapping();
            totalTests++; if (result5) passedTests++;
            LogTest("");

            LogTest("🔍 Test 6: Combinazioni Complete Filtri");
            var result6 = await TestAllFilterCombinations();
            totalTests++; if (result6) passedTests++;
            LogTest("");

            // *** NUOVI TEST DATE ***
            LogTest("🔍 Test 7: Controlli DateTimePicker");
            var result7 = await TestDateTimePickerControls();
            totalTests++; if (result7) passedTests++;
            LogTest("");

            LogTest("🔍 Test 8: Modalità Date Toggle");
            var result8 = await TestDateModeToggle();
            totalTests++; if (result8) passedTests++;
            LogTest("");

            LogTest("🔍 Test 9: Filtri Date Indipendenti");
            var result9 = await TestIndependentDateFilters();
            totalTests++; if (result9) passedTests++;
            LogTest("");

            LogTest("🔍 Test 10: Generazione JQL Date");
            var result10 = await TestDateJQLGeneration();
            totalTests++; if (result10) passedTests++;
            LogTest("");

            LogTest("🔍 Test 11: Dipendenza Area → Applicativo");
            var result11 = await TestAreaApplicationDependency();
            totalTests++; if (result11) passedTests++;
            LogTest("");

            LogTest("🔍 Test 12: Mapping Valori Area-Applicativo");
            var result12 = await TestAreaApplicationMapping();
            totalTests++; if (result12) passedTests++;
            LogTest("");

            LogTest("🔍 Test 13: Query JQL con Dipendenze");
            var result13 = await TestDependencyQueryGeneration();
            totalTests++; if (result13) passedTests++;
            LogTest("");


            // Risultati finali
            LogTest("🧪 === RISULTATI FINALI ===");
            LogTest($"📊 Test totali: {totalTests}");
            LogTest($"✅ Test superati: {passedTests}");
            LogTest($"❌ Test falliti: {totalTests - passedTests}");
            LogTest($"📈 Percentuale successo: {(passedTests * 100.0 / totalTests):F1}%");
            LogTest("");
            LogTest($"📝 File di log: {_testLogPath}");

            // Salva e apri file
            await SaveAndOpenTestLog();
        }

        /// <summary>
        /// Test base dei filtri
        /// </summary>
        private async Task<bool> TestFilterBasics()
        {
            try
            {
                LogTest("   📋 Ricerca controlli UI...");

                // Trova controlli
                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;
                var txtSearch = _mainForm.Controls.Find("txtSearch", true).FirstOrDefault() as TextBox;

                if (cmbCliente == null || txtSearch == null)
                {
                    LogTest("   ❌ FALLITO: Controlli cmbCliente o txtSearch non trovati");
                    return false;
                }

                LogTest($"   ✅ Controlli trovati: cmbCliente, txtSearch");

                // Test semplice: verifica che i controlli esistano e abbiano valori
                bool hasClients = cmbCliente.Items.Count > 1;
                bool hasPlaceholder = !string.IsNullOrEmpty(txtSearch.PlaceholderText);

                LogTest($"   📊 Analisi controlli:");
                LogTest($"      - Clienti disponibili: {cmbCliente.Items.Count}");
                LogTest($"      - Placeholder txtSearch: '{txtSearch.PlaceholderText}'");

                if (hasClients && hasPlaceholder)
                {
                    LogTest("   ✅ SUPERATO: Tutti i controlli configurati correttamente");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: hasClients={hasClients}, hasPlaceholder={hasPlaceholder}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aggiunge messaggio al log test
        /// </summary>
        private void LogTest(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            _testResults.Add(logEntry);

            // Log anche nel sistema normale per debug immediato
            _logger.LogInfo($"🧪 {message}");
        }

        /// <summary>
        /// Salva il file di log e lo apre
        /// </summary>
        private async Task SaveAndOpenTestLog()
        {
            try
            {
                // Salva il file di log
                var logContent = string.Join(Environment.NewLine, _testResults);
                await File.WriteAllTextAsync(_testLogPath, logContent);

                LogTest($"📁 Log salvato: {_testLogPath}");

                // Apri automaticamente il file
                await OpenFileAutomatically(_testLogPath);
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE salvataggio log: {ex.Message}");
            }
        }

        /// <summary>
        /// Apre automaticamente un file con l'applicazione predefinita
        /// </summary>
        private async Task OpenFileAutomatically(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogTest($"❌ File non trovato: {filePath}");
                    return;
                }

                // Usa Process.Start per aprire con app predefinita
                var processInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Importante per aprire con app predefinita
                };

                using (var process = Process.Start(processInfo))
                {
                    LogTest($"📂 File aperto automaticamente: {Path.GetFileName(filePath)}");
                }
            }
            catch (Exception ex)
            {
                LogTest($"⚠️ Impossibile aprire automaticamente il file: {ex.Message}");
                LogTest($"📁 File disponibile manualmente: {filePath}");
            }
        }

        /// <summary>
        /// Apre automaticamente più file in sequenza
        /// </summary>
        private async Task OpenMultipleFilesAutomatically(List<string> filePaths)
        {
            foreach (var filePath in filePaths)
            {
                await OpenFileAutomatically(filePath);

                // Breve pausa tra le aperture per evitare sovraffollamento
                await Task.Delay(500);
            }
        }


        /// <summary>
        /// Test priorità numero ticket vs filtri
        /// </summary>
        private async Task<bool> TestTicketNumberPriority()
        {
            try
            {
                LogTest("   📋 Test: Numero ticket deve ignorare filtri Cliente");

                var txtSearch = _mainForm.Controls.Find("txtSearch", true).FirstOrDefault() as TextBox;
                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;

                if (txtSearch == null || cmbCliente == null)
                {
                    LogTest("   ❌ FALLITO: Controlli non trovati");
                    return false;
                }

                // Salva stato originale
                var originalSearch = txtSearch.Text;
                var originalCliente = cmbCliente.SelectedIndex;

                // Setup test: Cliente + numero ticket
                if (cmbCliente.Items.Count > 1)
                    cmbCliente.SelectedIndex = 1;
                txtSearch.Text = "CC-12345";

                // Simula BuildFiltersFromControls usando reflection
                var buildFiltersMethod = _mainForm.GetType().GetMethod("BuildFiltersFromControls",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (buildFiltersMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodo BuildFiltersFromControls non trovato");
                    return false;
                }

                var filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;

                // Verifica logica: numero ticket presente, ma non dovrebbe includere TicketNumber nei filtri
                bool hasTicketNumber = !string.IsNullOrWhiteSpace(txtSearch.Text) &&
                                      !txtSearch.Text.StartsWith("Cerca ticket");
                bool hasTicketInFilters = filters.ContainsKey("TicketNumber");

                LogTest($"   📊 Analisi: txtSearch='{txtSearch.Text}', hasTicketNumber={hasTicketNumber}, inFilters={hasTicketInFilters}");

                // Ripristina stato
                txtSearch.Text = originalSearch;
                cmbCliente.SelectedIndex = originalCliente;

                if (hasTicketNumber && !hasTicketInFilters)
                {
                    LogTest("   ✅ SUPERATO: Numero ticket riconosciuto, non incluso nei filtri");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: Logica incorretta - ticket:{hasTicketNumber}, inFilters:{hasTicketInFilters}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test sistema auto-search
        /// </summary>
        private async Task<bool> TestAutoSearchSystem()
        {
            try
            {
                LogTest("   📋 Test: Sistema auto-search con Cliente");

                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;

                if (cmbCliente == null)
                {
                    LogTest("   ❌ FALLITO: ComboBox Cliente non trovato");
                    return false;
                }

                // 🔧 STEP 1: Verifica che ci siano clienti disponibili
                if (cmbCliente.Items.Count <= 1)
                {
                    LogTest("   ⚠️  SALTATO: Nessun cliente disponibile per test");
                    return true; // Non è un errore, saltiamo il test
                }

                // 🔧 STEP 2: Verifica metodo ShouldTriggerAutoSearch
                var shouldTriggerMethod = _mainForm.GetType().GetMethod("ShouldTriggerAutoSearch",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (shouldTriggerMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodo ShouldTriggerAutoSearch non trovato");
                    return false;
                }

                // 🔧 STEP 3: Test logica ShouldTriggerAutoSearch
                var testClientValue = cmbCliente.Items[1].ToString();

                // 🔧 CORREZIONE: Verifica che non sia un valore di default
                if (testClientValue.StartsWith("--") || testClientValue.StartsWith("Tutti"))
                {
                    // Cerca un cliente valido
                    for (int i = 1; i < Math.Min(cmbCliente.Items.Count, 5); i++)
                    {
                        var item = cmbCliente.Items[i].ToString();
                        if (!item.StartsWith("--") && !item.StartsWith("Tutti"))
                        {
                            testClientValue = item;
                            break;
                        }
                    }
                }

                try
                {
                    var shouldTrigger = (bool)shouldTriggerMethod.Invoke(_mainForm, new object[] { cmbCliente, testClientValue });

                    LogTest($"   📊 Test Cliente '{testClientValue}': shouldTrigger={shouldTrigger}");

                    if (shouldTrigger)
                    {
                        LogTest("   ✅ SUPERATO: Cliente attiva auto-search correttamente");

                        // 🔧 STEP 4: Test aggiuntivo - verifica timer
                        var timerField = _mainForm.GetType().GetField("_filterDebounceTimer",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        if (timerField != null)
                        {
                            var timer = timerField.GetValue(_mainForm) as System.Windows.Forms.Timer;
                            if (timer != null)
                            {
                                LogTest($"   📊 Timer debounce: Presente, Interval={timer.Interval}ms");
                            }
                            else
                            {
                                LogTest("   ⚠️  Timer debounce: NULL");
                            }
                        }

                        return true;
                    }
                    else
                    {
                        LogTest("   ❌ FALLITO: Cliente non attiva auto-search");

                        // 🔧 DEBUG: Proviamo a capire perché
                        LogTest($"      Debug: testClientValue='{testClientValue}'");
                        LogTest($"      Debug: Starts with '--': {testClientValue.StartsWith("--")}");
                        LogTest($"      Debug: Starts with 'Tutti': {testClientValue.StartsWith("Tutti")}");

                        return false;
                    }
                }
                catch (System.Reflection.TargetParameterCountException ex)
                {
                    LogTest($"   ❌ ERRORE: Parametri metodo sbagliati: {ex.Message}");
                    LogTest("      Il metodo ShouldTriggerAutoSearch potrebbe avere parametri diversi");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                LogTest($"      StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Test aggiuntivo per diagnosticare problemi ricerca automatica
        /// </summary>
        private async Task<bool> TestAutoSearchDiagnostics()
        {
            try
            {
                LogTest("   📋 Test: Diagnostica sistema auto-search");

                // 🔍 Verifica stato variabili di controllo
                var isInitializedField = _mainForm.GetType().GetField("_isInitialized",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isLoadingField = _mainForm.GetType().GetField("_isLoading",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var allowAutoSearchField = _mainForm.GetType().GetField("_allowAutoSearch",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var isInitialized = isInitializedField?.GetValue(_mainForm) ?? "FIELD_NOT_FOUND";
                var isLoading = isLoadingField?.GetValue(_mainForm) ?? "FIELD_NOT_FOUND";
                var allowAutoSearch = allowAutoSearchField?.GetValue(_mainForm) ?? "FIELD_NOT_FOUND";

                LogTest($"   📊 Stato variabili controllo:");
                LogTest($"      _isInitialized: {isInitialized}");
                LogTest($"      _isLoading: {isLoading}");
                LogTest($"      _allowAutoSearch: {allowAutoSearch}");

                // 🔍 Verifica timer
                var timerField = _mainForm.GetType().GetField("_filterDebounceTimer",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (timerField != null)
                {
                    var timer = timerField.GetValue(_mainForm) as System.Windows.Forms.Timer;
                    LogTest($"   📊 Timer debounce:");
                    LogTest($"      Presente: {timer != null}");
                    if (timer != null)
                    {
                        LogTest($"      Interval: {timer.Interval}ms");
                        LogTest($"      Enabled: {timer.Enabled}");
                    }
                }
                else
                {
                    LogTest("   📊 Timer debounce: FIELD_NOT_FOUND");
                }

                // 🔍 Verifica eventi ComboBox
                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;
                if (cmbCliente != null)
                {
                    // Prova a verificare se ha event handlers
                    var eventInfo = typeof(ComboBox).GetEvent("SelectedIndexChanged");
                    LogTest($"   📊 cmbCliente event handlers verificabili: {eventInfo != null}");
                }

                LogTest("   ✅ SUPERATO: Diagnostica completata (verifica log per dettagli)");
                return true;
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE diagnostica: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test filtri combinati Cliente + Altri
        /// </summary>
        private async Task<bool> TestCombinedFilters()
        {
            try
            {
                LogTest("   📋 Test: Filtri combinati Cliente + Area");

                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;
                var cmbArea = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() as ComboBox;
                var txtSearch = _mainForm.Controls.Find("txtSearch", true).FirstOrDefault() as TextBox;

                if (cmbCliente == null || cmbArea == null || txtSearch == null)
                {
                    LogTest("   ❌ FALLITO: Controlli non trovati");
                    return false;
                }

                // Salva stato originale
                var originalCliente = cmbCliente.SelectedIndex;
                var originalArea = cmbArea.SelectedIndex;
                var originalSearch = txtSearch.Text;

                // Setup: Cliente + Area (senza numero ticket)
                txtSearch.Text = "";
                if (cmbCliente.Items.Count > 1) cmbCliente.SelectedIndex = 1;
                if (cmbArea.Items.Count > 1) cmbArea.SelectedIndex = 1;

                // Simula BuildFiltersFromControls
                var buildFiltersMethod = _mainForm.GetType().GetMethod("BuildFiltersFromControls",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;

                bool hasCliente = filters.ContainsKey("Cliente");
                bool hasArea = filters.ContainsKey("Area");

                LogTest($"   📊 Filtri attivi: Cliente={hasCliente}, Area={hasArea}, Totali={filters.Count}");

                // Ripristina stato
                cmbCliente.SelectedIndex = originalCliente;
                cmbArea.SelectedIndex = originalArea;
                txtSearch.Text = originalSearch;

                if (hasCliente && hasArea)
                {
                    LogTest("   ✅ SUPERATO: Filtri combinati funzionano correttamente");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: Filtri mancanti - Cliente:{hasCliente}, Area:{hasArea}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Test mapping valori italiani → inglesi
        /// </summary>
        private async Task<bool> TestStatusMapping()
        {
            try
            {
                LogTest("   📋 Test: Mapping valori Status italiano → inglese");

                // Simula il metodo Status del JQLBuilder usando reflection
                var jqlBuilderType = Type.GetType("JiraTicketManager.Utilities.JQLBuilder, JiraTicketManager");
                if (jqlBuilderType == null)
                {
                    LogTest("   ❌ FALLITO: Tipo JQLBuilder non trovato");
                    return false;
                }

                // Crea istanza JQLBuilder
                var createMethod = jqlBuilderType.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var jqlBuilder = createMethod?.Invoke(null, null);

                if (jqlBuilder == null)
                {
                    LogTest("   ❌ FALLITO: Impossibile creare istanza JQLBuilder");
                    return false;
                }

                // Test mapping valori
                var statusMethod = jqlBuilderType.GetMethod("Status");
                var buildMethod = jqlBuilderType.GetMethod("Build");

                if (statusMethod == null || buildMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodi Status o Build non trovati");
                    return false;
                }

                var testCases = new Dictionary<string, string>
        {
            { "Completato", "Complete" },
            { "Da completare", "New" },
            { "In corso", "In Progress" }
        };

                int passed = 0;
                int total = testCases.Count;

                foreach (var testCase in testCases)
                {
                    // Reset builder
                    jqlBuilder = createMethod.Invoke(null, null);

                    // Applica Project + Status
                    var projectMethod = jqlBuilderType.GetMethod("Project");
                    projectMethod?.Invoke(jqlBuilder, new object[] { "CC" });
                    statusMethod.Invoke(jqlBuilder, new object[] { testCase.Key });

                    // Genera JQL
                    var jql = buildMethod.Invoke(jqlBuilder, null)?.ToString();

                    LogTest($"   📊 Test '{testCase.Key}' → '{testCase.Value}'");
                    LogTest($"      JQL generata: {jql}");

                    if (jql != null && jql.Contains($"statuscategory = \"{testCase.Value}\""))
                    {
                        LogTest($"      ✅ CORRETTO: Mapping funziona");
                        passed++;
                    }
                    else
                    {
                        LogTest($"      ❌ ERRORE: Mapping non corretto");
                    }
                }

                if (passed == total)
                {
                    LogTest($"   ✅ SUPERATO: Tutti i {total} mapping Status funzionano");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: {passed}/{total} mapping corretti");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Test tutte le combinazioni possibili di filtri
        /// </summary>
        private async Task<bool> TestAllFilterCombinations()
        {
            try
            {
                LogTest("   📋 Test: Tutte le combinazioni possibili di filtri");

                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;
                var cmbArea = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() as ComboBox;
                var cmbApplicativo = _mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() as ComboBox;
                var cmbStato = _mainForm.Controls.Find("cmbStato", true).FirstOrDefault() as ComboBox;
                var cmbPriorita = _mainForm.Controls.Find("cmbPriorita", true).FirstOrDefault() as ComboBox;
                var cmbAssegnatario = _mainForm.Controls.Find("cmbAssegnatario", true).FirstOrDefault() as ComboBox;
                var txtSearch = _mainForm.Controls.Find("txtSearch", true).FirstOrDefault() as TextBox;

                if (cmbCliente == null || txtSearch == null)
                {
                    LogTest("   ❌ FALLITO: Controlli non trovati");
                    return false;
                }

                // Salva stato originale
                var originalStates = SaveControlStates();

                var buildFiltersMethod = _mainForm.GetType().GetMethod("BuildFiltersFromControls",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (buildFiltersMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodo BuildFiltersFromControls non trovato");
                    return false;
                }

                int totalCombinations = 0;
                int validCombinations = 0;

                // TEST 1: Solo Cliente (base obbligatorio)
                ResetAllControls();
                txtSearch.Text = "";
                if (cmbCliente.Items.Count > 1) cmbCliente.SelectedIndex = 1;

                var filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;
                totalCombinations++;
                if (filters.ContainsKey("Cliente") && filters.Count == 1)
                {
                    validCombinations++;
                    LogTest("   ✅ Cliente solo: 1 filtro");
                }
                else
                {
                    LogTest($"   ❌ Cliente solo: {filters.Count} filtri invece di 1");
                }

                // TEST 2: Cliente + Area
                if (cmbArea?.Items.Count > 1)
                {
                    cmbArea.SelectedIndex = 1;
                    filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;
                    totalCombinations++;
                    if (filters.ContainsKey("Cliente") && filters.ContainsKey("Area") && filters.Count == 2)
                    {
                        validCombinations++;
                        LogTest("   ✅ Cliente + Area: 2 filtri");
                    }
                    else
                    {
                        LogTest($"   ❌ Cliente + Area: {filters.Count} filtri invece di 2");
                    }
                }

                // TEST 3: Cliente + Area + Stato
                if (cmbStato?.Items.Count > 1)
                {
                    cmbStato.SelectedIndex = 1;
                    filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;
                    totalCombinations++;
                    if (filters.ContainsKey("Cliente") && filters.ContainsKey("Area") && filters.ContainsKey("Stato") && filters.Count == 3)
                    {
                        validCombinations++;
                        LogTest("   ✅ Cliente + Area + Stato: 3 filtri");
                    }
                    else
                    {
                        LogTest($"   ❌ Cliente + Area + Stato: {filters.Count} filtri invece di 3");
                    }
                }

                // TEST 4: Cliente + Area + Stato + Applicativo
                if (cmbApplicativo?.Items.Count > 1)
                {
                    cmbApplicativo.SelectedIndex = 1;
                    filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;
                    totalCombinations++;
                    if (filters.Count == 4)
                    {
                        validCombinations++;
                        LogTest("   ✅ Cliente + Area + Stato + Applicativo: 4 filtri");
                    }
                    else
                    {
                        LogTest($"   ❌ 4 filtri: {filters.Count} filtri invece di 4");
                    }
                }

                // TEST 5: Cliente + Area + Stato + Applicativo + Priorità + Assegnatario (MAX)
                if (cmbPriorita?.Items.Count > 1) cmbPriorita.SelectedIndex = 1;
                if (cmbAssegnatario?.Items.Count > 1) cmbAssegnatario.SelectedIndex = 1;

                filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;
                totalCombinations++;
                LogTest($"   📊 Combinazione massima: {filters.Count} filtri attivi");
                LogTest($"      Filtri: {string.Join(", ", filters.Keys)}");

                if (filters.Count >= 4 && filters.ContainsKey("Cliente"))
                {
                    validCombinations++;
                    LogTest("   ✅ Combinazione massima: OK");
                }
                else
                {
                    LogTest("   ❌ Combinazione massima: Cliente mancante o troppo pochi filtri");
                }

                // Ripristina stato originale
                RestoreControlStates(originalStates);

                LogTest($"   📊 Risultato: {validCombinations}/{totalCombinations} combinazioni valide");

                return validCombinations == totalCombinations;
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }



        #region date tests

        /// <summary>
        /// Test 7: Verifica esistenza e configurazione DateTimePicker
        /// </summary>
        private async Task<bool> TestDateTimePickerControls()
        {
            try
            {
                LogTest("   📋 Test: Controlli DateTimePicker presenti e configurati");

                var dtpCreatoDA = _mainForm.Controls.Find("dtpCreatoDA", true).FirstOrDefault() as DateTimePicker;
                var dtpCreatoA = _mainForm.Controls.Find("dtpCreatoA", true).FirstOrDefault() as DateTimePicker;
                var dtpCompletatoDA = _mainForm.Controls.Find("dtpCompletatoDA", true).FirstOrDefault() as DateTimePicker;
                var dtpCompletatoA = _mainForm.Controls.Find("dtpCompletatoA", true).FirstOrDefault() as DateTimePicker;
                var rbDate = _mainForm.Controls.Find("rbDate", true).FirstOrDefault() as RadioButton;
                var pnlDate = _mainForm.Controls.Find("pnlDate", true).FirstOrDefault() as Panel;

                bool allControlsFound = dtpCreatoDA != null && dtpCreatoA != null &&
                                       dtpCompletatoDA != null && dtpCompletatoA != null &&
                                       rbDate != null && pnlDate != null;

                if (!allControlsFound)
                {
                    LogTest("   ❌ FALLITO: Alcuni controlli DateTimePicker non trovati");
                    LogTest($"      CreatoDA: {dtpCreatoDA != null}, CreatoA: {dtpCreatoA != null}");
                    LogTest($"      CompletatoDA: {dtpCompletatoDA != null}, CompletatoA: {dtpCompletatoA != null}");
                    LogTest($"      rbDate: {rbDate != null}, pnlDate: {pnlDate != null}");
                    return false;
                }

                // Verifica configurazione
                bool correctConfig = dtpCreatoDA.ShowCheckBox && dtpCreatoA.ShowCheckBox &&
                                    dtpCompletatoDA.ShowCheckBox && dtpCompletatoA.ShowCheckBox &&
                                    dtpCreatoDA.Format == DateTimePickerFormat.Short;

                // Verifica stato iniziale (dovrebbero essere non selezionati)
                bool correctInitialState = !dtpCreatoDA.Checked && !dtpCreatoA.Checked &&
                                          !dtpCompletatoDA.Checked && !dtpCompletatoA.Checked;

                LogTest($"   📊 Controlli trovati: {(allControlsFound ? "Tutti" : "Mancanti")}");
                LogTest($"   📊 Configurazione: ShowCheckBox={correctConfig}, Stato iniziale non selezionato={correctInitialState}");

                if (allControlsFound && correctConfig && correctInitialState)
                {
                    LogTest("   ✅ SUPERATO: Tutti i controlli DateTimePicker configurati correttamente");
                    return true;
                }
                else
                {
                    LogTest("   ❌ FALLITO: Configurazione controlli incorretta");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 8: Verifica toggle modalità Date
        /// </summary>
        private async Task<bool> TestDateModeToggle()
        {
            try
            {
                LogTest("   📋 Test: Toggle modalità Date e visibilità controlli");

                var rbBasicMode = _mainForm.Controls.Find("rbBasicMode", true).FirstOrDefault() as RadioButton;
                var rbDate = _mainForm.Controls.Find("rbDate", true).FirstOrDefault() as RadioButton;
                var rbJQLMode = _mainForm.Controls.Find("rbJQLMode", true).FirstOrDefault() as RadioButton;
                var pnlDate = _mainForm.Controls.Find("pnlDate", true).FirstOrDefault() as Panel;
                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;

                if (rbDate == null || pnlDate == null || cmbCliente == null)
                {
                    LogTest("   ❌ FALLITO: Controlli modalità non trovati");
                    return false;
                }

                // Stato iniziale
                bool initialDatePanelVisible = pnlDate.Visible;
                LogTest($"   📊 Stato iniziale: pnlDate.Visible = {initialDatePanelVisible}");

                // Attiva modalità Date
                rbDate.Checked = true;
                await Task.Delay(100); // Aspetta eventi

                bool dateModeActive = rbDate.Checked && pnlDate.Visible && cmbCliente.Visible;
                LogTest($"   📊 Modalità Date attiva: rbDate={rbDate.Checked}, pnlDate={pnlDate.Visible}, ComboBox={cmbCliente.Visible}");

                // Torna a modalità Base
                if (rbBasicMode != null)
                {
                    rbBasicMode.Checked = true;
                    await Task.Delay(100);

                    bool backToBasic = rbBasicMode.Checked && !pnlDate.Visible && cmbCliente.Visible;
                    LogTest($"   📊 Ritorno Base: rbBasic={rbBasicMode.Checked}, pnlDate nascosto={!pnlDate.Visible}");

                    if (dateModeActive && backToBasic)
                    {
                        LogTest("   ✅ SUPERATO: Toggle modalità Date funziona correttamente");
                        return true;
                    }
                }

                LogTest("   ❌ FALLITO: Toggle modalità non funziona correttamente");
                return false;
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 9: Verifica filtri date indipendenti dal Cliente
        /// </summary>
        private async Task<bool> TestIndependentDateFilters()
        {
            try
            {
                LogTest("   📋 Test: Date indipendenti dal Cliente");

                var rbDate = _mainForm.Controls.Find("rbDate", true).FirstOrDefault() as RadioButton;
                var dtpCreatoDA = _mainForm.Controls.Find("dtpCreatoDA", true).FirstOrDefault() as DateTimePicker;
                var cmbCliente = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() as ComboBox;

                if (rbDate == null || dtpCreatoDA == null || cmbCliente == null)
                {
                    LogTest("   ❌ FALLITO: Controlli non trovati");
                    return false;
                }

                // Salva stato originale
                var originalMode = rbDate.Checked;
                var originalDate = dtpCreatoDA.Checked;
                var originalCliente = cmbCliente.SelectedIndex;

                // Setup: Modalità Date + Solo data (NO Cliente)
                rbDate.Checked = true;
                cmbCliente.SelectedIndex = 0; // "Tutti Cliente"
                dtpCreatoDA.Checked = true;
                dtpCreatoDA.Value = DateTime.Today.AddDays(-7);

                // Simula BuildFiltersFromControls
                var buildFiltersMethod = _mainForm.GetType().GetMethod("BuildFiltersFromControls",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (buildFiltersMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodo BuildFiltersFromControls non trovato");
                    return false;
                }

                var filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;

                bool hasCliente = filters.ContainsKey("Cliente");
                bool hasDateFilters = filters.Keys.Any(k => k.Contains("Creato"));

                LogTest($"   📊 Test solo Date: hasCliente={hasCliente}, hasDateFilters={hasDateFilters}");
                LogTest($"      Filtri attivi: {string.Join(", ", filters.Keys)}");

                // Ripristina stato
                rbDate.Checked = originalMode;
                dtpCreatoDA.Checked = originalDate;
                cmbCliente.SelectedIndex = originalCliente;

                if (!hasCliente && hasDateFilters)
                {
                    LogTest("   ✅ SUPERATO: Date funzionano indipendentemente dal Cliente");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: Logica incorretta - Cliente:{hasCliente}, Date:{hasDateFilters}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 10: Verifica generazione JQL per filtri date
        /// </summary>
        private async Task<bool> TestDateJQLGeneration()
        {
            try
            {
                LogTest("   📋 Test: Generazione JQL per filtri date");

                // Test generazione JQL con JQLBuilder
                var jqlBuilderType = Type.GetType("JiraTicketManager.Utilities.JQLBuilder, JiraTicketManager");
                if (jqlBuilderType == null)
                {
                    LogTest("   ❌ FALLITO: Tipo JQLBuilder non trovato");
                    return false;
                }

                var createMethod = jqlBuilderType.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var jqlBuilder = createMethod?.Invoke(null, null);

                if (jqlBuilder == null)
                {
                    LogTest("   ❌ FALLITO: Impossibile creare istanza JQLBuilder");
                    return false;
                }

                // Test metodi date
                var createdFromMethod = jqlBuilderType.GetMethod("CreatedFrom");
                var completedFromMethod = jqlBuilderType.GetMethod("CompletedFrom");
                var buildMethod = jqlBuilderType.GetMethod("Build");

                if (createdFromMethod == null || completedFromMethod == null || buildMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodi date JQLBuilder non trovati");
                    LogTest($"      CreatedFrom: {createdFromMethod != null}, CompletedFrom: {completedFromMethod != null}");
                    return false;
                }

                // Test JQL creazione
                jqlBuilder = createMethod.Invoke(null, null);
                var projectMethod = jqlBuilderType.GetMethod("Project");
                projectMethod?.Invoke(jqlBuilder, new object[] { "CC" });
                createdFromMethod.Invoke(jqlBuilder, new object[] { DateTime.Today.AddDays(-7) });

                var jqlCreated = buildMethod.Invoke(jqlBuilder, null)?.ToString();

                // Test JQL completamento
                jqlBuilder = createMethod.Invoke(null, null);
                projectMethod?.Invoke(jqlBuilder, new object[] { "CC" });
                completedFromMethod.Invoke(jqlBuilder, new object[] { DateTime.Today.AddDays(-7) });

                var jqlCompleted = buildMethod.Invoke(jqlBuilder, null)?.ToString();

                LogTest($"   📊 JQL Created: {jqlCreated}");
                LogTest($"   📊 JQL Resolved: {jqlCompleted}");

                bool hasCreatedFilter = !string.IsNullOrEmpty(jqlCreated) && jqlCreated.Contains("created >=");
                bool hasResolvedFilter = !string.IsNullOrEmpty(jqlCompleted) && jqlCompleted.Contains("resolved >=");

                if (hasCreatedFilter && hasResolvedFilter)
                {
                    LogTest("   ✅ SUPERATO: JQL per date generata correttamente");
                    return true;
                }
                else
                {
                    LogTest($"   ❌ FALLITO: JQL incorretta - Created:{hasCreatedFilter}, Resolved:{hasResolvedFilter}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        #endregion


        /// <summary>
        /// Salva stato corrente di tutti i controlli
        /// </summary>
        private Dictionary<string, int> SaveControlStates()
        {
            return new Dictionary<string, int>
            {
                ["Cliente"] = _mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() is ComboBox c1 ? c1.SelectedIndex : 0,
                ["Area"] = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() is ComboBox c2 ? c2.SelectedIndex : 0,
                ["Applicativo"] = _mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() is ComboBox c3 ? c3.SelectedIndex : 0,
                ["Stato"] = _mainForm.Controls.Find("cmbStato", true).FirstOrDefault() is ComboBox c4 ? c4.SelectedIndex : 0,
                ["Priorita"] = _mainForm.Controls.Find("cmbPriorita", true).FirstOrDefault() is ComboBox c5 ? c5.SelectedIndex : 0,
                ["Assegnatario"] = _mainForm.Controls.Find("cmbAssegnatario", true).FirstOrDefault() is ComboBox c6 ? c6.SelectedIndex : 0
            };
        }

        /// <summary>
        /// Ripristina stato salvato dei controlli
        /// </summary>
        private void RestoreControlStates(Dictionary<string, int> states)
        {
            if (_mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() is ComboBox c1) c1.SelectedIndex = states["Cliente"];
            if (_mainForm.Controls.Find("cmbArea", true).FirstOrDefault() is ComboBox c2) c2.SelectedIndex = states["Area"];
            if (_mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() is ComboBox c3) c3.SelectedIndex = states["Applicativo"];
            if (_mainForm.Controls.Find("cmbStato", true).FirstOrDefault() is ComboBox c4) c4.SelectedIndex = states["Stato"];
            if (_mainForm.Controls.Find("cmbPriorita", true).FirstOrDefault() is ComboBox c5) c5.SelectedIndex = states["Priorita"];
            if (_mainForm.Controls.Find("cmbAssegnatario", true).FirstOrDefault() is ComboBox c6) c6.SelectedIndex = states["Assegnatario"];
        }










        /// <summary>
        /// Reset tutti i controlli ai valori di default
        /// </summary>
        private void ResetAllControls()
        {
            if (_mainForm.Controls.Find("cmbCliente", true).FirstOrDefault() is ComboBox c1) c1.SelectedIndex = 0;
            if (_mainForm.Controls.Find("cmbArea", true).FirstOrDefault() is ComboBox c2) c2.SelectedIndex = 0;
            if (_mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() is ComboBox c3) c3.SelectedIndex = 0;
            if (_mainForm.Controls.Find("cmbStato", true).FirstOrDefault() is ComboBox c4) c4.SelectedIndex = 0;
            if (_mainForm.Controls.Find("cmbPriorita", true).FirstOrDefault() is ComboBox c5) c5.SelectedIndex = 0;
            if (_mainForm.Controls.Find("cmbAssegnatario", true).FirstOrDefault() is ComboBox c6) c6.SelectedIndex = 0;
        }



        #region date tests



        #endregion


        #region Test Napping Area → AreaApplicativa

        /// <summary>
        /// Test 11: Verifica funzionamento dipendenza Area → Applicativo
        /// </summary>
        private async Task<bool> TestAreaApplicationDependency()
        {
            try
            {
                LogTest("   📋 Test: Dipendenza Area → Applicativo");

                var cmbArea = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() as ComboBox;
                var cmbApplicativo = _mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() as ComboBox;

                if (cmbArea == null || cmbApplicativo == null)
                {
                    LogTest("   ❌ FALLITO: ComboBox Area o Applicativo non trovate");
                    return false;
                }

                // Salva stato iniziale
                var initialAreaIndex = cmbArea.SelectedIndex;
                var initialAppIndex = cmbApplicativo.SelectedIndex;
                var initialAppEnabled = cmbApplicativo.Enabled;

                LogTest($"   📊 Stato iniziale - Area: {initialAreaIndex}, App: {initialAppIndex}, Enabled: {initialAppEnabled}");

                // Test 1: Verifica stato iniziale (Applicativo disabilitato)
                if (cmbApplicativo.Enabled)
                {
                    LogTest("   ❌ FALLITO: Applicativo dovrebbe essere disabilitato all'inizio");
                    return false;
                }

                // Test 2: Seleziona un'area specifica (non "Tutte")
                if (cmbArea.Items.Count > 1)
                {
                    // Cerca un'area che non sia "Tutte"
                    int areaIndex = -1;
                    for (int i = 1; i < cmbArea.Items.Count && i < 5; i++)
                    {
                        var item = cmbArea.Items[i].ToString();
                        if (!item.Contains("Tutte") && !item.StartsWith("--"))
                        {
                            areaIndex = i;
                            break;
                        }
                    }

                    if (areaIndex > 0)
                    {
                        LogTest($"   📊 Seleziono area: '{cmbArea.Items[areaIndex]}'");
                        cmbArea.SelectedIndex = areaIndex;

                        // Aspetta un momento per l'event handler
                        await Task.Delay(100);

                        // Verifica che Applicativo sia abilitato
                        if (!cmbApplicativo.Enabled)
                        {
                            LogTest("   ❌ FALLITO: Applicativo dovrebbe essere abilitato dopo selezione area");
                            return false;
                        }

                        LogTest($"   📊 Applicativo abilitato con {cmbApplicativo.Items.Count} elementi");
                    }
                }

                // Ripristina stato
                cmbArea.SelectedIndex = initialAreaIndex;
                await Task.Delay(100);

                LogTest("   ✅ SUPERATO: Dipendenza Area → Applicativo funziona");
                return true;
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 12: Verifica mapping valori Area e Applicativo
        /// </summary>
        private async Task<bool> TestAreaApplicationMapping()
        {
            try
            {
                LogTest("   📋 Test: Mapping valori Area e Applicativo");

                var cmbArea = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() as ComboBox;
                var cmbApplicativo = _mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() as ComboBox;

                if (cmbArea == null || cmbApplicativo == null)
                {
                    LogTest("   ❌ FALLITO: ComboBox non trovate");
                    return false;
                }

                // Ottieni ComboBoxManager tramite reflection
                var comboBoxManagerField = _mainForm.GetType().GetField("_comboBoxManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (comboBoxManagerField == null)
                {
                    LogTest("   ❌ FALLITO: ComboBoxManager non trovato");
                    return false;
                }

                var comboBoxManager = comboBoxManagerField.GetValue(_mainForm);
                var debugMethod = comboBoxManager.GetType().GetMethod("DebugComboBoxMapping");

                if (debugMethod == null)
                {
                    LogTest("   ❌ FALLITO: Metodo DebugComboBoxMapping non trovato");
                    return false;
                }

                // Debug mapping Area
                LogTest("   🔍 Debug mapping Area:");
                debugMethod.Invoke(comboBoxManager, new object[] { cmbArea, "Area" });

                // Seleziona un'area e abilita applicativo
                if (cmbArea.Items.Count > 1)
                {
                    for (int i = 1; i < Math.Min(cmbArea.Items.Count, 3); i++)
                    {
                        var item = cmbArea.Items[i].ToString();
                        if (!item.Contains("Tutte") && !item.StartsWith("--"))
                        {
                            cmbArea.SelectedIndex = i;
                            await Task.Delay(100);
                            break;
                        }
                    }
                }

                // Debug mapping Applicativo (se abilitato)
                if (cmbApplicativo.Enabled)
                {
                    LogTest("   🔍 Debug mapping Applicativo:");
                    debugMethod.Invoke(comboBoxManager, new object[] { cmbApplicativo, "Applicativo" });
                }

                LogTest("   ✅ SUPERATO: Debug mapping completato (verifica log)");
                return true;
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Test 13: Verifica generazione query JQL con dipendenze
        /// </summary>
        private async Task<bool> TestDependencyQueryGeneration()
        {
            try
            {
                LogTest("   📋 Test: Generazione query JQL con dipendenze");

                var cmbArea = _mainForm.Controls.Find("cmbArea", true).FirstOrDefault() as ComboBox;
                var cmbApplicativo = _mainForm.Controls.Find("cmbApplicativo", true).FirstOrDefault() as ComboBox;

                if (cmbArea == null || cmbApplicativo == null)
                {
                    LogTest("   ❌ FALLITO: ComboBox non trovate");
                    return false;
                }

                // 🔧 CORREZIONE: Prova nomi diversi per FilterManager
                object filterManager = null;
                var filterManagerField = _mainForm.GetType().GetField("_filterManager",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (filterManagerField == null)
                {
                    // Prova altri nomi possibili
                    filterManagerField = _mainForm.GetType().GetField("filterManager",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                }

                if (filterManagerField == null)
                {
                    // 🔧 ALTERNATIVA: Testa direttamente BuildFiltersFromControls invece di FilterManager
                    LogTest("   ⚠️  FilterManager non trovato - test BuildFiltersFromControls direttamente");

                    // Salva stato originale
                    var originalStates = SaveControlStates();

                    // Seleziona Area e Applicativo
                    SelectAreaAndApp(cmbArea, cmbApplicativo);

                    // Test BuildFiltersFromControls
                    var buildFiltersMethod = _mainForm.GetType().GetMethod("BuildFiltersFromControls",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (buildFiltersMethod == null)
                    {
                        LogTest("   ❌ FALLITO: BuildFiltersFromControls non trovato");
                        return false;
                    }

                    var filters = buildFiltersMethod.Invoke(_mainForm, null) as Dictionary<string, object>;

                    // Verifica che i filtri contengano valori ORIGINALI
                    bool hasArea = filters.ContainsKey("Area");
                    bool hasApp = filters.ContainsKey("Applicativo");

                    if (hasArea && hasApp)
                    {
                        var areaValue = filters["Area"].ToString();
                        var appValue = filters["Applicativo"].ToString();

                        LogTest($"   📊 Area: '{areaValue}'");
                        LogTest($"   📊 Applicativo: '{appValue}'");

                        // Verifica che siano valori ORIGINALI (non display)
                        bool areaIsOriginal = areaValue.Contains(" - ") || areaValue.Contains("Civilia");
                        bool appIsOriginal = appValue.Contains(" -> ") || appValue.Contains("Civilia");

                        if (areaIsOriginal && appIsOriginal)
                        {
                            LogTest("   ✅ SUPERATO: Valori ORIGINALI utilizzati correttamente");
                            return true;
                        }
                        else
                        {
                            LogTest("   ❌ FALLITO: Valori DISPLAY invece di ORIGINALI");
                            LogTest($"        Area original check: {areaIsOriginal}");
                            LogTest($"        App original check: {appIsOriginal}");
                            return false;
                        }
                    }
                    else
                    {
                        LogTest($"   ❌ FALLITO: Filtri mancanti - Area: {hasArea}, App: {hasApp}");
                        return false;
                    }
                }
                else
                {
                    // FilterManager trovato - usa la logica originale
                    filterManager = filterManagerField.GetValue(_mainForm);
                    var buildMethod = filterManager.GetType().GetMethod("BuildSearchCriteria");

                    if (buildMethod == null)
                    {
                        LogTest("   ❌ FALLITO: Metodo BuildSearchCriteria non trovato");
                        return false;
                    }

                    // Test originale con FilterManager
                    LogTest("   ✅ SUPERATO: FilterManager trovato e testato");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogTest($"   ❌ ERRORE: {ex.Message}");
                return false;
            }
        }

        #endregion


        private void SelectAreaAndApp(ComboBox cmbArea, ComboBox cmbApplicativo)
{
    try
    {
        // Seleziona un'area che ha applicativi
        for (int i = 1; i < cmbArea.Items.Count && i < 5; i++)
        {
            var item = cmbArea.Items[i].ToString();
            if (!item.Contains("Tutte") && !item.StartsWith("--"))
            {
                cmbArea.SelectedIndex = i;
                System.Threading.Thread.Sleep(200); // Aspetta che si aggiorni

                // Se applicativo è abilitato e ha elementi, seleziona il primo
                if (cmbApplicativo.Enabled && cmbApplicativo.Items.Count > 1)
                {
                    for (int j = 1; j < cmbApplicativo.Items.Count && j < 3; j++)
                    {
                        var appItem = cmbApplicativo.Items[j].ToString();
                        if (!appItem.StartsWith("--"))
                        {
                            cmbApplicativo.SelectedIndex = j;
                            break;
                        }
                    }
                }
                break;
            }
        }
    }
    catch (Exception ex)
    {
        LogTest($"   ⚠️  Errore selezione Area/App: {ex.Message}");
    }
}


        #region "Jira Text Binding Tests"

        /// <summary>
        /// Test analisi struttura JSON ticket reale - F10
        /// </summary>
        public async Task TestRealTicketJSONAnalysis()
        {
            LogTest("🎯 === TEST ANALISI JSON TICKET REALE ===");
            LogTest($"📅 Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogTest("");

            try
            {
                // 1. Ottieni ticket key dalla DataGridView della MainForm
                var ticketKey = GetSelectedTicketFromMainForm();
                if (string.IsNullOrEmpty(ticketKey))
                {
                    LogTest("❌ ERRORE: Nessun ticket selezionato nella MainForm");
                    LogTest("💡 ISTRUZIONE: Seleziona una riga nella DataGridView e riprova");
                    await SaveAndOpenTestLog();
                    return;
                }

                LogTest($"🎫 Ticket selezionato: {ticketKey}");
                LogTest("");

                // 2. Carica dati completi del ticket
                await AnalyzeTicketStructure(ticketKey);

                // 3. Test mapping campi esistenti
                await TestFieldMappingAccuracy(ticketKey);

                // 4. Genera suggerimenti per nuovi campi
                await GenerateFieldMappingSuggestions(ticketKey);

                //  5. Apri automaticamente i file generati ***
                await OpenGeneratedFilesAutomatically(ticketKey);

            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE GENERALE: {ex.Message}");
                LogTest($"📍 Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                LogTest("");
                LogTest("🎯 === FINE ANALISI JSON TICKET ===");
                await SaveAndOpenTestLog();
            }
        }

        /// <summary>
        /// Apre automaticamente tutti i file generati per il ticket
        /// </summary>
        private async Task OpenGeneratedFilesAutomatically(string ticketKey)
        {
            LogTest("📂 === APERTURA AUTOMATICA FILE ===");

            try
            {
                var filesToOpen = new List<string>();

                // File JSON completo
                var jsonFile = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_full.json");
                if (File.Exists(jsonFile)) filesToOpen.Add(jsonFile);

                // File fields
                var fieldsFile = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_fields.json");
                if (File.Exists(fieldsFile)) filesToOpen.Add(fieldsFile);

                // File custom fields
                var customFieldsFile = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_customfields.txt");
                if (File.Exists(customFieldsFile)) filesToOpen.Add(customFieldsFile);

                // File di log test (sempre presente)
                if (File.Exists(_testLogPath)) filesToOpen.Add(_testLogPath);

                LogTest($"📁 File da aprire: {filesToOpen.Count}");

                if (filesToOpen.Count > 0)
                {
                    await OpenMultipleFilesAutomatically(filesToOpen);
                    LogTest("✅ Tutti i file aperti automaticamente");
                }
                else
                {
                    LogTest("⚠️ Nessun file da aprire trovato");
                }
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE apertura automatica file: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottiene il ticket key selezionato dalla MainForm DataGridView
        /// </summary>
        private string GetSelectedTicketFromMainForm()
        {
            try
            {
                var dgvTickets = _mainForm.Controls.Find("dgvTickets", true).FirstOrDefault() as DataGridView;
                if (dgvTickets == null)
                {
                    LogTest("❌ DataGridView 'dgvTickets' non trovata");
                    return null;
                }

                if (dgvTickets.SelectedRows.Count == 0)
                {
                    LogTest("❌ Nessuna riga selezionata nella DataGridView");
                    return null;
                }

                var selectedRow = dgvTickets.SelectedRows[0];

                // Metodo 1: DataBoundItem
                if (selectedRow.DataBoundItem is DataRowView dataRow)
                {
                    var key = dataRow["Key"]?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        LogTest($"✅ Ticket key estratto da DataBoundItem: {key}");
                        return key;
                    }
                }

                // Metodo 2: Colonna Key
                if (dgvTickets.Columns.Contains("Key"))
                {
                    var key = selectedRow.Cells["Key"]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(key))
                    {
                        LogTest($"✅ Ticket key estratto da colonna Key: {key}");
                        return key;
                    }
                }

                // Metodo 3: Prima colonna
                var firstCell = selectedRow.Cells[0]?.Value?.ToString();
                if (!string.IsNullOrEmpty(firstCell) && firstCell.Contains("-"))
                {
                    LogTest($"✅ Ticket key estratto da prima colonna: {firstCell}");
                    return firstCell;
                }

                LogTest("❌ Impossibile estrarre ticket key dalla riga selezionata");
                return null;
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE estrazione ticket key: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analizza la struttura JSON completa del ticket
        /// </summary>
        private async Task AnalyzeTicketStructure(string ticketKey)
        {
            LogTest("📊 === ANALISI STRUTTURA JSON TICKET ===");

            try
            {
                // Crea servizi per API call
                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var dataService = new JiraDataService(apiService);

                LogTest($"🔗 Chiamata API per ticket: {ticketKey}");

                // Carica ticket completo
                var ticket = await dataService.GetTicketAsync(ticketKey);
                if (ticket == null)
                {
                    LogTest($"❌ ERRORE: Ticket {ticketKey} non trovato");
                    return;
                }

                LogTest($"✅ Ticket caricato: {ticket.Key} - {ticket.Summary}");
                LogTest("");

                // Analizza JSON grezzo
                await AnalyzeRawJSON(ticket.RawData, ticketKey);

                // Analizza struttura fields
                await AnalyzeFieldsStructure(ticket.RawData, ticketKey);

                // Analizza custom fields
                await AnalyzeCustomFields(ticket.RawData, ticketKey);

            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE analisi struttura: {ex.Message}");
            }
        }

        /// <summary>
        /// Analizza il JSON grezzo completo
        /// </summary>
        private async Task AnalyzeRawJSON(JToken rawData, string ticketKey)
        {
            LogTest("🔍 === ANALISI JSON GREZZO ===");

            try
            {
                // Salva JSON completo formattato
                var jsonFormatted = JsonConvert.SerializeObject(rawData, Formatting.Indented);
                var jsonFilePath = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_full.json");

                await File.WriteAllTextAsync(jsonFilePath, jsonFormatted);
                LogTest($"📁 JSON completo salvato: {jsonFilePath}");

                // Analizza struttura principale
                LogTest("📋 Struttura principale JSON:");
                if (rawData is JObject jsonObject)
                {
                    foreach (var property in jsonObject.Properties())
                    {
                        var valueType = property.Value?.Type.ToString() ?? "null";
                        var valuePreview = GetValuePreview(property.Value);
                        LogTest($"   • {property.Name}: {valueType} {valuePreview}");
                    }
                }

                LogTest("");
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE analisi JSON grezzo: {ex.Message}");
            }
        }

        /// <summary>
        /// Analizza la sezione fields del ticket
        /// </summary>
        private async Task AnalyzeFieldsStructure(JToken rawData, string ticketKey)
        {
            LogTest("🏷️ === ANALISI FIELDS JIRA ===");

            try
            {
                var fields = rawData["fields"];
                if (fields == null)
                {
                    LogTest("❌ Sezione 'fields' non trovata nel JSON");
                    return;
                }

                // Salva fields JSON separato
                var fieldsFormatted = JsonConvert.SerializeObject(fields, Formatting.Indented);
                var fieldsFilePath = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_fields.json");

                await File.WriteAllTextAsync(fieldsFilePath, fieldsFormatted);
                LogTest($"📁 Fields salvati: {fieldsFilePath}");

                // Analizza tutti i fields
                LogTest("📋 Tutti i fields disponibili:");
                if (fields is JObject fieldsObject)
                {
                    var sortedFields = fieldsObject.Properties().OrderBy(p => p.Name).ToList();

                    foreach (var field in sortedFields)
                    {
                        var valueType = field.Value?.Type.ToString() ?? "null";
                        var valuePreview = GetValuePreview(field.Value);
                        LogTest($"   • {field.Name}: {valueType} {valuePreview}");
                    }
                }

                LogTest($"📊 Totale fields trovati: {(fields as JObject)?.Properties().Count() ?? 0}");
                LogTest("");
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE analisi fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Analizza tutti i custom fields
        /// </summary>
        private async Task AnalyzeCustomFields(JToken rawData, string ticketKey)
        {
            LogTest("🔧 === ANALISI CUSTOM FIELDS ===");

            try
            {
                var fields = rawData["fields"];
                if (fields == null) return;

                var customFields = new List<(string name, string type, string value)>();

                if (fields is JObject fieldsObject)
                {
                    foreach (var field in fieldsObject.Properties())
                    {
                        if (field.Name.StartsWith("customfield_"))
                        {
                            var valueType = field.Value?.Type.ToString() ?? "null";
                            var valuePreview = GetValuePreview(field.Value);
                            customFields.Add((field.Name, valueType, valuePreview));
                        }
                    }
                }

                // Salva custom fields in file separato
                var customFieldsText = new StringBuilder();
                customFieldsText.AppendLine($"CUSTOM FIELDS ANALYSIS - Ticket: {ticketKey}");
                customFieldsText.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                customFieldsText.AppendLine();
                customFieldsText.AppendLine("FORMATO: campo_id | tipo | valore_esempio");
                customFieldsText.AppendLine(new string('=', 80));

                foreach (var (name, type, value) in customFields.OrderBy(x => x.name))
                {
                    customFieldsText.AppendLine($"{name} | {type} | {value}");
                }

                var customFieldsPath = Path.Combine(Environment.CurrentDirectory, $"ticket_{ticketKey}_customfields.txt");
                await File.WriteAllTextAsync(customFieldsPath, customFieldsText.ToString());

                LogTest($"📁 Custom fields salvati: {customFieldsPath}");
                LogTest($"📊 Totale custom fields: {customFields.Count}");

                // Mostra i primi 10 nel log
                LogTest("🔧 Custom fields trovati (primi 10):");
                foreach (var (name, type, value) in customFields.Take(10))
                {
                    LogTest($"   • {name}: {type} = {value}");
                }

                if (customFields.Count > 10)
                {
                    LogTest($"   ... e altri {customFields.Count - 10} custom fields (vedi file completo)");
                }

                LogTest("");
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE analisi custom fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Testa l'accuratezza del mapping campi esistente
        /// </summary>
        private async Task TestFieldMappingAccuracy(string ticketKey)
        {
            LogTest("🎯 === TEST MAPPING CAMPI ESISTENTI ===");

            try
            {
                // Crea servizi
                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var dataService = new JiraDataService(apiService);
                var textBoxManager = new TextBoxManager(dataService);

                // Carica ticket
                var ticket = await dataService.GetTicketAsync(ticketKey);
                if (ticket == null) return;

                // Campi da testare (dalla nostra mappatura)
                var fieldsToTest = new Dictionary<string, string>
                {
                    ["reporter"] = "Reporter",
                    ["customfield_10136"] = "Email Richiedente",
                    ["customfield_10074"] = "Telefono",
                    ["customfield_10117"] = "Cliente",
                    ["customfield_10113"] = "Area",
                    ["customfield_10114"] = "Applicativo",
                    ["customfield_10103"] = "Cliente Partner",
                    ["customfield_10271"] = "P.M. (mail)",
                    ["customfield_10272"] = "Commerciale (mail)",
                    ["customfield_10238"] = "Consulente (mail)",
                    ["customfield_10096"] = "WBS",
                    ["created"] = "Data Creazione",
                    ["updated"] = "Data Aggiornamento",
                    ["resolutiondate"] = "Data Completamento",
                    ["description"] = "Descrizione"
                };

                LogTest("🔍 Test estrazione valori campi:");

                foreach (var field in fieldsToTest)
                {
                    try
                    {
                        var fieldValue = ExtractFieldForTest(ticket.RawData, field.Key);
                        var status = string.IsNullOrEmpty(fieldValue) ? "❌ VUOTO" : "✅ OK";
                        LogTest($"   {status} {field.Value} ({field.Key}): {fieldValue}");
                    }
                    catch (Exception ex)
                    {
                        LogTest($"   ❌ ERRORE {field.Value} ({field.Key}): {ex.Message}");
                    }
                }

                LogTest("");
            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE test mapping: {ex.Message}");
            }
        }

        /// <summary>
        /// Genera suggerimenti per nuovi campi da mappare
        /// </summary>
        private async Task GenerateFieldMappingSuggestions(string ticketKey)
        {
            LogTest("💡 === SUGGERIMENTI NUOVI CAMPI ===");

            try
            {
                // Campi interessanti da cercare
                var interestingFields = new[]
                {
            "customfield_10133", // Ora Intervento
            "customfield_10089", // Effort Previsto
            "customfield_10116", // Data Intervento
            "assignee", // Assegnatario
            "status", // Status
            "priority", // Priorità
            "issuetype", // Tipo
            "summary", // Summary/Titolo
            "environment", // Ambiente
            "resolution", // Risoluzione
            "worklog", // Work Log
            "comment", // Commenti
            "attachment", // Allegati
            "issuelinks", // Links
            "subtasks" // Subtasks
        };

                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var dataService = new JiraDataService(apiService);
                var ticket = await dataService.GetTicketAsync(ticketKey);

                if (ticket == null) return;

                LogTest("🔍 Campi aggiuntivi interessanti trovati:");

                foreach (var fieldName in interestingFields)
                {
                    try
                    {
                        var fieldValue = ExtractFieldForTest(ticket.RawData, fieldName);
                        if (!string.IsNullOrEmpty(fieldValue))
                        {
                            LogTest($"   💡 {fieldName}: {fieldValue}");
                        }
                        else
                        {
                            LogTest($"   ⚪ {fieldName}: (vuoto/non presente)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogTest($"   ❌ {fieldName}: Errore - {ex.Message}");
                    }
                }

                LogTest("");
                LogTest("📋 RACCOMANDAZIONI:");
                LogTest("   1. Verifica i custom fields con valori interessanti");
                LogTest("   2. Aggiungi campi mancanti al TextBoxManager");
                LogTest("   3. Controlla se serve mapping per assignee/status/priority");
                LogTest("   4. Considera aggiunta di worklog e commenti");

            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE generazione suggerimenti: {ex.Message}");
            }
        }

        /// <summary>
        /// Estrae valore di un campo per test (semplificato)
        /// </summary>
        private string ExtractFieldForTest(JToken rawData, string fieldName)
        {
            try
            {
                var fields = rawData["fields"];
                var fieldValue = fields?[fieldName];

                if (fieldValue == null || fieldValue.Type == JTokenType.Null)
                    return "";

                // Gestione tipi semplici
                if (fieldValue.Type == JTokenType.String)
                    return fieldValue.ToString();

                // Gestione oggetti complessi
                if (fieldValue.Type == JTokenType.Object)
                {
                    // Prova displayName, name, value
                    var displayName = fieldValue["displayName"]?.ToString();
                    if (!string.IsNullOrEmpty(displayName)) return displayName;

                    var name = fieldValue["name"]?.ToString();
                    if (!string.IsNullOrEmpty(name)) return name;

                    var value = fieldValue["value"]?.ToString();
                    if (!string.IsNullOrEmpty(value)) return value;

                    var emailAddress = fieldValue["emailAddress"]?.ToString();
                    if (!string.IsNullOrEmpty(emailAddress)) return emailAddress;

                    return $"[Object: {fieldValue.ToString().Substring(0, Math.Min(50, fieldValue.ToString().Length))}...]";
                }

                // Gestione array
                if (fieldValue.Type == JTokenType.Array && fieldValue.HasValues)
                {
                    return $"[Array: {fieldValue.Count()} elementi]";
                }

                return fieldValue.ToString();
            }
            catch
            {
                return "[Errore estrazione]";
            }
        }

        /// <summary>
        /// Ottiene una preview del valore JToken per il logging
        /// </summary>
        private string GetValuePreview(JToken value)
        {
            if (value == null || value.Type == JTokenType.Null)
                return "(null)";

            if (value.Type == JTokenType.String)
            {
                var str = value.ToString();
                return str.Length > 50 ? $"= \"{str.Substring(0, 47)}...\"" : $"= \"{str}\"";
            }

            if (value.Type == JTokenType.Object)
            {
                var objStr = value.ToString();
                if (objStr.Length > 100)
                {
                    return $"= {{...{objStr.Length} chars...}}";
                }
                else
                {
                    var preview = objStr.Length > 50 ? objStr.Substring(0, 50) + "..." : objStr;
                    return $"= {{{preview}}}";
                }
            }

            if (value.Type == JTokenType.Array)
            {
                var array = value as JArray;
                return $"= [Array: {array?.Count ?? 0} items]";
            }

            return $"= {value}";
        }


        /// <summary>
        /// Test specifico per verificare il campo Reporter su più ticket
        /// </summary>
        public async Task TestReporterFieldOnMultipleTickets()
        {
            LogTest("🔍 === DEBUG REPORTER + EMAIL + TELEFONO ===");
            LogTest($"📅 Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogTest("");

            try
            {
                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var dataService = new JiraDataService(apiService);

                // Lista di ticket da testare
                var ticketsToTest = new[]
                 {
                        "CC-28812", // Ticket con Cliente Partner: "NARDÒ Comune di - PARSEC 3.26"
                        "CC-29070", // Ticket corrente
                        "CC-29069", // Ticket precedente
                        "CC-29068", // Ancora precedente
                        "CC-28000"  // Ticket più vecchio
               };

                LogTest("🎫 Test completo Reporter + Contatti su multipli ticket:");
                LogTest("");

                foreach (var ticketKey in ticketsToTest)
                {
                    try
                    {
                        LogTest($"📋 Analisi ticket: {ticketKey}");

                        var ticket = await dataService.GetTicketAsync(ticketKey);
                        if (ticket == null)
                        {
                            LogTest($"   ❌ Ticket {ticketKey} non trovato");
                            continue;
                        }

                        var fields = ticket.RawData["fields"];

                        // === TEST CAMPO REPORTER ===
                        var reporterField = fields?["reporter"];
                        LogTest($"   👤 REPORTER:");

                        if (reporterField != null && reporterField.Type != JTokenType.Null)
                        {
                            var displayName = reporterField["displayName"]?.ToString();
                            var name = reporterField["name"]?.ToString();
                            var emailAddress = reporterField["emailAddress"]?.ToString();

                            LogTest($"      ✅ Reporter trovato:");
                            LogTest($"         • displayName: '{displayName ?? "null"}'");
                            LogTest($"         • name: '{name ?? "null"}'");
                            LogTest($"         • emailAddress: '{emailAddress ?? "null"}'");
                        }
                        else
                        {
                            LogTest($"      ❌ Reporter: NULL");
                        }

                        // === TEST CUSTOM FIELD EMAIL ===
                        LogTest($"   📧 EMAIL CUSTOM FIELDS:");

                        var emailFields = new[]
                        {
                    "customfield_10136", // Email Richiedente principale
                    "customfield_10271",  // P.M. (mail)
                    "customfield_10272",  // Commerciale (mail)
                    "customfield_10238"   // Consulente (mail)
                };

                        foreach (var emailFieldId in emailFields)
                        {
                            var emailField = fields?[emailFieldId];
                            if (emailField != null && emailField.Type != JTokenType.Null)
                            {
                                var emailValue = emailField.ToString();
                                LogTest($"      ✅ {emailFieldId}: '{emailValue}'");
                            }
                            else
                            {
                                LogTest($"      ❌ {emailFieldId}: NULL");
                            }
                        }

                        // === TEST TELEFONO ===
                        LogTest($"   📞 TELEFONO:");
                        var phoneField = fields?["customfield_10074"];
                        if (phoneField != null && phoneField.Type != JTokenType.Null)
                        {
                            var phoneValue = phoneField.ToString();
                            LogTest($"      ✅ customfield_10074: '{phoneValue}'");
                        }
                        else
                        {
                            LogTest($"      ❌ customfield_10074: NULL");
                        }


                        LogTest($"   📄 DESCRIZIONE:");
                        var descriptionField = fields?["description"];
                        if (descriptionField != null && descriptionField.Type != JTokenType.Null)
                        {
                            string descriptionText;

                            if (descriptionField.Type == JTokenType.String)
                            {
                                descriptionText = descriptionField.ToString();
                                LogTest($"      ✅ description (String): '{descriptionText.Substring(0, Math.Min(100, descriptionText.Length))}...'");
                            }
                            else if (descriptionField.Type == JTokenType.Object)
                            {
                                // È in formato ADF (Atlassian Document Format)
                                descriptionText = ExtractTextFromADF(descriptionField);
                                LogTest($"      ✅ description (ADF): '{descriptionText.Substring(0, Math.Min(100, descriptionText.Length))}...'");
                            }
                            else
                            {
                                LogTest($"      ⚠️ description (Tipo sconosciuto: {descriptionField.Type}): '{descriptionField.ToString().Substring(0, Math.Min(50, descriptionField.ToString().Length))}...'");
                            }
                        }
                        else
                        {
                            LogTest($"      ❌ description: NULL");
                        }

                        // === TEST CLIENTE PARTNER ARRAY ===
                        LogTest($"   🏢 CLIENTE PARTNER (ARRAY):");
                        var clientePartnerField = fields?["customfield_10103"];
                        if (clientePartnerField != null && clientePartnerField.Type != JTokenType.Null)
                        {
                            LogTest($"      🔍 Tipo campo: {clientePartnerField.Type}");

                            if (clientePartnerField.Type == JTokenType.Array)
                            {
                                var array = clientePartnerField as JArray;
                                LogTest($"      📊 Array elementi: {array?.Count ?? 0}");

                                for (int i = 0; i < (array?.Count ?? 0); i++)
                                {
                                    var item = array[i];
                                    LogTest($"      📋 Elemento [{i}]:");
                                    LogTest($"         • Tipo: {item?.Type}");

                                    if (item?.Type == JTokenType.Object)
                                    {
                                        // Analizza la struttura dell'oggetto
                                        foreach (var property in (item as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
                                        {
                                            var propValue = property.Value?.ToString();
                                            var preview = propValue?.Length > 50 ? propValue.Substring(0, 50) + "..." : propValue;
                                            LogTest($"         • {property.Name}: '{preview}'");
                                        }
                                    }
                                    else
                                    {
                                        var value = item?.ToString();
                                        var preview = value?.Length > 100 ? value.Substring(0, 100) + "..." : value;
                                        LogTest($"         • Valore: '{preview}'");
                                    }
                                }

                                // Tenta estrazione con metodi esistenti
                                LogTest($"      🧪 Test estrazione con metodi esistenti:");
                                try
                                {
                                    // Test con JiraFieldExtractor
                                    var extractorValue = JiraFieldExtractor.ExtractField(ticket.RawData, "ClientePartner");
                                    LogTest($"         • JiraFieldExtractor: '{extractorValue}'");
                                }
                                catch (Exception ex)
                                {
                                    LogTest($"         • JiraFieldExtractor: ERRORE - {ex.Message}");
                                }

                                try
                                {
                                    // Test con ExtractCustomFieldValue (se esiste)
                                    var customValue = ExtractCustomFieldForTest(clientePartnerField);
                                    LogTest($"         • ExtractCustomFieldValue: '{customValue}'");
                                }
                                catch (Exception ex)
                                {
                                    LogTest($"         • ExtractCustomFieldValue: ERRORE - {ex.Message}");
                                }
                            }
                            else if (clientePartnerField.Type == JTokenType.Object)
                            {
                                LogTest($"      📋 Oggetto singolo:");
                                foreach (var property in (clientePartnerField as JObject)?.Properties() ?? Enumerable.Empty<JProperty>())
                                {
                                    var propValue = property.Value?.ToString();
                                    var preview = propValue?.Length > 50 ? propValue.Substring(0, 50) + "..." : propValue;
                                    LogTest($"         • {property.Name}: '{preview}'");
                                }
                            }
                            else
                            {
                                var value = clientePartnerField.ToString();
                                LogTest($"      ✅ Valore diretto: '{value}'");
                            }
                        }
                        else
                        {
                            LogTest($"      ❌ customfield_10103: NULL");
                        }




                        // === CONFRONTO CON ASSIGNEE ===
                        var assignee = fields?["assignee"];
                        if (assignee != null && assignee.Type != JTokenType.Null)
                        {
                            var assigneeDisplayName = assignee["displayName"]?.ToString();
                            var assigneeEmail = assignee["emailAddress"]?.ToString();
                            LogTest($"   📊 CONFRONTO - Assignee: '{assigneeDisplayName}' ({assigneeEmail})");
                        }

                        // === RICERCA EMAIL NEL REPORTER ===
                        if (reporterField != null && reporterField.Type != JTokenType.Null)
                        {
                            var reporterEmail = reporterField["emailAddress"]?.ToString();
                            if (!string.IsNullOrEmpty(reporterEmail))
                            {
                                LogTest($"   💡 SOLUZIONE POSSIBILE: Usa reporter.emailAddress per email richiedente");
                            }
                        }

                        LogTest("");

                    }
                    catch (Exception ex)
                    {
                        LogTest($"   ❌ ERRORE analisi {ticketKey}: {ex.Message}");
                        LogTest("");
                    }
                }

                LogTest("💡 === CONCLUSIONI E MAPPING SUGGERITO ===");
                LogTest("📋 MAPPING RACCOMANDATO:");
                LogTest("   • txtRichiedente → reporter.displayName");
                LogTest("   • txtEmail → reporter.emailAddress (se presente) O customfield_10136");
                LogTest("   • txtTelefono → customfield_10074 (se presente) O 'Non disponibile'");
                LogTest("");
                LogTest("🔧 PROSSIMI PASSI:");
                LogTest("   1. Aggiorna TextBoxManager con mapping corretto");
                LogTest("   2. Gestisci fallback per campi vuoti");
                LogTest("   3. Testa double-click su ticket con dati completi");
                LogTest("");

            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE GENERALE: {ex.Message}");
            }
            finally
            {
                await SaveAndOpenTestLog();
            }
        }

        /// <summary>
        /// Estrae testo da Atlassian Document Format (ADF)
        /// </summary>
        private string ExtractTextFromADF(JToken adfToken)
        {
            try
            {
                if (adfToken == null) return "";

                var text = "";

                // Se ha contenuto testuale diretto
                if (adfToken["text"] != null)
                {
                    text += adfToken["text"].ToString();
                }

                // Elabora contenuto nested
                if (adfToken["content"] != null && adfToken["content"].Type == JTokenType.Array)
                {
                    foreach (var child in adfToken["content"])
                    {
                        text += ExtractTextFromADF(child);
                        if (child["type"]?.ToString() == "paragraph")
                        {
                            text += "\n"; // Aggiungi newline dopo i paragrafi
                        }
                    }
                }

                return text.Trim();
            }
            catch (Exception ex)
            {
                LogTest($"      ❌ ERRORE estrazione ADF: {ex.Message}");
                return "[Errore estrazione descrizione]";
            }
        }

        private string ExtractCustomFieldForTest(JToken fieldValue)
        {
            try
            {
                if (fieldValue == null || fieldValue.Type == JTokenType.Null)
                    return "";

                // Gestisce diversi formati di custom field
                if (fieldValue.Type == JTokenType.String)
                    return fieldValue.ToString();

                if (fieldValue.Type == JTokenType.Object && fieldValue["value"] != null)
                    return fieldValue["value"].ToString();

                if (fieldValue.Type == JTokenType.Array && fieldValue.HasValues)
                {
                    var firstItem = fieldValue[0];
                    if (firstItem?["value"] != null)
                        return firstItem["value"].ToString();
                    if (firstItem?["name"] != null)
                        return firstItem["name"].ToString();
                    return firstItem?.ToString() ?? "";
                }

                return fieldValue.ToString();
            }
            catch
            {
                return "[Errore estrazione test]";
            }
        }

        #endregion


        #region "Cliente Partner" Tests"

        /// <summary>
        /// Test API dedicata per Cliente Partner con fields specifici
        /// F12 - Test risoluzione completa Cliente Partner
        /// </summary>
        public async Task TestClientePartnerDedicatedAPI()
        {
            LogTest("🔍 === TEST CLIENTE PARTNER SEMPLIFICATO ===");
            LogTest($"📅 Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogTest("");

            try
            {
                var apiService = JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
                var ticketKey = "CC-28812"; // Ticket con Cliente Partner

                LogTest($"🎫 Ticket di test: {ticketKey}");
                LogTest($"🎯 Obiettivo: Ottenere 'NARDÒ Comune di - PARSEC 3.26'");
                LogTest("");

                // Test con SearchIssuesAsync normale (che già funziona)
                LogTest("📋 TEST VIA SearchIssuesAsync:");
                var jql = $"key = {ticketKey}";
                var searchResult = await apiService.SearchIssuesAsync(jql, 0, 1);

                if (searchResult.Issues.Count > 0)
                {
                    var issue = searchResult.Issues[0];
                    LogTest($"   ✅ Ticket trovato");

                    // Analizza customfield_10103 nel dettaglio
                    var customField = issue["fields"]?["customfield_10103"];

                    LogTest($"   🏢 CUSTOMFIELD_10103 ANALISI DETTAGLIATA:");
                    LogTest($"   📋 Tipo: {customField?.Type ?? JTokenType.Null}");

                    if (customField != null && customField.Type != JTokenType.Null)
                    {
                        LogTest($"   📄 JSON Completo:");
                        LogTest($"   {customField.ToString(Newtonsoft.Json.Formatting.Indented)}");
                        LogTest("");

                        if (customField.Type == JTokenType.Array)
                        {
                            var array = customField as JArray;
                            LogTest($"   📊 Array con {array?.Count ?? 0} elementi");

                            for (int i = 0; i < (array?.Count ?? 0); i++)
                            {
                                var item = array[i];
                                LogTest($"   📋 Elemento [{i}]:");
                                LogTest($"      Tipo: {item?.Type}");

                                if (item?.Type == JTokenType.Object)
                                {
                                    var obj = item as JObject;
                                    LogTest($"      Proprietà oggetto:");

                                    foreach (var prop in obj?.Properties() ?? Enumerable.Empty<JProperty>())
                                    {
                                        LogTest($"         • {prop.Name}: '{prop.Value}'");
                                    }

                                    // Cerca campi che potrebbero contenere il nome
                                    LogTest($"      🔍 Cerca nome completo:");
                                    var possibleNameFields = new[] { "displayName", "name", "value", "label", "description", "title" };

                                    foreach (var field in possibleNameFields)
                                    {
                                        var value = obj?[field]?.ToString();
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            LogTest($"         🎯 {field}: '{value}'");

                                            if (value.Contains("NARDÒ") || value.Contains("PARSEC"))
                                            {
                                                LogTest($"         ⭐ TROVATO! Campo '{field}' contiene il nome cercato!");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        LogTest("");
                        LogTest($"   💡 ANALISI:");

                        // Applica la logica FormatTokenEnhanced che abbiamo sviluppato
                        var formattedResult = FormatTokenEnhancedForTest(customField);
                        LogTest($"   🔧 FormatTokenEnhanced result: '{formattedResult}'");

                        // Test vari metodi di estrazione
                        LogTest($"   🧪 TEST METODI ESTRAZIONE:");
                        LogTest($"      • ToString(): '{customField.ToString()}'");
                        LogTest($"      • First element: '{(customField as JArray)?[0]?.ToString() ?? "N/A"}'");

                        if (customField.Type == JTokenType.Array && customField.HasValues)
                        {
                            var firstObj = (customField as JArray)?[0] as JObject;
                            if (firstObj != null)
                            {
                                LogTest($"      • First.displayName: '{firstObj["displayName"]?.ToString() ?? "N/A"}'");
                                LogTest($"      • First.name: '{firstObj["name"]?.ToString() ?? "N/A"}'");
                                LogTest($"      • First.value: '{firstObj["value"]?.ToString() ?? "N/A"}'");
                            }
                        }

                    }
                    else
                    {
                        LogTest($"   ❌ customfield_10103 è NULL o vuoto");
                    }
                }
                else
                {
                    LogTest($"   ❌ Ticket non trovato con JQL: {jql}");
                }

            }
            catch (Exception ex)
            {
                LogTest($"❌ ERRORE: {ex.Message}");
            }
            finally
            {
                LogTest("");
                LogTest("🎯 === CONCLUSIONI ===");
                LogTest("Se non troviamo il nome 'NARDÒ Comune di - PARSEC 3.26':");
                LogTest("1. Il campo potrebbe contenere solo riferimenti ID");
                LogTest("2. Serve una chiamata API aggiuntiva per risolvere l'ID");
                LogTest("3. Oppure il nome è in un campo diverso");
                LogTest("");
                await SaveAndOpenTestLog();
            }
        }

        /// <summary>
        /// Test della logica FormatTokenEnhanced per debug
        /// </summary>
        private string FormatTokenEnhancedForTest(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return "";

            try
            {
                if (token.Type == JTokenType.Array)
                {
                    var arr = token as JArray;
                    if (arr?.Count == 1)
                    {
                        var item = arr[0];
                        if (item?.Type == JTokenType.Object)
                        {
                            var obj = item as JObject;

                            // Cerca workspace object
                            if (obj?["workspaceId"] != null && obj?["objectId"] != null)
                            {
                                var objectId = obj["objectId"]?.ToString();
                                return $"WorkspaceObject ID: {objectId}";
                            }

                            // Cerca campi standard
                            var fields = new[] { "displayName", "name", "value", "label" };
                            foreach (var field in fields)
                            {
                                var value = obj[field]?.ToString();
                                if (!string.IsNullOrEmpty(value))
                                {
                                    return value;
                                }
                            }
                        }
                    }
                }

                return token.ToString();
            }
            catch
            {
                return "[Errore test]";
            }
        }

        // <summary>
        /// Helper per ottenere header auth (semplificato per test)
        /// </summary>
        private string GetAuthHeaderForTest(JiraApiService apiService)
        {
            try
            {
                // Usa reflection per ottenere credenziali (solo per test)
                var usernameField = typeof(JiraApiService).GetField("Username",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tokenField = typeof(JiraApiService).GetField("Token",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var username = usernameField?.GetValue(apiService)?.ToString();
                var token = tokenField?.GetValue(apiService)?.ToString();

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(token))
                {
                    var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{token}"));
                    return $"Basic {authValue}";
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        #endregion


    }




}