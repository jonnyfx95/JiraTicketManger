using System;
using System.Collections.Generic;

namespace JiraTicketManager.Models
{
    /// <summary>
    /// Configurazione per il sistema di invio email automatico
    /// </summary>
    public class EmailConfiguration
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderPassword_Encrypted { get; set; } = "";
        public string SenderDisplayName { get; set; } = "Jira Manager System";

        public List<string> AdminEmails { get; set; } = new List<string>();

        public bool EnableWeeklyReports { get; set; } = true;
        public DayOfWeek WeeklyReportDay { get; set; } = DayOfWeek.Monday;
        public TimeSpan WeeklyReportTime { get; set; } = new TimeSpan(8, 0, 0); // 08:00

        public bool EnableCriticalAlerts { get; set; } = true;
        public int CriticalFailedLoginsThreshold { get; set; } = 3;
        public TimeSpan AlertCooldownPeriod { get; set; } = TimeSpan.FromHours(2);

        public DateTime LastWeeklyReportSent { get; set; } = DateTime.MinValue;
        public DateTime LastCriticalAlertSent { get; set; } = DateTime.MinValue;
    }

    /// <summary>
    /// Estende AppSettings per includere configurazione email
    /// </summary>
    public partial class ApplicationSettings
    {
        // ✅ AGGIUNGI questa proprietà alla tua classe ApplicationSettings esistente
        public EmailConfiguration EmailConfiguration { get; set; } = new EmailConfiguration();
    }
}