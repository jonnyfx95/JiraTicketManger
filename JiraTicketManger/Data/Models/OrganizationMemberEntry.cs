using System;
using System.Linq;

namespace JiraTicketManager.Data.Models
{
    /// <summary>
    /// Modello rappresentante un membro di un'organizzazione Jira.
    /// Estratto tramite query JQL: reporter in organizationMembers(ORGANIZZAZIONE)
    /// Path: Data/Models/OrganizationMemberEntry.cs
    /// </summary>
    public class OrganizationMemberEntry
    {
        #region Properties

        /// <summary>
        /// Nome organizzazione
        /// </summary>
        public string Organizzazione { get; set; } = "";

        /// <summary>
        /// Nome completo utente (reporter.displayName)
        /// </summary>
        public string Nome { get; set; } = "";

        /// <summary>
        /// Email utente (reporter.emailAddress)
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Account ID Jira (per deduplicazione)
        /// </summary>
        public string AccountId { get; set; } = "";

        /// <summary>
        /// Numero di ticket creati dall'utente in questa organizzazione
        /// </summary>
        public int NumeroTicket { get; set; } = 0;

        #endregion

        #region Constructors

        public OrganizationMemberEntry()
        {
        }

        public OrganizationMemberEntry(string organizzazione, string nome, string email, string accountId, int numeroTicket = 0)
        {
            Organizzazione = organizzazione ?? "";
            Nome = nome ?? "";
            Email = email ?? "";
            AccountId = accountId ?? "";
            NumeroTicket = numeroTicket;
        }

        #endregion

        #region Deduplication

        /// <summary>
        /// Genera chiave univoca per identificare duplicati.
        /// Usa AccountId + Organizzazione come chiave univoca.
        /// </summary>
        public string GetUniqueKey()
        {
            return $"{AccountId}_{Organizzazione}".ToLowerInvariant();
        }

        #endregion

        #region Validation

        /// <summary>
        /// Verifica se l'entry è valida (ha almeno organizzazione e nome)
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Organizzazione) &&
                   !string.IsNullOrWhiteSpace(Nome);
        }

        #endregion

        #region CSV Serialization

        /// <summary>
        /// Converte l'entry in formato CSV
        /// </summary>
        public string ToCsvLine()
        {
            return $"{EscapeCsv(Organizzazione)},{EscapeCsv(Nome)},{EscapeCsv(Email)},{EscapeCsv(AccountId)},{NumeroTicket}";
        }

        /// <summary>
        /// Crea un'entry da una riga CSV
        /// </summary>
        public static OrganizationMemberEntry FromCsvLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                var parts = SplitCsvLine(line);

                if (parts.Length < 5)
                    return null;

                return new OrganizationMemberEntry
                {
                    Organizzazione = UnescapeCsv(parts[0]),
                    Nome = UnescapeCsv(parts[1]),
                    Email = UnescapeCsv(parts[2]),
                    AccountId = UnescapeCsv(parts[3]),
                    NumeroTicket = int.TryParse(parts[4], out int count) ? count : 0
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Header CSV per il file di cache
        /// </summary>
        public static string GetCsvHeader()
        {
            return "Organizzazione,Nome,Email,AccountId,NumeroTicket";
        }

        #endregion

        #region Private Helpers - CSV

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Se contiene virgola, virgolette o newline, racchiudi tra virgolette e raddoppia le virgolette interne
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        private static string UnescapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Rimuovi virgolette esterne se presenti
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
                value = value.Replace("\"\"", "\""); // Ripristina virgolette doppie
            }

            return value;
        }

        private static string[] SplitCsvLine(string line)
        {
            var result = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            var currentField = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Doppia virgoletta → virgoletta singola
                        currentField.Append('"');
                        i++; // Salta la seconda virgoletta
                    }
                    else
                    {
                        // Toggle stato quotes
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // Fine campo
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            // Aggiungi ultimo campo
            result.Add(currentField.ToString());

            return result.ToArray();
        }

        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{Organizzazione} - {Nome} ({Email}) - {NumeroTicket} ticket";
        }

        #endregion
    }
}