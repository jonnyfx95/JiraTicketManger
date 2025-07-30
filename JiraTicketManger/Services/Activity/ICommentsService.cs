using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Interface specifica per la gestione dei commenti Jira.
    /// Tradotta dalla logica VB.NET LoadCommentsAsync().
    /// </summary>
    public interface ICommentsService
    {
        Task<List<JiraComment>> GetCommentsAsync(string ticketKey, IProgress<string> progress = null);
        Task<List<JiraComment>> GetCommentsWithVisibilityAsync(string ticketKey, IProgress<string> progress = null);
        Task<int> GetCommentsCountAsync(string ticketKey);
    }
}