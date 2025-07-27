using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        #region Control Declarations
        private Panel pnlHeader;
        private Panel pnlMain;
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlToolbar;
        private Panel pnlFilters;
        private Panel pnlNavigation;
        private DataGridView dgvTickets;
        private StatusStrip statusStrip1;

        // Header controls
        private Label lblAppTitle;
        private Label lblSubTitle;
        private Label lblConnectionStatus;

        // Sidebar controls
        private Button btnSidebarToggle;
        private Panel pnlSidebarContent;

        // Toolbar controls
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnRefresh;
        private Button btnExportExcel;
        private Button btnDashboard;
        private Button btnJiraAutomation;
        private Button btnTest;
        private Button btnConfig;

        // Controlli filtro date
        private Label lblCreatoCompletato;
        private DateTimePicker dtpCreatoDA;
        private DateTimePicker dtpCreatoA;
        private DateTimePicker dtpCompletatoDA;
        private DateTimePicker dtpCompletatoA;


        // Filter controls
        private RadioButton rbBasicMode;
        private RadioButton rbJQLMode;
        private ComboBox cmbCliente;
        private ComboBox cmbArea;
        private ComboBox cmbApplicativo;
        private ComboBox cmbAssegnatario;
        private ComboBox cmbTipo;
        private ComboBox cmbStato;
        private ComboBox cmbPriorita;
        private TextBox txtJQLQuery;

        // Navigation controls
        private Label lblResults;
        private Button btnFirstPage;
        private Button btnPreviousPage;
        private Button btnNextPage;
        private Button btnLastPage;
        private ComboBox cmbPageSize;

        // Status bar controls
        private ToolStripStatusLabel tslConnection;
        private ToolStripStatusLabel tslResults;
        private ToolStripStatusLabel tslLastUpdate;
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
            DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
            DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
            pnlHeader = new Panel();
            lblConnectionStatus = new Label();
            lblSubTitle = new Label();
            lblAppTitle = new Label();
            pnlMain = new Panel();
            pnlContent = new Panel();
            dgvTickets = new DataGridView();
            pnlFilters = new Panel();
            txtJQLQuery = new TextBox();
            rbDate = new RadioButton();
            pnlDate = new Panel();
            dtpCreatoDA = new DateTimePicker();
            lblFreccia2 = new Label();
            lblCreatoCompletato = new Label();
            lblFreccia1 = new Label();
            dtpCompletatoA = new DateTimePicker();
            dtpCompletatoDA = new DateTimePicker();
            dtpCreatoA = new DateTimePicker();
            cmbApplicativo = new ComboBox();
            btnSearchFilter = new Button();
            cmbPriorita = new ComboBox();
            cmbStato = new ComboBox();
            cmbTipo = new ComboBox();
            cmbAssegnatario = new ComboBox();
            cmbArea = new ComboBox();
            cmbCliente = new ComboBox();
            rbJQLMode = new RadioButton();
            rbBasicMode = new RadioButton();
            pnlToolbar = new Panel();
            btnConfig = new Button();
            btnTest = new Button();
            btnJiraAutomation = new Button();
            btnPulisci = new Button();
            btnDashboard = new Button();
            btnExportExcel = new Button();
            btnRefresh = new Button();
            btnSearch = new Button();
            txtSearch = new TextBox();
            pnlNavigation = new Panel();
            cmbPageSize = new ComboBox();
            btnLastPage = new Button();
            btnNextPage = new Button();
            btnPreviousPage = new Button();
            btnFirstPage = new Button();
            lblResults = new Label();
            pnlSidebar = new Panel();
            pnlSidebarContent = new Panel();
            btnSidebarToggle = new Button();
            statusStrip1 = new StatusStrip();
            tslConnection = new ToolStripStatusLabel();
            tslResults = new ToolStripStatusLabel();
            tslLastUpdate = new ToolStripStatusLabel();
            pnlHeader.SuspendLayout();
            pnlMain.SuspendLayout();
            pnlContent.SuspendLayout();
            ((ISupportInitialize)dgvTickets).BeginInit();
            pnlFilters.SuspendLayout();
            pnlDate.SuspendLayout();
            pnlToolbar.SuspendLayout();
            pnlNavigation.SuspendLayout();
            pnlSidebar.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(248, 249, 250);
            pnlHeader.Controls.Add(lblConnectionStatus);
            pnlHeader.Controls.Add(lblSubTitle);
            pnlHeader.Controls.Add(lblAppTitle);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(2142, 50);
            pnlHeader.TabIndex = 0;
            // 
            // lblConnectionStatus
            // 
            lblConnectionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblConnectionStatus.AutoSize = true;
            lblConnectionStatus.Font = new Font("Segoe UI", 8F);
            lblConnectionStatus.ForeColor = Color.FromArgb(40, 167, 69);
            lblConnectionStatus.Location = new Point(2022, 18);
            lblConnectionStatus.Name = "lblConnectionStatus";
            lblConnectionStatus.Size = new Size(87, 13);
            lblConnectionStatus.TabIndex = 2;
            lblConnectionStatus.Text = "Connesso a Jira";
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new Font("Segoe UI", 8F);
            lblSubTitle.ForeColor = Color.FromArgb(108, 117, 125);
            lblSubTitle.Location = new Point(20, 29);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new Size(187, 13);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "Sistema di gestione ticket avanzato";
            // 
            // lblAppTitle
            // 
            lblAppTitle.AutoSize = true;
            lblAppTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblAppTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblAppTitle.Location = new Point(20, 8);
            lblAppTitle.Name = "lblAppTitle";
            lblAppTitle.Size = new Size(159, 21);
            lblAppTitle.TabIndex = 0;
            lblAppTitle.Text = "Jira Ticket Manager";
            // 
            // pnlMain
            // 
            pnlMain.Controls.Add(pnlContent);
            pnlMain.Controls.Add(pnlSidebar);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 50);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(2142, 828);
            pnlMain.TabIndex = 1;
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.White;
            pnlContent.Controls.Add(dgvTickets);
            pnlContent.Controls.Add(pnlFilters);
            pnlContent.Controls.Add(pnlToolbar);
            pnlContent.Controls.Add(pnlNavigation);
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(40, 0);
            pnlContent.Name = "pnlContent";
            pnlContent.Size = new Size(2102, 828);
            pnlContent.TabIndex = 1;
            // 
            // dgvTickets
            // 
            dgvTickets.AllowUserToAddRows = false;
            dgvTickets.AllowUserToDeleteRows = false;
            dgvTickets.AllowUserToResizeRows = false;
            dgvTickets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTickets.BackgroundColor = Color.White;
            dgvTickets.BorderStyle = BorderStyle.None;
            dgvTickets.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvTickets.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dataGridViewCellStyle3.ForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(248, 249, 250);
            dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
            dgvTickets.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dgvTickets.ColumnHeadersHeight = 40;
            dgvTickets.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = Color.White;
            dataGridViewCellStyle4.Font = new Font("Segoe UI", 8F);
            dataGridViewCellStyle4.ForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle4.SelectionBackColor = Color.FromArgb(233, 246, 255);
            dataGridViewCellStyle4.SelectionForeColor = Color.FromArgb(52, 73, 94);
            dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
            dgvTickets.DefaultCellStyle = dataGridViewCellStyle4;
            dgvTickets.Dock = DockStyle.Fill;
            dgvTickets.EnableHeadersVisualStyles = false;
            dgvTickets.GridColor = Color.FromArgb(226, 232, 240);
            dgvTickets.Location = new Point(0, 133);
            dgvTickets.Name = "dgvTickets";
            dgvTickets.ReadOnly = true;
            dgvTickets.RowHeadersVisible = false;
            dgvTickets.RowTemplate.Height = 35;
            dgvTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTickets.Size = new Size(2102, 645);
            dgvTickets.TabIndex = 2;
            // 
            // pnlFilters
            // 
            pnlFilters.BackColor = Color.FromArgb(248, 249, 250);
            pnlFilters.BorderStyle = BorderStyle.FixedSingle;
            pnlFilters.Controls.Add(txtJQLQuery);
            pnlFilters.Controls.Add(rbDate);
            pnlFilters.Controls.Add(pnlDate);
            pnlFilters.Controls.Add(cmbApplicativo);
            pnlFilters.Controls.Add(btnSearchFilter);
            pnlFilters.Controls.Add(cmbPriorita);
            pnlFilters.Controls.Add(cmbStato);
            pnlFilters.Controls.Add(cmbTipo);
            pnlFilters.Controls.Add(cmbAssegnatario);
            pnlFilters.Controls.Add(cmbArea);
            pnlFilters.Controls.Add(cmbCliente);
            pnlFilters.Controls.Add(rbJQLMode);
            pnlFilters.Controls.Add(rbBasicMode);
            pnlFilters.Dock = DockStyle.Top;
            pnlFilters.Location = new Point(0, 50);
            pnlFilters.Name = "pnlFilters";
            pnlFilters.Size = new Size(2102, 83);
            pnlFilters.TabIndex = 1;
            // 
            // txtJQLQuery
            // 
            txtJQLQuery.Font = new Font("Consolas", 9F);
            txtJQLQuery.Location = new Point(17, 31);
            txtJQLQuery.Multiline = true;
            txtJQLQuery.Name = "txtJQLQuery";
            txtJQLQuery.PlaceholderText = "Inserisci query JQL...";
            txtJQLQuery.ScrollBars = ScrollBars.Vertical;
            txtJQLQuery.Size = new Size(1474, 39);
            txtJQLQuery.TabIndex = 10;
            txtJQLQuery.Visible = false;
            // 
            // rbDate
            // 
            rbDate.AutoSize = true;
            rbDate.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            rbDate.ForeColor = Color.FromArgb(52, 73, 94);
            rbDate.Location = new Point(120, 8);
            rbDate.Name = "rbDate";
            rbDate.Size = new Size(99, 17);
            rbDate.TabIndex = 31;
            rbDate.Text = "Modalità Date";
            rbDate.UseVisualStyleBackColor = true;
            // 
            // pnlDate
            // 
            pnlDate.Controls.Add(dtpCreatoDA);
            pnlDate.Controls.Add(lblFreccia2);
            pnlDate.Controls.Add(lblCreatoCompletato);
            pnlDate.Controls.Add(lblFreccia1);
            pnlDate.Controls.Add(dtpCompletatoA);
            pnlDate.Controls.Add(dtpCompletatoDA);
            pnlDate.Controls.Add(dtpCreatoA);
            pnlDate.Location = new Point(1573, 5);
            pnlDate.Name = "pnlDate";
            pnlDate.Size = new Size(218, 79);
            pnlDate.TabIndex = 11;
            // 
            // dtpCreatoDA
            // 
            dtpCreatoDA.Checked = false;
            dtpCreatoDA.Format = DateTimePickerFormat.Short;
            dtpCreatoDA.Location = new Point(3, 23);
            dtpCreatoDA.Name = "dtpCreatoDA";
            dtpCreatoDA.ShowCheckBox = true;
            dtpCreatoDA.Size = new Size(90, 23);
            dtpCreatoDA.TabIndex = 21;
            // 
            // lblFreccia2
            // 
            lblFreccia2.AutoSize = true;
            lblFreccia2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFreccia2.Location = new Point(99, 58);
            lblFreccia2.Name = "lblFreccia2";
            lblFreccia2.Size = new Size(17, 15);
            lblFreccia2.TabIndex = 29;
            lblFreccia2.Text = "→";
            // 
            // lblCreatoCompletato
            // 
            lblCreatoCompletato.AutoSize = true;
            lblCreatoCompletato.Font = new Font("Segoe UI", 9F);
            lblCreatoCompletato.Location = new Point(58, 5);
            lblCreatoCompletato.Name = "lblCreatoCompletato";
            lblCreatoCompletato.Size = new Size(110, 15);
            lblCreatoCompletato.TabIndex = 20;
            lblCreatoCompletato.Text = "Creato/Completato";
            // 
            // lblFreccia1
            // 
            lblFreccia1.AutoSize = true;
            lblFreccia1.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFreccia1.Location = new Point(99, 29);
            lblFreccia1.Name = "lblFreccia1";
            lblFreccia1.Size = new Size(17, 15);
            lblFreccia1.TabIndex = 22;
            lblFreccia1.Text = "→";
            // 
            // dtpCompletatoA
            // 
            dtpCompletatoA.Checked = false;
            dtpCompletatoA.Format = DateTimePickerFormat.Short;
            dtpCompletatoA.Location = new Point(122, 52);
            dtpCompletatoA.Name = "dtpCompletatoA";
            dtpCompletatoA.ShowCheckBox = true;
            dtpCompletatoA.Size = new Size(90, 23);
            dtpCompletatoA.TabIndex = 27;
            // 
            // dtpCompletatoDA
            // 
            dtpCompletatoDA.Checked = false;
            dtpCompletatoDA.Format = DateTimePickerFormat.Short;
            dtpCompletatoDA.Location = new Point(3, 52);
            dtpCompletatoDA.Name = "dtpCompletatoDA";
            dtpCompletatoDA.ShowCheckBox = true;
            dtpCompletatoDA.Size = new Size(90, 23);
            dtpCompletatoDA.TabIndex = 25;
            // 
            // dtpCreatoA
            // 
            dtpCreatoA.Checked = false;
            dtpCreatoA.Format = DateTimePickerFormat.Short;
            dtpCreatoA.Location = new Point(122, 23);
            dtpCreatoA.Name = "dtpCreatoA";
            dtpCreatoA.ShowCheckBox = true;
            dtpCreatoA.Size = new Size(90, 23);
            dtpCreatoA.TabIndex = 23;
            // 
            // cmbApplicativo
            // 
            cmbApplicativo.BackColor = Color.White;
            cmbApplicativo.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbApplicativo.Font = new Font("Segoe UI", 9F);
            cmbApplicativo.ForeColor = Color.FromArgb(73, 80, 87);
            cmbApplicativo.Items.AddRange(new object[] { "Tutti Applicativo", "App1", "App2", "App3" });
            cmbApplicativo.Location = new Point(699, 35);
            cmbApplicativo.Name = "cmbApplicativo";
            cmbApplicativo.Size = new Size(317, 23);
            cmbApplicativo.TabIndex = 4;
            // 
            // btnSearchFilter
            // 
            btnSearchFilter.BackColor = Color.FromArgb(0, 120, 212);
            btnSearchFilter.FlatAppearance.BorderSize = 0;
            btnSearchFilter.FlatStyle = FlatStyle.Flat;
            btnSearchFilter.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSearchFilter.ForeColor = Color.White;
            btnSearchFilter.Location = new Point(1497, 36);
            btnSearchFilter.Name = "btnSearchFilter";
            btnSearchFilter.Size = new Size(70, 23);
            btnSearchFilter.TabIndex = 10;
            btnSearchFilter.Text = "🔍 Cerca";
            btnSearchFilter.UseVisualStyleBackColor = false;
            // 
            // cmbPriorita
            // 
            cmbPriorita.BackColor = Color.White;
            cmbPriorita.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPriorita.Font = new Font("Segoe UI", 9F);
            cmbPriorita.ForeColor = Color.FromArgb(73, 80, 87);
            cmbPriorita.Items.AddRange(new object[] { "Tutti Priorità", "Bassa", "Media", "Alta" });
            cmbPriorita.Location = new Point(1351, 36);
            cmbPriorita.Name = "cmbPriorita";
            cmbPriorita.Size = new Size(140, 23);
            cmbPriorita.TabIndex = 8;
            // 
            // cmbStato
            // 
            cmbStato.BackColor = Color.White;
            cmbStato.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStato.Font = new Font("Segoe UI", 9F);
            cmbStato.ForeColor = Color.FromArgb(73, 80, 87);
            cmbStato.Items.AddRange(new object[] { "Tutti Stato", "Aperto", "In Corso", "Completato" });
            cmbStato.Location = new Point(230, 35);
            cmbStato.Name = "cmbStato";
            cmbStato.Size = new Size(140, 23);
            cmbStato.TabIndex = 7;
            // 
            // cmbTipo
            // 
            cmbTipo.BackColor = Color.White;
            cmbTipo.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipo.Font = new Font("Segoe UI", 9F);
            cmbTipo.ForeColor = Color.FromArgb(73, 80, 87);
            cmbTipo.Items.AddRange(new object[] { "Tutti Tipo", "Bug", "Feature", "Task" });
            cmbTipo.Location = new Point(1168, 36);
            cmbTipo.Name = "cmbTipo";
            cmbTipo.Size = new Size(177, 23);
            cmbTipo.TabIndex = 6;
            // 
            // cmbAssegnatario
            // 
            cmbAssegnatario.BackColor = Color.White;
            cmbAssegnatario.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAssegnatario.Font = new Font("Segoe UI", 9F);
            cmbAssegnatario.ForeColor = Color.FromArgb(73, 80, 87);
            cmbAssegnatario.Items.AddRange(new object[] { "Tutti Assegnatario", "Mario Rossi", "Luigi Verdi", "Anna Bianchi" });
            cmbAssegnatario.Location = new Point(1022, 35);
            cmbAssegnatario.Name = "cmbAssegnatario";
            cmbAssegnatario.Size = new Size(140, 23);
            cmbAssegnatario.TabIndex = 5;
            // 
            // cmbArea
            // 
            cmbArea.BackColor = Color.White;
            cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbArea.Font = new Font("Segoe UI", 9F);
            cmbArea.ForeColor = Color.FromArgb(73, 80, 87);
            cmbArea.Items.AddRange(new object[] { "Tutti Area", "Sviluppo", "Testing", "Produzione" });
            cmbArea.Location = new Point(376, 35);
            cmbArea.Name = "cmbArea";
            cmbArea.Size = new Size(317, 23);
            cmbArea.TabIndex = 3;
            // 
            // cmbCliente
            // 
            cmbCliente.BackColor = Color.White;
            cmbCliente.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCliente.Font = new Font("Segoe UI", 9F);
            cmbCliente.ForeColor = Color.FromArgb(73, 80, 87);
            cmbCliente.Items.AddRange(new object[] { "Tutti Cliente", "Cliente A", "Cliente B", "Cliente C" });
            cmbCliente.Location = new Point(20, 35);
            cmbCliente.Name = "cmbCliente";
            cmbCliente.Size = new Size(204, 23);
            cmbCliente.TabIndex = 2;
            // 
            // rbJQLMode
            // 
            rbJQLMode.AutoSize = true;
            rbJQLMode.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            rbJQLMode.ForeColor = Color.FromArgb(52, 73, 94);
            rbJQLMode.Location = new Point(225, 8);
            rbJQLMode.Name = "rbJQLMode";
            rbJQLMode.Size = new Size(94, 17);
            rbJQLMode.TabIndex = 1;
            rbJQLMode.Text = "Modalità JQL";
            rbJQLMode.UseVisualStyleBackColor = true;
            // 
            // rbBasicMode
            // 
            rbBasicMode.AutoSize = true;
            rbBasicMode.Checked = true;
            rbBasicMode.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            rbBasicMode.ForeColor = Color.FromArgb(52, 73, 94);
            rbBasicMode.Location = new Point(20, 8);
            rbBasicMode.Name = "rbBasicMode";
            rbBasicMode.Size = new Size(99, 17);
            rbBasicMode.TabIndex = 0;
            rbBasicMode.TabStop = true;
            rbBasicMode.Text = "Modalità Base";
            rbBasicMode.UseVisualStyleBackColor = true;
            // 
            // pnlToolbar
            // 
            pnlToolbar.BackColor = Color.FromArgb(248, 249, 250);
            pnlToolbar.BorderStyle = BorderStyle.FixedSingle;
            pnlToolbar.Controls.Add(btnConfig);
            pnlToolbar.Controls.Add(btnTest);
            pnlToolbar.Controls.Add(btnJiraAutomation);
            pnlToolbar.Controls.Add(btnPulisci);
            pnlToolbar.Controls.Add(btnDashboard);
            pnlToolbar.Controls.Add(btnExportExcel);
            pnlToolbar.Controls.Add(btnRefresh);
            pnlToolbar.Controls.Add(btnSearch);
            pnlToolbar.Controls.Add(txtSearch);
            pnlToolbar.Dock = DockStyle.Top;
            pnlToolbar.Location = new Point(0, 0);
            pnlToolbar.Name = "pnlToolbar";
            pnlToolbar.Size = new Size(2102, 50);
            pnlToolbar.TabIndex = 0;
            // 
            // btnConfig
            // 
            btnConfig.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnConfig.BackColor = Color.FromArgb(108, 117, 125);
            btnConfig.FlatAppearance.BorderSize = 0;
            btnConfig.FlatStyle = FlatStyle.Flat;
            btnConfig.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnConfig.ForeColor = Color.White;
            btnConfig.Location = new Point(1972, 14);
            btnConfig.Name = "btnConfig";
            btnConfig.Size = new Size(70, 23);
            btnConfig.TabIndex = 7;
            btnConfig.Text = "⚙️ Config";
            btnConfig.UseVisualStyleBackColor = false;
            // 
            // btnTest
            // 
            btnTest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTest.BackColor = Color.FromArgb(217, 119, 6);
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnTest.ForeColor = Color.White;
            btnTest.Location = new Point(1902, 14);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(60, 23);
            btnTest.TabIndex = 6;
            btnTest.Text = "🔧 Test";
            btnTest.UseVisualStyleBackColor = false;
            // 
            // btnJiraAutomation
            // 
            btnJiraAutomation.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnJiraAutomation.BackColor = Color.FromArgb(124, 58, 237);
            btnJiraAutomation.FlatAppearance.BorderSize = 0;
            btnJiraAutomation.FlatStyle = FlatStyle.Flat;
            btnJiraAutomation.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnJiraAutomation.ForeColor = Color.White;
            btnJiraAutomation.Location = new Point(1792, 14);
            btnJiraAutomation.Name = "btnJiraAutomation";
            btnJiraAutomation.Size = new Size(100, 23);
            btnJiraAutomation.TabIndex = 5;
            btnJiraAutomation.Text = "🤖 Automation";
            btnJiraAutomation.UseVisualStyleBackColor = false;
            // 
            // btnPulisci
            // 
            btnPulisci.BackColor = Color.FromArgb(0, 120, 212);
            btnPulisci.FlatAppearance.BorderSize = 0;
            btnPulisci.FlatStyle = FlatStyle.Flat;
            btnPulisci.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnPulisci.ForeColor = Color.White;
            btnPulisci.Location = new Point(356, 15);
            btnPulisci.Name = "btnPulisci";
            btnPulisci.Size = new Size(70, 23);
            btnPulisci.TabIndex = 30;
            btnPulisci.Text = "Pulisci";
            btnPulisci.UseVisualStyleBackColor = false;
            // 
            // btnDashboard
            // 
            btnDashboard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDashboard.BackColor = Color.FromArgb(8, 145, 178);
            btnDashboard.FlatAppearance.BorderSize = 0;
            btnDashboard.FlatStyle = FlatStyle.Flat;
            btnDashboard.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnDashboard.ForeColor = Color.White;
            btnDashboard.Location = new Point(1692, 14);
            btnDashboard.Name = "btnDashboard";
            btnDashboard.Size = new Size(90, 23);
            btnDashboard.TabIndex = 4;
            btnDashboard.Text = "📈 Dashboard";
            btnDashboard.UseVisualStyleBackColor = false;
            // 
            // btnExportExcel
            // 
            btnExportExcel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportExcel.BackColor = Color.FromArgb(22, 163, 74);
            btnExportExcel.FlatAppearance.BorderSize = 0;
            btnExportExcel.FlatStyle = FlatStyle.Flat;
            btnExportExcel.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnExportExcel.ForeColor = Color.White;
            btnExportExcel.Location = new Point(1592, 14);
            btnExportExcel.Name = "btnExportExcel";
            btnExportExcel.Size = new Size(90, 23);
            btnExportExcel.TabIndex = 3;
            btnExportExcel.Text = "📊 Export Excel";
            btnExportExcel.UseVisualStyleBackColor = false;
            // 
            // btnRefresh
            // 
            btnRefresh.BackColor = Color.FromArgb(0, 120, 212);
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.FlatStyle = FlatStyle.Flat;
            btnRefresh.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Location = new Point(432, 15);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(80, 23);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "🔄 Aggiorna";
            btnRefresh.UseVisualStyleBackColor = false;
            // 
            // btnSearch
            // 
            btnSearch.BackColor = Color.FromArgb(0, 120, 212);
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnSearch.ForeColor = Color.White;
            btnSearch.Location = new Point(280, 14);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(70, 23);
            btnSearch.TabIndex = 1;
            btnSearch.Text = "🔍 Cerca";
            btnSearch.UseVisualStyleBackColor = false;
            // 
            // txtSearch
            // 
            txtSearch.Font = new Font("Segoe UI", 9F);
            txtSearch.Location = new Point(20, 14);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Cerca ticket...";
            txtSearch.Size = new Size(250, 23);
            txtSearch.TabIndex = 0;
            // 
            // pnlNavigation
            // 
            pnlNavigation.BackColor = Color.FromArgb(248, 249, 250);
            pnlNavigation.BorderStyle = BorderStyle.FixedSingle;
            pnlNavigation.Controls.Add(cmbPageSize);
            pnlNavigation.Controls.Add(btnLastPage);
            pnlNavigation.Controls.Add(btnNextPage);
            pnlNavigation.Controls.Add(btnPreviousPage);
            pnlNavigation.Controls.Add(btnFirstPage);
            pnlNavigation.Controls.Add(lblResults);
            pnlNavigation.Dock = DockStyle.Bottom;
            pnlNavigation.Location = new Point(0, 778);
            pnlNavigation.Name = "pnlNavigation";
            pnlNavigation.Size = new Size(2102, 50);
            pnlNavigation.TabIndex = 3;
            // 
            // cmbPageSize
            // 
            cmbPageSize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbPageSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPageSize.Font = new Font("Segoe UI", 8F);
            cmbPageSize.FormattingEnabled = true;
            cmbPageSize.Items.AddRange(new object[] { "25", "50", "100" });
            cmbPageSize.Location = new Point(2022, 15);
            cmbPageSize.Name = "cmbPageSize";
            cmbPageSize.Size = new Size(60, 21);
            cmbPageSize.TabIndex = 5;
            // 
            // btnLastPage
            // 
            btnLastPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLastPage.BackColor = Color.White;
            btnLastPage.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnLastPage.FlatStyle = FlatStyle.Flat;
            btnLastPage.Font = new Font("Segoe UI", 8F);
            btnLastPage.ForeColor = Color.FromArgb(52, 73, 94);
            btnLastPage.Location = new Point(1984, 15);
            btnLastPage.Name = "btnLastPage";
            btnLastPage.Size = new Size(32, 28);
            btnLastPage.TabIndex = 4;
            btnLastPage.Text = "⏭️";
            btnLastPage.UseVisualStyleBackColor = false;
            // 
            // btnNextPage
            // 
            btnNextPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNextPage.BackColor = Color.White;
            btnNextPage.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnNextPage.FlatStyle = FlatStyle.Flat;
            btnNextPage.Font = new Font("Segoe UI", 8F);
            btnNextPage.ForeColor = Color.FromArgb(52, 73, 94);
            btnNextPage.Location = new Point(1946, 15);
            btnNextPage.Name = "btnNextPage";
            btnNextPage.Size = new Size(32, 28);
            btnNextPage.TabIndex = 3;
            btnNextPage.Text = "▶️";
            btnNextPage.UseVisualStyleBackColor = false;
            // 
            // btnPreviousPage
            // 
            btnPreviousPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPreviousPage.BackColor = Color.White;
            btnPreviousPage.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnPreviousPage.FlatStyle = FlatStyle.Flat;
            btnPreviousPage.Font = new Font("Segoe UI", 8F);
            btnPreviousPage.ForeColor = Color.FromArgb(52, 73, 94);
            btnPreviousPage.Location = new Point(1908, 15);
            btnPreviousPage.Name = "btnPreviousPage";
            btnPreviousPage.Size = new Size(32, 28);
            btnPreviousPage.TabIndex = 2;
            btnPreviousPage.Text = "◀️";
            btnPreviousPage.UseVisualStyleBackColor = false;
            // 
            // btnFirstPage
            // 
            btnFirstPage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFirstPage.BackColor = Color.White;
            btnFirstPage.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
            btnFirstPage.FlatStyle = FlatStyle.Flat;
            btnFirstPage.Font = new Font("Segoe UI", 8F);
            btnFirstPage.ForeColor = Color.FromArgb(52, 73, 94);
            btnFirstPage.Location = new Point(1870, 15);
            btnFirstPage.Name = "btnFirstPage";
            btnFirstPage.Size = new Size(32, 28);
            btnFirstPage.TabIndex = 1;
            btnFirstPage.Text = "⏮️";
            btnFirstPage.UseVisualStyleBackColor = false;
            // 
            // lblResults
            // 
            lblResults.AutoSize = true;
            lblResults.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblResults.ForeColor = Color.FromArgb(52, 73, 94);
            lblResults.Location = new Point(20, 16);
            lblResults.Name = "lblResults";
            lblResults.Size = new Size(86, 15);
            lblResults.TabIndex = 0;
            lblResults.Text = "Carico ticket...";
            // 
            // pnlSidebar
            // 
            pnlSidebar.BackColor = Color.White;
            pnlSidebar.BorderStyle = BorderStyle.FixedSingle;
            pnlSidebar.Controls.Add(pnlSidebarContent);
            pnlSidebar.Controls.Add(btnSidebarToggle);
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Name = "pnlSidebar";
            pnlSidebar.Size = new Size(40, 828);
            pnlSidebar.TabIndex = 0;
            // 
            // pnlSidebarContent
            // 
            pnlSidebarContent.AutoScroll = true;
            pnlSidebarContent.Dock = DockStyle.Fill;
            pnlSidebarContent.Location = new Point(0, 48);
            pnlSidebarContent.Name = "pnlSidebarContent";
            pnlSidebarContent.Size = new Size(38, 778);
            pnlSidebarContent.TabIndex = 1;
            pnlSidebarContent.Visible = false;
            // 
            // btnSidebarToggle
            // 
            btnSidebarToggle.BackColor = Color.FromArgb(0, 120, 212);
            btnSidebarToggle.Dock = DockStyle.Top;
            btnSidebarToggle.FlatAppearance.BorderSize = 0;
            btnSidebarToggle.FlatStyle = FlatStyle.Flat;
            btnSidebarToggle.Font = new Font("Segoe UI", 12F);
            btnSidebarToggle.ForeColor = Color.White;
            btnSidebarToggle.Location = new Point(0, 0);
            btnSidebarToggle.Name = "btnSidebarToggle";
            btnSidebarToggle.Size = new Size(38, 48);
            btnSidebarToggle.TabIndex = 0;
            btnSidebarToggle.Text = "≡";
            btnSidebarToggle.UseVisualStyleBackColor = false;
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.FromArgb(248, 249, 250);
            statusStrip1.Font = new Font("Segoe UI", 8F);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tslConnection, tslResults, tslLastUpdate });
            statusStrip1.Location = new Point(0, 878);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(2142, 22);
            statusStrip1.TabIndex = 2;
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
            tslResults.Size = new Size(1838, 17);
            tslResults.Spring = true;
            tslResults.Text = "📊 234 risultati";
            // 
            // tslLastUpdate
            // 
            tslLastUpdate.Name = "tslLastUpdate";
            tslLastUpdate.Size = new Size(187, 17);
            tslLastUpdate.Text = "🕒 Ultimo aggiornamento: 20:13:08";
            tslLastUpdate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 249, 250);
            ClientSize = new Size(2142, 900);
            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);
            Controls.Add(statusStrip1);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🎫 Jira Ticket Manager";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlMain.ResumeLayout(false);
            pnlContent.ResumeLayout(false);
            ((ISupportInitialize)dgvTickets).EndInit();
            pnlFilters.ResumeLayout(false);
            pnlFilters.PerformLayout();
            pnlDate.ResumeLayout(false);
            pnlDate.PerformLayout();
            pnlToolbar.ResumeLayout(false);
            pnlToolbar.PerformLayout();
            pnlNavigation.ResumeLayout(false);
            pnlNavigation.PerformLayout();
            pnlSidebar.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSearchFilter;
        private Label lblCompletato;
        private Label lblFreccia1;
        private Button btnPulisci;
        private Label lblFreccia2;
        private Panel pnlDate;
        private RadioButton rbDate;
    }
}