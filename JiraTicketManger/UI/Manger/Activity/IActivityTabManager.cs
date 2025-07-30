using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using JiraTicketManager.Data.Models.Activity;

namespace JiraTicketManager.UI.Manger.Activity
{
    /// <summary>
    /// Interface per la gestione dei tab di attività nel TicketDetailForm.
    /// Segue il pattern di IComboBoxManager esistente.
    /// </summary>
    public interface IActivityTabManager
    {
        /// <summary>
        /// Carica e popola tutti i tab di attività per un ticket
        /// </summary>
        Task LoadActivityTabsAsync(TabControl tabControl, string ticketKey, IProgress<string> progress = null);

        /// <summary>
        /// Aggiorna solo i conteggi nei titoli dei tab
        /// </summary>
        Task UpdateTabCountsAsync(TabControl tabControl, string ticketKey);

        /// <summary>
        /// Pulisce tutti i tab
        /// </summary>
        void ClearAllTabs(TabControl tabControl);

        /// <summary>
        /// Ottiene il sommario delle attività (per usage esterno)
        /// </summary>
        Task<ActivitySummary> GetActivitySummaryAsync(string ticketKey);
    }
}