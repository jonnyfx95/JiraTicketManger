using JiraTicketManager.Business;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace JiraTicketManager.Data.Models
{
    /// <summary>
    /// Modello rappresentante un ticket Jira con i campi principali
    /// </summary>
    public class JiraTicket
    {
        public string Key { get; set; } = "";
        public string Summary { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string IssueType { get; set; } = "";
        public string Assignee { get; set; } = "";
        public string AssigneeDisplayName { get; set; } = "";
        public string Reporter { get; set; } = "";
        public string Organization { get; set; } = "";
        public string Area { get; set; } = "";
        public string Application { get; set; } = "";
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public DateTime? ResolutionDate { get; set; }

        /// <summary>
        /// Dati raw JSON per accesso a campi non mappati
        /// </summary>
        public JToken RawData { get; set; }

        /// <summary>
        /// Crea un JiraTicket dai dati JSON dell'API Jira
        /// </summary>
        public static JiraTicket FromJiraJson(JToken issueJson)
        {
            var fields = issueJson["fields"];
            if (fields == null) return new JiraTicket();

            return new JiraTicket
            {
                Key = issueJson["key"]?.ToString() ?? "",
                Summary = fields["summary"]?.ToString() ?? "",
                Description = ExtractDescription(fields["description"]),
                Status = fields["status"]?["name"]?.ToString() ?? "",
                Priority = fields["priority"]?["name"]?.ToString() ?? "",
                IssueType = fields["issuetype"]?["name"]?.ToString() ?? "",
                Assignee = fields["assignee"]?["name"]?.ToString() ?? "",
                AssigneeDisplayName = fields["assignee"]?["displayName"]?.ToString() ?? "Non assegnato",
                Reporter = fields["reporter"]?["displayName"]?.ToString() ?? "",
                Organization = ExtractCustomFieldValue(fields, "customfield_10117") ?? "",
                Area = ExtractCustomFieldValue(fields, "customfield_10113") ?? "",
                Application = ExtractCustomFieldValue(fields, "customfield_10114") ?? "",
                Created = ParseJiraDate(fields["created"]?.ToString()),
                Updated = ParseJiraDate(fields["updated"]?.ToString()),
                ResolutionDate = ParseJiraDateNullable(fields["resolutiondate"]?.ToString()),
                RawData = issueJson
            };
        }

        private static string ExtractDescription(JToken descriptionToken)
        {
            if (descriptionToken == null) return "";

            // Gestisce sia string che ADF (Atlassian Document Format)
            if (descriptionToken.Type == JTokenType.String)
                return descriptionToken.ToString();

            // Per ADF, estrae il testo plain
            return ExtractTextFromADF(descriptionToken);
        }

        private static string ExtractTextFromADF(JToken adfToken)
        {
            // Implementazione semplificata per estrarre testo da ADF
            var text = "";
            if (adfToken["content"] is JArray content)
            {
                foreach (var paragraph in content)
                {
                    if (paragraph["content"] is JArray paragraphContent)
                    {
                        foreach (var textNode in paragraphContent)
                        {
                            text += textNode["text"]?.ToString() ?? "";
                        }
                    }
                    text += "\n";
                }
            }
            return text.Trim();
        }

        private static string ExtractCustomFieldValue(JToken fields, string fieldId)
        {
            var fieldValue = fields[fieldId];
            if (fieldValue == null) return null;

            // Gestisce diversi formati di custom field
            if (fieldValue.Type == JTokenType.String)
                return fieldValue.ToString();

            if (fieldValue.Type == JTokenType.Object && fieldValue["value"] != null)
                return fieldValue["value"].ToString();

            if (fieldValue.Type == JTokenType.Array && fieldValue.HasValues)
                return fieldValue[0]?["value"]?.ToString() ?? fieldValue[0]?.ToString();

            return fieldValue.ToString();
        }

        private static DateTime ParseJiraDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return DateTime.MinValue;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return DateTime.MinValue;
        }

        private static DateTime? ParseJiraDateNullable(string dateString)
        {
            if (string.IsNullOrEmpty(dateString))
                return null;

            if (DateTime.TryParse(dateString, out var date))
                return date;

            return null;
        }
    }

    /// <summary>
    /// Modello per rappresentare un campo Jira generico
    /// </summary>
    public class JiraField
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string DisplayValue { get; set; } = "";
        public JiraFieldType FieldType { get; set; }

        /// <summary>
        /// Dati aggiuntivi del campo (es. colore, icona, etc.)
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        public static JiraField Create(string id, string name, string value, JiraFieldType fieldType)
        {
            return new JiraField
            {
                Id = id,
                Name = name,
                Value = value,
                DisplayValue = CleanDisplayValue(value),
                FieldType = fieldType
            };
        }

        private static string CleanDisplayValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            // Rimuove caratteri speciali comuni nei nomi Jira
            return value.Replace("_", " ")
                       .Replace(".", " ")
                       .Trim();
        }
    }

    /// <summary>
    /// Risultato di una ricerca Jira
    /// </summary>
    public class JiraSearchResult
    {
        public List<JiraTicket> Issues { get; set; } = new();
        public int StartAt { get; set; }
        public int MaxResults { get; set; }
        public int Total { get; set; }
        public bool IsLast => StartAt + MaxResults >= Total;
        public int CurrentPage => (StartAt / MaxResults) + 1;
        public int TotalPages => (int)Math.Ceiling((double)Total / MaxResults);

        /// <summary>
        /// Crea un JiraSearchResult dai dati JSON dell'API
        /// </summary>
        public static JiraSearchResult FromJiraJson(JToken searchJson)
        {
            var result = new JiraSearchResult
            {
                StartAt = searchJson["startAt"]?.Value<int>() ?? 0,
                MaxResults = searchJson["maxResults"]?.Value<int>() ?? 50,
                Total = searchJson["total"]?.Value<int>() ?? 0
            };

            if (searchJson["issues"] is JArray issues)
            {
                foreach (var issue in issues)
                {
                    result.Issues.Add(JiraTicket.FromJiraJson(issue));
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Criteri di ricerca per i ticket Jira
    /// </summary>
    public class JiraSearchCriteria
    {
        public string Organization { get; set; } = "";
        public string Status { get; set; } = "";
        public string Priority { get; set; } = "";
        public string IssueType { get; set; } = "";
        public string Area { get; set; } = "";
        public string Application { get; set; } = "";
        public string Assignee { get; set; } = "";
        public string Project { get; set; } = "CC"; // Default project
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? UpdatedFrom { get; set; }
        public DateTime? UpdatedTo { get; set; }
        public string FreeText { get; set; } = "";
        public string CustomJQL { get; set; } = "";
        public DateTime? CompletedFrom { get; set; }
        public DateTime? CompletedTo { get; set; }

        /// <summary>
        /// Verifica se ci sono filtri attivi
        /// </summary>
        public bool HasActiveFilters()
        {
             return !string.IsNullOrEmpty(Organization) ||
                    !string.IsNullOrEmpty(Status) ||
                    !string.IsNullOrEmpty(Priority) ||
                    !string.IsNullOrEmpty(IssueType) ||
                    !string.IsNullOrEmpty(Area) ||
                    !string.IsNullOrEmpty(Application) ||
                    !string.IsNullOrEmpty(Assignee) ||
                    CreatedFrom.HasValue ||
                    CreatedTo.HasValue ||
                    UpdatedFrom.HasValue ||
                    UpdatedTo.HasValue ||
                    CompletedFrom.HasValue ||        
                    CompletedTo.HasValue ||         
                    !string.IsNullOrEmpty(FreeText) ||
                    !string.IsNullOrEmpty(CustomJQL);
        }

        /// <summary>
        /// Reset di tutti i filtri
        /// </summary>
        public void Reset()
        {
            Organization = "";
            Status = "";
            Priority = "";
            IssueType = "";
            Area = "";
            Application = "";
            Assignee = "";
            CreatedFrom = null;
            CreatedTo = null;
            UpdatedFrom = null;
            UpdatedTo = null;
            CompletedFrom = null;     
            CompletedTo = null;      
            FreeText = "";
            CustomJQL = "";
        }

        /// <summary>
        /// Clona i criteri di ricerca
        /// </summary>
        public JiraSearchCriteria Clone()
        {
            return new JiraSearchCriteria
            {
                Organization = Organization,
                Status = Status,
                Priority = Priority,
                IssueType = IssueType,
                Area = Area,
                Application = Application,
                Assignee = Assignee,
                Project = Project,
                CreatedFrom = CreatedFrom,
                CreatedTo = CreatedTo,
                UpdatedFrom = UpdatedFrom,
                UpdatedTo = UpdatedTo,
                CompletedFrom = CompletedFrom,   
                CompletedTo = CompletedTo,       
                FreeText = FreeText,
                CustomJQL = CustomJQL
            };
        }
    }

    /// <summary>
    /// Configurazione di paginazione
    /// </summary>
    public class PaginationConfig
    {
        public int PageSize { get; set; } = 50;
        public int CurrentPage { get; set; } = 1;
        public int StartAt => (CurrentPage - 1) * PageSize;

        public void Reset()
        {
            CurrentPage = 1;
        }

        public void NextPage()
        {
            CurrentPage++;
        }

        public void PreviousPage()
        {
            if (CurrentPage > 1) CurrentPage--;
        }

        public void GoToPage(int page)
        {
            if (page > 0) CurrentPage = page;
        }
    }
}