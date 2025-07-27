using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraTicketManager.Business;
using JiraTicketManager.Data.Models;

namespace JiraTicketManager.Data
{
    /// <summary>
    /// Interface per il servizio dati Jira.
    /// Separa la logica di accesso ai dati dalla UI.
    /// </summary>
    public interface IJiraDataService
    {
        #region Connection & Authentication

        /// <summary>
        /// Testa la connessione alle API Jira
        /// </summary>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Verifica se il servizio è configurato correttamente
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Dominio Jira configurato
        /// </summary>
        string Domain { get; }

        #endregion

        #region Ticket Search & Retrieval

        /// <summary>
        /// Ricerca ticket usando criteri strutturati
        /// </summary>
        Task<JiraSearchResult> SearchTicketsAsync(JiraSearchCriteria criteria, PaginationConfig pagination);

        /// <summary>
        /// Ricerca ticket usando JQL personalizzata
        /// </summary>
        Task<JiraSearchResult> SearchTicketsAsync(string jql, int startAt = 0, int maxResults = 50);

        /// <summary>
        /// Ottiene un singolo ticket per chiave
        /// </summary>
        Task<JiraTicket> GetTicketAsync(string ticketKey);

        /// <summary>
        /// Carica i dati iniziali con JQL di base
        /// </summary>
        Task<JiraSearchResult> LoadInitialDataAsync(int pageSize = 50);

        #endregion

        #region Field Values Loading
        /// <summary>
        /// Carica i valori per un tipo di campo specifico
        /// </summary>
        Task<List<JiraField>> GetFieldValuesAsync(JiraFieldType fieldType, IProgress<string> progress = null);
        /// <summary>
        /// Carica valori per un custom field specifico
        /// </summary>
        Task<List<JiraField>> GetCustomFieldValuesAsync(string fieldId, IProgress<string> progress = null);


        #endregion

        #region Batch Operations

        /// <summary>
        /// Carica tutti i valori dei campi in parallelo
        /// </summary>
        Task<Dictionary<JiraFieldType, List<JiraField>>> LoadAllFieldValuesAsync(IProgress<string> progress = null);

        /// <summary>
        /// Carica valori per campi specificati
        /// </summary>
        Task<Dictionary<JiraFieldType, List<JiraField>>> LoadFieldValuesAsync(
            IEnumerable<JiraFieldType> fieldTypes,
            IProgress<string> progress = null);

        #endregion

        #region Statistics & Analysis

        /// <summary>
        /// Ottiene statistiche sui ticket
        /// </summary>
        Task<JiraStatistics> GetStatisticsAsync(JiraSearchCriteria criteria = null);

        /// <summary>
        /// Conta i ticket per criterio
        /// </summary>
        Task<int> CountTicketsAsync(JiraSearchCriteria criteria);

        #endregion

        #region Error Handling & Events

        /// <summary>
        /// Evento per errori di connessione
        /// </summary>
        event EventHandler<JiraErrorEventArgs> ConnectionError;

        /// <summary>
        /// Evento per aggiornamenti di progresso
        /// </summary>
        event EventHandler<ProgressEventArgs> ProgressUpdated;

        #endregion
    }

    /// <summary>
    /// Statistiche sui ticket Jira
    /// </summary>
    public class JiraStatistics
    {
        public int TotalTickets { get; set; }
        public Dictionary<string, int> TicketsByStatus { get; set; } = new();
        public Dictionary<string, int> TicketsByPriority { get; set; } = new();
        public Dictionary<string, int> TicketsByType { get; set; } = new();
        public Dictionary<string, int> TicketsByAssignee { get; set; } = new();
        public DateTime? OldestTicket { get; set; }
        public DateTime? NewestTicket { get; set; }
        public TimeSpan? AverageAge { get; set; }
    }

    /// <summary>
    /// Argomenti per eventi di errore Jira
    /// </summary>
    public class JiraErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }
        public string Operation { get; }
        public bool IsRetryable { get; }

        public JiraErrorEventArgs(string message, Exception exception = null, string operation = "", bool isRetryable = false)
        {
            Message = message;
            Exception = exception;
            Operation = operation;
            IsRetryable = isRetryable;
        }
    }

    /// <summary>
    /// Argomenti per eventi di progresso
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public string Message { get; }
        public int PercentComplete { get; }
        public string Operation { get; }

        public ProgressEventArgs(string message, int percentComplete = 0, string operation = "")
        {
            Message = message;
            PercentComplete = percentComplete;
            Operation = operation;
        }
    }

    /// <summary>
    /// Configurazione per il servizio dati Jira
    /// </summary>
    public class JiraDataServiceConfig
    {
        public string Domain { get; set; } = "";
        public string Username { get; set; } = "";
        public string Token { get; set; } = "";
        public bool UseSSO { get; set; } = false;
        public int TimeoutSeconds { get; set; } = 30;
        public int MaxRetries { get; set; } = 3;
        public bool EnableCaching { get; set; } = true;
        public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// JQL di base per il caricamento iniziale
        /// </summary>
        public string BaseJQL { get; set; } = "project = CC AND statuscategory = \"In Progress\" ORDER BY updated DESC";

        /// <summary>
        /// Campi da includere nelle ricerche
        /// </summary>
        public string[] DefaultFields { get; set; } = new[]
        {
            "key", "summary", "status", "priority", "assignee", "issuetype",
            "created", "updated", "customfield_10117", "customfield_10113",
            "customfield_10114", "customfield_10172"
        };
    }
}