using System;
using System.Linq;

namespace JiraTicketManager.Data.Models
{
    /// <summary>
    /// Modello rappresentante una voce della rubrica telefonica Jira.
    /// Estratta dai ticket Jira tramite API.
    /// Path: Data/Models/PhoneBookEntry.cs
    /// </summary>
    public class PhoneBookEntry
    {
        #region Properties

        /// <summary>
        /// Cliente / Organizzazione (customfield_10117)
        /// </summary>
        public string Cliente { get; set; } = "";

        /// <summary>
        /// Applicativo (customfield_10114)
        /// </summary>
        public string Applicativo { get; set; } = "";

        /// <summary>
        /// Area (customfield_10113)
        /// </summary>
        public string Area { get; set; } = "";

        /// <summary>
        /// Nome contatto (reporter.displayName)
        /// </summary>
        public string Nome { get; set; } = "";

        /// <summary>
        /// Email contatto (reporter.emailAddress)
        /// </summary>
        public string Email { get; set; } = "";

        /// <summary>
        /// Telefono (customfield_10074)
        /// </summary>
        public string Telefono { get; set; } = "";

        #endregion

        #region Constructors

        /// <summary>
        /// Costruttore vuoto per deserializzazione
        /// </summary>
        public PhoneBookEntry()
        {
        }

        /// <summary>
        /// Costruttore con parametri per creazione rapida
        /// </summary>
        public PhoneBookEntry(string cliente, string applicativo, string area, string nome, string email, string telefono)
        {
            Cliente = cliente ?? "";
            Applicativo = applicativo ?? "";
            Area = area ?? "";
            Nome = nome ?? "";
            Email = email ?? "";
            Telefono = telefono ?? "";
        }

        #endregion

        #region Deduplication

        /// <summary>
        /// Genera una chiave univoca per identificare duplicati.
        /// Due entries con stesso Nome + Telefono sono considerate duplicate.
        /// </summary>
        /// <returns>Chiave univoca per deduplicazione</returns>
        public string GetUniqueKey()
        {
            // Normalizza nome e telefono per deduplicazione
            var normalizedNome = NormalizeString(Nome);
            var normalizedTelefono = NormalizeTelefono(Telefono);

            return $"{normalizedNome}|{normalizedTelefono}";
        }

        /// <summary>
        /// Verifica se questa entry è valida (ha almeno nome o email)
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Nome) || !string.IsNullOrWhiteSpace(Email);
        }

        #endregion

        #region CSV Serialization

        /// <summary>
        /// Separatore CSV (semicolon per compatibilità Excel italiano)
        /// </summary>
        public const string CSV_SEPARATOR = ";";

        /// <summary>
        /// Header CSV per il file cache
        /// </summary>
        public static string GetCsvHeader()
        {
            return $"Cliente{CSV_SEPARATOR}Applicativo{CSV_SEPARATOR}Area{CSV_SEPARATOR}Nome{CSV_SEPARATOR}Email{CSV_SEPARATOR}Telefono";
        }

        /// <summary>
        /// Converte questa entry in una riga CSV
        /// </summary>
        public string ToCsvLine()
        {
            return string.Join(CSV_SEPARATOR,
                EscapeCsvValue(Cliente),
                EscapeCsvValue(Applicativo),
                EscapeCsvValue(Area),
                EscapeCsvValue(Nome),
                EscapeCsvValue(Email),
                EscapeCsvValue(Telefono)
            );
        }

        /// <summary>
        /// Crea una PhoneBookEntry da una riga CSV
        /// </summary>
        /// <param name="csvLine">Riga CSV</param>
        /// <returns>PhoneBookEntry o null se parsing fallisce</returns>
        public static PhoneBookEntry FromCsvLine(string csvLine)
        {
            if (string.IsNullOrWhiteSpace(csvLine))
                return null;

            var parts = csvLine.Split(new[] { CSV_SEPARATOR }, StringSplitOptions.None);

            if (parts.Length < 6)
                return null; // Formato non valido

            return new PhoneBookEntry
            {
                Cliente = UnescapeCsvValue(parts[0]),
                Applicativo = UnescapeCsvValue(parts[1]),
                Area = UnescapeCsvValue(parts[2]),
                Nome = UnescapeCsvValue(parts[3]),
                Email = UnescapeCsvValue(parts[4]),
                Telefono = UnescapeCsvValue(parts[5])
            };
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Normalizza una stringa per comparazione (lowercase, trim, no spazi multipli)
        /// </summary>
        private static string NormalizeString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            // Lowercase, trim, rimuovi spazi multipli
            return string.Join(" ", value.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Normalizza un numero di telefono per comparazione (solo cifre)
        /// </summary>
        private static string NormalizeTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
                return "";

            // Estrae solo le cifre
            return new string(telefono.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Escape di un valore CSV (gestisce virgolette e separatori)
        /// </summary>
        private static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Se contiene separatore, newline o virgolette, racchiudi in virgolette
            if (value.Contains(CSV_SEPARATOR) || value.Contains("\n") || value.Contains("\""))
            {
                // Escape virgolette doppie
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        /// <summary>
        /// Unescape di un valore CSV
        /// </summary>
        private static string UnescapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Rimuovi virgolette iniziali/finali se presenti
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
                // Unescape virgolette doppie
                value = value.Replace("\"\"", "\"");
            }

            return value;
        }

        #endregion

        #region Overrides

        /// <summary>
        /// ToString per debugging
        /// </summary>
        public override string ToString()
        {
            return $"{Nome} ({Email}) - {Cliente}";
        }

        /// <summary>
        /// Equals basato su GetUniqueKey() per deduplicazione
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is PhoneBookEntry other)
            {
                return GetUniqueKey() == other.GetUniqueKey();
            }
            return false;
        }

        /// <summary>
        /// GetHashCode basato su GetUniqueKey()
        /// </summary>
        public override int GetHashCode()
        {
            return GetUniqueKey().GetHashCode();
        }

        #endregion
    }
}