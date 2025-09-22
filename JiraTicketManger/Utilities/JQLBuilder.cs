using JiraTicketManager.Business;        // ← CORRETTO: "Manager"
using JiraTicketManager.Data.Models;     // ← CORRETTO: "Manager"
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace JiraTicketManager.Utilities    // ← CORRETTO: "Manager"
{
    /// <summary>
    /// Builder fluente per costruire query JQL in modo pulito e sicuro.
    /// Gestisce automaticamente l'escaping e la validazione dei parametri.
    /// </summary>
    public class JQLBuilder
    {
        private readonly List<string> _conditions = new();
        private readonly List<string> _orderBy = new();
        private string _project = "";

        private JQLBuilder() { }

        /// <summary>
        /// Crea un nuovo builder JQL
        /// </summary>
        public static JQLBuilder Create() => new();

        /// <summary>
        /// Crea un builder con il progetto di default
        /// </summary>
        public static JQLBuilder CreateDefault() => new JQLBuilder().Project("CC");

        #region Basic Conditions

        /// <summary>
        /// Imposta il progetto
        /// </summary>
        public JQLBuilder Project(string project)
        {
            if (!string.IsNullOrWhiteSpace(project))
            {
                _project = project;
            }
            return this;
        }

        /// <summary>
        /// Aggiunge una condizione personalizzata
        /// </summary>
        public JQLBuilder Where(string condition)
        {
            if (!string.IsNullOrWhiteSpace(condition))
            {
                _conditions.Add(condition);
            }
            return this;
        }

        /// <summary>
        /// Aggiunge una condizione con field e valore
        /// </summary>
        public JQLBuilder Where(string field, string value, JQLOperator op = JQLOperator.Equals)
        {
            if (!string.IsNullOrWhiteSpace(field) && !string.IsNullOrWhiteSpace(value))
            {
                var escapedValue = EscapeJQLValue(value);
                var condition = op switch
                {
                    JQLOperator.Equals => $"{field} = \"{escapedValue}\"",
                    JQLOperator.NotEquals => $"{field} != \"{escapedValue}\"",
                    JQLOperator.Contains => $"{field} ~ \"{escapedValue}\"",
                    JQLOperator.NotContains => $"{field} !~ \"{escapedValue}\"",
                    JQLOperator.In => $"{field} in (\"{escapedValue}\")",
                    JQLOperator.NotIn => $"{field} not in (\"{escapedValue}\")",
                    _ => $"{field} = \"{escapedValue}\""
                };
                _conditions.Add(condition);
            }
            return this;
        }

        #endregion

        #region Jira Fields

        /// <summary>
        /// Filtra per organizzazione/cliente
        /// </summary>
        public JQLBuilder Organization(string organization)
        {
            if (!string.IsNullOrWhiteSpace(organization))
            {
                return Where("\"cliente[dropdown]\"", organization);
            }
            return this;
        }

        /// <summary>
        /// Filtra per stato
        /// </summary>

        public JQLBuilder Status(string status)
        {
            if (!string.IsNullOrWhiteSpace(status))
            {
                // Mappa valori italiani ComboBox → valori inglesi Jira
                var mappedStatus = status switch
                {
                    "Completato" => "Complete",
                    "Da completare" => "New",
                    "In corso" => "In Progress", 
                    _ => status // Fallback al valore originale
                };

                return Where("statuscategory", mappedStatus);
            }
            return this;
        }

        /// <summary>
        /// Filtra per priorità
        /// </summary>
        public JQLBuilder Priority(string priority)
        {
            if (!string.IsNullOrWhiteSpace(priority))
            {
                return Where("priority", priority);
            }
            return this;
        }

        /// <summary>
        /// Filtra per tipo di ticket
        /// </summary>
        public JQLBuilder IssueType(string issueType)
        {
            if (!string.IsNullOrWhiteSpace(issueType))
            {
                return Where("type", issueType);
            }
            return this;
        }

        /// <summary>
        /// Filtra per area
        /// </summary>
        public JQLBuilder Area(string area)
        {
            if (!string.IsNullOrWhiteSpace(area))
            {
                return Where("\"Area\"", area);
            }
            return this;
        }

        /// <summary>
        /// Filtra per applicativo
        /// </summary>
        public JQLBuilder Application(string application)
        {
            if (!string.IsNullOrWhiteSpace(application))
            {
                return Where("\"Applicativo\"", application);
            }
            return this;
        }

        /// <summary>
        /// Filtra per assegnatario
        /// </summary>
        public JQLBuilder Assignee(string assignee)
        {
            if (!string.IsNullOrWhiteSpace(assignee))
            {
                return Where("assignee", assignee);
            }
            return this;
        }
        /// <summary>
        /// Filtra per reporter
        /// </summary>
        public JQLBuilder Reporter(string reporter)
        {
            if (!string.IsNullOrWhiteSpace(reporter))
            {
                return Where("reporter", reporter);
            }
            return this;
        }

        #endregion

        #region Date Filters

        /// <summary>
        /// Filtra per data di creazione da
        /// </summary>
        public JQLBuilder CreatedFrom(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"created >= \"{jqlDate}\"");
        }

        /// <summary>
        /// Filtra per data di creazione fino a
        /// </summary>
        public JQLBuilder CreatedTo(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"created <= \"{jqlDate}\"");
        }

        /// <summary>
        /// Filtra per range di date di creazione
        /// </summary>
        public JQLBuilder CreatedBetween(DateTime from, DateTime to)
        {
            return CreatedFrom(from).CreatedTo(to);
        }

        /// <summary>
        /// Filtra per data di aggiornamento da
        /// </summary>
        public JQLBuilder UpdatedFrom(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"updated >= \"{jqlDate}\"");
        }

        /// <summary>
        /// Filtra per data di aggiornamento fino a
        /// </summary>
        public JQLBuilder UpdatedTo(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"updated <= \"{jqlDate}\"");
        }

        /// <summary>
        /// Filtra per range di date di aggiornamento
        /// </summary>
        public JQLBuilder UpdatedBetween(DateTime from, DateTime to)
        {
            return UpdatedFrom(from).UpdatedTo(to);
        }

        /// <summary>
        /// Filtra per data relativa (es. "-30d", "-1w", "-6M")
        /// </summary>
        public JQLBuilder CreatedSince(string relativeDate)
        {
            if (!string.IsNullOrWhiteSpace(relativeDate))
            {
                return Where($"created >= {relativeDate}");
            }
            return this;
        }

        /// <summary>
        /// Filtra per data di aggiornamento relativa (es. "-30d", "-1w", "-6M")
        /// </summary>
        public JQLBuilder UpdatedSince(string relativeDate)
        {
            if (!string.IsNullOrWhiteSpace(relativeDate))
            {
                return Where($"updated >= {relativeDate}");
            }
            return this;
        }

        /// <summary>
        /// Filtra per data di completamento da
        /// </summary>
        public JQLBuilder CompletedFrom(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"resolved >= \"{jqlDate}\"");
        }

        /// <summary>
        /// Filtra per data di completamento fino a
        /// </summary>
        public JQLBuilder CompletedTo(DateTime date)
        {
            var jqlDate = FormatJQLDate(date);
            return Where($"resolved <= \"{jqlDate}\"");
        }
        /// <summary>
        /// Filtra per range di date di completamento
        /// </summary>
        public JQLBuilder CompletedBetween(DateTime from, DateTime to)
        {
            return CompletedFrom(from).CompletedTo(to);
        }

        #endregion

        #region Text Search

        /// <summary>
        /// Ricerca testuale nel titolo (summary)
        /// </summary>
        public JQLBuilder SummaryContains(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                return Where("summary", text, JQLOperator.Contains);
            }
            return this;
        }

        /// <summary>
        /// Ricerca testuale nella descrizione
        /// </summary>
        public JQLBuilder DescriptionContains(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                return Where("description", text, JQLOperator.Contains);
            }
            return this;
        }

        /// <summary>
        /// Ricerca testuale generale (titolo o descrizione)
        /// </summary>
        public JQLBuilder TextContains(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                var escapedText = EscapeJQLValue(text);
                return Where($"(summary ~ \"{escapedText}\" OR description ~ \"{escapedText}\")");
            }
            return this;
        }

       
        /// <summary>
        /// Converte diversi formati di input in chiave ticket Jira valida
        /// Gestisce: numeri, CC-1234, cc-1234, link Atlassian completi
        /// </summary>
        public static string ParseTicketKey(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var cleanInput = input.Trim();

            try
            {
                // 1. LINK BROWSE: https://deda-next.atlassian.net/browse/CC-1234
                var browseMatch = Regex.Match(cleanInput, @"/browse/(CC-\d+)", RegexOptions.IgnoreCase);
                if (browseMatch.Success)
                {
                    return browseMatch.Groups[1].Value.ToUpper();
                }

                // 2. LINK FILTER: https://deda-next.atlassian.net/issues/?filter=1234
                var filterMatch = Regex.Match(cleanInput, @"[?&]filter=(\d+)", RegexOptions.IgnoreCase);
                if (filterMatch.Success)
                {
                    return $"CC-{filterMatch.Groups[1].Value}";
                }

                // 3. FORMATO CC-XXXX o cc-xxxx (con o senza trattino)
                var ccPatterns = new[]
                {
            @"^CC-(\d+)$",           // CC-1234
            @"^cc-(\d+)$",           // cc-1234  
            @"^CC(\d+)$",            // CC1234
            @"^cc(\d+)$"             // cc1234
        };

                foreach (var pattern in ccPatterns)
                {
                    var match = Regex.Match(cleanInput, pattern);
                    if (match.Success)
                    {
                        return $"CC-{match.Groups[1].Value}";
                    }
                }

                // 4. SOLO NUMERO: 1234 → CC-1234
                if (Regex.IsMatch(cleanInput, @"^\d+$"))
                {
                    return $"CC-{cleanInput}";
                }

                // 5. FORMATO NON RICONOSCIUTO
                return null;
            }
            catch (Exception)
            {
                // In caso di errore regex, fallback sicuro
                return null;
            }
        }

        #endregion

        #region Multiple Values

        /// <summary>
        /// Filtra per multipli stati
        /// </summary>
        public JQLBuilder StatusIn(params string[] statuses)
        {
            if (statuses?.Length > 0)
            {
                var values = string.Join(", ", statuses.Select(s => $"\"{EscapeJQLValue(s)}\""));
                return Where($"status in ({values})");
            }
            return this;
        }

        /// <summary>
        /// Filtra per multiple priorità
        /// </summary>
        public JQLBuilder PriorityIn(params string[] priorities)
        {
            if (priorities?.Length > 0)
            {
                var values = string.Join(", ", priorities.Select(p => $"\"{EscapeJQLValue(p)}\""));
                return Where($"priority in ({values})");
            }
            return this;
        }

        /// <summary>
        /// Filtra per multipli tipi
        /// </summary>
        public JQLBuilder IssueTypeIn(params string[] types)
        {
            if (types?.Length > 0)
            {
                var values = string.Join(", ", types.Select(t => $"\"{EscapeJQLValue(t)}\""));
                return Where($"type in ({values})");
            }
            return this;
        }

        #endregion

        #region Special Conditions

        /// <summary>
        /// Filtra solo ticket assegnati
        /// </summary>
        public JQLBuilder OnlyAssigned()
        {
            return Where("assignee is not EMPTY");
        }

        /// <summary>
        /// Filtra solo ticket non assegnati
        /// </summary>
        public JQLBuilder OnlyUnassigned()
        {
            return Where("assignee is EMPTY");
        }

        /// <summary>
        /// Filtra per ticket chiusi/risolti
        /// </summary>
        public JQLBuilder OnlyClosed()
        {
            return Where("status in (Closed, Resolved, Done)");
        }

        /// <summary>
        /// Filtra per ticket aperti
        /// </summary>
        public JQLBuilder OnlyOpen()
        {
            return Where("status not in (Closed, Resolved, Done)");
        }

        #endregion

        #region Ordering

        /// <summary>
        /// Ordina per campo specifico
        /// </summary>
        public JQLBuilder OrderBy(string field, SortDirection direction = SortDirection.Ascending)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                var dir = direction == SortDirection.Descending ? "DESC" : "ASC";
                _orderBy.Add($"{field} {dir}");
            }
            return this;
        }

        /// <summary>
        /// Ordina per data di aggiornamento (più recenti prima)
        /// </summary>
        public JQLBuilder OrderByUpdatedDesc()
        {
            return OrderBy("updated", SortDirection.Descending);
        }

        /// <summary>
        /// Ordina per data di creazione (più recenti prima)
        /// </summary>
        public JQLBuilder OrderByCreatedDesc()
        {
            return OrderBy("created", SortDirection.Descending);
        }

        /// <summary>
        /// Ordina per priorità
        /// </summary>
        public JQLBuilder OrderByPriority()
        {
            return OrderBy("priority", SortDirection.Descending);
        }

        #endregion

        #region From Criteria

        /// <summary>
        /// Costruisce JQL da criteri di ricerca strutturati
        /// </summary>
        public static JQLBuilder FromCriteria(JiraSearchCriteria criteria)
        {
            var builder = Create();

            if (!string.IsNullOrWhiteSpace(criteria.Project))
                builder.Project(criteria.Project);

            if (!string.IsNullOrWhiteSpace(criteria.Organization))
                builder.Organization(criteria.Organization);

            if (!string.IsNullOrWhiteSpace(criteria.Status))
                builder.Status(criteria.Status);

            if (!string.IsNullOrWhiteSpace(criteria.Priority))
                builder.Priority(criteria.Priority);

            if (!string.IsNullOrWhiteSpace(criteria.IssueType))
                builder.IssueType(criteria.IssueType);

            if (!string.IsNullOrWhiteSpace(criteria.Area))
                builder.Area(criteria.Area);

            if (!string.IsNullOrWhiteSpace(criteria.Application))
                builder.Application(criteria.Application);

            if (!string.IsNullOrWhiteSpace(criteria.Assignee))
                builder.Assignee(criteria.Assignee);

            // Date creazione (ESISTENTI)
            if (criteria.CreatedFrom.HasValue)
                builder.CreatedFrom(criteria.CreatedFrom.Value);

            if (criteria.CreatedTo.HasValue)
                builder.CreatedTo(criteria.CreatedTo.Value);

            // Date aggiornamento (ESISTENTI)
            if (criteria.UpdatedFrom.HasValue)
                builder.UpdatedFrom(criteria.UpdatedFrom.Value);

            if (criteria.UpdatedTo.HasValue)
                builder.UpdatedTo(criteria.UpdatedTo.Value);

            // *** NUOVO: Date completamento ***
            if (criteria.CompletedFrom.HasValue)
                builder.CompletedFrom(criteria.CompletedFrom.Value);

            if (criteria.CompletedTo.HasValue)
                builder.CompletedTo(criteria.CompletedTo.Value);

            // Ricerca testuale
            if (!string.IsNullOrWhiteSpace(criteria.FreeText))
                builder.TextContains(criteria.FreeText);

            // JQL personalizzato
            if (!string.IsNullOrWhiteSpace(criteria.CustomJQL))
                builder.Where(criteria.CustomJQL);

            // Ordinamento di default
            return builder.OrderByUpdatedDesc();
        }

        #endregion

        #region Build

        /// <summary>
        /// Costruisce la query JQL finale
        /// </summary>
        public string Build()
        {
            var jql = new StringBuilder();

            // Progetto (sempre per primo se specificato)
            if (!string.IsNullOrWhiteSpace(_project))
            {
                jql.Append($"project = {_project}");
            }

            // Condizioni
            if (_conditions.Count > 0)
            {
                if (jql.Length > 0) jql.Append(" AND ");
                jql.Append(string.Join(" AND ", _conditions));
            }

            // Ordinamento
            if (_orderBy.Count > 0)
            {
                jql.Append(" ORDER BY ");
                jql.Append(string.Join(", ", _orderBy));
            }

            var result = jql.ToString();

            // Log per debug
            if (!string.IsNullOrWhiteSpace(result))
            {
                System.Diagnostics.Debug.WriteLine($"JQL Generated: {result}");
            }

            return result;
        }

        /// <summary>
        /// Costruisce e ritorna la query come stringa (conversione implicita)
        /// </summary>
        public static implicit operator string(JQLBuilder builder) => builder.Build();

        #endregion

        #region Private Helpers

        /// <summary>
        /// Escape dei valori JQL per evitare injection
        /// </summary>
        private static string EscapeJQLValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            return value.Replace("\"", "\\\"")
                       .Replace("\\", "\\\\")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }

        /// <summary>
        /// Formatta una data per JQL
        /// </summary>
        private static string FormatJQLDate(DateTime date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Formatta una data/ora per JQL
        /// </summary>
        private static string FormatJQLDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        #endregion

        #region Enums

        /// <summary>
        /// Operatori JQL supportati
        /// </summary>
        public enum JQLOperator
        {
            Equals,
            NotEquals,
            Contains,
            NotContains,
            In,
            NotIn,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual
        }

        /// <summary>
        /// Direzioni di ordinamento
        /// </summary>
        public enum SortDirection
        {
            Ascending,
            Descending
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Costruisce una JQL di base per il progetto CC
        /// </summary>
        public static string GetBaseJQL()
        {
            return CreateDefault()
                .CreatedSince("-180d")
                .OrderByUpdatedDesc()
                .Build();
        }

        /// <summary>
        /// Costruisce una JQL per ticket recenti
        /// </summary>
        public static string GetRecentTicketsJQL(int days = 30)
        {
            return CreateDefault()
                .CreatedSince($"-{days}d")
                .OrderByCreatedDesc()
                .Build();
        }

        /// <summary>
        /// Costruisce una JQL per ticket aperti
        /// </summary>
        public static string GetOpenTicketsJQL()
        {
            return CreateDefault()
                .OnlyOpen()
                .OrderByPriority()
                .Build();
        }

        /// <summary>
        /// Costruisce una JQL per ticket non assegnati
        /// </summary>
        public static string GetUnassignedTicketsJQL()
        {
            return CreateDefault()
                .OnlyUnassigned()
                .OnlyOpen()
                .OrderByCreatedDesc()
                .Build();
        }

        /// <summary>
        /// Valida una stringa JQL di base
        /// </summary>
        public static bool IsValidJQL(string jql)
        {
            if (string.IsNullOrWhiteSpace(jql)) return false;

            // Controlli di base per JQL valida
            var lowerJql = jql.ToLower();

            // Deve contenere almeno un campo riconosciuto
            var validFields = new[] { "project", "status", "priority", "type", "assignee", "created", "updated" };
            if (!validFields.Any(field => lowerJql.Contains(field))) return false;

            // Non deve contenere caratteri pericolosi (basic check)
            var dangerousChars = new[] { ";", "--", "/*", "*/" };
            if (dangerousChars.Any(dangerous => jql.Contains(dangerous))) return false;

            return true;
        }

        /// <summary>
        /// Pulisce e normalizza una JQL
        /// </summary>
        public static string CleanJQL(string jql)
        {
            if (string.IsNullOrWhiteSpace(jql)) return "";

            return jql.Trim()
                     .Replace("  ", " ")  // Spazi doppi
                     .Replace(" AND  ", " AND ")
                     .Replace(" OR  ", " OR ")
                     .Replace("( ", "(")
                     .Replace(" )", ")");
        }

        #endregion
    }

    /// <summary>
    /// Extension methods per JQLBuilder
    /// </summary>
    public static class JQLBuilderExtensions
    {
        /// <summary>
        /// Aggiunge condizioni multiple con OR
        /// </summary>
        public static JQLBuilder OrWhere(this JQLBuilder builder, params string[] conditions)
        {
            if (conditions?.Length > 0)
            {
                var orCondition = $"({string.Join(" OR ", conditions)})";
                builder.Where(orCondition);
            }
            return builder;
        }

        /// <summary>
        /// Aggiunge una condizione solo se il valore non è vuoto
        /// </summary>
        public static JQLBuilder WhereIfNotEmpty(this JQLBuilder builder, string field, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                builder.Where(field, value);
            }
            return builder;
        }

        /// <summary>
        /// Aggiunge filtri da un dizionario
        /// </summary>
        public static JQLBuilder WhereFromDictionary(this JQLBuilder builder, Dictionary<string, string> filters)
        {
            if (filters != null)
            {
                foreach (var kvp in filters.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
                {
                    builder.Where(kvp.Key, kvp.Value);
                }
            }
            return builder;
        }

        /// <summary>
        /// Applica filtri rapidi predefiniti
        /// </summary>
        public static JQLBuilder ApplyQuickFilter(this JQLBuilder builder, QuickFilterType filterType)
        {
            return filterType switch
            {
                QuickFilterType.MyOpenTickets => builder.Assignee("currentUser()").OnlyOpen(),
                QuickFilterType.RecentlyUpdated => builder.UpdatedSince("-7d"),
                QuickFilterType.HighPriority => builder.PriorityIn("Highest", "High"),
                QuickFilterType.Unassigned => builder.OnlyUnassigned().OnlyOpen(),
                QuickFilterType.Overdue => builder.Where("due < now() AND status not in (Closed, Resolved, Done)"),
                _ => builder
            };
        }
    }

    /// <summary>
    /// Filtri rapidi predefiniti
    /// </summary>
    public enum QuickFilterType
    {
        MyOpenTickets,
        RecentlyUpdated,
        HighPriority,
        Unassigned,
        Overdue
    }
}