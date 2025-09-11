using JiraTicketManager.Data.Converters;
using JiraTicketManager.Extensions;
using JiraTicketManager.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace JiraTicketManager.Utilities
{
    /// <summary>
    /// Estrattore unificato per campi Jira con mappatura configurabile
    /// </summary>
    public static class JiraFieldExtractor
    {
        #region Field Configuration

        /// <summary>
        /// Configurazione per l'estrazione dei campi Jira
        /// </summary>
        private static readonly Dictionary<string, FieldConfig> FieldMappings = new()
        {
            // Campi base Jira
            ["Key"] = new FieldConfig { Path = "key", IsInFields = false, DefaultValue = "" },
            ["Summary"] = new FieldConfig { Path = "summary", DefaultValue = "" },
            ["Status"] = new FieldConfig { Path = "status", SubFields = ["name", "displayName"], DefaultValue = "Unknown" },
            ["Priority"] = new FieldConfig { Path = "priority", SubFields = ["name", "displayName"], DefaultValue = "Normal" },
            ["Assignee"] = new FieldConfig { Path = "assignee", SubFields = ["displayName", "name", "emailAddress"], DefaultValue = "Unassigned" },
            ["Type"] = new FieldConfig { Path = "issuetype", SubFields = ["name", "displayName"], DefaultValue = "Task" },
            ["Reporter"] = new FieldConfig { Path = "reporter", SubFields = ["displayName", "name", "emailAddress"], DefaultValue = "Unknown" },
            ["ConsulenteEmail"] = new FieldConfig { Path = "customfield_10238", IsCustomField = true, DefaultValue = "" },

            // Date
            ["Created"] = new FieldConfig { Path = "created", IsDate = true },
            ["Updated"] = new FieldConfig { Path = "updated", IsDate = true },
            ["ResolutionDate"] = new FieldConfig { Path = "resolutiondate", IsDate = true },

            // Custom fields Deda Group
            ["Cliente"] = new FieldConfig { Path = "customfield_10117", IsCustomField = true, DefaultValue = "" },
            ["Area"] = new FieldConfig { Path = "customfield_10113", IsCustomField = true, DefaultValue = "" },
            ["Applicativo"] = new FieldConfig { Path = "customfield_10114", IsCustomField = true, DefaultValue = "" },
            ["ClientePartner"] = new FieldConfig { Path = "customfield_10103", IsCustomField = true, DefaultValue = "" },
            ["Telefono"] = new FieldConfig { Path = "customfield_10074", IsCustomField = true, DefaultValue = "" },
            ["PMEmail"] = new FieldConfig { Path = "customfield_10271", IsCustomField = true, DefaultValue = "" },
            ["CommercialeEmail"] = new FieldConfig { Path = "customfield_10272", IsCustomField = true, DefaultValue = "" },
            ["WBS"] = new FieldConfig { Path = "customfield_10096", IsCustomField = true, DefaultValue = "" },

            // === 🆕 NUOVI CAMPI PIANIFICAZIONE ===
            ["DataIntervento"] = new FieldConfig { Path = "customfield_10116", IsCustomField = true, DefaultValue = "" },
            ["OraIntervento"] = new FieldConfig { Path = "customfield_10133", IsCustomField = true, DefaultValue = "" },
            ["Effort"] = new FieldConfig { Path = "customfield_10089", IsCustomField = true, DefaultValue = "" },

            // Altri campi
            ["Description"] = new FieldConfig { Path = "description", DefaultValue = "" }
        };

        /// <summary>
        /// Configurazione per un campo specifico
        /// </summary>
        private class FieldConfig
        {
            public string Path { get; set; }
            public string[] SubFields { get; set; } = new string[0];
            public string DefaultValue { get; set; } = "";
            public bool IsInFields { get; set; } = true;  // La maggior parte dei campi è in issue.fields
            public bool IsDate { get; set; } = false;
            public bool IsCustomField { get; set; } = false;
            public bool IsADF { get; set; } = false;  // Atlassian Document Format
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Estrae un campo generico da un'issue Jira
        /// </summary>
        /// <param name="issue">Issue completa Jira</param>
        /// <param name="fieldName">Nome del campo da estrarre (es. "Status", "Cliente")</param>
        /// <returns>Valore estratto o default</returns>
        public static object ExtractField(JToken issue, string fieldName)
        {
            try
            {
                if (!FieldMappings.TryGetValue(fieldName, out var config))
                {
                    throw new ArgumentException($"Campo '{fieldName}' non configurato");
                }

                // Determina il token di partenza
                var sourceToken = config.IsInFields ? issue?["fields"] : issue;
                var fieldToken = sourceToken?[config.Path];

                if (fieldToken == null || fieldToken.Type == JTokenType.Null)
                {
                    return config.IsDate ? DBNull.Value : config.DefaultValue;
                }

                // Gestione per tipo di campo
                if (config.IsDate)
                {
                    return fieldToken.ParseJiraDate();
                }

                if (config.IsCustomField)
                {
                    return fieldToken.ExtractCustomFieldValue();
                }

                // ✅ GESTIONE SPECIALE PER DESCRIZIONE E ADF
                if (config.IsADF || fieldName.Equals("description", StringComparison.OrdinalIgnoreCase))
                {
                    return ExtractTextFromADF(fieldToken);
                }

                if (config.SubFields.Length > 0)
                {
                    return fieldToken.GetSafeNestedValue(config.SubFields[0], config.SubFields[1..]);
                }

                // ✅ USA IL METODO AGGIORNATO CHE GESTISCE ADF
                return JiraDataConverter.GetSafeStringValue(fieldToken);
            }
            catch (Exception)
            {
                var config = FieldMappings.GetValueOrDefault(fieldName);
                return config?.IsDate == true ? DBNull.Value : config?.DefaultValue ?? "";
            }
        }

        /// <summary>
        /// Estrae un campo stringa da un'issue Jira
        /// </summary>
        /// <param name="issue">Issue completa Jira</param>
        /// <param name="fieldName">Nome del campo da estrarre</param>
        /// <returns>Valore stringa estratto</returns>
        public static string ExtractStringField(JToken issue, string fieldName)
        {
            var result = ExtractField(issue, fieldName);
            return result is string str ? str : result?.ToString() ?? "";
        }

        /// <summary>
        /// Estrae un campo data da un'issue Jira
        /// </summary>
        /// <param name="issue">Issue completa Jira</param>
        /// <param name="fieldName">Nome del campo data da estrarre</param>
        /// <returns>DateTime o DBNull.Value</returns>
        public static object ExtractDateField(JToken issue, string fieldName)
        {
            return ExtractField(issue, fieldName);
        }

        /// <summary>
        /// Estrae multipli campi in una sola chiamata
        /// </summary>
        /// <param name="issue">Issue completa Jira</param>
        /// <param name="fieldNames">Array di nomi campi da estrarre</param>
        /// <returns>Dizionario con campo -> valore</returns>
        public static Dictionary<string, object> ExtractMultipleFields(JToken issue, params string[] fieldNames)
        {
            var results = new Dictionary<string, object>();

            foreach (var fieldName in fieldNames)
            {
                results[fieldName] = ExtractField(issue, fieldName);
            }

            return results;
        }

        #endregion

        #region Legacy Methods (mantieni per compatibilità)

        /// <summary>
        /// Estrae il summary (titolo) del ticket
        /// </summary>
        public static string ExtractSummary(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Summary");

        /// <summary>
        /// Estrae la key del ticket
        /// </summary>
        public static string ExtractKey(JToken issue) => ExtractStringField(issue, "Key");

        /// <summary>
        /// Estrae lo status del ticket
        /// </summary>
        public static string ExtractStatus(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Status");

        /// <summary>
        /// Estrae la priorità del ticket
        /// </summary>
        public static string ExtractPriority(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Priority");

        /// <summary>
        /// Estrae l'assignee del ticket
        /// </summary>
        public static string ExtractAssignee(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Assignee");

        /// <summary>
        /// Estrae il tipo di issue
        /// </summary>
        public static string ExtractIssueType(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Type");

        /// <summary>
        /// Estrae il reporter del ticket
        /// </summary>
        public static string ExtractReporter(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Reporter");

        /// <summary>
        /// Estrae la descrizione
        /// </summary>
        public static string ExtractDescription(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Description");

        /// <summary>
        /// Estrae il campo Cliente
        /// </summary>
        public static string ExtractCliente(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Cliente");

        /// <summary>
        /// Estrae il campo Area
        /// </summary>
        public static string ExtractArea(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Area");

        /// <summary>
        /// Estrae il campo Applicativo
        /// </summary>
        public static string ExtractApplicativo(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "Applicativo");

        /// <summary>
        /// Estrae il campo Cliente Partner
        /// </summary>
        public static string ExtractClientePartner(JToken fields) => ExtractStringField(CreateIssueFromFields(fields), "ClientePartner");

        /// <summary>
        /// Estrae la data di creazione
        /// </summary>
        public static object ExtractCreated(JToken fields) => ExtractDateField(CreateIssueFromFields(fields), "Created");

        /// <summary>
        /// Estrae la data di ultimo aggiornamento
        /// </summary>
        public static object ExtractUpdated(JToken fields) => ExtractDateField(CreateIssueFromFields(fields), "Updated");

        /// <summary>
        /// Estrae la data di risoluzione
        /// </summary>
        public static object ExtractResolutionDate(JToken fields) => ExtractDateField(CreateIssueFromFields(fields), "ResolutionDate");

        #endregion

        #region Helper Methods

        /// <summary>
        /// Crea un oggetto issue fittizio dai fields per compatibilità
        /// </summary>
        private static JToken CreateIssueFromFields(JToken fields)
        {
            return new JObject
            {
                ["fields"] = fields
            };
        }

        /// <summary>
        /// Determina l'icona per il tipo di issue
        /// </summary>
        public static string DetermineTypeIcon(string issueType)
        {
            if (string.IsNullOrEmpty(issueType))
                return "📌";

            return issueType.ToLowerInvariant() switch
            {
                "bug" => "🐛",
                "task" => "📋",
                "story" => "📖",
                "epic" => "🎯",
                "improvement" => "⚡",
                "new feature" => "✨",
                "sub-task" => "📝",
                _ => "📌"
            };
        }

        /// <summary>
        /// Estrae testo da Atlassian Document Format (ADF)
        /// </summary>
        private static string ExtractTextFromADF(JToken adfNode)
        {
            try
            {
                if (adfNode == null) return "";

                var text = "";

                // Se ha contenuto testuale diretto
                if (adfNode["text"] != null)
                {
                    text += adfNode["text"].GetSafeStringValue();
                }

                // Elabora contenuto nested
                if (adfNode["content"] != null && adfNode["content"].Type == JTokenType.Array)
                {
                    foreach (var child in adfNode["content"])
                    {
                        text += ExtractTextFromADF(child);
                    }
                }

                return text;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Tronca descrizione per performance UI
        /// </summary>
        public static string TruncateDescription(string description, int maxLength = 200)
        {
            if (string.IsNullOrEmpty(description))
                return "";

            if (description.Length <= maxLength)
                return description;

            return description.Substring(0, maxLength) + "...";
        }

        /// <summary>
        /// Aggiunge o aggiorna la configurazione di un campo
        /// </summary>
        public static void ConfigureField(string fieldName, string jiraPath, string[] subFields = null, string defaultValue = "", bool isCustomField = false, bool isDate = false)
        {
            FieldMappings[fieldName] = new FieldConfig
            {
                Path = jiraPath,
                SubFields = subFields ?? new string[0],
                DefaultValue = defaultValue,
                IsCustomField = isCustomField,
                IsDate = isDate
            };
        }

        /// <summary>
        /// Inizializza il sistema con il logger per il ComplexFieldResolver
        /// </summary>
        public static void Initialize(LoggingService logger)
        {
            ComplexFieldResolver.Initialize(logger);
        }

        #endregion
    }
}