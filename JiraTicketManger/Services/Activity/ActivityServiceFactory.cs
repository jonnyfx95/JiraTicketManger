using JiraTicketManager.Services;

namespace JiraTicketManager.Services.Activity
{
    /// <summary>
    /// Factory per la creazione dei servizi di attività.
    /// Segue il pattern Factory esistente nel progetto.
    /// </summary>
    public static class ActivityServiceFactory
    {
        /// <summary>
        /// Crea un ActivityService dalle impostazioni esistenti
        /// </summary>
        public static IActivityService Create(JiraApiService jiraApiService)
        {
            return new JiraActivityService(jiraApiService);
        }

        /// <summary>
        /// Crea un ActivityService dalle impostazioni di default
        /// </summary>
        public static IActivityService CreateFromSettings()
        {
            var settingsService = SettingsService.CreateDefault();
            var jiraApiService = JiraApiService.CreateFromSettings(settingsService);
            return new JiraActivityService(jiraApiService);
        }

        /// <summary>
        /// Crea servizi separati per uso specializzato
        /// </summary>
        public static (ICommentsService comments, IHistoryService history, IAttachmentsService attachments)
            CreateSpecializedServices(JiraApiService jiraApiService)
        {
            return (
                new JiraCommentsService(jiraApiService),
                new JiraHistoryService(jiraApiService),
                new JiraAttachmentsService(jiraApiService)
            );
        }
    }
}