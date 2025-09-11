namespace JiraTicketManager.Forms
{
    partial class CommentDetailForm
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
            panelHeader = new Panel();
            lblTicketInfo = new Label();
            lblVisibilityBadge = new Label();
            lblDate = new Label();
            lblAuthor = new Label();
            panelBody = new Panel();
            txtCommentBody = new TextBox();
            panelFooter = new Panel();
            lblModified = new Label();
            panelHeader.SuspendLayout();
            panelBody.SuspendLayout();
            panelFooter.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(248, 249, 250);
            panelHeader.BorderStyle = BorderStyle.FixedSingle;
            panelHeader.Controls.Add(lblTicketInfo);
            panelHeader.Controls.Add(lblVisibilityBadge);
            panelHeader.Controls.Add(lblDate);
            panelHeader.Controls.Add(lblAuthor);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Padding = new Padding(20, 15, 20, 15);
            panelHeader.Size = new Size(684, 80);
            panelHeader.TabIndex = 0;
            // 
            // lblTicketInfo
            // 
            lblTicketInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTicketInfo.AutoSize = true;
            lblTicketInfo.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTicketInfo.ForeColor = Color.FromArgb(108, 117, 125);
            lblTicketInfo.Location = new Point(580, 45);
            lblTicketInfo.Name = "lblTicketInfo";
            lblTicketInfo.Size = new Size(102, 13);
            lblTicketInfo.TabIndex = 3;
            lblTicketInfo.Text = "🎫 Ticket CC-12345";
            // 
            // lblVisibilityBadge
            // 
            lblVisibilityBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblVisibilityBadge.AutoSize = true;
            lblVisibilityBadge.BackColor = Color.FromArgb(220, 38, 38);
            lblVisibilityBadge.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblVisibilityBadge.ForeColor = Color.White;
            lblVisibilityBadge.Location = new Point(580, 18);
            lblVisibilityBadge.Name = "lblVisibilityBadge";
            lblVisibilityBadge.Padding = new Padding(8, 4, 8, 4);
            lblVisibilityBadge.Size = new Size(88, 23);
            lblVisibilityBadge.TabIndex = 2;
            lblVisibilityBadge.Text = "🔒 PRIVATO";
            // 
            // lblDate
            // 
            lblDate.AutoSize = true;
            lblDate.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDate.ForeColor = Color.FromArgb(108, 117, 125);
            lblDate.Location = new Point(21, 40);
            lblDate.Name = "lblDate";
            lblDate.Size = new Size(131, 17);
            lblDate.TabIndex = 1;
            lblDate.Text = "📅 30/07/2025 15:45";
            // 
            // lblAuthor
            // 
            lblAuthor.AutoSize = true;
            lblAuthor.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAuthor.ForeColor = Color.FromArgb(33, 37, 41);
            lblAuthor.Location = new Point(20, 15);
            lblAuthor.Name = "lblAuthor";
            lblAuthor.Size = new Size(138, 21);
            lblAuthor.TabIndex = 0;
            lblAuthor.Text = "\U0001f7e3 Paola Tomasi";
            // 
            // panelBody
            // 
            panelBody.BackColor = Color.White;
            panelBody.Controls.Add(txtCommentBody);
            panelBody.Dock = DockStyle.Fill;
            panelBody.Location = new Point(0, 80);
            panelBody.Name = "panelBody";
            panelBody.Padding = new Padding(20);
            panelBody.Size = new Size(684, 332);
            panelBody.TabIndex = 1;
            // 
            // txtCommentBody
            // 
            txtCommentBody.BackColor = Color.White;
            txtCommentBody.BorderStyle = BorderStyle.None;
            txtCommentBody.Dock = DockStyle.Fill;
            txtCommentBody.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtCommentBody.ForeColor = Color.FromArgb(33, 37, 41);
            txtCommentBody.Location = new Point(20, 20);
            txtCommentBody.Multiline = true;
            txtCommentBody.Name = "txtCommentBody";
            txtCommentBody.ReadOnly = true;
            txtCommentBody.ScrollBars = ScrollBars.Vertical;
            txtCommentBody.Size = new Size(644, 292);
            txtCommentBody.TabIndex = 0;
            txtCommentBody.Text = "QUESTO È N.16097019 del 29/07/2023 - Vincenzo Ilsami di PROVINCIA DI MODENA richiede come sia possibile effettuare un calcolo pensionistico su Sito web che permetta di verificare...";
            // 
            // panelFooter
            // 
            panelFooter.BackColor = Color.FromArgb(248, 249, 250);
            panelFooter.BorderStyle = BorderStyle.FixedSingle;
            panelFooter.Controls.Add(lblModified);
            panelFooter.Dock = DockStyle.Bottom;
            panelFooter.Location = new Point(0, 412);
            panelFooter.Name = "panelFooter";
            panelFooter.Padding = new Padding(20, 10, 20, 10);
            panelFooter.Size = new Size(684, 50);
            panelFooter.TabIndex = 2;
            // 
            // lblModified
            // 
            lblModified.AutoSize = true;
            lblModified.Font = new Font("Segoe UI", 8.25F, FontStyle.Italic, GraphicsUnit.Point, 0);
            lblModified.ForeColor = Color.FromArgb(108, 117, 125);
            lblModified.Location = new Point(20, 18);
            lblModified.Name = "lblModified";
            lblModified.Size = new Size(170, 13);
            lblModified.TabIndex = 1;
            lblModified.Text = "✏️ Modificato il 30/07/2025 16:20";
            // 
            // CommentDetailForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(684, 462);
            Controls.Add(panelBody);
            Controls.Add(panelFooter);
            Controls.Add(panelHeader);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MaximizeBox = false;
            MinimumSize = new Size(500, 400);
            Name = "CommentDetailForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "💬 Dettaglio Commento";
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            panelBody.ResumeLayout(false);
            panelBody.PerformLayout();
            panelFooter.ResumeLayout(false);
            panelFooter.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblAuthor;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Label lblVisibilityBadge;
        private System.Windows.Forms.Panel panelBody;
        private System.Windows.Forms.TextBox txtCommentBody;
        private System.Windows.Forms.Panel panelFooter;
        private System.Windows.Forms.Label lblModified;
        private System.Windows.Forms.Label lblTicketInfo;
    }
}