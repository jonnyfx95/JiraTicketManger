using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JiraTicketManager.Tools
{
    /// <summary>
    /// Tool per generare file di mappatura Aree → Applicativi dai dati API
    /// CORREZIONE: Gli applicativi NON devono essere splittati sulle virgole
    /// </summary>
    public class AreaApplicativoMappingGenerator
    {
        /// <summary>
        /// Genera file di mappatura completo dai dati grezzi
        /// </summary>
        public static void GenerateMappingFile(string inputData, string outputPath = null)
        {
            try
            {
                // Parse dei dati dal log - CORRETTO: no split sulle virgole
                var applicativi = ParseApplicativiFromLog(inputData);

                // Genera mappatura
                var mapping = GenerateMapping(applicativi);

                // Genera file output
                var output = GenerateOutputFile(mapping, applicativi);

                // Salva file
                if (string.IsNullOrEmpty(outputPath))
                    outputPath = Path.Combine(Environment.CurrentDirectory, "AreaApplicativoMapping.txt");

                File.WriteAllText(outputPath, output, Encoding.UTF8);

                Console.WriteLine($"✅ File mappatura generato: {outputPath}");
                Console.WriteLine($"📊 Statistiche:");
                Console.WriteLine($"   - Totale applicativi: {applicativi.Count}");
                Console.WriteLine($"   - Aree trovate: {mapping.Keys.Count(k => k.StartsWith("AREA:"))}");
                Console.WriteLine($"   - Altri prodotti: {mapping.Keys.Count(k => !k.StartsWith("AREA:"))}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Errore: {ex.Message}");
            }
        }

        /// <summary>
        /// Estrae lista applicativi dal log grezzo
        /// CORREZIONE: Parse corretto senza split errato sulle virgole
        /// </summary>
        private static List<string> ParseApplicativiFromLog(string logData)
        {
            var applicativi = new List<string>();

            // Il formato del log è: "field customfield_10114 valori: app1, app2, app3, ..."
            var startMarker = "valori: ";
            var startIndex = logData.IndexOf(startMarker);

            if (startIndex == -1)
                throw new ArgumentException("Formato log non riconosciuto");

            var dataSection = logData.Substring(startIndex + startMarker.Length);

            // CORREZIONE: Parse intelligente che riconosce i pattern corretti
            var items = ParseApplicativiIntelligente(dataSection);

            foreach (var item in items)
            {
                var cleanItem = item.Trim();
                if (!string.IsNullOrEmpty(cleanItem))
                    applicativi.Add(cleanItem);
            }

            return applicativi.OrderBy(a => a).ToList();
        }

        /// <summary>
        /// Parse intelligente che riconosce i pattern corretti degli applicativi
        /// </summary>
        private static List<string> ParseApplicativiIntelligente(string dataSection)
        {
            var applicativi = new List<string>();
            var currentApp = new StringBuilder();
            var chars = dataSection.ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                char currentChar = chars[i];

                // Se troviamo una virgola
                if (currentChar == ',')
                {
                    // Controlla se è la fine di un applicativo o parte del nome
                    if (IsEndOfApplicativo(chars, i))
                    {
                        // Fine applicativo - aggiungi alla lista
                        var app = currentApp.ToString().Trim();
                        if (!string.IsNullOrEmpty(app))
                        {
                            applicativi.Add(app);
                        }
                        currentApp.Clear();

                        // Salta lo spazio dopo la virgola
                        if (i + 1 < chars.Length && chars[i + 1] == ' ')
                            i++;
                    }
                    else
                    {
                        // Virgola parte del nome - mantienila
                        currentApp.Append(currentChar);
                    }
                }
                else
                {
                    currentApp.Append(currentChar);
                }
            }

            // Aggiungi l'ultimo applicativo
            var lastApp = currentApp.ToString().Trim();
            if (!string.IsNullOrEmpty(lastApp))
            {
                applicativi.Add(lastApp);
            }

            return applicativi;
        }

        /// <summary>
        /// Determina se una virgola indica la fine di un applicativo
        /// </summary>
        private static bool IsEndOfApplicativo(char[] chars, int commaIndex)
        {
            // Cerca il testo dopo la virgola
            int nextIndex = commaIndex + 1;

            // Salta spazi
            while (nextIndex < chars.Length && chars[nextIndex] == ' ')
                nextIndex++;

            if (nextIndex >= chars.Length)
                return true; // Fine stringa

            // Controlla se dopo la virgola inizia un nuovo pattern di applicativo
            var remainingText = new string(chars, nextIndex, Math.Min(80, chars.Length - nextIndex));

            // Pattern che indicano inizio nuovo applicativo:
            var newAppPatterns = new[]
            {
                "Civilia Next - Area ",
                "Civilia Next - ",
                "Civilia Next Area Comune -> ",
                "Civilia Next -> ",  // AGGIUNTO: per Civilia Next -> GeoNext, Muse
                "Sistema Informativo Territoriale -> ",
                "Customer Care - ",
                "Civlia Web -> ",
                "Folium -> ",
                "Metadatamanager -> ",
                "Civilia - "
            };

            return newAppPatterns.Any(pattern => remainingText.StartsWith(pattern));
        }

        /// <summary>
        /// Genera mappatura organizzata
        /// </summary>
        private static Dictionary<string, List<string>> GenerateMapping(List<string> applicativi)
        {
            var mapping = new Dictionary<string, List<string>>();

            foreach (var app in applicativi)
            {
                var category = DetermineCategory(app);

                if (!mapping.ContainsKey(category))
                    mapping[category] = new List<string>();

                mapping[category].Add(app);
            }

            return mapping;
        }

        /// <summary>
        /// Determina la categoria dell'applicativo
        /// CORREZIONE: Pattern aggiornati e corretti
        /// </summary>
        private static string DetermineCategory(string applicativo)
        {
            // Pattern 1: Civilia Next - Area [NOME] -> [APP]
            if (applicativo.StartsWith("Civilia Next - Area ") && applicativo.Contains(" -> "))
            {
                var areaPart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return $"AREA: {areaPart.Replace("Civilia Next - Area ", "")}";
            }

            // Pattern 2: Civilia Next Area Comune -> [APP] (caso speciale)
            if (applicativo.StartsWith("Civilia Next Area Comune -> "))
            {
                return "AREA: Area Comune";
            }

            // Pattern 3: Sistema Informativo Territoriale -> [APP]
            if (applicativo.StartsWith("Sistema Informativo Territoriale -> "))
            {
                return "AREA: Sistema Informativo Territoriale";
            }

            // Pattern 4: Customer Care - [APP]
            if (applicativo.StartsWith("Customer Care - "))
            {
                return "AREA: Customer Care";
            }

            // Pattern 5: Civilia Next - [SERVIZIO] -> [APP] (Servizi)
            if (applicativo.StartsWith("Civilia Next - ") && applicativo.Contains(" -> "))
            {
                var servicePart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return $"SERVIZIO: {servicePart.Replace("Civilia Next - ", "")}";
            }

            // Pattern 6: Civilia Next -> [COMPONENTE] (componenti diretti)
            if (applicativo.StartsWith("Civilia Next -> "))
            {
                return "COMPONENTE: Civilia Next";
            }

            // Pattern 7: [PRODOTTO] -> [COMPONENTE] (altri prodotti)
            if (applicativo.Contains(" -> "))
            {
                var productPart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return $"PRODOTTO: {productPart}";
            }

            // Pattern 8: Standalone (dovrebbe essere molto raro ora)
            return "STANDALONE";
        }

        /// <summary>
        /// Genera contenuto file di output
        /// </summary>
        private static string GenerateOutputFile(Dictionary<string, List<string>> mapping, List<string> allApplicativi)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("🎯 MAPPATURA AREE E APPLICATIVI");
            sb.AppendLine($"📅 Generato: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"📊 Totale applicativi: {allApplicativi.Count}");
            sb.AppendLine();
            sb.AppendLine("==========================================");
            sb.AppendLine();

            // Sezione Aree (prioritaria)
            var areas = mapping.Keys.Where(k => k.StartsWith("AREA:")).OrderBy(k => k).ToList();
            if (areas.Any())
            {
                sb.AppendLine("🏢 AREE CIVILIA NEXT:");
                sb.AppendLine();

                foreach (var area in areas)
                {
                    var areaName = area.Replace("AREA: ", "");
                    var apps = mapping[area].OrderBy(a => a).ToList();

                    sb.AppendLine($"📁 {areaName} ({apps.Count} applicativi)");
                    sb.AppendLine($"   Valore originale: \"{GetAreaOriginalValue(areaName)}\"");
                    sb.AppendLine("   Applicativi:");

                    foreach (var app in apps)
                    {
                        var appName = ExtractAppName(app);
                        sb.AppendLine($"   ├── {appName}");
                        sb.AppendLine($"   │   Valore originale: \"{app}\"");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("==========================================");
                sb.AppendLine();
            }

            // Sezione Servizi
            var services = mapping.Keys.Where(k => k.StartsWith("SERVIZIO:")).OrderBy(k => k).ToList();
            if (services.Any())
            {
                sb.AppendLine("🔧 SERVIZI CIVILIA NEXT:");
                sb.AppendLine();

                foreach (var service in services)
                {
                    var serviceName = service.Replace("SERVIZIO: ", "");
                    var apps = mapping[service].OrderBy(a => a).ToList();

                    sb.AppendLine($"📁 {serviceName} ({apps.Count} applicativi)");
                    foreach (var app in apps)
                    {
                        var appName = ExtractAppName(app);
                        sb.AppendLine($"   ├── {appName}");
                        sb.AppendLine($"   │   Valore originale: \"{app}\"");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("==========================================");
                sb.AppendLine();
            }

            // Sezione Componenti
            var components = mapping.Keys.Where(k => k.StartsWith("COMPONENTE:")).OrderBy(k => k).ToList();
            if (components.Any())
            {
                sb.AppendLine("🧩 COMPONENTI:");
                sb.AppendLine();

                foreach (var component in components)
                {
                    var componentName = component.Replace("COMPONENTE: ", "");
                    var apps = mapping[component].OrderBy(a => a).ToList();

                    sb.AppendLine($"📁 {componentName} ({apps.Count} componenti)");
                    foreach (var app in apps)
                    {
                        var appName = ExtractAppName(app);
                        sb.AppendLine($"   ├── {appName}");
                        sb.AppendLine($"   │   Valore originale: \"{app}\"");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("==========================================");
                sb.AppendLine();
            }

            // Sezione Prodotti
            var products = mapping.Keys.Where(k => k.StartsWith("PRODOTTO:")).OrderBy(k => k).ToList();
            if (products.Any())
            {
                sb.AppendLine("📦 ALTRI PRODOTTI:");
                sb.AppendLine();

                foreach (var product in products)
                {
                    var productName = product.Replace("PRODOTTO: ", "");
                    var apps = mapping[product].OrderBy(a => a).ToList();

                    sb.AppendLine($"📁 {productName} ({apps.Count} componenti)");
                    foreach (var app in apps)
                    {
                        var appName = ExtractAppName(app);
                        sb.AppendLine($"   ├── {appName}");
                        sb.AppendLine($"   │   Valore originale: \"{app}\"");
                    }
                    sb.AppendLine();
                }

                sb.AppendLine("==========================================");
                sb.AppendLine();
            }

            // Sezione Standalone (se esistono)
            if (mapping.ContainsKey("STANDALONE"))
            {
                var standalone = mapping["STANDALONE"].OrderBy(a => a).ToList();
                sb.AppendLine($"🔹 APPLICATIVI STANDALONE ({standalone.Count}):");
                sb.AppendLine();

                foreach (var app in standalone)
                {
                    sb.AppendLine($"   • {app}");
                }
                sb.AppendLine();
                sb.AppendLine("==========================================");
                sb.AppendLine();
            }

            // Statistiche finali
            sb.AppendLine("📈 STATISTICHE:");
            sb.AppendLine($"   • Aree Civilia Next: {areas.Count}");
            sb.AppendLine($"   • Servizi Civilia Next: {services.Count}");
            sb.AppendLine($"   • Componenti: {components.Count}");
            sb.AppendLine($"   • Altri prodotti: {products.Count}");
            sb.AppendLine($"   • Standalone: {(mapping.ContainsKey("STANDALONE") ? mapping["STANDALONE"].Count : 0)}");
            sb.AppendLine($"   • TOTALE: {allApplicativi.Count} applicativi");

            return sb.ToString();
        }

        /// <summary>
        /// Ottiene il valore originale per un'area per le query JQL
        /// </summary>
        private static string GetAreaOriginalValue(string areaName)
        {
            return areaName switch
            {
                "Area Comune" => "Civilia Next Area Comune",
                "Sistema Informativo Territoriale" => "Sistema Informativo Territoriale",
                "Customer Care" => "Customer Care",
                _ => $"Civilia Next - Area {areaName}"
            };
        }

        /// <summary>
        /// Estrae nome applicativo pulito (parte dopo ->)
        /// </summary>
        private static string ExtractAppName(string fullApp)
        {
            if (fullApp.Contains(" -> "))
            {
                var parts = fullApp.Split(new[] { " -> " }, 2, StringSplitOptions.None);
                return parts[1].Trim();
            }
            return fullApp;
        }

        /// <summary>
        /// Metodo pubblico per estrarre l'area da un applicativo (per uso in ComboBoxManager)
        /// </summary>
        /// </summary>

        public static string ExtractAreaFromApplicativo(string applicativo)
        {
            if (string.IsNullOrEmpty(applicativo))
                return null;

            // Pattern 1: Civilia Next - Area [NOME] -> [APP]
            if (applicativo.StartsWith("Civilia Next - Area ") && applicativo.Contains(" -> "))
            {
                var areaPart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return areaPart.Replace("Civilia Next - Area ", "");
            }

            // Pattern 2: Civilia Next Area Comune -> [APP] (caso speciale)
            if (applicativo.StartsWith("Civilia Next Area Comune -> "))
            {
                return "Area Comune";
            }

            // Pattern 3: Civilia Next - [SERVIZIO] -> [APP] (SERVIZI SONO AREE VALIDE!)
            // DEVE VENIRE PRIMA di "Civilia Next -> " per evitare conflitti
            if (applicativo.StartsWith("Civilia Next - ") && applicativo.Contains(" -> "))
            {
                var servicePart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return servicePart.Replace("Civilia Next - ", "");
            }

            // Pattern 4: Civilia - [PRODOTTO] -> [APP] (ALTRI PRODOTTI con area)
            if (applicativo.StartsWith("Civilia - ") && applicativo.Contains(" -> "))
            {
                var productPart = applicativo.Split(new[] { " -> " }, 2, StringSplitOptions.None)[0];
                return productPart; // Restituisce "Civilia - GeoNext", "Civilia - Fattura Elettronica"
            }

            // Pattern 5: Sistema Informativo Territoriale -> [APP]
            if (applicativo.StartsWith("Sistema Informativo Territoriale -> "))
            {
                return "Sistema Informativo Territoriale";
            }

            // Pattern 6: Customer Care - [APP]
            if (applicativo.StartsWith("Customer Care - "))
            {
                return "Customer Care";
            }

            // 🔧 NUOVO Pattern 7: Civlia Web -> [APP] (area "Civlia Web")
            if (applicativo.StartsWith("Civlia Web -> "))
            {
                return "Civlia Web";
            }

            // 🔧 Pattern 8: Civilia Next -> GeoNext (associa a area Civilia - GeoNext)
            if (applicativo == "Civilia Next -> GeoNext")
            {
                return "Civilia - GeoNext";
            }

            // 🔧 Pattern 10: Civilia Next -> Muse (associa a area Civilia - Muse)
            if (applicativo == "Civilia Next -> Muse")
            {
                return "Civilia - Muse";
            }

            // 🔧 Pattern 9: Folium -> [APP] (area "Folium")
            if (applicativo.StartsWith("Folium -> "))
            {
                return "Folium";
            }

            // ❌ Altri componenti Civilia Next -> [COMPONENTE] rimangono senza area
            // Altri pattern (Metadatamanager ->) rimangono SENZA AREA
            return null;
        }

        /// <summary>
        /// Metodo pubblico per estrarre il nome dell'applicativo (per uso in ComboBoxManager)
        /// </summary>
        public static string ExtractApplicativoDisplayName(string applicativo)
        {
            if (string.IsNullOrEmpty(applicativo))
                return applicativo;

            // 🔧 PRIMO: Metadatamanager -> MDMGR rimane invariato (DEVE VENIRE PRIMA)
            if (applicativo.StartsWith("Metadatamanager -> "))
            {
                return applicativo; // Mantieni nome completo "Metadatamanager -> MDMGR"
            }

            // Pattern speciale: Civilia - [TUTTO] -> usa tutto dopo il primo "-"
            if (applicativo.StartsWith("Civilia - "))
            {
                // "Civilia - GeoNext -> API PDND" → "GeoNext -> API PDND" 
                return applicativo.Substring("Civilia - ".Length);
            }

            // Per tutti gli altri: usa il metodo esistente (tutto dopo "->")
            return ExtractAppName(applicativo);
        }
    }
}