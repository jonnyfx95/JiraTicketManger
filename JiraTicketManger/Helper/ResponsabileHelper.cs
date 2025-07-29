using System;
using System.Collections.Generic;
using System.Linq;

namespace JiraTicketManager.Helpers
{
    /// <summary>
    /// Helper per determinare il responsabile basato sull'area del ticket.
    /// Tradotto da FrmDettaglio.vb DeterminaResponsabile()
    /// </summary>
    public static class ResponsabileHelper
    {
        /// <summary>
        /// Mappa Area → Responsabile
        /// </summary>
        private static readonly Dictionary<string, string> AreaResponsabileMappa = new()
        {
            { "Demografia", "ARMANDO DILUISE" },
            { "Risorse Economiche", "ANDREA CAROPPO" },
            { "Affari Generali", "ERWIN QUAGLIERINI" },
            { "Gestione Entrate", "ARMANDO DILUISE" },
            { "Servizi On-Line", "ROBERT BERTE'; CECILIA RAMPULLA" },
            { "Folium", "ANTONIO MAIETTA" },
            { "Contratti", "MARCO GALETTI" },
            { "GeoNext", "VERONICA LENZI" },
            { "Civilia Web", "ERWIN QUAGLIERINI" },
            { "Area Comune", "ANTONIO MAIELLO" },
            { "Risorse Umane", "ANDREA CAROPPO" }
        };

        /// <summary>
        /// Determina il responsabile basato sull'area del ticket.
        /// Tradotto dalla logica VB.NET DeterminaResponsabile()
        /// </summary>
        /// <param name="area">Area del ticket (es. "Demografia", "Risorse Economiche")</param>
        /// <returns>Nome del responsabile o stringa vuota se non trovato</returns>
        public static string DeterminaResponsabile(string area)
        {
            if (string.IsNullOrWhiteSpace(area))
                return "";

            // Cerca la prima corrispondenza dove l'area contiene la chiave
            foreach (var voce in AreaResponsabileMappa)
            {
                if (area.Contains(voce.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return voce.Value;
                }
            }

            return "";
        }

        /// <summary>
        /// Ottiene tutti i responsabili configurati
        /// </summary>
        /// <returns>Lista dei responsabili unici</returns>
        public static List<string> GetAllResponsabili()
        {
            return AreaResponsabileMappa.Values
                .SelectMany(r => r.Split(';', StringSplitOptions.RemoveEmptyEntries))
                .Select(r => r.Trim())
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }

        /// <summary>
        /// Ottiene tutte le aree configurate
        /// </summary>
        /// <returns>Lista delle aree</returns>
        public static List<string> GetAllAree()
        {
            return AreaResponsabileMappa.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Verifica se un'area ha un responsabile configurato
        /// </summary>
        /// <param name="area">Area da verificare</param>
        /// <returns>True se ha un responsabile</returns>
        public static bool HasResponsabile(string area)
        {
            return !string.IsNullOrWhiteSpace(DeterminaResponsabile(area));
        }
    }
}