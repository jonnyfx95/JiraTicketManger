using JiraTicketManager.Services;
using JiraTicketManager.Services.Activity;
using JiraTicketManager.UI.Manger.Activity;

namespace JiraTicketManager.UI.Managers.Activity
{
    /// <summary>
    /// Factory per la creazione di ActivityTabManager.
    /// Segue il pattern Factory esistente nel progetto.
    /// </summary>
    public static class ActivityTabManagerFactory
    {
        /// <summary>
        /// Crea un ActivityTabManager con servizio esistente
        /// </summary>
        public static IActivityTabManager Create(IActivityService activityService)
        {
            return new ActivityTabManager(activityService);
        }

        /// <summary>
        /// Crea un ActivityTabManager dalle impostazioni di default
        /// </summary>
        public static IActivityTabManager CreateFromSettings()
        {
            var settingsService = SettingsService.CreateDefault();
            var jiraApiService = JiraApiService.CreateFromSettings(settingsService);
            var activityService = ActivityServiceFactory.Create(jiraApiService);
            return new ActivityTabManager(activityService);
        }

        /// <summary>
        /// Crea un ActivityTabManager con JiraApiService esistente
        /// </summary>
        public static IActivityTabManager CreateFromApiService(JiraApiService jiraApiService)
        {
            var activityService = ActivityServiceFactory.Create(jiraApiService);
            return new ActivityTabManager(activityService);
        }
    }
}