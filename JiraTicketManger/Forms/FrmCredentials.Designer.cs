namespace JiraTicketManager.Forms
{
    partial class FrmCredentials
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlMain = new Panel();
            pnlHeader = new Panel();
            lblTitle = new Label();
            lblSubtitle = new Label();
            pnlAuthModeSelector = new Panel();
            lblAuthMode = new Label();
            rbJiraApi = new RadioButton();
            rbMicrosoftSSO = new RadioButton();
            pnlDivider = new Panel();
            pnlJiraAuth = new Panel();
            cardJiraCredentials = new Panel();
            pnlJiraFields = new Panel();
            pnlServerField = new Panel();
            txtServer = new TextBox();
            lblServer = new Label();
            pnlUsernameField = new Panel();
            txtUsername = new TextBox();
            lblUsername = new Label();
            pnlTokenField = new Panel();
            txtToken = new TextBox();
            lblToken = new Label();
            lblJiraHint = new Label();
            pnlMicrosoftAuth = new Panel();
            cardMicrosoftSSO = new Panel();
            lblMicrosoftTitle = new Label();
            lblMicrosoftInfo = new Label();
            btnMicrosoftLogin = new Button();
            lblDomainRestriction = new Label();
            pnlActions = new Panel();
            btnTest = new Button();
            btnSave = new Button();
            btnCancel = new Button();
            pnlFooter = new Panel();
            lblStatus = new Label();
            pnlMain.SuspendLayout();
            pnlHeader.SuspendLayout();
            pnlAuthModeSelector.SuspendLayout();
            pnlJiraAuth.SuspendLayout();
            cardJiraCredentials.SuspendLayout();
            pnlJiraFields.SuspendLayout();
            pnlServerField.SuspendLayout();
            pnlUsernameField.SuspendLayout();
            pnlTokenField.SuspendLayout();
            pnlMicrosoftAuth.SuspendLayout();
            cardMicrosoftSSO.SuspendLayout();
            pnlActions.SuspendLayout();
            pnlFooter.SuspendLayout();
            SuspendLayout();
            // 
            // pnlMain
            // 
            pnlMain.BackColor = Color.White;
            pnlMain.Controls.Add(pnlHeader);
            pnlMain.Controls.Add(pnlAuthModeSelector);
            pnlMain.Controls.Add(pnlDivider);
            pnlMain.Controls.Add(pnlJiraAuth);
            pnlMain.Controls.Add(pnlMicrosoftAuth);
            pnlMain.Location = new Point(0, 0);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(520, 440);
            pnlMain.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(247, 249, 252);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 91);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Padding = new Padding(30, 25, 30, 20);
            pnlHeader.Size = new Size(520, 85);
            pnlHeader.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(30, 41, 59);
            lblTitle.Location = new Point(30, 45);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(205, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "🔐 Autenticazione";
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Dock = DockStyle.Top;
            lblSubtitle.Font = new Font("Segoe UI", 9F);
            lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitle.Location = new Point(30, 25);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Padding = new Padding(0, 5, 0, 0);
            lblSubtitle.Size = new Size(271, 20);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Scegli la modalità di accesso a Jira Ticket Manager";
            // 
            // pnlAuthModeSelector
            // 
            pnlAuthModeSelector.BackColor = Color.White;
            pnlAuthModeSelector.Controls.Add(lblAuthMode);
            pnlAuthModeSelector.Controls.Add(rbJiraApi);
            pnlAuthModeSelector.Controls.Add(rbMicrosoftSSO);
            pnlAuthModeSelector.Dock = DockStyle.Top;
            pnlAuthModeSelector.Location = new Point(0, 1);
            pnlAuthModeSelector.Name = "pnlAuthModeSelector";
            pnlAuthModeSelector.Padding = new Padding(30, 20, 30, 15);
            pnlAuthModeSelector.Size = new Size(520, 90);
            pnlAuthModeSelector.TabIndex = 1;
            // 
            // lblAuthMode
            // 
            lblAuthMode.AutoSize = true;
            lblAuthMode.Dock = DockStyle.Top;
            lblAuthMode.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAuthMode.ForeColor = Color.FromArgb(51, 65, 85);
            lblAuthMode.Location = new Point(30, 20);
            lblAuthMode.Name = "lblAuthMode";
            lblAuthMode.Padding = new Padding(0, 0, 0, 10);
            lblAuthMode.Size = new Size(141, 29);
            lblAuthMode.TabIndex = 0;
            lblAuthMode.Text = "Modalità di accesso";
            // 
            // rbJiraApi
            // 
            rbJiraApi.AutoSize = true;
            rbJiraApi.Cursor = Cursors.Hand;
            rbJiraApi.Font = new Font("Segoe UI", 9.5F);
            rbJiraApi.ForeColor = Color.FromArgb(51, 65, 85);
            rbJiraApi.Location = new Point(273, 52);
            rbJiraApi.Name = "rbJiraApi";
            rbJiraApi.Size = new Size(187, 21);
            rbJiraApi.TabIndex = 1;
            rbJiraApi.Text = "🔑 Jira API (Amministratori)";
            rbJiraApi.UseVisualStyleBackColor = true;
            // 
            // rbMicrosoftSSO
            // 
            rbMicrosoftSSO.AutoSize = true;
            rbMicrosoftSSO.Checked = true;
            rbMicrosoftSSO.Cursor = Cursors.Hand;
            rbMicrosoftSSO.Font = new Font("Segoe UI", 9.5F);
            rbMicrosoftSSO.ForeColor = Color.FromArgb(51, 65, 85);
            rbMicrosoftSSO.Location = new Point(34, 52);
            rbMicrosoftSSO.Name = "rbMicrosoftSSO";
            rbMicrosoftSSO.Size = new Size(222, 21);
            rbMicrosoftSSO.TabIndex = 2;
            rbMicrosoftSSO.TabStop = true;
            rbMicrosoftSSO.Text = "🏢 Microsoft SSO (@dedagroup)";
            rbMicrosoftSSO.UseVisualStyleBackColor = true;
            // 
            // pnlDivider
            // 
            pnlDivider.BackColor = Color.FromArgb(226, 232, 240);
            pnlDivider.Dock = DockStyle.Top;
            pnlDivider.Location = new Point(0, 0);
            pnlDivider.Name = "pnlDivider";
            pnlDivider.Size = new Size(520, 1);
            pnlDivider.TabIndex = 2;
            // 
            // pnlJiraAuth
            // 
            pnlJiraAuth.BackColor = Color.FromArgb(249, 250, 251);
            pnlJiraAuth.Controls.Add(cardJiraCredentials);
            pnlJiraAuth.Location = new Point(0, 176);
            pnlJiraAuth.Name = "pnlJiraAuth";
            pnlJiraAuth.Padding = new Padding(30, 20, 30, 20);
            pnlJiraAuth.Size = new Size(520, 260);
            pnlJiraAuth.TabIndex = 3;
            // 
            // cardJiraCredentials
            // 
            cardJiraCredentials.BackColor = Color.FromArgb(249, 250, 251);
            cardJiraCredentials.Controls.Add(pnlJiraFields);
            cardJiraCredentials.Controls.Add(lblJiraHint);
            cardJiraCredentials.Dock = DockStyle.Fill;
            cardJiraCredentials.Location = new Point(30, 20);
            cardJiraCredentials.Name = "cardJiraCredentials";
            cardJiraCredentials.Padding = new Padding(4);
            cardJiraCredentials.Size = new Size(460, 220);
            cardJiraCredentials.TabIndex = 0;
            // 
            // pnlJiraFields
            // 
            pnlJiraFields.BackColor = Color.FromArgb(249, 250, 251);
            pnlJiraFields.Controls.Add(pnlServerField);
            pnlJiraFields.Controls.Add(pnlUsernameField);
            pnlJiraFields.Controls.Add(pnlTokenField);
            pnlJiraFields.Location = new Point(4, 35);
            pnlJiraFields.Name = "pnlJiraFields";
            pnlJiraFields.Padding = new Padding(20, 15, 20, 20);
            pnlJiraFields.Size = new Size(452, 185);
            pnlJiraFields.TabIndex = 1;
            // 
            // pnlServerField
            // 
            pnlServerField.BackColor = Color.FromArgb(249, 250, 251);
            pnlServerField.Controls.Add(txtServer);
            pnlServerField.Controls.Add(lblServer);
            pnlServerField.Location = new Point(8, 15);
            pnlServerField.Name = "pnlServerField";
            pnlServerField.Size = new Size(436, 54);
            pnlServerField.TabIndex = 0;
            // 
            // txtServer
            // 
            txtServer.BorderStyle = BorderStyle.FixedSingle;
            txtServer.Font = new Font("Segoe UI", 11F);
            txtServer.Location = new Point(0, 20);
            txtServer.Name = "txtServer";
            txtServer.PlaceholderText = "https://your-company.atlassian.net";
            txtServer.Size = new Size(433, 27);
            txtServer.TabIndex = 1;
            // 
            // lblServer
            // 
            lblServer.AutoSize = true;
            lblServer.Dock = DockStyle.Top;
            lblServer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblServer.ForeColor = Color.FromArgb(50, 50, 50);
            lblServer.Location = new Point(0, 0);
            lblServer.Name = "lblServer";
            lblServer.Padding = new Padding(0, 0, 0, 5);
            lblServer.Size = new Size(71, 20);
            lblServer.TabIndex = 0;
            lblServer.Text = "Server URL";
            // 
            // pnlUsernameField
            // 
            pnlUsernameField.BackColor = Color.FromArgb(249, 250, 251);
            pnlUsernameField.Controls.Add(txtUsername);
            pnlUsernameField.Controls.Add(lblUsername);
            pnlUsernameField.Location = new Point(8, 75);
            pnlUsernameField.Name = "pnlUsernameField";
            pnlUsernameField.Size = new Size(436, 54);
            pnlUsernameField.TabIndex = 1;
            // 
            // txtUsername
            // 
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Font = new Font("Segoe UI", 11F);
            txtUsername.Location = new Point(0, 20);
            txtUsername.Name = "txtUsername";
            txtUsername.PlaceholderText = "mario.rossi@dedagroup.com";
            txtUsername.Size = new Size(433, 27);
            txtUsername.TabIndex = 1;
            // 
            // lblUsername
            // 
            lblUsername.AutoSize = true;
            lblUsername.Dock = DockStyle.Top;
            lblUsername.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblUsername.ForeColor = Color.FromArgb(50, 50, 50);
            lblUsername.Location = new Point(0, 0);
            lblUsername.Name = "lblUsername";
            lblUsername.Padding = new Padding(0, 0, 0, 5);
            lblUsername.Size = new Size(79, 20);
            lblUsername.TabIndex = 0;
            lblUsername.Text = "Email Utente";
            // 
            // pnlTokenField
            // 
            pnlTokenField.BackColor = Color.FromArgb(249, 250, 251);
            pnlTokenField.Controls.Add(txtToken);
            pnlTokenField.Controls.Add(lblToken);
            pnlTokenField.Location = new Point(8, 135);
            pnlTokenField.Name = "pnlTokenField";
            pnlTokenField.Size = new Size(436, 47);
            pnlTokenField.TabIndex = 2;
            // 
            // txtToken
            // 
            txtToken.BorderStyle = BorderStyle.FixedSingle;
            txtToken.Font = new Font("Segoe UI", 11F);
            txtToken.Location = new Point(0, 20);
            txtToken.Name = "txtToken";
            txtToken.PasswordChar = '●';
            txtToken.PlaceholderText = "Genera il token dalle impostazioni di Jira";
            txtToken.Size = new Size(433, 27);
            txtToken.TabIndex = 1;
            // 
            // lblToken
            // 
            lblToken.AutoSize = true;
            lblToken.Dock = DockStyle.Top;
            lblToken.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblToken.ForeColor = Color.FromArgb(50, 50, 50);
            lblToken.Location = new Point(0, 0);
            lblToken.Name = "lblToken";
            lblToken.Padding = new Padding(0, 0, 0, 5);
            lblToken.Size = new Size(174, 20);
            lblToken.TabIndex = 0;
            lblToken.Text = "API Token (opzionale per test)";
            // 
            // lblJiraHint
            // 
            lblJiraHint.BackColor = Color.FromArgb(249, 250, 251);
            lblJiraHint.Dock = DockStyle.Top;
            lblJiraHint.Font = new Font("Segoe UI", 8.5F);
            lblJiraHint.ForeColor = Color.FromArgb(100, 116, 139);
            lblJiraHint.Location = new Point(4, 4);
            lblJiraHint.Name = "lblJiraHint";
            lblJiraHint.Padding = new Padding(16, 8, 16, 8);
            lblJiraHint.Size = new Size(452, 31);
            lblJiraHint.TabIndex = 0;
            lblJiraHint.Text = "💡 Accesso semplificato - inserisci la tua email aziendale";
            lblJiraHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pnlMicrosoftAuth
            // 
            pnlMicrosoftAuth.BackColor = Color.FromArgb(249, 250, 251);
            pnlMicrosoftAuth.Controls.Add(cardMicrosoftSSO);
            pnlMicrosoftAuth.Location = new Point(0, 176);
            pnlMicrosoftAuth.Name = "pnlMicrosoftAuth";
            pnlMicrosoftAuth.Padding = new Padding(30, 20, 30, 20);
            pnlMicrosoftAuth.Size = new Size(520, 260);
            pnlMicrosoftAuth.TabIndex = 4;
            pnlMicrosoftAuth.Visible = false;
            // 
            // cardMicrosoftSSO
            // 
            cardMicrosoftSSO.BackColor = Color.FromArgb(249, 250, 251);
            cardMicrosoftSSO.Controls.Add(lblMicrosoftTitle);
            cardMicrosoftSSO.Controls.Add(lblMicrosoftInfo);
            cardMicrosoftSSO.Controls.Add(btnMicrosoftLogin);
            cardMicrosoftSSO.Controls.Add(lblDomainRestriction);
            cardMicrosoftSSO.Dock = DockStyle.Fill;
            cardMicrosoftSSO.Location = new Point(30, 20);
            cardMicrosoftSSO.Name = "cardMicrosoftSSO";
            cardMicrosoftSSO.Padding = new Padding(30, 25, 30, 25);
            cardMicrosoftSSO.Size = new Size(460, 220);
            cardMicrosoftSSO.TabIndex = 0;
            // 
            // lblMicrosoftTitle
            // 
            lblMicrosoftTitle.AutoSize = true;
            lblMicrosoftTitle.Dock = DockStyle.Top;
            lblMicrosoftTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblMicrosoftTitle.ForeColor = Color.FromArgb(30, 64, 175);
            lblMicrosoftTitle.Location = new Point(30, 105);
            lblMicrosoftTitle.Name = "lblMicrosoftTitle";
            lblMicrosoftTitle.Padding = new Padding(0, 0, 0, 10);
            lblMicrosoftTitle.Size = new Size(254, 31);
            lblMicrosoftTitle.TabIndex = 0;
            lblMicrosoftTitle.Text = "🏢 Accesso Aziendale Microsoft";
            // 
            // lblMicrosoftInfo
            // 
            lblMicrosoftInfo.AutoSize = true;
            lblMicrosoftInfo.Dock = DockStyle.Top;
            lblMicrosoftInfo.Font = new Font("Segoe UI", 9F);
            lblMicrosoftInfo.ForeColor = Color.FromArgb(71, 85, 105);
            lblMicrosoftInfo.Location = new Point(30, 70);
            lblMicrosoftInfo.Name = "lblMicrosoftInfo";
            lblMicrosoftInfo.Padding = new Padding(0, 0, 0, 20);
            lblMicrosoftInfo.Size = new Size(407, 35);
            lblMicrosoftInfo.TabIndex = 1;
            lblMicrosoftInfo.Text = "Accedi con le tue credenziali Microsoft aziendali per utilizzare l'applicazione.";
            // 
            // btnMicrosoftLogin
            // 
            btnMicrosoftLogin.BackColor = Color.FromArgb(0, 120, 212);
            btnMicrosoftLogin.Cursor = Cursors.Hand;
            btnMicrosoftLogin.Dock = DockStyle.Top;
            btnMicrosoftLogin.FlatAppearance.BorderSize = 0;
            btnMicrosoftLogin.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 90, 158);
            btnMicrosoftLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 103, 184);
            btnMicrosoftLogin.FlatStyle = FlatStyle.Flat;
            btnMicrosoftLogin.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnMicrosoftLogin.ForeColor = Color.White;
            btnMicrosoftLogin.Location = new Point(30, 25);
            btnMicrosoftLogin.Name = "btnMicrosoftLogin";
            btnMicrosoftLogin.Size = new Size(400, 45);
            btnMicrosoftLogin.TabIndex = 2;
            btnMicrosoftLogin.Text = "🔗 Accedi con Microsoft";
            btnMicrosoftLogin.UseVisualStyleBackColor = false;
            // 
            // lblDomainRestriction
            // 
            lblDomainRestriction.BackColor = Color.FromArgb(255, 248, 220);
            lblDomainRestriction.Dock = DockStyle.Bottom;
            lblDomainRestriction.Font = new Font("Segoe UI", 8.5F);
            lblDomainRestriction.ForeColor = Color.FromArgb(133, 77, 14);
            lblDomainRestriction.Location = new Point(30, 157);
            lblDomainRestriction.Name = "lblDomainRestriction";
            lblDomainRestriction.Padding = new Padding(12, 8, 12, 8);
            lblDomainRestriction.Size = new Size(400, 38);
            lblDomainRestriction.TabIndex = 3;
            lblDomainRestriction.Text = "⚠️ Accesso limitato ai domini @dedagroup.com";
            lblDomainRestriction.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pnlActions
            // 
            pnlActions.BackColor = Color.FromArgb(248, 250, 252);
            pnlActions.Controls.Add(btnTest);
            pnlActions.Controls.Add(btnSave);
            pnlActions.Controls.Add(btnCancel);
            pnlActions.Location = new Point(0, 431);
            pnlActions.Name = "pnlActions";
            pnlActions.Padding = new Padding(30, 12, 30, 12);
            pnlActions.Size = new Size(520, 57);
            pnlActions.TabIndex = 1;
            // 
            // btnTest
            // 
            btnTest.BackColor = Color.FromArgb(16, 124, 16);
            btnTest.Cursor = Cursors.Hand;
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatAppearance.MouseDownBackColor = Color.FromArgb(12, 94, 12);
            btnTest.FlatAppearance.MouseOverBackColor = Color.FromArgb(14, 109, 14);
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnTest.ForeColor = Color.White;
            btnTest.Location = new Point(30, 15);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(120, 35);
            btnTest.TabIndex = 0;
            btnTest.Text = "Test Connessione";
            btnTest.UseVisualStyleBackColor = false;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.BackColor = Color.FromArgb(34, 139, 34);
            btnSave.Cursor = Cursors.Hand;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 104, 25);
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 120, 29);
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.ForeColor = Color.White;
            btnSave.Location = new Point(340, 15);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(80, 35);
            btnSave.TabIndex = 1;
            btnSave.Text = "Salva";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.BackColor = Color.FromArgb(148, 163, 184);
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.MouseDownBackColor = Color.FromArgb(113, 128, 150);
            btnCancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(129, 140, 158);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnCancel.ForeColor = Color.White;
            btnCancel.Location = new Point(430, 15);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 35);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Annulla";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // pnlFooter
            // 
            pnlFooter.BackColor = Color.WhiteSmoke;
            pnlFooter.Controls.Add(lblStatus);
            pnlFooter.Location = new Point(0, 497);
            pnlFooter.Name = "pnlFooter";
            pnlFooter.Padding = new Padding(30, 8, 30, 8);
            pnlFooter.Size = new Size(520, 54);
            pnlFooter.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Dock = DockStyle.Bottom;
            lblStatus.Font = new Font("Segoe UI", 8F);
            lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
            lblStatus.Location = new Point(30, 33);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(42, 13);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Pronto";
            // 
            // FrmCredentials
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(249, 250, 251);
            ClientSize = new Size(521, 551);
            Controls.Add(pnlMain);
            Controls.Add(pnlActions);
            Controls.Add(pnlFooter);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmCredentials";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Autenticazione - Jira Ticket Manager";
            pnlMain.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlAuthModeSelector.ResumeLayout(false);
            pnlAuthModeSelector.PerformLayout();
            pnlJiraAuth.ResumeLayout(false);
            cardJiraCredentials.ResumeLayout(false);
            pnlJiraFields.ResumeLayout(false);
            pnlServerField.ResumeLayout(false);
            pnlServerField.PerformLayout();
            pnlUsernameField.ResumeLayout(false);
            pnlUsernameField.PerformLayout();
            pnlTokenField.ResumeLayout(false);
            pnlTokenField.PerformLayout();
            pnlMicrosoftAuth.ResumeLayout(false);
            cardMicrosoftSSO.ResumeLayout(false);
            cardMicrosoftSSO.PerformLayout();
            pnlActions.ResumeLayout(false);
            pnlFooter.ResumeLayout(false);
            pnlFooter.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblSubtitle;
        private System.Windows.Forms.Panel pnlAuthModeSelector;
        private System.Windows.Forms.Label lblAuthMode;
        private System.Windows.Forms.RadioButton rbJiraApi;
        private System.Windows.Forms.RadioButton rbMicrosoftSSO;
        private System.Windows.Forms.Panel pnlDivider;
        private System.Windows.Forms.Panel pnlJiraAuth;
        private System.Windows.Forms.Panel cardJiraCredentials;
        private System.Windows.Forms.Panel pnlJiraFields;
        private System.Windows.Forms.Panel pnlServerField;
        private System.Windows.Forms.TextBox txtServer;
        private System.Windows.Forms.Label lblServer;
        private System.Windows.Forms.Panel pnlUsernameField;
        private System.Windows.Forms.TextBox txtUsername;
        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Panel pnlTokenField;
        private System.Windows.Forms.TextBox txtToken;
        private System.Windows.Forms.Label lblToken;
        private System.Windows.Forms.Label lblJiraHint;
        private System.Windows.Forms.Panel pnlMicrosoftAuth;
        private System.Windows.Forms.Panel cardMicrosoftSSO;
        private System.Windows.Forms.Label lblMicrosoftTitle;
        private System.Windows.Forms.Label lblMicrosoftInfo;
        private System.Windows.Forms.Button btnMicrosoftLogin;
        private System.Windows.Forms.Label lblDomainRestriction;
        private System.Windows.Forms.Panel pnlActions;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Panel pnlFooter;
        private System.Windows.Forms.Label lblStatus;
    }
}