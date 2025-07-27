using Newtonsoft.Json.Linq;
using JiraTicketManager.Extensions;
using JiraTicketManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JiraTicketManager.Utilities
{
    /// <summary>
    /// Risolutore avanzato per campi complessi Jira (oggetti, array, riferimenti)
    /// Porta dal VB.NET ComplexFieldResolver con miglioramenti
    /// </summary>
    public static class ComplexFieldResolver
    {
        private static LoggingService _logger;

        /// <summary>
        /// Inizializza il resolver con il logger
        /// </summary>
        public static void Initialize(LoggingService logger)
        {
            _logger = logger;
        }

        #region Public Methods

        /// <summary>
        /// Risolve un campo complesso con analisi ricorsiva
        /// </summary>
        /// <param name="fieldToken">Token del campo da risolvere</param>
        /// <param name="fieldName">Nome del campo per debug</param>
        /// <returns>Valore risolto o stringa vuota</returns>
        public static string ResolveComplexField(JToken fieldToken, string fieldName = "")
        {
            try
            {
                _logger?.LogDebug($"🔍 Risolvo campo complesso: {fieldName}");

                if (fieldToken == null || fieldToken.Type == JTokenType.Null)
                {
                    _logger?.LogDebug($"❌ Campo {fieldName} è null");
                    return "";
                }

                // Determina il tipo e usa il resolver appropriato
                return fieldToken.Type switch
                {
                    JTokenType.Object => ResolveObjectField(fieldToken as JObject, fieldName),
                    JTokenType.Array => ResolveArrayField(fieldToken as JArray, fieldName),
                    JTokenType.String => fieldToken.GetSafeStringValue(),
                    _ => fieldToken.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore risoluzione campo {fieldName}", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae organizzazione da customfield_10002 (array di organizzazioni)
        /// </summary>
        public static string ExtractOrganization(JToken orgField)
        {
            try
            {
                _logger?.LogDebug("🏢 Estrazione organizzazione da customfield_10002");

                if (orgField?.Type == JTokenType.Array)
                {
                    var orgArray = orgField as JArray;
                    if (orgArray?.Count > 0)
                    {
                        var firstOrg = orgArray[0];
                        if (firstOrg?.Type == JTokenType.Object)
                        {
                            var orgName = firstOrg["name"]?.GetSafeStringValue();
                            if (!string.IsNullOrEmpty(orgName))
                            {
                                _logger?.LogDebug($"🎉 Organizzazione trovata: {orgName}");
                                return orgName;
                            }
                        }
                    }
                }

                return ResolveComplexField(orgField, "Organization");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore estrazione organizzazione", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae persone da campi PM/Commerciale (customfield_10091, 10092, 10093)
        /// </summary>
        public static string ExtractPersonField(JToken personField, string personType = "")
        {
            try
            {
                _logger?.LogDebug($"👤 Estrazione persona: {personType}");

                if (personField?.Type == JTokenType.Object)
                {
                    var personObj = personField as JObject;

                    // Cerca campi comuni per persone
                    var personFields = new[] { "displayName", "name", "emailAddress", "key", "accountId" };

                    foreach (var field in personFields)
                    {
                        var value = personObj?[field]?.GetSafeStringValue();
                        if (!string.IsNullOrEmpty(value))
                        {
                            _logger?.LogDebug($"🎉 {personType} trovato via {field}: {value}");
                            return value;
                        }
                    }
                }

                return ResolveComplexField(personField, personType);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore estrazione persona {personType}", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae valori da campi AutoComplete complessi
        /// </summary>
        public static string ExtractAutoCompleteField(JToken autoField)
        {
            try
            {
                _logger?.LogDebug("🔄 Estrazione campo AutoComplete");

                if (autoField?.Type == JTokenType.Array)
                {
                    var autoArray = autoField as JArray;
                    if (autoArray?.Count > 0)
                    {
                        var firstItem = autoArray[0];
                        return ExtractTextFromAutoCompleteItem(firstItem);
                    }
                }
                else if (autoField?.Type == JTokenType.Object)
                {
                    return ExtractTextFromAutoCompleteItem(autoField);
                }

                return autoField?.GetSafeStringValue() ?? "";
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore estrazione AutoComplete", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae testo da oggetti CMDB (Configuration Management Database)
        /// </summary>
        public static string ExtractCMDBObject(JToken cmdbField)
        {
            try
            {
                _logger?.LogDebug("🔧 Estrazione oggetto CMDB");

                if (cmdbField?.Type == JTokenType.Object)
                {
                    var cmdbObj = cmdbField as JObject;

                    // Campi comuni negli oggetti CMDB
                    var cmdbFields = new[]
                    {
                        "label", "displayName", "name", "title", "summary",
                        "objectName", "assetName", "configurationItem",
                        "description", "shortName", "fullName"
                    };

                    foreach (var field in cmdbFields)
                    {
                        var value = cmdbObj?[field]?.GetSafeStringValue();
                        if (!string.IsNullOrEmpty(value) && value.Length > 2)
                        {
                            _logger?.LogDebug($"🎉 CMDB valore trovato in {field}: {value}");
                            return value;
                        }
                    }

                    // Cerca in attributi o properties
                    if (cmdbObj?["attributes"]?.Type == JTokenType.Object)
                    {
                        var attrResult = ExtractCMDBObject(cmdbObj["attributes"]);
                        if (!string.IsNullOrEmpty(attrResult))
                            return attrResult;
                    }

                    if (cmdbObj?["properties"]?.Type == JTokenType.Object)
                    {
                        var propResult = ExtractCMDBObject(cmdbObj["properties"]);
                        if (!string.IsNullOrEmpty(propResult))
                            return propResult;
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore estrazione CMDB", ex);
                return "";
            }
        }

        #endregion

        #region Private Methods - Object Resolution

        /// <summary>
        /// Risolve un campo oggetto complesso
        /// </summary>
        private static string ResolveObjectField(JObject obj, string fieldName)
        {
            try
            {
                _logger?.LogDebug($"📦 Risolvo oggetto: {fieldName}");

                // Strategia 1: Campi standard comuni
                var standardFields = new[] { "value", "name", "displayName", "key", "id", "summary", "title" };

                foreach (var field in standardFields)
                {
                    var value = obj?[field]?.GetSafeStringValue();
                    if (!string.IsNullOrEmpty(value))
                    {
                        _logger?.LogDebug($"✅ Valore trovato in {field}: {value}");
                        return value;
                    }
                }

                // Strategia 2: Riconoscimento pattern specifici
                if (obj?.ContainsKey("accountId") == true && obj?.ContainsKey("displayName") == true)
                {
                    // È un utente Jira
                    return ExtractPersonField(obj, "User");
                }

                if (obj?.ContainsKey("objectName") == true || obj?.ContainsKey("configurationItem") == true)
                {
                    // È un oggetto CMDB
                    return ExtractCMDBObject(obj);
                }

                if (obj?.ContainsKey("options") == true || obj?.ContainsKey("values") == true)
                {
                    // È un campo AutoComplete
                    return ExtractAutoCompleteField(obj);
                }

                // Strategia 3: Analisi ricorsiva limitata
                return AnalyzeObjectRecursively(obj, maxDepth: 2);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore risoluzione oggetto {fieldName}", ex);
                return "";
            }
        }

        /// <summary>
        /// Risolve un campo array
        /// </summary>
        private static string ResolveArrayField(JArray array, string fieldName)
        {
            try
            {
                _logger?.LogDebug($"📋 Risolvo array: {fieldName}, elementi: {array?.Count}");

                if (array?.Count == 0)
                    return "";

                // Prendi il primo elemento e risolvilo
                var firstElement = array[0];
                var resolvedValue = ResolveComplexField(firstElement, $"{fieldName}[0]");

                // Se abbiamo più elementi, aggiungi info
                if (array.Count > 1)
                {
                    _logger?.LogDebug($"📋 Array {fieldName} ha {array.Count} elementi, uso solo il primo");
                }

                return resolvedValue;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Errore risoluzione array {fieldName}", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae testo da un elemento AutoComplete
        /// </summary>
        private static string ExtractTextFromAutoCompleteItem(JToken item)
        {
            if (item?.Type == JTokenType.Object)
            {
                var itemObj = item as JObject;

                // Percorsi comuni per AutoComplete
                var paths = new[]
                {
                    "value", "label", "name", "displayName",
                    "option.value", "option.label",
                    "data.value", "data.label"
                };

                foreach (var path in paths)
                {
                    var value = ExtractNestedValue(itemObj, path);
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }

            return item?.GetSafeStringValue() ?? "";
        }

        /// <summary>
        /// Analizza un oggetto ricorsivamente con profondità limitata
        /// </summary>
        private static string AnalyzeObjectRecursively(JObject obj, int maxDepth = 2, int currentDepth = 0)
        {
            if (currentDepth >= maxDepth || obj == null)
                return "";

            try
            {
                // Cerca in tutte le proprietà dell'oggetto
                foreach (var prop in obj.Properties())
                {
                    if (prop.Value?.Type == JTokenType.String)
                    {
                        var value = prop.Value.GetSafeStringValue();
                        if (!string.IsNullOrEmpty(value) && value.Length > 2)
                        {
                            _logger?.LogDebug($"🔍 Valore ricorsivo trovato in {prop.Name}: {value}");
                            return value;
                        }
                    }
                    else if (prop.Value?.Type == JTokenType.Object && currentDepth < maxDepth - 1)
                    {
                        var nestedResult = AnalyzeObjectRecursively(prop.Value as JObject, maxDepth, currentDepth + 1);
                        if (!string.IsNullOrEmpty(nestedResult))
                            return nestedResult;
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore analisi ricorsiva oggetto", ex);
                return "";
            }
        }

        /// <summary>
        /// Estrae valore usando un percorso con notazione punto
        /// </summary>
        private static string ExtractNestedValue(JObject obj, string path)
        {
            try
            {
                var parts = path.Split('.');
                JToken current = obj;

                foreach (var part in parts)
                {
                    current = current?[part];
                    if (current == null)
                        return "";
                }

                return current.GetSafeStringValue();
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Estrae tutti i possibili nomi da un oggetto per debug
        /// </summary>
        public static List<string> ExtractAllPossibleNames(JObject obj, int maxDepth = 3)
        {
            var names = new List<string>();

            try
            {
                ExtractNamesRecursively(obj, "", names, maxDepth, 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Errore estrazione nomi oggetto", ex);
            }

            return names.Distinct().Take(50).ToList(); // Limita a 50 nomi
        }

        /// <summary>
        /// Estrae nomi ricorsivamente per debug
        /// </summary>
        private static void ExtractNamesRecursively(JToken token, string prefix, List<string> names, int maxDepth, int currentDepth)
        {
            if (currentDepth >= maxDepth || token == null)
                return;

            try
            {
                if (token.Type == JTokenType.Object)
                {
                    var obj = token as JObject;
                    foreach (var prop in obj.Properties())
                    {
                        var fullName = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";

                        if (prop.Value?.Type == JTokenType.String)
                        {
                            var value = prop.Value.GetSafeStringValue();
                            if (!string.IsNullOrEmpty(value))
                                names.Add($"{fullName} = \"{value}\"");
                        }
                        else if (prop.Value?.Type == JTokenType.Object)
                        {
                            ExtractNamesRecursively(prop.Value, fullName, names, maxDepth, currentDepth + 1);
                        }
                        else if (prop.Value?.Type == JTokenType.Array)
                        {
                            var array = prop.Value as JArray;
                            for (int i = 0; i < Math.Min(3, array.Count); i++) // Solo primi 3 elementi
                            {
                                ExtractNamesRecursively(array[i], $"{fullName}[{i}]", names, maxDepth, currentDepth + 1);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignora errori nell'estrazione per debug
            }
        }

        #endregion


       


    }
}