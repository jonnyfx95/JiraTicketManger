using System;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Classe base per tutti gli elementi di attività Jira.
    /// Fornisce proprietà comuni e pattern di base.
    /// </summary>
    public abstract class ActivityItemBase
    {
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public string Author { get; set; }

        /// <summary>
        /// Data formattata nel formato italiano
        /// </summary>
        public string FormattedCreated => Created.ToString("dd/MM/yyyy HH:mm");

        /// <summary>
        /// Età relativa dell'elemento (es. "2 ore fa")
        /// </summary>
        public string RelativeAge
        {
            get
            {
                var timeSpan = DateTime.Now - Created;

                if (timeSpan.TotalMinutes < 1)
                    return "Appena ora";
                else if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minuti fa";
                else if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} ore fa";
                else if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} giorni fa";
                else
                    return FormattedCreated;
            }
        }
    }
}