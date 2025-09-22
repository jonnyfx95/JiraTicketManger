using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    /// <summary>
    /// Servizio completo per gestire transizioni di stato dei ticket Jira.
    /// Supporta transizioni generiche per qualsiasi tipo di workflow.
    /// </summary>
    public class JiraTransitionService
    {
        private readonly JiraApiService _jiraApiService;
        private readonly LoggingService _logger;

        public JiraTransitionService(JiraApiService jiraApiService = null)
        {
            _jiraApiService = jiraApiService ?? JiraApiService.CreateFromSettings(SettingsService.CreateDefault());
            _logger = LoggingService.CreateForComponent("JiraTransition");
        }

        #region Public API Methods

        /// <summary>
        /// Esegue una transizione specificando il nome della transizione
        /// METODO PRINCIPALE per uso generico
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket (es: CC-12345)</param>
        /// <param name="transitionName">Nome esatto della transizione (es: "Assegna al secondo livello")</param>
        /// <returns>True se la transizione è stata eseguita con successo</returns>
        public async Task<TransitionResult> ExecuteTransitionByNameAsync(string ticketKey, string transitionName)
        {
            var result = new TransitionResult { TicketKey = ticketKey, RequestedTransition = transitionName };

            try
            {
                _logger.LogInfo($"Esecuzione transizione '{transitionName}' per {ticketKey}");

                // Ottieni stato corrente
                result.CurrentStatus = await GetCurrentStatusAsync(ticketKey);
                if (string.IsNullOrEmpty(result.CurrentStatus))
                {
                    result.ErrorMessage = "Impossibile ottenere lo stato corrente del ticket";
                    return result;
                }

                // Ottieni transizioni disponibili
                var availableTransitions = await GetAvailableTransitionsAsync(ticketKey);
                if (availableTransitions == null || !availableTransitions.Any())
                {
                    result.ErrorMessage = "Nessuna transizione disponibile per questo ticket";
                    return result;
                }

                // Trova la transizione richiesta
                var targetTransition = FindTransitionByName(availableTransitions, transitionName);
                if (targetTransition == null)
                {
                    result.ErrorMessage = $"Transizione '{transitionName}' non trovata";
                    result.AvailableTransitions = availableTransitions.Select(t => t.Name).ToList();
                    return result;
                }

                // Esegui la transizione
                result.Success = await ExecuteTransitionAsync(ticketKey, targetTransition.Id);
                if (result.Success)
                {
                    result.NewStatus = targetTransition.ToStatus;
                    result.TransitionId = targetTransition.Id;
                    _logger.LogInfo($"Transizione completata: {result.CurrentStatus} → {result.NewStatus}");
                }
                else
                {
                    result.ErrorMessage = "Errore durante l'esecuzione della transizione";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore transizione '{transitionName}' per {ticketKey}: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Esegue una transizione specificando l'ID della transizione
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <param name="transitionId">ID della transizione (es: "51")</param>
        /// <returns>Risultato della transizione</returns>
        public async Task<TransitionResult> ExecuteTransitionByIdAsync(string ticketKey, string transitionId)
        {
            var result = new TransitionResult { TicketKey = ticketKey, TransitionId = transitionId };

            try
            {
                _logger.LogInfo($"Esecuzione transizione ID '{transitionId}' per {ticketKey}");

                result.CurrentStatus = await GetCurrentStatusAsync(ticketKey);
                result.Success = await ExecuteTransitionAsync(ticketKey, transitionId);

                if (result.Success)
                {
                    // Ottieni il nuovo stato dopo la transizione
                    await Task.Delay(1000); // Piccola pausa per permettere a Jira di aggiornare
                    result.NewStatus = await GetCurrentStatusAsync(ticketKey);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore transizione ID '{transitionId}' per {ticketKey}: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Transisce verso uno stato target specifico (cerca automaticamente la transizione)
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <param name="targetStatusName">Nome dello stato target (es: "Completato")</param>
        /// <returns>Risultato della transizione</returns>
        public async Task<TransitionResult> TransitionToStatusAsync(string ticketKey, string targetStatusName)
        {
            var result = new TransitionResult { TicketKey = ticketKey, RequestedTargetStatus = targetStatusName };

            try
            {
                _logger.LogInfo($"Transizione verso stato '{targetStatusName}' per {ticketKey}");

                result.CurrentStatus = await GetCurrentStatusAsync(ticketKey);

                // Controlla se è già nello stato target
                if (string.Equals(result.CurrentStatus, targetStatusName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Success = true;
                    result.NewStatus = targetStatusName;
                    _logger.LogInfo($"Ticket {ticketKey} già nello stato '{targetStatusName}'");
                    return result;
                }

                // Trova transizione verso lo stato target
                var availableTransitions = await GetAvailableTransitionsAsync(ticketKey);
                var targetTransition = availableTransitions?.FirstOrDefault(t =>
                    string.Equals(t.ToStatus, targetStatusName, StringComparison.OrdinalIgnoreCase));

                if (targetTransition == null)
                {
                    result.ErrorMessage = $"Nessuna transizione disponibile verso '{targetStatusName}'";
                    result.AvailableTransitions = availableTransitions?.Select(t => $"{t.Name} → {t.ToStatus}").ToList() ?? new List<string>();
                    return result;
                }

                result.RequestedTransition = targetTransition.Name;
                result.Success = await ExecuteTransitionAsync(ticketKey, targetTransition.Id);

                if (result.Success)
                {
                    result.NewStatus = targetTransition.ToStatus;
                    result.TransitionId = targetTransition.Id;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore transizione verso '{targetStatusName}' per {ticketKey}: {ex.Message}", ex);
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Ottiene tutte le transizioni disponibili per un ticket
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <returns>Lista delle transizioni disponibili</returns>
        public async Task<List<JiraTransition>> GetAvailableTransitionsAsync(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Caricamento transizioni disponibili per {ticketKey}");

                // Costruisci URL per l'endpoint transitions (API v2)
                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}/transitions";

                using var httpClient = new HttpClient();

                var authHeader = _jiraApiService.GetAuthorizationHeader();
                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogError("Nessuna autorizzazione disponibile per API transitions");
                    return new List<JiraTransition>();
                }

                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Errore API transizioni: {response.StatusCode}");

                    // Fallback con API v3
                    return await TryGetTransitionsWithApiV3(ticketKey);
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var jsonObject = JObject.Parse(jsonContent);

                var transitions = new List<JiraTransition>();
                var transitionsArray = jsonObject["transitions"] as JArray;

                if (transitionsArray != null)
                {
                    foreach (var transition in transitionsArray)
                    {
                        var jiraTransition = ParseTransitionFromJson(transition);
                        if (jiraTransition != null)
                        {
                            transitions.Add(jiraTransition);
                        }
                    }
                }

                _logger.LogInfo($"Trovate {transitions.Count} transizioni per {ticketKey}");
                return transitions;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore lettura transizioni per {ticketKey}: {ex.Message}");
                return new List<JiraTransition>();
            }
        }

        /// <summary>
        /// Ottiene lo stato corrente del ticket
        /// </summary>
        /// <param name="ticketKey">Chiave del ticket</param>
        /// <returns>Nome dello stato corrente</returns>
        public async Task<string> GetCurrentStatusAsync(string ticketKey)
        {
            try
            {
                var jql = $"key = {ticketKey}";
                var searchResult = await _jiraApiService.SearchIssuesAsync(jql, 0, 1);

                if (searchResult?.Issues?.Count > 0)
                {
                    var issue = searchResult.Issues[0];
                    var statusName = issue["fields"]?["status"]?["name"]?.ToString();
                    return statusName ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore lettura stato per {ticketKey}: {ex.Message}");
                return "";
            }
        }

        #endregion

        #region Convenience Methods for Common Workflows

        /// <summary>
        /// Metodo di convenienza per il workflow di pianificazione
        /// AGGIORNATO: Gestisce transizioni sequenziali e evita duplicazioni
        /// </summary>
        public async Task<TransitionResult> TransitionToPlanningCompleteAsync(string ticketKey)
        {
            try
            {
                _logger.LogInfo($"Inizio transizione pianificazione completa per {ticketKey}");

                // Ottieni stato corrente
                var currentStatus = await GetCurrentStatusAsync(ticketKey);
                _logger.LogInfo($"Stato corrente: '{currentStatus}'");

                // Se è già pianificato, non fare nulla
                if (string.Equals(currentStatus, "Attività Pianificata", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo($"Ticket {ticketKey} già in stato 'Attività Pianificata'");
                    return new TransitionResult
                    {
                        TicketKey = ticketKey,
                        Success = true,
                        CurrentStatus = currentStatus,
                        NewStatus = currentStatus
                    };
                }

                // GESTIONE SEQUENZIALE: Nuovo → Pianificazione Attività → Attività Pianificata
                var finalResult = new TransitionResult { TicketKey = ticketKey, CurrentStatus = currentStatus };

                // Passo 1: Se è "Nuovo", vai a "Pianificazione Attività"
                if (string.Equals(currentStatus, "Nuovo", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo($"Passo 1: Transizione {currentStatus} → Pianificazione Attività");

                    var step1Result = await TransitionToStatusAsync(ticketKey, "Pianificazione Attività");
                    if (!step1Result.Success)
                    {
                        _logger.LogError($"Passo 1 fallito: {step1Result.ErrorMessage}");
                        return step1Result; // Restituisci il fallimento del primo passo
                    }

                    _logger.LogInfo($"Passo 1 completato: {step1Result.CurrentStatus} → {step1Result.NewStatus}");
                    finalResult.CurrentStatus = currentStatus; // Mantieni lo stato originale nel risultato finale

                    // Aggiorna lo stato corrente per il passo 2
                    currentStatus = step1Result.NewStatus;

                    // Piccola pausa per permettere a Jira di aggiornare
                    await Task.Delay(1000);
                }

                // Passo 2: Da "Pianificazione Attività" → "Attività Pianificata"
                if (string.Equals(currentStatus, "Pianificazione Attività", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInfo($"Passo 2: Transizione {currentStatus} → Attività Pianificata");

                    var step2Result = await TransitionToStatusAsync(ticketKey, "Attività Pianificata");
                    if (!step2Result.Success)
                    {
                        _logger.LogError($"Passo 2 fallito: {step2Result.ErrorMessage}");
                        // Se il passo 2 fallisce ma il passo 1 è riuscito, è un successo parziale
                        finalResult.Success = false;
                        finalResult.ErrorMessage = $"Transizione parziale completata. Passo 2 fallito: {step2Result.ErrorMessage}";
                        finalResult.NewStatus = currentStatus; // Rimane in "Pianificazione Attività"
                        return finalResult;
                    }

                    _logger.LogInfo($"Passo 2 completato: {step2Result.CurrentStatus} → {step2Result.NewStatus}");

                    // Successo completo
                    finalResult.Success = true;
                    finalResult.NewStatus = step2Result.NewStatus;
                    finalResult.TransitionId = step2Result.TransitionId;

                    return finalResult;
                }

                // Se non è in nessuno degli stati gestiti, prova transizione diretta
                _logger.LogInfo($"Tentativo transizione diretta da '{currentStatus}' a 'Attività Pianificata'");
                return await TransitionToStatusAsync(ticketKey, "Attività Pianificata");

            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore transizione pianificazione per {ticketKey}: {ex.Message}");
                return new TransitionResult
                {
                    TicketKey = ticketKey,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Metodo di convenienza per completare un ticket
        /// </summary>
        public async Task<TransitionResult> CompleteTicketAsync(string ticketKey)
        {
            // Prima prova "Completato", poi fallback su altri stati di completamento
            var preferredStates = new[] { "Completato", "Done", "Risolto", "Closed", "Chiuso" };

            foreach (var state in preferredStates)
            {
                var result = await TransitionToStatusAsync(ticketKey, state);
                if (result.Success) return result;
            }

            // Se nessuno stato di completamento è raggiungibile, restituisci l'ultimo tentativo
            return await TransitionToStatusAsync(ticketKey, "Completato");
        }

        #endregion

        #region Private Helper Methods

        private JiraTransition FindTransitionByName(List<JiraTransition> transitions, string transitionName)
        {
            // Cerca match esatto
            var exactMatch = transitions.FirstOrDefault(t =>
                string.Equals(t.Name, transitionName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null) return exactMatch;

            // Cerca match parziale
            var partialMatch = transitions.FirstOrDefault(t =>
                t.Name.Contains(transitionName, StringComparison.OrdinalIgnoreCase));

            return partialMatch;
        }

        private JiraTransition ParseTransitionFromJson(JToken transition)
        {
            try
            {
                var id = transition["id"]?.ToString();
                var name = transition["name"]?.ToString();
                var toStatusName = transition["to"]?["name"]?.ToString();
                var toStatusId = transition["to"]?["id"]?.ToString();

                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                {
                    return new JiraTransition
                    {
                        Id = id,
                        Name = name,
                        ToStatus = toStatusName ?? "Unknown",
                        ToStatusId = toStatusId ?? ""
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore parsing transizione: {ex.Message}");
            }

            return null;
        }

        private async Task<List<JiraTransition>> TryGetTransitionsWithApiV3(string ticketKey)
        {
            try
            {
                var url = $"{_jiraApiService.Domain}/rest/api/3/issue/{ticketKey}/transitions";

                using var httpClient = new HttpClient();
                var authHeader = _jiraApiService.GetAuthorizationHeader();

                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonContent);

                    var transitions = new List<JiraTransition>();
                    var transitionsArray = jsonObject["transitions"] as JArray;

                    if (transitionsArray != null)
                    {
                        foreach (var transition in transitionsArray)
                        {
                            var jiraTransition = ParseTransitionFromJson(transition);
                            if (jiraTransition != null)
                            {
                                transitions.Add(jiraTransition);
                            }
                        }
                    }

                    _logger.LogInfo($"Fallback API v3 riuscito: {transitions.Count} transizioni");
                    return transitions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore fallback API v3: {ex.Message}");
            }

            return new List<JiraTransition>();
        }

        private async Task<bool> ExecuteTransitionAsync(string ticketKey, string transitionId)
        {
            try
            {
                _logger.LogInfo($"Esecuzione transizione ID {transitionId} per {ticketKey}");

                var url = $"{_jiraApiService.Domain}/rest/api/2/issue/{ticketKey}/transitions";

                using var httpClient = new HttpClient();

                var authHeader = _jiraApiService.GetAuthorizationHeader();
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var payload = new
                {
                    transition = new
                    {
                        id = transitionId
                    }
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInfo($"Transizione {transitionId} eseguita con successo per {ticketKey}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Errore esecuzione transizione {transitionId} per {ticketKey}: {response.StatusCode} - {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore esecuzione transizione {transitionId} per {ticketKey}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Rappresenta una transizione Jira
        /// </summary>
        public class JiraTransition
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string ToStatus { get; set; }
            public string ToStatusId { get; set; }

            public override string ToString()
            {
                return $"{Name} → {ToStatus}";
            }
        }

        /// <summary>
        /// Risultato di un'operazione di transizione
        /// </summary>
        public class TransitionResult
        {
            public string TicketKey { get; set; }
            public bool Success { get; set; }
            public string CurrentStatus { get; set; }
            public string NewStatus { get; set; }
            public string RequestedTransition { get; set; }
            public string RequestedTargetStatus { get; set; }
            public string TransitionId { get; set; }
            public string ErrorMessage { get; set; }
            public List<string> AvailableTransitions { get; set; } = new List<string>();

            public string GetUserMessage()
            {
                if (Success)
                {
                    if (CurrentStatus == NewStatus)
                        return $"Ticket {TicketKey} già nello stato '{NewStatus}'";
                    else
                        return $"Transizione completata: {CurrentStatus} → {NewStatus}";
                }
                else
                {
                    var message = $"Transizione fallita per {TicketKey}";
                    if (!string.IsNullOrEmpty(ErrorMessage))
                        message += $": {ErrorMessage}";

                    if (AvailableTransitions.Any())
                        message += $"\nTransizioni disponibili: {string.Join(", ", AvailableTransitions)}";

                    return message;
                }
            }
        }

        #endregion

        #region Debug Methods

        /// <summary>
        /// Metodo debug per investigare le transizioni disponibili
        /// </summary>
        public async Task<string> DebugTransitionsAsync(string ticketKey)
        {
            var report = new StringBuilder();
            report.AppendLine($"=== DEBUG TRANSIZIONI PER {ticketKey} ===");
            report.AppendLine($"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            try
            {
                // Stato corrente
                var currentStatus = await GetCurrentStatusAsync(ticketKey);
                report.AppendLine($"Stato corrente: '{currentStatus}'");
                report.AppendLine();

                // Transizioni disponibili
                var transitions = await GetAvailableTransitionsAsync(ticketKey);
                report.AppendLine($"Transizioni disponibili ({transitions.Count}):");

                foreach (var t in transitions)
                {
                    report.AppendLine($"  - ID: {t.Id}, Nome: '{t.Name}' → '{t.ToStatus}'");
                }

                report.AppendLine();
                report.AppendLine("=== ESEMPI DI USO ===");
                report.AppendLine("// Per nome transizione:");
                foreach (var t in transitions.Take(3))
                {
                    report.AppendLine($"await transitionService.ExecuteTransitionByNameAsync(\"{ticketKey}\", \"{t.Name}\");");
                }

                report.AppendLine();
                report.AppendLine("// Per stato target:");
                var uniqueStates = transitions.Select(t => t.ToStatus).Distinct();
                foreach (var state in uniqueStates.Take(3))
                {
                    report.AppendLine($"await transitionService.TransitionToStatusAsync(\"{ticketKey}\", \"{state}\");");
                }
            }
            catch (Exception ex)
            {
                report.AppendLine($"ERRORE: {ex.Message}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Salva report debug su file
        /// </summary>
        public static async Task SaveDebugReportAsync(string ticketKey, string report)
        {
            try
            {
                var fileName = $"transition_debug_{ticketKey}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);

                await File.WriteAllTextAsync(filePath, report);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LoggingService.CreateForComponent("TransitionDebug").LogError($"Errore salvataggio report: {ex.Message}");
            }
        }

        #endregion
    }
}