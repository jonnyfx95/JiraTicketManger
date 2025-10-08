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
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
            pnlHeader = new Panel();
            lblSubtitle = new Label();
            lblTitle = new Label();
            pnlToolbar = new Panel();
            lblFilterHint = new Label();
            btnClear = new Button();
            btnExport = new Button();
            btnRefresh = new Button();
            txtFilter = new TextBox();
            pnlContent = new Panel();
            dgvPhoneBook = new DataGridView();
            colCliente = new DataGridViewTextBoxColumn();
            colApplicativo = new DataGridViewTextBoxColumn();
            colArea = new DataGridViewTextBoxColumn();
            colNome = new DataGridViewTextBoxColumn();
            colEmail = new DataGridViewTextBoxColumn();
            colTelefono = new DataGridViewTextBoxColumn();
            statusStrip1 = new StatusStrip();
            tslConnection = new ToolStripStatusLabel();
            tslResults = new ToolStripStatusLabel();
            tslLastUpdate = new ToolStripStatusLabel();
            prgLoading = new ProgressBar();
            txtSearchTicket = new TextBox();
            lblTicketHint = new Label();
            chkTeams = new CheckBox();
            pnlHeader.SuspendLayout();
            pnlToolbar.SuspendLayout();
            pnlContent.SuspendLayout();
            ((ISupportInitialize)dgvPhoneBook).BeginInit();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(41, 128, 185);
            pnlHeader.Controls.Add(lblSubtitle);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1400, 70);
            pnlHeader.TabIndex = 0;
            // 
            // lblSubtitle
            // 
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9F);
            lblSubtitle.ForeColor = Color.FromArgb(236, 240, 241);
            lblSubtitle.Location = new Point(23, 45);
            lblSubtitle.Name = "lblSubtitle";
            lblSubtitle.Size = new Size(207, 15);
            lblSubtitle.TabIndex = 1;
            lblSubtitle.Text = "Gestione contatti estratti dai ticket Jira";
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.Location = new Point(20, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(282, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "📞 Rubrica Telefonica Jira";
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = Color.FromArgb(248, 249, 250);
            pnlToolbar.BorderStyle = BorderStyle.FixedSingle;
            pnlToolbar.Controls.Add(chkTeams);
            pnlToolbar.Controls.Add(lblTicketHint);
            pnlToolbar.Controls.Add(txtSearchTicket);
            pnlToolbar.Controls.Add(lblFilterHint);
            pnlToolbar.Controls.Add(btnClear);
            pnlToolbar.Controls.Add(btnExport);
            pnlToolbar.Controls.Add(btnRefresh);
            pnlToolbar.Controls.Add(txtFilter);
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Location = new Point(0, 70);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new Size(1400, 80);
            pnlToolbar.TabIndex = 1;
            // 
            // lblFilterHint
            // 
            lblFilterHint.AutoSize = true;
            lblFilterHint.Font = new Font("Segoe UI", 8F);
            lblFilterHint.ForeColor = Color.FromArgb(108, 117, 125);
            lblFilterHint.Location = new Point(20, 50);
            lblFilterHint.Name = "lblFilterHint";
            lblFilterHint.Size = new Size(282, 13);
            lblFilterHint.TabIndex = 4;
            lblFilterHint.Text = "💡 Usa virgole per filtri multipli (es: \"Mario, Roma, 06\")";
            // 
            // btnClear
            // 
            btnClear.BackColor = Color.FromArgb(108, 117, 125);
            btnClear.Cursor = Cursors.Hand;
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnClear.ForeColor = Color.White;
            btnClear.Location = new Point(1116, 20);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(100, 30);
            btnClear.TabIndex = 3;
            btnClear.Text = "🗑️ Pulisci";
            btnClear.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            btnExport.BackColor = Color.FromArgb(22, 163, 74);
            btnExport.Cursor = Cursors.Hand;
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.FlatStyle = FlatStyle.Flat;
            btnExport.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnExport.ForeColor = Color.White;
            btnExport.Location = new Point(960, 20);
            btnExport.Name = "btnExport";
            btnExport.Size = new Size(150, 30);
            btnExport.TabIndex = 2;
            btnExport.Text = "📊 Esporta Excel";
            btnExport.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            btnRefresh.BackColor = Color.FromArgb(8, 145, 178);
            btnRefresh.Cursor = Cursors.Hand;
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Location = new Point(806, 20);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(148, 30);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "🔄 Aggiorna tramite API";
            btnRefresh.UseVisualStyleBackColor = false;
            // 
            // txtFilter
            // 
            txtFilter.Font = new Font("Segoe UI", 10F);
            txtFilter.Location = new Point(20, 20);
            txtFilter.Name = "txtFilter";
            txtFilter.PlaceholderText = "🔍 Filtra per cliente, nome, email, telefono...";
            txtFilter.Size = new Size(450, 25);
            txtFilter.TabIndex = 0;
            // 
            // pnlContent
            // 
            pnlContent.Controls.Add(dgvPhoneBook);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 150);
            pnlContent.Name = "pnlContent";
            pnlContent.Padding = new Padding(10);
            pnlContent.Size = new Size(1400, 580);
            pnlContent.TabIndex = 2;
            // 
            // dgvPhoneBook
            // 
            dgvPhoneBook.AllowUserToAddRows = false;
            dgvPhoneBook.AllowUserToDeleteRows = false;
            dgvPhoneBook.AllowUserToResizeRows = false;
            dgvPhoneBook.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPhoneBook.BackgroundColor = Color.White;
            dgvPhoneBook.BorderStyle = BorderStyle.None;
            dgvPhoneBook.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvPhoneBook.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle1.ForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle1.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle1.SelectionForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            dgvPhoneBook.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            dgvPhoneBook.ColumnHeadersHeight = 40;
            dgvPhoneBook.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvPhoneBook.Columns.AddRange(new DataGridViewColumn[] { colCliente, colApplicativo, colArea, colNome, colEmail, colTelefono });
            dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = Color.White;
            dataGridViewCellStyle2.Font = new Font("Segoe UI", 8F);
            dataGridViewCellStyle2.ForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(233, 246, 255);
            dataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
            dgvPhoneBook.DefaultCellStyle = dataGridViewCellStyle2;
            dgvPhoneBook.Dock = DockStyle.Fill;
            dgvPhoneBook.EnableHeadersVisualStyles = false;
            dgvPhoneBook.GridColor = Color.FromArgb(226, 232, 240);
            dgvPhoneBook.Location = new Point(10, 10);
            dgvPhoneBook.MultiSelect = false;
            dgvPhoneBook.Name = "dgvPhoneBook";
            dgvPhoneBook.ReadOnly = true;
            dgvPhoneBook.RowHeadersVisible = false;
            dgvPhoneBook.RowTemplate.Height = 35;
            dgvPhoneBook.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhoneBook.Size = new Size(1380, 560);
            dgvPhoneBook.TabIndex = 0;
            // 
            // colCliente
            // 
            colCliente.DataPropertyName = "Cliente";
            colCliente.HeaderText = "Cliente / Organizzazione";
            colCliente.MinimumWidth = 150;
            colCliente.Name = "colCliente";
            colCliente.ReadOnly = true;
            // 
            // colApplicativo
            // 
            colApplicativo.DataPropertyName = "Applicativo";
            colApplicativo.HeaderText = "Applicativo";
            colApplicativo.MinimumWidth = 150;
            colApplicativo.Name = "colApplicativo";
            colApplicativo.ReadOnly = true;
            // 
            // colArea
            // 
            colArea.DataPropertyName = "Area";
            colArea.HeaderText = "Area";
            colArea.MinimumWidth = 100;
            colArea.Name = "colArea";
            colArea.ReadOnly = true;
            // 
            // colNome
            // 
            colNome.DataPropertyName = "Nome";
            colNome.HeaderText = "Nome";
            colNome.MinimumWidth = 120;
            colNome.Name = "colNome";
            colNome.ReadOnly = true;
            // 
            // colEmail
            // 
            colEmail.DataPropertyName = "Email";
            colEmail.HeaderText = "Email";
            colEmail.MinimumWidth = 180;
            colEmail.Name = "colEmail";
            colEmail.ReadOnly = true;
            // 
            // colTelefono
            // 
            colTelefono.DataPropertyName = "Telefono";
            colTelefono.HeaderText = "Telefono";
            colTelefono.MinimumWidth = 120;
            colTelefono.Name = "colTelefono";
            colTelefono.ReadOnly = true;
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.FromArgb(248, 249, 250);
            statusStrip1.Font = new Font("Segoe UI", 8F);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tslConnection, tslResults, tslLastUpdate });
            statusStrip1.Location = new Point(0, 735);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1400, 22);
            statusStrip1.TabIndex = 4;
            statusStrip1.Text = "statusStrip1";
            // 
            // tslConnection
            // 
            tslConnection.ForeColor = Color.FromArgb(40, 167, 69);
            tslConnection.Name = "tslConnection";
            tslConnection.Size = new Size(102, 17);
            tslConnection.Text = "\U0001f7e2 Connesso a Jira";
            // 
            // tslResults
            // 
            tslResults.Name = "tslResults";
            tslResults.Size = new Size(1190, 17);
            tslResults.Spring = true;
            tslResults.Text = "📊 0 contatti";
            // 
            // tslLastUpdate
            // 
            tslLastUpdate.Name = "tslLastUpdate";
            tslLastUpdate.Size = new Size(93, 17);
            tslLastUpdate.Text = "⏱️ Ultimo agg: --";
            tslLastUpdate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // prgLoading
            // 
            prgLoading.Dock = DockStyle.Bottom;
            prgLoading.Location = new Point(0, 730);
            prgLoading.Name = "prgLoading";
            prgLoading.Size = new Size(1400, 5);
            prgLoading.Style = ProgressBarStyle.Marquee;
            prgLoading.TabIndex = 3;
            prgLoading.Visible = false;
            // 
            // txtSearchTicket
            // 
            txtSearchTicket.Font = new Font("Segoe UI", 10F);
            txtSearchTicket.Location = new Point(476, 20);
            txtSearchTicket.Name = "txtSearchTicket";
            txtSearchTicket.PlaceholderText = "🔍 Cerca per numero di Ticket";
            txtSearchTicket.Size = new Size(324, 25);
            txtSearchTicket.TabIndex = 5;
            // 
            // lblTicketHint
            // 
            lblTicketHint.AutoSize = true;
            lblTicketHint.Font = new Font("Segoe UI", 8F);
            lblTicketHint.ForeColor = Color.FromArgb(108, 117, 125);
            lblTicketHint.Location = new Point(476, 50);
            lblTicketHint.Name = "lblTicketHint";
            lblTicketHint.Size = new Size(189, 13);
            lblTicketHint.TabIndex = 6;
            lblTicketHint.Text = "💡 Cerca per numero di ticket o link";
            // 
            // chkTeams
            // 
            chkTeams.AutoSize = true;
            chkTeams.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chkTeams.Location = new Point(1232, 26);
            chkTeams.Name = "chkTeams";
            chkTeams.Size = new Size(127, 19);
            chkTeams.TabIndex = 7;
            chkTeams.Text = "Chiama con Teams";
            chkTeams.UseVisualStyleBackColor = true;
            // 
            // PhoneBookForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 757);
            Controls.Add(pnlContent);
            Controls.Add(prgLoading);
            Controls.Add(statusStrip1);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1000, 600);
            Name = "PhoneBookForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Rubrica Telefonica Jira - Dedagroup";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            pnlContent.ResumeLayout(false);
            ((ISupportInitialize)dgvPhoneBook).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTicketHint;
        private TextBox txtSearchTicket;
        private CheckBox chkTeams;
    }
}