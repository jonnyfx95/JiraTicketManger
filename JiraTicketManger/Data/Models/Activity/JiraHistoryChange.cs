using JiraTicketManager.Data.Converters;
using Newtonsoft.Json.Linq;

namespace JiraTicketManager.Data.Models.Activity
{
    /// <summary>
    /// Modello per una singola modifica nella cronologia Jira.
    /// Rappresenta il cambio di un singolo campo.
    /// </summary>
    public class JiraHistoryChange
    {
        public string Field { get; set; }
        public string FieldType { get; set; }
        public string FieldId { get; set; }
        public string FromValue { get; set; }
        public string FromDisplayValue { get; set; }
        public string ToValue { get; set; }
        public string ToDisplayValue { get; set; }

        /// <summary>
        /// Nome del campo in formato leggibile
        /// </summary>
        public string FieldDisplayName => GetFieldDisplayName(Field);

        /// <summary>
        /// Descrizione del cambiamento in formato "Da → A"
        /// </summary>
        public string ChangeDescription
        {
            get
            {
                var from = FromDisplayValue ?? FromValue ?? "[Vuoto]";
                var to = ToDisplayValue ?? ToValue ?? "[Vuoto]";

                if (string.IsNullOrEmpty(from) && !string.IsNullOrEmpty(to))
                    return $"Impostato a: {to}";

                if (!string.IsNullOrEmpty(from) && string.IsNullOrEmpty(to))
                    return $"Rimosso: {from}";

                return $"{from} → {to}";
            }
        }

        /// <summary>
        /// Colore rappresentativo per il tipo di campo (per UI timeline)
        /// </summary>
        public string FieldColor
        {
            get
            {
                return Field?.ToLower() switch
                {
                    "status" => "#28a745",      // Verde per stati
                    "assignee" => "#007bff",    // Blu per assegnazioni
                    "priority" => "#dc3545",    // Rosso per priorità
                    "summary" => "#6c757d",     // Grigio per summary
                    "description" => "#6c757d", // Grigio per descrizione
                    "fixversion" => "#fd7e14",  // Arancione per versioni
                    "component" => "#6f42c1",   // Viola per componenti
                    _ => "#17a2b8"              // Ciano per altri
                };
            }
        }

        /// <summary>
        /// Crea un JiraHistoryChange da un JToken JSON.
        /// Utilizza le funzioni helper esistenti del progetto C#.
        /// </summary>
        public static JiraHistoryChange FromJToken(JToken changeToken)
        {
            return new JiraHistoryChange
            {
                Field = JiraDataConverter.GetSafeStringValue(changeToken["field"]),
                FieldType = JiraDataConverter.GetSafeStringValue(changeToken["fieldtype"]),
                FieldId = JiraDataConverter.GetSafeStringValue(changeToken["fieldId"]),
                FromValue = JiraDataConverter.GetSafeStringValue(changeToken["from"]),
                FromDisplayValue = JiraDataConverter.GetSafeStringValue(changeToken["fromString"]),
                ToValue = JiraDataConverter.GetSafeStringValue(changeToken["to"]),
                ToDisplayValue = JiraDataConverter.GetSafeStringValue(changeToken["toString"])
            };
        }

        private static string GetFieldDisplayName(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName)) return "Campo sconosciuto";

            return fieldName.ToLower() switch
            {
                "status" => "Stato",
                "assignee" => "Assegnatario",
                "priority" => "Priorità",
                "summary" => "Oggetto",
                "description" => "Descrizione",
                "fixversion" => "Fix Version",
                "component" => "Componente",
                "attachment" => "Allegato",
                "issuetype" => "Tipo Ticket",
                "resolution" => "Risoluzione",
                "reporter" => "Segnalatore",
                "labels" => "Etichette",
                "timeestimate" => "Stima Tempo",
                "timespent" => "Tempo Impiegato",
                _ => fieldName
            };
        }
    }
}