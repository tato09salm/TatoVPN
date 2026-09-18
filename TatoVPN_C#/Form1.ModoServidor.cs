using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using miVPN.Services.Implementations;

namespace miVPN;

public partial class Form1
{
    // Controles de Modo Servidor
    private Label lblModoServidorTitle = null!;
    private Label lblModoServidorSub = null!;

    // Tarjeta izquierda (Control & Configuración)
    private Panel panelServerControlCard = null!;
    private Label lblServerStatusBadge = null!;
    private Label lblServerStatusDesc = null!;
    private Button btnServerToggle = null!;

    // Selector de Modo / Propósito del Servidor
    private Label lblPurposeTitle = null!;
    private RadioButton rbServerPurposeFiles   = null!;
    private RadioButton rbServerPurposeHttp    = null!;
    private RadioButton rbServerPurposeDesktop = null!;

    // Panel de estado y acción del Servidor OpenSSH de Windows
    private Panel panelOpenSshCard = null!;
    private Label lblOpenSshStatus = null!;
    private Button btnOpenSshAction = null!;

    // Credenciales
    private Label lblCredTitle = null!;
    private Label lblUser = null!;
    private TextBox txtServerUser = null!;
    private Label lblPass = null!;
    private TextBox txtServerPass = null!;
    private Button btnTogglePassVisibility = null!;
    private Label lblPassHelp = null!;

    // Puertos
    private Label lblServerSshPort = null!;
    private NumericUpDown numServerSshPort = null!;
    private Label lblServerProxyPort = null!;
    private NumericUpDown numServerProxyPort = null!;

    // Modo de Red
    private RadioButton rbServerLan = null!;
    private RadioButton rbServerRemote = null!;
    private Panel pnlStatusBox = null!;
    private Panel grpPurpose = null!;
    private Panel grpNetwork = null!;
    private Label lblLanDesc = null!;
    private Label lblRemoteDesc = null!;
    private Label lblModeTitle = null!;

    // Tarjeta derecha (Datos para conectar dispositivos & Logs)
    private Panel panelServerInfoCard = null!;
    private Label lblInfoTitle = null!;
    private Label lblIpLocal = null!;
    private Label lblPublicHost = null!;
    private Label lblConnString = null!;
    private Label lblLogs = null!;
    private TextBox txtServerLocalIp = null!;
    private Button btnCopyLocalIp = null!;
    private Button btnRefreshIp = null!;
    private TextBox txtServerPublicHost = null!;
    private Button btnCopyPublicHost = null!;
    private TextBox txtServerConnectionString = null!;
    private Button btnCopyConnectionString = null!;
    private RichTextBox rtbServerLogs = null!;
    private Button btnClearServerLogs = null!;

    // Estado del Servidor
    private bool _isServerRunning;
    private LocalSshServerService?    _localSshServer;
    private LocalHttpProxyService?    _localHttpProxy;
    private BadVpnUdpGwService?       _badvpnUdpGw;
    private RemoteTunnelService?      _remoteTunnel;
    private RemoteDesktopServerService? _remoteDesktopServer;
    private string? _remotePublicHost;
    private int? _remotePublicPort;
    private int _sshActiveTunnels;
    private int _proxyActiveConnections;

    // Reglas de Firewall de Windows
    private const string FwRuleSsh = "TatoVPN_Server_SSH_Inbound";
    private const string FwRuleProxy = "TatoVPN_Server_Proxy_Inbound";
    private const string FwRuleOpenSsh = "TatoVPN_Server_OpenSSH_Inbound";
    private const string FwRuleDesktop = "TatoVPN_Server_Desktop_Inbound";

    private void InitializeModoServidor()
    {
        panelModoServidor.AutoScroll = true;
        panelModoServidor.BackColor = Color.FromArgb(11, 15, 25);
        panelModoServidor.Dock = DockStyle.Fill;
        panelModoServidor.Location = new Point(0, 0);
        panelModoServidor.Name = "panelModoServidor";
        panelModoServidor.Padding = new Padding(24, 16, 24, 24);
        panelModoServidor.Size = new Size(855, 700);
        panelModoServidor.TabIndex = 5;
        panelModoServidor.Visible = false;

        // Encabezado
        lblModoServidorTitle = new Label
        {
            Text = "🖥️  Modo Servidor Local (Tato Host)",
            Font = new Font("Segoe UI", 14.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(24, 16),
            AutoSize = true
        };

        lblModoServidorSub = new Label
        {
            Text = "Servidor para transferir archivos vía Conexión Remota (SFTP), compartir internet o escritorio remoto.",
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(24, 48),
            AutoSize = true
        };

        panelModoServidor.Controls.Add(lblModoServidorTitle);
        panelModoServidor.Controls.Add(lblModoServidorSub);

        BuildServerControlCard();
        BuildServerInfoCard();

        panelModoServidor.Resize += (s, e) => AdjustModoServidorLayout();
        AdjustModoServidorLayout();

        RefreshLocalIp();
        UpdatePurposeUiState();
        UpdateConnectionStringPreview();
        AppendServerLog("ℹ️ Módulo Modo Servidor listo con soporte para Conexión Remota (SFTP), HTTP Injector y Escritorio Remoto.");
    }

    private void BuildServerControlCard()
    {
        panelServerControlCard = new Panel
        {
            Location = new Point(24, 80),
            Size = new Size(420, 650),
            BackColor = Color.FromArgb(22, 32, 48),
            BorderStyle = BorderStyle.None
        };

        // Estado del Servidor (Banner)
        pnlStatusBox = new Panel
        {
            Location = new Point(18, 16),
            Size = new Size(384, 68),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblServerStatusBadge = new Label
        {
            Text = "● SERVIDOR DETENIDO",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            Location = new Point(14, 10),
            AutoSize = true
        };

        lblServerStatusDesc = new Label
        {
            Text = "El servidor está apagado. Presiona encender para activarlo.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(14, 34),
            AutoSize = true
        };

        pnlStatusBox.Controls.Add(lblServerStatusBadge);
        pnlStatusBox.Controls.Add(lblServerStatusDesc);
        panelServerControlCard.Controls.Add(pnlStatusBox);

        // Botón Encender / Apagar
        btnServerToggle = new Button
        {
            Text = "▶  Encender Servidor",
            Location = new Point(18, 96),
            Size = new Size(384, 42),
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnServerToggle.FlatAppearance.BorderSize = 0;
        btnServerToggle.Click += BtnServerToggle_Click;
        panelServerControlCard.Controls.Add(btnServerToggle);

        // ── Grupo 1: Selector de Modo / Propósito ──
        lblPurposeTitle = new Label
        {
            Text = "🎯 Modo de Operación / Propósito:",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 150),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblPurposeTitle);

        grpPurpose = new Panel
        {
            Location  = new Point(18, 174),
            Size      = new Size(384, 92),
            BackColor = Color.Transparent
        };

        rbServerPurposeFiles = new RadioButton
        {
            Text      = "📁 Conexión Remota (Archivos / SFTP)",
            Location  = new Point(4, 2),
            Size      = new Size(376, 28),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            Checked   = true
        };
        rbServerPurposeFiles.CheckedChanged += (s, e) => UpdatePurposeUiState();
        grpPurpose.Controls.Add(rbServerPurposeFiles);

        rbServerPurposeHttp = new RadioButton
        {
            Text      = "📱 Compartir Internet (HTTP Injector / Proxy)",
            Location  = new Point(4, 32),
            Size      = new Size(376, 28),
            ForeColor = Color.FromArgb(203, 213, 225),
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        rbServerPurposeHttp.CheckedChanged += (s, e) => UpdatePurposeUiState();
        grpPurpose.Controls.Add(rbServerPurposeHttp);

        rbServerPurposeDesktop = new RadioButton
        {
            Text      = "🖥️ Escritorio Remoto (Ver y controlar esta laptop)",
            Location  = new Point(4, 62),
            Size      = new Size(376, 28),
            ForeColor = Color.FromArgb(203, 213, 225),
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        rbServerPurposeDesktop.CheckedChanged += (s, e) => UpdatePurposeUiState();
        grpPurpose.Controls.Add(rbServerPurposeDesktop);

        panelServerControlCard.Controls.Add(grpPurpose);

        // Panel de Estado y Acción de OpenSSH de Windows
        panelOpenSshCard = new Panel
        {
            Location = new Point(18, 276),
            Size = new Size(384, 64),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblOpenSshStatus = new Label
        {
            Text = "● Verificando Servidor OpenSSH...",
            Location = new Point(12, 10),
            Size = new Size(250, 44),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184)
        };
        panelOpenSshCard.Controls.Add(lblOpenSshStatus);

        btnOpenSshAction = new Button
        {
            Text = "🛡️ Instalar",
            Location = new Point(266, 14),
            Size = new Size(108, 36),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnOpenSshAction.FlatAppearance.BorderSize = 0;
        btnOpenSshAction.Click += BtnOpenSshAction_Click;
        panelOpenSshCard.Controls.Add(btnOpenSshAction);

        panelServerControlCard.Controls.Add(panelOpenSshCard);

        // Sección Credenciales
        lblCredTitle = new Label
        {
            Text = "🔑 Credenciales de Windows (OpenSSH):",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 350),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblCredTitle);

        lblUser = new Label
        {
            Text = "Usuario Windows:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 374),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblUser);

        lblPass = new Label
        {
            Text = "Contraseña Windows:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(210, 374),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblPass);

        txtServerUser = new TextBox
        {
            Text = Environment.UserName,
            Location = new Point(18, 396),
            Size = new Size(180, 28),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        txtServerUser.TextChanged += (s, e) => UpdateConnectionStringPreview();
        panelServerControlCard.Controls.Add(txtServerUser);

        txtServerPass = new TextBox
        {
            Text = "",
            Location = new Point(210, 396),
            Size = new Size(140, 28),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            UseSystemPasswordChar = true
        };
        txtServerPass.TextChanged += (s, e) => UpdateConnectionStringPreview();
        panelServerControlCard.Controls.Add(txtServerPass);

        btnTogglePassVisibility = new Button
        {
            Text = "👁️",
            Location = new Point(356, 395),
            Size = new Size(38, 30),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnTogglePassVisibility.FlatAppearance.BorderSize = 0;
        btnTogglePassVisibility.Click += (s, e) =>
        {
            txtServerPass.UseSystemPasswordChar = !txtServerPass.UseSystemPasswordChar;
            btnTogglePassVisibility.Text = txtServerPass.UseSystemPasswordChar ? "👁️" : "🔒";
        };
        panelServerControlCard.Controls.Add(btnTogglePassVisibility);

        // Texto de ayuda (mantenido en código para compatibilidad, pero oculto del diseño visual)
        lblPassHelp = new Label
        {
            Text = "💡 Usa el usuario y contraseña de tu cuenta de Windows en esta laptop.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(250, 204, 21),
            Location = new Point(18, 430),
            AutoSize = true,
            Visible = false
        };
        panelServerControlCard.Controls.Add(lblPassHelp);

        // Puertos
        lblServerSshPort = new Label
        {
            Text = "Puerto OpenSSH (SFTP):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 460),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblServerSshPort);

        lblServerProxyPort = new Label
        {
            Text = "Puerto Proxy (HTTP/SOCKS):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(210, 460),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblServerProxyPort);

        numServerSshPort = new NumericUpDown
        {
            Location = new Point(18, 482),
            Size = new Size(180, 28),
            Minimum = 1,
            Maximum = 65535,
            Value = 22,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            Enabled = false
        };
        numServerSshPort.ValueChanged += (s, e) => UpdateConnectionStringPreview();
        panelServerControlCard.Controls.Add(numServerSshPort);

        numServerProxyPort = new NumericUpDown
        {
            Location = new Point(210, 482),
            Size = new Size(184, 28),
            Minimum = 1,
            Maximum = 65535,
            Value = 1080,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        panelServerControlCard.Controls.Add(numServerProxyPort);

        // ── Grupo 2: Modo de Red ──
        lblModeTitle = new Label
        {
            Text = "🌐 Modo de Red / Alcance",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 524),
            AutoSize = true
        };
        panelServerControlCard.Controls.Add(lblModeTitle);

        grpNetwork = new Panel
        {
            Location = new Point(18, 548),
            Size = new Size(384, 110),
            BackColor = Color.Transparent
        };

        rbServerLan = new RadioButton
        {
            Text = "📶 Red Local (Wi-Fi / LAN / Hotspot)",
            Location = new Point(4, 2),
            Size = new Size(376, 26),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Checked = true
        };
        rbServerLan.CheckedChanged += (s, e) =>
        {
            if (rbServerLan.Checked)
            {
                StopRemoteTunnelIfRunning();
                txtServerPublicHost.ForeColor = Color.FromArgb(148, 163, 184);
                txtServerPublicHost.Text = "Disponible en modo local o túnel";
                UpdateConnectionStringPreview();
            }
        };
        grpNetwork.Controls.Add(rbServerLan);

        lblLanDesc = new Label
        {
            Text = "Para laptops o celulares conectados a la misma red Wi-Fi o zona compartida.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(24, 28),
            Size = new Size(354, 24),
            AutoSize = true
        };
        grpNetwork.Controls.Add(lblLanDesc);

        rbServerRemote = new RadioButton
        {
            Text = "🌍 Acceso Remoto (Túnel Inverso / Internet)",
            Location = new Point(4, 56),
            Size = new Size(376, 26),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        rbServerRemote.CheckedChanged += (s, e) =>
        {
            if (rbServerRemote.Checked)
            {
                txtServerPublicHost.ForeColor = Color.FromArgb(56, 189, 248);
                if (string.IsNullOrEmpty(_remotePublicHost))
                {
                    txtServerPublicHost.Text = _isServerRunning ? "⏳ Creando túnel inverso..." : "Se activará al encender servidor";
                }
                else
                {
                    txtServerPublicHost.Text = $"{_remotePublicHost}:{_remotePublicPort}";
                }
                UpdateConnectionStringPreview();

                if (_isServerRunning && _remoteTunnel == null)
                {
                    bool isFiles = rbServerPurposeFiles.Checked;
                    StartRemoteTunnel(isFiles ? WindowsOpenSshService.OpenSshPort : (int)numServerSshPort.Value, isRemoteFiles: isFiles);
                }
            }
        };
        grpNetwork.Controls.Add(rbServerRemote);

        lblRemoteDesc = new Label
        {
            Text = "Permite conectar desde cualquier red o datos móviles mediante túnel inverso sin abrir puertos.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(24, 82),
            Size = new Size(354, 26),
            AutoSize = true
        };
        grpNetwork.Controls.Add(lblRemoteDesc);

        panelServerControlCard.Controls.Add(grpNetwork);

        panelModoServidor.Controls.Add(panelServerControlCard);
    }

    private void BuildServerInfoCard()
    {
        panelServerInfoCard = new Panel
        {
            Location = new Point(460, 80),
            Size = new Size(420, 650),
            BackColor = Color.FromArgb(22, 32, 48),
            BorderStyle = BorderStyle.None
        };

        lblInfoTitle = new Label
        {
            Text = "📡 Datos para tus Dispositivos",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 16),
            AutoSize = true
        };
        panelServerInfoCard.Controls.Add(lblInfoTitle);

        // IP Local de la Laptop
        lblIpLocal = new Label
        {
            Text = "IP Local de tu PC (Wi-Fi / Hotspot):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 48),
            AutoSize = true
        };
        panelServerInfoCard.Controls.Add(lblIpLocal);

        txtServerLocalIp = new TextBox
        {
            Text = "127.0.0.1",
            ReadOnly = true,
            Location = new Point(18, 70),
            Size = new Size(250, 30),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(56, 189, 248),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };
        panelServerInfoCard.Controls.Add(txtServerLocalIp);

        btnCopyLocalIp = new Button
        {
            Text = "Copiar",
            Location = new Point(276, 69),
            Size = new Size(76, 30),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopyLocalIp.FlatAppearance.BorderSize = 0;
        btnCopyLocalIp.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(txtServerLocalIp.Text))
            {
                Clipboard.SetText(txtServerLocalIp.Text);
                ShowNotificationTip("IP Local copiada al portapapeles");
            }
        };
        panelServerInfoCard.Controls.Add(btnCopyLocalIp);

        btnRefreshIp = new Button
        {
            Text = "🔄",
            Location = new Point(358, 69),
            Size = new Size(44, 30),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(56, 189, 248),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRefreshIp.FlatAppearance.BorderSize = 0;
        btnRefreshIp.Click += (s, e) =>
        {
            RefreshLocalIp();
            UpdateConnectionStringPreview();
            AppendServerLog($"🔄 IP Local actualizada: {txtServerLocalIp.Text}");
        };
        panelServerInfoCard.Controls.Add(btnRefreshIp);

        // Host Público / Túnel
        lblPublicHost = new Label
        {
            Text = "Host Público / Túnel Remoto:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 112),
            AutoSize = true
        };
        panelServerInfoCard.Controls.Add(lblPublicHost);

        txtServerPublicHost = new TextBox
        {
            Text = "Disponible en modo local o túnel",
            ReadOnly = true,
            Location = new Point(18, 134),
            Size = new Size(302, 30),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(148, 163, 184),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F)
        };
        panelServerInfoCard.Controls.Add(txtServerPublicHost);

        btnCopyPublicHost = new Button
        {
            Text = "Copiar",
            Location = new Point(326, 133),
            Size = new Size(76, 30),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopyPublicHost.FlatAppearance.BorderSize = 0;
        btnCopyPublicHost.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(txtServerPublicHost.Text))
            {
                Clipboard.SetText(txtServerPublicHost.Text);
                ShowNotificationTip("Host copiado al portapapeles");
            }
        };
        panelServerInfoCard.Controls.Add(btnCopyPublicHost);

        // Credencial Formato TatoVPN
        lblConnString = new Label
        {
            Text = "Credencial Formato TatoVPN / SSH:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(18, 176),
            AutoSize = true
        };
        panelServerInfoCard.Controls.Add(lblConnString);

        txtServerConnectionString = new TextBox
        {
            ReadOnly = true,
            Location = new Point(18, 198),
            Size = new Size(384, 30),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(250, 204, 21),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        panelServerInfoCard.Controls.Add(txtServerConnectionString);

        btnCopyConnectionString = new Button
        {
            Text = "📋  Copiar Configuración Completa",
            Location = new Point(18, 236),
            Size = new Size(384, 38),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        btnCopyConnectionString.FlatAppearance.BorderSize = 0;
        btnCopyConnectionString.Click += (s, e) =>
        {
            if (!string.IsNullOrWhiteSpace(txtServerConnectionString.Text))
            {
                Clipboard.SetText(txtServerConnectionString.Text);
                string tip = rbServerPurposeFiles.Checked
                    ? "¡Configuración copiada! Pégala en el módulo 'Conexión Remota' de la otra laptop."
                    : "¡Configuración copiada! Pégala en tu celular o TatoVPN.";
                ShowNotificationTip(tip);
            }
        };
        panelServerInfoCard.Controls.Add(btnCopyConnectionString);

        // Log de Actividad del Servidor
        lblLogs = new Label
        {
            Text = "📋 Registro en Vivo del Servidor:",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(18, 288),
            AutoSize = true
        };
        panelServerInfoCard.Controls.Add(lblLogs);

        btnClearServerLogs = new Button
        {
            Text = "Limpiar",
            Location = new Point(326, 284),
            Size = new Size(76, 28),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(148, 163, 184),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F),
            Cursor = Cursors.Hand
        };
        btnClearServerLogs.FlatAppearance.BorderSize = 0;
        btnClearServerLogs.Click += (s, e) => rtbServerLogs.Clear();
        panelServerInfoCard.Controls.Add(btnClearServerLogs);

        rtbServerLogs = new RichTextBox
        {
            Location = new Point(18, 318),
            Size = new Size(384, 310),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(203, 213, 225),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 8.8F),
            ReadOnly = true
        };
        panelServerInfoCard.Controls.Add(rtbServerLogs);

        panelModoServidor.Controls.Add(panelServerInfoCard);
    }

    private void AdjustModoServidorLayout()
    {
        if (panelModoServidor == null || panelServerControlCard == null || panelServerInfoCard == null)
            return;

        int availWidth = panelModoServidor.ClientSize.Width;
        int availHeight = panelModoServidor.ClientSize.Height;
        if (availWidth <= 0) return;

        int marginX = 24;
        int gap = 20;

        lblModoServidorTitle.Location = new Point(marginX, 16);
        lblModoServidorSub.Location = new Point(marginX, lblModoServidorTitle.Bottom + 4);
        int startY = lblModoServidorSub.Bottom + 16;

        // Si el ancho disponible es >= 880px, mostramos 2 columnas lado a lado
        if (availWidth >= 880)
        {
            int maxTotalWidth = 1180;
            int totalCardsWidth = Math.Min(maxTotalWidth, availWidth - (marginX * 2));
            int startX = marginX + Math.Max(0, (availWidth - (marginX * 2) - totalCardsWidth) / 2);
            int cardWidth = (totalCardsWidth - gap) / 2;

            panelServerControlCard.Location = new Point(startX, startY);
            panelServerControlCard.Width = cardWidth;

            int requiredControlHeight = AdjustServerControlCardControls();
            int targetHeight = Math.Max(requiredControlHeight, Math.Max(660, availHeight - startY - 24));
            panelServerControlCard.Height = targetHeight;

            panelServerInfoCard.Location = new Point(startX + cardWidth + gap, startY);
            panelServerInfoCard.Width = cardWidth;
            panelServerInfoCard.Height = targetHeight;

            AdjustServerInfoCardControls();
        }
        else
        {
            // Ancho menor a 880px: 1 columna apilada verticalmente
            int cardWidth = Math.Max(320, availWidth - (marginX * 2));
            int startX = marginX;

            panelServerControlCard.Location = new Point(startX, startY);
            panelServerControlCard.Width = cardWidth;
            int requiredControlHeight = AdjustServerControlCardControls();
            panelServerControlCard.Height = requiredControlHeight;

            panelServerInfoCard.Location = new Point(startX, panelServerControlCard.Bottom + gap);
            panelServerInfoCard.Width = cardWidth;
            panelServerInfoCard.Height = Math.Max(620, Math.Min(700, availHeight - 100));

            AdjustServerInfoCardControls();
        }
    }

    private int AdjustServerControlCardControls()
    {
        if (panelServerControlCard == null) return 650;
        int cardWidth = panelServerControlCard.ClientSize.Width;
        if (cardWidth <= 100) return 650;

        int padX = 18;
        int innerWidth = cardWidth - (padX * 2);
        int currentY = 16;

        // 1. Estado del Servidor (Banner)
        pnlStatusBox.Location = new Point(padX, currentY);
        pnlStatusBox.Width = innerWidth;
        lblServerStatusBadge.Location = new Point(14, 10);
        lblServerStatusDesc.Location = new Point(14, lblServerStatusBadge.Bottom + 4);
        lblServerStatusDesc.MaximumSize = new Size(innerWidth - 28, 0);
        pnlStatusBox.Height = Math.Max(66, lblServerStatusDesc.Bottom + 10);
        currentY += pnlStatusBox.Height + 12;

        // 2. Botón Encender / Apagar
        btnServerToggle.Location = new Point(padX, currentY);
        btnServerToggle.Width = innerWidth;
        btnServerToggle.Height = 42;
        currentY += btnServerToggle.Height + 16;

        // 3. Propósito
        lblPurposeTitle.Location = new Point(padX, currentY);
        lblPurposeTitle.Width = innerWidth;
        currentY += lblPurposeTitle.Height + 6;

        grpPurpose.Location = new Point(padX, currentY);
        grpPurpose.Width = innerWidth;

        int purposeY = 2;
        RadioButton[] purposeRadios = { rbServerPurposeFiles, rbServerPurposeHttp, rbServerPurposeDesktop };
        foreach (var rb in purposeRadios)
        {
            rb.Location = new Point(4, purposeY);
            rb.Width = innerWidth - 8;
            Size measured = TextRenderer.MeasureText(rb.Text, rb.Font, new Size(Math.Max(60, rb.Width - 28), int.MaxValue), TextFormatFlags.WordBreak);
            rb.Height = Math.Max(30, measured.Height + 8);
            purposeY += rb.Height + 4;
        }
        grpPurpose.Height = purposeY;
        currentY += grpPurpose.Height + 14;

        // 4. Panel OpenSSH (solo visible en modo archivos)
        if (panelOpenSshCard.Visible)
        {
            panelOpenSshCard.Location = new Point(padX, currentY);
            panelOpenSshCard.Width = innerWidth;
            panelOpenSshCard.Height = 74;

            btnOpenSshAction.Width = 110;
            btnOpenSshAction.Height = 36;
            btnOpenSshAction.Left = innerWidth - btnOpenSshAction.Width - 12;
            btnOpenSshAction.Top = (panelOpenSshCard.Height - btnOpenSshAction.Height) / 2;

            lblOpenSshStatus.Location = new Point(12, 8);
            lblOpenSshStatus.Width = Math.Max(80, btnOpenSshAction.Left - 20);
            lblOpenSshStatus.Height = 58;

            currentY += panelOpenSshCard.Height + 14;
        }

        // 5. Credenciales y Puertos (visible en Archivos y HTTP)
        if (lblCredTitle.Visible)
        {
            lblCredTitle.Location = new Point(padX, currentY);
            lblCredTitle.Width = innerWidth;
            currentY += lblCredTitle.Height + 6;

            int credGap = 12;
            int colWidth = (innerWidth - credGap) / 2;
            int rightColX = padX + colWidth + credGap;

            // Labels Usuario / Contraseña
            lblUser.Location = new Point(padX, currentY);
            lblUser.Width = colWidth;
            lblPass.Location = new Point(rightColX, currentY);
            lblPass.Width = colWidth;
            currentY += Math.Max(lblUser.Height, lblPass.Height) + 4;

            // Inputs Usuario / Contraseña + Ojo
            txtServerUser.Location = new Point(padX, currentY);
            txtServerUser.Width = colWidth;
            txtServerUser.Height = 28;

            int eyeWidth = 38;
            int passInputWidth = Math.Max(40, colWidth - eyeWidth - 6);
            txtServerPass.Location = new Point(rightColX, currentY);
            txtServerPass.Width = passInputWidth;
            txtServerPass.Height = 28;

            btnTogglePassVisibility.Location = new Point(rightColX + passInputWidth + 6, currentY - 1);
            btnTogglePassVisibility.Size = new Size(eyeWidth, 30);
            currentY += 28 + 12;

            // Help label: permanece declarado pero visualmente oculto y colapsado (0 px)
            lblPassHelp.Visible = false;
            lblPassHelp.Height = 0;

            // Puertos
            if (lblServerSshPort.Visible)
            {
                lblServerSshPort.Location = new Point(padX, currentY);
                lblServerSshPort.Width = colWidth;

                lblServerProxyPort.Location = new Point(rightColX, currentY);
                lblServerProxyPort.Width = colWidth;
                currentY += Math.Max(lblServerSshPort.Height, lblServerProxyPort.Height) + 4;

                numServerSshPort.Location = new Point(padX, currentY);
                numServerSshPort.Width = colWidth;
                numServerSshPort.Height = 28;

                numServerProxyPort.Location = new Point(rightColX, currentY);
                numServerProxyPort.Width = colWidth;
                numServerProxyPort.Height = 28;
                currentY += 28 + 14;
            }
        }

        // 6. Modo de Red
        lblModeTitle.Location = new Point(padX, currentY);
        lblModeTitle.Width = innerWidth;
        currentY += lblModeTitle.Height + 8;

        grpNetwork.Location = new Point(padX, currentY);
        grpNetwork.Width = innerWidth;

        // Opción 1: LAN
        rbServerLan.Location = new Point(4, 4);
        rbServerLan.Width = innerWidth - 8;
        Size lanTitleSize = TextRenderer.MeasureText(rbServerLan.Text, rbServerLan.Font, new Size(Math.Max(60, rbServerLan.Width - 28), int.MaxValue), TextFormatFlags.WordBreak);
        rbServerLan.Height = Math.Max(30, lanTitleSize.Height + 6);

        lblLanDesc.Location = new Point(28, rbServerLan.Bottom + 2);
        lblLanDesc.Width = innerWidth - 36;
        lblLanDesc.MaximumSize = new Size(innerWidth - 36, 0);

        // Opción 2: Acceso Remoto
        int remoteY = lblLanDesc.Bottom + 12;
        rbServerRemote.Location = new Point(4, remoteY);
        rbServerRemote.Width = innerWidth - 8;
        Size remoteTitleSize = TextRenderer.MeasureText(rbServerRemote.Text, rbServerRemote.Font, new Size(Math.Max(60, rbServerRemote.Width - 28), int.MaxValue), TextFormatFlags.WordBreak);
        rbServerRemote.Height = Math.Max(30, remoteTitleSize.Height + 6);

        lblRemoteDesc.Location = new Point(28, rbServerRemote.Bottom + 2);
        lblRemoteDesc.Width = innerWidth - 36;
        lblRemoteDesc.MaximumSize = new Size(innerWidth - 36, 0);

        grpNetwork.Height = lblRemoteDesc.Bottom + 8;
        currentY += grpNetwork.Height + 18;

        return currentY;
    }

    private void AdjustServerInfoCardControls()
    {
        if (panelServerInfoCard == null) return;
        int cardWidth = panelServerInfoCard.ClientSize.Width;
        if (cardWidth <= 100) return;

        int padX = 18;
        int innerWidth = cardWidth - (padX * 2);

        lblInfoTitle.Location = new Point(padX, 16);
        lblInfoTitle.Width = innerWidth;

        // IP Local
        lblIpLocal.Location = new Point(padX, 48);
        lblIpLocal.Width = innerWidth;

        int btnRefreshWidth = 46;
        int btnCopyWidth = 88;
        int btnHeight = 32;

        btnRefreshIp.Size = new Size(btnRefreshWidth, btnHeight);
        btnRefreshIp.Location = new Point(cardWidth - padX - btnRefreshWidth, 70);

        btnCopyLocalIp.Size = new Size(btnCopyWidth, btnHeight);
        btnCopyLocalIp.Location = new Point(btnRefreshIp.Left - 8 - btnCopyWidth, 70);

        txtServerLocalIp.Location = new Point(padX, 71);
        txtServerLocalIp.Size = new Size(Math.Max(80, btnCopyLocalIp.Left - padX - 8), 30);

        // Host Público
        lblPublicHost.Location = new Point(padX, 112);
        lblPublicHost.Width = innerWidth;

        btnCopyPublicHost.Size = new Size(btnCopyWidth, btnHeight);
        btnCopyPublicHost.Location = new Point(cardWidth - padX - btnCopyWidth, 134);

        txtServerPublicHost.Location = new Point(padX, 134);
        txtServerPublicHost.Size = new Size(Math.Max(80, btnCopyPublicHost.Left - padX - 8), 30);

        // Cadena de Conexión
        lblConnString.Location = new Point(padX, 176);
        lblConnString.Width = innerWidth;

        txtServerConnectionString.Location = new Point(padX, 198);
        txtServerConnectionString.Size = new Size(innerWidth, 30);

        btnCopyConnectionString.Location = new Point(padX, 236);
        btnCopyConnectionString.Size = new Size(innerWidth, 38);

        // Logs
        int btnClearWidth = 88;
        btnClearServerLogs.Size = new Size(btnClearWidth, 30);
        btnClearServerLogs.Location = new Point(cardWidth - padX - btnClearWidth, 282);

        lblLogs.Location = new Point(padX, 287);
        lblLogs.Width = Math.Max(100, btnClearServerLogs.Left - padX - 8);

        rtbServerLogs.Location = new Point(padX, 322);
        rtbServerLogs.Width = innerWidth;
        rtbServerLogs.Height = Math.Max(260, panelServerInfoCard.ClientSize.Height - rtbServerLogs.Top - 18);
    }

    private void UpdatePurposeUiState()
    {
        bool isFilesMode   = rbServerPurposeFiles.Checked;
        bool isDesktopMode = rbServerPurposeDesktop?.Checked ?? false;
        bool isHttpMode    = rbServerPurposeHttp?.Checked ?? false;

        // El panel OpenSSH solo aplica para modo Archivos
        panelOpenSshCard.Visible = isFilesMode;

        // Las credenciales solo aplican para Archivos y HTTP Injector, no para Escritorio
        bool showCreds = !isDesktopMode;
        lblCredTitle.Visible   = showCreds;
        lblUser.Visible        = showCreds;
        lblPass.Visible        = showCreds;
        txtServerUser.Visible  = showCreds;
        txtServerPass.Visible  = showCreds;
        btnTogglePassVisibility.Visible = showCreds;
        lblPassHelp.Visible    = false; // Oculto permanentemente del diseño visual

        // Los puertos solo aplican para Archivos y HTTP Injector
        lblServerSshPort.Visible   = showCreds;
        numServerSshPort.Visible   = showCreds;
        lblServerProxyPort.Visible = isHttpMode;
        numServerProxyPort.Visible = isHttpMode;

        if (isFilesMode)
        {
            lblCredTitle.Text = "🔑 Credenciales de Windows (OpenSSH):";
            lblUser.Text = "Usuario Windows:";
            lblPass.Text = "Contraseña Windows:";
            txtServerUser.Text = Environment.UserName;
            lblServerSshPort.Text = "Puerto OpenSSH (SFTP):";
            numServerSshPort.Value = WindowsOpenSshService.OpenSshPort;
            numServerSshPort.Enabled = false;
            UpdateOpenSshStatusUi();
        }
        else if (isDesktopMode)
        {
            // En modo escritorio remoto no se necesitan credenciales propias.
            // El túnel SSH ya está autenticado. El servidor desktop no pide usuario/contraseña.
        }
        else
        {
            lblCredTitle.Text = "🔑 Credenciales de la Cuenta SSH:";
            lblUser.Text = "Usuario:";
            lblPass.Text = "Contraseña:";
            txtServerUser.Text = "tatouser";
            lblServerSshPort.Text = "Puerto SSH (HTTP Injector):";
            numServerSshPort.Value = 2222;
            numServerSshPort.Enabled = !_isServerRunning;
            numServerProxyPort.Enabled = !_isServerRunning;
        }

        UpdateConnectionStringPreview();
        AdjustModoServidorLayout();
    }

    private void UpdateOpenSshStatusUi()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateOpenSshStatusUi);
            return;
        }

        var status = WindowsOpenSshService.GetStatus();
        switch (status)
        {
            case WindowsOpenSshStatus.InstalledRunning:
                lblOpenSshStatus.Text = "● Servidor OpenSSH de Windows:\r\n  Activo y listo (puerto 22)";
                lblOpenSshStatus.ForeColor = Color.FromArgb(34, 197, 94);
                btnOpenSshAction.Text = "✅ Activo";
                btnOpenSshAction.BackColor = Color.FromArgb(20, 83, 45);
                btnOpenSshAction.Enabled = false;
                break;

            case WindowsOpenSshStatus.InstalledStopped:
                lblOpenSshStatus.Text = "● Servidor OpenSSH de Windows:\r\n  Instalado pero detenido";
                lblOpenSshStatus.ForeColor = Color.FromArgb(251, 191, 36);
                btnOpenSshAction.Text = "▶ Iniciar sshd";
                btnOpenSshAction.BackColor = Color.FromArgb(234, 88, 12);
                btnOpenSshAction.Enabled = !_isServerRunning;
                break;

            case WindowsOpenSshStatus.NotInstalled:
            default:
                lblOpenSshStatus.Text = "● Servidor OpenSSH de Windows:\r\n  No instalado (requerido para SFTP)";
                lblOpenSshStatus.ForeColor = Color.FromArgb(239, 68, 68);
                btnOpenSshAction.Text = "🛡️ Instalar";
                btnOpenSshAction.BackColor = Color.FromArgb(220, 38, 38);
                btnOpenSshAction.Enabled = !_isServerRunning;
                break;
        }
    }

    private async void BtnOpenSshAction_Click(object? sender, EventArgs e)
    {
        var status = WindowsOpenSshService.GetStatus();
        if (status == WindowsOpenSshStatus.NotInstalled)
        {
            var dialog = MessageBox.Show(
                "TatoVPN instalará el 'Servidor OpenSSH' nativo de Windows (sshd).\n\n" +
                "Esta característica permite transferir archivos de forma rápida y segura vía SFTP.\n\n" +
                "¿Deseas proceder con la instalación con permisos de Administrador (UAC)?",
                "Instalar Servidor OpenSSH de Windows",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dialog != DialogResult.Yes) return;

            btnOpenSshAction.Enabled = false;
            btnOpenSshAction.Text = "⏳ Instalando...";
            AppendServerLog("🛡️ Iniciando instalación de OpenSSH.Server con permisos de Administrador...");

            var (ok, msg) = await WindowsOpenSshService.InstallAndStartAsync();
            AppendServerLog(ok ? $"✅ {msg}" : $"❌ {msg}");
            UpdateOpenSshStatusUi();

            if (ok)
            {
                MessageBox.Show("¡Servidor OpenSSH de Windows instalado y activado con éxito!",
                    "Instalación Completa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"No se pudo completar la instalación:\n{msg}",
                    "Aviso de Instalación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else if (status == WindowsOpenSshStatus.InstalledStopped)
        {
            btnOpenSshAction.Enabled = false;
            btnOpenSshAction.Text = "⏳ Iniciando...";
            AppendServerLog("▶ Iniciando servicio OpenSSH (sshd)...");

            var (ok, msg) = await WindowsOpenSshService.StartServiceAsync();
            AppendServerLog(ok ? $"✅ {msg}" : $"❌ {msg}");
            UpdateOpenSshStatusUi();
        }
    }

    private void RefreshLocalIp()
    {
        try
        {
            var candidates = new List<(string ip, string name, bool hasGateway, bool isWifi)>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                string fullName = (ni.Name + " " + ni.Description).ToLowerInvariant();

                // Descartar adaptadores virtuales que confunden la IP
                if (fullName.Contains("virtualbox") || fullName.Contains("vmware") ||
                    fullName.Contains("wintun") || fullName.Contains("vethernet") ||
                    fullName.Contains("bluetooth") || fullName.Contains("tatovpn") ||
                    fullName.Contains("hyper-v") || fullName.Contains("wsl"))
                {
                    continue;
                }

                var ipProps = ni.GetIPProperties();
                bool hasGateway = ipProps.GatewayAddresses.Any(g =>
                    g.Address != null &&
                    !g.Address.Equals(IPAddress.Any) &&
                    !string.IsNullOrWhiteSpace(g.Address.ToString()) &&
                    g.Address.AddressFamily == AddressFamily.InterNetwork);

                bool isWifi = ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                              fullName.Contains("wi-fi") || fullName.Contains("wireless");

                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ipStr = addr.Address.ToString();
                        if (ipStr != "127.0.0.1" && !ipStr.StartsWith("169.254."))
                        {
                            candidates.Add((ipStr, ni.Name, hasGateway, isWifi));
                        }
                    }
                }
            }

            // Priorizar adaptador con puerta de enlace válida y Wi-Fi (conexión con celular)
            var best = candidates
                .OrderByDescending(c => c.hasGateway && c.isWifi)
                .ThenByDescending(c => c.hasGateway)
                .ThenByDescending(c => c.isWifi)
                .ThenByDescending(c => c.ip.StartsWith("192.168.") || c.ip.StartsWith("10.") || c.ip.StartsWith("172."))
                .FirstOrDefault();

            txtServerLocalIp.Text = !string.IsNullOrEmpty(best.ip) ? best.ip : "127.0.0.1";
        }
        catch
        {
            txtServerLocalIp.Text = "127.0.0.1";
        }
    }

    private void UpdateConnectionStringPreview()
    {
        bool isFiles   = rbServerPurposeFiles?.Checked   ?? false;
        bool isDesktop = rbServerPurposeDesktop?.Checked ?? false;

        int defaultPort = isFiles ? WindowsOpenSshService.OpenSshPort
                        : isDesktop ? RemoteDesktopServerService.DefaultPort
                        : (int)numServerSshPort.Value;
        string host;
        int port;

        if (rbServerRemote.Checked)
        {
            if (!string.IsNullOrWhiteSpace(_remotePublicHost) && _remotePublicPort.HasValue)
            {
                host = _remotePublicHost;
                port = _remotePublicPort.Value;
            }
            else if (!txtServerPublicHost.Text.Contains("disponible") &&
                     !txtServerPublicHost.Text.Contains("activará") &&
                     !txtServerPublicHost.Text.Contains("Creando") &&
                     !txtServerPublicHost.Text.Contains("Conectando") &&
                     txtServerPublicHost.Text.Contains(":"))
            {
                var parts = txtServerPublicHost.Text.Split(':');
                host = parts[0];
                int.TryParse(parts.Length > 1 ? parts[1] : "0", out port);
                if (port == 0) port = defaultPort;
            }
            else
            {
                txtServerConnectionString.Text = rbServerRemote.Checked && !_isServerRunning
                    ? "(Enciende el servidor para obtener el enlace público)"
                    : "(Esperando túnel público...)";
                return;
            }
        }
        else
        {
            host = txtServerLocalIp.Text;
            port = defaultPort;
        }

        if (isDesktop)
        {
            // Para escritorio remoto no hay usuario/contraseña. Solo host:puerto@escritorio
            txtServerConnectionString.Text = $"{host}:{port}@escritorio";
        }
        else
        {
            string user = string.IsNullOrWhiteSpace(txtServerUser.Text)
                ? (isFiles ? Environment.UserName : "tatouser")
                : txtServerUser.Text.Trim();
            string pass = txtServerPass.Text.Trim();
            string passDisplay = string.IsNullOrEmpty(pass) ? "<ingresa_contraseña>" : pass;
            txtServerConnectionString.Text = $"{host}:{port}@{user}:{passDisplay}";
        }
    }

    private void AppendServerLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendServerLog(message));
            return;
        }

        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        rtbServerLogs.AppendText($"[{timestamp}] {message}\r\n");
        rtbServerLogs.SelectionStart = rtbServerLogs.Text.Length;
        rtbServerLogs.ScrollToCaret();
    }

    private void ShowNotificationTip(string message)
    {
        MessageBox.Show(message, "Modo Servidor", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async void BtnServerToggle_Click(object? sender, EventArgs e)
    {
        if (!_isServerRunning)
        {
            await StartServerModeAsync();
        }
        else
        {
            StopServerMode();
        }
    }

    private async Task StartServerModeAsync()
    {
        try
        {
            btnServerToggle.Enabled = false;
            RefreshLocalIp();
            UpdateConnectionStringPreview();

            bool isFilesMode   = rbServerPurposeFiles.Checked;
            bool isDesktopMode = rbServerPurposeDesktop?.Checked ?? false;
            string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? (isFilesMode ? Environment.UserName : "tatouser") : txtServerUser.Text.Trim();
            string pass = txtServerPass.Text.Trim();

            // En modo escritorio no se requiere contraseña
            if (!isDesktopMode && string.IsNullOrWhiteSpace(pass))
            {
                string msg = isFilesMode
                    ? "Por favor ingresa la contraseña de tu cuenta de Windows en esta laptop."
                    : "Por favor ingresa una contraseña para el servidor SSH.";
                MessageBox.Show(msg, "Contraseña requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isFilesMode)
            {
                // Flujo A: Servidor OpenSSH nativo de Windows para Conexión Remota / Archivos (SFTP)
                var openSshStatus = WindowsOpenSshService.GetStatus();
                if (openSshStatus == WindowsOpenSshStatus.NotInstalled)
                {
                    var dlg = MessageBox.Show(
                        "Para usar Conexión Remota (archivos SFTP), se requiere tener instalado el Servidor OpenSSH nativo de Windows.\n\n" +
                        "¿Deseas instalarlo y activarlo ahora? (Requerirá confirmación de Administrador UAC)",
                        "Servidor OpenSSH no instalado",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (dlg == DialogResult.Yes)
                    {
                        AppendServerLog("🛡️ Solicitando instalación del Servidor OpenSSH de Windows (UAC)...");
                        var (instOk, instMsg) = await WindowsOpenSshService.InstallAndStartAsync();
                        AppendServerLog(instOk ? $"✅ {instMsg}" : $"❌ {instMsg}");
                        UpdateOpenSshStatusUi();
                        if (!instOk) return;
                    }
                    else
                    {
                        AppendServerLog("⚠️ Operación cancelada: El Servidor OpenSSH es requerido para el módulo de archivos.");
                        return;
                    }
                }
                else if (openSshStatus == WindowsOpenSshStatus.InstalledStopped)
                {
                    AppendServerLog("▶ Iniciando servicio sshd (Servidor OpenSSH de Windows)...");
                    var (startOk, startMsg) = await WindowsOpenSshService.StartServiceAsync();
                    AppendServerLog(startOk ? $"✅ {startMsg}" : $"❌ {startMsg}");
                    UpdateOpenSshStatusUi();
                    if (!startOk) return;
                }

                // Habilitar regla de firewall para puerto 22
                ApplyServerFirewallRulesForFiles();

                _isServerRunning = true;
                lblServerStatusBadge.Text = "● SERVIDOR SFTP EN LÍNEA";
                lblServerStatusBadge.ForeColor = Color.FromArgb(34, 197, 94);
                lblServerStatusDesc.Text = "Servidor OpenSSH activo en puerto 22. Listo para transferir archivos.";
                btnServerToggle.Text = "⏹  Apagar Servidor";
                btnServerToggle.BackColor = Color.FromArgb(220, 38, 38);

                LockControlsWhileRunning(true);

                if (rbServerRemote.Checked)
                {
                    StartRemoteTunnel(WindowsOpenSshService.OpenSshPort, isRemoteFiles: true);
                }
                else
                {
                    string localIp = txtServerLocalIp.Text;
                    AppendServerLog("==================================================");
                    AppendServerLog("📁 ¡SERVIDOR DE ARCHIVOS SFTP LISTO EN RED LOCAL!");
                    AppendServerLog($"   • Host / IP Local : {localIp}");
                    AppendServerLog($"   • Puerto          : 22");
                    AppendServerLog($"   • Usuario Windows : {user}");
                    AppendServerLog($"   • Contraseña      : (Tu contraseña de Windows)");
                    AppendServerLog("💡 En la otra laptop: Abre 'Conexión Remota', ingresa estos datos y conéctate.");
                    AppendServerLog("==================================================");
                    AppendServerLog("⏳ Servidor listo. Esperando conexión desde otra laptop...");
                }
            }
            else if (isDesktopMode)
            {
                // ── Flujo C: Escritorio Remoto — inicia RemoteDesktopServerService ──────
                ApplyServerFirewallRulesForDesktop(RemoteDesktopServerService.DefaultPort);
                _remoteDesktopServer = new RemoteDesktopServerService();
                _remoteDesktopServer.OnLog += AppendServerLog;
                _remoteDesktopServer.OnClientConnected += () =>
                {
                    if (InvokeRequired) { BeginInvoke(UpdateServerStatusSummary); return; }
                    UpdateServerStatusSummary();
                    AppendServerLog("✅ Cliente de escritorio remoto conectado. Streaming de pantalla iniciado.");
                };
                _remoteDesktopServer.OnClientDisconnected += () =>
                {
                    if (InvokeRequired) { BeginInvoke(UpdateServerStatusSummary); return; }
                    UpdateServerStatusSummary();
                    AppendServerLog("🔌 Cliente de escritorio remoto desconectado. Esperando nueva conexión...");
                };
                _remoteDesktopServer.Start();

                _isServerRunning = true;
                lblServerStatusBadge.Text      = "● SERVIDOR ESCRITORIO EN LÍNEA";
                lblServerStatusBadge.ForeColor = Color.FromArgb(34, 197, 94);
                lblServerStatusDesc.Text       = $"Servidor de escritorio activo en puerto {RemoteDesktopServerService.DefaultPort}. Esperando cliente...";
                btnServerToggle.Text      = "⏹  Apagar Servidor";
                btnServerToggle.BackColor = Color.FromArgb(220, 38, 38);

                LockControlsWhileRunning(true);

                if (rbServerRemote.Checked)
                {
                    // Iniciar túnel Pinggy apuntando al puerto del servidor de escritorio
                    StartRemoteTunnel(RemoteDesktopServerService.DefaultPort, isRemoteFiles: false);
                }
                else
                {
                    string localIp = txtServerLocalIp.Text;
                    AppendServerLog("==================================================");
                    AppendServerLog("🖥️ ¡SERVIDOR DE ESCRITORIO REMOTO LISTO EN RED LOCAL!");
                    AppendServerLog($"   • Host / IP Local : {localIp}");
                    AppendServerLog($"   • Puerto          : {RemoteDesktopServerService.DefaultPort}");
                    AppendServerLog("   • (No se necesita usuario ni contraseña)");
                    AppendServerLog("💡 En la otra laptop: Abre TatoVPN → Escritorio Remoto,");
                    AppendServerLog($"   ingresa  {localIp}:{RemoteDesktopServerService.DefaultPort}  y presiona Conectar.");
                    AppendServerLog("📸 El cliente verá tu pantalla y podrá manejar mouse y teclado.");
                    AppendServerLog("==================================================");
                    AppendServerLog("⏳ Esperando conexión del cliente de escritorio remoto...");
                }
            }
            else
            {
                // Flujo B: Modo Servidor clásico para HTTP Injector / Celulares
                int sshPort = (int)numServerSshPort.Value;
                int proxyPort = (int)numServerProxyPort.Value;

                // Aplicar reglas temporales en Windows Firewall para permitir acceso desde el celular
                ApplyServerFirewallRules(sshPort, proxyPort);

                // Iniciar servidor SSH real
                _localSshServer = new LocalSshServerService();
                _localSshServer.OnLog += AppendServerLog;
                _localSshServer.OnActiveTunnelsChanged += count =>
                {
                    _sshActiveTunnels = count;
                    UpdateServerStatusSummary();
                };
                _localSshServer.Start(sshPort, user, pass);

                // Iniciar servidor Proxy (HTTP CONNECT + SOCKS5)
                _localHttpProxy = new LocalHttpProxyService();
                _localHttpProxy.OnLog += AppendServerLog;
                _localHttpProxy.OnActiveConnectionsChanged += count =>
                {
                    _proxyActiveConnections = count;
                    UpdateServerStatusSummary();
                };
                _localHttpProxy.Start(proxyPort);

                // Iniciar BadVPN UDPGW (soporte nativo UDP y DNS para HTTP Injector en puerto 7300)
                _badvpnUdpGw = new BadVpnUdpGwService();
                _badvpnUdpGw.OnLog += AppendServerLog;
                _badvpnUdpGw.Start(7300);

                _isServerRunning = true;

                lblServerStatusBadge.Text = "● SERVIDOR EN LÍNEA";
                lblServerStatusBadge.ForeColor = Color.FromArgb(34, 197, 94);
                UpdateServerStatusSummary();
                btnServerToggle.Text = "⏹  Apagar Servidor";
                btnServerToggle.BackColor = Color.FromArgb(220, 38, 38);

                LockControlsWhileRunning(true);

                if (rbServerRemote.Checked)
                {
                    StartRemoteTunnel(sshPort, isRemoteFiles: false);
                }
                else
                {
                    string localIp = txtServerLocalIp.Text;
                    AppendServerLog("==================================================");
                    AppendServerLog("📱 DATOS PARA CONFIGURAR HTTP INJECTOR EN TU CELULAR:");
                    AppendServerLog($"   • Host SSH / IP : {localIp}");
                    AppendServerLog($"   • Puerto SSH    : {sshPort}");
                    AppendServerLog($"   • Usuario       : {user}");
                    AppendServerLog($"   • Contraseña    : {pass}");
                    AppendServerLog($"   • Puerto Proxy  : {proxyPort} (Opcional para HTTP Proxy)");
                    AppendServerLog("💡 Modo recomendado en HTTP Injector: Túnel 'SSH (Directo)'");
                    AppendServerLog("==================================================");
                    AppendServerLog("⏳ Servidor listo. Esperando conexión desde tu celular...");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            AppendServerLog($"❌ Error al iniciar servidor: {ex.Message}");
            MessageBox.Show($"No se pudo encender el servidor:\n{ex.Message}",
                "Error de Modo Servidor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            StopServerMode();
        }
        finally
        {
            btnServerToggle.Enabled = true;
        }
    }

    private void StartRemoteTunnel(int sshPort, bool isRemoteFiles = false)
    {
        try
        {
            StopRemoteTunnelIfRunning();

            txtServerPublicHost.Text = "⏳ Conectando túnel público...";
            txtServerPublicHost.ForeColor = Color.FromArgb(56, 189, 248);

            _remoteTunnel = new RemoteTunnelService();
            _remoteTunnel.OnLog += AppendServerLog;
            _remoteTunnel.OnTunnelConnected += (host, port) =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => ApplyRemoteTunnelInfo(host, port, isRemoteFiles));
                    return;
                }
                ApplyRemoteTunnelInfo(host, port, isRemoteFiles);
            };
            _remoteTunnel.OnTunnelDisconnected += () =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(HandleRemoteTunnelDisconnected);
                    return;
                }
                HandleRemoteTunnelDisconnected();
            };

            _remoteTunnel.Start(sshPort, isRemoteFilesMode: isRemoteFiles);
        }
        catch (Exception ex)
        {
            AppendServerLog($"⚠️ Error al iniciar túnel remoto: {ex.Message}");
        }
    }

    private void StopRemoteTunnelIfRunning()
    {
        try
        {
            if (_remoteTunnel != null)
            {
                _remoteTunnel.Dispose();
                _remoteTunnel = null;
            }
            _remotePublicHost = null;
            _remotePublicPort = null;
        }
        catch { }
    }

    private void ApplyRemoteTunnelInfo(string host, int port, bool isRemoteFiles)
    {
        _remotePublicHost = host;
        _remotePublicPort = port;
        txtServerPublicHost.Text = $"{host}:{port}";
        txtServerPublicHost.ForeColor = Color.FromArgb(34, 197, 94);
        UpdateConnectionStringPreview();

        bool isDesktop = rbServerPurposeDesktop?.Checked ?? false;
        string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? (isRemoteFiles ? Environment.UserName : "tatouser") : txtServerUser.Text.Trim();
        string pass = txtServerPass.Text.Trim();

        if (isDesktop)
        {
            AppendServerLog("==================================================");
            AppendServerLog("🖥️ ¡TÚNEL DE ESCRITORIO REMOTO LISTO!");
            AppendServerLog("💻 DATOS PARA CONECTAR DESDE OTRA LAPTOP (TatoVPN → Escritorio Remoto):");
            AppendServerLog($"   • Host / Puerto  : {host}:{port}");
            AppendServerLog("   • (No se necesita usuario ni contraseña para conectar)");
            AppendServerLog("💡 Copia la cadena del campo 'Credencial Formato TatoVPN' y úsala en el cliente.");
            AppendServerLog("📸 El cliente verá tu pantalla y podrá manejar el mouse y teclado remotamente.");
            AppendServerLog("==================================================");
        }
        else if (isRemoteFiles)
        {
            AppendServerLog("==================================================");
            AppendServerLog("📁 ¡TÚNEL DE CONEXIÓN REMOTA (SFTP) LISTO!");
            AppendServerLog("💻 DATOS PARA CONECTAR DESDE OTRA LAPTOP (TatoVPN):");
            AppendServerLog($"   • Host Remoto    : {host}");
            AppendServerLog($"   • Puerto Remoto  : {port}");
            AppendServerLog($"   • Usuario Windows: {user}");
            AppendServerLog($"   • Contraseña     : (Contraseña de tu cuenta de Windows)");
            AppendServerLog("💡 En la otra laptop: Abre 'Conexión Remota', ingresa estos datos y conéctate.");
            AppendServerLog("==================================================");
        }
        else
        {
            AppendServerLog("==================================================");
            AppendServerLog("🌍 ¡TÚNEL DE ACCESO REMOTO LISTO!");
            AppendServerLog("📱 DATOS PARA CONFIGURAR HTTP INJECTOR EN CUALQUIER CELULAR:");
            AppendServerLog($"   • Host SSH / IP : {host}");
            AppendServerLog($"   • Puerto SSH    : {port}");
            AppendServerLog($"   • Usuario       : {user}");
            AppendServerLog($"   • Contraseña    : {pass}");
            AppendServerLog("💡 En HTTP Injector: Tipo de túnel 'SSH (Directo)'");
            AppendServerLog("🌍 ¡Funciona desde cualquier celular del mundo con datos móviles!");
            AppendServerLog("==================================================");
        }
    }

    private void HandleRemoteTunnelDisconnected()
    {
        _remotePublicHost = null;
        _remotePublicPort = null;

        if (_isServerRunning && rbServerRemote.Checked)
        {
            // El servidor sigue activo en modo remoto: informar y relanzar el túnel automáticamente.
            // RemoteTunnelService ya implementa backoff; este método solo actualiza la UI.
            txtServerPublicHost.Text = "⏳ Reconectando túnel...";
            txtServerPublicHost.ForeColor = Color.FromArgb(251, 191, 36);
            UpdateConnectionStringPreview();
            AppendServerLog("🔄 Túnel caído. Reconectando automáticamente (ver log de túnel)...");
        }
    }

    private void UpdateServerStatusSummary()
    {
        if (InvokeRequired)
        {
            BeginInvoke(UpdateServerStatusSummary);
            return;
        }

        if (_isServerRunning)
        {
            if (rbServerPurposeFiles.Checked)
            {
                lblServerStatusDesc.Text = "Servidor OpenSSH activo en puerto 22 (SFTP listo para Conexión Remota).";
            }
            else if (rbServerPurposeDesktop?.Checked ?? false)
            {
                bool clientConnected = _remoteDesktopServer?.IsRunning ?? false;
                lblServerStatusDesc.Text = clientConnected
                    ? $"Servidor de escritorio activo en puerto {RemoteDesktopServerService.DefaultPort}. Cliente conectado."
                    : $"Servidor de escritorio activo en puerto {RemoteDesktopServerService.DefaultPort}. Esperando cliente...";
            }
            else
            {
                int sshPort = (int)numServerSshPort.Value;
                int proxyPort = (int)numServerProxyPort.Value;
                lblServerStatusDesc.Text = $"SSH: {sshPort} ({_sshActiveTunnels} túneles) | Proxy: {proxyPort} ({_proxyActiveConnections} conex.)";
            }
        }
    }

    private void LockControlsWhileRunning(bool running)
    {
        rbServerPurposeFiles.Enabled   = !running;
        rbServerPurposeHttp.Enabled    = !running;
        if (rbServerPurposeDesktop != null)
            rbServerPurposeDesktop.Enabled = !running;
        rbServerLan.Enabled    = !running;
        rbServerRemote.Enabled = !running;
        txtServerUser.ReadOnly = running;
        txtServerPass.ReadOnly = running;

        if (rbServerPurposeFiles.Checked)
        {
            numServerSshPort.Enabled = false;
            numServerProxyPort.Enabled = false;
            btnOpenSshAction.Enabled = !running && WindowsOpenSshService.GetStatus() != WindowsOpenSshStatus.InstalledRunning;
        }
        else if (rbServerPurposeDesktop?.Checked ?? false)
        {
            numServerSshPort.Enabled   = false;
            numServerProxyPort.Enabled = false;
        }
        else
        {
            numServerSshPort.Enabled   = !running;
            numServerProxyPort.Enabled = !running;
        }
    }

    private void StopServerMode()
    {
        try
        {
            _isServerRunning = false;

            StopRemoteTunnelIfRunning();

            if (_localSshServer != null)
            {
                _localSshServer.Dispose();
                _localSshServer = null;
            }

            if (_localHttpProxy != null)
            {
                _localHttpProxy.Dispose();
                _localHttpProxy = null;
            }

            if (_badvpnUdpGw != null)
            {
                _badvpnUdpGw.Dispose();
                _badvpnUdpGw = null;
            }

            if (_remoteDesktopServer != null)
            {
                _remoteDesktopServer.Dispose();
                _remoteDesktopServer = null;
            }

            RemoveServerFirewallRules();

            _sshActiveTunnels = 0;
            _proxyActiveConnections = 0;

            if (lblServerStatusBadge != null && !lblServerStatusBadge.IsDisposed)
            {
                lblServerStatusBadge.Text = "● SERVIDOR DETENIDO";
                lblServerStatusBadge.ForeColor = Color.FromArgb(239, 68, 68);
            }
            if (lblServerStatusDesc != null && !lblServerStatusDesc.IsDisposed)
            {
                lblServerStatusDesc.Text = "El servidor está apagado. Presiona encender para activarlo.";
            }
            if (btnServerToggle != null && !btnServerToggle.IsDisposed)
            {
                btnServerToggle.Text = "▶  Encender Servidor";
                btnServerToggle.BackColor = Color.FromArgb(234, 88, 12);
            }

            LockControlsWhileRunning(false);

            if (txtServerPublicHost != null && !txtServerPublicHost.IsDisposed)
            {
                txtServerPublicHost.Text = rbServerRemote.Checked ? "Se activará al encender servidor" : "Disponible en modo local o túnel";
                txtServerPublicHost.ForeColor = Color.FromArgb(148, 163, 184);
            }

            AppendServerLog("🛑 Servidor detenido. Puertos liberados y Firewall restaurado.");
        }
        catch (Exception ex)
        {
            AppendServerLog($"⚠️ Error al detener: {ex.Message}");
        }
    }

    private void ApplyServerFirewallRulesForFiles()
    {
        try
        {
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleOpenSsh}\"");
            RunNetshDirect($"advfirewall firewall add rule name=\"{FwRuleOpenSsh}\" dir=in action=allow protocol=TCP localport={WindowsOpenSshService.OpenSshPort} profile=any");
            AppendServerLog($"🛡️ Firewall de Windows configurado: puerto {WindowsOpenSshService.OpenSshPort} (OpenSSH SFTP) permitido.");
        }
        catch (Exception ex)
        {
            AppendServerLog($"⚠️ Aviso Firewall: {ex.Message}");
        }
    }

    private void ApplyServerFirewallRules(int sshPort, int proxyPort)
    {
        try
        {
            RemoveServerFirewallRules();
            RunNetshDirect($"advfirewall firewall add rule name=\"{FwRuleSsh}\" dir=in action=allow protocol=TCP localport={sshPort} profile=any");
            RunNetshDirect($"advfirewall firewall add rule name=\"{FwRuleProxy}\" dir=in action=allow protocol=TCP localport={proxyPort} profile=any");
            AppendServerLog($"🛡️ Firewall de Windows configurado: puertos {sshPort} y {proxyPort} permitidos.");
        }
        catch (Exception ex)
        {
            AppendServerLog($"⚠️ Aviso Firewall: {ex.Message}");
        }
    }

    private void ApplyServerFirewallRulesForDesktop(int port)
    {
        try
        {
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleDesktop}\"");
            RunNetshDirect($"advfirewall firewall add rule name=\"{FwRuleDesktop}\" dir=in action=allow protocol=TCP localport={port} profile=any");
            AppendServerLog($"🛡️ Firewall de Windows configurado: puerto {port} (Escritorio Remoto) permitido.");
        }
        catch (Exception ex)
        {
            AppendServerLog($"⚠️ Aviso Firewall: {ex.Message}");
        }
    }

    private void RemoveServerFirewallRules()
    {
        try
        {
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleSsh}\"");
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleProxy}\"");
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleOpenSsh}\"");
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleDesktop}\"");
        }
        catch { }
    }

    private static void RunNetshDirect(string arguments)
    {
        try
        {
            using var proc = Process.Start(new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            proc?.WaitForExit(3000);
        }
        catch { }
    }

    private void RefreshModoServidorUi()
    {
        RefreshLocalIp();
        UpdatePurposeUiState();
        UpdateConnectionStringPreview();
        UpdateOpenSshStatusUi();
        AdjustModoServidorLayout();
    }
}
