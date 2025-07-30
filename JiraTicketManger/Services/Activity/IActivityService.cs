using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Interface principale per tutti i servizi di gestione attività Jira.
    /// Centralizza l'accesso a Comments, History e Attachments.
    /// </summary>
    public interface IActivityService
    {
        Task<List<JiraComment>> GetCommentsAsync(string ticketKey, IProgress<string> progress = null);
        Task<List<JiraHistoryItem>> GetHistoryAsync(string ticketKey, IProgress<string> progress = null);
        Task<List<JiraAttachment>> GetAttachmentsAsync(string ticketKey, IProgress<string> progress = null);

        // Metodi di utilità
        Task<ActivitySummary> GetActivitySummaryAsync(string ticketKey, IProgress<string> progress = null);
        Task<bool> TestActivityEndpointsAsync(string ticketKey);
    }
}