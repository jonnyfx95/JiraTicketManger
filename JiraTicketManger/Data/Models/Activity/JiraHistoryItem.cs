using System;
using System.Collections.Generic;
using System.Linq;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Services;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Modello per gli elementi di cronologia Jira.
    /// Tradotto dalla logica VB.NET PopulateHistoryTab().
    /// </summary>
    public class JiraHistoryItem : ActivityItemBase
    {
        public List<JiraHistoryChange> Changes { get; set; } = new List<JiraHistoryChange>();
        public string AuthorDisplayName { get; set; }
        public string AuthorEmailAddress { get; set; }

        /// <summary>
        /// Numero totale di campi modificati in questo evento
        /// </summary>
        public int ChangeCount => Changes?.Count ?? 0;

        /// <summary>
        /// Descrizione sintetica delle modifiche per UI
        /// </summary>
        public string ChangesSummary
        {
            get
            {
                if (!Changes.Any()) return "Nessuna modifica";

                if (Changes.Count == 1)
                    return Changes[0].FieldDisplayName;

                return $"{Changes.Count} campi modificati";
            }
        }

        /// <summary>
        /// Icona rappresentativa del tipo di modifica principale
        /// </summary>
        public string ChangeIcon
        {
            get
            {
                if (!Changes.Any()) return "📝";

                var mainChange = Changes.First();
                return mainChange.Field.ToLower() switch
                {
                    "status" => "🔄",
                    "assignee" => "👤",
                    "priority" => "⚡",
                    "summary" => "📝",
                    "description" => "📄",
                    "fixversion" => "🏷️",
                    "component" => "🧩",
                    "attachment" => "📎",
                    _ => "✏️"
                };
            }
        }

        /// <summary>
        /// Crea un JiraHistoryItem da un JToken JSON.
        /// Utilizza le funzioni helper esistenti del progetto C#.
        /// </summary>
        public static JiraHistoryItem FromJToken(JToken historyToken)
        {
            try
            {
                var historyItem = new JiraHistoryItem();

                // ID e timestamp usando helper esistenti
                historyItem.Id = JiraDataConverter.GetSafeStringValue(historyToken["id"]);

                var createdString = JiraDataConverter.GetSafeStringValue(historyToken["created"]);
                historyItem.Created = DateTime.TryParse(createdString, out var created) ? created : DateTime.Now;

                // Autore usando helper esistenti
                var authorToken = historyToken["author"];
                if (authorToken != null)
                {
                    historyItem.Author = JiraDataConverter.GetSafeStringValue(authorToken["name"]);
                    historyItem.AuthorDisplayName = JiraDataConverter.GetSafeStringValue(authorToken["displayName"]);
                    historyItem.AuthorEmailAddress = JiraDataConverter.GetSafeStringValue(authorToken["emailAddress"]);
                }

                // Modifiche usando helper esistenti
                var itemsToken = historyToken["items"];
                if (itemsToken != null)
                {
                    foreach (var itemToken in itemsToken)
                    {
                        var change = JiraHistoryChange.FromJToken(itemToken);
                        historyItem.Changes.Add(change);
                    }
                }

                return historyItem;
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("JiraHistoryItem");
                logger.LogError($"Errore parsing history item: {ex.Message}");
                throw;
            }
        }
    }
}
