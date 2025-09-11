using System;
using JiraTicketManager.Data.Converters;
using JiraTicketManager.Services;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Modello per i commenti Jira.
    /// Tradotto e migliorato dalla logica VB.NET PopulateCommentsTab().
    /// </summary>
    public class JiraComment : ActivityItemBase
    {
        public string Body { get; set; }
        public string AuthorDisplayName { get; set; }
        public string AuthorEmailAddress { get; set; }
        public string AuthorAvatarUrl { get; set; }
        public bool IsInternal { get; set; }
        public string VisibilityType { get; set; }
        public string VisibilityValue { get; set; }
        public DateTime Updated { get; set; }

        /// <summary>
        /// Indica se il commento è stato modificato
        /// </summary>
        public bool IsEdited => Updated > Created;

        /// <summary>
        /// Testo pulito del commento (senza markup Jira)
        /// </summary>
        public string CleanBody => CleanJiraMarkup(Body);

        /// <summary>
        /// Emoji avatar basato sull'autore per UI chat-style
        /// </summary>
        public string AvatarEmoji => GetAvatarEmoji(AuthorDisplayName ?? Author);

        /// <summary>
        /// Indica se il commento è privato/interno
        /// </summary>
        public bool IsPrivate => IsInternal || !string.IsNullOrEmpty(VisibilityType);

        /// <summary>
        /// Descrizione della visibilità per UI
        /// </summary>
        public string VisibilityDescription
        {
            get
            {
                if (IsInternal) return "Commento interno";
                if (!string.IsNullOrEmpty(VisibilityType))
                    return $"Visibile a: {VisibilityValue ?? VisibilityType}";
                return "Pubblico";
            }
        }

        /// <summary>
        /// Crea un JiraComment da un JToken JSON.
        /// Utilizza le funzioni helper esistenti del progetto C#.
        /// </summary>
        public static JiraComment FromJToken(JToken commentToken)
        {
            try
            {
                var comment = new JiraComment();

                // ID e timestamp usando helper esistenti
                comment.Id = JiraDataConverter.GetSafeStringValue(commentToken["id"]);

                var createdString = JiraDataConverter.GetSafeStringValue(commentToken["created"]);
                comment.Created = DateTime.TryParse(createdString, out var created) ? created : DateTime.Now;

                var updatedString = JiraDataConverter.GetSafeStringValue(commentToken["updated"]);
                comment.Updated = DateTime.TryParse(updatedString, out var updated) ? updated : comment.Created;

                // Autore
                var authorToken = commentToken["author"];
                if (authorToken != null)
                {
                    comment.Author = JiraDataConverter.GetSafeStringValue(authorToken["name"]);
                    comment.AuthorDisplayName = JiraDataConverter.GetSafeStringValue(authorToken["displayName"]);
                    comment.AuthorEmailAddress = JiraDataConverter.GetSafeStringValue(authorToken["emailAddress"]);
                    comment.AuthorAvatarUrl = JiraDataConverter.GetSafeStringValue(authorToken["avatarUrls"]?["48x48"]);
                }

                // Corpo del commento
                comment.Body = JiraDataConverter.GetSafeStringValue(commentToken["body"]);

                // ✅ AGGIUNTO: Gestione jsdPublic per visibilità corretta
                var jsdPublicToken = commentToken["jsdPublic"];
                if (jsdPublicToken != null)
                {
                    // jsdPublic = true significa PUBBLICO, jsdPublic = false significa PRIVATO
                    var jsdPublicValue = jsdPublicToken.ToString().ToLower();
                    comment.IsInternal = !(jsdPublicValue == "true");
                }
                else
                {
                    // Fallback al campo visibility standard
                    var visibilityToken = commentToken["visibility"];
                    if (visibilityToken != null)
                    {
                        comment.VisibilityType = JiraDataConverter.GetSafeStringValue(visibilityToken["type"]);
                        comment.VisibilityValue = JiraDataConverter.GetSafeStringValue(visibilityToken["value"]);
                        comment.IsInternal = !string.IsNullOrEmpty(comment.VisibilityType);
                    }
                    else
                    {
                        comment.IsInternal = false; // Default: pubblico
                    }
                }

                return comment;
            }
            catch (Exception ex)
            {
                var logger = LoggingService.CreateForComponent("JiraComment");
                logger.LogError($"Errore parsing commento: {ex.Message}");
                throw;
            }
        }

        private static string CleanJiraMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Rimuove markup Jira comune
            return text
                .Replace("*", "")        // Bold
                .Replace("_", "")        // Italic  
                .Replace("{{", "")       // Monospace start
                .Replace("}}", "")       // Monospace end
                .Replace("{code}", "")   // Code block
                .Replace("{code:}", "")  // Code block end
                .Trim();
        }

        private static string GetAvatarEmoji(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "👤";

            // Genera emoji basato sulla prima lettera del nome
            var firstChar = char.ToUpper(displayName[0]);
            return firstChar switch
            {
                'A' => "🅰️",
                'B' => "🅱️",
                'C' => "🔵",
                'D' => "💎",
                'E' => "📧",
                'F' => "🔥",
                'G' => "🟢",
                'H' => "🏠",
                'I' => "ℹ️",
                'J' => "⚡",
                'K' => "🔑",
                'L' => "💡",
                'M' => "📱",
                'N' => "🆕",
                'O' => "⭕",
                'P' => "🟣",
                'Q' => "❓",
                'R' => "🔴",
                'S' => "⭐",
                'T' => "🔺",
                'U' => "🔆",
                'V' => "✅",
                'W' => "⚪",
                'X' => "❌",
                'Y' => "🟡",
                'Z' => "⚡",
                _ => "👤"
            };
        }
    }
}
