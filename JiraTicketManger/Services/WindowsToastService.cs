using System;
using System.Windows.Forms;
using JiraTicketManager.Services;

namespace JiraTicketManager.Services
{
    public class WindowsToastService
    {
        private readonly LoggingService _logger;
        private readonly string _appName;

        public WindowsToastService(string appName = "Jira Ticket Manager")
        {
            _appName = appName;
            _logger = LoggingService.CreateForComponent("ToastService");
        }

        public void ShowSuccess(string title, string message)
        {
            try
            {
                _logger.LogInfo($"Toast Success: {title} - {message}");
                ShowWindowsToast(title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowSuccess", ex);
            }
        }

        public void ShowError(string title, string message)
        {
            try
            {
                _logger.LogInfo($"Toast Error: {title} - {message}");
                ShowWindowsToast(title, message, ToolTipIcon.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowError", ex);
            }
        }

        public void ShowInfo(string title, string message)
        {
            try
            {
                _logger.LogInfo($"Toast Info: {title} - {message}");
                ShowWindowsToast(title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowInfo", ex);
            }
        }

        public void ShowWarning(string title, string message)
        {
            try
            {
                _logger.LogInfo($"Toast Warning: {title} - {message}");
                ShowWindowsToast(title, message, ToolTipIcon.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowWarning", ex);
            }
        }

        public void ShowAuthenticationSuccess(string userEmail)
        {
            ShowSuccess(
                "🎉 Accesso Autorizzato",
                $"Benvenuto {userEmail}\nAutenticazione Microsoft SSO completata"
            );
        }

        public void ShowAuthenticationError(string reason)
        {
            ShowError(
                "❌ Accesso Negato",
                $"Autenticazione fallita\n{reason}"
            );
        }

        public void ShowProgress(string title, string message)
        {
            try
            {
                _logger.LogInfo($"Toast Progress: {title} - {message}");
                ShowWindowsToast(title, message, ToolTipIcon.Info);
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowProgress", ex);
            }
        }

        private void ShowWindowsToast(string title, string message, ToolTipIcon icon)
        {
            try
            {
                // Crea NotifyIcon che si auto-pulisce
                var notifyIcon = new NotifyIcon();
                notifyIcon.Icon = SystemIcons.Application;
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = message;
                notifyIcon.BalloonTipIcon = icon;
                notifyIcon.Visible = true;

                // Evento per cleanup quando balloon tip si chiude
                notifyIcon.BalloonTipClosed += (s, e) =>
                {
                    try
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Dispose();
                    }
                    catch { }
                };

                // Timer di sicurezza per cleanup dopo 6 secondi
                var timer = new System.Windows.Forms.Timer();
                timer.Interval = 6000;
                timer.Tick += (s, e) =>
                {
                    try
                    {
                        timer.Stop();
                        timer.Dispose();
                        if (notifyIcon != null)
                        {
                            notifyIcon.Visible = false;
                            notifyIcon.Dispose();
                        }
                    }
                    catch { }
                };
                timer.Start();

                // Mostra il balloon tip
                notifyIcon.ShowBalloonTip(5000);

                _logger.LogInfo($"Toast mostrato: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError("ShowWindowsToast", ex);

                // Fallback a MessageBox se toast fallisce
                MessageBox.Show($"{title}\n\n{message}", _appName, MessageBoxButtons.OK,
                    icon == ToolTipIcon.Error ? MessageBoxIcon.Error : MessageBoxIcon.Information);
            }
        }

        public static WindowsToastService CreateDefault()
        {
            return new WindowsToastService();
        }
    }
}