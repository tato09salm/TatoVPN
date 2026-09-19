namespace miVPN;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

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
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        notifyIcon1 = new NotifyIcon(components);
        trayContextMenu = new ContextMenuStrip(components);
        trayMenuOpen = new ToolStripMenuItem();
        trayMenuDisconnect = new ToolStripMenuItem();
        toolStripSeparator1 = new ToolStripSeparator();
        trayMenuExit = new ToolStripMenuItem();
        panelSidebar = new Panel();
        panelSidebarStatusCard = new Panel();
        lblSidebarStatusSub = new Label();
        lblSidebarStatusState = new Label();
        lblSidebarDot = new Label();
        lblSidebarStatusTitle = new Label();
        panelNavButtons = new Panel();
        btnNavAcerca = new Button();
        btnNavEscritorioRemoto = new Button();
        btnNavConexionRemota = new Button();
        btnNavModoServidor = new Button();
        btnNavFiltro = new Button();
        btnNavRegistro = new Button();
        btnNavConfigs = new Button();
        btnNavConfigSsh = new Button();
        btnNavInicio = new Button();
        btnNavDashboard = new Button();
        btnTopSettings = new Button();
        btnTopFile = new Button();
        btnTunnelType = new Button();
        panelSniRow = new Panel();
        lblSniTag = new Label();
        lblSniValue = new Label();
        btnEditSni = new Button();
        panelSniConfig = new Panel();
        lblSniConfigHeader = new Label();
        lblSniConfigSub = new Label();
        groupSniHost = new Panel();
        lblSniHostTitle = new Label();
        lblSniHostSub = new Label();
        txtSniHostInput = new TextBox();
        groupSniVersion = new Panel();
        lblSniVersionTitle = new Label();
        lblSniVersionSub = new Label();
        cmbSniVersionInput = new ComboBox();
        panelSniButtons = new Panel();
        btnSaveSni = new Button();
        btnCancelSni = new Button();
        panelTunnelType = new Panel();
        lblTunnelHeader = new Label();
        lblTunnelSub = new Label();
        groupTunnelProtocol = new Panel();
        lblTunnelProtocolTitle = new Label();
        rbTunnelSsh = new RadioButton();
        lblTunnelSshDesc = new Label();
        rbTunnelV2ray = new RadioButton();
        lblTunnelV2rayDesc = new Label();
        groupConnectFrom = new Panel();
        lblConnectFromTitle = new Label();
        rbTunnelDirect = new RadioButton();
        lblTunnelDirectDesc = new Label();
        rbTunnelTls = new RadioButton();
        lblTunnelTlsDesc = new Label();
        groupTunnelOptions = new Panel();
        lblTunnelOptionsTitle = new Label();
        chkCustomPayload = new CheckBox();
        lblCustomPayloadDesc = new Label();
        panelTunnelButtons = new Panel();
        btnSaveTunnel = new Button();
        btnCancelTunnel = new Button();
        picLogo = new PictureBox();
        panelMain = new Panel();
        panelInicio = new Panel();
        panelFeatureBadges = new Panel();
        lblFeature4Sub = new Label();
        lblFeature4Title = new Label();
        lblFeature3Sub = new Label();
        lblFeature3Title = new Label();
        lblFeature2Sub = new Label();
        lblFeature2Title = new Label();
        lblFeature1Sub = new Label();
        lblFeature1Title = new Label();
        panelQuickInfoCard = new Panel();
        lblInfoIpVal = new Label();
        lblInfoIpLabel = new Label();
        lblInfoUptimeVal = new Label();
        lblInfoUptimeLabel = new Label();
        lblInfoTunnelVal = new Label();
        lblInfoTunnelLabel = new Label();
        lblInfoProxyVal = new Label();
        lblInfoProxyLabel = new Label();
        lblInfoCipherVal = new Label();
        lblInfoCipherLabel = new Label();
        lblInfoProtoVal = new Label();
        lblInfoProtoLabel = new Label();
        lblQuickInfoTitle = new Label();
        panelMainConnectionCard = new Panel();
        btnGoToConfig = new Button();
        lblQuickConfigSummary = new Label();
        btnDisconnect = new Button();
        btnConnect = new Button();
        lblRingStatusSub = new Label();
        lblRingStatusText = new Label();
        lblRingLockIcon = new Label();
        picStatusRing = new PictureBox();
        lblStatusRingTitle = new Label();
        panelConfigSsh = new Panel();
        panelConfigSshButtons = new Panel();
        btnConnectFromConfig = new Button();
        btnSaveConfig = new Button();
        groupAdvancedSsh = new Panel();
        lblAdvancedSshTitle = new Label();
        lblInternalSshDesc = new Label();
        groupTls = new Panel();
        chkUseTls = new CheckBox();
        txtTlsSni = new TextBox();
        lblTlsSni = new Label();
        numTlsPort = new NumericUpDown();
        lblTlsPort = new Label();
        lblTlsGroupTitle = new Label();
        groupSocks = new Panel();
        chkEnableTun = new CheckBox();
        lblSocksPort = new Label();
        numSocksPort = new NumericUpDown();
        txtSocksIp = new TextBox();
        lblSocksIp = new Label();
        lblSocksGroupTitle = new Label();
        groupCredentials = new Panel();
        btnTogglePass = new Button();
        txtPassword = new TextBox();
        lblPassword = new Label();
        txtUsername = new TextBox();
        lblUsername = new Label();
        lblCredGroupTitle = new Label();
        groupSsh = new Panel();
        lblSshPort = new Label();
        numSshPort = new NumericUpDown();
        txtSshHost = new TextBox();
        lblSshHost = new Label();
        lblSshGroupTitle = new Label();
        lblConfigSshSub = new Label();
        lblConfigSshHeader = new Label();
        panelConfigs = new Panel();
        dgvConfigs = new DataGridView();
        colCfgName = new DataGridViewTextBoxColumn();
        colCfgDate = new DataGridViewTextBoxColumn();
        colCfgUse = new DataGridViewButtonColumn();
        colCfgDelete = new DataGridViewButtonColumn();
        panelConfigsTop = new Panel();
        btnSaveConfigTop = new Button();
        lblConfigsCount = new Label();
        lblConfigsTitle = new Label();
        panelRegistro = new Panel();
        rtbLogs = new RichTextBox();
        panelRegistroTop = new Panel();
        btnClearHistory = new Button();
        lblHistoryCount = new Label();
        lblHistoryTitle = new Label();
        panelAcercaDe = new Panel();
        panelModoServidor = new Panel();
        panelAcercaDonationsCard = new Panel();
        lblAcercaYapeThank = new Label();
        lblAcercaYapeBadge = new Label();
        panelAcercaYapeBorder = new Panel();
        picAcercaYape = new PictureBox();
        lblAcercaDonationsSub = new Label();
        lblAcercaDonationsTitle = new Label();
        panelAcercaInfoCard = new Panel();
        lblAcercaCopyright = new Label();
        lblAcercaGratis = new Label();
        panelAcercaDivider2 = new Panel();
        lblSpec6Val = new Label();
        lblSpec6Label = new Label();
        lblSpec5Val = new Label();
        lblSpec5Label = new Label();
        lblSpec4Val = new Label();
        lblSpec4Label = new Label();
        lblSpec3Val = new Label();
        lblSpec3Label = new Label();
        lblSpec2Val = new Label();
        lblSpec2Label = new Label();
        lblSpec1Val = new Label();
        lblSpec1Label = new Label();
        lblAcercaSpecsTitle = new Label();
        lblAcercaDescription = new Label();
        panelAcercaDivider1 = new Panel();
        lblAcercaVersion = new Label();
        lblAcercaAppTitle = new Label();
        picAcercaLogo = new PictureBox();
        lblAcercaSub = new Label();
        lblAcercaHeader = new Label();
        panelTopHeader = new Panel();
        lblSubtitle = new Label();
        lblTitle = new Label();
        lblState = new Label();
        lblStateValue = new Label();
        groupStatus = new GroupBox();
        trayContextMenu.SuspendLayout();
        panelSidebar.SuspendLayout();
        panelSidebarStatusCard.SuspendLayout();
        panelNavButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
        panelMain.SuspendLayout();
        panelInicio.SuspendLayout();
        panelFeatureBadges.SuspendLayout();
        panelQuickInfoCard.SuspendLayout();
        panelMainConnectionCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picStatusRing).BeginInit();
        panelConfigSsh.SuspendLayout();
        panelConfigSshButtons.SuspendLayout();
        groupTls.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numTlsPort).BeginInit();
        groupSocks.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numSocksPort).BeginInit();
        groupCredentials.SuspendLayout();
        groupSsh.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numSshPort).BeginInit();
        panelConfigs.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvConfigs).BeginInit();
        panelConfigsTop.SuspendLayout();
        panelRegistro.SuspendLayout();
        panelRegistroTop.SuspendLayout();
        panelAcercaDe.SuspendLayout();
        panelModoServidor.SuspendLayout();
        panelAcercaDonationsCard.SuspendLayout();
        panelAcercaYapeBorder.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picAcercaYape).BeginInit();
        panelAcercaInfoCard.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picAcercaLogo).BeginInit();
        panelTopHeader.SuspendLayout();
        SuspendLayout();
        // 
        // notifyIcon1
        // 
        notifyIcon1.ContextMenuStrip = trayContextMenu;
        notifyIcon1.Text = "TatoVPN - Desconectado";
        notifyIcon1.Visible = true;
        notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
        // 
        // trayContextMenu
        // 
        trayContextMenu.ImageScalingSize = new Size(20, 20);
        trayContextMenu.Items.AddRange(new ToolStripItem[] { trayMenuOpen, trayMenuDisconnect, toolStripSeparator1, trayMenuExit });
        trayContextMenu.Name = "trayContextMenu";
        trayContextMenu.Size = new Size(207, 82);
        // 
        // trayMenuOpen
        // 
        trayMenuOpen.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        trayMenuOpen.Name = "trayMenuOpen";
        trayMenuOpen.Size = new Size(206, 24);
        trayMenuOpen.Text = "👁️ Abrir TatoVPN";
        trayMenuOpen.Click += trayMenuOpen_Click;
        // 
        // trayMenuDisconnect
        // 
        trayMenuDisconnect.Name = "trayMenuDisconnect";
        trayMenuDisconnect.Size = new Size(206, 24);
        trayMenuDisconnect.Text = "⏹ Desconectar";
        trayMenuDisconnect.Click += btnDisconnect_Click;
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(203, 6);
        // 
        // trayMenuExit
        // 
        trayMenuExit.Name = "trayMenuExit";
        trayMenuExit.Size = new Size(206, 24);
        trayMenuExit.Text = "❌ Salir";
        trayMenuExit.Click += trayMenuExit_Click;
        // 
        // panelSidebar
        // 
        panelSidebar.BackColor = Color.FromArgb(17, 24, 39);
        panelSidebar.Controls.Add(panelSidebarStatusCard);
        panelSidebar.Controls.Add(panelNavButtons);
        panelSidebar.Controls.Add(picLogo);
        panelSidebar.Dock = DockStyle.Left;
        panelSidebar.Location = new Point(0, 0);
        panelSidebar.Name = "panelSidebar";
        panelSidebar.Size = new Size(280, 700);
        panelSidebar.TabIndex = 0;
        // 
        // panelSidebarStatusCard
        // 
        panelSidebarStatusCard.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelSidebarStatusCard.BackColor = Color.FromArgb(30, 41, 59);
        panelSidebarStatusCard.Controls.Add(lblSidebarStatusSub);
        panelSidebarStatusCard.Controls.Add(lblSidebarStatusState);
        panelSidebarStatusCard.Controls.Add(lblSidebarDot);
        panelSidebarStatusCard.Controls.Add(lblSidebarStatusTitle);
        panelSidebarStatusCard.Location = new Point(12, 590);
        panelSidebarStatusCard.Name = "panelSidebarStatusCard";
        panelSidebarStatusCard.Size = new Size(256, 95);
        panelSidebarStatusCard.TabIndex = 2;
        // 
        // lblSidebarStatusSub
        // 
        lblSidebarStatusSub.AutoEllipsis = true;
        lblSidebarStatusSub.AutoSize = false;
        lblSidebarStatusSub.Font = new Font("Segoe UI", 8F);
        lblSidebarStatusSub.ForeColor = Color.FromArgb(100, 116, 139);
        lblSidebarStatusSub.Location = new Point(12, 65);
        lblSidebarStatusSub.Name = "lblSidebarStatusSub";
        lblSidebarStatusSub.Size = new Size(232, 22);
        lblSidebarStatusSub.TabIndex = 3;
        lblSidebarStatusSub.Text = "No hay conexión activa";
        // 
        // lblSidebarStatusState
        // 
        lblSidebarStatusState.AutoSize = true;
        lblSidebarStatusState.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblSidebarStatusState.ForeColor = Color.FromArgb(148, 163, 184);
        lblSidebarStatusState.Location = new Point(12, 40);
        lblSidebarStatusState.Name = "lblSidebarStatusState";
        lblSidebarStatusState.Size = new Size(121, 23);
        lblSidebarStatusState.TabIndex = 2;
        lblSidebarStatusState.Text = "Desconectado";
        // 
        // lblSidebarDot
        // 
        lblSidebarDot.AutoSize = true;
        lblSidebarDot.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblSidebarDot.ForeColor = Color.FromArgb(100, 116, 139);
        lblSidebarDot.Location = new Point(12, 12);
        lblSidebarDot.Name = "lblSidebarDot";
        lblSidebarDot.Size = new Size(24, 28);
        lblSidebarDot.TabIndex = 0;
        lblSidebarDot.Text = "●";
        // 
        // lblSidebarStatusTitle
        // 
        lblSidebarStatusTitle.AutoSize = true;
        lblSidebarStatusTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSidebarStatusTitle.ForeColor = Color.FromArgb(226, 232, 240);
        lblSidebarStatusTitle.Location = new Point(34, 15);
        lblSidebarStatusTitle.Name = "lblSidebarStatusTitle";
        lblSidebarStatusTitle.Size = new Size(102, 20);
        lblSidebarStatusTitle.TabIndex = 1;
        lblSidebarStatusTitle.Text = "Estado actual";
        // 
        // panelNavButtons
        // 
        panelNavButtons.Controls.Add(btnNavAcerca);
        panelNavButtons.Controls.Add(btnNavEscritorioRemoto);
        panelNavButtons.Controls.Add(btnNavConexionRemota);
        panelNavButtons.Controls.Add(btnNavModoServidor);
        panelNavButtons.Controls.Add(btnNavRegistro);
        panelNavButtons.Controls.Add(btnNavFiltro);
        panelNavButtons.Controls.Add(btnNavConfigs);
        panelNavButtons.Controls.Add(btnNavConfigSsh);
        panelNavButtons.Controls.Add(btnNavInicio);
        panelNavButtons.Controls.Add(btnNavDashboard);
        panelNavButtons.AutoScroll = true;
        panelNavButtons.Location = new Point(12, 112);
        panelNavButtons.Name = "panelNavButtons";
        panelNavButtons.Size = new Size(256, 468);
        panelNavButtons.TabIndex = 1;
        // 
        // btnNavAcerca
        // 
        btnNavAcerca.BackColor = Color.Transparent;
        btnNavAcerca.Cursor = Cursors.Hand;
        btnNavAcerca.FlatAppearance.BorderSize = 0;
        btnNavAcerca.FlatStyle = FlatStyle.Flat;
        btnNavAcerca.Font = new Font("Segoe UI", 9F);
        btnNavAcerca.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavAcerca.Location = new Point(0, 414);
        btnNavAcerca.Name = "btnNavAcerca";
        btnNavAcerca.Padding = new Padding(8, 0, 0, 0);
        btnNavAcerca.Size = new Size(256, 42);
        btnNavAcerca.TabIndex = 8;
        btnNavAcerca.Text = "ℹ️  Acerca de";
        btnNavAcerca.TextAlign = ContentAlignment.MiddleLeft;
        btnNavAcerca.UseVisualStyleBackColor = false;
        btnNavAcerca.Click += btnNavAcerca_Click;
        // 
        // btnNavConexionRemota
        // 
        btnNavConexionRemota.BackColor = Color.Transparent;
        btnNavConexionRemota.Cursor = Cursors.Hand;
        btnNavConexionRemota.FlatAppearance.BorderSize = 0;
        btnNavConexionRemota.FlatStyle = FlatStyle.Flat;
        btnNavConexionRemota.Font = new Font("Segoe UI", 9F);
        btnNavConexionRemota.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavConexionRemota.Location = new Point(0, 322);
        btnNavConexionRemota.Name = "btnNavConexionRemota";
        btnNavConexionRemota.Padding = new Padding(8, 0, 0, 0);
        btnNavConexionRemota.Size = new Size(256, 42);
        btnNavConexionRemota.TabIndex = 7;
        btnNavConexionRemota.Text = "📁  Conexión Remota";
        btnNavConexionRemota.TextAlign = ContentAlignment.MiddleLeft;
        btnNavConexionRemota.UseVisualStyleBackColor = false;
        btnNavConexionRemota.Click += btnNavConexionRemota_Click;
        // 
        // btnNavEscritorioRemoto
        // 
        btnNavEscritorioRemoto.BackColor = Color.Transparent;
        btnNavEscritorioRemoto.Cursor = Cursors.Hand;
        btnNavEscritorioRemoto.FlatAppearance.BorderSize = 0;
        btnNavEscritorioRemoto.FlatStyle = FlatStyle.Flat;
        btnNavEscritorioRemoto.Font = new Font("Segoe UI", 9F);
        btnNavEscritorioRemoto.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavEscritorioRemoto.Location = new Point(0, 368);
        btnNavEscritorioRemoto.Name = "btnNavEscritorioRemoto";
        btnNavEscritorioRemoto.Padding = new Padding(8, 0, 0, 0);
        btnNavEscritorioRemoto.Size = new Size(256, 42);
        btnNavEscritorioRemoto.TabIndex = 9;
        btnNavEscritorioRemoto.Text = "🖥️  Escritorio Remoto";
        btnNavEscritorioRemoto.TextAlign = ContentAlignment.MiddleLeft;
        btnNavEscritorioRemoto.UseVisualStyleBackColor = false;
        btnNavEscritorioRemoto.Click += btnNavEscritorioRemoto_Click;
        // 
        // btnNavModoServidor
        // 
        btnNavModoServidor.BackColor = Color.Transparent;
        btnNavModoServidor.Cursor = Cursors.Hand;
        btnNavModoServidor.FlatAppearance.BorderSize = 0;
        btnNavModoServidor.FlatStyle = FlatStyle.Flat;
        btnNavModoServidor.Font = new Font("Segoe UI", 9F);
        btnNavModoServidor.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavModoServidor.Location = new Point(0, 276);
        btnNavModoServidor.Name = "btnNavModoServidor";
        btnNavModoServidor.Padding = new Padding(8, 0, 0, 0);
        btnNavModoServidor.Size = new Size(256, 42);
        btnNavModoServidor.TabIndex = 6;
        btnNavModoServidor.Text = "🖥️  Modo Servidor";
        btnNavModoServidor.TextAlign = ContentAlignment.MiddleLeft;
        btnNavModoServidor.UseVisualStyleBackColor = false;
        btnNavModoServidor.Click += btnNavModoServidor_Click;
        // 
        // btnNavRegistro
        // 
        btnNavRegistro.BackColor = Color.Transparent;
        btnNavRegistro.Cursor = Cursors.Hand;
        btnNavRegistro.FlatAppearance.BorderSize = 0;
        btnNavRegistro.FlatStyle = FlatStyle.Flat;
        btnNavRegistro.Font = new Font("Segoe UI", 9F);
        btnNavRegistro.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavRegistro.Location = new Point(0, 230);
        btnNavRegistro.Name = "btnNavRegistro";
        btnNavRegistro.Padding = new Padding(8, 0, 0, 0);
        btnNavRegistro.Size = new Size(256, 42);
        btnNavRegistro.TabIndex = 5;
        btnNavRegistro.Text = "📈  Registro de conexión";
        btnNavRegistro.TextAlign = ContentAlignment.MiddleLeft;
        btnNavRegistro.UseVisualStyleBackColor = false;
        btnNavRegistro.Click += btnNavRegistro_Click;
        // 
        // btnNavFiltro
        // 
        btnNavFiltro.BackColor = Color.Transparent;
        btnNavFiltro.Cursor = Cursors.Hand;
        btnNavFiltro.FlatAppearance.BorderSize = 0;
        btnNavFiltro.FlatStyle = FlatStyle.Flat;
        btnNavFiltro.Font = new Font("Segoe UI", 9F);
        btnNavFiltro.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavFiltro.Location = new Point(0, 184);
        btnNavFiltro.Name = "btnNavFiltro";
        btnNavFiltro.Padding = new Padding(8, 0, 0, 0);
        btnNavFiltro.Size = new Size(256, 42);
        btnNavFiltro.TabIndex = 4;
        btnNavFiltro.Text = "🛡️  Filtrado de Contenido";
        btnNavFiltro.TextAlign = ContentAlignment.MiddleLeft;
        btnNavFiltro.UseVisualStyleBackColor = false;
        btnNavFiltro.Click += btnNavFiltro_Click;
        // 
        // btnNavConfigs
        // 
        btnNavConfigs.BackColor = Color.Transparent;
        btnNavConfigs.Cursor = Cursors.Hand;
        btnNavConfigs.FlatAppearance.BorderSize = 0;
        btnNavConfigs.FlatStyle = FlatStyle.Flat;
        btnNavConfigs.Font = new Font("Segoe UI", 9F);
        btnNavConfigs.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavConfigs.Location = new Point(0, 138);
        btnNavConfigs.Name = "btnNavConfigs";
        btnNavConfigs.Padding = new Padding(8, 0, 0, 0);
        btnNavConfigs.Size = new Size(256, 42);
        btnNavConfigs.TabIndex = 3;
        btnNavConfigs.Text = "💾  Mis configuraciones";
        btnNavConfigs.TextAlign = ContentAlignment.MiddleLeft;
        btnNavConfigs.UseVisualStyleBackColor = false;
        btnNavConfigs.Click += btnNavConfigs_Click;
        // 
        // btnNavConfigSsh
        // 
        btnNavConfigSsh.BackColor = Color.Transparent;
        btnNavConfigSsh.Cursor = Cursors.Hand;
        btnNavConfigSsh.FlatAppearance.BorderSize = 0;
        btnNavConfigSsh.FlatStyle = FlatStyle.Flat;
        btnNavConfigSsh.Font = new Font("Segoe UI", 9F);
        btnNavConfigSsh.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavConfigSsh.Location = new Point(0, 92);
        btnNavConfigSsh.Name = "btnNavConfigSsh";
        btnNavConfigSsh.Padding = new Padding(8, 0, 0, 0);
        btnNavConfigSsh.Size = new Size(256, 42);
        btnNavConfigSsh.TabIndex = 2;
        btnNavConfigSsh.Text = "🔑  Configuración SSH";
        btnNavConfigSsh.TextAlign = ContentAlignment.MiddleLeft;
        btnNavConfigSsh.UseVisualStyleBackColor = false;
        btnNavConfigSsh.Click += btnNavConfigSsh_Click;
        // 
        // btnNavInicio
        // 
        btnNavInicio.BackColor = Color.FromArgb(234, 88, 12);
        btnNavInicio.Cursor = Cursors.Hand;
        btnNavInicio.FlatAppearance.BorderSize = 0;
        btnNavInicio.FlatStyle = FlatStyle.Flat;
        btnNavInicio.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnNavInicio.ForeColor = Color.White;
        btnNavInicio.Location = new Point(0, 46);
        btnNavInicio.Name = "btnNavInicio";
        btnNavInicio.Padding = new Padding(8, 0, 0, 0);
        btnNavInicio.Size = new Size(256, 42);
        btnNavInicio.TabIndex = 1;
        btnNavInicio.Text = "🏠  Inicio";
        btnNavInicio.TextAlign = ContentAlignment.MiddleLeft;
        btnNavInicio.UseVisualStyleBackColor = false;
        btnNavInicio.Click += btnNavInicio_Click;
        // 
        // btnNavDashboard
        // 
        btnNavDashboard.BackColor = Color.Transparent;
        btnNavDashboard.Cursor = Cursors.Hand;
        btnNavDashboard.FlatAppearance.BorderSize = 0;
        btnNavDashboard.FlatStyle = FlatStyle.Flat;
        btnNavDashboard.Font = new Font("Segoe UI", 9F);
        btnNavDashboard.ForeColor = Color.FromArgb(148, 163, 184);
        btnNavDashboard.Location = new Point(0, 0);
        btnNavDashboard.Name = "btnNavDashboard";
        btnNavDashboard.Padding = new Padding(8, 0, 0, 0);
        btnNavDashboard.Size = new Size(256, 42);
        btnNavDashboard.TabIndex = 0;
        btnNavDashboard.Text = "📊  Dashboard";
        btnNavDashboard.TextAlign = ContentAlignment.MiddleLeft;
        btnNavDashboard.UseVisualStyleBackColor = false;
        btnNavDashboard.Click += btnNavDashboard_Click;
        // 
        // picLogo
        // 
        picLogo.Location = new Point(12, 10);
        picLogo.Name = "picLogo";
        picLogo.Size = new Size(256, 95);
        picLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picLogo.TabIndex = 0;
        picLogo.TabStop = false;
        // 
        // panelMain
        // 
        panelMain.BackColor = Color.FromArgb(11, 15, 25);
        panelMain.Controls.Add(panelInicio);
        panelMain.Controls.Add(panelTunnelType);
        panelMain.Controls.Add(panelSniConfig);
        panelMain.Controls.Add(panelConfigSsh);
        panelMain.Controls.Add(panelConfigs);
        panelMain.Controls.Add(panelRegistro);
        panelMain.Controls.Add(panelModoServidor);
        panelMain.Controls.Add(panelAcercaDe);
        panelMain.Dock = DockStyle.Fill;
        panelMain.Location = new Point(235, 0);
        panelMain.Name = "panelMain";
        panelMain.Size = new Size(855, 700);
        panelMain.TabIndex = 1;
        // 
        // panelInicio
        // 
        panelInicio.BackColor = Color.FromArgb(11, 15, 25);
        panelInicio.Controls.Add(panelSniRow);
        panelInicio.Controls.Add(btnTunnelType);
        panelInicio.Controls.Add(btnTopSettings);
        panelInicio.Controls.Add(btnTopFile);
        panelInicio.Controls.Add(btnDisconnect);
        panelInicio.Controls.Add(btnConnect);
        panelInicio.Controls.Add(lblRingLockIcon);
        panelInicio.Controls.Add(picStatusRing);
        panelInicio.Dock = DockStyle.Fill;
        panelInicio.Location = new Point(0, 0);
        panelInicio.Name = "panelInicio";
        panelInicio.Padding = new Padding(20, 5, 20, 15);
        panelInicio.Size = new Size(855, 700);
        panelInicio.TabIndex = 1;
        // 
        // panelSniRow
        // 
        panelSniRow.BackColor = Color.FromArgb(22, 32, 48);
        panelSniRow.Controls.Add(btnEditSni);
        panelSniRow.Controls.Add(lblSniValue);
        panelSniRow.Controls.Add(lblSniTag);
        panelSniRow.Location = new Point(307, 444);
        panelSniRow.Name = "panelSniRow";
        panelSniRow.Size = new Size(240, 34);
        panelSniRow.TabIndex = 8;
        panelSniRow.Visible = false;
        panelSniRow.Anchor = AnchorStyles.None;
        // 
        // lblSniTag
        // 
        lblSniTag.AutoSize = true;
        lblSniTag.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
        lblSniTag.ForeColor = Color.FromArgb(234, 88, 12);
        lblSniTag.Location = new Point(8, 8);
        lblSniTag.Name = "lblSniTag";
        lblSniTag.Size = new Size(36, 19);
        lblSniTag.TabIndex = 0;
        lblSniTag.Text = "SNI:";
        // 
        // lblSniValue
        // 
        lblSniValue.AutoEllipsis = true;
        lblSniValue.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSniValue.ForeColor = Color.FromArgb(226, 232, 240);
        lblSniValue.Location = new Point(44, 7);
        lblSniValue.Name = "lblSniValue";
        lblSniValue.Size = new Size(160, 20);
        lblSniValue.TabIndex = 1;
        lblSniValue.Text = "m.facebook.com";
        lblSniValue.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnEditSni
        // 
        btnEditSni.BackColor = Color.Transparent;
        btnEditSni.Cursor = Cursors.Hand;
        btnEditSni.FlatAppearance.BorderSize = 0;
        btnEditSni.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
        btnEditSni.FlatStyle = FlatStyle.Flat;
        btnEditSni.Font = new Font("Segoe UI Emoji", 9F);
        btnEditSni.ForeColor = Color.FromArgb(96, 165, 250);
        btnEditSni.Location = new Point(208, 3);
        btnEditSni.Name = "btnEditSni";
        btnEditSni.Size = new Size(28, 28);
        btnEditSni.TabIndex = 2;
        btnEditSni.Text = "✏️";
        btnEditSni.UseVisualStyleBackColor = false;
        btnEditSni.Click += btnEditSni_Click;
        // 
        // btnTunnelType
        // 
        btnTunnelType.BackColor = Color.FromArgb(22, 32, 48);
        btnTunnelType.Cursor = Cursors.Hand;
        btnTunnelType.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        btnTunnelType.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
        btnTunnelType.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 65, 85);
        btnTunnelType.FlatStyle = FlatStyle.Flat;
        btnTunnelType.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnTunnelType.ForeColor = Color.FromArgb(226, 232, 240);
        btnTunnelType.Location = new Point(317, 398);
        btnTunnelType.Name = "btnTunnelType";
        btnTunnelType.Size = new Size(220, 36);
        btnTunnelType.TabIndex = 7;
        btnTunnelType.Text = "🛡️ SSH · Direct  ▼";
        btnTunnelType.UseVisualStyleBackColor = false;
        btnTunnelType.Click += btnTunnelType_Click;
        btnTunnelType.Anchor = AnchorStyles.None;
        // 
        // btnTopSettings
        // 
        btnTopSettings.BackColor = Color.FromArgb(22, 32, 48);
        btnTopSettings.Cursor = Cursors.Hand;
        btnTopSettings.FlatAppearance.BorderSize = 0;
        btnTopSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
        btnTopSettings.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 65, 85);
        btnTopSettings.FlatStyle = FlatStyle.Flat;
        btnTopSettings.Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular);
        btnTopSettings.ForeColor = Color.FromArgb(226, 232, 240);
        btnTopSettings.Location = new Point(795, 18);
        btnTopSettings.Name = "btnTopSettings";
        btnTopSettings.Size = new Size(40, 40);
        btnTopSettings.TabIndex = 8;
        btnTopSettings.Text = "⚙️";
        btnTopSettings.UseVisualStyleBackColor = false;
        btnTopSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        // 
        // btnTopFile
        // 
        btnTopFile.BackColor = Color.FromArgb(22, 32, 48);
        btnTopFile.Cursor = Cursors.Hand;
        btnTopFile.FlatAppearance.BorderSize = 0;
        btnTopFile.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
        btnTopFile.FlatAppearance.MouseDownBackColor = Color.FromArgb(51, 65, 85);
        btnTopFile.FlatStyle = FlatStyle.Flat;
        btnTopFile.Font = new Font("Segoe UI Emoji", 13F, FontStyle.Regular);
        btnTopFile.ForeColor = Color.FromArgb(226, 232, 240);
        btnTopFile.Location = new Point(745, 18);
        btnTopFile.Name = "btnTopFile";
        btnTopFile.Size = new Size(40, 40);
        btnTopFile.TabIndex = 7;
        btnTopFile.Text = "📄";
        btnTopFile.UseVisualStyleBackColor = false;
        btnTopFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        // 
        // btnDisconnect
        // 
        btnDisconnect.BackColor = Color.FromArgb(153, 27, 27);
        btnDisconnect.Cursor = Cursors.Hand;
        btnDisconnect.FlatAppearance.BorderSize = 0;
        btnDisconnect.FlatStyle = FlatStyle.Flat;
        btnDisconnect.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnDisconnect.ForeColor = Color.White;
        btnDisconnect.Location = new Point(342, 342);
        btnDisconnect.Name = "btnDisconnect";
        btnDisconnect.Size = new Size(170, 44);
        btnDisconnect.TabIndex = 6;
        btnDisconnect.Text = "⏹  Desconectar";
        btnDisconnect.UseVisualStyleBackColor = false;
        btnDisconnect.Visible = false;
        btnDisconnect.Click += btnDisconnect_Click;
        btnDisconnect.Anchor = AnchorStyles.None;
        // 
        // btnConnect
        // 
        btnConnect.BackColor = Color.FromArgb(22, 163, 74);
        btnConnect.Cursor = Cursors.Hand;
        btnConnect.FlatAppearance.BorderSize = 0;
        btnConnect.FlatStyle = FlatStyle.Flat;
        btnConnect.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        btnConnect.ForeColor = Color.White;
        btnConnect.Location = new Point(342, 342);
        btnConnect.Name = "btnConnect";
        btnConnect.Size = new Size(170, 44);
        btnConnect.TabIndex = 5;
        btnConnect.Text = "▶  Conectar";
        btnConnect.UseVisualStyleBackColor = false;
        btnConnect.Click += btnConnect_Click;
        btnConnect.Anchor = AnchorStyles.None;
        // 
        // lblRingLockIcon
        // 
        lblRingLockIcon.BackColor = Color.Transparent;
        lblRingLockIcon.Cursor = Cursors.Hand;
        lblRingLockIcon.Font = new Font("Segoe UI", 32F);
        lblRingLockIcon.ForeColor = Color.FromArgb(234, 88, 12);
        lblRingLockIcon.Location = new Point(342, 145);
        lblRingLockIcon.Name = "lblRingLockIcon";
        lblRingLockIcon.Size = new Size(170, 55);
        lblRingLockIcon.TabIndex = 2;
        lblRingLockIcon.Text = "🔓";
        lblRingLockIcon.TextAlign = ContentAlignment.MiddleCenter;
        lblRingLockIcon.Visible = false;
        lblRingLockIcon.Click += StatusRing_Click;
        lblRingLockIcon.Anchor = AnchorStyles.None;
        // 
        // picStatusRing
        // 
        picStatusRing.BackColor = Color.Transparent;
        picStatusRing.Cursor = Cursors.Hand;
        picStatusRing.Location = new Point(342, 110);
        picStatusRing.Name = "picStatusRing";
        picStatusRing.Size = new Size(170, 170);
        picStatusRing.TabIndex = 1;
        picStatusRing.TabStop = false;
        picStatusRing.Click += StatusRing_Click;
        picStatusRing.Paint += PicStatusRing_Paint;
        picStatusRing.Anchor = AnchorStyles.None;
        // 
        // panelFeatureBadges
        // 
        panelFeatureBadges.BackColor = Color.Transparent;
        panelFeatureBadges.Controls.Add(lblFeature4Sub);
        panelFeatureBadges.Controls.Add(lblFeature4Title);
        panelFeatureBadges.Controls.Add(lblFeature3Sub);
        panelFeatureBadges.Controls.Add(lblFeature3Title);
        panelFeatureBadges.Controls.Add(lblFeature2Sub);
        panelFeatureBadges.Controls.Add(lblFeature2Title);
        panelFeatureBadges.Controls.Add(lblFeature1Sub);
        panelFeatureBadges.Controls.Add(lblFeature1Title);
        panelFeatureBadges.Location = new Point(20, 548);
        panelFeatureBadges.Name = "panelFeatureBadges";
        panelFeatureBadges.Size = new Size(815, 80);
        panelFeatureBadges.TabIndex = 2;
        // 
        // lblFeature4Sub
        // 
        lblFeature4Sub.Font = new Font("Segoe UI", 7.5F);
        lblFeature4Sub.ForeColor = Color.FromArgb(148, 163, 184);
        lblFeature4Sub.Location = new Point(600, 24);
        lblFeature4Sub.Name = "lblFeature4Sub";
        lblFeature4Sub.Size = new Size(170, 45);
        lblFeature4Sub.TabIndex = 7;
        lblFeature4Sub.Text = "Conexión estable y permanente";
        // 
        // lblFeature4Title
        // 
        lblFeature4Title.AutoSize = true;
        lblFeature4Title.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblFeature4Title.ForeColor = Color.FromArgb(248, 250, 252);
        lblFeature4Title.Location = new Point(600, 5);
        lblFeature4Title.Name = "lblFeature4Title";
        lblFeature4Title.Size = new Size(105, 20);
        lblFeature4Title.TabIndex = 6;
        lblFeature4Title.Text = "🛡️  Confiable";
        // 
        // lblFeature3Sub
        // 
        lblFeature3Sub.Font = new Font("Segoe UI", 7.5F);
        lblFeature3Sub.ForeColor = Color.FromArgb(148, 163, 184);
        lblFeature3Sub.Location = new Point(400, 24);
        lblFeature3Sub.Name = "lblFeature3Sub";
        lblFeature3Sub.Size = new Size(170, 45);
        lblFeature3Sub.TabIndex = 5;
        lblFeature3Sub.Text = "Optimizado para mejor rendimiento";
        // 
        // lblFeature3Title
        // 
        lblFeature3Title.AutoSize = true;
        lblFeature3Title.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblFeature3Title.ForeColor = Color.FromArgb(248, 250, 252);
        lblFeature3Title.Location = new Point(400, 5);
        lblFeature3Title.Name = "lblFeature3Title";
        lblFeature3Title.Size = new Size(88, 20);
        lblFeature3Title.TabIndex = 4;
        lblFeature3Title.Text = "⚡  Rápido";
        // 
        // lblFeature2Sub
        // 
        lblFeature2Sub.Font = new Font("Segoe UI", 7.5F);
        lblFeature2Sub.ForeColor = Color.FromArgb(148, 163, 184);
        lblFeature2Sub.Location = new Point(200, 24);
        lblFeature2Sub.Name = "lblFeature2Sub";
        lblFeature2Sub.Size = new Size(170, 45);
        lblFeature2Sub.TabIndex = 3;
        lblFeature2Sub.Text = "Navega sin límites ni restricciones";
        // 
        // lblFeature2Title
        // 
        lblFeature2Title.AutoSize = true;
        lblFeature2Title.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblFeature2Title.ForeColor = Color.FromArgb(248, 250, 252);
        lblFeature2Title.Location = new Point(200, 5);
        lblFeature2Title.Name = "lblFeature2Title";
        lblFeature2Title.Size = new Size(74, 20);
        lblFeature2Title.TabIndex = 2;
        lblFeature2Title.Text = "🌐  Libre";
        // 
        // lblFeature1Sub
        // 
        lblFeature1Sub.Font = new Font("Segoe UI", 7.5F);
        lblFeature1Sub.ForeColor = Color.FromArgb(148, 163, 184);
        lblFeature1Sub.Location = new Point(0, 24);
        lblFeature1Sub.Name = "lblFeature1Sub";
        lblFeature1Sub.Size = new Size(170, 45);
        lblFeature1Sub.TabIndex = 1;
        lblFeature1Sub.Text = "Túnel cifrado y conexión protegida";
        // 
        // lblFeature1Title
        // 
        lblFeature1Title.AutoSize = true;
        lblFeature1Title.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblFeature1Title.ForeColor = Color.FromArgb(248, 250, 252);
        lblFeature1Title.Location = new Point(0, 5);
        lblFeature1Title.Name = "lblFeature1Title";
        lblFeature1Title.Size = new Size(88, 20);
        lblFeature1Title.TabIndex = 0;
        lblFeature1Title.Text = "🔒  Seguro";
        // 
        // panelQuickInfoCard
        // 
        panelQuickInfoCard.BackColor = Color.FromArgb(22, 32, 48);
        panelQuickInfoCard.Controls.Add(lblInfoIpVal);
        panelQuickInfoCard.Controls.Add(lblInfoIpLabel);
        panelQuickInfoCard.Controls.Add(lblInfoUptimeVal);
        panelQuickInfoCard.Controls.Add(lblInfoUptimeLabel);
        panelQuickInfoCard.Controls.Add(lblInfoTunnelVal);
        panelQuickInfoCard.Controls.Add(lblInfoTunnelLabel);
        panelQuickInfoCard.Controls.Add(lblInfoProxyVal);
        panelQuickInfoCard.Controls.Add(lblInfoProxyLabel);
        panelQuickInfoCard.Controls.Add(lblInfoCipherVal);
        panelQuickInfoCard.Controls.Add(lblInfoCipherLabel);
        panelQuickInfoCard.Controls.Add(lblInfoProtoVal);
        panelQuickInfoCard.Controls.Add(lblInfoProtoLabel);
        panelQuickInfoCard.Controls.Add(lblQuickInfoTitle);
        panelQuickInfoCard.Location = new Point(580, 5);
        panelQuickInfoCard.Name = "panelQuickInfoCard";
        panelQuickInfoCard.Size = new Size(255, 395);
        panelQuickInfoCard.TabIndex = 1;
        // 
        // lblInfoIpVal
        // 
        lblInfoIpVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoIpVal.ForeColor = Color.FromArgb(226, 232, 240);
        lblInfoIpVal.Location = new Point(110, 280);
        lblInfoIpVal.Name = "lblInfoIpVal";
        lblInfoIpVal.Size = new Size(130, 24);
        lblInfoIpVal.TabIndex = 12;
        lblInfoIpVal.Text = "-";
        lblInfoIpVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoIpLabel
        // 
        lblInfoIpLabel.Font = new Font("Segoe UI", 9F);
        lblInfoIpLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoIpLabel.Location = new Point(14, 280);
        lblInfoIpLabel.Name = "lblInfoIpLabel";
        lblInfoIpLabel.Size = new Size(95, 24);
        lblInfoIpLabel.TabIndex = 11;
        lblInfoIpLabel.Text = "📍 IP pública";
        lblInfoIpLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfoUptimeVal
        // 
        lblInfoUptimeVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoUptimeVal.ForeColor = Color.FromArgb(226, 232, 240);
        lblInfoUptimeVal.Location = new Point(130, 235);
        lblInfoUptimeVal.Name = "lblInfoUptimeVal";
        lblInfoUptimeVal.Size = new Size(110, 24);
        lblInfoUptimeVal.TabIndex = 10;
        lblInfoUptimeVal.Text = "00:00:00";
        lblInfoUptimeVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoUptimeLabel
        // 
        lblInfoUptimeLabel.Font = new Font("Segoe UI", 9F);
        lblInfoUptimeLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoUptimeLabel.Location = new Point(14, 235);
        lblInfoUptimeLabel.Name = "lblInfoUptimeLabel";
        lblInfoUptimeLabel.Size = new Size(115, 24);
        lblInfoUptimeLabel.TabIndex = 9;
        lblInfoUptimeLabel.Text = "⏱️ Tiempo activo";
        lblInfoUptimeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfoTunnelVal
        // 
        lblInfoTunnelVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoTunnelVal.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoTunnelVal.Location = new Point(110, 190);
        lblInfoTunnelVal.Name = "lblInfoTunnelVal";
        lblInfoTunnelVal.Size = new Size(130, 24);
        lblInfoTunnelVal.TabIndex = 8;
        lblInfoTunnelVal.Text = "Inactivo";
        lblInfoTunnelVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoTunnelLabel
        // 
        lblInfoTunnelLabel.Font = new Font("Segoe UI", 9F);
        lblInfoTunnelLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoTunnelLabel.Location = new Point(14, 190);
        lblInfoTunnelLabel.Name = "lblInfoTunnelLabel";
        lblInfoTunnelLabel.Size = new Size(95, 24);
        lblInfoTunnelLabel.TabIndex = 7;
        lblInfoTunnelLabel.Text = "🚪 Túnel";
        lblInfoTunnelLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfoProxyVal
        // 
        lblInfoProxyVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoProxyVal.ForeColor = Color.FromArgb(226, 232, 240);
        lblInfoProxyVal.Location = new Point(110, 145);
        lblInfoProxyVal.Name = "lblInfoProxyVal";
        lblInfoProxyVal.Size = new Size(130, 24);
        lblInfoProxyVal.TabIndex = 6;
        lblInfoProxyVal.Text = "SOCKS5";
        lblInfoProxyVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoProxyLabel
        // 
        lblInfoProxyLabel.Font = new Font("Segoe UI", 9F);
        lblInfoProxyLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoProxyLabel.Location = new Point(14, 145);
        lblInfoProxyLabel.Name = "lblInfoProxyLabel";
        lblInfoProxyLabel.Size = new Size(95, 24);
        lblInfoProxyLabel.TabIndex = 5;
        lblInfoProxyLabel.Text = "🌐 Proxy";
        lblInfoProxyLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfoCipherVal
        // 
        lblInfoCipherVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoCipherVal.ForeColor = Color.FromArgb(226, 232, 240);
        lblInfoCipherVal.Location = new Point(110, 100);
        lblInfoCipherVal.Name = "lblInfoCipherVal";
        lblInfoCipherVal.Size = new Size(130, 24);
        lblInfoCipherVal.TabIndex = 4;
        lblInfoCipherVal.Text = "AES-256";
        lblInfoCipherVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoCipherLabel
        // 
        lblInfoCipherLabel.Font = new Font("Segoe UI", 9F);
        lblInfoCipherLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoCipherLabel.Location = new Point(14, 100);
        lblInfoCipherLabel.Name = "lblInfoCipherLabel";
        lblInfoCipherLabel.Size = new Size(95, 24);
        lblInfoCipherLabel.TabIndex = 3;
        lblInfoCipherLabel.Text = "🔑 Cifrado";
        lblInfoCipherLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblInfoProtoVal
        // 
        lblInfoProtoVal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblInfoProtoVal.ForeColor = Color.FromArgb(226, 232, 240);
        lblInfoProtoVal.Location = new Point(110, 55);
        lblInfoProtoVal.Name = "lblInfoProtoVal";
        lblInfoProtoVal.Size = new Size(130, 24);
        lblInfoProtoVal.TabIndex = 2;
        lblInfoProtoVal.Text = "SSH + SOCKS5";
        lblInfoProtoVal.TextAlign = ContentAlignment.MiddleRight;
        // 
        // lblInfoProtoLabel
        // 
        lblInfoProtoLabel.Font = new Font("Segoe UI", 9F);
        lblInfoProtoLabel.ForeColor = Color.FromArgb(148, 163, 184);
        lblInfoProtoLabel.Location = new Point(14, 55);
        lblInfoProtoLabel.Name = "lblInfoProtoLabel";
        lblInfoProtoLabel.Size = new Size(95, 24);
        lblInfoProtoLabel.TabIndex = 1;
        lblInfoProtoLabel.Text = "🛡️ Protocolo";
        lblInfoProtoLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblQuickInfoTitle
        // 
        lblQuickInfoTitle.AutoSize = true;
        lblQuickInfoTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblQuickInfoTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblQuickInfoTitle.Location = new Point(14, 14);
        lblQuickInfoTitle.Name = "lblQuickInfoTitle";
        lblQuickInfoTitle.Size = new Size(184, 25);
        lblQuickInfoTitle.TabIndex = 0;
        lblQuickInfoTitle.Text = "Información rápida";
        // 
        // panelMainConnectionCard
        // 
        panelMainConnectionCard.BackColor = Color.FromArgb(22, 32, 48);
        panelMainConnectionCard.Controls.Add(btnGoToConfig);
        panelMainConnectionCard.Controls.Add(lblQuickConfigSummary);
        panelMainConnectionCard.Controls.Add(lblRingStatusSub);
        panelMainConnectionCard.Controls.Add(lblRingStatusText);
        panelMainConnectionCard.Controls.Add(lblStatusRingTitle);
        panelMainConnectionCard.Location = new Point(20, 5);
        panelMainConnectionCard.Name = "panelMainConnectionCard";
        panelMainConnectionCard.Size = new Size(540, 390);
        panelMainConnectionCard.TabIndex = 0;
        // 
        // btnGoToConfig
        // 
        btnGoToConfig.BackColor = Color.FromArgb(30, 41, 59);
        btnGoToConfig.Cursor = Cursors.Hand;
        btnGoToConfig.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        btnGoToConfig.FlatStyle = FlatStyle.Flat;
        btnGoToConfig.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        btnGoToConfig.ForeColor = Color.FromArgb(96, 165, 250);
        btnGoToConfig.Location = new Point(390, 338);
        btnGoToConfig.Name = "btnGoToConfig";
        btnGoToConfig.Size = new Size(130, 30);
        btnGoToConfig.TabIndex = 8;
        btnGoToConfig.Text = "⚙️ Configurar SSH";
        btnGoToConfig.UseVisualStyleBackColor = false;
        btnGoToConfig.Click += btnNavConfigSsh_Click;
        // 
        // lblQuickConfigSummary
        // 
        lblQuickConfigSummary.Font = new Font("Segoe UI", 8.5F);
        lblQuickConfigSummary.ForeColor = Color.FromArgb(148, 163, 184);
        lblQuickConfigSummary.Location = new Point(20, 342);
        lblQuickConfigSummary.Name = "lblQuickConfigSummary";
        lblQuickConfigSummary.Size = new Size(360, 24);
        lblQuickConfigSummary.TabIndex = 7;
        lblQuickConfigSummary.Text = "Servidor actual: fr1.sshweb.site:22 (sshocean-mrmao233)";
        lblQuickConfigSummary.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblRingStatusSub
        // 
        lblRingStatusSub.Font = new Font("Segoe UI", 8.5F);
        lblRingStatusSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblRingStatusSub.Location = new Point(20, 242);
        lblRingStatusSub.Name = "lblRingStatusSub";
        lblRingStatusSub.Size = new Size(500, 20);
        lblRingStatusSub.TabIndex = 4;
        lblRingStatusSub.Text = "Haz clic en el candado o en el botón para conectar";
        lblRingStatusSub.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblRingStatusText
        // 
        lblRingStatusText.BackColor = Color.Transparent;
        lblRingStatusText.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblRingStatusText.ForeColor = Color.FromArgb(234, 88, 12);
        lblRingStatusText.Location = new Point(160, 215);
        lblRingStatusText.Name = "lblRingStatusText";
        lblRingStatusText.Size = new Size(220, 24);
        lblRingStatusText.TabIndex = 3;
        lblRingStatusText.Text = "DESCONECTADO";
        lblRingStatusText.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblStatusRingTitle
        // 
        lblStatusRingTitle.AutoSize = true;
        lblStatusRingTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblStatusRingTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblStatusRingTitle.Location = new Point(16, 14);
        lblStatusRingTitle.Name = "lblStatusRingTitle";
        lblStatusRingTitle.Size = new Size(186, 25);
        lblStatusRingTitle.TabIndex = 0;
        lblStatusRingTitle.Text = "Estado de conexión";
        // 
        // panelTunnelType
        // 
        panelTunnelType.BackColor = Color.FromArgb(11, 15, 25);
        panelTunnelType.Controls.Add(panelTunnelButtons);
        panelTunnelType.Controls.Add(groupTunnelOptions);
        panelTunnelType.Controls.Add(groupConnectFrom);
        panelTunnelType.Controls.Add(groupTunnelProtocol);
        panelTunnelType.Controls.Add(lblTunnelSub);
        panelTunnelType.Controls.Add(lblTunnelHeader);
        panelTunnelType.Dock = DockStyle.Fill;
        panelTunnelType.Location = new Point(0, 0);
        panelTunnelType.Name = "panelTunnelType";
        panelTunnelType.Padding = new Padding(24, 20, 24, 20);
        panelTunnelType.Size = new Size(855, 700);
        panelTunnelType.TabIndex = 2;
        panelTunnelType.Visible = false;
        // 
        // lblTunnelHeader
        // 
        lblTunnelHeader.AutoSize = true;
        lblTunnelHeader.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblTunnelHeader.ForeColor = Color.FromArgb(248, 250, 252);
        lblTunnelHeader.Location = new Point(24, 18);
        lblTunnelHeader.Name = "lblTunnelHeader";
        lblTunnelHeader.Size = new Size(330, 30);
        lblTunnelHeader.TabIndex = 0;
        lblTunnelHeader.Text = "🛠️  Tipo de Túnel y Conexión";
        // 
        // lblTunnelSub
        // 
        lblTunnelSub.AutoSize = true;
        lblTunnelSub.Font = new Font("Segoe UI", 8.5F);
        lblTunnelSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblTunnelSub.Location = new Point(26, 50);
        lblTunnelSub.Name = "lblTunnelSub";
        lblTunnelSub.Size = new Size(540, 20);
        lblTunnelSub.TabIndex = 1;
        lblTunnelSub.Text = "Configura el protocolo de encapsulación, origen de conexión y opciones avanzadas.";
        // 
        // groupTunnelProtocol
        // 
        groupTunnelProtocol.BackColor = Color.FromArgb(22, 32, 48);
        groupTunnelProtocol.Controls.Add(lblTunnelV2rayDesc);
        groupTunnelProtocol.Controls.Add(rbTunnelV2ray);
        groupTunnelProtocol.Controls.Add(lblTunnelSshDesc);
        groupTunnelProtocol.Controls.Add(rbTunnelSsh);
        groupTunnelProtocol.Controls.Add(lblTunnelProtocolTitle);
        groupTunnelProtocol.Location = new Point(24, 82);
        groupTunnelProtocol.Name = "groupTunnelProtocol";
        groupTunnelProtocol.Size = new Size(805, 110);
        groupTunnelProtocol.TabIndex = 2;
        // 
        // lblTunnelProtocolTitle
        // 
        lblTunnelProtocolTitle.AutoSize = true;
        lblTunnelProtocolTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblTunnelProtocolTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblTunnelProtocolTitle.Location = new Point(16, 12);
        lblTunnelProtocolTitle.Name = "lblTunnelProtocolTitle";
        lblTunnelProtocolTitle.Size = new Size(118, 20);
        lblTunnelProtocolTitle.TabIndex = 0;
        lblTunnelProtocolTitle.Text = "TIPO DE TÚNEL";
        // 
        // rbTunnelSsh
        // 
        rbTunnelSsh.AutoSize = true;
        rbTunnelSsh.Checked = true;
        rbTunnelSsh.Cursor = Cursors.Hand;
        rbTunnelSsh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbTunnelSsh.ForeColor = Color.FromArgb(248, 250, 252);
        rbTunnelSsh.Location = new Point(18, 38);
        rbTunnelSsh.Name = "rbTunnelSsh";
        rbTunnelSsh.Size = new Size(174, 25);
        rbTunnelSsh.TabIndex = 1;
        rbTunnelSsh.TabStop = true;
        rbTunnelSsh.Text = "🔒 Secure Shell (SSH)";
        rbTunnelSsh.UseVisualStyleBackColor = true;
        // 
        // lblTunnelSshDesc
        // 
        lblTunnelSshDesc.AutoSize = true;
        lblTunnelSshDesc.Font = new Font("Segoe UI", 8F);
        lblTunnelSshDesc.ForeColor = Color.FromArgb(148, 163, 184);
        lblTunnelSshDesc.Location = new Point(40, 65);
        lblTunnelSshDesc.Name = "lblTunnelSshDesc";
        lblTunnelSshDesc.Size = new Size(330, 19);
        lblTunnelSshDesc.TabIndex = 2;
        lblTunnelSshDesc.Text = "Túnel cifrado punto a punto mediante servidor SSH";
        // 
        // rbTunnelV2ray
        // 
        rbTunnelV2ray.AutoSize = true;
        rbTunnelV2ray.Enabled = false;
        rbTunnelV2ray.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbTunnelV2ray.ForeColor = Color.FromArgb(100, 116, 139);
        rbTunnelV2ray.Location = new Point(420, 38);
        rbTunnelV2ray.Name = "rbTunnelV2ray";
        rbTunnelV2ray.Size = new Size(198, 25);
        rbTunnelV2ray.TabIndex = 3;
        rbTunnelV2ray.Text = "⚡ V2Ray (Próximamente)";
        rbTunnelV2ray.UseVisualStyleBackColor = true;
        // 
        // lblTunnelV2rayDesc
        // 
        lblTunnelV2rayDesc.AutoSize = true;
        lblTunnelV2rayDesc.Font = new Font("Segoe UI", 8F);
        lblTunnelV2rayDesc.ForeColor = Color.FromArgb(71, 85, 105);
        lblTunnelV2rayDesc.Location = new Point(442, 65);
        lblTunnelV2rayDesc.Name = "lblTunnelV2rayDesc";
        lblTunnelV2rayDesc.Size = new Size(310, 19);
        lblTunnelV2rayDesc.TabIndex = 4;
        lblTunnelV2rayDesc.Text = "Soporte para VMess, VLESS y Trojan (En desarrollo)";
        // 
        // groupConnectFrom
        // 
        groupConnectFrom.BackColor = Color.FromArgb(22, 32, 48);
        groupConnectFrom.Controls.Add(lblTunnelTlsDesc);
        groupConnectFrom.Controls.Add(rbTunnelTls);
        groupConnectFrom.Controls.Add(lblTunnelDirectDesc);
        groupConnectFrom.Controls.Add(rbTunnelDirect);
        groupConnectFrom.Controls.Add(lblConnectFromTitle);
        groupConnectFrom.Location = new Point(24, 206);
        groupConnectFrom.Name = "groupConnectFrom";
        groupConnectFrom.Size = new Size(805, 110);
        groupConnectFrom.TabIndex = 3;
        // 
        // lblConnectFromTitle
        // 
        lblConnectFromTitle.AutoSize = true;
        lblConnectFromTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblConnectFromTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblConnectFromTitle.Location = new Point(16, 12);
        lblConnectFromTitle.Name = "lblConnectFromTitle";
        lblConnectFromTitle.Size = new Size(140, 20);
        lblConnectFromTitle.TabIndex = 0;
        lblConnectFromTitle.Text = "CONECTAR DESDE";
        // 
        // rbTunnelDirect
        // 
        rbTunnelDirect.AutoSize = true;
        rbTunnelDirect.Checked = true;
        rbTunnelDirect.Cursor = Cursors.Hand;
        rbTunnelDirect.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbTunnelDirect.ForeColor = Color.FromArgb(248, 250, 252);
        rbTunnelDirect.Location = new Point(18, 38);
        rbTunnelDirect.Name = "rbTunnelDirect";
        rbTunnelDirect.Size = new Size(153, 25);
        rbTunnelDirect.TabIndex = 1;
        rbTunnelDirect.TabStop = true;
        rbTunnelDirect.Text = "🌐 None (Direct)";
        rbTunnelDirect.UseVisualStyleBackColor = true;
        // 
        // lblTunnelDirectDesc
        // 
        lblTunnelDirectDesc.AutoSize = true;
        lblTunnelDirectDesc.Font = new Font("Segoe UI", 8F);
        lblTunnelDirectDesc.ForeColor = Color.FromArgb(148, 163, 184);
        lblTunnelDirectDesc.Location = new Point(40, 65);
        lblTunnelDirectDesc.Name = "lblTunnelDirectDesc";
        lblTunnelDirectDesc.Size = new Size(330, 19);
        lblTunnelDirectDesc.TabIndex = 2;
        lblTunnelDirectDesc.Text = "Conexión TCP directa estándar al servidor SSH";
        // 
        // rbTunnelTls
        // 
        rbTunnelTls.AutoSize = true;
        rbTunnelTls.Cursor = Cursors.Hand;
        rbTunnelTls.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        rbTunnelTls.ForeColor = Color.FromArgb(248, 250, 252);
        rbTunnelTls.Location = new Point(420, 38);
        rbTunnelTls.Name = "rbTunnelTls";
        rbTunnelTls.Size = new Size(116, 25);
        rbTunnelTls.TabIndex = 3;
        rbTunnelTls.Text = "🛡️ TLS / SSL";
        rbTunnelTls.UseVisualStyleBackColor = true;
        // 
        // lblTunnelTlsDesc
        // 
        lblTunnelTlsDesc.AutoSize = true;
        lblTunnelTlsDesc.Font = new Font("Segoe UI", 8F);
        lblTunnelTlsDesc.ForeColor = Color.FromArgb(148, 163, 184);
        lblTunnelTlsDesc.Location = new Point(442, 65);
        lblTunnelTlsDesc.Name = "lblTunnelTlsDesc";
        lblTunnelTlsDesc.Size = new Size(330, 19);
        lblTunnelTlsDesc.TabIndex = 4;
        lblTunnelTlsDesc.Text = "Túnel encapsulado en capa SSL con SNI (Puerto 443)";
        // 
        // groupTunnelOptions
        // 
        groupTunnelOptions.BackColor = Color.FromArgb(22, 32, 48);
        groupTunnelOptions.Controls.Add(lblCustomPayloadDesc);
        groupTunnelOptions.Controls.Add(chkCustomPayload);
        groupTunnelOptions.Controls.Add(lblTunnelOptionsTitle);
        groupTunnelOptions.Location = new Point(24, 330);
        groupTunnelOptions.Name = "groupTunnelOptions";
        groupTunnelOptions.Size = new Size(805, 95);
        groupTunnelOptions.TabIndex = 4;
        // 
        // lblTunnelOptionsTitle
        // 
        lblTunnelOptionsTitle.AutoSize = true;
        lblTunnelOptionsTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblTunnelOptionsTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblTunnelOptionsTitle.Location = new Point(16, 12);
        lblTunnelOptionsTitle.Name = "lblTunnelOptionsTitle";
        lblTunnelOptionsTitle.Size = new Size(84, 20);
        lblTunnelOptionsTitle.TabIndex = 0;
        lblTunnelOptionsTitle.Text = "OPCIONES";
        // 
        // chkCustomPayload
        // 
        chkCustomPayload.AutoSize = true;
        chkCustomPayload.Cursor = Cursors.Hand;
        chkCustomPayload.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        chkCustomPayload.ForeColor = Color.FromArgb(248, 250, 252);
        chkCustomPayload.Location = new Point(18, 38);
        chkCustomPayload.Name = "chkCustomPayload";
        chkCustomPayload.Size = new Size(168, 25);
        chkCustomPayload.TabIndex = 1;
        chkCustomPayload.Text = "📦 Custom Payload";
        chkCustomPayload.UseVisualStyleBackColor = true;
        // 
        // lblCustomPayloadDesc
        // 
        lblCustomPayloadDesc.AutoSize = true;
        lblCustomPayloadDesc.Font = new Font("Segoe UI", 8F);
        lblCustomPayloadDesc.ForeColor = Color.FromArgb(148, 163, 184);
        lblCustomPayloadDesc.Location = new Point(40, 65);
        lblCustomPayloadDesc.Name = "lblCustomPayloadDesc";
        lblCustomPayloadDesc.Size = new Size(390, 19);
        lblCustomPayloadDesc.TabIndex = 2;
        lblCustomPayloadDesc.Text = "Permite personalizar payload HTTP para bypass de red móvil";
        // 
        // panelTunnelButtons
        // 
        panelTunnelButtons.Controls.Add(btnSaveTunnel);
        panelTunnelButtons.Controls.Add(btnCancelTunnel);
        panelTunnelButtons.Location = new Point(24, 440);
        panelTunnelButtons.Name = "panelTunnelButtons";
        panelTunnelButtons.Size = new Size(805, 52);
        panelTunnelButtons.TabIndex = 5;
        // 
        // btnCancelTunnel
        // 
        btnCancelTunnel.BackColor = Color.FromArgb(30, 41, 59);
        btnCancelTunnel.Cursor = Cursors.Hand;
        btnCancelTunnel.FlatAppearance.BorderSize = 0;
        btnCancelTunnel.FlatStyle = FlatStyle.Flat;
        btnCancelTunnel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnCancelTunnel.ForeColor = Color.FromArgb(226, 232, 240);
        btnCancelTunnel.Location = new Point(420, 5);
        btnCancelTunnel.Name = "btnCancelTunnel";
        btnCancelTunnel.Size = new Size(165, 42);
        btnCancelTunnel.TabIndex = 0;
        btnCancelTunnel.Text = "◀  Volver a Inicio";
        btnCancelTunnel.UseVisualStyleBackColor = false;
        btnCancelTunnel.Click += btnCancelTunnel_Click;
        // 
        // btnSaveTunnel
        // 
        btnSaveTunnel.BackColor = Color.FromArgb(234, 88, 12);
        btnSaveTunnel.Cursor = Cursors.Hand;
        btnSaveTunnel.FlatAppearance.BorderSize = 0;
        btnSaveTunnel.FlatStyle = FlatStyle.Flat;
        btnSaveTunnel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSaveTunnel.ForeColor = Color.White;
        btnSaveTunnel.Location = new Point(600, 5);
        btnSaveTunnel.Name = "btnSaveTunnel";
        btnSaveTunnel.Size = new Size(205, 42);
        btnSaveTunnel.TabIndex = 1;
        btnSaveTunnel.Text = "💾  Guardar";
        btnSaveTunnel.UseVisualStyleBackColor = false;
        btnSaveTunnel.Click += btnSaveTunnel_Click;
        // 
        // panelSniConfig
        // 
        panelSniConfig.BackColor = Color.FromArgb(11, 15, 25);
        panelSniConfig.Controls.Add(panelSniButtons);
        panelSniConfig.Controls.Add(groupSniVersion);
        panelSniConfig.Controls.Add(groupSniHost);
        panelSniConfig.Controls.Add(lblSniConfigSub);
        panelSniConfig.Controls.Add(lblSniConfigHeader);
        panelSniConfig.Dock = DockStyle.Fill;
        panelSniConfig.Location = new Point(0, 0);
        panelSniConfig.Name = "panelSniConfig";
        panelSniConfig.Padding = new Padding(24, 20, 24, 20);
        panelSniConfig.Size = new Size(855, 700);
        panelSniConfig.TabIndex = 3;
        panelSniConfig.Visible = false;
        // 
        // lblSniConfigHeader
        // 
        lblSniConfigHeader.AutoSize = true;
        lblSniConfigHeader.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblSniConfigHeader.ForeColor = Color.FromArgb(248, 250, 252);
        lblSniConfigHeader.Location = new Point(24, 18);
        lblSniConfigHeader.Name = "lblSniConfigHeader";
        lblSniConfigHeader.Size = new Size(310, 30);
        lblSniConfigHeader.TabIndex = 0;
        lblSniConfigHeader.Text = "🔒  Configuración SNI / SSL";
        // 
        // lblSniConfigSub
        // 
        lblSniConfigSub.AutoSize = true;
        lblSniConfigSub.Font = new Font("Segoe UI", 8.5F);
        lblSniConfigSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblSniConfigSub.Location = new Point(26, 50);
        lblSniConfigSub.Name = "lblSniConfigSub";
        lblSniConfigSub.Size = new Size(540, 20);
        lblSniConfigSub.TabIndex = 1;
        lblSniConfigSub.Text = "Personaliza el nombre de host o dominio SNI y la versión de protocolo TLS.";
        // 
        // groupSniHost
        // 
        groupSniHost.BackColor = Color.FromArgb(22, 32, 48);
        groupSniHost.Controls.Add(txtSniHostInput);
        groupSniHost.Controls.Add(lblSniHostSub);
        groupSniHost.Controls.Add(lblSniHostTitle);
        groupSniHost.Location = new Point(24, 82);
        groupSniHost.Name = "groupSniHost";
        groupSniHost.Size = new Size(805, 105);
        groupSniHost.TabIndex = 2;
        // 
        // lblSniHostTitle
        // 
        lblSniHostTitle.AutoSize = true;
        lblSniHostTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSniHostTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblSniHostTitle.Location = new Point(16, 12);
        lblSniHostTitle.Name = "lblSniHostTitle";
        lblSniHostTitle.Size = new Size(270, 20);
        lblSniHostTitle.TabIndex = 0;
        lblSniHostTitle.Text = "NOMBRE DE HOST O DOMINIO (SNI)";
        // 
        // lblSniHostSub
        // 
        lblSniHostSub.AutoSize = true;
        lblSniHostSub.Font = new Font("Segoe UI", 8F);
        lblSniHostSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblSniHostSub.Location = new Point(16, 34);
        lblSniHostSub.Name = "lblSniHostSub";
        lblSniHostSub.Size = new Size(505, 19);
        lblSniHostSub.TabIndex = 1;
        lblSniHostSub.Text = "Dominio de destino utilizado para la negociación de certificado TLS (Server Name Indication)";
        // 
        // txtSniHostInput
        // 
        txtSniHostInput.BackColor = Color.FromArgb(30, 41, 59);
        txtSniHostInput.BorderStyle = BorderStyle.FixedSingle;
        txtSniHostInput.Font = new Font("Segoe UI", 10F);
        txtSniHostInput.ForeColor = Color.White;
        txtSniHostInput.Location = new Point(16, 58);
        txtSniHostInput.Name = "txtSniHostInput";
        txtSniHostInput.PlaceholderText = "ej: m.facebook.com";
        txtSniHostInput.Size = new Size(770, 30);
        txtSniHostInput.TabIndex = 2;
        // 
        // groupSniVersion
        // 
        groupSniVersion.BackColor = Color.FromArgb(22, 32, 48);
        groupSniVersion.Controls.Add(cmbSniVersionInput);
        groupSniVersion.Controls.Add(lblSniVersionSub);
        groupSniVersion.Controls.Add(lblSniVersionTitle);
        groupSniVersion.Location = new Point(24, 200);
        groupSniVersion.Name = "groupSniVersion";
        groupSniVersion.Size = new Size(805, 105);
        groupSniVersion.TabIndex = 3;
        // 
        // lblSniVersionTitle
        // 
        lblSniVersionTitle.AutoSize = true;
        lblSniVersionTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSniVersionTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblSniVersionTitle.Location = new Point(16, 12);
        lblSniVersionTitle.Name = "lblSniVersionTitle";
        lblSniVersionTitle.Size = new Size(106, 20);
        lblSniVersionTitle.TabIndex = 0;
        lblSniVersionTitle.Text = "VERSIÓN TLS";
        // 
        // lblSniVersionSub
        // 
        lblSniVersionSub.AutoSize = true;
        lblSniVersionSub.Font = new Font("Segoe UI", 8F);
        lblSniVersionSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblSniVersionSub.Location = new Point(16, 34);
        lblSniVersionSub.Name = "lblSniVersionSub";
        lblSniVersionSub.Size = new Size(520, 19);
        lblSniVersionSub.TabIndex = 1;
        lblSniVersionSub.Text = "Selecciona la versión del protocolo de seguridad SSL/TLS requerida para el túnel";
        // 
        // cmbSniVersionInput
        // 
        cmbSniVersionInput.BackColor = Color.FromArgb(30, 41, 59);
        cmbSniVersionInput.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbSniVersionInput.FlatStyle = FlatStyle.Flat;
        cmbSniVersionInput.Font = new Font("Segoe UI", 9.5F);
        cmbSniVersionInput.ForeColor = Color.White;
        cmbSniVersionInput.FormattingEnabled = true;
        cmbSniVersionInput.Items.AddRange(new object[] { "Default", "TLSv1", "TLSv1.1", "TLSv1.2", "TLSv1.3" });
        cmbSniVersionInput.Location = new Point(16, 58);
        cmbSniVersionInput.Name = "cmbSniVersionInput";
        cmbSniVersionInput.Size = new Size(360, 29);
        cmbSniVersionInput.TabIndex = 2;
        // 
        // panelSniButtons
        // 
        panelSniButtons.Controls.Add(btnSaveSni);
        panelSniButtons.Controls.Add(btnCancelSni);
        panelSniButtons.Location = new Point(24, 320);
        panelSniButtons.Name = "panelSniButtons";
        panelSniButtons.Size = new Size(805, 52);
        panelSniButtons.TabIndex = 4;
        // 
        // btnCancelSni
        // 
        btnCancelSni.BackColor = Color.FromArgb(30, 41, 59);
        btnCancelSni.Cursor = Cursors.Hand;
        btnCancelSni.FlatAppearance.BorderSize = 0;
        btnCancelSni.FlatStyle = FlatStyle.Flat;
        btnCancelSni.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnCancelSni.ForeColor = Color.FromArgb(226, 232, 240);
        btnCancelSni.Location = new Point(420, 5);
        btnCancelSni.Name = "btnCancelSni";
        btnCancelSni.Size = new Size(165, 42);
        btnCancelSni.TabIndex = 0;
        btnCancelSni.Text = "◀  Volver a Inicio";
        btnCancelSni.UseVisualStyleBackColor = false;
        btnCancelSni.Click += btnCancelSni_Click;
        // 
        // btnSaveSni
        // 
        btnSaveSni.BackColor = Color.FromArgb(234, 88, 12);
        btnSaveSni.Cursor = Cursors.Hand;
        btnSaveSni.FlatAppearance.BorderSize = 0;
        btnSaveSni.FlatStyle = FlatStyle.Flat;
        btnSaveSni.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSaveSni.ForeColor = Color.White;
        btnSaveSni.Location = new Point(600, 5);
        btnSaveSni.Name = "btnSaveSni";
        btnSaveSni.Size = new Size(205, 42);
        btnSaveSni.TabIndex = 1;
        btnSaveSni.Text = "💾  Guardar";
        btnSaveSni.UseVisualStyleBackColor = false;
        btnSaveSni.Click += btnSaveSni_Click;
        // 
        // panelConfigSsh
        // 
        panelConfigSsh.BackColor = Color.FromArgb(11, 15, 25);
        panelConfigSsh.Controls.Add(panelConfigSshButtons);
        panelConfigSsh.Controls.Add(groupAdvancedSsh);
        panelConfigSsh.Controls.Add(groupSocks);
        panelConfigSsh.Controls.Add(groupSsh);
        panelConfigSsh.Controls.Add(lblConfigSshSub);
        panelConfigSsh.Controls.Add(lblConfigSshHeader);
        panelConfigSsh.Dock = DockStyle.Fill;
        panelConfigSsh.Location = new Point(0, 0);
        panelConfigSsh.Name = "panelConfigSsh";
        panelConfigSsh.Padding = new Padding(24, 20, 24, 20);
        panelConfigSsh.Size = new Size(855, 700);
        panelConfigSsh.TabIndex = 2;
        panelConfigSsh.Visible = false;
        // 
        // lblConfigSshHeader
        // 
        lblConfigSshHeader.AutoSize = true;
        lblConfigSshHeader.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
        lblConfigSshHeader.ForeColor = Color.FromArgb(248, 250, 252);
        lblConfigSshHeader.Location = new Point(24, 18);
        lblConfigSshHeader.Name = "lblConfigSshHeader";
        lblConfigSshHeader.Size = new Size(330, 30);
        lblConfigSshHeader.TabIndex = 0;
        lblConfigSshHeader.Text = "🔑  Configuración de Cuenta SSH";
        // 
        // lblConfigSshSub
        // 
        lblConfigSshSub.AutoSize = true;
        lblConfigSshSub.Font = new Font("Segoe UI", 8.5F);
        lblConfigSshSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblConfigSshSub.Location = new Point(26, 50);
        lblConfigSshSub.Name = "lblConfigSshSub";
        lblConfigSshSub.Size = new Size(560, 20);
        lblConfigSshSub.TabIndex = 1;
        lblConfigSshSub.Text = "Ingresa los datos de tu servidor VPS / SSH y configura los parámetros de escucha local.";
        // 
        // groupSsh
        // 
        groupSsh.BackColor = Color.FromArgb(22, 32, 48);
        groupSsh.Controls.Add(btnTogglePass);
        groupSsh.Controls.Add(txtPassword);
        groupSsh.Controls.Add(lblPassword);
        groupSsh.Controls.Add(txtUsername);
        groupSsh.Controls.Add(lblUsername);
        groupSsh.Controls.Add(numTlsPort);
        groupSsh.Controls.Add(lblTlsPort);
        groupSsh.Controls.Add(txtSshHost);
        groupSsh.Controls.Add(lblSshHost);
        groupSsh.Controls.Add(lblSshGroupTitle);
        groupSsh.Location = new Point(24, 80);
        groupSsh.Name = "groupSsh";
        groupSsh.Size = new Size(805, 175);
        groupSsh.TabIndex = 2;
        // 
        // lblSshGroupTitle
        // 
        lblSshGroupTitle.AutoSize = true;
        lblSshGroupTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSshGroupTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblSshGroupTitle.Location = new Point(16, 12);
        lblSshGroupTitle.Name = "lblSshGroupTitle";
        lblSshGroupTitle.Size = new Size(245, 20);
        lblSshGroupTitle.TabIndex = 0;
        lblSshGroupTitle.Text = "DATOS DE LA VPS O CUENTA SSH";
        // 
        // lblSshHost
        // 
        lblSshHost.AutoSize = true;
        lblSshHost.Font = new Font("Segoe UI", 8.5F);
        lblSshHost.ForeColor = Color.FromArgb(148, 163, 184);
        lblSshHost.Location = new Point(16, 36);
        lblSshHost.Name = "lblSshHost";
        lblSshHost.Size = new Size(69, 20);
        lblSshHost.TabIndex = 1;
        lblSshHost.Text = "Host SSH";
        // 
        // txtSshHost
        // 
        txtSshHost.BackColor = Color.FromArgb(30, 41, 59);
        txtSshHost.BorderStyle = BorderStyle.FixedSingle;
        txtSshHost.Font = new Font("Segoe UI", 9.5F);
        txtSshHost.ForeColor = Color.FromArgb(248, 250, 252);
        txtSshHost.Location = new Point(16, 58);
        txtSshHost.Name = "txtSshHost";
        txtSshHost.PlaceholderText = "ej: fr1.sshweb.site o 192.168.1.1";
        txtSshHost.Size = new Size(630, 29);
        txtSshHost.TabIndex = 0;
        // 
        // lblTlsPort
        // 
        lblTlsPort.AutoSize = true;
        lblTlsPort.Font = new Font("Segoe UI", 8.5F);
        lblTlsPort.ForeColor = Color.FromArgb(148, 163, 184);
        lblTlsPort.Location = new Point(660, 36);
        lblTlsPort.Name = "lblTlsPort";
        lblTlsPort.Size = new Size(95, 20);
        lblTlsPort.TabIndex = 3;
        lblTlsPort.Text = "Puerto (443)";
        // 
        // numTlsPort
        // 
        numTlsPort.BackColor = Color.FromArgb(30, 41, 59);
        numTlsPort.BorderStyle = BorderStyle.FixedSingle;
        numTlsPort.Font = new Font("Segoe UI", 9.5F);
        numTlsPort.ForeColor = Color.FromArgb(248, 250, 252);
        numTlsPort.Location = new Point(660, 58);
        numTlsPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numTlsPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numTlsPort.Name = "numTlsPort";
        numTlsPort.Size = new Size(125, 29);
        numTlsPort.TabIndex = 1;
        numTlsPort.Value = new decimal(new int[] { 443, 0, 0, 0 });
        // 
        // lblUsername
        // 
        lblUsername.AutoSize = true;
        lblUsername.Font = new Font("Segoe UI", 8.5F);
        lblUsername.ForeColor = Color.FromArgb(148, 163, 184);
        lblUsername.Location = new Point(16, 98);
        lblUsername.Name = "lblUsername";
        lblUsername.Size = new Size(139, 20);
        lblUsername.TabIndex = 4;
        lblUsername.Text = "Nombre de Usuario";
        // 
        // txtUsername
        // 
        txtUsername.BackColor = Color.FromArgb(30, 41, 59);
        txtUsername.BorderStyle = BorderStyle.FixedSingle;
        txtUsername.Font = new Font("Segoe UI", 9.5F);
        txtUsername.ForeColor = Color.FromArgb(248, 250, 252);
        txtUsername.Location = new Point(16, 120);
        txtUsername.Name = "txtUsername";
        txtUsername.PlaceholderText = "ej: sshocean-usuario";
        txtUsername.Size = new Size(360, 29);
        txtUsername.TabIndex = 2;
        // 
        // lblPassword
        // 
        lblPassword.AutoSize = true;
        lblPassword.Font = new Font("Segoe UI", 8.5F);
        lblPassword.ForeColor = Color.FromArgb(148, 163, 184);
        lblPassword.Location = new Point(396, 98);
        lblPassword.Name = "lblPassword";
        lblPassword.Size = new Size(83, 20);
        lblPassword.TabIndex = 5;
        lblPassword.Text = "Contraseña";
        // 
        // txtPassword
        // 
        txtPassword.BackColor = Color.FromArgb(30, 41, 59);
        txtPassword.BorderStyle = BorderStyle.FixedSingle;
        txtPassword.Font = new Font("Segoe UI", 9.5F);
        txtPassword.ForeColor = Color.FromArgb(248, 250, 252);
        txtPassword.Location = new Point(396, 120);
        txtPassword.Name = "txtPassword";
        txtPassword.Size = new Size(345, 29);
        txtPassword.TabIndex = 3;
        txtPassword.UseSystemPasswordChar = true;
        // 
        // btnTogglePass
        // 
        btnTogglePass.BackColor = Color.FromArgb(30, 41, 59);
        btnTogglePass.Cursor = Cursors.Hand;
        btnTogglePass.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        btnTogglePass.FlatStyle = FlatStyle.Flat;
        btnTogglePass.Font = new Font("Segoe UI Emoji", 10F);
        btnTogglePass.ForeColor = Color.FromArgb(248, 250, 252);
        btnTogglePass.Location = new Point(745, 120);
        btnTogglePass.Name = "btnTogglePass";
        btnTogglePass.Size = new Size(40, 29);
        btnTogglePass.TabIndex = 4;
        btnTogglePass.Text = "👁️";
        btnTogglePass.UseVisualStyleBackColor = false;
        btnTogglePass.Click += btnTogglePass_Click;
        // 
        // groupSocks
        // 
        groupSocks.BackColor = Color.FromArgb(22, 32, 48);
        groupSocks.Controls.Add(chkEnableTun);
        groupSocks.Controls.Add(numSocksPort);
        groupSocks.Controls.Add(lblSocksPort);
        groupSocks.Controls.Add(txtSocksIp);
        groupSocks.Controls.Add(lblSocksIp);
        groupSocks.Controls.Add(lblSocksGroupTitle);
        groupSocks.Location = new Point(24, 268);
        groupSocks.Name = "groupSocks";
        groupSocks.Size = new Size(805, 135);
        groupSocks.TabIndex = 3;
        // 
        // lblSocksGroupTitle
        // 
        lblSocksGroupTitle.AutoSize = true;
        lblSocksGroupTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblSocksGroupTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblSocksGroupTitle.Location = new Point(16, 12);
        lblSocksGroupTitle.Name = "lblSocksGroupTitle";
        lblSocksGroupTitle.Size = new Size(270, 20);
        lblSocksGroupTitle.TabIndex = 0;
        lblSocksGroupTitle.Text = "CONFIGURACIÓN LOCAL (SOCKS5 / VPN)";
        // 
        // lblSocksIp
        // 
        lblSocksIp.AutoSize = true;
        lblSocksIp.Font = new Font("Segoe UI", 8.5F);
        lblSocksIp.ForeColor = Color.FromArgb(148, 163, 184);
        lblSocksIp.Location = new Point(16, 36);
        lblSocksIp.Name = "lblSocksIp";
        lblSocksIp.Size = new Size(98, 20);
        lblSocksIp.TabIndex = 1;
        lblSocksIp.Text = "IP de escucha";
        // 
        // txtSocksIp
        // 
        txtSocksIp.BackColor = Color.FromArgb(30, 41, 59);
        txtSocksIp.BorderStyle = BorderStyle.FixedSingle;
        txtSocksIp.Font = new Font("Segoe UI", 9.5F);
        txtSocksIp.ForeColor = Color.FromArgb(248, 250, 252);
        txtSocksIp.Location = new Point(16, 58);
        txtSocksIp.Name = "txtSocksIp";
        txtSocksIp.Size = new Size(490, 29);
        txtSocksIp.TabIndex = 0;
        txtSocksIp.Text = "127.0.0.1";
        // 
        // lblSocksPort
        // 
        lblSocksPort.AutoSize = true;
        lblSocksPort.Font = new Font("Segoe UI", 8.5F);
        lblSocksPort.ForeColor = Color.FromArgb(148, 163, 184);
        lblSocksPort.Location = new Point(526, 36);
        lblSocksPort.Name = "lblSocksPort";
        lblSocksPort.Size = new Size(91, 20);
        lblSocksPort.TabIndex = 3;
        lblSocksPort.Text = "Puerto Local";
        // 
        // numSocksPort
        // 
        numSocksPort.BackColor = Color.FromArgb(30, 41, 59);
        numSocksPort.BorderStyle = BorderStyle.FixedSingle;
        numSocksPort.Font = new Font("Segoe UI", 9.5F);
        numSocksPort.ForeColor = Color.FromArgb(248, 250, 252);
        numSocksPort.Location = new Point(526, 58);
        numSocksPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numSocksPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numSocksPort.Name = "numSocksPort";
        numSocksPort.Size = new Size(260, 29);
        numSocksPort.TabIndex = 1;
        numSocksPort.Value = new decimal(new int[] { 1080, 0, 0, 0 });
        // 
        // chkEnableTun
        // 
        chkEnableTun.AutoSize = true;
        chkEnableTun.Checked = true;
        chkEnableTun.CheckState = CheckState.Checked;
        chkEnableTun.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chkEnableTun.ForeColor = Color.FromArgb(56, 189, 248);
        chkEnableTun.Location = new Point(16, 98);
        chkEnableTun.Name = "chkEnableTun";
        chkEnableTun.Size = new Size(629, 24);
        chkEnableTun.TabIndex = 2;
        chkEnableTun.Text = "🛡️   Activar VPN a nivel de sistema (TUN/Wintun - Redirigir TODO el tráfico de la PC)";
        chkEnableTun.UseVisualStyleBackColor = true;
        // 
        // groupAdvancedSsh
        // 
        groupAdvancedSsh.BackColor = Color.FromArgb(22, 32, 48);
        groupAdvancedSsh.Controls.Add(lblInternalSshDesc);
        groupAdvancedSsh.Controls.Add(numSshPort);
        groupAdvancedSsh.Controls.Add(lblSshPort);
        groupAdvancedSsh.Controls.Add(lblAdvancedSshTitle);
        groupAdvancedSsh.Location = new Point(24, 415);
        groupAdvancedSsh.Name = "groupAdvancedSsh";
        groupAdvancedSsh.Size = new Size(805, 100);
        groupAdvancedSsh.TabIndex = 4;
        // 
        // lblAdvancedSshTitle
        // 
        lblAdvancedSshTitle.AutoSize = true;
        lblAdvancedSshTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblAdvancedSshTitle.ForeColor = Color.FromArgb(234, 88, 12);
        lblAdvancedSshTitle.Location = new Point(16, 12);
        lblAdvancedSshTitle.Name = "lblAdvancedSshTitle";
        lblAdvancedSshTitle.Size = new Size(225, 20);
        lblAdvancedSshTitle.TabIndex = 0;
        lblAdvancedSshTitle.Text = "⚙️   CONFIGURACIÓN AVANZADA";
        // 
        // lblSshPort
        // 
        lblSshPort.AutoSize = true;
        lblSshPort.Font = new Font("Segoe UI", 8.5F);
        lblSshPort.ForeColor = Color.FromArgb(148, 163, 184);
        lblSshPort.Location = new Point(16, 36);
        lblSshPort.Name = "lblSshPort";
        lblSshPort.Size = new Size(195, 20);
        lblSshPort.TabIndex = 1;
        lblSshPort.Text = "Puerto SSH entrada (interno)";
        // 
        // numSshPort
        // 
        numSshPort.BackColor = Color.FromArgb(30, 41, 59);
        numSshPort.BorderStyle = BorderStyle.FixedSingle;
        numSshPort.Font = new Font("Segoe UI", 9.5F);
        numSshPort.ForeColor = Color.FromArgb(248, 250, 252);
        numSshPort.Location = new Point(16, 58);
        numSshPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        numSshPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        numSshPort.Name = "numSshPort";
        numSshPort.Size = new Size(160, 29);
        numSshPort.TabIndex = 2;
        numSshPort.Value = new decimal(new int[] { 22, 0, 0, 0 });
        // 
        // lblInternalSshDesc
        // 
        lblInternalSshDesc.AutoSize = true;
        lblInternalSshDesc.Font = new Font("Segoe UI", 8.5F);
        lblInternalSshDesc.ForeColor = Color.FromArgb(148, 163, 184);
        lblInternalSshDesc.Location = new Point(195, 62);
        lblInternalSshDesc.Name = "lblInternalSshDesc";
        lblInternalSshDesc.Size = new Size(440, 20);
        lblInternalSshDesc.TabIndex = 3;
        lblInternalSshDesc.Text = "Puerto interno del servicio SSH en el servidor (por defecto 22).";
        // 
        // panelConfigSshButtons
        // 
        panelConfigSshButtons.Controls.Add(btnConnectFromConfig);
        panelConfigSshButtons.Controls.Add(btnSaveConfig);
        panelConfigSshButtons.Location = new Point(24, 528);
        panelConfigSshButtons.Name = "panelConfigSshButtons";
        panelConfigSshButtons.Size = new Size(805, 52);
        panelConfigSshButtons.TabIndex = 5;
        // 
        // btnSaveConfig
        // 
        btnSaveConfig.BackColor = Color.FromArgb(30, 41, 59);
        btnSaveConfig.Cursor = Cursors.Hand;
        btnSaveConfig.FlatAppearance.BorderSize = 0;
        btnSaveConfig.FlatStyle = FlatStyle.Flat;
        btnSaveConfig.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnSaveConfig.ForeColor = Color.FromArgb(226, 232, 240);
        btnSaveConfig.Location = new Point(400, 5);
        btnSaveConfig.Name = "btnSaveConfig";
        btnSaveConfig.Size = new Size(185, 42);
        btnSaveConfig.TabIndex = 0;
        btnSaveConfig.Text = "💾  Guardar config";
        btnSaveConfig.UseVisualStyleBackColor = false;
        btnSaveConfig.Click += btnSaveConfig_Click;
        // 
        // btnConnectFromConfig
        // 
        btnConnectFromConfig.BackColor = Color.FromArgb(234, 88, 12);
        btnConnectFromConfig.Cursor = Cursors.Hand;
        btnConnectFromConfig.FlatAppearance.BorderSize = 0;
        btnConnectFromConfig.FlatStyle = FlatStyle.Flat;
        btnConnectFromConfig.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnConnectFromConfig.ForeColor = Color.White;
        btnConnectFromConfig.Location = new Point(595, 5);
        btnConnectFromConfig.Name = "btnConnectFromConfig";
        btnConnectFromConfig.Size = new Size(210, 42);
        btnConnectFromConfig.TabIndex = 1;
        btnConnectFromConfig.Text = "▶  Guardar y Conectar";
        btnConnectFromConfig.UseVisualStyleBackColor = false;
        btnConnectFromConfig.Click += btnConnect_Click;
        // 
        // panelConfigs
        // 
        panelConfigs.BackColor = Color.FromArgb(11, 15, 25);
        panelConfigs.Controls.Add(dgvConfigs);
        panelConfigs.Controls.Add(panelConfigsTop);
        panelConfigs.Dock = DockStyle.Fill;
        panelConfigs.Location = new Point(0, 65);
        panelConfigs.Name = "panelConfigs";
        panelConfigs.Padding = new Padding(20, 10, 20, 15);
        panelConfigs.Size = new Size(855, 635);
        panelConfigs.TabIndex = 3;
        panelConfigs.Visible = false;
        // 
        // dgvConfigs
        // 
        dgvConfigs.AllowUserToAddRows = false;
        dgvConfigs.AllowUserToDeleteRows = false;
        dgvConfigs.AllowUserToResizeRows = false;
        dgvConfigs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvConfigs.BackgroundColor = Color.FromArgb(15, 23, 42);
        dgvConfigs.BorderStyle = BorderStyle.None;
        dgvConfigs.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(30, 41, 59);
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        dataGridViewCellStyle1.ForeColor = Color.FromArgb(248, 250, 252);
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        dgvConfigs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        dgvConfigs.ColumnHeadersHeight = 36;
        dgvConfigs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        dgvConfigs.Columns.AddRange(new DataGridViewColumn[] { colCfgName, colCfgDate, colCfgUse, colCfgDelete });
        dgvConfigs.Dock = DockStyle.Fill;
        dgvConfigs.EnableHeadersVisualStyles = false;
        dgvConfigs.GridColor = Color.FromArgb(51, 65, 85);
        dgvConfigs.Location = new Point(20, 75);
        dgvConfigs.MultiSelect = false;
        dgvConfigs.Name = "dgvConfigs";
        dgvConfigs.ReadOnly = true;
        dgvConfigs.RowHeadersVisible = false;
        dgvConfigs.RowHeadersWidth = 51;
        dgvConfigs.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(22, 32, 48);
        dgvConfigs.RowTemplate.DefaultCellStyle.ForeColor = Color.FromArgb(248, 250, 252);
        dgvConfigs.RowTemplate.DefaultCellStyle.SelectionBackColor = Color.FromArgb(234, 88, 12);
        dgvConfigs.RowTemplate.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvConfigs.RowTemplate.Height = 38;
        dgvConfigs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvConfigs.Size = new Size(815, 545);
        dgvConfigs.TabIndex = 1;
        dgvConfigs.CellContentClick += dgvConfigs_CellContentClick;
        dgvConfigs.CellDoubleClick += dgvConfigs_CellDoubleClick;
        dgvConfigs.CellPainting += dgvConfigs_CellPainting;
        dgvConfigs.CellMouseMove += dgvConfigs_CellMouseMove;
        dgvConfigs.CellMouseLeave += dgvConfigs_CellMouseLeave;
        // 
        // colCfgName
        // 
        colCfgName.DataPropertyName = "Name";
        colCfgName.FillWeight = 140F;
        colCfgName.HeaderText = "Nombre";
        colCfgName.MinimumWidth = 6;
        colCfgName.Name = "colCfgName";
        colCfgName.ReadOnly = true;
        // 
        // colCfgDate
        // 
        colCfgDate.DataPropertyName = "CreatedAtDisplay";
        colCfgDate.FillWeight = 90F;
        colCfgDate.HeaderText = "Fecha de creación";
        colCfgDate.MinimumWidth = 6;
        colCfgDate.Name = "colCfgDate";
        colCfgDate.ReadOnly = true;
        // 
        // colCfgUse
        // 
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dataGridViewCellStyle2.BackColor = Color.FromArgb(22, 163, 74);
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle2.ForeColor = Color.White;
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(22, 163, 74);
        dataGridViewCellStyle2.SelectionForeColor = Color.White;
        colCfgUse.DefaultCellStyle = dataGridViewCellStyle2;
        colCfgUse.FillWeight = 60F;
        colCfgUse.FlatStyle = FlatStyle.Flat;
        colCfgUse.HeaderText = "Acciones";
        colCfgUse.MinimumWidth = 6;
        colCfgUse.Name = "colCfgUse";
        colCfgUse.ReadOnly = true;
        colCfgUse.Text = "⚡ Usar";
        colCfgUse.UseColumnTextForButtonValue = true;
        // 
        // colCfgDelete
        // 
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter;
        dataGridViewCellStyle3.BackColor = Color.FromArgb(220, 38, 38);
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        dataGridViewCellStyle3.ForeColor = Color.White;
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(220, 38, 38);
        dataGridViewCellStyle3.SelectionForeColor = Color.White;
        colCfgDelete.DefaultCellStyle = dataGridViewCellStyle3;
        colCfgDelete.FillWeight = 60F;
        colCfgDelete.FlatStyle = FlatStyle.Flat;
        colCfgDelete.HeaderText = "";
        colCfgDelete.MinimumWidth = 6;
        colCfgDelete.Name = "colCfgDelete";
        colCfgDelete.ReadOnly = true;
        colCfgDelete.Text = "🗑️ Eliminar";
        colCfgDelete.UseColumnTextForButtonValue = true;
        // 
        // panelConfigsTop
        // 
        panelConfigsTop.BackColor = Color.FromArgb(22, 32, 48);
        panelConfigsTop.Controls.Add(btnSaveConfigTop);
        panelConfigsTop.Controls.Add(lblConfigsCount);
        panelConfigsTop.Controls.Add(lblConfigsTitle);
        panelConfigsTop.Dock = DockStyle.Top;
        panelConfigsTop.Location = new Point(20, 10);
        panelConfigsTop.Name = "panelConfigsTop";
        panelConfigsTop.Size = new Size(815, 65);
        panelConfigsTop.TabIndex = 0;
        // 
        // btnSaveConfigTop
        // 
        btnSaveConfigTop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSaveConfigTop.BackColor = Color.FromArgb(22, 163, 74);
        btnSaveConfigTop.Cursor = Cursors.Hand;
        btnSaveConfigTop.FlatAppearance.BorderSize = 0;
        btnSaveConfigTop.FlatStyle = FlatStyle.Flat;
        btnSaveConfigTop.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnSaveConfigTop.ForeColor = Color.White;
        btnSaveConfigTop.Location = new Point(625, 14);
        btnSaveConfigTop.Name = "btnSaveConfigTop";
        btnSaveConfigTop.Size = new Size(175, 36);
        btnSaveConfigTop.TabIndex = 2;
        btnSaveConfigTop.Text = "➕ Guardar config actual";
        btnSaveConfigTop.UseVisualStyleBackColor = false;
        btnSaveConfigTop.Click += btnSaveConfig_Click;
        // 
        // lblConfigsCount
        // 
        lblConfigsCount.AutoSize = true;
        lblConfigsCount.Font = new Font("Segoe UI", 8.5F);
        lblConfigsCount.ForeColor = Color.FromArgb(148, 163, 184);
        lblConfigsCount.Location = new Point(14, 38);
        lblConfigsCount.Name = "lblConfigsCount";
        lblConfigsCount.Size = new Size(205, 20);
        lblConfigsCount.TabIndex = 1;
        lblConfigsCount.Text = "Configuraciones guardadas: 0";
        // 
        // lblConfigsTitle
        // 
        lblConfigsTitle.AutoSize = true;
        lblConfigsTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblConfigsTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblConfigsTitle.Location = new Point(14, 12);
        lblConfigsTitle.Name = "lblConfigsTitle";
        lblConfigsTitle.Size = new Size(341, 28);
        lblConfigsTitle.TabIndex = 0;
        lblConfigsTitle.Text = "💾 Mis configuraciones guardadas";
        // 
        // panelRegistro
        // 
        panelRegistro.BackColor = Color.FromArgb(11, 15, 25);
        panelRegistro.Controls.Add(rtbLogs);
        panelRegistro.Controls.Add(panelRegistroTop);
        panelRegistro.Dock = DockStyle.Fill;
        panelRegistro.Location = new Point(0, 65);
        panelRegistro.Name = "panelRegistro";
        panelRegistro.Padding = new Padding(20, 10, 20, 15);
        panelRegistro.Size = new Size(855, 635);
        panelRegistro.TabIndex = 4;
        panelRegistro.Visible = false;
        // 
        // rtbLogs
        // 
        rtbLogs.BackColor = Color.FromArgb(15, 23, 42);
        rtbLogs.BorderStyle = BorderStyle.None;
        rtbLogs.Dock = DockStyle.Fill;
        rtbLogs.Font = new Font("Consolas", 9.5F);
        rtbLogs.ForeColor = Color.FromArgb(56, 189, 248);
        rtbLogs.Location = new Point(20, 75);
        rtbLogs.Name = "rtbLogs";
        rtbLogs.ReadOnly = true;
        rtbLogs.Size = new Size(815, 545);
        rtbLogs.TabIndex = 1;
        rtbLogs.Text = "";
        // 
        // panelRegistroTop
        // 
        panelRegistroTop.BackColor = Color.FromArgb(22, 32, 48);
        panelRegistroTop.Controls.Add(btnClearHistory);
        panelRegistroTop.Controls.Add(lblHistoryCount);
        panelRegistroTop.Controls.Add(lblHistoryTitle);
        panelRegistroTop.Dock = DockStyle.Top;
        panelRegistroTop.Location = new Point(20, 10);
        panelRegistroTop.Name = "panelRegistroTop";
        panelRegistroTop.Size = new Size(815, 65);
        panelRegistroTop.TabIndex = 0;
        // 
        // btnClearHistory
        // 
        btnClearHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearHistory.BackColor = Color.FromArgb(51, 65, 85);
        btnClearHistory.Cursor = Cursors.Hand;
        btnClearHistory.FlatAppearance.BorderSize = 0;
        btnClearHistory.FlatStyle = FlatStyle.Flat;
        btnClearHistory.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        btnClearHistory.ForeColor = Color.White;
        btnClearHistory.Location = new Point(650, 14);
        btnClearHistory.Name = "btnClearHistory";
        btnClearHistory.Size = new Size(150, 36);
        btnClearHistory.TabIndex = 2;
        btnClearHistory.Text = "🗑️ Limpiar registro";
        btnClearHistory.UseVisualStyleBackColor = false;
        btnClearHistory.Click += btnClearHistory_Click;
        // 
        // lblHistoryCount
        // 
        lblHistoryCount.AutoSize = true;
        lblHistoryCount.Font = new Font("Segoe UI", 8.5F);
        lblHistoryCount.ForeColor = Color.FromArgb(148, 163, 184);
        lblHistoryCount.Location = new Point(14, 38);
        lblHistoryCount.Name = "lblHistoryCount";
        lblHistoryCount.Size = new Size(65, 20);
        lblHistoryCount.TabIndex = 1;
        lblHistoryCount.Text = "Líneas: 0";
        // 
        // lblHistoryTitle
        // 
        lblHistoryTitle.AutoSize = true;
        lblHistoryTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblHistoryTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblHistoryTitle.Location = new Point(14, 12);
        lblHistoryTitle.Name = "lblHistoryTitle";
        lblHistoryTitle.Size = new Size(246, 28);
        lblHistoryTitle.TabIndex = 0;
        lblHistoryTitle.Text = "📋 Registro de conexión";
        // 
        // panelAcercaDe
        // 
        panelAcercaDe.AutoScroll = true;
        panelAcercaDe.BackColor = Color.FromArgb(11, 15, 25);
        panelAcercaDe.Controls.Add(panelAcercaDonationsCard);
        panelAcercaDe.Controls.Add(panelAcercaInfoCard);
        panelAcercaDe.Controls.Add(lblAcercaSub);
        panelAcercaDe.Controls.Add(lblAcercaHeader);
        panelAcercaDe.Dock = DockStyle.Fill;
        panelAcercaDe.Location = new Point(0, 65);
        panelAcercaDe.Name = "panelAcercaDe";
        panelAcercaDe.Padding = new Padding(20, 10, 20, 15);
        panelAcercaDe.Size = new Size(855, 635);
        panelAcercaDe.TabIndex = 4;
        panelAcercaDe.Visible = false;
        // 
        // panelAcercaDonationsCard
        // 
        panelAcercaDonationsCard.BackColor = Color.FromArgb(22, 32, 48);
        panelAcercaDonationsCard.Controls.Add(lblAcercaYapeThank);
        panelAcercaDonationsCard.Controls.Add(lblAcercaYapeBadge);
        panelAcercaDonationsCard.Controls.Add(panelAcercaYapeBorder);
        panelAcercaDonationsCard.Controls.Add(lblAcercaDonationsSub);
        panelAcercaDonationsCard.Controls.Add(lblAcercaDonationsTitle);
        panelAcercaDonationsCard.Location = new Point(505, 60);
        panelAcercaDonationsCard.Name = "panelAcercaDonationsCard";
        panelAcercaDonationsCard.Size = new Size(330, 520);
        panelAcercaDonationsCard.TabIndex = 3;
        // 
        // lblAcercaYapeThank
        // 
        lblAcercaYapeThank.Font = new Font("Segoe UI", 8.5F);
        lblAcercaYapeThank.ForeColor = Color.FromArgb(203, 213, 225);
        lblAcercaYapeThank.Location = new Point(16, 428);
        lblAcercaYapeThank.Name = "lblAcercaYapeThank";
        lblAcercaYapeThank.Size = new Size(298, 45);
        lblAcercaYapeThank.TabIndex = 4;
        lblAcercaYapeThank.Text = "¡Cualquier apoyo voluntario ayuda a mantener el proyecto activo y actualizado! 🙌";
        lblAcercaYapeThank.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // lblAcercaYapeBadge
        // 
        lblAcercaYapeBadge.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblAcercaYapeBadge.ForeColor = Color.FromArgb(168, 85, 247);
        lblAcercaYapeBadge.Location = new Point(16, 400);
        lblAcercaYapeBadge.Name = "lblAcercaYapeBadge";
        lblAcercaYapeBadge.Size = new Size(298, 24);
        lblAcercaYapeBadge.TabIndex = 3;
        lblAcercaYapeBadge.Text = "📱 Yape Perú";
        lblAcercaYapeBadge.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // panelAcercaYapeBorder
        // 
        panelAcercaYapeBorder.BackColor = Color.FromArgb(15, 23, 42);
        panelAcercaYapeBorder.Controls.Add(picAcercaYape);
        panelAcercaYapeBorder.Location = new Point(25, 80);
        panelAcercaYapeBorder.Name = "panelAcercaYapeBorder";
        panelAcercaYapeBorder.Padding = new Padding(6);
        panelAcercaYapeBorder.Size = new Size(280, 310);
        panelAcercaYapeBorder.TabIndex = 2;
        // 
        // picAcercaYape
        // 
        picAcercaYape.BackColor = Color.FromArgb(15, 23, 42);
        picAcercaYape.Dock = DockStyle.Fill;
        picAcercaYape.Location = new Point(6, 6);
        picAcercaYape.Name = "picAcercaYape";
        picAcercaYape.Size = new Size(268, 298);
        picAcercaYape.SizeMode = PictureBoxSizeMode.Zoom;
        picAcercaYape.TabIndex = 0;
        picAcercaYape.TabStop = false;
        // 
        // lblAcercaDonationsSub
        // 
        lblAcercaDonationsSub.Font = new Font("Segoe UI", 8.5F);
        lblAcercaDonationsSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblAcercaDonationsSub.Location = new Point(18, 42);
        lblAcercaDonationsSub.Name = "lblAcercaDonationsSub";
        lblAcercaDonationsSub.Size = new Size(294, 34);
        lblAcercaDonationsSub.TabIndex = 1;
        lblAcercaDonationsSub.Text = "Escanea con Yape para apoyar el mantenimiento y futuras mejoras:";
        // 
        // lblAcercaDonationsTitle
        // 
        lblAcercaDonationsTitle.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
        lblAcercaDonationsTitle.ForeColor = Color.FromArgb(192, 132, 252);
        lblAcercaDonationsTitle.Location = new Point(16, 16);
        lblAcercaDonationsTitle.Name = "lblAcercaDonationsTitle";
        lblAcercaDonationsTitle.Size = new Size(298, 24);
        lblAcercaDonationsTitle.TabIndex = 0;
        lblAcercaDonationsTitle.Text = "💜 Donaciones (Yape)";
        // 
        // panelAcercaInfoCard
        // 
        panelAcercaInfoCard.BackColor = Color.FromArgb(22, 32, 48);
        panelAcercaInfoCard.Controls.Add(lblAcercaCopyright);
        panelAcercaInfoCard.Controls.Add(lblAcercaGratis);
        panelAcercaInfoCard.Controls.Add(panelAcercaDivider2);
        panelAcercaInfoCard.Controls.Add(lblSpec6Val);
        panelAcercaInfoCard.Controls.Add(lblSpec6Label);
        panelAcercaInfoCard.Controls.Add(lblSpec5Val);
        panelAcercaInfoCard.Controls.Add(lblSpec5Label);
        panelAcercaInfoCard.Controls.Add(lblSpec4Val);
        panelAcercaInfoCard.Controls.Add(lblSpec4Label);
        panelAcercaInfoCard.Controls.Add(lblSpec3Val);
        panelAcercaInfoCard.Controls.Add(lblSpec3Label);
        panelAcercaInfoCard.Controls.Add(lblSpec2Val);
        panelAcercaInfoCard.Controls.Add(lblSpec2Label);
        panelAcercaInfoCard.Controls.Add(lblSpec1Val);
        panelAcercaInfoCard.Controls.Add(lblSpec1Label);
        panelAcercaInfoCard.Controls.Add(lblAcercaSpecsTitle);
        panelAcercaInfoCard.Controls.Add(lblAcercaDescription);
        panelAcercaInfoCard.Controls.Add(panelAcercaDivider1);
        panelAcercaInfoCard.Controls.Add(lblAcercaVersion);
        panelAcercaInfoCard.Controls.Add(lblAcercaAppTitle);
        panelAcercaInfoCard.Controls.Add(picAcercaLogo);
        panelAcercaInfoCard.Location = new Point(20, 60);
        panelAcercaInfoCard.Name = "panelAcercaInfoCard";
        panelAcercaInfoCard.Size = new Size(470, 520);
        panelAcercaInfoCard.TabIndex = 2;
        // 
        // lblAcercaCopyright
        // 
        lblAcercaCopyright.Font = new Font("Segoe UI", 8F);
        lblAcercaCopyright.ForeColor = Color.FromArgb(100, 116, 139);
        lblAcercaCopyright.Location = new Point(20, 480);
        lblAcercaCopyright.Name = "lblAcercaCopyright";
        lblAcercaCopyright.Size = new Size(430, 20);
        lblAcercaCopyright.TabIndex = 20;
        lblAcercaCopyright.Text = "© 2026 TatoVPN — Desarrollado con dedicación por Tato.";
        // 
        // lblAcercaGratis
        // 
        lblAcercaGratis.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblAcercaGratis.ForeColor = Color.FromArgb(56, 189, 248);
        lblAcercaGratis.Location = new Point(20, 385);
        lblAcercaGratis.Name = "lblAcercaGratis";
        lblAcercaGratis.Size = new Size(430, 42);
        lblAcercaGratis.TabIndex = 19;
        lblAcercaGratis.Text = "✨ TatoVPN es una herramienta gratuita creada para la comunidad. Sin suscripciones forzadas ni publicidad.";
        // 
        // panelAcercaDivider2
        // 
        panelAcercaDivider2.BackColor = Color.FromArgb(51, 65, 85);
        panelAcercaDivider2.Location = new Point(20, 370);
        panelAcercaDivider2.Name = "panelAcercaDivider2";
        panelAcercaDivider2.Size = new Size(430, 1);
        panelAcercaDivider2.TabIndex = 18;
        // 
        // lblSpec6Val
        // 
        lblSpec6Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec6Val.ForeColor = Color.FromArgb(56, 189, 248);
        lblSpec6Val.Location = new Point(200, 330);
        lblSpec6Val.Name = "lblSpec6Val";
        lblSpec6Val.Size = new Size(250, 22);
        lblSpec6Val.TabIndex = 17;
        lblSpec6Val.Text = "Tato";
        // 
        // lblSpec6Label
        // 
        lblSpec6Label.Font = new Font("Segoe UI", 9F);
        lblSpec6Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec6Label.Location = new Point(20, 330);
        lblSpec6Label.Name = "lblSpec6Label";
        lblSpec6Label.Size = new Size(170, 22);
        lblSpec6Label.TabIndex = 16;
        lblSpec6Label.Text = "👨‍💻 Desarrollado por:";
        // 
        // lblSpec5Val
        // 
        lblSpec5Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec5Val.ForeColor = Color.FromArgb(74, 222, 128);
        lblSpec5Val.Location = new Point(200, 302);
        lblSpec5Val.Name = "lblSpec5Val";
        lblSpec5Val.Size = new Size(250, 22);
        lblSpec5Val.TabIndex = 15;
        lblSpec5Val.Text = "Gratuita / Libre uso";
        // 
        // lblSpec5Label
        // 
        lblSpec5Label.Font = new Font("Segoe UI", 9F);
        lblSpec5Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec5Label.Location = new Point(20, 302);
        lblSpec5Label.Name = "lblSpec5Label";
        lblSpec5Label.Size = new Size(170, 22);
        lblSpec5Label.TabIndex = 14;
        lblSpec5Label.Text = "📜 Tipo de Licencia:";
        // 
        // lblSpec4Val
        // 
        lblSpec4Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec4Val.ForeColor = Color.FromArgb(226, 232, 240);
        lblSpec4Val.Location = new Point(200, 274);
        lblSpec4Val.Name = "lblSpec4Val";
        lblSpec4Val.Size = new Size(250, 22);
        lblSpec4Val.TabIndex = 13;
        lblSpec4Val.Text = "Windows 10 / 11 (x64)";
        // 
        // lblSpec4Label
        // 
        lblSpec4Label.Font = new Font("Segoe UI", 9F);
        lblSpec4Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec4Label.Location = new Point(20, 274);
        lblSpec4Label.Name = "lblSpec4Label";
        lblSpec4Label.Size = new Size(170, 22);
        lblSpec4Label.TabIndex = 12;
        lblSpec4Label.Text = "💻 Plataforma:";
        // 
        // lblSpec3Val
        // 
        lblSpec3Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec3Val.ForeColor = Color.FromArgb(226, 232, 240);
        lblSpec3Val.Location = new Point(200, 246);
        lblSpec3Val.Name = "lblSpec3Val";
        lblSpec3Val.Size = new Size(250, 22);
        lblSpec3Val.TabIndex = 11;
        lblSpec3Val.Text = "SSH2 / TLS SNI / SOCKS5";
        // 
        // lblSpec3Label
        // 
        lblSpec3Label.Font = new Font("Segoe UI", 9F);
        lblSpec3Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec3Label.Location = new Point(20, 246);
        lblSpec3Label.Name = "lblSpec3Label";
        lblSpec3Label.Size = new Size(170, 22);
        lblSpec3Label.TabIndex = 10;
        lblSpec3Label.Text = "🔒 Protocolos:";
        // 
        // lblSpec2Val
        // 
        lblSpec2Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec2Val.ForeColor = Color.FromArgb(226, 232, 240);
        lblSpec2Val.Location = new Point(200, 218);
        lblSpec2Val.Name = "lblSpec2Val";
        lblSpec2Val.Size = new Size(250, 22);
        lblSpec2Val.TabIndex = 9;
        lblSpec2Val.Text = "Tun2Socks + Wintun";
        // 
        // lblSpec2Label
        // 
        lblSpec2Label.Font = new Font("Segoe UI", 9F);
        lblSpec2Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec2Label.Location = new Point(20, 218);
        lblSpec2Label.Name = "lblSpec2Label";
        lblSpec2Label.Size = new Size(170, 22);
        lblSpec2Label.TabIndex = 8;
        lblSpec2Label.Text = "🛡️ Adaptador Virtual:";
        // 
        // lblSpec1Val
        // 
        lblSpec1Val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSpec1Val.ForeColor = Color.FromArgb(226, 232, 240);
        lblSpec1Val.Location = new Point(200, 190);
        lblSpec1Val.Name = "lblSpec1Val";
        lblSpec1Val.Size = new Size(250, 22);
        lblSpec1Val.TabIndex = 7;
        lblSpec1Val.Text = "SSH.NET 2026.0.0";
        // 
        // lblSpec1Label
        // 
        lblSpec1Label.Font = new Font("Segoe UI", 9F);
        lblSpec1Label.ForeColor = Color.FromArgb(148, 163, 184);
        lblSpec1Label.Location = new Point(20, 190);
        lblSpec1Label.Name = "lblSpec1Label";
        lblSpec1Label.Size = new Size(170, 22);
        lblSpec1Label.TabIndex = 6;
        lblSpec1Label.Text = "🚀 Motor SSH:";
        // 
        // lblAcercaSpecsTitle
        // 
        lblAcercaSpecsTitle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblAcercaSpecsTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblAcercaSpecsTitle.Location = new Point(20, 160);
        lblAcercaSpecsTitle.Name = "lblAcercaSpecsTitle";
        lblAcercaSpecsTitle.Size = new Size(430, 22);
        lblAcercaSpecsTitle.TabIndex = 5;
        lblAcercaSpecsTitle.Text = "⚙️ Especificaciones y Componentes:";
        // 
        // lblAcercaDescription
        // 
        lblAcercaDescription.Font = new Font("Segoe UI", 9F);
        lblAcercaDescription.ForeColor = Color.FromArgb(226, 232, 240);
        lblAcercaDescription.Location = new Point(20, 90);
        lblAcercaDescription.Name = "lblAcercaDescription";
        lblAcercaDescription.Size = new Size(430, 60);
        lblAcercaDescription.TabIndex = 4;
        lblAcercaDescription.Text = "TatoVPN es una aplicación para Windows diseñada para brindar una conexión a Internet segura, privada y sin restricciones mediante túneles SSH y proxy SOCKS5 con adaptador virtual Wintun.";
        // 
        // panelAcercaDivider1
        // 
        panelAcercaDivider1.BackColor = Color.FromArgb(51, 65, 85);
        panelAcercaDivider1.Location = new Point(20, 78);
        panelAcercaDivider1.Name = "panelAcercaDivider1";
        panelAcercaDivider1.Size = new Size(430, 1);
        panelAcercaDivider1.TabIndex = 3;
        // 
        // lblAcercaVersion
        // 
        lblAcercaVersion.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        lblAcercaVersion.ForeColor = Color.FromArgb(148, 163, 184);
        lblAcercaVersion.Location = new Point(84, 44);
        lblAcercaVersion.Name = "lblAcercaVersion";
        lblAcercaVersion.Size = new Size(360, 20);
        lblAcercaVersion.TabIndex = 2;
        lblAcercaVersion.Text = "Versión 1.0.0 (x64) • Stable Release";
        // 
        // lblAcercaAppTitle
        // 
        lblAcercaAppTitle.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        lblAcercaAppTitle.ForeColor = Color.FromArgb(56, 189, 248);
        lblAcercaAppTitle.Location = new Point(82, 14);
        lblAcercaAppTitle.Name = "lblAcercaAppTitle";
        lblAcercaAppTitle.Size = new Size(360, 28);
        lblAcercaAppTitle.TabIndex = 1;
        lblAcercaAppTitle.Text = "TatoVPN";
        // 
        // picAcercaLogo
        // 
        picAcercaLogo.Location = new Point(20, 16);
        picAcercaLogo.Name = "picAcercaLogo";
        picAcercaLogo.Size = new Size(52, 52);
        picAcercaLogo.SizeMode = PictureBoxSizeMode.Zoom;
        picAcercaLogo.TabIndex = 0;
        picAcercaLogo.TabStop = false;
        // 
        // lblAcercaSub
        // 
        lblAcercaSub.AutoSize = true;
        lblAcercaSub.Font = new Font("Segoe UI", 8.5F);
        lblAcercaSub.ForeColor = Color.FromArgb(148, 163, 184);
        lblAcercaSub.Location = new Point(42, 44);
        lblAcercaSub.Name = "lblAcercaSub";
        lblAcercaSub.Size = new Size(643, 20);
        lblAcercaSub.TabIndex = 1;
        lblAcercaSub.Text = "Información del cliente, especificaciones del sistema y donaciones para el soporte del proyecto.";
        // 
        // lblAcercaHeader
        // 
        lblAcercaHeader.AutoSize = true;
        lblAcercaHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        lblAcercaHeader.ForeColor = Color.FromArgb(248, 250, 252);
        lblAcercaHeader.Location = new Point(40, 20);
        lblAcercaHeader.Name = "lblAcercaHeader";
        lblAcercaHeader.Size = new Size(227, 28);
        lblAcercaHeader.TabIndex = 0;
        lblAcercaHeader.Text = "ℹ️ Acerca de TatoVPN";
        // 
        // panelTopHeader
        // 
        panelTopHeader.BackColor = Color.FromArgb(11, 15, 25);
        panelTopHeader.Controls.Add(lblSubtitle);
        panelTopHeader.Controls.Add(lblTitle);
        panelTopHeader.Dock = DockStyle.Top;
        panelTopHeader.Location = new Point(0, 0);
        panelTopHeader.Name = "panelTopHeader";
        panelTopHeader.Padding = new Padding(24, 14, 24, 10);
        panelTopHeader.Size = new Size(855, 65);
        panelTopHeader.TabIndex = 0;
        // 
        // lblSubtitle
        // 
        lblSubtitle.AutoSize = true;
        lblSubtitle.Font = new Font("Segoe UI", 8.5F);
        lblSubtitle.ForeColor = Color.FromArgb(148, 163, 184);
        lblSubtitle.Location = new Point(26, 40);
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Size = new Size(409, 20);
        lblSubtitle.TabIndex = 1;
        lblSubtitle.Text = "Conecta de forma segura a través de SSH con proxy SOCKS5";
        // 
        // lblTitle
        // 
        lblTitle.AutoSize = true;
        lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        lblTitle.ForeColor = Color.FromArgb(248, 250, 252);
        lblTitle.Location = new Point(24, 12);
        lblTitle.Name = "lblTitle";
        lblTitle.Size = new Size(327, 32);
        lblTitle.TabIndex = 0;
        lblTitle.Text = "Cliente SSH / Túnel SOCKS5";
        // 
        // lblState
        // 
        lblState.Location = new Point(0, 0);
        lblState.Name = "lblState";
        lblState.Size = new Size(100, 23);
        lblState.TabIndex = 0;
        // 
        // lblStateValue
        // 
        lblStateValue.Location = new Point(0, 0);
        lblStateValue.Name = "lblStateValue";
        lblStateValue.Size = new Size(100, 23);
        lblStateValue.TabIndex = 0;
        // 
        // groupStatus
        // 
        groupStatus.Location = new Point(0, 0);
        groupStatus.Name = "groupStatus";
        groupStatus.Size = new Size(200, 100);
        groupStatus.TabIndex = 0;
        groupStatus.TabStop = false;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(15, 23, 42);
        ClientSize = new Size(1090, 700);
        Controls.Add(panelMain);
        Controls.Add(panelSidebar);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimumSize = new Size(1090, 700);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "TatoVPN";
        FormClosing += Form1_FormClosing;
        Resize += Form1_Resize;
        trayContextMenu.ResumeLayout(false);
        panelSidebar.ResumeLayout(false);
        panelSidebarStatusCard.ResumeLayout(false);
        panelSidebarStatusCard.PerformLayout();
        panelNavButtons.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
        panelMain.ResumeLayout(false);
        panelInicio.ResumeLayout(false);
        panelFeatureBadges.ResumeLayout(false);
        panelFeatureBadges.PerformLayout();
        panelQuickInfoCard.ResumeLayout(false);
        panelQuickInfoCard.PerformLayout();
        panelMainConnectionCard.ResumeLayout(false);
        panelMainConnectionCard.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picStatusRing).EndInit();
        panelConfigSsh.ResumeLayout(false);
        panelConfigSsh.PerformLayout();
        panelConfigSshButtons.ResumeLayout(false);
        groupTls.ResumeLayout(false);
        groupTls.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numTlsPort).EndInit();
        groupSocks.ResumeLayout(false);
        groupSocks.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numSocksPort).EndInit();
        groupCredentials.ResumeLayout(false);
        groupCredentials.PerformLayout();
        groupSsh.ResumeLayout(false);
        groupSsh.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numSshPort).EndInit();
        panelConfigs.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvConfigs).EndInit();
        panelConfigsTop.ResumeLayout(false);
        panelConfigsTop.PerformLayout();
        panelRegistro.ResumeLayout(false);
        panelRegistroTop.ResumeLayout(false);
        panelRegistroTop.PerformLayout();
        panelAcercaDe.ResumeLayout(false);
        panelAcercaDe.PerformLayout();
        panelModoServidor.ResumeLayout(false);
        panelAcercaDonationsCard.ResumeLayout(false);
        panelAcercaYapeBorder.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picAcercaYape).EndInit();
        panelAcercaInfoCard.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)picAcercaLogo).EndInit();
        panelTopHeader.ResumeLayout(false);
        panelTopHeader.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Panel panelSidebar;
    private System.Windows.Forms.PictureBox picLogo;
    private System.Windows.Forms.Panel panelNavButtons;
    private System.Windows.Forms.Button btnNavDashboard;
    private System.Windows.Forms.Button btnNavInicio;
    private System.Windows.Forms.Button btnNavConfigSsh;
    private System.Windows.Forms.Button btnNavConfigs;
    private System.Windows.Forms.Button btnNavFiltro;
    private System.Windows.Forms.Button btnNavRegistro;
    private System.Windows.Forms.Button btnNavModoServidor;
    private System.Windows.Forms.Button btnNavConexionRemota;
    private System.Windows.Forms.Button btnNavEscritorioRemoto;
    private System.Windows.Forms.Button btnNavAcerca;
    private System.Windows.Forms.Panel panelModoServidor;
    private System.Windows.Forms.Panel panelSidebarStatusCard;
    private System.Windows.Forms.Label lblSidebarDot;
    private System.Windows.Forms.Label lblSidebarStatusTitle;
    private System.Windows.Forms.Label lblSidebarStatusState;
    private System.Windows.Forms.Label lblSidebarStatusSub;

    private System.Windows.Forms.Panel panelMain;
    private System.Windows.Forms.Panel panelTopHeader;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Label lblSubtitle;

    private System.Windows.Forms.Button btnTopFile;
    private System.Windows.Forms.Button btnTopSettings;
    private System.Windows.Forms.Button btnTunnelType;
    private System.Windows.Forms.Panel panelTunnelType;
    private System.Windows.Forms.Label lblTunnelHeader;
    private System.Windows.Forms.Label lblTunnelSub;
    private System.Windows.Forms.Panel groupTunnelProtocol;
    private System.Windows.Forms.Label lblTunnelProtocolTitle;
    private System.Windows.Forms.RadioButton rbTunnelSsh;
    private System.Windows.Forms.Label lblTunnelSshDesc;
    private System.Windows.Forms.RadioButton rbTunnelV2ray;
    private System.Windows.Forms.Label lblTunnelV2rayDesc;
    private System.Windows.Forms.Panel groupConnectFrom;
    private System.Windows.Forms.Label lblConnectFromTitle;
    private System.Windows.Forms.RadioButton rbTunnelDirect;
    private System.Windows.Forms.Label lblTunnelDirectDesc;
    private System.Windows.Forms.RadioButton rbTunnelTls;
    private System.Windows.Forms.Label lblTunnelTlsDesc;
    private System.Windows.Forms.Panel groupTunnelOptions;
    private System.Windows.Forms.Label lblTunnelOptionsTitle;
    private System.Windows.Forms.CheckBox chkCustomPayload;
    private System.Windows.Forms.Label lblCustomPayloadDesc;
    private System.Windows.Forms.Panel panelTunnelButtons;
    private System.Windows.Forms.Button btnSaveTunnel;
    private System.Windows.Forms.Button btnCancelTunnel;
    private System.Windows.Forms.Panel panelSniConfig;
    private System.Windows.Forms.Label lblSniConfigHeader;
    private System.Windows.Forms.Label lblSniConfigSub;
    private System.Windows.Forms.Panel groupSniHost;
    private System.Windows.Forms.Label lblSniHostTitle;
    private System.Windows.Forms.Label lblSniHostSub;
    private System.Windows.Forms.TextBox txtSniHostInput;
    private System.Windows.Forms.Panel groupSniVersion;
    private System.Windows.Forms.Label lblSniVersionTitle;
    private System.Windows.Forms.Label lblSniVersionSub;
    private System.Windows.Forms.ComboBox cmbSniVersionInput;
    private System.Windows.Forms.Panel panelSniButtons;
    private System.Windows.Forms.Button btnSaveSni;
    private System.Windows.Forms.Button btnCancelSni;
    private System.Windows.Forms.Panel panelSniRow;
    private System.Windows.Forms.Label lblSniTag;
    private System.Windows.Forms.Label lblSniValue;
    private System.Windows.Forms.Button btnEditSni;
    private System.Windows.Forms.Panel panelInicio;
    private System.Windows.Forms.Panel panelMainConnectionCard;
    private System.Windows.Forms.Label lblStatusRingTitle;
    private System.Windows.Forms.PictureBox picStatusRing;
    private System.Windows.Forms.Label lblRingLockIcon;
    private System.Windows.Forms.Label lblRingStatusText;
    private System.Windows.Forms.Label lblRingStatusSub;
    private System.Windows.Forms.Button btnConnect;
    private System.Windows.Forms.Button btnDisconnect;
    private System.Windows.Forms.Label lblQuickConfigSummary;
    private System.Windows.Forms.Button btnGoToConfig;

    private System.Windows.Forms.Panel panelQuickInfoCard;
    private System.Windows.Forms.Label lblQuickInfoTitle;
    private System.Windows.Forms.Label lblInfoProtoLabel;
    private System.Windows.Forms.Label lblInfoProtoVal;
    private System.Windows.Forms.Label lblInfoCipherLabel;
    private System.Windows.Forms.Label lblInfoCipherVal;
    private System.Windows.Forms.Label lblInfoProxyLabel;
    private System.Windows.Forms.Label lblInfoProxyVal;
    private System.Windows.Forms.Label lblInfoTunnelLabel;
    private System.Windows.Forms.Label lblInfoTunnelVal;
    private System.Windows.Forms.Label lblInfoUptimeLabel;
    private System.Windows.Forms.Label lblInfoUptimeVal;
    private System.Windows.Forms.Label lblInfoIpLabel;
    private System.Windows.Forms.Label lblInfoIpVal;

    private System.Windows.Forms.Panel panelFeatureBadges;
    private System.Windows.Forms.Label lblFeature1Title;
    private System.Windows.Forms.Label lblFeature1Sub;
    private System.Windows.Forms.Label lblFeature2Title;
    private System.Windows.Forms.Label lblFeature2Sub;
    private System.Windows.Forms.Label lblFeature3Title;
    private System.Windows.Forms.Label lblFeature3Sub;
    private System.Windows.Forms.Label lblFeature4Title;
    private System.Windows.Forms.Label lblFeature4Sub;

    private System.Windows.Forms.Panel panelConfigSsh;
    private System.Windows.Forms.Label lblConfigSshHeader;
    private System.Windows.Forms.Label lblConfigSshSub;
    private System.Windows.Forms.Panel groupSsh;
    private System.Windows.Forms.Label lblSshGroupTitle;
    private System.Windows.Forms.Label lblSshHost;
    private System.Windows.Forms.TextBox txtSshHost;
    private System.Windows.Forms.Label lblSshPort;
    private System.Windows.Forms.NumericUpDown numSshPort;

    private System.Windows.Forms.Panel groupCredentials;
    private System.Windows.Forms.Label lblCredGroupTitle;
    private System.Windows.Forms.Label lblUsername;
    private System.Windows.Forms.TextBox txtUsername;
    private System.Windows.Forms.Label lblPassword;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.Button btnTogglePass;

    private System.Windows.Forms.Panel groupSocks;
    private System.Windows.Forms.Label lblSocksGroupTitle;
    private System.Windows.Forms.Label lblSocksIp;
    private System.Windows.Forms.TextBox txtSocksIp;
    private System.Windows.Forms.Label lblSocksPort;
    private System.Windows.Forms.NumericUpDown numSocksPort;
    public System.Windows.Forms.CheckBox chkEnableTun;

    private System.Windows.Forms.Panel groupAdvancedSsh;
    private System.Windows.Forms.Label lblAdvancedSshTitle;
    private System.Windows.Forms.Label lblInternalSshDesc;

    private System.Windows.Forms.Panel groupTls;
    private System.Windows.Forms.Label lblTlsGroupTitle;
    private System.Windows.Forms.CheckBox chkUseTls;
    private System.Windows.Forms.Label lblTlsPort;
    private System.Windows.Forms.NumericUpDown numTlsPort;
    private System.Windows.Forms.Label lblTlsSni;
    private System.Windows.Forms.TextBox txtTlsSni;

    private System.Windows.Forms.Panel panelConfigSshButtons;
    private System.Windows.Forms.Button btnSaveConfig;
    private System.Windows.Forms.Button btnConnectFromConfig;

    private System.Windows.Forms.Panel panelConfigs;
    private System.Windows.Forms.Panel panelConfigsTop;
    private System.Windows.Forms.Label lblConfigsTitle;
    private System.Windows.Forms.Label lblConfigsCount;
    private System.Windows.Forms.Button btnSaveConfigTop;
    private System.Windows.Forms.DataGridView dgvConfigs;
    private System.Windows.Forms.DataGridViewTextBoxColumn colCfgName;
    private System.Windows.Forms.DataGridViewTextBoxColumn colCfgDate;
    private System.Windows.Forms.DataGridViewButtonColumn colCfgUse;
    private System.Windows.Forms.DataGridViewButtonColumn colCfgDelete;

    private System.Windows.Forms.Panel panelRegistro;
    private System.Windows.Forms.Panel panelRegistroTop;
    private System.Windows.Forms.Label lblHistoryTitle;
    private System.Windows.Forms.Label lblHistoryCount;
    private System.Windows.Forms.Button btnClearHistory;
    private System.Windows.Forms.RichTextBox rtbLogs;

    private System.Windows.Forms.GroupBox groupStatus;
    private System.Windows.Forms.Label lblState;
    private System.Windows.Forms.Label lblStateValue;

    private System.Windows.Forms.NotifyIcon notifyIcon1;
    private System.Windows.Forms.ContextMenuStrip trayContextMenu;
    private System.Windows.Forms.ToolStripMenuItem trayMenuOpen;
    private System.Windows.Forms.ToolStripMenuItem trayMenuDisconnect;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripMenuItem trayMenuExit;

    private System.Windows.Forms.Panel panelAcercaDe;
    private System.Windows.Forms.Label lblAcercaHeader;
    private System.Windows.Forms.Label lblAcercaSub;
    private System.Windows.Forms.Panel panelAcercaInfoCard;
    private System.Windows.Forms.PictureBox picAcercaLogo;
    private System.Windows.Forms.Label lblAcercaAppTitle;
    private System.Windows.Forms.Label lblAcercaVersion;
    private System.Windows.Forms.Panel panelAcercaDivider1;
    private System.Windows.Forms.Label lblAcercaDescription;
    private System.Windows.Forms.Label lblAcercaSpecsTitle;
    private System.Windows.Forms.Label lblSpec1Label;
    private System.Windows.Forms.Label lblSpec1Val;
    private System.Windows.Forms.Label lblSpec2Label;
    private System.Windows.Forms.Label lblSpec2Val;
    private System.Windows.Forms.Label lblSpec3Label;
    private System.Windows.Forms.Label lblSpec3Val;
    private System.Windows.Forms.Label lblSpec4Label;
    private System.Windows.Forms.Label lblSpec4Val;
    private System.Windows.Forms.Label lblSpec5Label;
    private System.Windows.Forms.Label lblSpec5Val;
    private System.Windows.Forms.Label lblSpec6Label;
    private System.Windows.Forms.Label lblSpec6Val;
    private System.Windows.Forms.Panel panelAcercaDivider2;
    private System.Windows.Forms.Label lblAcercaGratis;
    private System.Windows.Forms.Label lblAcercaCopyright;

    private System.Windows.Forms.Panel panelAcercaDonationsCard;
    private System.Windows.Forms.Label lblAcercaDonationsTitle;
    private System.Windows.Forms.Label lblAcercaDonationsSub;
    private System.Windows.Forms.Panel panelAcercaYapeBorder;
    private System.Windows.Forms.PictureBox picAcercaYape;
    private System.Windows.Forms.Label lblAcercaYapeBadge;
    private System.Windows.Forms.Label lblAcercaYapeThank;
}
