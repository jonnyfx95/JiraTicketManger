using System;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio centralizzato per la gestione del progresso delle operazioni.
    /// Integrato con StatusBar, Toast e Progress UI esistenti.
    /// </summary>
    public interface IProgressService
    {
        #region Progress Operations

        /// <summary>
        /// Avvia una nuova operazione con progresso
        /// </summary>
        /// <param name="operationId">ID univoco dell'operazione</param>
        /// <param name="title">Titolo dell'operazione</param>
        /// <param name="totalSteps">Numero totale di step (opzionale per progress indeterminato)</param>
        void StartOperation(string operationId, string title, int? totalSteps = null);

        /// <summary>
        /// Aggiorna il progresso di un'operazione
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        /// <param name="currentStep">Step corrente</param>
        /// <param name="message">Messaggio di stato</param>
        void UpdateProgress(string operationId, int currentStep, string message);

        /// <summary>
        /// Aggiorna solo il messaggio senza cambiare il progresso
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        /// <param name="message">Nuovo messaggio</param>
        void UpdateMessage(string operationId, string message);

        /// <summary>
        /// Completa un'operazione con successo
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        /// <param name="finalMessage">Messaggio finale</param>
        /// <param name="showToast">Se mostrare toast di successo</param>
        void CompleteOperation(string operationId, string finalMessage = null, bool showToast = true);

        /// <summary>
        /// Termina un'operazione con errore
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        /// <param name="errorMessage">Messaggio di errore</param>
        /// <param name="showToast">Se mostrare toast di errore</param>
        void FailOperation(string operationId, string errorMessage, bool showToast = true);

        /// <summary>
        /// Cancella un'operazione
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        void CancelOperation(string operationId);

        #endregion

        #region Status Management

        /// <summary>
        /// Verifica se un'operazione è in corso
        /// </summary>
        /// <param name="operationId">ID dell'operazione</param>
        /// <returns>True se l'operazione è attiva</returns>
        bool IsOperationActive(string operationId);

        /// <summary>
        /// Ottiene la lista delle operazioni attive
        /// </summary>
        /// <returns>Array di ID operazioni attive</returns>
        string[] GetActiveOperations();

        /// <summary>
        /// Verifica se ci sono operazioni in corso
        /// </summary>
        /// <returns>True se ci sono operazioni attive</returns>
        bool HasActiveOperations();

        #endregion

        #region Events

        /// <summary>
        /// Evento scatenato quando viene avviata un'operazione
        /// </summary>
        event EventHandler<ProgressEventArgs> OperationStarted;

        /// <summary>
        /// Evento scatenato quando viene aggiornato il progresso
        /// </summary>
        event EventHandler<ProgressEventArgs> ProgressUpdated;

        /// <summary>
        /// Evento scatenato quando un'operazione viene completata
        /// </summary>
        event EventHandler<ProgressEventArgs> OperationCompleted;

        /// <summary>
        /// Evento scatenato quando un'operazione fallisce
        /// </summary>
        event EventHandler<ProgressEventArgs> OperationFailed;

        #endregion

        #region Quick Operations

        /// <summary>
        /// Operazione rapida con progress indeterminato
        /// </summary>
        /// <param name="message">Messaggio da mostrare</param>
        /// <returns>ID operazione per controllo</returns>
        string ShowIndeterminateProgress(string message);

        /// <summary>
        /// Nasconde tutti i progress attivi
        /// </summary>
        void HideAllProgress();

        #endregion
    }

    /// <summary>
    /// Argomenti per gli eventi di progresso
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public string OperationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? CurrentStep { get; set; }
        public int? TotalSteps { get; set; }
        public int PercentComplete => TotalSteps.HasValue && TotalSteps > 0
            ? (int)((CurrentStep ?? 0) * 100.0 / TotalSteps.Value)
            : 0;
        public ProgressOperationStatus Status { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public ProgressEventArgs(string operationId, string title, string message,
            int? currentStep = null, int? totalSteps = null,
            ProgressOperationStatus status = ProgressOperationStatus.InProgress)
        {
            OperationId = operationId;
            Title = title;
            Message = message;
            CurrentStep = currentStep;
            TotalSteps = totalSteps;
            Status = status;
        }
    }

    /// <summary>
    /// Stati delle operazioni di progresso
    /// </summary>
    public enum ProgressOperationStatus
    {
        Started,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Operazione di progresso interna
    /// </summary>
    internal class ProgressOperation
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string CurrentMessage { get; set; }
        public int? CurrentStep { get; set; }
        public int? TotalSteps { get; set; }
        public ProgressOperationStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastUpdateTime { get; set; }

        public int PercentComplete => TotalSteps.HasValue && TotalSteps > 0
            ? (int)((CurrentStep ?? 0) * 100.0 / TotalSteps.Value)
            : 0;

        public ProgressOperation(string id, string title, int? totalSteps = null)
        {
            Id = id;
            Title = title;
            TotalSteps = totalSteps;
            CurrentStep = 0;
            Status = ProgressOperationStatus.Started;
            StartTime = DateTime.Now;
            LastUpdateTime = DateTime.Now;
        }
    }
}