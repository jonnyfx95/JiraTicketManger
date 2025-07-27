namespace JiraTicketManager.Forms
{
    partial class FrmDettaglio
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
            pnlHeader = new Panel();
            pnlHeaderRight = new Panel();
            pnlHeaderLeft = new Panel();
            tsMain = new ToolStrip();
            tslSearch = new ToolStripLabel();
            tstbTicketSearch = new ToolStripTextBox();
            tsbSearch = new ToolStripButton();
            tssSearchSeparator = new ToolStripSeparator();
            tsbRefresh = new ToolStripButton();
            tsbOpenJira = new ToolStripButton();
            pnlMain = new Panel();
            pnlCenter = new Panel();
            pnlAttivita = new Panel();
            tabAttivita = new TabControl();
            tabCommenti = new TabPage();
            lvCommenti = new ListView();
            tabCronologia = new TabPage();
            tabAllegati = new TabPage();
            lblAttivitaTitolo = new Label();
            splitterCentral = new Splitter();
            pnlDescrizione = new Panel();
            txtDescrizione = new TextBox();
            lblDescrizioneTitolo = new Label();
            pnlRight = new Panel();
            pnlAnteprima = new Panel();
            tbAnteprimaEmail = new TabControl();
            tpAnteprimaEmail = new TabPage();
            cmbTemplate = new ComboBox();
            txtCorpoEmail = new TextBox();
            tpCommento = new TabPage();
            txtCommento = new TextBox();
            lblAnteprimaTitolo = new Label();
            pnlAssegnazione = new Panel();
            txtPriorityLab = new TextBox();
            lblPriorityLab = new Label();
            txtAssegnatario = new TextBox();
            lblAssegnatario = new Label();
            lblAssegnazioneTitolo = new Label();
            pnlClienteApp = new Panel();
            txtClientePartner = new TextBox();
            lblClientePartner = new Label();
            txtApplicativo = new TextBox();
            lblApplicativo = new Label();
            txtArea = new TextBox();
            lblArea = new Label();
            txtCliente = new TextBox();
            lblCliente = new Label();
            lblClienteAppTitolo = new Label();
            pnlLeft = new Panel();
            pnlPianificazione = new Panel();
            txtDataCompletamento = new TextBox();
            lblDataCompletamento = new Label();
            cmbConsulente = new ComboBox();
            lblConsulente = new Label();
            txtWBS = new TextBox();
            lblWBS = new Label();
            txtResponsabile = new TextBox();
            lblResponsabile = new Label();
            txtPM = new TextBox();
            lblPM = new Label();
            txtCommerciale = new TextBox();
            lblCommerciale = new Label();
            lblPianificazione = new Label();
            pnlDettagliTicket = new Panel();
            txtDataAggiornamento = new TextBox();
            lblDataAggiornamento = new Label();
            txtDataCreazione = new TextBox();
            lblDataCreazione = new Label();
            txtTicketTipo = new TextBox();
            lblTicketTipo = new Label();
            txtEmail = new TextBox();
            lblEmail = new Label();
            txtTelefono = new TextBox();
            lblTelefono = new Label();
            txtRichiedente = new TextBox();
            lblRichiedente = new Label();
            lblDettagliTicketTitolo = new Label();
            pnlAzioni = new Panel();
            btnTest = new Button();
            btnChiudi = new Button();
            btnEsporta = new Button();
            btnAssegnaAMe = new Button();
            btnCommento = new Button();
            btnPianificazione = new Button();
            btnChiudiTicket = new Button();
            pnlStatusBar = new Panel();
            lblStatus = new Label();
            pnlHeader.SuspendLayout();
            pnlHeaderLeft.SuspendLayout();
            tsMain.SuspendLayout();
            pnlMain.SuspendLayout();
            pnlCenter.SuspendLayout();
            pnlAttivita.SuspendLayout();
            tabAttivita.SuspendLayout();
            tabCommenti.SuspendLayout();
            pnlDescrizione.SuspendLayout();
            pnlRight.SuspendLayout();
            pnlAnteprima.SuspendLayout();
            tbAnteprimaEmail.SuspendLayout();
            tpAnteprimaEmail.SuspendLayout();
            tpCommento.SuspendLayout();
            pnlAssegnazione.SuspendLayout();
            pnlClienteApp.SuspendLayout();
            pnlLeft.SuspendLayout();
            pnlPianificazione.SuspendLayout();
            pnlDettagliTicket.SuspendLayout();
            pnlAzioni.SuspendLayout();
            pnlStatusBar.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(248, 249, 250);
            pnlHeader.BorderStyle = BorderStyle.FixedSingle;
            pnlHeader.Controls.Add(pnlHeaderRight);
            pnlHeader.Controls.Add(pnlHeaderLeft);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1184, 90);
            pnlHeader.TabIndex = 0;
            // 
            // pnlHeaderRight
            // 
            pnlHeaderRight.Dock = DockStyle.Right;
            pnlHeaderRight.Location = new Point(848, 0);
            pnlHeaderRight.Name = "pnlHeaderRight";
            pnlHeaderRight.Size = new Size(334, 88);
            pnlHeaderRight.TabIndex = 1;
            // 
            // pnlHeaderLeft
            // 
            pnlHeaderLeft.Controls.Add(tsMain);
            pnlHeaderLeft.Dock = DockStyle.Fill;
            pnlHeaderLeft.Location = new Point(0, 0);
            pnlHeaderLeft.Name = "pnlHeaderLeft";
            pnlHeaderLeft.Size = new Size(1182, 88);
            pnlHeaderLeft.TabIndex = 0;
            // 
            // tsMain
            // 
            tsMain.BackColor = Color.FromArgb(248, 249, 250);
            tsMain.Font = new Font("Segoe UI", 9F);
            tsMain.GripStyle = ToolStripGripStyle.Hidden;
            tsMain.Items.AddRange(new ToolStripItem[] { tslSearch, tstbTicketSearch, tsbSearch, tssSearchSeparator, tsbRefresh, tsbOpenJira });
            tsMain.Location = new Point(0, 0);
            tsMain.Name = "tsMain";
            tsMain.Padding = new Padding(16, 8, 16, 8);
            tsMain.Size = new Size(1182, 39);
            tsMain.TabIndex = 0;
            // 
            // tslSearch
            // 
            tslSearch.ForeColor = Color.FromArgb(52, 73, 94);
            tslSearch.Name = "tslSearch";
            tslSearch.Size = new Size(87, 20);
            tslSearch.Text = "🔍 Cerca ticket:";
            // 
            // tstbTicketSearch
            // 
            tstbTicketSearch.BorderStyle = BorderStyle.FixedSingle;
            tstbTicketSearch.Name = "tstbTicketSearch";
            tstbTicketSearch.Size = new Size(200, 23);
            // 
            // tsbSearch
            // 
            tsbSearch.BackColor = Color.FromArgb(52, 152, 219);
            tsbSearch.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tsbSearch.ForeColor = Color.White;
            tsbSearch.Name = "tsbSearch";
            tsbSearch.Size = new Size(71, 20);
            tsbSearch.Text = "🔄 Refresh";
            // 
            // tssSearchSeparator
            // 
            tssSearchSeparator.Name = "tssSearchSeparator";
            tssSearchSeparator.Size = new Size(6, 23);
            // 
            // tsbRefresh
            // 
            tsbRefresh.BackColor = Color.FromArgb(52, 152, 219);
            tsbRefresh.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tsbRefresh.ForeColor = Color.White;
            tsbRefresh.Name = "tsbRefresh";
            tsbRefresh.Size = new Size(71, 20);
            tsbRefresh.Text = "🔄 Refresh";
            // 
            // tsbOpenJira
            // 
            tsbOpenJira.BackColor = Color.FromArgb(46, 204, 113);
            tsbOpenJira.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            tsbOpenJira.ForeColor = Color.White;
            tsbOpenJira.Name = "tsbOpenJira";
            tsbOpenJira.Size = new Size(85, 20);
            tsbOpenJira.Text = "🌐 Apri in Jira";
            // 
            // pnlMain
            // 
            pnlMain.BackColor = Color.White;
            pnlMain.Controls.Add(pnlCenter);
            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlLeft);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 90);
            pnlMain.Name = "pnlMain";
            pnlMain.Padding = new Padding(8);
            pnlMain.Size = new Size(1184, 581);
            pnlMain.TabIndex = 1;
            // 
            // pnlCenter
            // 
            pnlCenter.BackColor = Color.White;
            pnlCenter.BorderStyle = BorderStyle.FixedSingle;
            pnlCenter.Controls.Add(pnlAttivita);
            pnlCenter.Controls.Add(splitterCentral);
            pnlCenter.Controls.Add(pnlDescrizione);
            pnlCenter.Dock = DockStyle.Fill;
            pnlCenter.Location = new Point(328, 8);
            pnlCenter.Name = "pnlCenter";
            pnlCenter.Padding = new Padding(8);
            pnlCenter.Size = new Size(528, 565);
            pnlCenter.TabIndex = 1;
            // 
            // pnlAttivita
            // 
            pnlAttivita.BackColor = Color.White;
            pnlAttivita.BorderStyle = BorderStyle.FixedSingle;
            pnlAttivita.Controls.Add(tabAttivita);
            pnlAttivita.Controls.Add(lblAttivitaTitolo);
            pnlAttivita.Dock = DockStyle.Bottom;
            pnlAttivita.Location = new Point(8, 301);
            pnlAttivita.Name = "pnlAttivita";
            pnlAttivita.Padding = new Padding(16);
            pnlAttivita.Size = new Size(510, 248);
            pnlAttivita.TabIndex = 2;
            // 
            // tabAttivita
            // 
            tabAttivita.Controls.Add(tabCommenti);
            tabAttivita.Controls.Add(tabCronologia);
            tabAttivita.Controls.Add(tabAllegati);
            tabAttivita.Dock = DockStyle.Fill;
            tabAttivita.Font = new Font("Segoe UI", 9F);
            tabAttivita.Location = new Point(16, 44);
            tabAttivita.Name = "tabAttivita";
            tabAttivita.SelectedIndex = 0;
            tabAttivita.Size = new Size(476, 186);
            tabAttivita.TabIndex = 1;
            // 
            // tabCommenti
            // 
            tabCommenti.BackColor = Color.White;
            tabCommenti.Controls.Add(lvCommenti);
            tabCommenti.Location = new Point(4, 24);
            tabCommenti.Name = "tabCommenti";
            tabCommenti.Padding = new Padding(8);
            tabCommenti.Size = new Size(468, 158);
            tabCommenti.TabIndex = 0;
            tabCommenti.Text = "💬 Commenti";
            // 
            // lvCommenti
            // 
            lvCommenti.BackColor = Color.White;
            lvCommenti.BorderStyle = BorderStyle.None;
            lvCommenti.Dock = DockStyle.Fill;
            lvCommenti.Font = new Font("Segoe UI", 9F);
            lvCommenti.ForeColor = Color.FromArgb(52, 73, 94);
            lvCommenti.FullRowSelect = true;
            lvCommenti.GridLines = true;
            lvCommenti.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            lvCommenti.Location = new Point(8, 8);
            lvCommenti.MultiSelect = false;
            lvCommenti.Name = "lvCommenti";
            lvCommenti.Size = new Size(452, 142);
            lvCommenti.TabIndex = 0;
            lvCommenti.UseCompatibleStateImageBehavior = false;
            lvCommenti.View = View.Details;
            // 
            // tabCronologia
            // 
            tabCronologia.BackColor = Color.White;
            tabCronologia.Location = new Point(4, 24);
            tabCronologia.Name = "tabCronologia";
            tabCronologia.Padding = new Padding(8);
            tabCronologia.Size = new Size(468, 158);
            tabCronologia.TabIndex = 1;
            tabCronologia.Text = "📅 Cronologia";
            // 
            // tabAllegati
            // 
            tabAllegati.BackColor = Color.White;
            tabAllegati.Location = new Point(4, 24);
            tabAllegati.Name = "tabAllegati";
            tabAllegati.Padding = new Padding(8);
            tabAllegati.Size = new Size(468, 158);
            tabAllegati.TabIndex = 2;
            tabAllegati.Text = "📎 Allegati";
            // 
            // lblAttivitaTitolo
            // 
            lblAttivitaTitolo.AutoSize = true;
            lblAttivitaTitolo.Dock = DockStyle.Top;
            lblAttivitaTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblAttivitaTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblAttivitaTitolo.Location = new Point(16, 16);
            lblAttivitaTitolo.Name = "lblAttivitaTitolo";
            lblAttivitaTitolo.Padding = new Padding(0, 0, 0, 8);
            lblAttivitaTitolo.Size = new Size(88, 28);
            lblAttivitaTitolo.TabIndex = 0;
            lblAttivitaTitolo.Text = "📊 Attività";
            // 
            // splitterCentral
            // 
            splitterCentral.BackColor = Color.FromArgb(226, 232, 240);
            splitterCentral.Dock = DockStyle.Bottom;
            splitterCentral.Location = new Point(8, 549);
            splitterCentral.Name = "splitterCentral";
            splitterCentral.Size = new Size(510, 6);
            splitterCentral.TabIndex = 1;
            splitterCentral.TabStop = false;
            // 
            // pnlDescrizione
            // 
            pnlDescrizione.BackColor = Color.White;
            pnlDescrizione.BorderStyle = BorderStyle.FixedSingle;
            pnlDescrizione.Controls.Add(txtDescrizione);
            pnlDescrizione.Controls.Add(lblDescrizioneTitolo);
            pnlDescrizione.Dock = DockStyle.Fill;
            pnlDescrizione.Location = new Point(8, 8);
            pnlDescrizione.Name = "pnlDescrizione";
            pnlDescrizione.Size = new Size(510, 547);
            pnlDescrizione.TabIndex = 0;
            // 
            // txtDescrizione
            // 
            txtDescrizione.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtDescrizione.BackColor = Color.White;
            txtDescrizione.BorderStyle = BorderStyle.FixedSingle;
            txtDescrizione.Font = new Font("Segoe UI", 9F);
            txtDescrizione.ForeColor = Color.FromArgb(52, 73, 94);
            txtDescrizione.Location = new Point(16, 48);
            txtDescrizione.Multiline = true;
            txtDescrizione.Name = "txtDescrizione";
            txtDescrizione.ReadOnly = true;
            txtDescrizione.ScrollBars = ScrollBars.Vertical;
            txtDescrizione.Size = new Size(478, 483);
            txtDescrizione.TabIndex = 1;
            // 
            // lblDescrizioneTitolo
            // 
            lblDescrizioneTitolo.AutoSize = true;
            lblDescrizioneTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblDescrizioneTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblDescrizioneTitolo.Location = new Point(16, 16);
            lblDescrizioneTitolo.Name = "lblDescrizioneTitolo";
            lblDescrizioneTitolo.Size = new Size(161, 20);
            lblDescrizioneTitolo.TabIndex = 0;
            lblDescrizioneTitolo.Text = "📝 Descrizione Ticket";
            // 
            // pnlRight
            // 
            pnlRight.BackColor = Color.White;
            pnlRight.BorderStyle = BorderStyle.FixedSingle;
            pnlRight.Controls.Add(pnlAnteprima);
            pnlRight.Controls.Add(pnlAssegnazione);
            pnlRight.Controls.Add(pnlClienteApp);
            pnlRight.Dock = DockStyle.Right;
            pnlRight.Location = new Point(856, 8);
            pnlRight.Name = "pnlRight";
            pnlRight.Padding = new Padding(8);
            pnlRight.Size = new Size(320, 565);
            pnlRight.TabIndex = 2;
            // 
            // pnlAnteprima
            // 
            pnlAnteprima.BackColor = Color.White;
            pnlAnteprima.BorderStyle = BorderStyle.FixedSingle;
            pnlAnteprima.Controls.Add(tbAnteprimaEmail);
            pnlAnteprima.Controls.Add(lblAnteprimaTitolo);
            pnlAnteprima.Dock = DockStyle.Fill;
            pnlAnteprima.Location = new Point(8, 358);
            pnlAnteprima.Name = "pnlAnteprima";
            pnlAnteprima.Size = new Size(302, 197);
            pnlAnteprima.TabIndex = 2;
            // 
            // tbAnteprimaEmail
            // 
            tbAnteprimaEmail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tbAnteprimaEmail.Controls.Add(tpAnteprimaEmail);
            tbAnteprimaEmail.Controls.Add(tpCommento);
            tbAnteprimaEmail.Font = new Font("Segoe UI", 9F);
            tbAnteprimaEmail.Location = new Point(16, 48);
            tbAnteprimaEmail.Name = "tbAnteprimaEmail";
            tbAnteprimaEmail.SelectedIndex = 0;
            tbAnteprimaEmail.Size = new Size(270, 133);
            tbAnteprimaEmail.TabIndex = 1;
            // 
            // tpAnteprimaEmail
            // 
            tpAnteprimaEmail.BackColor = Color.White;
            tpAnteprimaEmail.Controls.Add(cmbTemplate);
            tpAnteprimaEmail.Controls.Add(txtCorpoEmail);
            tpAnteprimaEmail.Location = new Point(4, 24);
            tpAnteprimaEmail.Name = "tpAnteprimaEmail";
            tpAnteprimaEmail.Padding = new Padding(8);
            tpAnteprimaEmail.Size = new Size(262, 105);
            tpAnteprimaEmail.TabIndex = 0;
            tpAnteprimaEmail.Text = "Anteprima";
            // 
            // cmbTemplate
            // 
            cmbTemplate.BackColor = Color.White;
            cmbTemplate.Dock = DockStyle.Top;
            cmbTemplate.FlatStyle = FlatStyle.Flat;
            cmbTemplate.Font = new Font("Segoe UI", 9F);
            cmbTemplate.ForeColor = Color.FromArgb(52, 73, 94);
            cmbTemplate.Location = new Point(8, 8);
            cmbTemplate.Name = "cmbTemplate";
            cmbTemplate.Size = new Size(246, 23);
            cmbTemplate.TabIndex = 0;
            // 
            // txtCorpoEmail
            // 
            txtCorpoEmail.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtCorpoEmail.BackColor = Color.FromArgb(248, 249, 250);
            txtCorpoEmail.BorderStyle = BorderStyle.FixedSingle;
            txtCorpoEmail.Font = new Font("Segoe UI", 9F);
            txtCorpoEmail.ForeColor = Color.FromArgb(52, 73, 94);
            txtCorpoEmail.Location = new Point(8, 40);
            txtCorpoEmail.Multiline = true;
            txtCorpoEmail.Name = "txtCorpoEmail";
            txtCorpoEmail.ReadOnly = true;
            txtCorpoEmail.ScrollBars = ScrollBars.Vertical;
            txtCorpoEmail.Size = new Size(246, 59);
            txtCorpoEmail.TabIndex = 1;
            // 
            // tpCommento
            // 
            tpCommento.BackColor = Color.White;
            tpCommento.Controls.Add(txtCommento);
            tpCommento.Location = new Point(4, 24);
            tpCommento.Name = "tpCommento";
            tpCommento.Padding = new Padding(8);
            tpCommento.Size = new Size(262, 105);
            tpCommento.TabIndex = 1;
            tpCommento.Text = "Commento";
            // 
            // txtCommento
            // 
            txtCommento.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtCommento.BackColor = Color.White;
            txtCommento.BorderStyle = BorderStyle.FixedSingle;
            txtCommento.Font = new Font("Segoe UI", 9F);
            txtCommento.ForeColor = Color.FromArgb(52, 73, 94);
            txtCommento.Location = new Point(8, 8);
            txtCommento.Multiline = true;
            txtCommento.Name = "txtCommento";
            txtCommento.ScrollBars = ScrollBars.Vertical;
            txtCommento.Size = new Size(246, 364);
            txtCommento.TabIndex = 0;
            // 
            // lblAnteprimaTitolo
            // 
            lblAnteprimaTitolo.AutoSize = true;
            lblAnteprimaTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblAnteprimaTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblAnteprimaTitolo.Location = new Point(16, 16);
            lblAnteprimaTitolo.Name = "lblAnteprimaTitolo";
            lblAnteprimaTitolo.Size = new Size(160, 20);
            lblAnteprimaTitolo.TabIndex = 0;
            lblAnteprimaTitolo.Text = "📧 TEMPLATE EMAIL";
            // 
            // pnlAssegnazione
            // 
            pnlAssegnazione.BackColor = Color.White;
            pnlAssegnazione.BorderStyle = BorderStyle.FixedSingle;
            pnlAssegnazione.Controls.Add(txtPriorityLab);
            pnlAssegnazione.Controls.Add(lblPriorityLab);
            pnlAssegnazione.Controls.Add(txtAssegnatario);
            pnlAssegnazione.Controls.Add(lblAssegnatario);
            pnlAssegnazione.Controls.Add(lblAssegnazioneTitolo);
            pnlAssegnazione.Dock = DockStyle.Top;
            pnlAssegnazione.Location = new Point(8, 208);
            pnlAssegnazione.Name = "pnlAssegnazione";
            pnlAssegnazione.Size = new Size(302, 150);
            pnlAssegnazione.TabIndex = 1;
            // 
            // txtPriorityLab
            // 
            txtPriorityLab.BackColor = Color.FromArgb(248, 249, 250);
            txtPriorityLab.BorderStyle = BorderStyle.FixedSingle;
            txtPriorityLab.Font = new Font("Segoe UI", 9F);
            txtPriorityLab.ForeColor = Color.FromArgb(52, 73, 94);
            txtPriorityLab.Location = new Point(16, 114);
            txtPriorityLab.Name = "txtPriorityLab";
            txtPriorityLab.ReadOnly = true;
            txtPriorityLab.Size = new Size(270, 23);
            txtPriorityLab.TabIndex = 4;
            // 
            // lblPriorityLab
            // 
            lblPriorityLab.AutoSize = true;
            lblPriorityLab.Font = new Font("Segoe UI", 9F);
            lblPriorityLab.ForeColor = Color.FromArgb(73, 80, 87);
            lblPriorityLab.Location = new Point(16, 96);
            lblPriorityLab.Name = "lblPriorityLab";
            lblPriorityLab.Size = new Size(45, 15);
            lblPriorityLab.TabIndex = 3;
            lblPriorityLab.Text = "Priorità";
            // 
            // txtAssegnatario
            // 
            txtAssegnatario.BackColor = Color.FromArgb(248, 249, 250);
            txtAssegnatario.BorderStyle = BorderStyle.FixedSingle;
            txtAssegnatario.Font = new Font("Segoe UI", 9F);
            txtAssegnatario.ForeColor = Color.FromArgb(52, 73, 94);
            txtAssegnatario.Location = new Point(16, 66);
            txtAssegnatario.Name = "txtAssegnatario";
            txtAssegnatario.ReadOnly = true;
            txtAssegnatario.Size = new Size(270, 23);
            txtAssegnatario.TabIndex = 2;
            // 
            // lblAssegnatario
            // 
            lblAssegnatario.AutoSize = true;
            lblAssegnatario.Font = new Font("Segoe UI", 9F);
            lblAssegnatario.ForeColor = Color.FromArgb(73, 80, 87);
            lblAssegnatario.Location = new Point(16, 48);
            lblAssegnatario.Name = "lblAssegnatario";
            lblAssegnatario.Size = new Size(75, 15);
            lblAssegnatario.TabIndex = 1;
            lblAssegnatario.Text = "Assegnatario";
            // 
            // lblAssegnazioneTitolo
            // 
            lblAssegnazioneTitolo.AutoSize = true;
            lblAssegnazioneTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblAssegnazioneTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblAssegnazioneTitolo.Location = new Point(16, 16);
            lblAssegnazioneTitolo.Name = "lblAssegnazioneTitolo";
            lblAssegnazioneTitolo.Size = new Size(85, 20);
            lblAssegnazioneTitolo.TabIndex = 0;
            lblAssegnazioneTitolo.Text = "🎫 TICKET";
            // 
            // pnlClienteApp
            // 
            pnlClienteApp.BackColor = Color.White;
            pnlClienteApp.BorderStyle = BorderStyle.FixedSingle;
            pnlClienteApp.Controls.Add(txtClientePartner);
            pnlClienteApp.Controls.Add(lblClientePartner);
            pnlClienteApp.Controls.Add(txtApplicativo);
            pnlClienteApp.Controls.Add(lblApplicativo);
            pnlClienteApp.Controls.Add(txtArea);
            pnlClienteApp.Controls.Add(lblArea);
            pnlClienteApp.Controls.Add(txtCliente);
            pnlClienteApp.Controls.Add(lblCliente);
            pnlClienteApp.Controls.Add(lblClienteAppTitolo);
            pnlClienteApp.Dock = DockStyle.Top;
            pnlClienteApp.Location = new Point(8, 8);
            pnlClienteApp.Name = "pnlClienteApp";
            pnlClienteApp.Size = new Size(302, 200);
            pnlClienteApp.TabIndex = 0;
            // 
            // txtClientePartner
            // 
            txtClientePartner.BackColor = Color.FromArgb(248, 249, 250);
            txtClientePartner.BorderStyle = BorderStyle.FixedSingle;
            txtClientePartner.Font = new Font("Segoe UI", 9F);
            txtClientePartner.ForeColor = Color.FromArgb(52, 73, 94);
            txtClientePartner.Location = new Point(16, 162);
            txtClientePartner.Name = "txtClientePartner";
            txtClientePartner.ReadOnly = true;
            txtClientePartner.Size = new Size(270, 23);
            txtClientePartner.TabIndex = 8;
            // 
            // lblClientePartner
            // 
            lblClientePartner.AutoSize = true;
            lblClientePartner.Font = new Font("Segoe UI", 9F);
            lblClientePartner.ForeColor = Color.FromArgb(73, 80, 87);
            lblClientePartner.Location = new Point(16, 144);
            lblClientePartner.Name = "lblClientePartner";
            lblClientePartner.Size = new Size(85, 15);
            lblClientePartner.TabIndex = 7;
            lblClientePartner.Text = "Cliente Partner";
            // 
            // txtApplicativo
            // 
            txtApplicativo.BackColor = Color.FromArgb(248, 249, 250);
            txtApplicativo.BorderStyle = BorderStyle.FixedSingle;
            txtApplicativo.Font = new Font("Segoe UI", 9F);
            txtApplicativo.ForeColor = Color.FromArgb(52, 73, 94);
            txtApplicativo.Location = new Point(140, 144);
            txtApplicativo.Name = "txtApplicativo";
            txtApplicativo.ReadOnly = true;
            txtApplicativo.Size = new Size(146, 23);
            txtApplicativo.TabIndex = 6;
            // 
            // lblApplicativo
            // 
            lblApplicativo.AutoSize = true;
            lblApplicativo.Font = new Font("Segoe UI", 9F);
            lblApplicativo.ForeColor = Color.FromArgb(73, 80, 87);
            lblApplicativo.Location = new Point(16, 144);
            lblApplicativo.Name = "lblApplicativo";
            lblApplicativo.Size = new Size(67, 15);
            lblApplicativo.TabIndex = 5;
            lblApplicativo.Text = "Applicativo";
            // 
            // txtArea
            // 
            txtArea.BackColor = Color.FromArgb(248, 249, 250);
            txtArea.BorderStyle = BorderStyle.FixedSingle;
            txtArea.Font = new Font("Segoe UI", 9F);
            txtArea.ForeColor = Color.FromArgb(52, 73, 94);
            txtArea.Location = new Point(16, 114);
            txtArea.Name = "txtArea";
            txtArea.ReadOnly = true;
            txtArea.Size = new Size(270, 23);
            txtArea.TabIndex = 4;
            // 
            // lblArea
            // 
            lblArea.AutoSize = true;
            lblArea.Font = new Font("Segoe UI", 9F);
            lblArea.ForeColor = Color.FromArgb(73, 80, 87);
            lblArea.Location = new Point(16, 96);
            lblArea.Name = "lblArea";
            lblArea.Size = new Size(31, 15);
            lblArea.TabIndex = 3;
            lblArea.Text = "Area";
            // 
            // txtCliente
            // 
            txtCliente.BackColor = Color.FromArgb(248, 249, 250);
            txtCliente.BorderStyle = BorderStyle.FixedSingle;
            txtCliente.Font = new Font("Segoe UI", 9F);
            txtCliente.ForeColor = Color.FromArgb(52, 73, 94);
            txtCliente.Location = new Point(16, 66);
            txtCliente.Name = "txtCliente";
            txtCliente.ReadOnly = true;
            txtCliente.Size = new Size(270, 23);
            txtCliente.TabIndex = 2;
            // 
            // lblCliente
            // 
            lblCliente.AutoSize = true;
            lblCliente.Font = new Font("Segoe UI", 9F);
            lblCliente.ForeColor = Color.FromArgb(73, 80, 87);
            lblCliente.Location = new Point(16, 48);
            lblCliente.Name = "lblCliente";
            lblCliente.Size = new Size(44, 15);
            lblCliente.TabIndex = 1;
            lblCliente.Text = "Cliente";
            // 
            // lblClienteAppTitolo
            // 
            lblClienteAppTitolo.AutoSize = true;
            lblClienteAppTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblClienteAppTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblClienteAppTitolo.Location = new Point(16, 16);
            lblClienteAppTitolo.Name = "lblClienteAppTitolo";
            lblClienteAppTitolo.Size = new Size(178, 20);
            lblClienteAppTitolo.TabIndex = 0;
            lblClienteAppTitolo.Text = "🏢 Cliente e Applicativo";
            // 
            // pnlLeft
            // 
            pnlLeft.BackColor = Color.White;
            pnlLeft.BorderStyle = BorderStyle.FixedSingle;
            pnlLeft.Controls.Add(pnlPianificazione);
            pnlLeft.Controls.Add(pnlDettagliTicket);
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Location = new Point(8, 8);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Padding = new Padding(8);
            pnlLeft.Size = new Size(320, 565);
            pnlLeft.TabIndex = 0;
            // 
            // pnlPianificazione
            // 
            pnlPianificazione.BackColor = Color.White;
            pnlPianificazione.BorderStyle = BorderStyle.FixedSingle;
            pnlPianificazione.Controls.Add(txtDataCompletamento);
            pnlPianificazione.Controls.Add(lblDataCompletamento);
            pnlPianificazione.Controls.Add(cmbConsulente);
            pnlPianificazione.Controls.Add(lblConsulente);
            pnlPianificazione.Controls.Add(txtWBS);
            pnlPianificazione.Controls.Add(lblWBS);
            pnlPianificazione.Controls.Add(txtResponsabile);
            pnlPianificazione.Controls.Add(lblResponsabile);
            pnlPianificazione.Controls.Add(txtPM);
            pnlPianificazione.Controls.Add(lblPM);
            pnlPianificazione.Controls.Add(txtCommerciale);
            pnlPianificazione.Controls.Add(lblCommerciale);
            pnlPianificazione.Controls.Add(lblPianificazione);
            pnlPianificazione.Dock = DockStyle.Fill;
            pnlPianificazione.Location = new Point(8, 288);
            pnlPianificazione.Name = "pnlPianificazione";
            pnlPianificazione.Size = new Size(302, 267);
            pnlPianificazione.TabIndex = 1;
            // 
            // txtDataCompletamento
            // 
            txtDataCompletamento.BackColor = Color.FromArgb(248, 249, 250);
            txtDataCompletamento.BorderStyle = BorderStyle.FixedSingle;
            txtDataCompletamento.Font = new Font("Segoe UI", 9F);
            txtDataCompletamento.ForeColor = Color.FromArgb(52, 73, 94);
            txtDataCompletamento.Location = new Point(16, 306);
            txtDataCompletamento.Name = "txtDataCompletamento";
            txtDataCompletamento.ReadOnly = true;
            txtDataCompletamento.Size = new Size(270, 23);
            txtDataCompletamento.TabIndex = 12;
            // 
            // lblDataCompletamento
            // 
            lblDataCompletamento.AutoSize = true;
            lblDataCompletamento.Font = new Font("Segoe UI", 9F);
            lblDataCompletamento.ForeColor = Color.FromArgb(73, 80, 87);
            lblDataCompletamento.Location = new Point(16, 288);
            lblDataCompletamento.Name = "lblDataCompletamento";
            lblDataCompletamento.Size = new Size(136, 15);
            lblDataCompletamento.TabIndex = 11;
            lblDataCompletamento.Text = "📅 Data Completamento";
            // 
            // cmbConsulente
            // 
            cmbConsulente.BackColor = Color.White;
            cmbConsulente.FlatStyle = FlatStyle.Flat;
            cmbConsulente.Font = new Font("Segoe UI", 9F);
            cmbConsulente.ForeColor = Color.FromArgb(52, 73, 94);
            cmbConsulente.Location = new Point(16, 258);
            cmbConsulente.Name = "cmbConsulente";
            cmbConsulente.Size = new Size(270, 23);
            cmbConsulente.TabIndex = 10;
            // 
            // lblConsulente
            // 
            lblConsulente.AutoSize = true;
            lblConsulente.Font = new Font("Segoe UI", 9F);
            lblConsulente.ForeColor = Color.FromArgb(73, 80, 87);
            lblConsulente.Location = new Point(16, 240);
            lblConsulente.Name = "lblConsulente";
            lblConsulente.Size = new Size(82, 15);
            lblConsulente.TabIndex = 9;
            lblConsulente.Text = "🙋 Consulente";
            // 
            // txtWBS
            // 
            txtWBS.BackColor = Color.FromArgb(248, 249, 250);
            txtWBS.BorderStyle = BorderStyle.FixedSingle;
            txtWBS.Font = new Font("Segoe UI", 9F);
            txtWBS.ForeColor = Color.FromArgb(52, 73, 94);
            txtWBS.Location = new Point(16, 210);
            txtWBS.Name = "txtWBS";
            txtWBS.ReadOnly = true;
            txtWBS.Size = new Size(270, 23);
            txtWBS.TabIndex = 8;
            // 
            // lblWBS
            // 
            lblWBS.AutoSize = true;
            lblWBS.Font = new Font("Segoe UI", 9F);
            lblWBS.ForeColor = Color.FromArgb(73, 80, 87);
            lblWBS.Location = new Point(16, 192);
            lblWBS.Name = "lblWBS";
            lblWBS.Size = new Size(86, 15);
            lblWBS.TabIndex = 7;
            lblWBS.Text = "📈 Codice WBS";
            // 
            // txtResponsabile
            // 
            txtResponsabile.BackColor = Color.FromArgb(248, 249, 250);
            txtResponsabile.BorderStyle = BorderStyle.FixedSingle;
            txtResponsabile.Font = new Font("Segoe UI", 9F);
            txtResponsabile.ForeColor = Color.FromArgb(52, 73, 94);
            txtResponsabile.Location = new Point(16, 162);
            txtResponsabile.Name = "txtResponsabile";
            txtResponsabile.ReadOnly = true;
            txtResponsabile.Size = new Size(270, 23);
            txtResponsabile.TabIndex = 6;
            // 
            // lblResponsabile
            // 
            lblResponsabile.AutoSize = true;
            lblResponsabile.Font = new Font("Segoe UI", 9F);
            lblResponsabile.ForeColor = Color.FromArgb(73, 80, 87);
            lblResponsabile.Location = new Point(16, 144);
            lblResponsabile.Name = "lblResponsabile";
            lblResponsabile.Size = new Size(91, 15);
            lblResponsabile.TabIndex = 5;
            lblResponsabile.Text = "👨‍💼 Responsabile";
            // 
            // txtPM
            // 
            txtPM.BackColor = Color.FromArgb(248, 249, 250);
            txtPM.BorderStyle = BorderStyle.FixedSingle;
            txtPM.Font = new Font("Segoe UI", 9F);
            txtPM.ForeColor = Color.FromArgb(52, 73, 94);
            txtPM.Location = new Point(16, 114);
            txtPM.Name = "txtPM";
            txtPM.ReadOnly = true;
            txtPM.Size = new Size(270, 23);
            txtPM.TabIndex = 4;
            // 
            // lblPM
            // 
            lblPM.AutoSize = true;
            lblPM.Font = new Font("Segoe UI", 9F);
            lblPM.ForeColor = Color.FromArgb(73, 80, 87);
            lblPM.Location = new Point(16, 96);
            lblPM.Name = "lblPM";
            lblPM.Size = new Size(109, 15);
            lblPM.TabIndex = 3;
            lblPM.Text = "💼 Project Manager";
            // 
            // txtCommerciale
            // 
            txtCommerciale.BackColor = Color.FromArgb(248, 249, 250);
            txtCommerciale.BorderStyle = BorderStyle.FixedSingle;
            txtCommerciale.Font = new Font("Segoe UI", 9F);
            txtCommerciale.ForeColor = Color.FromArgb(52, 73, 94);
            txtCommerciale.Location = new Point(16, 66);
            txtCommerciale.Name = "txtCommerciale";
            txtCommerciale.ReadOnly = true;
            txtCommerciale.Size = new Size(270, 23);
            txtCommerciale.TabIndex = 2;
            // 
            // lblCommerciale
            // 
            lblCommerciale.AutoSize = true;
            lblCommerciale.Font = new Font("Segoe UI", 9F);
            lblCommerciale.ForeColor = Color.FromArgb(73, 80, 87);
            lblCommerciale.Location = new Point(16, 48);
            lblCommerciale.Name = "lblCommerciale";
            lblCommerciale.Size = new Size(93, 15);
            lblCommerciale.TabIndex = 1;
            lblCommerciale.Text = "💼 Commerciale";
            // 
            // lblPianificazione
            // 
            lblPianificazione.AutoSize = true;
            lblPianificazione.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblPianificazione.ForeColor = Color.FromArgb(52, 73, 94);
            lblPianificazione.Location = new Point(16, 16);
            lblPianificazione.Name = "lblPianificazione";
            lblPianificazione.Size = new Size(181, 20);
            lblPianificazione.TabIndex = 0;
            lblPianificazione.Text = "👥 PIANIFICAZIONE 🔒";
            // 
            // pnlDettagliTicket
            // 
            pnlDettagliTicket.BackColor = Color.White;
            pnlDettagliTicket.BorderStyle = BorderStyle.FixedSingle;
            pnlDettagliTicket.Controls.Add(txtDataAggiornamento);
            pnlDettagliTicket.Controls.Add(lblDataAggiornamento);
            pnlDettagliTicket.Controls.Add(txtDataCreazione);
            pnlDettagliTicket.Controls.Add(lblDataCreazione);
            pnlDettagliTicket.Controls.Add(txtTicketTipo);
            pnlDettagliTicket.Controls.Add(lblTicketTipo);
            pnlDettagliTicket.Controls.Add(txtEmail);
            pnlDettagliTicket.Controls.Add(lblEmail);
            pnlDettagliTicket.Controls.Add(txtTelefono);
            pnlDettagliTicket.Controls.Add(lblTelefono);
            pnlDettagliTicket.Controls.Add(txtRichiedente);
            pnlDettagliTicket.Controls.Add(lblRichiedente);
            pnlDettagliTicket.Controls.Add(lblDettagliTicketTitolo);
            pnlDettagliTicket.Dock = DockStyle.Top;
            pnlDettagliTicket.Location = new Point(8, 8);
            pnlDettagliTicket.Name = "pnlDettagliTicket";
            pnlDettagliTicket.Size = new Size(302, 280);
            pnlDettagliTicket.TabIndex = 0;
            // 
            // txtDataAggiornamento
            // 
            txtDataAggiornamento.BackColor = Color.FromArgb(248, 249, 250);
            txtDataAggiornamento.BorderStyle = BorderStyle.FixedSingle;
            txtDataAggiornamento.Font = new Font("Segoe UI", 9F);
            txtDataAggiornamento.ForeColor = Color.FromArgb(52, 73, 94);
            txtDataAggiornamento.Location = new Point(140, 240);
            txtDataAggiornamento.Name = "txtDataAggiornamento";
            txtDataAggiornamento.ReadOnly = true;
            txtDataAggiornamento.Size = new Size(146, 23);
            txtDataAggiornamento.TabIndex = 12;
            // 
            // lblDataAggiornamento
            // 
            lblDataAggiornamento.AutoSize = true;
            lblDataAggiornamento.Font = new Font("Segoe UI", 9F);
            lblDataAggiornamento.ForeColor = Color.FromArgb(73, 80, 87);
            lblDataAggiornamento.Location = new Point(16, 240);
            lblDataAggiornamento.Name = "lblDataAggiornamento";
            lblDataAggiornamento.Size = new Size(118, 15);
            lblDataAggiornamento.TabIndex = 11;
            lblDataAggiornamento.Text = "Data Aggiornamento";
            // 
            // txtDataCreazione
            // 
            txtDataCreazione.BackColor = Color.FromArgb(248, 249, 250);
            txtDataCreazione.BorderStyle = BorderStyle.FixedSingle;
            txtDataCreazione.Font = new Font("Segoe UI", 9F);
            txtDataCreazione.ForeColor = Color.FromArgb(52, 73, 94);
            txtDataCreazione.Location = new Point(156, 210);
            txtDataCreazione.Name = "txtDataCreazione";
            txtDataCreazione.ReadOnly = true;
            txtDataCreazione.Size = new Size(130, 23);
            txtDataCreazione.TabIndex = 10;
            // 
            // lblDataCreazione
            // 
            lblDataCreazione.AutoSize = true;
            lblDataCreazione.Font = new Font("Segoe UI", 9F);
            lblDataCreazione.ForeColor = Color.FromArgb(73, 80, 87);
            lblDataCreazione.Location = new Point(156, 192);
            lblDataCreazione.Name = "lblDataCreazione";
            lblDataCreazione.Size = new Size(86, 15);
            lblDataCreazione.TabIndex = 9;
            lblDataCreazione.Text = "Data Creazione";
            // 
            // txtTicketTipo
            // 
            txtTicketTipo.BackColor = Color.FromArgb(248, 249, 250);
            txtTicketTipo.BorderStyle = BorderStyle.FixedSingle;
            txtTicketTipo.Font = new Font("Segoe UI", 9F);
            txtTicketTipo.ForeColor = Color.FromArgb(52, 73, 94);
            txtTicketTipo.Location = new Point(16, 210);
            txtTicketTipo.Name = "txtTicketTipo";
            txtTicketTipo.ReadOnly = true;
            txtTicketTipo.Size = new Size(130, 23);
            txtTicketTipo.TabIndex = 8;
            // 
            // lblTicketTipo
            // 
            lblTicketTipo.AutoSize = true;
            lblTicketTipo.Font = new Font("Segoe UI", 9F);
            lblTicketTipo.ForeColor = Color.FromArgb(73, 80, 87);
            lblTicketTipo.Location = new Point(16, 192);
            lblTicketTipo.Name = "lblTicketTipo";
            lblTicketTipo.Size = new Size(31, 15);
            lblTicketTipo.TabIndex = 7;
            lblTicketTipo.Text = "Tipo";
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.FromArgb(248, 249, 250);
            txtEmail.BorderStyle = BorderStyle.FixedSingle;
            txtEmail.Font = new Font("Segoe UI", 9F);
            txtEmail.ForeColor = Color.FromArgb(52, 73, 94);
            txtEmail.Location = new Point(16, 162);
            txtEmail.Name = "txtEmail";
            txtEmail.ReadOnly = true;
            txtEmail.Size = new Size(270, 23);
            txtEmail.TabIndex = 6;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 9F);
            lblEmail.ForeColor = Color.FromArgb(73, 80, 87);
            lblEmail.Location = new Point(16, 144);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(36, 15);
            lblEmail.TabIndex = 5;
            lblEmail.Text = "Email";
            // 
            // txtTelefono
            // 
            txtTelefono.BackColor = Color.FromArgb(248, 249, 250);
            txtTelefono.BorderStyle = BorderStyle.FixedSingle;
            txtTelefono.Font = new Font("Segoe UI", 9F);
            txtTelefono.ForeColor = Color.FromArgb(52, 73, 94);
            txtTelefono.Location = new Point(16, 114);
            txtTelefono.Name = "txtTelefono";
            txtTelefono.ReadOnly = true;
            txtTelefono.Size = new Size(270, 23);
            txtTelefono.TabIndex = 4;
            // 
            // lblTelefono
            // 
            lblTelefono.AutoSize = true;
            lblTelefono.Font = new Font("Segoe UI", 9F);
            lblTelefono.ForeColor = Color.FromArgb(73, 80, 87);
            lblTelefono.Location = new Point(16, 96);
            lblTelefono.Name = "lblTelefono";
            lblTelefono.Size = new Size(53, 15);
            lblTelefono.TabIndex = 3;
            lblTelefono.Text = "Telefono";
            // 
            // txtRichiedente
            // 
            txtRichiedente.BackColor = Color.FromArgb(248, 249, 250);
            txtRichiedente.BorderStyle = BorderStyle.FixedSingle;
            txtRichiedente.Font = new Font("Segoe UI", 9F);
            txtRichiedente.ForeColor = Color.FromArgb(52, 73, 94);
            txtRichiedente.Location = new Point(16, 66);
            txtRichiedente.Name = "txtRichiedente";
            txtRichiedente.ReadOnly = true;
            txtRichiedente.Size = new Size(270, 23);
            txtRichiedente.TabIndex = 2;
            // 
            // lblRichiedente
            // 
            lblRichiedente.AutoSize = true;
            lblRichiedente.Font = new Font("Segoe UI", 9F);
            lblRichiedente.ForeColor = Color.FromArgb(73, 80, 87);
            lblRichiedente.Location = new Point(16, 48);
            lblRichiedente.Name = "lblRichiedente";
            lblRichiedente.Size = new Size(69, 15);
            lblRichiedente.TabIndex = 1;
            lblRichiedente.Text = "Richiedente";
            // 
            // lblDettagliTicketTitolo
            // 
            lblDettagliTicketTitolo.AutoSize = true;
            lblDettagliTicketTitolo.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblDettagliTicketTitolo.ForeColor = Color.FromArgb(52, 73, 94);
            lblDettagliTicketTitolo.Location = new Point(16, 16);
            lblDettagliTicketTitolo.Name = "lblDettagliTicketTitolo";
            lblDettagliTicketTitolo.Size = new Size(108, 20);
            lblDettagliTicketTitolo.TabIndex = 0;
            lblDettagliTicketTitolo.Text = "👤 CONTATTI";
            // 
            // pnlAzioni
            // 
            pnlAzioni.BackColor = Color.FromArgb(248, 249, 250);
            pnlAzioni.BorderStyle = BorderStyle.FixedSingle;
            pnlAzioni.Controls.Add(btnTest);
            pnlAzioni.Controls.Add(btnChiudi);
            pnlAzioni.Controls.Add(btnEsporta);
            pnlAzioni.Controls.Add(btnAssegnaAMe);
            pnlAzioni.Controls.Add(btnCommento);
            pnlAzioni.Controls.Add(btnPianificazione);
            pnlAzioni.Controls.Add(btnChiudiTicket);
            pnlAzioni.Dock = DockStyle.Bottom;
            pnlAzioni.Location = new Point(0, 671);
            pnlAzioni.Name = "pnlAzioni";
            pnlAzioni.Padding = new Padding(16, 12, 16, 12);
            pnlAzioni.Size = new Size(1184, 60);
            pnlAzioni.TabIndex = 2;
            // 
            // btnTest
            // 
            btnTest.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnTest.BackColor = Color.FromArgb(192, 192, 0);
            btnTest.FlatAppearance.BorderSize = 0;
            btnTest.FlatStyle = FlatStyle.Flat;
            btnTest.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnTest.ForeColor = Color.White;
            btnTest.Location = new Point(783, 12);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(120, 35);
            btnTest.TabIndex = 6;
            btnTest.Text = "🔧 Test";
            btnTest.UseVisualStyleBackColor = false;
            btnTest.Visible = false;
            // 
            // btnChiudi
            // 
            btnChiudi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChiudi.BackColor = Color.FromArgb(231, 76, 60);
            btnChiudi.FlatAppearance.BorderSize = 0;
            btnChiudi.FlatStyle = FlatStyle.Flat;
            btnChiudi.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnChiudi.ForeColor = Color.White;
            btnChiudi.Location = new Point(1044, 12);
            btnChiudi.Name = "btnChiudi";
            btnChiudi.Size = new Size(120, 35);
            btnChiudi.TabIndex = 5;
            btnChiudi.Text = "❌ Chiudi";
            btnChiudi.UseVisualStyleBackColor = false;
            // 
            // btnEsporta
            // 
            btnEsporta.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnEsporta.BackColor = Color.FromArgb(46, 204, 113);
            btnEsporta.FlatAppearance.BorderSize = 0;
            btnEsporta.FlatStyle = FlatStyle.Flat;
            btnEsporta.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnEsporta.ForeColor = Color.White;
            btnEsporta.Location = new Point(913, 12);
            btnEsporta.Name = "btnEsporta";
            btnEsporta.Size = new Size(120, 35);
            btnEsporta.TabIndex = 4;
            btnEsporta.Text = "💾 Esporta";
            btnEsporta.UseVisualStyleBackColor = false;
            // 
            // btnAssegnaAMe
            // 
            btnAssegnaAMe.BackColor = Color.FromArgb(230, 126, 34);
            btnAssegnaAMe.FlatAppearance.BorderSize = 0;
            btnAssegnaAMe.FlatStyle = FlatStyle.Flat;
            btnAssegnaAMe.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAssegnaAMe.ForeColor = Color.White;
            btnAssegnaAMe.Location = new Point(340, 12);
            btnAssegnaAMe.Name = "btnAssegnaAMe";
            btnAssegnaAMe.Size = new Size(150, 35);
            btnAssegnaAMe.TabIndex = 2;
            btnAssegnaAMe.Text = "👤 Assegna a me";
            btnAssegnaAMe.UseVisualStyleBackColor = false;
            // 
            // btnCommento
            // 
            btnCommento.BackColor = Color.FromArgb(155, 89, 182);
            btnCommento.FlatAppearance.BorderSize = 0;
            btnCommento.FlatStyle = FlatStyle.Flat;
            btnCommento.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCommento.ForeColor = Color.White;
            btnCommento.Location = new Point(180, 12);
            btnCommento.Name = "btnCommento";
            btnCommento.Size = new Size(150, 35);
            btnCommento.TabIndex = 1;
            btnCommento.Text = "💬 Commento";
            btnCommento.UseVisualStyleBackColor = false;
            // 
            // btnPianificazione
            // 
            btnPianificazione.BackColor = Color.FromArgb(52, 152, 219);
            btnPianificazione.FlatAppearance.BorderSize = 0;
            btnPianificazione.FlatStyle = FlatStyle.Flat;
            btnPianificazione.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnPianificazione.ForeColor = Color.White;
            btnPianificazione.Location = new Point(20, 12);
            btnPianificazione.Name = "btnPianificazione";
            btnPianificazione.Size = new Size(150, 35);
            btnPianificazione.TabIndex = 0;
            btnPianificazione.Text = "📨 Pianificazione";
            btnPianificazione.UseVisualStyleBackColor = false;
            // 
            // btnChiudiTicket
            // 
            btnChiudiTicket.BackColor = Color.FromArgb(229, 115, 115);
            btnChiudiTicket.FlatAppearance.BorderSize = 0;
            btnChiudiTicket.FlatStyle = FlatStyle.Flat;
            btnChiudiTicket.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnChiudiTicket.ForeColor = Color.White;
            btnChiudiTicket.Location = new Point(500, 12);
            btnChiudiTicket.Name = "btnChiudiTicket";
            btnChiudiTicket.Size = new Size(134, 35);
            btnChiudiTicket.TabIndex = 3;
            btnChiudiTicket.Text = "🔒 Chiudi Ticket";
            btnChiudiTicket.UseVisualStyleBackColor = false;
            // 
            // pnlStatusBar
            // 
            pnlStatusBar.BackColor = Color.FromArgb(52, 73, 94);
            pnlStatusBar.Controls.Add(lblStatus);
            pnlStatusBar.Dock = DockStyle.Bottom;
            pnlStatusBar.Location = new Point(0, 731);
            pnlStatusBar.Name = "pnlStatusBar";
            pnlStatusBar.Size = new Size(1184, 30);
            pnlStatusBar.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = Color.FromArgb(248, 249, 250);
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.ForeColor = Color.FromArgb(52, 73, 94);
            lblStatus.Location = new Point(0, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(10, 0, 0, 0);
            lblStatus.Size = new Size(1184, 30);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Pronto";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // FrmDettaglio
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1184, 761);
            Controls.Add(pnlMain);
            Controls.Add(pnlAzioni);
            Controls.Add(pnlStatusBar);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1200, 800);
            Name = "FrmDettaglio";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "🎫 Jira TicketMate - Dettaglio Ticket";
            pnlHeader.ResumeLayout(false);
            pnlHeaderLeft.ResumeLayout(false);
            pnlHeaderLeft.PerformLayout();
            tsMain.ResumeLayout(false);
            tsMain.PerformLayout();
            pnlMain.ResumeLayout(false);
            pnlCenter.ResumeLayout(false);
            pnlAttivita.ResumeLayout(false);
            pnlAttivita.PerformLayout();
            tabAttivita.ResumeLayout(false);
            tabCommenti.ResumeLayout(false);
            pnlDescrizione.ResumeLayout(false);
            pnlDescrizione.PerformLayout();
            pnlRight.ResumeLayout(false);
            pnlAnteprima.ResumeLayout(false);
            pnlAnteprima.PerformLayout();
            tbAnteprimaEmail.ResumeLayout(false);
            tpAnteprimaEmail.ResumeLayout(false);
            tpAnteprimaEmail.PerformLayout();
            tpCommento.ResumeLayout(false);
            tpCommento.PerformLayout();
            pnlAssegnazione.ResumeLayout(false);
            pnlAssegnazione.PerformLayout();
            pnlClienteApp.ResumeLayout(false);
            pnlClienteApp.PerformLayout();
            pnlLeft.ResumeLayout(false);
            pnlPianificazione.ResumeLayout(false);
            pnlPianificazione.PerformLayout();
            pnlDettagliTicket.ResumeLayout(false);
            pnlDettagliTicket.PerformLayout();
            pnlAzioni.ResumeLayout(false);
            pnlStatusBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // ===================================================================
        // === DICHIARAZIONE CONTROLLI ===
        // ===================================================================

        // === HEADER ===
        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Panel pnlHeaderLeft;
        private System.Windows.Forms.Panel pnlHeaderRight;
        private System.Windows.Forms.ToolStrip tsMain;
        private System.Windows.Forms.ToolStripLabel tslSearch;
        private System.Windows.Forms.ToolStripTextBox tstbTicketSearch;
        private System.Windows.Forms.ToolStripButton tsbSearch;
        private System.Windows.Forms.ToolStripSeparator tssSearchSeparator;
        private System.Windows.Forms.ToolStripButton tsbRefresh;
        private System.Windows.Forms.ToolStripButton tsbOpenJira;

        // === LAYOUT PRINCIPALE ===
        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Panel pnlLeft;
        private System.Windows.Forms.Panel pnlCenter;
        private System.Windows.Forms.Panel pnlRight;

        // === PANNELLO SINISTRO ===
        private System.Windows.Forms.Panel pnlDettagliTicket;
        private System.Windows.Forms.Label lblDettagliTicketTitolo;
        private System.Windows.Forms.Label lblRichiedente;
        private System.Windows.Forms.TextBox txtRichiedente;
        private System.Windows.Forms.Label lblTelefono;
        private System.Windows.Forms.TextBox txtTelefono;
        private System.Windows.Forms.Label lblEmail;
        private System.Windows.Forms.TextBox txtEmail;
        private System.Windows.Forms.Label lblTicketTipo;
        private System.Windows.Forms.TextBox txtTicketTipo;
        private System.Windows.Forms.Label lblDataCreazione;
        private System.Windows.Forms.TextBox txtDataCreazione;
        private System.Windows.Forms.Label lblDataAggiornamento;
        private System.Windows.Forms.TextBox txtDataAggiornamento;

        private System.Windows.Forms.Panel pnlPianificazione;
        private System.Windows.Forms.Label lblPianificazione;
        private System.Windows.Forms.Label lblCommerciale;
        private System.Windows.Forms.TextBox txtCommerciale;
        private System.Windows.Forms.Label lblPM;
        private System.Windows.Forms.TextBox txtPM;
        private System.Windows.Forms.Label lblResponsabile;
        private System.Windows.Forms.TextBox txtResponsabile;
        private System.Windows.Forms.Label lblWBS;
        private System.Windows.Forms.TextBox txtWBS;
        private System.Windows.Forms.Label lblConsulente;
        private System.Windows.Forms.ComboBox cmbConsulente;
        private System.Windows.Forms.Label lblDataCompletamento;
        private System.Windows.Forms.TextBox txtDataCompletamento;

        // === PANNELLO CENTRALE ===
        private System.Windows.Forms.Panel pnlDescrizione;
        private System.Windows.Forms.Label lblDescrizioneTitolo;
        private System.Windows.Forms.TextBox txtDescrizione;

        private System.Windows.Forms.Panel pnlAttivita;
        private System.Windows.Forms.Label lblAttivitaTitolo;
        private System.Windows.Forms.TabControl tabAttivita;
        private System.Windows.Forms.TabPage tabCommenti;
        private System.Windows.Forms.ListView lvCommenti;
        private System.Windows.Forms.TabPage tabCronologia;
        private System.Windows.Forms.TabPage tabAllegati;

        private System.Windows.Forms.Splitter splitterCentral;

        // === PANNELLO DESTRO ===
        private System.Windows.Forms.Panel pnlClienteApp;
        private System.Windows.Forms.Label lblClienteAppTitolo;
        private System.Windows.Forms.Label lblCliente;
        private System.Windows.Forms.TextBox txtCliente;
        private System.Windows.Forms.Label lblArea;
        private System.Windows.Forms.TextBox txtArea;
        private System.Windows.Forms.Label lblApplicativo;
        private System.Windows.Forms.TextBox txtApplicativo;
        private System.Windows.Forms.Label lblClientePartner;
        private System.Windows.Forms.TextBox txtClientePartner;

        private System.Windows.Forms.Panel pnlAssegnazione;
        private System.Windows.Forms.Label lblAssegnazioneTitolo;
        private System.Windows.Forms.Label lblAssegnatario;
        private System.Windows.Forms.TextBox txtAssegnatario;
        private System.Windows.Forms.Label lblPriorityLab;
        private System.Windows.Forms.TextBox txtPriorityLab;

        private System.Windows.Forms.Panel pnlAnteprima;
        private System.Windows.Forms.Label lblAnteprimaTitolo;
        private System.Windows.Forms.TabControl tbAnteprimaEmail;
        private System.Windows.Forms.TabPage tpAnteprimaEmail;
        private System.Windows.Forms.ComboBox cmbTemplate;
        private System.Windows.Forms.TextBox txtCorpoEmail;
        private System.Windows.Forms.TabPage tpCommento;
        private System.Windows.Forms.TextBox txtCommento;

        // === AZIONI ===
        private System.Windows.Forms.Panel pnlAzioni;
        private System.Windows.Forms.Button btnPianificazione;
        private System.Windows.Forms.Button btnCommento;
        private System.Windows.Forms.Button btnAssegnaAMe;
        private System.Windows.Forms.Button btnChiudiTicket;
        private System.Windows.Forms.Button btnEsporta;
        private System.Windows.Forms.Button btnChiudi;
        private System.Windows.Forms.Button btnTest;

        // === STATUS BAR ===
        private System.Windows.Forms.Panel pnlStatusBar;
        private System.Windows.Forms.Label lblStatus;
    }
}