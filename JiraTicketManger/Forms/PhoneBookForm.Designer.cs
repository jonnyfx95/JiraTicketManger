using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    partial class PhoneBookForm
    {
        private IContainer components = null;

        #region Control Declarations
        private Panel pnlHeader;
        private Panel pnlToolbar;
        private Panel pnlContent;
        private DataGridView dgvPhoneBook;
        private ProgressBar prgLoading;
        private StatusStrip statusStrip1;

        // Header controls
        private Label lblTitle;
        private Label lblSubtitle;

        // Toolbar controls
        private TextBox txtFilter;
        private Button btnRefresh;
        private Button btnExport;
        private Button btnClear;
        private Label lblFilterHint;

        // Status bar controls - IDENTICI A MainForm
        private ToolStripStatusLabel tslConnection;
        private ToolStripStatusLabel tslResults;
        private ToolStripStatusLabel tslLastUpdate;

        // DataGridView columns
        private DataGridViewTextBoxColumn colCliente;
        private DataGridViewTextBoxColumn colApplicativo;
        private DataGridViewTextBoxColumn colArea;
        private DataGridViewTextBoxColumn colNome;
        private DataGridViewTextBoxColumn colEmail;
        private DataGridViewTextBoxColumn colTelefono;
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
            this.pnlToolbar = new Panel();
            this.txtFilter = new TextBox();
            this.btnRefresh = new Button();
            this.btnExport = new Button();
            this.btnClear = new Button();
            this.lblFilterHint = new Label();
            this.pnlContent = new Panel();
            this.dgvPhoneBook = new DataGridView();
            this.colCliente = new DataGridViewTextBoxColumn();
            this.colApplicativo = new DataGridViewTextBoxColumn();
            this.colArea = new DataGridViewTextBoxColumn();
            this.colNome = new DataGridViewTextBoxColumn();
            this.colEmail = new DataGridViewTextBoxColumn();
            this.colTelefono = new DataGridViewTextBoxColumn();
            this.statusStrip1 = new StatusStrip();
            this.tslConnection = new ToolStripStatusLabel();
            this.tslResults = new ToolStripStatusLabel();
            this.tslLastUpdate = new ToolStripStatusLabel();
            this.prgLoading = new ProgressBar();
            this.pnlHeader.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            this.pnlContent.SuspendLayout();
            ((ISupportInitialize)(this.dgvPhoneBook)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();

            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = Color.FromArgb(41, 128, 185);
            this.pnlHeader.Controls.Add(this.lblSubtitle);
            this.pnlHeader.Controls.Add(this.lblTitle);
            this.pnlHeader.Dock = DockStyle.Top;
            this.pnlHeader.Location = new Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new Size(1400, 70);
            this.pnlHeader.TabIndex = 0;

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.White;
            this.lblTitle.Location = new Point(20, 15);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(350, 30);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "📞 Rubrica Telefonica Jira";

            // 
            // lblSubtitle
            // 
            this.lblSubtitle.AutoSize = true;
            this.lblSubtitle.Font = new Font("Segoe UI", 9F);
            this.lblSubtitle.ForeColor = Color.FromArgb(236, 240, 241);
            this.lblSubtitle.Location = new Point(23, 45);
            this.lblSubtitle.Name = "lblSubtitle";
            this.lblSubtitle.Size = new Size(300, 15);
            this.lblSubtitle.TabIndex = 1;
            this.lblSubtitle.Text = "Gestione contatti estratti dai ticket Jira";

            // 
            // pnlToolbar
            // 
            this.pnlToolbar.BackColor = Color.FromArgb(248, 249, 250);
            this.pnlToolbar.BorderStyle = BorderStyle.FixedSingle;
            this.pnlToolbar.Controls.Add(this.lblFilterHint);
            this.pnlToolbar.Controls.Add(this.btnClear);
            this.pnlToolbar.Controls.Add(this.btnExport);
            this.pnlToolbar.Controls.Add(this.btnRefresh);
            this.pnlToolbar.Controls.Add(this.txtFilter);
            this.pnlToolbar.Dock = DockStyle.Top;
            this.pnlToolbar.Location = new Point(0, 70);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Size = new Size(1400, 80);
            this.pnlToolbar.TabIndex = 1;

            // 
            // txtFilter
            // 
            this.txtFilter.Font = new Font("Segoe UI", 10F);
            this.txtFilter.Location = new Point(20, 20);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.Size = new Size(450, 25);
            this.txtFilter.TabIndex = 0;
            this.txtFilter.PlaceholderText = "🔍 Filtra per cliente, nome, email, telefono...";

            // 
            // lblFilterHint
            // 
            this.lblFilterHint.AutoSize = true;
            this.lblFilterHint.Font = new Font("Segoe UI", 8F);
            this.lblFilterHint.ForeColor = Color.FromArgb(108, 117, 125);
            this.lblFilterHint.Location = new Point(20, 50);
            this.lblFilterHint.Name = "lblFilterHint";
            this.lblFilterHint.Size = new Size(320, 13);
            this.lblFilterHint.TabIndex = 4;
            this.lblFilterHint.Text = "💡 Usa virgole per filtri multipli (es: \"Mario, Roma, 06\")";

            // 
            // btnRefresh
            // 
            this.btnRefresh.BackColor = Color.FromArgb(8, 145, 178);
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = FlatStyle.Flat;
            this.btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnRefresh.ForeColor = Color.White;
            this.btnRefresh.Location = new Point(490, 18);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new Size(130, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "🔄 Aggiorna";
            this.btnRefresh.UseVisualStyleBackColor = false;
            this.btnRefresh.Cursor = Cursors.Hand;

            // 
            // btnExport
            // 
            this.btnExport.BackColor = Color.FromArgb(22, 163, 74);
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.FlatStyle = FlatStyle.Flat;
            this.btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnExport.ForeColor = Color.White;
            this.btnExport.Location = new Point(630, 18);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new Size(150, 30);
            this.btnExport.TabIndex = 2;
            this.btnExport.Text = "📊 Esporta Excel";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Cursor = Cursors.Hand;

            // 
            // btnClear
            // 
            this.btnClear.BackColor = Color.FromArgb(108, 117, 125);
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = FlatStyle.Flat;
            this.btnClear.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.btnClear.ForeColor = Color.White;
            this.btnClear.Location = new Point(790, 18);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new Size(100, 30);
            this.btnClear.TabIndex = 3;
            this.btnClear.Text = "🗑️ Pulisci";
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Cursor = Cursors.Hand;

            // 
            // pnlContent
            // 
            this.pnlContent.Controls.Add(this.dgvPhoneBook);
            this.pnlContent.Dock = DockStyle.Fill;
            this.pnlContent.Location = new Point(0, 150);
            this.pnlContent.Name = "pnlContent";
            this.pnlContent.Padding = new Padding(10);
            this.pnlContent.Size = new Size(1400, 580);
            this.pnlContent.TabIndex = 2;

            // 
            // dgvPhoneBook - STILE IDENTICO A MainForm.dgvTickets
            // 
            this.dgvPhoneBook.AllowUserToAddRows = false;
            this.dgvPhoneBook.AllowUserToDeleteRows = false;
            this.dgvPhoneBook.AllowUserToResizeRows = false;
            this.dgvPhoneBook.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvPhoneBook.BackgroundColor = Color.White;
            this.dgvPhoneBook.BorderStyle = BorderStyle.None;
            this.dgvPhoneBook.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvPhoneBook.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            // ✅ Header Style - IDENTICO A MainForm
            this.dgvPhoneBook.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 250);
            this.dgvPhoneBook.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            this.dgvPhoneBook.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
            this.dgvPhoneBook.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 249, 250);
            this.dgvPhoneBook.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(52, 73, 94);
            this.dgvPhoneBook.ColumnHeadersHeight = 40;
            this.dgvPhoneBook.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvPhoneBook.Columns.AddRange(new DataGridViewColumn[] {
                this.colCliente,
                this.colApplicativo,
                this.colArea,
                this.colNome,
                this.colEmail,
                this.colTelefono});
            // ✅ Cell Style - IDENTICO A MainForm
            this.dgvPhoneBook.DefaultCellStyle.BackColor = Color.White;
            this.dgvPhoneBook.DefaultCellStyle.Font = new Font("Segoe UI", 8F);
            this.dgvPhoneBook.DefaultCellStyle.ForeColor = Color.FromArgb(52, 73, 94);
            this.dgvPhoneBook.DefaultCellStyle.SelectionBackColor = Color.FromArgb(233, 246, 255);
            this.dgvPhoneBook.DefaultCellStyle.SelectionForeColor = Color.FromArgb(52, 73, 94);
            this.dgvPhoneBook.Dock = DockStyle.Fill;
            this.dgvPhoneBook.EnableHeadersVisualStyles = false;
            this.dgvPhoneBook.GridColor = Color.FromArgb(226, 232, 240);
            this.dgvPhoneBook.Location = new Point(10, 10);
            this.dgvPhoneBook.MultiSelect = false;
            this.dgvPhoneBook.Name = "dgvPhoneBook";
            this.dgvPhoneBook.ReadOnly = true;
            this.dgvPhoneBook.RowHeadersVisible = false;
            this.dgvPhoneBook.RowTemplate.Height = 35;
            this.dgvPhoneBook.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvPhoneBook.Size = new Size(1380, 560);
            this.dgvPhoneBook.TabIndex = 0;

            // 
            // colCliente
            // 
            this.colCliente.DataPropertyName = "Cliente";
            this.colCliente.HeaderText = "Cliente / Organizzazione";
            this.colCliente.MinimumWidth = 150;
            this.colCliente.Name = "colCliente";
            this.colCliente.ReadOnly = true;
            this.colCliente.Width = 230;

            // 
            // colApplicativo
            // 
            this.colApplicativo.DataPropertyName = "Applicativo";
            this.colApplicativo.HeaderText = "Applicativo";
            this.colApplicativo.MinimumWidth = 150;
            this.colApplicativo.Name = "colApplicativo";
            this.colApplicativo.ReadOnly = true;
            this.colApplicativo.Width = 250;

            // 
            // colArea
            // 
            this.colArea.DataPropertyName = "Area";
            this.colArea.HeaderText = "Area";
            this.colArea.MinimumWidth = 100;
            this.colArea.Name = "colArea";
            this.colArea.ReadOnly = true;
            this.colArea.Width = 150;

            // 
            // colNome
            // 
            this.colNome.DataPropertyName = "Nome";
            this.colNome.HeaderText = "Nome";
            this.colNome.MinimumWidth = 120;
            this.colNome.Name = "colNome";
            this.colNome.ReadOnly = true;
            this.colNome.Width = 180;

            // 
            // colEmail
            // 
            this.colEmail.DataPropertyName = "Email";
            this.colEmail.HeaderText = "Email";
            this.colEmail.MinimumWidth = 180;
            this.colEmail.Name = "colEmail";
            this.colEmail.ReadOnly = true;
            this.colEmail.Width = 250;

            // 
            // colTelefono
            // 
            this.colTelefono.DataPropertyName = "Telefono";
            this.colTelefono.HeaderText = "Telefono";
            this.colTelefono.MinimumWidth = 120;
            this.colTelefono.Name = "colTelefono";
            this.colTelefono.ReadOnly = true;
            this.colTelefono.Width = 150;

            // 
            // prgLoading
            // 
            this.prgLoading.Dock = DockStyle.Bottom;
            this.prgLoading.Location = new Point(0, 730);
            this.prgLoading.Name = "prgLoading";
            this.prgLoading.Size = new Size(1400, 5);
            this.prgLoading.Style = ProgressBarStyle.Marquee;
            this.prgLoading.TabIndex = 3;
            this.prgLoading.Visible = false;

            // 
            // statusStrip1 - ✅ IDENTICO A MainForm
            // 
            this.statusStrip1.BackColor = Color.FromArgb(248, 249, 250);
            this.statusStrip1.Font = new Font("Segoe UI", 8F);
            this.statusStrip1.Items.AddRange(new ToolStripItem[] {
                this.tslConnection,
                this.tslResults,
                this.tslLastUpdate
            });
            this.statusStrip1.Location = new Point(0, 735);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new Size(1400, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";

            // 
            // tslConnection
            // 
            this.tslConnection.ForeColor = Color.FromArgb(40, 167, 69);
            this.tslConnection.Name = "tslConnection";
            this.tslConnection.Size = new Size(120, 17);
            this.tslConnection.Text = "🟢 Connesso a Jira";

            // 
            // tslResults
            // 
            this.tslResults.Name = "tslResults";
            this.tslResults.Size = new Size(100, 17);
            this.tslResults.Spring = true;
            this.tslResults.Text = "📊 0 contatti";

            // 
            // tslLastUpdate
            // 
            this.tslLastUpdate.Name = "tslLastUpdate";
            this.tslLastUpdate.Size = new Size(180, 17);
            this.tslLastUpdate.Text = "⏱️ Ultimo agg: --";
            this.tslLastUpdate.TextAlign = ContentAlignment.MiddleRight;

            // 
            // PhoneBookForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1400, 757);
            this.Controls.Add(this.pnlContent);
            this.Controls.Add(this.prgLoading);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.pnlToolbar);
            this.Controls.Add(this.pnlHeader);
            this.Font = new Font("Segoe UI", 9F);
            this.MinimumSize = new Size(1000, 600);
            this.Name = "PhoneBookForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Rubrica Telefonica Jira - Deda Group";
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.pnlContent.ResumeLayout(false);
            ((ISupportInitialize)(this.dgvPhoneBook)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}