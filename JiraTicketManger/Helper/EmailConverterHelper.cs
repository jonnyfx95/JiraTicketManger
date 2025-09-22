using System;
using System.Text.RegularExpressions;
using JiraTicketManager.Services;

namespace JiraTicketManager.Helpers
{
    public static class EmailConverterHelper
    {
        private static readonly LoggingService _logger = LoggingService.CreateForComponent("EmailConverter");

        /// <summary>
        /// Converte un nome completo in email aziendale @dedagroup.it
        /// Logica: primo.restounito@dedagroup.it
        /// </summary>
        /// <param name="fullName">Nome completo (es. "Jonathan Felix Da Silva")</param>
        /// <returns>Email convertita (es. "jonathan.felixdasilva@dedagroup.it")</returns>
        /// <summary>
        /// Converte un nome completo in email aziendale @dedagroup.it
        /// Logica: primo.restounito@dedagroup.it CON GESTIONE CASI SPECIALI
        /// </summary>
        /// <param name="fullName">Nome completo (es. "Jonathan Felix Da Silva")</param>
        /// <returns>Email convertita (es. "jonathan.felixdasilva@dedagroup.it")</returns>
        public static string ConvertNameToEmail(string fullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    _logger.LogWarning("ConvertNameToEmail: Nome vuoto o null");
                    return null;
                }

                _logger.LogInfo($"Conversione nome: '{fullName}'");

                // 🔥 AGGIUNGI I CASI SPECIALI ALL'INIZIO
                var specialCases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NICOLA GIOVANNI LUPO", "nicolagiovanni.lupo@dedagroup.it" },
            { "JONATHAN FELIX DA SILVA", "jonathan.felixdasilva@dedagroup.it" },
            { "FRANCESCA FELICITA MAIELLO", "francescafelicita.maiello@dedagroup.it" },
            { "GIANNI LORENZO ZULLI", "giannilorenzo.zulli@dedagroup.it" },
            { "RAZVAN ALEXANDRU BARABANCEA", "razvanalexandru.barabancea@dedagroup.it" }
        };

                // Controlla se è un caso speciale
                if (specialCases.TryGetValue(fullName.Trim(), out var specialEmail))
                {
                    _logger.LogInfo($"Caso speciale applicato: '{fullName}' → '{specialEmail}'");
                    return specialEmail;
                }

                // Se non è un caso speciale, continua con la logica normale
                // Pulizia iniziale
                string cleanName = fullName.Trim()
                    .Replace("'", "")                       // Rimuovi apostrofi
                    .Replace("-", "")                       // Rimuovi trattini (non convertire a punti)
                    .Replace("  ", " ")                     // Spazi doppi -> singoli
                    .Trim();

                // Split in parole
                var words = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length < 2)
                {
                    _logger.LogWarning($"Nome '{fullName}' ha meno di 2 parole");
                    return null;
                }

                // Primo = nome, resto = cognomi uniti
                string firstName = words[0].ToLowerInvariant();

                // Unisci tutti i cognomi (dal secondo in poi) senza separatori
                string lastNames = "";
                for (int i = 1; i < words.Length; i++)
                {
                    lastNames += words[i].ToLowerInvariant();
                }

                // Pulizia caratteri non validi (solo lettere e numeri)
                firstName = Regex.Replace(firstName, @"[^a-z0-9]", "");
                lastNames = Regex.Replace(lastNames, @"[^a-z0-9]", "");

                // Validazione finale
                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastNames))
                {
                    _logger.LogWarning($"Nome '{fullName}' ha prodotto parti vuote: firstName='{firstName}', lastNames='{lastNames}'");
                    return null;
                }

                string finalEmail = $"{firstName}.{lastNames}@dedagroup.it";
                _logger.LogInfo($"Email convertita: '{finalEmail}'");

                return finalEmail;
            }
            catch (Exception ex)
            {
                _logger.LogError("ConvertNameToEmail", ex);
                return null;
            }
        }

        /// <summary>
        /// Converte un nome in formato Jira (punto separato, per assignee)
        /// </summary>
        /// <param name="fullName">Nome completo</param>
        /// <returns>Formato Jira (es. "jonathan.felix.da.silva")</returns>
        public static string ConvertNameToJiraFormat(string fullName)
        {
            try
            {
                var email = ConvertNameToEmail(fullName);
                if (string.IsNullOrEmpty(email))
                    return null;

                // Rimuovi dominio per formato Jira
                string jiraFormat = email.Replace("@dedagroup.it", "");
                _logger.LogInfo($"Formato Jira: '{jiraFormat}'");
                return jiraFormat;
            }
            catch (Exception ex)
            {
                _logger.LogError("ConvertNameToJiraFormat", ex);
                return null;
            }
        }

        /// <summary>
        /// Estrae e converte il primo nome valido trovato in un testo
        /// </summary>
        /// <param name="pageText">Testo della pagina</param>
        /// <returns>Email del primo nome valido trovato</returns>
        public static string ExtractAndConvertFirstName(string pageText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(pageText))
                    return null;

                _logger.LogInfo("Estrazione nome da testo pagina");

                // Pattern per trovare nomi nella pagina
                var namePatterns = new[]
                {
                    @"(?:Home\n|Tutorial\n)([A-Z][a-z]+(?:\s+[A-Z][a-z]*){1,3})",  // Nome dopo Home/Tutorial
                    @"([A-Z][a-z]+(?:\s+[A-Z][a-z]*){1,3})\s*(?=\n|$)",           // Nome seguito da newline
                    @"([A-Z][a-z]+\s+[A-Z][a-z]+(?:\s+[A-Z][a-z]*)*)"             // Pattern generico
                };

                foreach (var pattern in namePatterns)
                {
                    var matches = Regex.Matches(pageText, pattern);
                    foreach (Match match in matches)
                    {
                        string candidateName = match.Groups[1].Value.Trim();

                        // Filtra nomi validi
                        if (IsValidPersonName(candidateName))
                        {
                            _logger.LogInfo($"Nome valido trovato: '{candidateName}'");
                            return ConvertNameToEmail(candidateName);
                        }
                    }
                }

                _logger.LogWarning("Nessun nome valido trovato nel testo");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError("ExtractAndConvertFirstName", ex);
                return null;
            }
        }

        /// <summary>
        /// Verifica se una stringa è un nome di persona valido
        /// </summary>
        private static bool IsValidPersonName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Criteri di validazione
            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Deve avere almeno 2 parole
            if (words.Length < 2)
                return false;

            // Ogni parola deve iniziare con maiuscola
            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word) || !char.IsUpper(word[0]))
                    return false;
            }

            // Esclusioni (parole che non sono nomi)
            var exclusions = new[] { "DEDAGROUP", "COPYRIGHT", "TUTORIAL", "HOME", "SPA" };
            foreach (var exclusion in exclusions)
            {
                if (name.ToUpperInvariant().Contains(exclusion))
                    return false;
            }

            // Nome troppo lungo (probabilmente non è un nome)
            if (name.Length > 50)
                return false;

            return true;
        }

        /// <summary>
        /// Metodi di utilità per testing
        /// </summary>
        public static class TestHelpers
        {
            public static void TestConversions()
            {
                var logger = LoggingService.CreateForComponent("EmailConverter.Test");

                var testNames = new[]
                {
                    "Jonathan Felix Da Silva",
                    "Mario Rossi",
                    "Anna Maria Verdi",
                    "Giuseppe D'Angelo",
                    "Jean-Claude Van Damme"
                };

                logger.LogInfo("=== TEST CONVERSIONI EMAIL ===");
                foreach (var name in testNames)
                {
                    var email = ConvertNameToEmail(name);
                    var jira = ConvertNameToJiraFormat(name);
                    logger.LogInfo($"'{name}' → Email: '{email}' | Jira: '{jira}'");
                }
            }
        }

        /// <summary>
        /// Converte username in formato display per ComboBox
        /// Es: "andrea.rossi" → "ANDREA ROSSI"
        /// </summary>
        /// <param name="username">Username formato punto (es: "andrea.rossi")</param>
        /// <returns>Nome formattato per display (es: "ANDREA ROSSI")</returns>
        public static string FormatUsernameForDisplay(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    _logger.LogWarning("FormatUsernameForDisplay: Username vuoto o null");
                    return "";
                }

                _logger.LogDebug($"Formattazione username per display: '{username}'");

                // Se contiene @, estrai solo la parte prima della @
                if (username.Contains("@"))
                {
                    username = username.Split('@')[0];
                }

                // ✅ CASI SPECIALI - Gestione nomi composti
                var specialCases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "NICOLAGIOVANNI.LUPO", "NICOLA GIOVANNI LUPO" },
            { "JONATHAN.FELIXDASILVA", "JONATHAN FELIX DA SILVA" },
            { "FRANCESCAFELICITA.MAIELLO", "FRANCESCA FELICITA MAIELLO" },
            { "GIANNILORENZO.ZULLI", "GIANNI LORENZO ZULLI" },
            {"RAZVANALEXANDRU.BARABANCEA", "RAZVAN ALEXANDRU BARABANCEA"}

        };

                // Controlla se è un caso speciale
                var upperUsername = username.ToUpper();
                if (specialCases.ContainsKey(upperUsername))
                {
                    var specialResult = specialCases[upperUsername];
                    _logger.LogDebug($"Caso speciale applicato: '{username}' → '{specialResult}'");
                    return specialResult;
                }

                // ✅ LOGICA NORMALE - Converti punti in spazi e maiuscolo
                string displayName = username.Replace(".", " ").ToUpper().Trim();

                _logger.LogDebug($"Username '{username}' → Display: '{displayName}'");
                return displayName;
            }
            catch (Exception ex)
            {
                _logger.LogError("FormatUsernameForDisplay", ex);
                return username; // Fallback al valore originale
            }
        }


        // <summary>
        /// Pulisce il nome cliente rimuovendo asterischi e altri caratteri indesiderati
        /// </summary>
        /// <param name="clientName">Nome cliente da pulire</param>
        /// <returns>Nome cliente pulito senza asterischi</returns>
        public static string CleanClientName(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                return clientName ?? "";

            try
            {
                // Rimuovi asterischi multipli e singoli con tutti i pattern possibili
                var cleaned = clientName
                    .Replace(" ***", "")      // Rimuovi " ***" (con spazio)
                    .Replace("*** ", "")      // Rimuovi "*** " (con spazio dopo)
                    .Replace("***", "")       // Rimuovi "***" standalone
                    .Replace(" **", "")       // Rimuovi " **" (con spazio)
                    .Replace("** ", "")       // Rimuovi "** " (con spazio dopo)  
                    .Replace("**", "")        // Rimuovi "**" standalone
                    .Replace(" *", "")        // Rimuovi " *" (con spazio)
                    .Replace("* ", "")        // Rimuovi "* " (con spazio dopo)
                    .Trim()                   // Rimuovi spazi extra all'inizio/fine
                    .Replace("  ", " ");      // Rimuovi doppi spazi interni

                // Se il risultato è vuoto dopo la pulizia, ritorna l'originale
                return string.IsNullOrWhiteSpace(cleaned) ? clientName : cleaned;
            }
            catch (Exception)
            {
                // In caso di errore, ritorna il nome originale
                return clientName;
            }
        }

        /// <summary>
        /// Formatta il nome di una persona da MAIUSCOLO a Proper Case
        /// Es: "SERENA BELLAGOTTI" → "Serena Bellagotti"
        /// </summary>
        /// <param name="personName">Nome in formato maiuscolo</param>
        /// <returns>Nome formattato in Proper Case</returns>
        public static string FormatPersonName(string personName)
        {
            if (string.IsNullOrWhiteSpace(personName))
                return personName ?? "";

            try
            {
                // Split per parole e formatta ognuna
                var words = personName.Trim()
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                var formattedWords = new List<string>();

                foreach (var word in words)
                {
                    if (string.IsNullOrEmpty(word))
                        continue;

                    // Gestisci casi speciali per nomi italiani
                    var formattedWord = FormatSingleWord(word.Trim());
                    formattedWords.Add(formattedWord);
                }

                return string.Join(" ", formattedWords);
            }
            catch (Exception)
            {
                // Fallback: ritorna originale
                return personName;
            }
        }

        /// <summary>
        /// Formatta una singola parola gestendo casi speciali italiani
        /// </summary>
        public static string FormatSingleWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            // Converti tutto in minuscolo
            var lowerWord = word.ToLowerInvariant();

            // Gestisci prefissi e particelle comuni nei nomi italiani
            if (IsItalianNameParticle(lowerWord))
            {
                return lowerWord; // mantieni minuscolo per "di", "del", "della", etc.
            }

            // Gestisci abbreviazioni
            if (lowerWord.Length <= 2)
            {
                return word.ToUpperInvariant(); // "MC", "LA", etc.
            }

            // Formattazione standard: Prima lettera maiuscola
            return char.ToUpperInvariant(lowerWord[0]) + lowerWord.Substring(1);
        }

        /// <summary>
        /// Controlla se è una particella italiana da mantenere minuscola
        /// </summary>
        public static bool IsItalianNameParticle(string word)
        {
            var particles = new[]
            {
        "di", "del", "della", "delle", "dei", "degli", "da", "dal", "dalla",
        "de", "la", "le", "lo", "gli", "van", "von", "d'", "dell'", "dall'"
    };

            return particles.Contains(word.ToLowerInvariant());
        }
    }
}