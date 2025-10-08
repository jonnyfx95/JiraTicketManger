// ============================================================================
// PASSO 1: Crea nuovo file Services/TicketClosureService.cs
// ============================================================================

using JiraTicketManager.Services;
using JiraTicketManager.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;


namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio per gestire la chiusura automatica dei ticket con campi custom appropriati
    /// Supporta diversi tipi di ticket (pianificazione, intervento, etc.)
    /// </summary>
    public class TicketClosureService : IDisposable
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly JiraApiService _jiraApiService;
        private readonly JiraTransitionService _transitionService;
        private readonly WorkspaceObjectResolver _workspaceResolver;
        private bool _disposed = false;

        // Costanti per i campi di chiusura pianificazione
        private const string WORKSPACE_ID = "c541ca01-a3a4-400b-a389-573d1f19899a";
        private const string CATEGORIA_OBJECT_ID = "958";  // PIANIFICAZIONE
        private const string MOTIVAZIONE_OBJECT_ID = "769"; // Inviata in Pianificazione

        #endregion

        #region Constructor

        public TicketClosureService(JiraApiService jiraApiService)
        {
            _jiraApiService = jiraApiService ?? throw new ArgumentNullException(nameof(jiraApiService));
            _logger = LoggingService.CreateForComponent("TicketClosureService");
            _transitionService = new JiraTransitionService(_jiraApiService);
            _workspaceResolver = new WorkspaceObjectResolver();

            _logger.LogDebug("TicketClosureService inizializzato");
        }

        /// <summary>
        /// Factory method per creare istanza da SettingsService
        /// </summary>
        public static TicketClosureService CreateFromSettings(SettingsService settingsService)
        {
            var jiraApiService = JiraApiService.CreateFromSettings(settingsService);
            return new TicketClosureService(jiraApiService);
        }

        #endregion

        #region Main Closure Methods

        /// <summary>
        /// Chiude un ticket di pianificazione con i campi custom appropriati
        /// </summary>
        public async Task<ClosureResult> ClosePlanningTicketAsync(string ticketKey)
        {
            if (string.IsNullOrEmpty(ticketKey))
            {
                throw new ArgumentException("TicketKey non può essere vuoto", nameof(ticketKey));
            }

            var operationId = Guid.NewGuid().ToString();
            _logger.LogInfo($"[{operationId}] Inizio chiusura ticket pianificazione: {ticketKey}");

            try
            {
                // FASE 1: Validazione ticket
                _logger.LogInfo($"[{operationId}] FASE 1: Validazione ticket");
                var validation = await ValidateTicketForClosureAsync(ticketKey);
                if (!validation.IsValid)
                {
                    return new ClosureResult
                    {
                        TicketKey = ticketKey,
                        Success = false,
                        ErrorMessage = validation.ErrorMessage,
                        Phase = "Validazione"
                    };
                }

                // FASE 2: Preparazione campi custom
                _logger.LogInfo($"[{operationId}] FASE 2: Preparazione campi custom");
                var customFields = await PreparePlanningClosureFieldsAsync();

                // FASE 3: Aggiornamento campi
                _logger.LogInfo($"[{operationId}] FASE 3: Aggiornamento campi custom");
                var updateSuccess = await UpdateTicketCustomFieldsAsync(ticketKey, customFields);
                if (!updateSuccess)
                {
                    return new ClosureResult
                    {
                        TicketKey = ticketKey,
                        Success = false,
                        ErrorMessage = "Errore aggiornamento campi custom",
                        Phase = "Aggiornamento Campi"
                    };
                }

                // FASE 4: Transizione a completato
                _logger.LogInfo($"[{operationId}] FASE 4: Transizione a completato");
                var transitionResult = await _transitionService.CompleteTicketAsync(ticketKey);
                if (!transitionResult.Success)
                {
                    return new ClosureResult
                    {
                        TicketKey = ticketKey,
                        Success = false,
                        ErrorMessage = $"Errore transizione: {transitionResult.ErrorMessage}",
                        Phase = "Transizione",
                        FieldsUpdated = true
                    };
                }

                _logger.LogInfo($"[{operationId}] ✅ Ticket {ticketKey} chiuso con successo");

                return new ClosureResult
                {
                    TicketKey = ticketKey,
                    Success = true,
                    FinalStatus = transitionResult.NewStatus,
                    Phase = "Completato",
                    FieldsUpdated = true,
                    TransitionCompleted = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{operationId}] Errore chiusura {ticketKey}", ex);
                return new ClosureResult
                {
                    TicketKey = ticketKey,
                    Success = false,
                    ErrorMessage = ex.Message,
                    Phase = "Errore Generale"
                };
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Valida se il ticket può essere chiuso
        /// </summary>
        private async Task<ValidationResult> ValidateTicketForClosureAsync(string ticketKey)
        {
            try
            {
                // Controlla stato corrente
                var currentStatus = await _transitionService.GetCurrentStatusAsync(ticketKey);
                if (string.IsNullOrEmpty(currentStatus))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Impossibile ottenere stato ticket"
                    };
                }

                // Stati che NON possono essere chiusi
                var finalStates = new[] { "Chiuso", "Closed", "Done", "Completato", "Risolto" };
                if (finalStates.Any(state =>
                    string.Equals(currentStatus, state, StringComparison.OrdinalIgnoreCase)))
                {
                    return new ValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Ticket già in stato finale: {currentStatus}"
                    };
                }

                return new ValidationResult
                {
                    IsValid = true,
                    CurrentStatus = currentStatus
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore validazione {ticketKey}", ex);
                return new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region Custom Fields Methods

        /// <summary>
        /// Prepara i campi custom per chiusura pianificazione
        /// </summary>
        private async Task<Dictionary<string, object>> PreparePlanningClosureFieldsAsync()
        {
            try
            {
                // Verifica valori workspace (per debug)
                var categoriaName = await _workspaceResolver.ResolveWorkspaceObjectAsync(
                    WORKSPACE_ID, CATEGORIA_OBJECT_ID);
                var motivazioneName = await _workspaceResolver.ResolveWorkspaceObjectAsync(
                    WORKSPACE_ID, MOTIVAZIONE_OBJECT_ID);

                _logger.LogDebug($"Categoria risolto: {categoriaName}");
                _logger.LogDebug($"Motivazione risolto: {motivazioneName}");

                // Prepara i campi nel formato corretto per l'API
                var fields = new Dictionary<string, object>
                {
                    // customfield_10095 (Categoria) - CMDB workspace format
                    ["customfield_10095"] = new[]
                    {
                        new
                        {
                            workspaceId = WORKSPACE_ID,
                            id = $"{WORKSPACE_ID}:{CATEGORIA_OBJECT_ID}",
                            objectId = CATEGORIA_OBJECT_ID
                        }
                    },

                    // customfield_10109 (Motivazione Chiusura) - CMDB workspace format
                    ["customfield_10109"] = new[]
                    {
                        new
                        {
                            workspaceId = WORKSPACE_ID,
                            id = $"{WORKSPACE_ID}:{MOTIVAZIONE_OBJECT_ID}",
                            objectId = MOTIVAZIONE_OBJECT_ID
                        }
                    },

                    // customfield_10087 (Metodologia Chiusura) - option format
                    ["customfield_10087"] = new { value = "Schedulata" }
                };

                _logger.LogDebug($"Campi custom preparati: {fields.Count} campi");
                return fields;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore preparazione campi custom", ex);
                throw;
            }
        }

        /// <summary>
        /// Aggiorna i campi custom del ticket usando i metodi helper
        /// </summary>
        private async Task<bool> UpdateTicketCustomFieldsAsync(string ticketKey, Dictionary<string, object> customFields)
        {
            try
            {
                _logger.LogInfo($"Aggiornamento campi custom per {ticketKey} usando metodi helper");

                // Prepara i campi workspace
                var workspaceFields = new Dictionary<string, (string workspaceId, string objectId)>
                {
                    ["customfield_10095"] = (WORKSPACE_ID, CATEGORIA_OBJECT_ID),      // Categoria
                    ["customfield_10109"] = (WORKSPACE_ID, MOTIVAZIONE_OBJECT_ID)     // Motivazione
                };

                // Aggiorna campi workspace in batch
                var workspaceSuccess = await _jiraApiService.UpdateMultipleWorkspaceFieldsAsync(ticketKey, workspaceFields);
                if (!workspaceSuccess)
                {
                    _logger.LogError($"❌ Errore aggiornamento campi workspace per {ticketKey}");
                    return false;
                }

                _logger.LogInfo($"✅ Campi workspace aggiornati per {ticketKey}");

                // Aggiorna campo Metodologia Chiusura (option field)
                var methodologySuccess = await _jiraApiService.UpdateOptionFieldAsync(
                    ticketKey, "customfield_10087", "Schedulata");

                if (!methodologySuccess)
                {
                    _logger.LogError($"❌ Errore aggiornamento Metodologia Chiusura per {ticketKey}");
                    return false;
                }

                _logger.LogInfo($"✅ Metodologia Chiusura aggiornata per {ticketKey}");

                // Log finale
                _logger.LogInfo($"✅ Tutti i campi custom aggiornati per {ticketKey}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore update campi {ticketKey}", ex);
                return false;
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // JiraTransitionService non implementa IDisposable
                    _workspaceResolver?.Dispose();
                    _logger.LogDebug("TicketClosureService disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Errore dispose TicketClosureService", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// Risultato operazione di chiusura
    /// </summary>
    public class ClosureResult
    {
        public string TicketKey { get; set; } = "";
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string FinalStatus { get; set; } = "";
        public string Phase { get; set; } = "";
        public bool FieldsUpdated { get; set; }
        public bool TransitionCompleted { get; set; }

        public string GetSummary()
        {
            if (Success)
                return $"✅ {TicketKey} chiuso con successo (stato: {FinalStatus})";
            else
                return $"❌ {TicketKey} - Errore in fase {Phase}: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Risultato validazione ticket
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string CurrentStatus { get; set; } = "";
    }

    #endregion
}