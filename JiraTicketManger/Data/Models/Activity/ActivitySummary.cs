using System;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Sommario delle attività di un ticket per aggiornamento rapido UI
    /// </summary>
    public class ActivitySummary
    {
        public string TicketKey { get; set; }
        public int CommentsCount { get; set; }
        public int HistoryCount { get; set; }
        public int AttachmentsCount { get; set; }
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Totale elementi di attività
        /// </summary>
        public int TotalActivityCount => CommentsCount + HistoryCount + AttachmentsCount;

        /// <summary>
        /// Indica se il ticket ha attività
        /// </summary>
        public bool HasActivity => TotalActivityCount > 0;

        /// <summary>
        /// Testo per aggiornamento tab titles
        /// </summary>
        public string GetTabTitle(string baseTitle, int count)
        {
            return count > 0 ? $"{baseTitle} ({count})" : baseTitle;
        }
    }
}