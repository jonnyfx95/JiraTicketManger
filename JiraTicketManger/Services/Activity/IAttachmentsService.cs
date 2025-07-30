using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JiraTicketManager.Data.Models.Activity;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Interface specifica per la gestione degli allegati Jira.
    /// Tradotta dalla logica VB.NET LoadAttachmentsAsync().
    /// </summary>
    public interface IAttachmentsService
    {
        Task<List<JiraAttachment>> GetAttachmentsAsync(string ticketKey, IProgress<string> progress = null);
        Task<byte[]> DownloadAttachmentAsync(JiraAttachment attachment, IProgress<int> downloadProgress = null);
        Task<bool> SaveAttachmentAsync(JiraAttachment attachment, string filePath, IProgress<int> downloadProgress = null);
        Task<int> GetAttachmentsCountAsync(string ticketKey);
    }
}
