using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Interface specifica per la gestione della cronologia Jira.
    /// Tradotta dalla logica VB.NET LoadHistoryAsync().
    /// </summary>
    public interface IHistoryService
    {
        Task<List<JiraHistoryItem>> GetHistoryAsync(string ticketKey, IProgress<string> progress = null);
        Task<List<JiraHistoryItem>> GetHistoryWithChangelogAsync(string ticketKey, IProgress<string> progress = null);
        Task<int> GetHistoryCountAsync(string ticketKey);
    }
}