using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    partial class AutomationForm
    {
        private System.ComponentModel.IContainer components = null;

        #region Control Declarations

        // Main Layout
        private Panel pnlHeader;
        private Panel pnlMain;
        private Panel pnlFooter;

        // Header Controls
        private Label lblTitle;
        private Label lblSubtitle;
        private PictureBox picIcon;

        // Main Content Panels
        private Panel pnlControls;
        private Panel pnlLog;

        // Control Panel Elements
        private GroupBox grpStatus;
        private GroupBox grpActions;
        private GroupBox grpCounters;

        // Status Indicators
        private Label lblStatusTitle;
        private Label lblStatusValue;
        private PictureBox picStatus;

        // Action Buttons
        private Button btnStartAutomation;
        private Button btnStopAutomation;
        private Button btnClearLog;
        private Button btnSaveLog;

        // Progress and Counters
        private ProgressBar progressMain;
        private Label lblProgress;

        // Counters
        private Label lblFoundTitle;
        private Label lblFoundValue;
        private Label lblProcessedTitle;
        private Label lblProcessedValue;
        private Label lblSuccessTitle;
        private Label lblSuccessValue;
        private Label lblErrorsTitle;
        private Label lblErrorsValue;

        // Log Console
        private RichTextBox txtConsoleLog;
        private Label lblLogTitle;

        // Footer
        private Label lblFooterInfo;

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlHeader = new Panel();
            this.lblTitle = new Label();
            this.lblSubtitle = new Label();
            this.picIcon = new PictureBox();
            this.pnlMain = new Panel();
            this.pnlControls = new Panel();
            this.grpStatus = new GroupBox();
            this.lblStatusTitle = new Label();
            this.lblStatusValue = new Label();
            this.picStatus = new PictureBox();
            this.grpActions = new GroupBox();
            this.btnStartAutomation = new Button();
            this.btnStopAutomation = new Button();
            this.btnClearLog = new Button();
            this.btnSaveLog = new Button();
            this.grpCounters = new GroupBox();
            this.lblFoundTitle = new Label();
            this.lblFoundValue = new Label();
            this.lblProcessedTitle = new Label();
            this.lblProcessedValue = new Label();
            this.lblSuccessTitle = new Label();
            this.lblSuccessValue = new Label();
            this.lblErrorsTitle = new Label();
            this.lblErrorsValue = new Label();
            this.progressMain = new ProgressBar();
            this.lblProgress = new Label();
            this.pnlLog = new Panel();
            this.lblLogTitle = new Label();
            this.txtConsoleLog = new RichTextBox();
            this.pnlFooter = new Panel();
            this.lblFooterInfo = new Label();

            this.pnlHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).BeginInit();
            this.pnlMain.SuspendLayout();
            this.pnlControls.SuspendLayout();
            this.grpStatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).BeginInit();
            this.grpActions.SuspendLayout();
            this.grpCounters.SuspendLayout();
            this.pnlLog.SuspendLayout();
            this.pnlFooter.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = Color.FromArgb(248, 249, 250);
            this.pnlHeader.BorderStyle = BorderStyle.FixedSingle;
            this.pnlHeader.Controls.Add(this.picIcon);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Location = new Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new Size(1200, 70);
            this.pnlHeader.TabIndex = 0;

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(52, 73, 94);
            this.lblTitle.Location = new Point(60, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(280, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "🤖 Automazione Jira";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new Font("Segoe UI", 10F);
            this.lblSubtitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblSubtitle.Location = new Point(60, 45);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new Size(420, 19);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Sistema automatico per processamento ticket Area Demografia";

            // 
            // picIcon
            // 
            this.picIcon.BackColor = Color.FromArgb(124, 58, 237);
            this.picIcon.Location = new Point(15, 15);
            this.picIcon.Name = "picIcon";
            this.picIcon.Size = new Size(40, 40);
            this.picIcon.TabIndex = 2;
            this.picIcon.TabStop = false;

            // 
            // pnlMain
            // 
            this.pnlMain.BackColor = Color.White;
            this.pnlMain.Controls.Add(this.pnlLog);
            this.pnlMain.Controls.Add(this.pnlControls);
            this.pnlMain.Dock = DockStyle.Fill;
            this.pnlMain.Location = new Point(0, 70);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Padding = new Padding(10);
            this.pnlMain.Size = new Size(1200, 580);
            this.pnlMain.TabIndex = 1;

            // 
            // pnlControls
            // 
            this.pnlControls.BackColor = Color.FromArgb(248, 249, 250);
            this.pnlControls.BorderStyle = BorderStyle.FixedSingle;
            this.pnlControls.Controls.Add(this.lblProgress);
            this.pnlControls.Controls.Add(this.progressMain);
            this.pnlControls.Controls.Add(this.grpCounters);
            this.pnlControls.Controls.Add(this.grpActions);
            this.pnlControls.Controls.Add(this.grpStatus);
            this.pnlControls.Dock = DockStyle.Top;
            this.pnlControls.Location = new Point(10, 10);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Padding = new Padding(15);
            this.pnlControls.Size = new Size(1180, 200);
            this.pnlControls.TabIndex = 0;

            // 
            // grpStatus
            // 
            this.grpStatus.Controls.Add(this.picStatus);
            this.grpStatus.Controls.Add(this.lblStatusValue);
            this.grpStatus.Controls.Add(this.lblStatusTitle);
            this.grpStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpStatus.ForeColor = Color.FromArgb(52, 73, 94);
            this.grpStatus.Location = new Point(20, 20);
            this.grpStatus.Name = "grpStatus";
            this.grpStatus.Size = new Size(200, 80);
            this.grpStatus.TabIndex = 0;
            this.grpStatus.TabStop = false;
            this.grpStatus.Text = "📊 Stato Sistema";

            // 
            // lblStatusTitle
            // 
            this.lblStatusTitle.AutoSize = true;
            this.lblStatusTitle.Font = new Font("Segoe UI", 8F);
            this.lblStatusTitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblStatusTitle.Location = new Point(10, 25);
            this.lblStatusTitle.Name = "lblStatusTitle";
            this.lblStatusTitle.Size = new Size(35, 13);
            this.lblStatusTitle.TabIndex = 0;
            this.lblStatusTitle.Text = "Stato:";

            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblStatusValue.ForeColor = Color.FromArgb(40, 167, 69);
            this.lblStatusValue.Location = new Point(10, 45);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new Size(49, 19);
            this.lblStatusValue.TabIndex = 1;
            this.lblStatusValue.Text = "Pronto";

            // 
            // picStatus
            // 
            this.picStatus.BackColor = Color.FromArgb(40, 167, 69);
            this.picStatus.Location = new Point(160, 40);
            this.picStatus.Name = "picStatus";
            this.picStatus.Size = new Size(20, 20);
            this.picStatus.TabIndex = 2;
            this.picStatus.TabStop = false;

            // 
            // grpActions
            // 
            this.grpActions.Controls.Add(this.btnSaveLog);
            this.grpActions.Controls.Add(this.btnClearLog);
            this.grpActions.Controls.Add(this.btnStopAutomation);
            this.grpActions.Controls.Add(this.btnStartAutomation);
            this.grpActions.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpActions.ForeColor = Color.FromArgb(52, 73, 94);
            this.grpActions.Location = new Point(240, 20);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new Size(420, 80);
            this.grpActions.TabIndex = 1;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "⚡ Controlli";

            // 
            // btnStartAutomation
            // 
            this.btnStartAutomation.BackColor = Color.FromArgb(40, 167, 69);
            this.btnStartAutomation.FlatAppearance.BorderSize = 0;
            this.btnStartAutomation.FlatStyle = FlatStyle.Flat;
            this.btnStartAutomation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStartAutomation.ForeColor = Color.White;
            this.btnStartAutomation.Location = new Point(15, 25);
            this.btnStartAutomation.Name = "btnStartAutomation";
            this.btnStartAutomation.Size = new Size(120, 35);
            this.btnStartAutomation.TabIndex = 0;
            this.btnStartAutomation.Text = "▶️ Avvia";
            this.btnStartAutomation.UseVisualStyleBackColor = false;

            // 
            // btnStopAutomation
            // 
            this.btnStopAutomation.BackColor = Color.FromArgb(220, 53, 69);
            this.btnStopAutomation.Enabled = false;
            this.btnStopAutomation.FlatAppearance.BorderSize = 0;
            this.btnStopAutomation.FlatStyle = FlatStyle.Flat;
            this.btnStopAutomation.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnStopAutomation.ForeColor = Color.White;
            this.btnStopAutomation.Location = new Point(145, 25);
            this.btnStopAutomation.Name = "btnStopAutomation";
            this.btnStopAutomation.Size = new Size(120, 35);
            this.btnStopAutomation.TabIndex = 1;
            this.btnStopAutomation.Text = "⏹️ Stop";
            this.btnStopAutomation.UseVisualStyleBackColor = false;

            // 
            // btnClearLog
            // 
            this.btnClearLog.BackColor = Color.FromArgb(108, 117, 125);
            this.btnClearLog.FlatAppearance.BorderSize = 0;
            this.btnClearLog.FlatStyle = FlatStyle.Flat;
            this.btnClearLog.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.btnClearLog.ForeColor = Color.White;
            this.btnClearLog.Location = new Point(275, 25);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new Size(60, 35);
            this.btnClearLog.TabIndex = 2;
            this.btnClearLog.Text = "🗑️ Clear";
            this.btnClearLog.UseVisualStyleBackColor = false;

            // 
            // btnSaveLog
            // 
            this.btnSaveLog.BackColor = Color.FromArgb(8, 145, 178);
            this.btnSaveLog.FlatAppearance.BorderSize = 0;
            this.btnSaveLog.FlatStyle = FlatStyle.Flat;
            this.btnSaveLog.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            this.btnSaveLog.ForeColor = Color.White;
            this.btnSaveLog.Location = new Point(345, 25);
            this.btnSaveLog.Name = "btnSaveLog";
            this.btnSaveLog.Size = new Size(60, 35);
            this.btnSaveLog.TabIndex = 3;
            this.btnSaveLog.Text = "💾 Save";
            this.btnSaveLog.UseVisualStyleBackColor = false;

            // 
            // grpCounters
            // 
            this.grpCounters.Controls.Add(this.lblErrorsValue);
            this.grpCounters.Controls.Add(this.lblErrorsTitle);
            this.grpCounters.Controls.Add(this.lblSuccessValue);
            this.grpCounters.Controls.Add(this.lblSuccessTitle);
            this.grpCounters.Controls.Add(this.lblProcessedValue);
            this.grpCounters.Controls.Add(this.lblProcessedTitle);
            this.grpCounters.Controls.Add(this.lblFoundValue);
            this.grpCounters.Controls.Add(this.lblFoundTitle);
            this.grpCounters.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.grpCounters.ForeColor = Color.FromArgb(52, 73, 94);
            this.grpCounters.Location = new Point(680, 20);
            this.grpCounters.Name = "grpCounters";
            this.grpCounters.Size = new Size(480, 80);
            this.grpCounters.TabIndex = 2;
            this.grpCounters.TabStop = false;
            this.grpCounters.Text = "📈 Statistiche";

            // 
            // lblFoundTitle
            // 
            this.lblFoundTitle.AutoSize = true;
            this.lblFoundTitle.Font = new Font("Segoe UI", 8F);
            this.lblFoundTitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblFoundTitle.Location = new Point(15, 25);
            this.lblFoundTitle.Name = "lblFoundTitle";
            this.lblFoundTitle.Size = new Size(46, 13);
            this.lblFoundTitle.TabIndex = 0;
            this.lblFoundTitle.Text = "Trovati:";

            // 
            // lblFoundValue
            // 
            this.lblFoundValue.AutoSize = true;
            this.lblFoundValue.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblFoundValue.ForeColor = Color.FromArgb(8, 145, 178);
            this.lblFoundValue.Location = new Point(15, 40);
            this.lblFoundValue.Name = "lblFoundValue";
            this.lblFoundValue.Size = new Size(19, 21);
            this.lblFoundValue.TabIndex = 1;
            this.lblFoundValue.Text = "0";

            // 
            // lblProcessedTitle
            // 
            this.lblProcessedTitle.AutoSize = true;
            this.lblProcessedTitle.Font = new Font("Segoe UI", 8F);
            this.lblProcessedTitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblProcessedTitle.Location = new Point(135, 25);
            this.lblProcessedTitle.Name = "lblProcessedTitle";
            this.lblProcessedTitle.Size = new Size(62, 13);
            this.lblProcessedTitle.TabIndex = 2;
            this.lblProcessedTitle.Text = "Processati:";

            // 
            // lblProcessedValue
            // 
            this.lblProcessedValue.AutoSize = true;
            this.lblProcessedValue.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblProcessedValue.ForeColor = Color.FromArgb(255, 193, 7);
            this.lblProcessedValue.Location = new Point(135, 40);
            this.lblProcessedValue.Name = "lblProcessedValue";
            this.lblProcessedValue.Size = new Size(19, 21);
            this.lblProcessedValue.TabIndex = 3;
            this.lblProcessedValue.Text = "0";

            // 
            // lblSuccessTitle
            // 
            this.lblSuccessTitle.AutoSize = true;
            this.lblSuccessTitle.Font = new Font("Segoe UI", 8F);
            this.lblSuccessTitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblSuccessTitle.Location = new Point(265, 25);
            this.lblSuccessTitle.Name = "lblSuccessTitle";
            this.lblSuccessTitle.Size = new Size(50, 13);
            this.lblSuccessTitle.TabIndex = 4;
            this.lblSuccessTitle.Text = "Successi:";

            // 
            // lblSuccessValue
            // 
            this.lblSuccessValue.AutoSize = true;
            this.lblSuccessValue.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblSuccessValue.ForeColor = Color.FromArgb(40, 167, 69);
            this.lblSuccessValue.Location = new Point(265, 40);
            this.lblSuccessValue.Name = "lblSuccessValue";
            this.lblSuccessValue.Size = new Size(19, 21);
            this.lblSuccessValue.TabIndex = 5;
            this.lblSuccessValue.Text = "0";

            // 
            // lblErrorsTitle
            // 
            this.lblErrorsTitle.AutoSize = true;
            this.lblErrorsTitle.Font = new Font("Segoe UI", 8F);
            this.lblErrorsTitle.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblErrorsTitle.Location = new Point(385, 25);
            this.lblErrorsTitle.Name = "lblErrorsTitle";
            this.lblErrorsTitle.Size = new Size(35, 13);
            this.lblErrorsTitle.TabIndex = 6;
            this.lblErrorsTitle.Text = "Errori:";

            // 
            // lblErrorsValue
            // 
            this.lblErrorsValue.AutoSize = true;
            this.lblErrorsValue.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            this.lblErrorsValue.ForeColor = Color.FromArgb(220, 53, 69);
            this.lblErrorsValue.Location = new Point(385, 40);
            this.lblErrorsValue.Name = "lblErrorsValue";
            this.lblErrorsValue.Size = new Size(19, 21);
            this.lblErrorsValue.TabIndex = 7;
            this.lblErrorsValue.Text = "0";

            // 
            // progressMain
            // 
            this.progressMain.Location = new Point(20, 115);
            this.progressMain.Name = "progressMain";
            this.progressMain.Size = new Size(1140, 20);
            this.progressMain.Style = ProgressBarStyle.Continuous;
            this.progressMain.TabIndex = 3;

            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Font = new Font("Segoe UI", 8F);
            this.lblProgress.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblProgress.Location = new Point(20, 145);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new Size(180, 13);
            this.lblProgress.TabIndex = 4;
            this.lblProgress.Text = "In attesa di avviare l'automazione...";

            // 
            // pnlLog
            // 
            this.pnlLog.BackColor = Color.White;
            this.pnlLog.BorderStyle = BorderStyle.FixedSingle;
            this.pnlLog.Controls.Add(this.txtConsoleLog);
            this.pnlLog.Controls.Add(this.lblLogTitle);
            this.pnlLog.Dock = DockStyle.Fill;
            this.pnlLog.Location = new Point(10, 210);
            this.pnlLog.Name = "pnlLog";
            this.pnlLog.Padding = new Padding(15);
            this.pnlLog.Size = new Size(1180, 360);
            this.pnlLog.TabIndex = 1;

            // 
            // lblLogTitle
            // 
            this.lblLogTitle.AutoSize = true;
            this.lblLogTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            this.lblLogTitle.ForeColor = Color.FromArgb(52, 73, 94);
            this.lblLogTitle.Location = new Point(15, 15);
            this.lblLogTitle.Name = "lblLogTitle";
            this.lblLogTitle.Size = new Size(160, 19);
            this.lblLogTitle.TabIndex = 0;
            this.lblLogTitle.Text = "📝 Console di Sistema";

            // 
            // txtConsoleLog
            // 
            this.txtConsoleLog.BackColor = Color.FromArgb(33, 37, 41);
            this.txtConsoleLog.BorderStyle = BorderStyle.None;
            this.txtConsoleLog.Font = new Font("Consolas", 9F);
            this.txtConsoleLog.ForeColor = Color.FromArgb(248, 249, 250);
            this.txtConsoleLog.Location = new Point(15, 45);
            this.txtConsoleLog.Name = "txtConsoleLog";
            this.txtConsoleLog.ReadOnly = true;
            this.txtConsoleLog.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.txtConsoleLog.Size = new Size(1150, 300);
            this.txtConsoleLog.TabIndex = 1;
            this.txtConsoleLog.Text = "[Sistema] Automazione Jira - Pronta per l'avvio\n[Info] Configurazione caricata: Area Demografia\n[Info] In attesa di comandi...";

            // 
            // pnlFooter
            // 
            this.pnlFooter.BackColor = Color.FromArgb(248, 249, 250);
            this.pnlFooter.BorderStyle = BorderStyle.FixedSingle;
            this.pnlFooter.Controls.Add(this.lblFooterInfo);
            this.pnlFooter.Dock = DockStyle.Bottom;
            this.pnlFooter.Location = new Point(0, 650);
            this.pnlFooter.Name = "pnlFooter";
            this.pnlFooter.Size = new Size(1200, 30);
            this.pnlFooter.TabIndex = 2;

            // 
            // lblFooterInfo
            // 
            this.lblFooterInfo.AutoSize = true;
            this.lblFooterInfo.Font = new Font("Segoe UI", 8F);
            this.lblFooterInfo.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblFooterInfo.Location = new Point(15, 8);
            this.lblFooterInfo.Name = "lblFooterInfo";
            this.lblFooterInfo.Size = new Size(350, 13);
            this.lblFooterInfo.TabIndex = 0;
            this.lblFooterInfo.Text = "🕒 Ultima esecuzione: Mai eseguita | 📂 Log salvati in: logs/";

            // 
            // AutomationForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(1200, 680);
            this.Controls.Add(this.pnlMain);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlFooter);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(1000, 600);
            this.Name = "AutomationForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "🤖 Automazione Jira - Sistema Demografia";

            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picIcon)).EndInit();
            this.pnlMain.ResumeLayout(false);
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.grpStatus.ResumeLayout(false);
            this.grpStatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStatus)).EndInit();
            this.grpActions.ResumeLayout(false);
            this.grpCounters.ResumeLayout(false);
            this.grpCounters.PerformLayout();
            this.pnlLog.ResumeLayout(false);
            this.pnlLog.PerformLayout();
            this.pnlFooter.ResumeLayout(false);
            this.pnlFooter.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}