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
    private RadioButton rbServerPurposeFiles = null!;
    private RadioButton rbServerPurposeHttp = null!;

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

    // Tarjeta derecha (Datos para conectar dispositivos & Logs)
    private Panel panelServerInfoCard = null!;
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
    private LocalSshServerService? _localSshServer;
    private LocalHttpProxyService? _localHttpProxy;
    private BadVpnUdpGwService? _badvpnUdpGw;
    private RemoteTunnelService? _remoteTunnel;
    private string? _remotePublicHost;
    private int? _remotePublicPort;
    private int _sshActiveTunnels;
    private int _proxyActiveConnections;

    // Reglas de Firewall de Windows
    private const string FwRuleSsh = "TatoVPN_Server_SSH_Inbound";
    private const string FwRuleProxy = "TatoVPN_Server_Proxy_Inbound";
    private const string FwRuleOpenSsh = "TatoVPN_Server_OpenSSH_Inbound";

    private void InitializeModoServidor()
    {
        panelModoServidor.AutoScroll = true;
        panelModoServidor.BackColor = Color.FromArgb(11, 15, 25);
        panelModoServidor.Dock = DockStyle.Fill;
        panelModoServidor.Location = new Point(0, 0);
        panelModoServidor.Name = "panelModoServidor";
        panelModoServidor.Padding = new Padding(20, 10, 20, 15);
        panelModoServidor.Size = new Size(855, 700);
        panelModoServidor.TabIndex = 5;
        panelModoServidor.Visible = false;

        // Encabezado
        lblModoServidorTitle = new Label
        {
            Text = "🖥️  Modo Servidor Local (Tato Host)",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 15),
            Size = new Size(500, 32),
            AutoSize = false
        };

        lblModoServidorSub = new Label
        {
            Text = "Servidor para transferir archivos vía Conexión Remota (SFTP) o compartir internet con HTTP Injector.",
            Font = new Font("Segoe UI", 8.8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(20, 47),
            Size = new Size(815, 22),
            AutoSize = false
        };

        panelModoServidor.Controls.Add(lblModoServidorTitle);
        panelModoServidor.Controls.Add(lblModoServidorSub);

        BuildServerControlCard();
        BuildServerInfoCard();

        RefreshLocalIp();
        UpdatePurposeUiState();
        UpdateConnectionStringPreview();
        AppendServerLog("ℹ️ Módulo Modo Servidor listo con soporte para Conexión Remota (SFTP) y HTTP Injector.");
    }

    private void BuildServerControlCard()
    {
        panelServerControlCard = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(400, 595),
            BackColor = Color.FromArgb(22, 32, 48),
            BorderStyle = BorderStyle.None
        };

        // Estado del Servidor
        var pnlStatusBox = new Panel
        {
            Location = new Point(16, 14),
            Size = new Size(368, 68),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblServerStatusBadge = new Label
        {
            Text = "● SERVIDOR DETENIDO",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            Location = new Point(12, 10),
            Size = new Size(344, 24)
        };

        lblServerStatusDesc = new Label
        {
            Text = "El servidor está apagado. Presiona encender para activarlo.",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(12, 36),
            Size = new Size(344, 26)
        };

        pnlStatusBox.Controls.Add(lblServerStatusBadge);
        pnlStatusBox.Controls.Add(lblServerStatusDesc);
        panelServerControlCard.Controls.Add(pnlStatusBox);

        // Botón Encender / Apagar
        btnServerToggle = new Button
        {
            Text = "▶  Encender Servidor",
            Location = new Point(16, 88),
            Size = new Size(368, 40),
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnServerToggle.FlatAppearance.BorderSize = 0;
        btnServerToggle.Click += BtnServerToggle_Click;
        panelServerControlCard.Controls.Add(btnServerToggle);

        // ── Grupo 1: Selector de Modo / Propósito (Panel propio para que sea grupo independiente) ──
        lblPurposeTitle = new Label
        {
            Text = "🎯 Modo de Operación / Propósito:",
            Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 134),
            Size = new Size(368, 20)
        };
        panelServerControlCard.Controls.Add(lblPurposeTitle);

        // Panel contenedor del grupo de propósito — imprescindible para que WinForms
        // trate estos RadioButtons como grupo separado al de 'Modo de Red' más abajo.
        var grpPurpose = new Panel
        {
            Location = new Point(16, 156),
            Size = new Size(368, 52),
            BackColor = Color.Transparent
        };

        rbServerPurposeFiles = new RadioButton
        {
            Text = "📁 Conexión Remota (Archivos / SFTP)",
            Location = new Point(0, 0),
            Size = new Size(368, 24),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Checked = true
        };
        rbServerPurposeFiles.CheckedChanged += (s, e) => UpdatePurposeUiState();
        grpPurpose.Controls.Add(rbServerPurposeFiles);

        rbServerPurposeHttp = new RadioButton
        {
            Text = "📱 Compartir Internet (HTTP Injector / Proxy)",
            Location = new Point(0, 26),
            Size = new Size(368, 24),
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        rbServerPurposeHttp.CheckedChanged += (s, e) => UpdatePurposeUiState();
        grpPurpose.Controls.Add(rbServerPurposeHttp);

        panelServerControlCard.Controls.Add(grpPurpose);

        // Panel de Estado y Acción de OpenSSH de Windows
        panelOpenSshCard = new Panel
        {
            Location = new Point(16, 210),
            Size = new Size(368, 58),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblOpenSshStatus = new Label
        {
            Text = "● Verificando Servidor OpenSSH...",
            Location = new Point(10, 8),
            Size = new Size(240, 42),
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184)
        };
        panelOpenSshCard.Controls.Add(lblOpenSshStatus);

        btnOpenSshAction = new Button
        {
            Text = "🛡️ Instalar",
            Location = new Point(255, 12),
            Size = new Size(103, 34),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8F, FontStyle.Bold),
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
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 274),
            Size = new Size(368, 20)
        };
        panelServerControlCard.Controls.Add(lblCredTitle);

        lblUser = new Label
        {
            Text = "Usuario Windows:",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 296),
            Size = new Size(175, 18)
        };
        panelServerControlCard.Controls.Add(lblUser);

        lblPass = new Label
        {
            Text = "Contraseña Windows:",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(200, 296),
            Size = new Size(184, 18)
        };
        panelServerControlCard.Controls.Add(lblPass);

        txtServerUser = new TextBox
        {
            Text = Environment.UserName,
            Location = new Point(16, 316),
            Size = new Size(175, 27),
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
            Location = new Point(200, 316),
            Size = new Size(135, 27),
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
            Location = new Point(339, 315),
            Size = new Size(45, 29),
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

        lblPassHelp = new Label
        {
            Text = "💡 Usa el usuario y contraseña de tu cuenta de Windows en esta laptop.",
            Font = new Font("Segoe UI", 7.8F),
            ForeColor = Color.FromArgb(250, 204, 21),
            Location = new Point(16, 346),
            Size = new Size(368, 28)
        };
        panelServerControlCard.Controls.Add(lblPassHelp);

        // Puertos
        lblServerSshPort = new Label
        {
            Text = "Puerto OpenSSH (SFTP):",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 376),
            Size = new Size(175, 18)
        };
        panelServerControlCard.Controls.Add(lblServerSshPort);

        lblServerProxyPort = new Label
        {
            Text = "Puerto Proxy (HTTP/SOCKS):",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(200, 376),
            Size = new Size(184, 18)
        };
        panelServerControlCard.Controls.Add(lblServerProxyPort);

        numServerSshPort = new NumericUpDown
        {
            Location = new Point(16, 396),
            Size = new Size(175, 27),
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
            Location = new Point(200, 396),
            Size = new Size(184, 27),
            Minimum = 1,
            Maximum = 65535,
            Value = 1080,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        panelServerControlCard.Controls.Add(numServerProxyPort);

        // ── Grupo 2: Modo de Red (Panel propio — grupo independiente de Propósito) ──
        var lblModeTitle = new Label
        {
            Text = "🌐 Modo de Red / Alcance",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 432),
            Size = new Size(368, 20)
        };
        panelServerControlCard.Controls.Add(lblModeTitle);

        // Panel contenedor del grupo de red — imprescindible para que WinForms
        // trate estos RadioButtons como grupo separado al de 'Propósito' de arriba.
        var grpNetwork = new Panel
        {
            Location = new Point(16, 454),
            Size = new Size(368, 110),
            BackColor = Color.Transparent
        };

        rbServerLan = new RadioButton
        {
            Text = "📶 Red Local (Wi-Fi / LAN / Hotspot)",
            Location = new Point(0, 0),
            Size = new Size(368, 24),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
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

        var lblLanDesc = new Label
        {
            Text = "Para laptops o celulares conectados a la misma red Wi-Fi o zona compartida.",
            Font = new Font("Segoe UI", 7.8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(20, 24),
            Size = new Size(348, 26)
        };
        grpNetwork.Controls.Add(lblLanDesc);

        rbServerRemote = new RadioButton
        {
            Text = "🌍 Acceso Remoto (Túnel Inverso / Internet)",
            Location = new Point(0, 54),
            Size = new Size(368, 24),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.8F, FontStyle.Bold)
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

        var lblRemoteDesc = new Label
        {
            Text = "Permite conectar desde cualquier red o datos móviles mediante túnel inverso sin abrir puertos.",
            Font = new Font("Segoe UI", 7.8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(20, 78),
            Size = new Size(348, 30)
        };
        grpNetwork.Controls.Add(lblRemoteDesc);

        panelServerControlCard.Controls.Add(grpNetwork);

        panelModoServidor.Controls.Add(panelServerControlCard);
    }

    private void BuildServerInfoCard()
    {
        panelServerInfoCard = new Panel
        {
            Location = new Point(435, 75),
            Size = new Size(400, 595),
            BackColor = Color.FromArgb(22, 32, 48),
            BorderStyle = BorderStyle.None
        };

        var lblInfoTitle = new Label
        {
            Text = "📡 Datos para tus Dispositivos",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 16),
            Size = new Size(368, 24)
        };
        panelServerInfoCard.Controls.Add(lblInfoTitle);

        // IP Local de la Laptop
        var lblIpLocal = new Label
        {
            Text = "IP Local de tu PC (Wi-Fi / Hotspot):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 48),
            Size = new Size(250, 20)
        };
        panelServerInfoCard.Controls.Add(lblIpLocal);

        txtServerLocalIp = new TextBox
        {
            Text = "127.0.0.1",
            ReadOnly = true,
            Location = new Point(16, 70),
            Size = new Size(230, 27),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(56, 189, 248),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
        };
        panelServerInfoCard.Controls.Add(txtServerLocalIp);

        btnCopyLocalIp = new Button
        {
            Text = "Copiar",
            Location = new Point(252, 69),
            Size = new Size(70, 29),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
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
            Location = new Point(328, 69),
            Size = new Size(56, 29),
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
        var lblPublicHost = new Label
        {
            Text = "Host Público / Túnel Remoto:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 108),
            Size = new Size(250, 20)
        };
        panelServerInfoCard.Controls.Add(lblPublicHost);

        txtServerPublicHost = new TextBox
        {
            Text = "Disponible en modo local o túnel",
            ReadOnly = true,
            Location = new Point(16, 130),
            Size = new Size(280, 27),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(148, 163, 184),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 8.5F)
        };
        panelServerInfoCard.Controls.Add(txtServerPublicHost);

        btnCopyPublicHost = new Button
        {
            Text = "Copiar",
            Location = new Point(302, 129),
            Size = new Size(82, 29),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
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
        var lblConnString = new Label
        {
            Text = "Credencial Formato TatoVPN / SSH:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 168),
            Size = new Size(250, 20)
        };
        panelServerInfoCard.Controls.Add(lblConnString);

        txtServerConnectionString = new TextBox
        {
            ReadOnly = true,
            Location = new Point(16, 190),
            Size = new Size(368, 27),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(250, 204, 21),
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        panelServerInfoCard.Controls.Add(txtServerConnectionString);

        btnCopyConnectionString = new Button
        {
            Text = "📋  Copiar Configuración Completa",
            Location = new Point(16, 224),
            Size = new Size(368, 34),
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
        var lblLogs = new Label
        {
            Text = "📋 Registro en Vivo del Servidor:",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 270),
            Size = new Size(250, 20)
        };
        panelServerInfoCard.Controls.Add(lblLogs);

        btnClearServerLogs = new Button
        {
            Text = "Limpiar",
            Location = new Point(310, 266),
            Size = new Size(74, 25),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(148, 163, 184),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 7.8F),
            Cursor = Cursors.Hand
        };
        btnClearServerLogs.FlatAppearance.BorderSize = 0;
        btnClearServerLogs.Click += (s, e) => rtbServerLogs.Clear();
        panelServerInfoCard.Controls.Add(btnClearServerLogs);

        rtbServerLogs = new RichTextBox
        {
            Location = new Point(16, 296),
            Size = new Size(368, 282),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(148, 163, 184),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 8.5F),
            ReadOnly = true
        };
        panelServerInfoCard.Controls.Add(rtbServerLogs);

        panelModoServidor.Controls.Add(panelServerInfoCard);
    }

    private void UpdatePurposeUiState()
    {
        bool isFilesMode = rbServerPurposeFiles.Checked;

        panelOpenSshCard.Visible = isFilesMode;
        lblPassHelp.Visible = isFilesMode;

        if (isFilesMode)
        {
            lblCredTitle.Text = "🔑 Credenciales de Windows (OpenSSH):";
            lblUser.Text = "Usuario Windows:";
            lblPass.Text = "Contraseña Windows:";
            txtServerUser.Text = Environment.UserName;
            lblServerSshPort.Text = "Puerto OpenSSH (SFTP):";
            numServerSshPort.Value = WindowsOpenSshService.OpenSshPort;
            numServerSshPort.Enabled = false;

            lblServerProxyPort.Visible = false;
            numServerProxyPort.Visible = false;

            UpdateOpenSshStatusUi();
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

            lblServerProxyPort.Visible = true;
            numServerProxyPort.Visible = true;
            numServerProxyPort.Enabled = !_isServerRunning;
        }

        UpdateConnectionStringPreview();
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
        bool isFiles = rbServerPurposeFiles?.Checked ?? false;
        int defaultPort = isFiles ? WindowsOpenSshService.OpenSshPort : (int)numServerSshPort.Value;
        string host;
        int port;

        if (rbServerRemote.Checked)
        {
            if (!string.IsNullOrWhiteSpace(_remotePublicHost) && _remotePublicPort.HasValue)
            {
                // Túnel activo: usar host y puerto públicos reales
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
                // Túnel aún no disponible: mostrar aviso en la credencial
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

        string user = string.IsNullOrWhiteSpace(txtServerUser.Text)
            ? (isFiles ? Environment.UserName : "tatouser")
            : txtServerUser.Text.Trim();
        string pass = txtServerPass.Text.Trim();

        // Formato legible: host:puerto@usuario:contraseña
        // Si la contraseña está vacía se muestra un marcador claro para que el usuario la complete.
        string passDisplay = string.IsNullOrEmpty(pass) ? "<ingresa_contraseña>" : pass;
        txtServerConnectionString.Text = $"{host}:{port}@{user}:{passDisplay}";
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

            bool isFilesMode = rbServerPurposeFiles.Checked;
            string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? (isFilesMode ? Environment.UserName : "tatouser") : txtServerUser.Text.Trim();
            string pass = txtServerPass.Text.Trim();

            if (string.IsNullOrWhiteSpace(pass))
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

        string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? (isRemoteFiles ? Environment.UserName : "tatouser") : txtServerUser.Text.Trim();
        string pass = txtServerPass.Text.Trim();

        if (isRemoteFiles)
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
        rbServerPurposeFiles.Enabled = !running;
        rbServerPurposeHttp.Enabled = !running;
        rbServerLan.Enabled = !running;
        rbServerRemote.Enabled = !running;
        txtServerUser.ReadOnly = running;
        txtServerPass.ReadOnly = running;

        if (rbServerPurposeFiles.Checked)
        {
            numServerSshPort.Enabled = false;
            numServerProxyPort.Enabled = false;
            btnOpenSshAction.Enabled = !running && WindowsOpenSshService.GetStatus() != WindowsOpenSshStatus.InstalledRunning;
        }
        else
        {
            numServerSshPort.Enabled = !running;
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

    private void RemoveServerFirewallRules()
    {
        try
        {
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleSsh}\"");
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleProxy}\"");
            RunNetshDirect($"advfirewall firewall delete rule name=\"{FwRuleOpenSsh}\"");
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
    }
}
