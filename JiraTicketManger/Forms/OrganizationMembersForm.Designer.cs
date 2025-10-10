namespace JiraTicketManager.Forms
{
    partial class OrganizationMembersForm
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panelTop = new System.Windows.Forms.Panel();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.txtFilter = new System.Windows.Forms.TextBox();
            this.lblFilter = new System.Windows.Forms.Label();
            this.dgvOrganizationMembers = new System.Windows.Forms.DataGridView();
            this.colOrganizzazione = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNome = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colEmail = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNumeroTicket = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAttivo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tslConnection = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslResults = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslLastUpdate = new System.Windows.Forms.ToolStripStatusLabel();
            this.prgLoading = new System.Windows.Forms.ProgressBar();
            this.panelTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrganizationMembers)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelTop.Controls.Add(this.btnClear);
            this.panelTop.Controls.Add(this.btnExport);
            this.panelTop.Controls.Add(this.btnRefresh);
            this.panelTop.Controls.Add(this.txtFilter);
            this.panelTop.Controls.Add(this.lblFilter);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1200, 60);
            this.panelTop.TabIndex = 0;
            // 
            // btnClear
            // 
            this.btnClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClear.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.btnClear.FlatAppearance.BorderSize = 0;
            this.btnClear.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnClear.ForeColor = System.Drawing.Color.White;
            this.btnClear.Location = new System.Drawing.Point(1110, 18);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(75, 25);
            this.btnClear.TabIndex = 4;
            this.btnClear.Text = "Pulisci";
            this.btnClear.UseVisualStyleBackColor = false;
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(163)))), ((int)(((byte)(74)))));
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(1000, 18);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(100, 25);
            this.btnExport.TabIndex = 3;
            this.btnExport.Text = "📊 Esporta";
            this.btnExport.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.btnRefresh.FlatAppearance.BorderSize = 0;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnRefresh.ForeColor = System.Drawing.Color.White;
            this.btnRefresh.Location = new System.Drawing.Point(880, 18);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(110, 25);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "🔄 Aggiorna";
            this.btnRefresh.UseVisualStyleBackColor = false;
            // 
            // txtFilter
            // 
            this.txtFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFilter.BackColor = System.Drawing.Color.White;
            this.txtFilter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtFilter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtFilter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.txtFilter.Location = new System.Drawing.Point(180, 18);
            this.txtFilter.Name = "txtFilter";
            this.txtFilter.PlaceholderText = "Cerca per organizzazione, nome o email...";
            this.txtFilter.Size = new System.Drawing.Size(685, 23);
            this.txtFilter.TabIndex = 1;
            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblFilter.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            this.lblFilter.Location = new System.Drawing.Point(15, 21);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(159, 15);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "🔍 Ricerca membri org.:";
            // 
            // dgvOrganizationMembers
            // 
            this.dgvOrganizationMembers.AllowUserToAddRows = false;
            this.dgvOrganizationMembers.AllowUserToDeleteRows = false;
            this.dgvOrganizationMembers.AllowUserToResizeRows = false;
            this.dgvOrganizationMembers.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvOrganizationMembers.BackgroundColor = System.Drawing.Color.White;
            this.dgvOrganizationMembers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvOrganizationMembers.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvOrganizationMembers.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvOrganizationMembers.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvOrganizationMembers.ColumnHeadersHeight = 40;
            this.dgvOrganizationMembers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this.dgvOrganizationMembers.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colOrganizzazione,
            this.colNome,
            this.colEmail,
            this.colNumeroTicket,
            this.colAttivo});
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(233)))), ((int)(((byte)(246)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(52)))), ((int)(((byte)(73)))), ((int)(((byte)(94)))));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvOrganizationMembers.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvOrganizationMembers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOrganizationMembers.EnableHeadersVisualStyles = false;
            this.dgvOrganizationMembers.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(232)))), ((int)(((byte)(240)))));
            this.dgvOrganizationMembers.Location = new System.Drawing.Point(0, 60);
            this.dgvOrganizationMembers.MultiSelect = false;
            this.dgvOrganizationMembers.Name = "dgvOrganizationMembers";
            this.dgvOrganizationMembers.ReadOnly = true;
            this.dgvOrganizationMembers.RowHeadersVisible = false;
            this.dgvOrganizationMembers.RowTemplate.Height = 35;
            this.dgvOrganizationMembers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvOrganizationMembers.Size = new System.Drawing.Size(1200, 560);
            this.dgvOrganizationMembers.TabIndex = 1;
            // 
            // colOrganizzazione
            // 
            this.colOrganizzazione.DataPropertyName = "Organizzazione";
            this.colOrganizzazione.FillWeight = 25F;
            this.colOrganizzazione.HeaderText = "Organizzazione";
            this.colOrganizzazione.Name = "colOrganizzazione";
            this.colOrganizzazione.ReadOnly = true;
            // 
            // colNome
            // 
            this.colNome.DataPropertyName = "Nome";
            this.colNome.FillWeight = 25F;
            this.colNome.HeaderText = "Nome";
            this.colNome.Name = "colNome";
            this.colNome.ReadOnly = true;
            // 
            // colEmail
            // 
            this.colEmail.DataPropertyName = "Email";
            this.colEmail.FillWeight = 30F;
            this.colEmail.HeaderText = "Email";
            this.colEmail.Name = "colEmail";
            this.colEmail.ReadOnly = true;
            // 
            // colNumeroTicket
            // 
            this.colNumeroTicket.DataPropertyName = "NumeroTicket";
            this.colNumeroTicket.FillWeight = 10F;
            this.colNumeroTicket.HeaderText = "N° Ticket";
            this.colNumeroTicket.Name = "colNumeroTicket";
            this.colNumeroTicket.ReadOnly = true;
            // 
            // colAttivo
            // 
            this.colAttivo.DataPropertyName = "Attivo";
            this.colAttivo.FillWeight = 10F;
            this.colAttivo.HeaderText = "Stato";
            this.colAttivo.Name = "colAttivo";
            this.colAttivo.ReadOnly = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.statusStrip1.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslConnection,
            this.tslResults,
            this.tslLastUpdate});
            this.statusStrip1.Location = new System.Drawing.Point(0, 625);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip1.SizingGrip = false;
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // tslConnection
            // 
            this.tslConnection.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(167)))), ((int)(((byte)(69)))));
            this.tslConnection.Name = "tslConnection";
            this.tslConnection.Size = new System.Drawing.Size(102, 17);
            this.tslConnection.Text = "🟢 Connesso a Jira";
            // 
            // tslResults
            // 
            this.tslResults.Name = "tslResults";
            this.tslResults.Size = new System.Drawing.Size(990, 17);
            this.tslResults.Spring = true;
            this.tslResults.Text = "📊 0 membri";
            // 
            // tslLastUpdate
            // 
            this.tslLastUpdate.Name = "tslLastUpdate";
            this.tslLastUpdate.Size = new System.Drawing.Size(93, 17);
            this.tslLastUpdate.Text = "⏱️ Ultimo agg: --";
            this.tslLastUpdate.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // prgLoading
            // 
            this.prgLoading.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.prgLoading.Location = new System.Drawing.Point(0, 620);
            this.prgLoading.Name = "prgLoading";
            this.prgLoading.Size = new System.Drawing.Size(1200, 5);
            this.prgLoading.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.prgLoading.TabIndex = 3;
            this.prgLoading.Visible = false;
            // 
            // OrganizationMembersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 647);
            this.Controls.Add(this.dgvOrganizationMembers);
            this.Controls.Add(this.prgLoading);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.Name = "OrganizationMembersForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Membri Organizzazioni Jira";
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOrganizationMembers)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.TextBox txtFilter;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.DataGridView dgvOrganizationMembers;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tslConnection;
        private System.Windows.Forms.ToolStripStatusLabel tslResults;
        private System.Windows.Forms.ToolStripStatusLabel tslLastUpdate;
        private System.Windows.Forms.ProgressBar prgLoading;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrganizzazione;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNome;
        private System.Windows.Forms.DataGridViewTextBoxColumn colEmail;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNumeroTicket;
        private System.Windows.Forms.DataGridViewTextBoxColumn colAttivo;
    }
}