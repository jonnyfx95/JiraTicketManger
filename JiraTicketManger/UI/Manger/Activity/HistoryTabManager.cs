// =====================================================
// === BLOCCO 3 - PARTE 3: HISTORY TAB MANAGER ===
// =====================================================

// FILE: UI/Managers/Activity/HistoryTabManager.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data.Models.Activity;
using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;

namespace JiraTicketManager.UI.Managers.Activity
{
    /// <summary>
    /// Manager specializzato per la gestione del tab cronologia.
    /// Implementa uno stile timeline moderno invece del DataGridView tradizionale.
    /// </summary>
    public class HistoryTabManager
    {
        private readonly IActivityService _activityService;
        private readonly LoggingService _logger;
        private ListView _historyListView;

        public HistoryTabManager(IActivityService activityService)
        {
            _activityService = activityService ?? throw new ArgumentNullException(nameof(activityService));
            _logger = LoggingService.CreateForComponent("HistoryTabManager");
        }

        /// <summary>
        /// Carica e visualizza la cronologia nel tab
        /// </summary>
        public async Task LoadHistoryAsync(TabPage historyTabPage, string ticketKey, IProgress<string> progress = null)
        {
            try
            {
                _logger.LogInfo($"Caricamento cronologia nel tab per ticket: {ticketKey}");
                progress?.Report("Caricamento cronologia...");

                // Trova o crea il ListView per la cronologia
                _historyListView = FindOrCreateHistoryListView(historyTabPage);

                if (_historyListView == null)
                {
                    _logger.LogError("Impossibile trovare o creare ListView per cronologia");
                    return;
                }

                // Pulisci il ListView
                _historyListView.Items.Clear();

                // Carica la cronologia dal servizio
                var historyItems = await _activityService.GetHistoryAsync(ticketKey, progress);

                if (historyItems == null || !historyItems.Any())
                {
                    ShowNoHistoryMessage();
                    _logger.LogInfo("Nessuna cronologia trovata");
                    return;
                }

                // Popola il ListView con la cronologia
                PopulateHistoryListView(historyItems);

                progress?.Report($"Caricati {historyItems.Count} elementi cronologia");
                _logger.LogInfo($"Caricati e visualizzati {historyItems.Count} elementi cronologia");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore caricamento cronologia per {ticketKey}", ex);
                ShowError(historyTabPage, $"Errore caricamento cronologia: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Pulisce il tab cronologia
        /// </summary>
        public void ClearTab(TabPage historyTabPage)
        {
            try
            {
                if (historyTabPage == null) return;

                var listView = FindHistoryListView(historyTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();
                    _logger.LogDebug("Tab cronologia pulito");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore pulizia tab cronologia", ex);
            }
        }

        /// <summary>
        /// Mostra un messaggio di errore nel tab
        /// </summary>
        public void ShowError(TabPage historyTabPage, string errorMessage)
        {
            try
            {
                if (historyTabPage == null) return;

                var listView = FindOrCreateHistoryListView(historyTabPage);
                if (listView != null)
                {
                    listView.Items.Clear();

                    // Crea un elemento di errore
                    var errorItem = new ListViewItem("❌ Errore");
                    errorItem.SubItems.Add(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    errorItem.SubItems.Add(errorMessage);
                    errorItem.SubItems.Add("Sistema");
                    errorItem.ForeColor = Color.Red;
                    errorItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                    listView.Items.Add(errorItem);
                    _logger.LogDebug($"Messaggio di errore mostrato nel tab cronologia: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione errore tab cronologia", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// Trova il ListView della cronologia nel TabPage o lo crea se non esiste
        /// </summary>
        private ListView FindOrCreateHistoryListView(TabPage historyTabPage)
        {
            if (historyTabPage == null) return null;

            // Cerca un ListView esistente
            var existingListView = FindHistoryListView(historyTabPage);
            if (existingListView != null)
                return existingListView;

            // Se non esiste, crealo
            return CreateHistoryListView(historyTabPage);
        }

        /// <summary>
        /// Trova il ListView della cronologia esistente
        /// </summary>
        private ListView FindHistoryListView(TabPage historyTabPage)
        {
            // Cerca per nome (dal Designer)
            var listView = historyTabPage.Controls.Find("lvHistory", true).FirstOrDefault() as ListView;

            // Se non trovato per nome, cerca il primo ListView
            if (listView == null)
            {
                listView = historyTabPage.Controls.OfType<ListView>().FirstOrDefault();
            }

            return listView;
        }

        /// <summary>
        /// Crea un nuovo ListView per la cronologia con stile timeline
        /// </summary>
        private ListView CreateHistoryListView(TabPage historyTabPage)
        {
            try
            {
                var listView = new ListView
                {
                    Name = "lvHistoryGenerated",
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Segoe UI", 9F),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };

                // Configura le colonne per stile timeline
                SetupHistoryColumns(listView);

                // Aggiungi al TabPage
                historyTabPage.Controls.Add(listView);

                _logger.LogDebug("ListView cronologia creato dinamicamente");
                return listView;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore creazione ListView cronologia", ex);
                return null;
            }
        }

        /// <summary>
        /// Configura le colonne del ListView per uno stile timeline
        /// </summary>
        private void SetupHistoryColumns(ListView listView)
        {
            listView.Columns.Clear();

            // Colonne ottimizzate per visualizzazione timeline
            listView.Columns.Add("⚡ Azione", 120);        // Tipo azione con icona
            listView.Columns.Add("📅 Data", 120);          // Data
            listView.Columns.Add("🔄 Modifiche", 350);     // Descrizione modifiche
            listView.Columns.Add("👤 Autore", 150);        // Autore
        }

        /// <summary>
        /// Popola il ListView con la lista degli elementi cronologia
        /// </summary>
        private void PopulateHistoryListView(List<JiraHistoryItem> historyItems)
        {
            try
            {
                _historyListView.Items.Clear();

                // Ordina la cronologia per data (più recenti prima)
                var sortedHistory = historyItems.OrderByDescending(h => h.Created).ToList();

                foreach (var historyItem in sortedHistory)
                {
                    AddHistoryItemToListView(historyItem);
                }

                // Auto-ridimensiona le colonne
                _historyListView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

                _logger.LogDebug($"Popolamento ListView cronologia completato con {historyItems.Count} elementi");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore popolamento ListView cronologia", ex);
            }
        }

        /// <summary>
        /// Aggiunge un singolo elemento cronologia al ListView con stile timeline
        /// </summary>
        private void AddHistoryItemToListView(JiraHistoryItem historyItem)
        {
            try
            {
                // Per ogni JiraHistoryItem, aggiungi una riga per ogni Change
                if (historyItem.Changes != null && historyItem.Changes.Any())
                {
                    foreach (var change in historyItem.Changes)
                    {
                        AddSingleChangeToListView(historyItem, change);
                    }
                }
                else
                {
                    // Se non ci sono change specifici, aggiungi una riga generica
                    AddGenericHistoryItemToListView(historyItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiunta elemento cronologia al ListView: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiunge una singola modifica al ListView
        /// </summary>
        private void AddSingleChangeToListView(JiraHistoryItem historyItem, JiraHistoryChange change)
        {
            try
            {
                var item = new ListViewItem();

                // Colonna 1: Icona + tipo campo
                var actionText = GetChangeIcon(change.Field) + " " + change.FieldDisplayName;
                item.Text = actionText;

                // Colonna 2: Data formattata
                item.SubItems.Add(historyItem.FormattedCreated);

                // Colonna 3: Descrizione della modifica con "Da → A"
                item.SubItems.Add(change.ChangeDescription);

                // Colonna 4: Autore
                item.SubItems.Add(historyItem.AuthorDisplayName ?? historyItem.Author);

                // Applica stile visivo basato sul tipo di campo
                ApplyHistoryStyling(item, change);

                // Salva gli oggetti nel Tag per uso futuro
                item.Tag = new { HistoryItem = historyItem, Change = change };

                _historyListView.Items.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiunta singola modifica al ListView: {ex.Message}");
            }
        }

        /// <summary>
        /// Aggiunge un elemento cronologia generico (senza change specifici)
        /// </summary>
        private void AddGenericHistoryItemToListView(JiraHistoryItem historyItem)
        {
            try
            {
                var item = new ListViewItem();

                // Colonna 1: Azione generica
                item.Text = "📝 Modifica";

                // Colonna 2: Data
                item.SubItems.Add(historyItem.FormattedCreated);

                // Colonna 3: Descrizione generica
                item.SubItems.Add(historyItem.ChangesSummary);

                // Colonna 4: Autore
                item.SubItems.Add(historyItem.AuthorDisplayName ?? historyItem.Author);

                // Stile neutro
                item.ForeColor = Color.FromArgb(108, 117, 125); // Grigio

                item.Tag = historyItem;
                _historyListView.Items.Add(item);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore aggiunta elemento generico cronologia: {ex.Message}");
            }
        }

        /// <summary>
        /// Ottiene l'icona appropriata per il tipo di campo modificato
        /// </summary>
        private string GetChangeIcon(string fieldName)
        {
            return fieldName?.ToLower() switch
            {
                "status" => "🔄",          // Cambio stato
                "assignee" => "👤",        // Assegnazione
                "priority" => "⚡",        // Priorità
                "summary" => "📝",         // Titolo
                "description" => "📄",     // Descrizione
                "fixversion" => "🏷️",      // Versione
                "component" => "🧩",       // Componente
                "attachment" => "📎",      // Allegato
                "issuetype" => "🎯",       // Tipo ticket
                "resolution" => "✅",      // Risoluzione
                "reporter" => "📢",        // Segnalatore
                "labels" => "🏷️",          // Etichette
                "timeestimate" => "⏱️",     // Stima tempo
                "timespent" => "⏰",       // Tempo speso
                _ => "✏️"                 // Default
            };
        }

        /// <summary>
        /// Applica lo stile visivo all'elemento cronologia
        /// </summary>
        private void ApplyHistoryStyling(ListViewItem item, JiraHistoryChange change)
        {
            // Colori basati sul tipo di campo (dal modello JiraHistoryChange)
            var fieldColor = ColorTranslator.FromHtml(change.FieldColor);

            // Applica il colore del testo
            item.ForeColor = fieldColor;

            // Evidenzia modifiche importanti
            if (IsImportantChange(change.Field))
            {
                item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            }

            // Stile per modifiche recenti (meno di 24 ore)
            if (item.Tag is { } tag && tag.GetType().GetProperty("HistoryItem")?.GetValue(tag) is JiraHistoryItem historyItem)
            {
                if ((DateTime.Now - historyItem.Created).TotalHours < 24)
                {
                    item.BackColor = Color.FromArgb(248, 249, 250); // Grigio molto chiaro
                }
            }
        }

        /// <summary>
        /// Determina se una modifica è considerata importante
        /// </summary>
        private bool IsImportantChange(string fieldName)
        {
            var importantFields = new[] { "status", "assignee", "priority", "resolution" };
            return importantFields.Contains(fieldName?.ToLower());
        }

        /// <summary>
        /// Mostra un messaggio quando non c'è cronologia
        /// </summary>
        private void ShowNoHistoryMessage()
        {
            try
            {
                var noHistoryItem = new ListViewItem("📅 Nessuna cronologia");
                noHistoryItem.SubItems.Add("");
                noHistoryItem.SubItems.Add("Non ci sono modifiche registrate per questo ticket");
                noHistoryItem.SubItems.Add("");
                noHistoryItem.ForeColor = Color.Gray;
                noHistoryItem.Font = new Font("Segoe UI", 9, FontStyle.Italic);

                _historyListView.Items.Add(noHistoryItem);
                _logger.LogDebug("Messaggio 'nessuna cronologia' visualizzato");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore visualizzazione messaggio 'nessuna cronologia'", ex);
            }
        }

        #endregion
    }
}