using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using JiraTicketManager.Utilities;

namespace JiraTicketManager.Extensions
{
    /// <summary>
    /// Estensioni per JToken per semplificare l'estrazione sicura di valori da JSON Jira
    /// </summary>
    public static class JTokenExtensions
    {
        /// <summary>
        /// Estrae in modo sicuro un valore stringa da un JToken
        /// </summary>
        /// <param name="token">Token da cui estrarre il valore</param>
        /// <returns>Stringa pulita o stringa vuota se null</returns>
        public static string GetSafeStringValue(this JToken token)
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return "";

                var value = token.ToString();
                return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Estrae un valore nested con fallback multipli per gestire diversi formati Jira
        /// </summary>
        /// <param name="token">Token parent</param>
        /// <param name="fieldName">Nome campo primario</param>
        /// <param name="possibleSubFields">Array di nomi campo alternativi</param>
        /// <returns>Primo valore valido trovato o stringa vuota</returns>
        public static string GetSafeNestedValue(this JToken token, string fieldName, params string[] possibleSubFields)
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return "";

                // Prova il campo primario
                var primaryValue = token[fieldName].GetSafeStringValue();
                if (!string.IsNullOrEmpty(primaryValue))
                    return primaryValue;

                // Prova i fallback
                if (possibleSubFields != null)
                {
                    foreach (var fallback in possibleSubFields)
                    {
                        var fallbackValue = token[fallback].GetSafeStringValue();
                        if (!string.IsNullOrEmpty(fallbackValue))
                            return fallbackValue;
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Parse sicuro delle date Jira con supporto per formati ISO multipli
        /// </summary>
        /// <param name="dateToken">Token contenente la data</param>
        /// <returns>DateTime o DBNull.Value se parsing fallisce</returns>
        public static object ParseJiraDate(this JToken dateToken)
        {
            try
            {
                if (dateToken == null || dateToken.Type == JTokenType.Null)
                    return DBNull.Value;

                var dateString = dateToken.GetSafeStringValue();
                if (string.IsNullOrWhiteSpace(dateString))
                    return DBNull.Value;

                // Formati date Jira comuni
                var formats = new[]
                {
                    "yyyy-MM-ddTHH:mm:ss.fffzzz",    // ISO completo con timezone
                    "yyyy-MM-ddTHH:mm:ss.fff",       // ISO senza timezone
                    "yyyy-MM-ddTHH:mm:sszzz",        // ISO con timezone senza millisecondi
                    "yyyy-MM-ddTHH:mm:ss",           // ISO base
                    "yyyy-MM-dd HH:mm:ss",           // Formato standard
                    "yyyy-MM-dd"                     // Solo data
                };

                // Prova parsing con formati specifici
                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                // Fallback: parsing automatico
                if (DateTime.TryParse(dateString, out DateTime fallbackResult))
                    return fallbackResult;

                return DBNull.Value;
            }
            catch
            {
                return DBNull.Value;
            }
        }

        /// <summary>
        /// Estrae valore da custom field gestendo oggetti complessi
        /// </summary>
        /// <param name="token">Token del custom field</param>
        /// <returns>Valore estratto o stringa vuota</returns>
        public static string ExtractCustomFieldValue(this JToken token)
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return "";

                // Se è un array, prendi il primo elemento
                if (token.Type == JTokenType.Array)
                {
                    var firstItem = token.FirstOrDefault();
                    if (firstItem != null)
                        return firstItem.ExtractCustomFieldValue();
                    return "";
                }

                // Se è un oggetto complesso, usa il ComplexFieldResolver
                if (token.Type == JTokenType.Object)
                {
                    // Prova prima i campi standard
                    var standardValue = token.GetSafeNestedValue("value", "name", "displayName", "key", "emailAddress");
                    if (!string.IsNullOrEmpty(standardValue))
                        return standardValue;

                    // Se non trova nulla, usa il resolver avanzato
                    return ComplexFieldResolver.ResolveComplexField(token, "CustomField");
                }

                // Fallback: valore diretto
                return token.GetSafeStringValue();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Estrae l'email da un oggetto user Jira
        /// </summary>
        /// <param name="userToken">Token dell'utente</param>
        /// <returns>Email dell'utente o stringa vuota</returns>
        public static string ExtractUserEmail(this JToken userToken)
        {
            try
            {
                if (userToken == null || userToken.Type == JTokenType.Null)
                    return "";

                return userToken.GetSafeNestedValue("emailAddress", "name", "displayName");
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Estrae il display name da un oggetto user Jira
        /// </summary>
        /// <param name="userToken">Token dell'utente</param>
        /// <returns>Nome visualizzato dell'utente o stringa vuota</returns>
        public static string ExtractUserDisplayName(this JToken userToken)
        {
            try
            {
                if (userToken == null || userToken.Type == JTokenType.Null)
                    return "";

                return userToken.GetSafeNestedValue("displayName", "name", "emailAddress");
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Verifica se un token rappresenta un valore vuoto
        /// </summary>
        /// <param name="token">Token da verificare</param>
        /// <returns>True se il token è vuoto o null</returns>
        public static bool IsEmpty(this JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
                return true;

            var value = token.ToString();
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Converte un JToken in un tipo specifico con fallback sicuro
        /// </summary>
        /// <typeparam name="T">Tipo di destinazione</typeparam>
        /// <param name="token">Token da convertire</param>
        /// <param name="defaultValue">Valore di default se conversione fallisce</param>
        /// <returns>Valore convertito o default</returns>
        public static T SafeConvert<T>(this JToken token, T defaultValue = default(T))
        {
            try
            {
                if (token == null || token.Type == JTokenType.Null)
                    return defaultValue;

                return token.ToObject<T>() ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}