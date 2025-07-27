using JiraTicketManager.Forms;
using JiraTicketManager.Services;
using JiraTicketManager.Config; // ← NUOVO: AssemblyResolver
using System;
using System.Windows.Forms;

namespace JiraTicketManager
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 🚨 CRITICO: Inizializza il resolver per le assembly - DEVE essere la prima operazione
            // IDENTICO AL VB.NET: AssemblyResolver.Initialize()
            AssemblyResolver.Initialize();

            // 🎨 Configurazione applicazione per high DPI (come VB.NET)
            Application.EnableVisualStyles();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetCompatibleTextRenderingDefault(false);

            // 🛡️ Gestione eccezioni non gestite (opzionale, come VB.NET)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            try
            {
                // 📝 Debug log di avvio (come VB.NET)
                CreateDebugLog("=== AVVIO APPLICAZIONE C# ===");
                CreateDebugLog("AssemblyResolver inizializzato");

                // CONTROLLO CREDENZIALI: Verifica se abbiamo credenziali valide nel settings.json
                var settingsService = SettingsService.CreateDefault();

                bool hasValidCredentials = settingsService.IsConfigurationValid();

                // Debug per capire lo stato
                string configStatus = settingsService.GetConfigurationStatus();
                System.Diagnostics.Debug.WriteLine($"Status: {configStatus}");
                System.Diagnostics.Debug.WriteLine($"Valido: {hasValidCredentials}");

                if (hasValidCredentials)
                {
                    // Se abbiamo credenziali valide, avvia direttamente MainForm
                    System.Diagnostics.Debug.WriteLine("✅ Credenziali valide - Avvio MainForm");
                    CreateDebugLog("Credenziali valide - Avvio MainForm");
                    Application.Run(new MainForm());
                }
                else
                {
                    // Se non abbiamo credenziali valide, avvia FrmCredentials
                    System.Diagnostics.Debug.WriteLine("❌ Credenziali non valide - Avvio FrmCredentials");
                    CreateDebugLog("Credenziali non valide - Avvio FrmCredentials");

                    var credentialsForm = new FrmCredentials();
                    var result = credentialsForm.ShowDialog();

                    if (result == DialogResult.OK)
                    {
                        // Dopo il salvataggio credenziali, avvia MainForm
                        System.Diagnostics.Debug.WriteLine("✅ Credenziali salvate - Avvio MainForm");
                        CreateDebugLog("Credenziali salvate - Avvio MainForm");
                        Application.Run(new MainForm());
                    }
                    // Se l'utente chiude FrmCredentials, l'app si chiude
                }
            }
            catch (Exception ex)
            {
                CreateDebugLog($"ERRORE FATALE: {ex.Message}");
                MessageBox.Show($"Errore durante l'avvio: {ex.Message}",
                              "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Crea log di debug come nel VB.NET
        /// </summary>
        private static void CreateDebugLog(string message)
        {
            try
            {
                string logFile = Path.Combine(Application.StartupPath, "Debug.txt");
                using (var writer = new StreamWriter(logFile, true))
                {
                    writer.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
                }
            }
            catch
            {
                // Ignora errori di scrittura
            }
        }

        /// <summary>
        /// Gestione eccezioni thread (come VB.NET)
        /// </summary>
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            CreateDebugLog($"Thread Exception: {e.Exception.Message}");

            var result = MessageBox.Show($"Si è verificato un errore:\r\n\r\n{e.Exception.Message}\r\n\r\nVuoi continuare?",
                                       "Errore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// Gestione eccezioni non gestite (come VB.NET)
        /// </summary>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                CreateDebugLog($"Unhandled Exception: {ex.Message}");
                MessageBox.Show($"Errore fatale:\r\n\r\n{ex.Message}",
                              "Errore Fatale", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}