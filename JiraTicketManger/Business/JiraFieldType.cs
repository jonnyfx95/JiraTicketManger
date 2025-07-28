namespace JiraTicketManager.Business
{
    /// <summary>
    /// Enumerazione dei tipi di campi Jira supportati.
    /// Mappatura diretta con i field ID dell'API Jira.
    /// </summary>
    public enum JiraFieldType
    {
        /// <summary>
        /// Organizzazioni/Clienti - API: /rest/servicedeskapi/organization
        /// </summary>
        Organization,

        /// <summary>
        /// Stati ticket - API: /rest/api/2/status
        /// </summary>
        Status,

        /// <summary>
        /// Priorità - API: /rest/api/2/priority
        /// </summary>
        Priority,

        /// <summary>
        /// Tipi di ticket - API: /rest/api/2/issuetype
        /// </summary>
        IssueType,

        /// <summary>
        /// Area (customfield_10113) - API: search con distinct values
        /// </summary>
        Area,

        /// <summary>
        /// Applicativo (customfield_10114) - API: search con distinct values
        /// </summary>
        Application,

        /// <summary>
        /// Assegnatari - API: /rest/api/2/user/assignable/search
        /// </summary>
        Assignee,

        /// <summary>
        /// Progetti - API: /rest/api/2/project
        /// </summary>
        Project,

        /// Consulente (customfield_10238) - API: search con distinct values
        /// </summary>
        Consulente,

        /// <summary>
        /// Custom field generico - richiede field ID
        /// </summary>
        CustomField


    }

    /// <summary>
    /// Classe di utilità per gestire le informazioni sui campi Jira.
    /// </summary>
    public static class JiraFieldTypeHelper
    {
        /// <summary>
        /// Mappatura field type -> API endpoint
        /// </summary>
        public static readonly Dictionary<JiraFieldType, string> ApiEndpoints = new()
        {
            { JiraFieldType.Organization, "/rest/servicedeskapi/organization" },
            { JiraFieldType.Status, "/rest/api/2/status" },
            { JiraFieldType.Priority, "/rest/api/2/priority" },
            { JiraFieldType.IssueType, "/rest/api/2/issuetype" },
            { JiraFieldType.Assignee, "/rest/api/2/user/assignable/search" },
            { JiraFieldType.Project, "/rest/api/2/project" }
        };

        /// <summary>
        /// Mappatura field type -> Custom Field ID
        /// </summary>
        public static readonly Dictionary<JiraFieldType, string> CustomFieldIds = new()
{
    { JiraFieldType.Area, "customfield_10113" },
    { JiraFieldType.Application, "customfield_10114" },
    { JiraFieldType.Consulente, "customfield_10238" }
};

        /// <summary>
        /// Mappatura field type -> Nome campo JQL
        /// </summary>
        public static readonly Dictionary<JiraFieldType, string> JqlFieldNames = new()
{
         { JiraFieldType.Organization, "\"Client/Organization\"" },
         { JiraFieldType.Status, "status" },
         { JiraFieldType.Priority, "priority" },
         { JiraFieldType.IssueType, "type" },
         { JiraFieldType.Area, "\"Area\"" },
         { JiraFieldType.Application, "\"Applicativo\"" },
         { JiraFieldType.Assignee, "assignee" },
         { JiraFieldType.Consulente, "\"Consulente\"" }, 
         { JiraFieldType.Project, "project" }
};

        /// <summary>
        /// Mappatura field type -> Nome visualizzazione
        /// </summary>
        public static readonly Dictionary<JiraFieldType, string> DisplayNames = new()
{
    { JiraFieldType.Organization, "Clienti" },
    { JiraFieldType.Status, "Stati" },
    { JiraFieldType.Priority, "Priorità" },
    { JiraFieldType.IssueType, "Tipi" },
    { JiraFieldType.Area, "Aree" },
    { JiraFieldType.Application, "Applicativi" },
    { JiraFieldType.Assignee, "Assegnatari" },
    { JiraFieldType.Consulente, "Consulenti" },
    { JiraFieldType.Project, "Progetti" }
};

        /// <summary>
        /// Ottiene l'endpoint API per un tipo di campo
        /// </summary>
        public static string GetApiEndpoint(JiraFieldType fieldType)
        {
            return fieldType switch
            {
                JiraFieldType.Organization => "/rest/servicedeskapi/organization",
                JiraFieldType.Status => "/rest/api/2/status",
                JiraFieldType.Priority => "/rest/api/2/priority",
                JiraFieldType.IssueType => "/rest/api/2/issuetype",
                JiraFieldType.Assignee => "/rest/api/2/user/assignable/search?project=CC",
                JiraFieldType.Project => "/rest/api/2/project",
                _ => ""
            };
        }

        /// <summary>
        /// Ottiene l'ID del custom field per un tipo di campo
        /// </summary>
        public static string GetCustomFieldId(JiraFieldType fieldType)
        {
            return CustomFieldIds.TryGetValue(fieldType, out var fieldId) ? fieldId : "";
        }

        /// <summary>
        /// Ottiene il nome del campo JQL per un tipo di campo
        /// </summary>
        public static string GetJqlFieldName(JiraFieldType fieldType)
        {
            return JqlFieldNames.TryGetValue(fieldType, out var fieldName) ? fieldName : fieldType.ToString().ToLower();
        }

        /// <summary>
        /// Ottiene il nome di visualizzazione per un tipo di campo
        /// </summary>
        public static string GetDisplayName(JiraFieldType fieldType)
        {
            return DisplayNames.TryGetValue(fieldType, out var displayName) ? displayName : fieldType.ToString();
        }

        /// <summary>
        /// Verifica se un campo è un custom field
        /// </summary>
        public static bool IsCustomField(JiraFieldType fieldType)
        {
            return fieldType == JiraFieldType.Area ||
                   fieldType == JiraFieldType.Application ||
                   fieldType == JiraFieldType.Consulente ||
                   fieldType == JiraFieldType.CustomField;
        }

        /// <summary>
        /// Verifica se un campo supporta la ricerca diretta via API
        /// </summary>
        public static bool HasDirectApiEndpoint(JiraFieldType fieldType)
        {
            return ApiEndpoints.ContainsKey(fieldType);
        }
    }
}