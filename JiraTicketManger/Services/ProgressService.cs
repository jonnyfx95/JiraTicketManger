using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Forms;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Implementazione del servizio di progresso integrato con MainForm esistente.
    /// Gestisce StatusBar, Toast notifications e stato UI.
    /// </summary>
    public class ProgressService : IProgressService
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly WindowsToastService _toastService;
        private readonly ConcurrentDictionary<string, ProgressOperation> _activeOperations;

        // UI Components (integrazione con MainForm esistente)
        private StatusStrip _statusStrip;
        private Form _parentForm;
        private Action<string> _updateStatusCallback;
        private Action<bool> _setLoadingStateCallback;

        #endregion

        #region Events

        public event EventHandler<ProgressEventArgs> OperationStarted;
        public event EventHandler<ProgressEventArgs> ProgressUpdated;
        public event EventHandler<ProgressEventArgs> OperationCompleted;
        public event EventHandler<ProgressEventArgs> OperationFailed;

        #endregion

        #region Constructor

        public ProgressService(WindowsToastService toastService, Form parentForm = null)
        {
            _logger = LoggingService.CreateForComponent("ProgressService");
            _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
            _activeOperations = new ConcurrentDictionary<string, ProgressOperation>();
            _parentForm = parentForm;

            // Auto-discover StatusStrip nel MainForm
            if (_parentForm != null)
            {
                _statusStrip = FindStatusStrip(_parentForm);
            }

            _logger.LogInfo("ProgressService inizializzato");
        }

        #endregion

        #region Public Methods - Progress Operations

        public void StartOperation(string operationId, string title, int? totalSteps = null)
        {
            try
            {
                _logger.LogInfo($"🚀 Avvio operazione: {operationId} - {title}");

                var operation = new ProgressOperation(operationId, title, totalSteps);
                _activeOperations.AddOrUpdate(operationId, operation, (key, existing) => operation);

                // Update UI
                UpdateUIForOperation(operation);
                SetLoadingState(true);

                // Fire event
                var args = CreateEventArgs(operation);
                OperationStarted?.Invoke(this, args);

                _logger.LogDebug($"Operazione {operationId} avviata - TotalSteps: {totalSteps}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore avvio operazione {operationId}", ex);
            }
        }

        public void UpdateProgress(string operationId, int currentStep, string message)
        {
            try
            {
                if (!_activeOperations.TryGetValue(operationId, out var operation))
                {
                    _logger.LogWarning($"Operazione {operationId} non trovata per update progress");
                    return;
                }

                operation.CurrentStep = currentStep;
                operation.CurrentMessage = message;
                operation.LastUpdateTime = DateTime.Now;
                operation.Status = ProgressOperationStatus.InProgress;

                // Update UI
                UpdateUIForOperation(operation);

                // Fire event
                var args = CreateEventArgs(operation);
                ProgressUpdated?.Invoke(this, args);

                _logger.LogDebug($"Progress {operationId}: {currentStep}/{operation.TotalSteps} - {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore update progress {operationId}", ex);
            }
        }

        public void UpdateMessage(string operationId, string message)
        {
            try
            {
                if (!_activeOperations.TryGetValue(operationId, out var operation))
                {
                    _logger.LogWarning($"Operazione {operationId} non trovata per update message");
                    return;
                }

                operation.CurrentMessage = message;
                operation.LastUpdateTime = DateTime.Now;

                // Update UI
                UpdateUIForOperation(operation);

                // Fire event
                var args = CreateEventArgs(operation);
                ProgressUpdated?.Invoke(this, args);

                _logger.LogDebug($"Message update {operationId}: {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore update message {operationId}", ex);
            }
        }

        public void CompleteOperation(string operationId, string finalMessage = null, bool showToast = true)
        {
            try
            {
                if (!_activeOperations.TryGetValue(operationId, out var operation))
                {
                    _logger.LogWarning($"Operazione {operationId} non trovata per complete");
                    return;
                }

                operation.Status = ProgressOperationStatus.Completed;
                operation.CurrentMessage = finalMessage ?? $"{operation.Title} completato";
                operation.LastUpdateTime = DateTime.Now;

                // Show success toast
                if (showToast)
                {
                    _toastService.ShowSuccess("Operazione completata", operation.CurrentMessage);
                }

                // Fire event before removal
                var args = CreateEventArgs(operation);
                OperationCompleted?.Invoke(this, args);

                // Remove from active operations
                _activeOperations.TryRemove(operationId, out _);

                // Update UI
                UpdateUIAfterOperationEnd();

                var duration = DateTime.Now - operation.StartTime;
                _logger.LogInfo($"✅ Operazione {operationId} completata in {duration.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore complete operation {operationId}", ex);
            }
        }

        public void FailOperation(string operationId, string errorMessage, bool showToast = true)
        {
            try
            {
                if (!_activeOperations.TryGetValue(operationId, out var operation))
                {
                    _logger.LogWarning($"Operazione {operationId} non trovata per fail");
                    return;
                }

                operation.Status = ProgressOperationStatus.Failed;
                operation.CurrentMessage = errorMessage;
                operation.LastUpdateTime = DateTime.Now;

                // Show error toast
                if (showToast)
                {
                    _toastService.ShowError("Operazione fallita", errorMessage);
                }

                // Fire event before removal
                var args = CreateEventArgs(operation);
                OperationFailed?.Invoke(this, args);

                // Remove from active operations
                _activeOperations.TryRemove(operationId, out _);

                // Update UI
                UpdateUIAfterOperationEnd();

                _logger.LogError($"❌ Operazione {operationId} fallita: {errorMessage}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore fail operation {operationId}", ex);
            }
        }

        public void CancelOperation(string operationId)
        {
            try
            {
                if (!_activeOperations.TryGetValue(operationId, out var operation))
                {
                    _logger.LogWarning($"Operazione {operationId} non trovata per cancel");
                    return;
                }

                operation.Status = ProgressOperationStatus.Cancelled;
                operation.LastUpdateTime = DateTime.Now;

                // Remove from active operations
                _activeOperations.TryRemove(operationId, out _);

                // Update UI
                UpdateUIAfterOperationEnd();

                _logger.LogInfo($"🚫 Operazione {operationId} cancellata");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore cancel operation {operationId}", ex);
            }
        }

        #endregion

        #region Public Methods - Status Management

        public bool IsOperationActive(string operationId)
        {
            return _activeOperations.ContainsKey(operationId);
        }

        public string[] GetActiveOperations()
        {
            return _activeOperations.Keys.ToArray();
        }

        public bool HasActiveOperations()
        {
            return _activeOperations.Count > 0;
        }

        #endregion

        #region Public Methods - Quick Operations

        public string ShowIndeterminateProgress(string message)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8]; // Short ID
            StartOperation(operationId, "Operazione in corso", null);
            UpdateMessage(operationId, message);
            return operationId;
        }

        public void HideAllProgress()
        {
            try
            {
                var activeOps = _activeOperations.Keys.ToArray();
                foreach (var operationId in activeOps)
                {
                    CancelOperation(operationId);
                }

                SetLoadingState(false);
                UpdateStatusMessage("Pronto");

                _logger.LogInfo($"Nascosti {activeOps.Length} progress attivi");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore hide all progress", ex);
            }
        }

        #endregion

        #region UI Integration Methods

        /// <summary>
        /// Configura i callback per l'integrazione con MainForm
        /// </summary>
        public void ConfigureUICallbacks(Action<string> updateStatusCallback, Action<bool> setLoadingStateCallback)
        {
            _updateStatusCallback = updateStatusCallback;
            _setLoadingStateCallback = setLoadingStateCallback;
            _logger.LogDebug("UI Callbacks configurati");
        }

        /// <summary>
        /// Imposta manualmente il StatusStrip da utilizzare
        /// </summary>
        public void SetStatusStrip(StatusStrip statusStrip)
        {
            _statusStrip = statusStrip;
            _logger.LogDebug("StatusStrip configurato manualmente");
        }

        #endregion

        #region Private UI Methods

        private void UpdateUIForOperation(ProgressOperation operation)
        {
            try
            {
                var message = FormatProgressMessage(operation);
                UpdateStatusMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore update UI per {operation.Id}: {ex.Message}");
            }
        }

        private void UpdateUIAfterOperationEnd()
        {
            try
            {
                if (_activeOperations.IsEmpty)
                {
                    SetLoadingState(false);
                    UpdateStatusMessage("Pronto");
                }
                else
                {
                    // Se ci sono ancora operazioni attive, mostra la prima
                    var firstActive = _activeOperations.Values.FirstOrDefault();
                    if (firstActive != null)
                    {
                        UpdateUIForOperation(firstActive);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore update UI after operation end: {ex.Message}");
            }
        }

        private string FormatProgressMessage(ProgressOperation operation)
        {
            if (operation.TotalSteps.HasValue)
            {
                // Progress determinato
                var percent = operation.PercentComplete;
                return $"{operation.CurrentMessage} ({percent}%)";
            }
            else
            {
                // Progress indeterminato
                return operation.CurrentMessage ?? operation.Title;
            }
        }

        private void UpdateStatusMessage(string message)
        {
            try
            {
                // Usa callback se disponibile
                if (_updateStatusCallback != null)
                {
                    _updateStatusCallback(message);
                    return;
                }

                // Fallback su StatusStrip diretto
                if (_statusStrip != null && _statusStrip.Items.Count > 1)
                {
                    if (_statusStrip.InvokeRequired)
                    {
                        _statusStrip.Invoke(() => _statusStrip.Items[1].Text = message);
                    }
                    else
                    {
                        _statusStrip.Items[1].Text = message;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore update status message: {ex.Message}");
            }
        }

        private void SetLoadingState(bool isLoading)
        {
            try
            {
                // Usa callback se disponibile
                if (_setLoadingStateCallback != null)
                {
                    _setLoadingStateCallback(isLoading);
                    return;
                }

                // Fallback su form diretto
                if (_parentForm != null)
                {
                    if (_parentForm.InvokeRequired)
                    {
                        _parentForm.Invoke(() => _parentForm.UseWaitCursor = isLoading);
                    }
                    else
                    {
                        _parentForm.UseWaitCursor = isLoading;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Errore set loading state: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private StatusStrip FindStatusStrip(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is StatusStrip statusStrip)
                    return statusStrip;

                var found = FindStatusStrip(control);
                if (found != null)
                    return found;
            }
            return null;
        }

        private ProgressEventArgs CreateEventArgs(ProgressOperation operation)
        {
            return new ProgressEventArgs(
                operation.Id,
                operation.Title,
                operation.CurrentMessage,
                operation.CurrentStep,
                operation.TotalSteps,
                operation.Status
            );
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Crea ProgressService per MainForm con auto-configurazione
        /// </summary>
        public static ProgressService CreateForMainForm(MainForm mainForm, WindowsToastService toastService)
        {
            var service = new ProgressService(toastService, mainForm);

            // Auto-configura i callback usando i metodi esistenti del MainForm
            service.ConfigureUICallbacks(
                message => mainForm.Invoke(() => {
                    // Usa il metodo UpdateStatusMessage esistente
                    var method = mainForm.GetType().GetMethod("UpdateStatusMessage",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainForm, new object[] { message });
                }),
                isLoading => mainForm.Invoke(() => {
                    // Usa i metodi ShowProgress/HideProgress esistenti
                    if (isLoading)
                    {
                        var showMethod = mainForm.GetType().GetMethod("ShowProgress",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        showMethod?.Invoke(mainForm, new object[] { "" });
                    }
                    else
                    {
                        var hideMethod = mainForm.GetType().GetMethod("HideProgress",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        hideMethod?.Invoke(mainForm, null);
                    }
                })
            );

            return service;
        }

        /// <summary>
        /// Crea ProgressService standalone per testing
        /// </summary>
        public static ProgressService CreateStandalone(WindowsToastService toastService)
        {
            return new ProgressService(toastService);
        }

        #endregion
    }
}