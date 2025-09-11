using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace JiraTicketManager.Forms
{
    partial class TicketDetailForm
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
            pnlTicketInfo = new Panel();
            pnlTicketAction = new Panel();
            btnChiudi = new Button();
            btnAssegna = new Button();
            pnlMetadata = new Panel();
            lblAssegnatario = new Label();
            lblPriorita = new Label();
            lblTipo = new Label();
            lblStatus = new Label();
            lblTicketSummary = new Label();
            lblTicketKey = new Label();
            tsHeader = new ToolStrip();
            tslSearch = new ToolStripLabel();
            tstbTicketSearch = new ToolStripTextBox();
            tsbSearch = new ToolStripButton();
            tssSeparator1 = new ToolStripSeparator();
            tsbRefresh = new ToolStripButton();
            tsbOpenJira = new ToolStripButton();
            tssSeparator2 = new ToolStripSeparator();
            tsbClose = new ToolStripButton();
            btnEsporta = new Button();
            pnlMain = new Panel();
            pnlRight = new Panel();
            pnlTeamPlanning = new Panel();
            txtWBS = new TextBox();
            lblWBS = new Label();
            lblCommerciale = new Label();
            cmbCommerciale = new ComboBox();
            txtResponsabile = new TextBox();
            lblResponsabile = new Label();
            cmbPM = new ComboBox();
            lblPM = new Label();
            cmbConsulente = new ComboBox();
            lblConsulente = new Label();
            lblTeamPlanningTitle = new Label();
            pnlOrganization = new Panel();
            txtClientePartner = new TextBox();
            lblClientePartner = new Label();
            txtApplicativo = new TextBox();
            lblApplicativo = new Label();
            txtArea = new TextBox();
            lblArea = new Label();
            txtCliente = new TextBox();
            lblCliente = new Label();
            lblOrganizationTitle = new Label();
            pnlCenter = new Panel();
            statusStrip1 = new StatusStrip();
            tslConnection = new ToolStripStatusLabel();
            tsCommentNumber = new ToolStripStatusLabel();
            tslLastUpdate = new ToolStripStatusLabel();
            pnlActivity = new Panel();
            tcActivity = new TabControl();
            tpComments = new TabPage();
            lvComments = new ListView();
            tpHistory = new TabPage();
            lvHistory = new ListView();
            tpAttachments = new TabPage();
            lvAttachments = new ListView();
            lblActivityTitle = new Label();
            pnlDescription = new Panel();
            txtDescrizione = new TextBox();
            lblDescriptionTitle = new Label();
            pnlLeft = new Panel();
            panel1 = new Panel();
            btnPianifica = new Button();
            btnCommento = new Button();
            pnlPlanningIntervento = new Panel();
            txtEffort = new TextBox();
            lblEffort = new Label();
            txtOraIntervento = new TextBox();
            lblOraIntervento = new Label();
            txtDataIntervento = new TextBox();
            lblDataIntervento = new Label();
            cmbTipoPianificazione = new ComboBox();
            lblTipoPianificazione = new Label();
            lblPlanningInterventoTitle = new Label();
            pnlTimeline = new Panel();
            txtDataCompletamento = new TextBox();
            lblDataCompletamento = new Label();
            txtDataAggiornamento = new TextBox();
            lblDataAggiornamento = new Label();
            txtDataCreazione = new TextBox();
            lblDataCreazione = new Label();
            lblTimelineTitle = new Label();
            pnlContact = new Panel();
            txtTelefono = new TextBox();
            lblTelefono = new Label();
            txtEmail = new TextBox();
            lblEmail = new Label();
            txtRichiedente = new TextBox();
            lblRichiedente = new Label();
            lblContactTitle = new Label();
            pnlHeader.SuspendLayout();
            pnlTicketInfo.SuspendLayout();
            pnlTicketAction.SuspendLayout();
            pnlMetadata.SuspendLayout();
            tsHeader.SuspendLayout();
            pnlMain.SuspendLayout();
            pnlRight.SuspendLayout();
            pnlTeamPlanning.SuspendLayout();
            pnlOrganization.SuspendLayout();
            pnlCenter.SuspendLayout();
            statusStrip1.SuspendLayout();
            pnlActivity.SuspendLayout();
            tcActivity.SuspendLayout();
            tpComments.SuspendLayout();
            tpHistory.SuspendLayout();
            tpAttachments.SuspendLayout();
            pnlDescription.SuspendLayout();
            pnlLeft.SuspendLayout();
            panel1.SuspendLayout();
            pnlPlanningIntervento.SuspendLayout();
            pnlTimeline.SuspendLayout();
            pnlContact.SuspendLayout();
            SuspendLayout();
            // 
            // pnlHeader
            // 
            pnlHeader.BackColor = Color.FromArgb(248, 249, 250);
            pnlHeader.BorderStyle = BorderStyle.FixedSingle;
            pnlHeader.Controls.Add(pnlTicketInfo);
            pnlHeader.Controls.Add(tsHeader);
            pnlHeader.Dock = DockStyle.Top;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1700, 115);
            pnlHeader.TabIndex = 0;
            // 
            // pnlTicketInfo
            // 
            pnlTicketInfo.BackColor = Color.FromArgb(248, 249, 250);
            pnlTicketInfo.Controls.Add(pnlTicketAction);
            pnlTicketInfo.Controls.Add(pnlMetadata);
            pnlTicketInfo.Controls.Add(lblTicketSummary);
            pnlTicketInfo.Controls.Add(lblTicketKey);
            pnlTicketInfo.Dock = DockStyle.Fill;
            pnlTicketInfo.Location = new Point(0, 25);
            pnlTicketInfo.Name = "pnlTicketInfo";
            pnlTicketInfo.Padding = new Padding(15, 10, 15, 5);
            pnlTicketInfo.Size = new Size(1698, 88);
            pnlTicketInfo.TabIndex = 1;
            // 
            // pnlTicketAction
            // 
            pnlTicketAction.Controls.Add(btnChiudi);
            pnlTicketAction.Controls.Add(btnAssegna);
            pnlTicketAction.Dock = DockStyle.Right;
            pnlTicketAction.Location = new Point(1478, 10);
            pnlTicketAction.Name = "pnlTicketAction";
            pnlTicketAction.Size = new Size(205, 73);
            pnlTicketAction.TabIndex = 5;
            // 
            // btnChiudi
            // 
            btnChiudi.BackColor = Color.FromArgb(40, 167, 69);
            btnChiudi.FlatAppearance.BorderSize = 0;
            btnChiudi.FlatStyle = FlatStyle.Flat;
            btnChiudi.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnChiudi.ForeColor = Color.White;
            btnChiudi.Location = new Point(3, 5);
            btnChiudi.Name = "btnChiudi";
            btnChiudi.Size = new Size(106, 23);
            btnChiudi.TabIndex = 2;
            btnChiudi.Text = "✅ Chiudi Ticket";
            btnChiudi.UseVisualStyleBackColor = false;
            // 
            // btnAssegna
            // 
            btnAssegna.BackColor = Color.FromArgb(217, 119, 6);
            btnAssegna.FlatAppearance.BorderSize = 0;
            btnAssegna.FlatStyle = FlatStyle.Flat;
            btnAssegna.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnAssegna.ForeColor = Color.White;
            btnAssegna.Location = new Point(110, 5);
            btnAssegna.Name = "btnAssegna";
            btnAssegna.Size = new Size(90, 23);
            btnAssegna.TabIndex = 1;
            btnAssegna.Text = "👤 Assegna";
            btnAssegna.UseVisualStyleBackColor = false;
            // 
            // pnlMetadata
            // 
            pnlMetadata.BackColor = Color.Transparent;
            pnlMetadata.Controls.Add(lblAssegnatario);
            pnlMetadata.Controls.Add(lblPriorita);
            pnlMetadata.Controls.Add(lblTipo);
            pnlMetadata.Controls.Add(lblStatus);
            pnlMetadata.Location = new Point(15, 40);
            pnlMetadata.Name = "pnlMetadata";
            pnlMetadata.Size = new Size(1130, 40);
            pnlMetadata.TabIndex = 2;
            // 
            // lblAssegnatario
            // 
            lblAssegnatario.BackColor = Color.FromArgb(233, 236, 239);
            lblAssegnatario.Font = new Font("Segoe UI", 8F);
            lblAssegnatario.ForeColor = Color.FromArgb(73, 80, 87);
            lblAssegnatario.Location = new Point(435, 4);
            lblAssegnatario.Name = "lblAssegnatario";
            lblAssegnatario.Padding = new Padding(5, 2, 2, 2);
            lblAssegnatario.Size = new Size(136, 31);
            lblAssegnatario.TabIndex = 3;
            lblAssegnatario.Text = "👤";
            lblAssegnatario.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblPriorita
            // 
            lblPriorita.BackColor = Color.FromArgb(233, 236, 239);
            lblPriorita.Font = new Font("Segoe UI", 8F);
            lblPriorita.ForeColor = Color.FromArgb(255, 87, 34);
            lblPriorita.Location = new Point(293, 4);
            lblPriorita.Name = "lblPriorita";
            lblPriorita.Padding = new Padding(5, 2, 2, 2);
            lblPriorita.Size = new Size(136, 31);
            lblPriorita.TabIndex = 2;
            lblPriorita.Text = "\U0001f7e0 High";
            lblPriorita.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblTipo
            // 
            lblTipo.BackColor = Color.FromArgb(233, 236, 239);
            lblTipo.Font = new Font("Segoe UI", 8F);
            lblTipo.ForeColor = Color.FromArgb(73, 80, 87);
            lblTipo.Location = new Point(151, 4);
            lblTipo.Name = "lblTipo";
            lblTipo.Padding = new Padding(5, 2, 2, 2);
            lblTipo.Size = new Size(136, 31);
            lblTipo.TabIndex = 1;
            lblTipo.Text = "🐛 Bug";
            lblTipo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            lblStatus.BackColor = Color.FromArgb(220, 53, 69);
            lblStatus.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblStatus.ForeColor = Color.White;
            lblStatus.Location = new Point(9, 4);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(136, 31);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Inoltrato (Terzo Livello)";
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTicketSummary
            // 
            lblTicketSummary.Font = new Font("Segoe UI", 10F);
            lblTicketSummary.ForeColor = Color.FromArgb(52, 73, 94);
            lblTicketSummary.Location = new Point(134, 12);
            lblTicketSummary.Name = "lblTicketSummary";
            lblTicketSummary.Size = new Size(600, 20);
            lblTicketSummary.TabIndex = 1;
            lblTicketSummary.Text = "Summary del ticket che può essere molto lungo e descrivere il problema";
            // 
            // lblTicketKey
            // 
            lblTicketKey.AutoSize = true;
            lblTicketKey.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTicketKey.ForeColor = Color.FromArgb(0, 120, 212);
            lblTicketKey.Location = new Point(16, 10);
            lblTicketKey.Name = "lblTicketKey";
            lblTicketKey.Size = new Size(112, 25);
            lblTicketKey.TabIndex = 0;
            lblTicketKey.Text = "[CC-11986]";
            // 
            // tsHeader
            // 
            tsHeader.BackColor = Color.FromArgb(248, 249, 250);
            tsHeader.GripStyle = ToolStripGripStyle.Hidden;
            tsHeader.Items.AddRange(new ToolStripItem[] { tslSearch, tstbTicketSearch, tsbSearch, tssSeparator1, tsbRefresh, tsbOpenJira, tssSeparator2, tsbClose });
            tsHeader.Location = new Point(0, 0);
            tsHeader.Name = "tsHeader";
            tsHeader.RenderMode = ToolStripRenderMode.Professional;
            tsHeader.Size = new Size(1698, 25);
            tsHeader.TabIndex = 0;
            // 
            // tslSearch
            // 
            tslSearch.BackColor = Color.White;
            tslSearch.Name = "tslSearch";
            tslSearch.Size = new Size(19, 22);
            tslSearch.Text = "🔍";
            tslSearch.ToolTipText = "Cerca ticket";
            // 
            // tstbTicketSearch
            // 
            tstbTicketSearch.BackColor = Color.FromArgb(0, 120, 212);
            tstbTicketSearch.ForeColor = Color.FromArgb(73, 80, 87);
            tstbTicketSearch.Name = "tstbTicketSearch";
            tstbTicketSearch.Size = new Size(200, 25);
            tstbTicketSearch.ToolTipText = "Inserisci numero ticket (es: CC-12345) e premi Enter";
            // 
            // tsbSearch
            // 
            tsbSearch.BackColor = Color.FromArgb(0, 120, 212);
            tsbSearch.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbSearch.Font = new Font("Segoe UI", 9F);
            tsbSearch.ForeColor = Color.White;
            tsbSearch.Name = "tsbSearch";
            tsbSearch.Size = new Size(41, 22);
            tsbSearch.Text = "Cerca";
            tsbSearch.ToolTipText = "Cerca e carica ticket";
            // 
            // tssSeparator1
            // 
            tssSeparator1.Name = "tssSeparator1";
            tssSeparator1.Size = new Size(6, 25);
            // 
            // tsbRefresh
            // 
            tsbRefresh.BackColor = Color.FromArgb(0, 120, 212);
            tsbRefresh.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbRefresh.Font = new Font("Segoe UI", 9F);
            tsbRefresh.ForeColor = Color.White;
            tsbRefresh.Name = "tsbRefresh";
            tsbRefresh.Size = new Size(23, 22);
            tsbRefresh.Text = "🔄";
            tsbRefresh.ToolTipText = "Ricarica ticket corrente (F5)";
            // 
            // tsbOpenJira
            // 
            tsbOpenJira.BackColor = Color.FromArgb(0, 120, 212);
            tsbOpenJira.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbOpenJira.Font = new Font("Segoe UI", 9F);
            tsbOpenJira.ForeColor = Color.White;
            tsbOpenJira.Name = "tsbOpenJira";
            tsbOpenJira.Size = new Size(23, 22);
            tsbOpenJira.Text = "🌐";
            tsbOpenJira.ToolTipText = "Apri ticket in Jira (browser)";
            // 
            // tssSeparator2
            // 
            tssSeparator2.Name = "tssSeparator2";
            tssSeparator2.Size = new Size(6, 25);
            // 
            // tsbClose
            // 
            tsbClose.Alignment = ToolStripItemAlignment.Right;
            tsbClose.DisplayStyle = ToolStripItemDisplayStyle.Text;
            tsbClose.Font = new Font("Segoe UI", 9F);
            tsbClose.ForeColor = Color.FromArgb(52, 73, 94);
            tsbClose.Name = "tsbClose";
            tsbClose.Size = new Size(23, 22);
            tsbClose.Text = "❌";
            tsbClose.ToolTipText = "Chiudi finestra (Esc)";
            // 
            // btnEsporta
            // 
            btnEsporta.BackColor = Color.FromArgb(8, 145, 178);
            btnEsporta.FlatAppearance.BorderSize = 0;
            btnEsporta.FlatStyle = FlatStyle.Flat;
            btnEsporta.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnEsporta.ForeColor = Color.White;
            btnEsporta.Location = new Point(12, 392);
            btnEsporta.Name = "btnEsporta";
            btnEsporta.Size = new Size(90, 23);
            btnEsporta.TabIndex = 4;
            btnEsporta.Text = "📊 Esporta";
            btnEsporta.UseVisualStyleBackColor = false;
            // 
            // pnlMain
            // 
            pnlMain.BackColor = Color.FromArgb(248, 249, 250);
            pnlMain.Controls.Add(pnlRight);
            pnlMain.Controls.Add(pnlCenter);
            pnlMain.Controls.Add(pnlLeft);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 115);
            pnlMain.Name = "pnlMain";
            pnlMain.Padding = new Padding(5);
            pnlMain.Size = new Size(1700, 746);
            pnlMain.TabIndex = 1;
            // 
            // pnlRight
            // 
            pnlRight.BackColor = Color.White;
            pnlRight.BorderStyle = BorderStyle.FixedSingle;
            pnlRight.Controls.Add(btnEsporta);
            pnlRight.Controls.Add(pnlTeamPlanning);
            pnlRight.Controls.Add(pnlOrganization);
            pnlRight.Dock = DockStyle.Right;
            pnlRight.Location = new Point(1321, 5);
            pnlRight.Name = "pnlRight";
            pnlRight.Padding = new Padding(5);
            pnlRight.Size = new Size(374, 736);
            pnlRight.TabIndex = 2;
            // 
            // pnlTeamPlanning
            // 
            pnlTeamPlanning.BackColor = Color.White;
            pnlTeamPlanning.BorderStyle = BorderStyle.FixedSingle;
            pnlTeamPlanning.Controls.Add(txtWBS);
            pnlTeamPlanning.Controls.Add(lblWBS);
            pnlTeamPlanning.Controls.Add(lblCommerciale);
            pnlTeamPlanning.Controls.Add(cmbCommerciale);
            pnlTeamPlanning.Controls.Add(txtResponsabile);
            pnlTeamPlanning.Controls.Add(lblResponsabile);
            pnlTeamPlanning.Controls.Add(cmbPM);
            pnlTeamPlanning.Controls.Add(lblPM);
            pnlTeamPlanning.Controls.Add(cmbConsulente);
            pnlTeamPlanning.Controls.Add(lblConsulente);
            pnlTeamPlanning.Controls.Add(lblTeamPlanningTitle);
            pnlTeamPlanning.Location = new Point(6, 189);
            pnlTeamPlanning.Name = "pnlTeamPlanning";
            pnlTeamPlanning.Size = new Size(360, 197);
            pnlTeamPlanning.TabIndex = 1;
            // 
            // txtWBS
            // 
            txtWBS.BackColor = Color.White;
            txtWBS.BorderStyle = BorderStyle.FixedSingle;
            txtWBS.Font = new Font("Segoe UI", 9F);
            txtWBS.ForeColor = Color.FromArgb(73, 80, 87);
            txtWBS.Location = new Point(90, 149);
            txtWBS.Name = "txtWBS";
            txtWBS.ReadOnly = true;
            txtWBS.Size = new Size(255, 23);
            txtWBS.TabIndex = 8;
            // 
            // lblWBS
            // 
            lblWBS.AutoSize = true;
            lblWBS.Font = new Font("Segoe UI", 9F);
            lblWBS.ForeColor = Color.FromArgb(52, 73, 94);
            lblWBS.Location = new Point(3, 151);
            lblWBS.Name = "lblWBS";
            lblWBS.Size = new Size(34, 15);
            lblWBS.TabIndex = 7;
            lblWBS.Text = "WBS:";
            // 
            // lblCommerciale
            // 
            lblCommerciale.AutoSize = true;
            lblCommerciale.Font = new Font("Segoe UI", 9F);
            lblCommerciale.ForeColor = Color.FromArgb(52, 73, 94);
            lblCommerciale.Location = new Point(2, 93);
            lblCommerciale.Name = "lblCommerciale";
            lblCommerciale.Size = new Size(81, 15);
            lblCommerciale.TabIndex = 7;
            lblCommerciale.Text = "Commerciale:";
            // 
            // cmbCommerciale
            // 
            cmbCommerciale.BackColor = Color.White;
            cmbCommerciale.Font = new Font("Segoe UI", 9F);
            cmbCommerciale.ForeColor = Color.FromArgb(73, 80, 87);
            cmbCommerciale.Location = new Point(90, 91);
            cmbCommerciale.Name = "cmbCommerciale";
            cmbCommerciale.Size = new Size(255, 23);
            cmbCommerciale.TabIndex = 8;
            // 
            // txtResponsabile
            // 
            txtResponsabile.BackColor = Color.White;
            txtResponsabile.BorderStyle = BorderStyle.FixedSingle;
            txtResponsabile.Font = new Font("Segoe UI", 9F);
            txtResponsabile.ForeColor = Color.FromArgb(73, 80, 87);
            txtResponsabile.Location = new Point(90, 120);
            txtResponsabile.Name = "txtResponsabile";
            txtResponsabile.ReadOnly = true;
            txtResponsabile.Size = new Size(255, 23);
            txtResponsabile.TabIndex = 6;
            // 
            // lblResponsabile
            // 
            lblResponsabile.AutoSize = true;
            lblResponsabile.Font = new Font("Segoe UI", 9F);
            lblResponsabile.ForeColor = Color.FromArgb(52, 73, 94);
            lblResponsabile.Location = new Point(2, 122);
            lblResponsabile.Name = "lblResponsabile";
            lblResponsabile.Size = new Size(79, 15);
            lblResponsabile.TabIndex = 5;
            lblResponsabile.Text = "Responsabile:";
            // 
            // cmbPM
            // 
            cmbPM.BackColor = Color.White;
            cmbPM.Font = new Font("Segoe UI", 9F);
            cmbPM.ForeColor = Color.FromArgb(73, 80, 87);
            cmbPM.Location = new Point(90, 62);
            cmbPM.Name = "cmbPM";
            cmbPM.Size = new Size(255, 23);
            cmbPM.TabIndex = 4;
            // 
            // lblPM
            // 
            lblPM.AutoSize = true;
            lblPM.Font = new Font("Segoe UI", 9F);
            lblPM.ForeColor = Color.FromArgb(52, 73, 94);
            lblPM.Location = new Point(5, 64);
            lblPM.Name = "lblPM";
            lblPM.Size = new Size(28, 15);
            lblPM.TabIndex = 3;
            lblPM.Text = "PM:";
            // 
            // cmbConsulente
            // 
            cmbConsulente.BackColor = Color.White;
            cmbConsulente.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConsulente.Font = new Font("Segoe UI", 9F);
            cmbConsulente.ForeColor = Color.FromArgb(73, 80, 87);
            cmbConsulente.Items.AddRange(new object[] { "anna.verdi@company.com", "marco.neri@company.com", "giulia.rossi@company.com" });
            cmbConsulente.Location = new Point(90, 32);
            cmbConsulente.Name = "cmbConsulente";
            cmbConsulente.Size = new Size(255, 23);
            cmbConsulente.TabIndex = 2;
            // 
            // lblConsulente
            // 
            lblConsulente.AutoSize = true;
            lblConsulente.Font = new Font("Segoe UI", 9F);
            lblConsulente.ForeColor = Color.FromArgb(52, 73, 94);
            lblConsulente.Location = new Point(5, 35);
            lblConsulente.Name = "lblConsulente";
            lblConsulente.Size = new Size(70, 15);
            lblConsulente.TabIndex = 1;
            lblConsulente.Text = "Consulente:";
            // 
            // lblTeamPlanningTitle
            // 
            lblTeamPlanningTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblTeamPlanningTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTeamPlanningTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblTeamPlanningTitle.Location = new Point(5, 5);
            lblTeamPlanningTitle.Name = "lblTeamPlanningTitle";
            lblTeamPlanningTitle.Padding = new Padding(5, 2, 2, 2);
            lblTeamPlanningTitle.Size = new Size(350, 20);
            lblTeamPlanningTitle.TabIndex = 0;
            lblTeamPlanningTitle.Text = "👥 TEAM PIANIFICAZIONE";
            // 
            // pnlOrganization
            // 
            pnlOrganization.BackColor = Color.White;
            pnlOrganization.BorderStyle = BorderStyle.FixedSingle;
            pnlOrganization.Controls.Add(txtClientePartner);
            pnlOrganization.Controls.Add(lblClientePartner);
            pnlOrganization.Controls.Add(txtApplicativo);
            pnlOrganization.Controls.Add(lblApplicativo);
            pnlOrganization.Controls.Add(txtArea);
            pnlOrganization.Controls.Add(lblArea);
            pnlOrganization.Controls.Add(txtCliente);
            pnlOrganization.Controls.Add(lblCliente);
            pnlOrganization.Controls.Add(lblOrganizationTitle);
            pnlOrganization.Location = new Point(5, 10);
            pnlOrganization.Name = "pnlOrganization";
            pnlOrganization.Size = new Size(360, 180);
            pnlOrganization.TabIndex = 0;
            // 
            // txtClientePartner
            // 
            txtClientePartner.BackColor = Color.White;
            txtClientePartner.BorderStyle = BorderStyle.FixedSingle;
            txtClientePartner.Font = new Font("Segoe UI", 9F);
            txtClientePartner.ForeColor = Color.FromArgb(73, 80, 87);
            txtClientePartner.Location = new Point(90, 121);
            txtClientePartner.Name = "txtClientePartner";
            txtClientePartner.ReadOnly = true;
            txtClientePartner.Size = new Size(255, 23);
            txtClientePartner.TabIndex = 10;
            // 
            // lblClientePartner
            // 
            lblClientePartner.AutoSize = true;
            lblClientePartner.Font = new Font("Segoe UI", 9F);
            lblClientePartner.ForeColor = Color.FromArgb(52, 73, 94);
            lblClientePartner.Location = new Point(2, 123);
            lblClientePartner.Name = "lblClientePartner";
            lblClientePartner.Size = new Size(48, 15);
            lblClientePartner.TabIndex = 9;
            lblClientePartner.Text = "Partner:";
            // 
            // txtApplicativo
            // 
            txtApplicativo.BackColor = Color.White;
            txtApplicativo.BorderStyle = BorderStyle.FixedSingle;
            txtApplicativo.Font = new Font("Segoe UI", 9F);
            txtApplicativo.ForeColor = Color.FromArgb(73, 80, 87);
            txtApplicativo.Location = new Point(90, 92);
            txtApplicativo.Name = "txtApplicativo";
            txtApplicativo.ReadOnly = true;
            txtApplicativo.Size = new Size(255, 23);
            txtApplicativo.TabIndex = 6;
            // 
            // lblApplicativo
            // 
            lblApplicativo.AutoSize = true;
            lblApplicativo.Font = new Font("Segoe UI", 9F);
            lblApplicativo.ForeColor = Color.FromArgb(52, 73, 94);
            lblApplicativo.Location = new Point(-1, 94);
            lblApplicativo.Name = "lblApplicativo";
            lblApplicativo.Size = new Size(70, 15);
            lblApplicativo.TabIndex = 5;
            lblApplicativo.Text = "Applicativo:";
            // 
            // txtArea
            // 
            txtArea.BackColor = Color.White;
            txtArea.BorderStyle = BorderStyle.FixedSingle;
            txtArea.Font = new Font("Segoe UI", 9F);
            txtArea.ForeColor = Color.FromArgb(73, 80, 87);
            txtArea.Location = new Point(90, 62);
            txtArea.Name = "txtArea";
            txtArea.ReadOnly = true;
            txtArea.Size = new Size(255, 23);
            txtArea.TabIndex = 4;
            // 
            // lblArea
            // 
            lblArea.AutoSize = true;
            lblArea.Font = new Font("Segoe UI", 9F);
            lblArea.ForeColor = Color.FromArgb(52, 73, 94);
            lblArea.Location = new Point(-1, 64);
            lblArea.Name = "lblArea";
            lblArea.Size = new Size(34, 15);
            lblArea.TabIndex = 3;
            lblArea.Text = "Area:";
            // 
            // txtCliente
            // 
            txtCliente.BackColor = Color.White;
            txtCliente.BorderStyle = BorderStyle.FixedSingle;
            txtCliente.Font = new Font("Segoe UI", 9F);
            txtCliente.ForeColor = Color.FromArgb(73, 80, 87);
            txtCliente.Location = new Point(90, 32);
            txtCliente.Name = "txtCliente";
            txtCliente.ReadOnly = true;
            txtCliente.Size = new Size(255, 23);
            txtCliente.TabIndex = 2;
            // 
            // lblCliente
            // 
            lblCliente.AutoSize = true;
            lblCliente.Font = new Font("Segoe UI", 9F);
            lblCliente.ForeColor = Color.FromArgb(52, 73, 94);
            lblCliente.Location = new Point(-1, 35);
            lblCliente.Name = "lblCliente";
            lblCliente.Size = new Size(47, 15);
            lblCliente.TabIndex = 1;
            lblCliente.Text = "Cliente:";
            // 
            // lblOrganizationTitle
            // 
            lblOrganizationTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblOrganizationTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblOrganizationTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblOrganizationTitle.Location = new Point(5, 5);
            lblOrganizationTitle.Name = "lblOrganizationTitle";
            lblOrganizationTitle.Padding = new Padding(5, 2, 2, 2);
            lblOrganizationTitle.Size = new Size(350, 20);
            lblOrganizationTitle.TabIndex = 0;
            lblOrganizationTitle.Text = "🏢 ORGANIZZAZIONE";
            // 
            // pnlCenter
            // 
            pnlCenter.BackColor = Color.White;
            pnlCenter.BorderStyle = BorderStyle.FixedSingle;
            pnlCenter.Controls.Add(statusStrip1);
            pnlCenter.Controls.Add(pnlActivity);
            pnlCenter.Controls.Add(pnlDescription);
            pnlCenter.Dock = DockStyle.Fill;
            pnlCenter.Location = new Point(385, 5);
            pnlCenter.Name = "pnlCenter";
            pnlCenter.Padding = new Padding(5, 10, 5, 10);
            pnlCenter.Size = new Size(1310, 736);
            pnlCenter.TabIndex = 1;
            // 
            // statusStrip1
            // 
            statusStrip1.BackColor = Color.FromArgb(248, 249, 250);
            statusStrip1.Font = new Font("Segoe UI", 8F);
            statusStrip1.Items.AddRange(new ToolStripItem[] { tslConnection, tsCommentNumber, tslLastUpdate });
            statusStrip1.Location = new Point(5, 702);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1298, 22);
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
            // tsCommentNumber
            // 
            tsCommentNumber.Name = "tsCommentNumber";
            tsCommentNumber.Size = new Size(994, 17);
            tsCommentNumber.Spring = true;
            tsCommentNumber.Text = "Numero Commenti";
            // 
            // tslLastUpdate
            // 
            tslLastUpdate.Name = "tslLastUpdate";
            tslLastUpdate.Size = new Size(187, 17);
            tslLastUpdate.Text = "🕒 Ultimo aggiornamento: 20:13:08";
            tslLastUpdate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // pnlActivity
            // 
            pnlActivity.BackColor = Color.White;
            pnlActivity.BorderStyle = BorderStyle.FixedSingle;
            pnlActivity.Controls.Add(tcActivity);
            pnlActivity.Controls.Add(lblActivityTitle);
            pnlActivity.Dock = DockStyle.Fill;
            pnlActivity.Location = new Point(5, 360);
            pnlActivity.Name = "pnlActivity";
            pnlActivity.Size = new Size(1298, 364);
            pnlActivity.TabIndex = 1;
            // 
            // tcActivity
            // 
            tcActivity.Controls.Add(tpComments);
            tcActivity.Controls.Add(tpHistory);
            tcActivity.Controls.Add(tpAttachments);
            tcActivity.Dock = DockStyle.Fill;
            tcActivity.Font = new Font("Segoe UI", 9F);
            tcActivity.Location = new Point(0, 0);
            tcActivity.Name = "tcActivity";
            tcActivity.SelectedIndex = 0;
            tcActivity.Size = new Size(1296, 362);
            tcActivity.TabIndex = 1;
            // 
            // tpComments
            // 
            tpComments.BackColor = Color.White;
            tpComments.Controls.Add(lvComments);
            tpComments.Location = new Point(4, 24);
            tpComments.Name = "tpComments";
            tpComments.Padding = new Padding(3);
            tpComments.Size = new Size(1288, 334);
            tpComments.TabIndex = 0;
            tpComments.Text = "Comments (3)";
            // 
            // lvComments
            // 
            lvComments.Dock = DockStyle.Fill;
            lvComments.Font = new Font("Segoe UI", 9F);
            lvComments.FullRowSelect = true;
            lvComments.GridLines = true;
            lvComments.Location = new Point(3, 3);
            lvComments.Name = "lvComments";
            lvComments.Size = new Size(1282, 328);
            lvComments.TabIndex = 0;
            lvComments.UseCompatibleStateImageBehavior = false;
            lvComments.View = View.Details;
            // 
            // tpHistory
            // 
            tpHistory.BackColor = Color.White;
            tpHistory.Controls.Add(lvHistory);
            tpHistory.Location = new Point(4, 24);
            tpHistory.Name = "tpHistory";
            tpHistory.Padding = new Padding(3);
            tpHistory.Size = new Size(1288, 334);
            tpHistory.TabIndex = 1;
            tpHistory.Text = "History (12)";
            // 
            // lvHistory
            // 
            lvHistory.Dock = DockStyle.Fill;
            lvHistory.Font = new Font("Segoe UI", 9F);
            lvHistory.FullRowSelect = true;
            lvHistory.GridLines = true;
            lvHistory.Location = new Point(3, 3);
            lvHistory.Name = "lvHistory";
            lvHistory.Size = new Size(1282, 328);
            lvHistory.TabIndex = 0;
            lvHistory.UseCompatibleStateImageBehavior = false;
            lvHistory.View = View.Details;
            // 
            // tpAttachments
            // 
            tpAttachments.BackColor = Color.White;
            tpAttachments.Controls.Add(lvAttachments);
            tpAttachments.Location = new Point(4, 24);
            tpAttachments.Name = "tpAttachments";
            tpAttachments.Padding = new Padding(3);
            tpAttachments.Size = new Size(1288, 334);
            tpAttachments.TabIndex = 2;
            tpAttachments.Text = "Attachments (2)";
            // 
            // lvAttachments
            // 
            lvAttachments.Dock = DockStyle.Fill;
            lvAttachments.Font = new Font("Segoe UI", 9F);
            lvAttachments.FullRowSelect = true;
            lvAttachments.GridLines = true;
            lvAttachments.Location = new Point(3, 3);
            lvAttachments.Name = "lvAttachments";
            lvAttachments.Size = new Size(1282, 328);
            lvAttachments.TabIndex = 0;
            lvAttachments.UseCompatibleStateImageBehavior = false;
            lvAttachments.View = View.Details;
            // 
            // lblActivityTitle
            // 
            lblActivityTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblActivityTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblActivityTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblActivityTitle.Location = new Point(5, 5);
            lblActivityTitle.Name = "lblActivityTitle";
            lblActivityTitle.Padding = new Padding(5, 2, 2, 2);
            lblActivityTitle.Size = new Size(840, 20);
            lblActivityTitle.TabIndex = 0;
            lblActivityTitle.Text = "💬 ATTIVITÀ";
            // 
            // pnlDescription
            // 
            pnlDescription.BackColor = Color.White;
            pnlDescription.BorderStyle = BorderStyle.FixedSingle;
            pnlDescription.Controls.Add(txtDescrizione);
            pnlDescription.Controls.Add(lblDescriptionTitle);
            pnlDescription.Dock = DockStyle.Top;
            pnlDescription.Location = new Point(5, 10);
            pnlDescription.Name = "pnlDescription";
            pnlDescription.Size = new Size(1298, 350);
            pnlDescription.TabIndex = 0;
            // 
            // txtDescrizione
            // 
            txtDescrizione.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtDescrizione.BackColor = Color.White;
            txtDescrizione.BorderStyle = BorderStyle.FixedSingle;
            txtDescrizione.Font = new Font("Segoe UI", 9F);
            txtDescrizione.ForeColor = Color.FromArgb(73, 80, 87);
            txtDescrizione.Location = new Point(15, 35);
            txtDescrizione.Multiline = true;
            txtDescrizione.Name = "txtDescrizione";
            txtDescrizione.ReadOnly = true;
            txtDescrizione.ScrollBars = ScrollBars.Vertical;
            txtDescrizione.Size = new Size(932, 308);
            txtDescrizione.TabIndex = 1;
            // 
            // lblDescriptionTitle
            // 
            lblDescriptionTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblDescriptionTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDescriptionTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblDescriptionTitle.Location = new Point(5, 5);
            lblDescriptionTitle.Name = "lblDescriptionTitle";
            lblDescriptionTitle.Padding = new Padding(5, 2, 2, 2);
            lblDescriptionTitle.Size = new Size(840, 20);
            lblDescriptionTitle.TabIndex = 0;
            lblDescriptionTitle.Text = "📝 DESCRIZIONE";
            // 
            // pnlLeft
            // 
            pnlLeft.BackColor = Color.White;
            pnlLeft.BorderStyle = BorderStyle.FixedSingle;
            pnlLeft.Controls.Add(panel1);
            pnlLeft.Controls.Add(pnlPlanningIntervento);
            pnlLeft.Controls.Add(pnlTimeline);
            pnlLeft.Controls.Add(pnlContact);
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Location = new Point(5, 5);
            pnlLeft.Name = "pnlLeft";
            pnlLeft.Padding = new Padding(5);
            pnlLeft.Size = new Size(380, 736);
            pnlLeft.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.Controls.Add(btnPianifica);
            panel1.Controls.Add(btnCommento);
            panel1.Location = new Point(11, 452);
            panel1.Name = "panel1";
            panel1.Size = new Size(362, 29);
            panel1.TabIndex = 4;
            // 
            // btnPianifica
            // 
            btnPianifica.BackColor = Color.FromArgb(0, 120, 212);
            btnPianifica.FlatAppearance.BorderSize = 0;
            btnPianifica.FlatStyle = FlatStyle.Flat;
            btnPianifica.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnPianifica.ForeColor = Color.White;
            btnPianifica.Location = new Point(3, 3);
            btnPianifica.Name = "btnPianifica";
            btnPianifica.Size = new Size(90, 23);
            btnPianifica.TabIndex = 0;
            btnPianifica.Text = "📅 Pianifica";
            btnPianifica.UseVisualStyleBackColor = false;
            // 
            // btnCommento
            // 
            btnCommento.BackColor = Color.FromArgb(108, 117, 125);
            btnCommento.FlatAppearance.BorderSize = 0;
            btnCommento.FlatStyle = FlatStyle.Flat;
            btnCommento.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btnCommento.ForeColor = Color.White;
            btnCommento.Location = new Point(268, 3);
            btnCommento.Name = "btnCommento";
            btnCommento.Size = new Size(90, 23);
            btnCommento.TabIndex = 3;
            btnCommento.Text = "💬 Commento";
            btnCommento.UseVisualStyleBackColor = false;
            // 
            // pnlPlanningIntervento
            // 
            pnlPlanningIntervento.BackColor = Color.White;
            pnlPlanningIntervento.BorderStyle = BorderStyle.FixedSingle;
            pnlPlanningIntervento.Controls.Add(txtEffort);
            pnlPlanningIntervento.Controls.Add(lblEffort);
            pnlPlanningIntervento.Controls.Add(txtOraIntervento);
            pnlPlanningIntervento.Controls.Add(lblOraIntervento);
            pnlPlanningIntervento.Controls.Add(txtDataIntervento);
            pnlPlanningIntervento.Controls.Add(lblDataIntervento);
            pnlPlanningIntervento.Controls.Add(cmbTipoPianificazione);
            pnlPlanningIntervento.Controls.Add(lblTipoPianificazione);
            pnlPlanningIntervento.Controls.Add(lblPlanningInterventoTitle);
            pnlPlanningIntervento.Location = new Point(13, 286);
            pnlPlanningIntervento.Name = "pnlPlanningIntervento";
            pnlPlanningIntervento.Size = new Size(360, 160);
            pnlPlanningIntervento.TabIndex = 2;
            // 
            // txtEffort
            // 
            txtEffort.BackColor = Color.White;
            txtEffort.BorderStyle = BorderStyle.FixedSingle;
            txtEffort.Font = new Font("Segoe UI", 9F);
            txtEffort.ForeColor = Color.FromArgb(73, 80, 87);
            txtEffort.Location = new Point(80, 92);
            txtEffort.Name = "txtEffort";
            txtEffort.Size = new Size(80, 23);
            txtEffort.TabIndex = 8;
            // 
            // lblEffort
            // 
            lblEffort.AutoSize = true;
            lblEffort.Font = new Font("Segoe UI", 9F);
            lblEffort.ForeColor = Color.FromArgb(52, 73, 94);
            lblEffort.Location = new Point(15, 95);
            lblEffort.Name = "lblEffort";
            lblEffort.Size = new Size(57, 15);
            lblEffort.TabIndex = 7;
            lblEffort.Text = "Effort (h):";
            // 
            // txtOraIntervento
            // 
            txtOraIntervento.Font = new Font("Segoe UI", 9F);
            txtOraIntervento.Location = new Point(255, 62);
            txtOraIntervento.Name = "txtOraIntervento";
            txtOraIntervento.Size = new Size(90, 23);
            txtOraIntervento.TabIndex = 6;
            // 
            // lblOraIntervento
            // 
            lblOraIntervento.AutoSize = true;
            lblOraIntervento.Font = new Font("Segoe UI", 9F);
            lblOraIntervento.ForeColor = Color.FromArgb(52, 73, 94);
            lblOraIntervento.Location = new Point(220, 65);
            lblOraIntervento.Name = "lblOraIntervento";
            lblOraIntervento.Size = new Size(29, 15);
            lblOraIntervento.TabIndex = 5;
            lblOraIntervento.Text = "Ora:";
            // 
            // txtDataIntervento
            // 
            txtDataIntervento.Font = new Font("Segoe UI", 9F);
            txtDataIntervento.Location = new Point(80, 62);
            txtDataIntervento.Name = "txtDataIntervento";
            txtDataIntervento.Size = new Size(130, 23);
            txtDataIntervento.TabIndex = 4;
            // 
            // lblDataIntervento
            // 
            lblDataIntervento.AutoSize = true;
            lblDataIntervento.Font = new Font("Segoe UI", 9F);
            lblDataIntervento.ForeColor = Color.FromArgb(52, 73, 94);
            lblDataIntervento.Location = new Point(15, 65);
            lblDataIntervento.Name = "lblDataIntervento";
            lblDataIntervento.Size = new Size(34, 15);
            lblDataIntervento.TabIndex = 3;
            lblDataIntervento.Text = "Data:";
            // 
            // cmbTipoPianificazione
            // 
            cmbTipoPianificazione.BackColor = Color.White;
            cmbTipoPianificazione.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTipoPianificazione.Font = new Font("Segoe UI", 9F);
            cmbTipoPianificazione.ForeColor = Color.FromArgb(73, 80, 87);
            cmbTipoPianificazione.Items.AddRange(new object[] { "Pianificazione Classica", "Date Multiple", "Concordare con Consulente" });
            cmbTipoPianificazione.Location = new Point(80, 32);
            cmbTipoPianificazione.Name = "cmbTipoPianificazione";
            cmbTipoPianificazione.Size = new Size(265, 23);
            cmbTipoPianificazione.TabIndex = 2;
            // 
            // lblTipoPianificazione
            // 
            lblTipoPianificazione.AutoSize = true;
            lblTipoPianificazione.Font = new Font("Segoe UI", 9F);
            lblTipoPianificazione.ForeColor = Color.FromArgb(52, 73, 94);
            lblTipoPianificazione.Location = new Point(15, 35);
            lblTipoPianificazione.Name = "lblTipoPianificazione";
            lblTipoPianificazione.Size = new Size(34, 15);
            lblTipoPianificazione.TabIndex = 1;
            lblTipoPianificazione.Text = "Tipo:";
            // 
            // lblPlanningInterventoTitle
            // 
            lblPlanningInterventoTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblPlanningInterventoTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPlanningInterventoTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblPlanningInterventoTitle.Location = new Point(5, 5);
            lblPlanningInterventoTitle.Name = "lblPlanningInterventoTitle";
            lblPlanningInterventoTitle.Padding = new Padding(5, 2, 2, 2);
            lblPlanningInterventoTitle.Size = new Size(350, 20);
            lblPlanningInterventoTitle.TabIndex = 0;
            lblPlanningInterventoTitle.Text = "📅 PIANIFICAZIONE INTERVENTO";
            // 
            // pnlTimeline
            // 
            pnlTimeline.BackColor = Color.White;
            pnlTimeline.BorderStyle = BorderStyle.FixedSingle;
            pnlTimeline.Controls.Add(txtDataCompletamento);
            pnlTimeline.Controls.Add(lblDataCompletamento);
            pnlTimeline.Controls.Add(txtDataAggiornamento);
            pnlTimeline.Controls.Add(lblDataAggiornamento);
            pnlTimeline.Controls.Add(txtDataCreazione);
            pnlTimeline.Controls.Add(lblDataCreazione);
            pnlTimeline.Controls.Add(lblTimelineTitle);
            pnlTimeline.Location = new Point(10, 150);
            pnlTimeline.Name = "pnlTimeline";
            pnlTimeline.Size = new Size(360, 130);
            pnlTimeline.TabIndex = 1;
            // 
            // txtDataCompletamento
            // 
            txtDataCompletamento.BackColor = Color.White;
            txtDataCompletamento.BorderStyle = BorderStyle.FixedSingle;
            txtDataCompletamento.Font = new Font("Segoe UI", 9F);
            txtDataCompletamento.ForeColor = Color.FromArgb(73, 80, 87);
            txtDataCompletamento.Location = new Point(80, 92);
            txtDataCompletamento.Name = "txtDataCompletamento";
            txtDataCompletamento.ReadOnly = true;
            txtDataCompletamento.Size = new Size(265, 23);
            txtDataCompletamento.TabIndex = 6;
            txtDataCompletamento.Text = "-";
            // 
            // lblDataCompletamento
            // 
            lblDataCompletamento.AutoSize = true;
            lblDataCompletamento.Font = new Font("Segoe UI", 9F);
            lblDataCompletamento.ForeColor = Color.FromArgb(52, 73, 94);
            lblDataCompletamento.Location = new Point(3, 95);
            lblDataCompletamento.Name = "lblDataCompletamento";
            lblDataCompletamento.Size = new Size(73, 15);
            lblDataCompletamento.TabIndex = 5;
            lblDataCompletamento.Text = "Completato:";
            // 
            // txtDataAggiornamento
            // 
            txtDataAggiornamento.BackColor = Color.White;
            txtDataAggiornamento.BorderStyle = BorderStyle.FixedSingle;
            txtDataAggiornamento.Font = new Font("Segoe UI", 9F);
            txtDataAggiornamento.ForeColor = Color.FromArgb(73, 80, 87);
            txtDataAggiornamento.Location = new Point(80, 62);
            txtDataAggiornamento.Name = "txtDataAggiornamento";
            txtDataAggiornamento.ReadOnly = true;
            txtDataAggiornamento.Size = new Size(265, 23);
            txtDataAggiornamento.TabIndex = 4;
            // 
            // lblDataAggiornamento
            // 
            lblDataAggiornamento.AutoSize = true;
            lblDataAggiornamento.Font = new Font("Segoe UI", 9F);
            lblDataAggiornamento.ForeColor = Color.FromArgb(52, 73, 94);
            lblDataAggiornamento.Location = new Point(5, 65);
            lblDataAggiornamento.Name = "lblDataAggiornamento";
            lblDataAggiornamento.Size = new Size(70, 15);
            lblDataAggiornamento.TabIndex = 3;
            lblDataAggiornamento.Text = "Aggiornato:";
            // 
            // txtDataCreazione
            // 
            txtDataCreazione.BackColor = Color.White;
            txtDataCreazione.BorderStyle = BorderStyle.FixedSingle;
            txtDataCreazione.Font = new Font("Segoe UI", 9F);
            txtDataCreazione.ForeColor = Color.FromArgb(73, 80, 87);
            txtDataCreazione.Location = new Point(80, 32);
            txtDataCreazione.Name = "txtDataCreazione";
            txtDataCreazione.ReadOnly = true;
            txtDataCreazione.Size = new Size(265, 23);
            txtDataCreazione.TabIndex = 2;
            // 
            // lblDataCreazione
            // 
            lblDataCreazione.AutoSize = true;
            lblDataCreazione.Font = new Font("Segoe UI", 9F);
            lblDataCreazione.ForeColor = Color.FromArgb(52, 73, 94);
            lblDataCreazione.Location = new Point(13, 36);
            lblDataCreazione.Name = "lblDataCreazione";
            lblDataCreazione.Size = new Size(45, 15);
            lblDataCreazione.TabIndex = 1;
            lblDataCreazione.Text = "Creato:";
            // 
            // lblTimelineTitle
            // 
            lblTimelineTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblTimelineTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTimelineTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblTimelineTitle.Location = new Point(5, 5);
            lblTimelineTitle.Name = "lblTimelineTitle";
            lblTimelineTitle.Padding = new Padding(5, 2, 2, 2);
            lblTimelineTitle.Size = new Size(350, 20);
            lblTimelineTitle.TabIndex = 0;
            lblTimelineTitle.Text = "📅 TIMELINE";
            // 
            // pnlContact
            // 
            pnlContact.BackColor = Color.White;
            pnlContact.BorderStyle = BorderStyle.FixedSingle;
            pnlContact.Controls.Add(txtTelefono);
            pnlContact.Controls.Add(lblTelefono);
            pnlContact.Controls.Add(txtEmail);
            pnlContact.Controls.Add(lblEmail);
            pnlContact.Controls.Add(txtRichiedente);
            pnlContact.Controls.Add(lblRichiedente);
            pnlContact.Controls.Add(lblContactTitle);
            pnlContact.Location = new Point(10, 10);
            pnlContact.Name = "pnlContact";
            pnlContact.Size = new Size(360, 130);
            pnlContact.TabIndex = 0;
            // 
            // txtTelefono
            // 
            txtTelefono.BackColor = Color.White;
            txtTelefono.BorderStyle = BorderStyle.FixedSingle;
            txtTelefono.Font = new Font("Segoe UI", 9F);
            txtTelefono.ForeColor = Color.FromArgb(73, 80, 87);
            txtTelefono.Location = new Point(80, 92);
            txtTelefono.Name = "txtTelefono";
            txtTelefono.ReadOnly = true;
            txtTelefono.Size = new Size(265, 23);
            txtTelefono.TabIndex = 6;
            // 
            // lblTelefono
            // 
            lblTelefono.AutoSize = true;
            lblTelefono.Font = new Font("Segoe UI", 9F);
            lblTelefono.ForeColor = Color.FromArgb(52, 73, 94);
            lblTelefono.Location = new Point(15, 95);
            lblTelefono.Name = "lblTelefono";
            lblTelefono.Size = new Size(56, 15);
            lblTelefono.TabIndex = 5;
            lblTelefono.Text = "Telefono:";
            // 
            // txtEmail
            // 
            txtEmail.BackColor = Color.White;
            txtEmail.BorderStyle = BorderStyle.FixedSingle;
            txtEmail.Font = new Font("Segoe UI", 9F);
            txtEmail.ForeColor = Color.FromArgb(73, 80, 87);
            txtEmail.Location = new Point(80, 62);
            txtEmail.Name = "txtEmail";
            txtEmail.ReadOnly = true;
            txtEmail.Size = new Size(265, 23);
            txtEmail.TabIndex = 4;
            // 
            // lblEmail
            // 
            lblEmail.AutoSize = true;
            lblEmail.Font = new Font("Segoe UI", 9F);
            lblEmail.ForeColor = Color.FromArgb(52, 73, 94);
            lblEmail.Location = new Point(15, 65);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(39, 15);
            lblEmail.TabIndex = 3;
            lblEmail.Text = "Email:";
            // 
            // txtRichiedente
            // 
            txtRichiedente.BackColor = Color.White;
            txtRichiedente.BorderStyle = BorderStyle.FixedSingle;
            txtRichiedente.Font = new Font("Segoe UI", 9F);
            txtRichiedente.ForeColor = Color.FromArgb(73, 80, 87);
            txtRichiedente.Location = new Point(80, 32);
            txtRichiedente.Name = "txtRichiedente";
            txtRichiedente.ReadOnly = true;
            txtRichiedente.Size = new Size(265, 23);
            txtRichiedente.TabIndex = 2;
            // 
            // lblRichiedente
            // 
            lblRichiedente.AutoSize = true;
            lblRichiedente.Font = new Font("Segoe UI", 9F);
            lblRichiedente.ForeColor = Color.FromArgb(52, 73, 94);
            lblRichiedente.Location = new Point(15, 35);
            lblRichiedente.Name = "lblRichiedente";
            lblRichiedente.Size = new Size(43, 15);
            lblRichiedente.TabIndex = 1;
            lblRichiedente.Text = "Nome:";
            // 
            // lblContactTitle
            // 
            lblContactTitle.BackColor = Color.FromArgb(248, 249, 250);
            lblContactTitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblContactTitle.ForeColor = Color.FromArgb(52, 73, 94);
            lblContactTitle.Location = new Point(5, 5);
            lblContactTitle.Name = "lblContactTitle";
            lblContactTitle.Padding = new Padding(5, 2, 2, 2);
            lblContactTitle.Size = new Size(350, 20);
            lblContactTitle.TabIndex = 0;
            lblContactTitle.Text = "👤 RICHIEDENTE";
            // 
            // TicketDetailForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(248, 249, 250);
            ClientSize = new Size(1700, 861);
            Controls.Add(pnlMain);
            Controls.Add(pnlHeader);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(1400, 900);
            Name = "TicketDetailForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Dettaglio Ticket";
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            pnlTicketInfo.ResumeLayout(false);
            pnlTicketInfo.PerformLayout();
            pnlTicketAction.ResumeLayout(false);
            pnlMetadata.ResumeLayout(false);
            tsHeader.ResumeLayout(false);
            tsHeader.PerformLayout();
            pnlMain.ResumeLayout(false);
            pnlRight.ResumeLayout(false);
            pnlTeamPlanning.ResumeLayout(false);
            pnlTeamPlanning.PerformLayout();
            pnlOrganization.ResumeLayout(false);
            pnlOrganization.PerformLayout();
            pnlCenter.ResumeLayout(false);
            pnlCenter.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            pnlActivity.ResumeLayout(false);
            tcActivity.ResumeLayout(false);
            tpComments.ResumeLayout(false);
            tpHistory.ResumeLayout(false);
            tpAttachments.ResumeLayout(false);
            pnlDescription.ResumeLayout(false);
            pnlDescription.PerformLayout();
            pnlLeft.ResumeLayout(false);
            panel1.ResumeLayout(false);
            pnlPlanningIntervento.ResumeLayout(false);
            pnlPlanningIntervento.PerformLayout();
            pnlTimeline.ResumeLayout(false);
            pnlTimeline.PerformLayout();
            pnlContact.ResumeLayout(false);
            pnlContact.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        #region Control Declarations

        // === MAIN LAYOUT PANELS ===
        private Panel pnlHeader;
        private Panel pnlMain;
        private Panel pnlLeft;
        private Panel pnlCenter;
        private Panel pnlRight;

        // === HEADER CONTROLS ===
        private ToolStrip tsHeader;
        private ToolStripLabel tslSearch;
        private ToolStripTextBox tstbTicketSearch;
        private ToolStripButton tsbSearch;
        private ToolStripSeparator tssSeparator1;
        private ToolStripButton tsbRefresh;
        private ToolStripButton tsbOpenJira;
        private ToolStripSeparator tssSeparator2;
        private ToolStripButton tsbClose;

        // === HEADER METADATA ===
        private Panel pnlTicketInfo;
        private Label lblTicketKey;
        private Label lblTicketSummary;
        private Panel pnlMetadata;
        private Label lblStatus;
        private Label lblTipo;
        private Label lblPriorita;
        private Label lblAssegnatario;

        // === LEFT PANEL - CONTACT CARD ===
        private Panel pnlContact;
        private Label lblContactTitle;
        private Label lblRichiedente;
        private TextBox txtRichiedente;
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblTelefono;
        private TextBox txtTelefono;

        // === LEFT PANEL - TIMELINE CARD ===
        private Panel pnlTimeline;
        private Label lblTimelineTitle;
        private Label lblDataCreazione;
        private TextBox txtDataCreazione;
        private Label lblDataAggiornamento;
        private TextBox txtDataAggiornamento;
        private Label lblDataCompletamento;
        private TextBox txtDataCompletamento;

        // === LEFT PANEL - PLANNING INTERVENTO CARD ===
        private Panel pnlPlanningIntervento;
        private Label lblPlanningInterventoTitle;
        private Label lblTipoPianificazione;
        private ComboBox cmbTipoPianificazione;
        private Label lblDataIntervento;
        private TextBox txtDataIntervento;
        private Label lblOraIntervento;
        private TextBox txtOraIntervento;
        private Label lblEffort;
        private TextBox txtEffort;

        // === CENTER PANEL - DESCRIPTION ===
        private Panel pnlDescription;
        private Label lblDescriptionTitle;
        private TextBox txtDescrizione;

        // === CENTER PANEL - ACTIVITY TABS ===
        private Panel pnlActivity;
        private Label lblActivityTitle;
        private TabControl tcActivity;
        private TabPage tpComments;
        private ListView lvComments;
        private TabPage tpHistory;
        private ListView lvHistory;
        private TabPage tpAttachments;
        private ListView lvAttachments;

        // === RIGHT PANEL - ORGANIZATION CARD ===
        private Panel pnlOrganization;
        private Label lblOrganizationTitle;
        private Label lblCliente;
        private TextBox txtCliente;
        private Label lblArea;
        private TextBox txtArea;
        private Label lblApplicativo;
        private TextBox txtApplicativo;
        private Label lblCommerciale;
        private ComboBox cmbCommerciale;
        private Label lblClientePartner;
        private TextBox txtClientePartner;

        // === RIGHT PANEL - TEAM PLANNING CARD ===
        private Panel pnlTeamPlanning;
        private Label lblTeamPlanningTitle;
        private Label lblConsulente;
        private ComboBox cmbConsulente;
        private Label lblPM;
        private ComboBox cmbPM;
        private Label lblWBS;

        // === ACTION BAR BUTTONS ===
        private Button btnPianifica;
        private Button btnAssegna;
        private Button btnCommento;
        private Button btnEsporta;

        #endregion

        private Button btnChiudi;
        private Panel pnlTicketAction;
        private Panel panel1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel tslConnection;
        private ToolStripStatusLabel tsCommentNumber;
        private ToolStripStatusLabel tslLastUpdate;
        private TextBox txtWBS;
        private TextBox txtResponsabile;
        private Label lblResponsabile;
    }
}