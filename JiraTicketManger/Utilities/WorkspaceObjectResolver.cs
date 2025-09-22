using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using JiraTicketManager.Services;

namespace JiraTicketManager.Utilities
{
    /// <summary>
    /// Risolve workspace objects (Cliente Partner) tramite Jira Service Management Insight API
    /// Classe specializzata per gestire i reference ID e convertirli in nomi leggibili
    /// </summary>
    public class WorkspaceObjectResolver : IDisposable
    {
        #region Private Fields

        private readonly LoggingService _logger;
        private readonly HttpClient _httpClient;
        private readonly string _domain;
        private readonly string _username;
        private readonly string _apiToken;
        private bool _disposed = false;

        #endregion

        #region Constructor

        public WorkspaceObjectResolver()
        {
            _logger = LoggingService.CreateForComponent("WorkspaceObjectResolver");

            try
            {
                // Ottieni credenziali decrittate dal ConfigService esistente
                var configService = ConfigService.CreateDefault();
                (_domain, _username, _apiToken) = configService.GetDecryptedCredentials();

                if (string.IsNullOrEmpty(_domain) || string.IsNullOrEmpty(_username) || string.IsNullOrEmpty(_apiToken))
                {
                    _logger.LogWarning("❌ Credenziali mancanti per WorkspaceObjectResolver");
                    throw new InvalidOperationException("Credenziali Jira non configurate correttamente");
                }

                // Setup HttpClient con autenticazione Basic
                _httpClient = new HttpClient();
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_apiToken}"));
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

                _logger.LogInfo("WorkspaceObjectResolver inizializzato correttamente");
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore inizializzazione WorkspaceObjectResolver", ex);
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Risolve un workspace object dal suo JSON token
        /// </summary>
        /// <param name="workspaceObjectToken">JToken contenente workspaceId e objectId</param>
        /// <returns>Nome display risolto o fallback con ID</returns>
        public async Task<string> ResolveFromTokenAsync(JToken workspaceObjectToken)
        {
            try
            {
                _logger.LogDebug("🔍 Inizio ResolveFromTokenAsync");

                if (workspaceObjectToken?.Type != JTokenType.Object)
                {
                    _logger.LogWarning($"Token non è oggetto valido. Tipo: {workspaceObjectToken?.Type}");
                    return "[Token non valido]";
                }

                var obj = workspaceObjectToken as JObject;
                var workspaceId = obj?["workspaceId"]?.ToString();
                var objectId = obj?["objectId"]?.ToString();

                _logger.LogInfo($"Workspace ID: {workspaceId}");
                _logger.LogInfo($"Object ID: {objectId}");

                if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(objectId))
                {
                    _logger.LogWarning($"ID mancanti - WS: '{workspaceId}', Obj: '{objectId}'");
                    return $"[ID mancanti: WS={workspaceId}, Obj={objectId}]";
                }

                var result = await ResolveWorkspaceObjectAsync(workspaceId, objectId);
                _logger.LogInfo($"Risultato risoluzione: {result}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ResolveFromTokenAsync", ex);
                return "[Errore risoluzione token]";
            }
        }

        /// <summary>
        /// Risolve un array di workspace objects
        /// </summary>
        /// <param name="workspaceObjectArray">JArray contenente workspace objects</param>
        /// <returns>Nomi risolti separati da virgola</returns>
        public async Task<string> ResolveArrayAsync(JToken workspaceObjectArray)
        {
            try
            {
                _logger.LogDebug("🔍 Inizio ResolveArrayAsync");

                if (workspaceObjectArray?.Type != JTokenType.Array)
                {
                    _logger.LogWarning($"Token non è array valido. Tipo: {workspaceObjectArray?.Type}");
                    return "[Non è un array]";
                }

                var array = workspaceObjectArray as JArray;
                _logger.LogInfo($"Array con {array?.Count ?? 0} elementi");

                if (array?.Count == 0)
                {
                    _logger.LogInfo("Array vuoto");
                    return "";
                }

                var resolvedNames = new List<string>();

                for (int i = 0; i < array.Count; i++)
                {
                    var item = array[i];
                    _logger.LogInfo($"Elaborazione elemento {i + 1}/{array.Count}");

                    var resolvedName = await ResolveFromTokenAsync(item);
                    if (!string.IsNullOrEmpty(resolvedName) && !resolvedName.StartsWith("["))
                    {
                        resolvedNames.Add(resolvedName);
                        _logger.LogInfo($"✅ Elemento {i + 1} risolto: {resolvedName}");
                    }
                    else
                    {
                        _logger.LogWarning($"❌ Elemento {i + 1} non risolto: {resolvedName}");
                    }
                }

                if (resolvedNames.Count == 0)
                {
                    _logger.LogWarning("Nessun elemento risolto nell'array");
                    return "[Nessun elemento risolto]";
                }

                var result = string.Join(", ", resolvedNames);
                _logger.LogInfo($"✅ Array risolto completamente: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore ResolveArrayAsync", ex);
                return "[Errore risoluzione array]";
            }
        }


        /// <summary>
        /// Risolve direttamente workspace object con ID specifici
        /// </summary>
        /// <param name="workspaceId">ID del workspace Jira</param>
        /// <param name="objectId">ID dell'oggetto specifico</param>
        /// <returns>Nome display risolto</returns>
        public async Task<string> ResolveWorkspaceObjectAsync(string workspaceId, string objectId)
        {
            try
            {
                if (string.IsNullOrEmpty(workspaceId) || string.IsNullOrEmpty(objectId))
                {
                    return $"[ID mancanti: WS={workspaceId}, Obj={objectId}]";
                }

                _logger.LogDebug($"Risoluzione workspace object: {workspaceId}/{objectId}");

                // URL dell'API Insight che abbiamo testato e funziona
                var url = $"{_domain}/gateway/api/jsm/insight/workspace/{workspaceId}/v1/object/{objectId}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    // Estrai il nome display dal JSON
                    var displayName = ExtractDisplayNameFromJson(json);

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        _logger.LogInfo($"✅ Workspace object risolto: {objectId} → {displayName}");
                        return displayName;
                    }
                    else
                    {
                        _logger.LogWarning($"Nome display non trovato per object {objectId}");
                        return $"WorkspaceObject #{objectId}";
                    }
                }
                else
                {
                    _logger.LogWarning($"❌ API workspace object fallita: {response.StatusCode} per {objectId}");
                    return $"WorkspaceObject #{objectId}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore risoluzione workspace object {objectId}", ex);
                return $"WorkspaceObject #{objectId}";
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Estrae il nome display dal JSON dell'API Insight
        /// </summary>
        /// <param name="json">JSON response dall'API</param>
        /// <returns>Nome display estratto</returns>
        private string ExtractDisplayNameFromJson(JObject json)
        {
            try
            {
                // Strategia 1: Campi diretti più comuni
                var directFields = new[]
                {
                    "label",           // Campo più comune per display name in Insight
                    "displayName",
                    "name",
                    "title",
                    "value",
                    "objectDisplayName",
                    "summary"
                };

                foreach (var field in directFields)
                {
                    var value = json[field]?.ToString();
                    if (!string.IsNullOrEmpty(value) && value.Length > 2)
                    {
                        _logger.LogDebug($"Nome trovato in campo diretto '{field}': {value}");
                        return value;
                    }
                }

                // Strategia 2: Cerca nella sezione attributes (struttura tipica Insight)
                var attributes = json["attributes"];
                if (attributes is JArray attributesArray)
                {
                    foreach (var attr in attributesArray)
                    {
                        var attrName = attr["objectTypeAttribute"]?["name"]?.ToString()?.ToLower();

                        // Cerca attributi che potrebbero contenere il nome
                        if (attrName != null && (
                            attrName.Contains("name") ||
                            attrName.Contains("label") ||
                            attrName.Contains("title") ||
                            attrName.Contains("display") ||
                            attrName.Contains("description")))
                        {
                            var displayValue = attr["objectAttributeValues"]?[0]?["displayValue"]?.ToString();
                            if (!string.IsNullOrEmpty(displayValue))
                            {
                                _logger.LogDebug($"Nome trovato in attributo '{attrName}': {displayValue}");
                                return displayValue;
                            }

                            var value = attr["objectAttributeValues"]?[0]?["value"]?.ToString();
                            if (!string.IsNullOrEmpty(value))
                            {
                                _logger.LogDebug($"Valore trovato in attributo '{attrName}': {value}");
                                return value;
                            }
                        }
                    }

                    // Strategia 3: Se non trova nome specifico, usa il primo attributo con valore significativo
                    foreach (var attr in attributesArray)
                    {
                        var displayValue = attr["objectAttributeValues"]?[0]?["displayValue"]?.ToString();
                        if (!string.IsNullOrEmpty(displayValue) && displayValue.Length > 5)
                        {
                            _logger.LogDebug($"Nome fallback da primo attributo: {displayValue}");
                            return displayValue;
                        }
                    }
                }

                // Strategia 4: Prova nella sezione objectType come ultima risorsa
                var objectType = json["objectType"];
                if (objectType != null)
                {
                    var typeName = objectType["name"]?.ToString();
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        _logger.LogDebug($"Nome da objectType: {typeName}");
                        return $"[{typeName}]";
                    }
                }

                _logger.LogDebug("Nessun nome display trovato nel JSON");
                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError("Errore estrazione display name da JSON", ex);
                return "";
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _httpClient?.Dispose();
                    _logger.LogDebug("WorkspaceObjectResolver disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError("Errore dispose WorkspaceObjectResolver", ex);
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }
}