namespace JiraTicketManager.Forms
{
    partial class CommentPreviewDialog
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
            try
            {
                this.SuspendLayout();

                // === FORM PROPERTIES ===
                this.Text = "💬 Anteprima Commento Jira";
                this.Size = new Size(800, 600);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MinimumSize = new Size(600, 400);
                this.MaximizeBox = true;
                this.ShowIcon = false;
                this.BackColor = Color.White;
                this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);

                // === HEADER PANEL ===
                panelHeader = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 80,
                    BackColor = Color.FromArgb(248, 249, 250),
                    Padding = new Padding(20, 15, 20, 10)
                };

                lblTitle = new Label
                {
                    Text = "📧 ANTEPRIMA COMMENTO EMAIL INOLTRATA",
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(52, 73, 94),
                    AutoSize = true,
                    Location = new Point(20, 15)
                };

                lblTicketInfo = new Label
                {
                    Text = $"🎫 Ticket: {_ticketKey}",
                    Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    AutoSize = true,
                    Location = new Point(20, 45)
                };

                panelHeader.Controls.Add(lblTitle);
                panelHeader.Controls.Add(lblTicketInfo);

                // === BODY PANEL ===
                panelBody = new Panel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20)
                };

                txtCommentPreview = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9F, FontStyle.Regular), // Font monospace per formattazione
                    ForeColor = Color.FromArgb(73, 80, 87),
                    Text = _commentContent,
                    TabStop = true
                };

                panelBody.Controls.Add(txtCommentPreview);

                // === FOOTER PANEL ===
                panelFooter = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 70,
                    BackColor = Color.FromArgb(248, 249, 250),
                    Padding = new Padding(20, 15, 20, 15)
                };

                // Character count label
                lblCharacterCount = new Label
                {
                    Text = $"📏 Lunghezza: {_commentContent.Length} caratteri",
                    Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(108, 117, 125),
                    AutoSize = true,
                    Location = new Point(20, 15)
                };

                // Buttons
                btnAnnulla = new Button
                {
                    Text = "❌ Annulla",
                    Size = new Size(100, 30),
                    Location = new Point(panelFooter.Width - 220, 15),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.FromArgb(108, 117, 125),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    DialogResult = DialogResult.Cancel,
                    UseVisualStyleBackColor = false
                };
                btnAnnulla.FlatAppearance.BorderSize = 0;

                btnInvia = new Button
                {
                    Text = "✅ Invia Commento",
                    Size = new Size(120, 30),
                    Location = new Point(panelFooter.Width - 110, 15),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.FromArgb(0, 120, 212),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    DialogResult = DialogResult.OK,
                    UseVisualStyleBackColor = false
                };
                btnInvia.FlatAppearance.BorderSize = 0;

                panelFooter.Controls.Add(lblCharacterCount);
                panelFooter.Controls.Add(btnAnnulla);
                panelFooter.Controls.Add(btnInvia);

                // === ADD TO FORM ===
                this.Controls.Add(panelBody);
                this.Controls.Add(panelHeader);
                this.Controls.Add(panelFooter);

                this.ResumeLayout(false);
                this.PerformLayout();

                _logger.LogInfo("Controlli dialog inizializzati");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Errore inizializzazione controlli dialog: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}