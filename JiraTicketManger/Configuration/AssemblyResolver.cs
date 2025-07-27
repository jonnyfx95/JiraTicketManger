using System;
using System.IO;
using System.Reflection;

namespace JiraTicketManager.Config
{
    /// <summary>
    /// AssemblyResolver per caricare DLL dalla cartella libs/
    /// TRADOTTO IDENTICAMENTE dal VB.NET
    /// </summary>
    public class AssemblyResolver
    {
        /// <summary>
        /// Inizializza il resolver per le assembly - DEVE essere chiamato per primo in Main()
        /// </summary>
        public static void Initialize()
        {
            // Registra l'evento AssemblyResolve per gestire il caricamento delle assembly
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        /// <summary>
        /// Evento che risolve il caricamento delle assembly dalla cartella libs/
        /// </summary>
        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            try
            {
                // Ottiene il nome dell'assembly senza la versione, cultura, ecc.
                var assemblyName = new AssemblyName(args.Name);
                string dllName = assemblyName.Name + ".dll";

                // Percorso della sottocartella libs
                string libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs");

                // Percorso completo del file DLL nella sottocartella libs
                string dllPath = Path.Combine(libsPath, dllName);

                // Se il file esiste, caricalo
                if (File.Exists(dllPath))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ AssemblyResolver: Caricato {dllName} da libs/");
                    return Assembly.LoadFrom(dllPath);
                }

                // Debug per assembly non trovate
                System.Diagnostics.Debug.WriteLine($"❌ AssemblyResolver: {dllName} non trovato in libs/");

                // Altrimenti, restituisci null per consentire al runtime di continuare la ricerca
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AssemblyResolver ERRORE: {ex.Message}");
                return null;
            }
        }
    }
}