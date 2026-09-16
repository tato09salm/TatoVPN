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
    private TextBox txtServerUser = null!;
    private TextBox txtServerPass = null!;
    private Button btnTogglePassVisibility = null!;
    private NumericUpDown numServerSshPort = null!;
    private NumericUpDown numServerProxyPort = null!;
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
            Text = "Convierte esta laptop en servidor SSH y Proxy real para conectar HTTP Injector desde tu celular.",
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
        UpdateConnectionStringPreview();
        AppendServerLog("ℹ️ Módulo Modo Servidor listo con soporte completo para SSH y HTTP Injector.");
    }

    private void BuildServerControlCard()
    {
        panelServerControlCard = new Panel
        {
            Location = new Point(20, 75),
            Size = new Size(400, 580),
            BackColor = Color.FromArgb(22, 32, 48),
            BorderStyle = BorderStyle.None
        };

        // Estado del Servidor
        var pnlStatusBox = new Panel
        {
            Location = new Point(16, 16),
            Size = new Size(368, 80),
            BackColor = Color.FromArgb(15, 23, 42)
        };

        lblServerStatusBadge = new Label
        {
            Text = "● SERVIDOR DETENIDO",
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            Location = new Point(14, 12),
            Size = new Size(340, 26)
        };

        lblServerStatusDesc = new Label
        {
            Text = "El servidor está apagado. Presiona encender para activarlo.",
            Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(14, 40),
            Size = new Size(340, 30)
        };

        pnlStatusBox.Controls.Add(lblServerStatusBadge);
        pnlStatusBox.Controls.Add(lblServerStatusDesc);
        panelServerControlCard.Controls.Add(pnlStatusBox);

        // Botón Encender / Apagar
        btnServerToggle = new Button
        {
            Text = "▶  Encender Servidor",
            Location = new Point(16, 105),
            Size = new Size(368, 44),
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnServerToggle.FlatAppearance.BorderSize = 0;
        btnServerToggle.Click += BtnServerToggle_Click;
        panelServerControlCard.Controls.Add(btnServerToggle);

        // Título de Sección Credenciales
        var lblCredTitle = new Label
        {
            Text = "🔑 Credenciales de la Cuenta SSH",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 165),
            Size = new Size(368, 24)
        };
        panelServerControlCard.Controls.Add(lblCredTitle);

        // Usuario SSH
        var lblUser = new Label
        {
            Text = "Usuario:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 195),
            Size = new Size(100, 20)
        };
        panelServerControlCard.Controls.Add(lblUser);

        txtServerUser = new TextBox
        {
            Text = "tatouser",
            Location = new Point(16, 218),
            Size = new Size(368, 27),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        txtServerUser.TextChanged += (s, e) => UpdateConnectionStringPreview();
        panelServerControlCard.Controls.Add(txtServerUser);

        // Contraseña SSH
        var lblPass = new Label
        {
            Text = "Contraseña:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 255),
            Size = new Size(100, 20)
        };
        panelServerControlCard.Controls.Add(lblPass);

        txtServerPass = new TextBox
        {
            Text = "tatopass123",
            Location = new Point(16, 278),
            Size = new Size(300, 27),
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
            Location = new Point(322, 277),
            Size = new Size(62, 29),
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

        // Puerto SSH & Proxy
        var lblSshPort = new Label
        {
            Text = "Puerto SSH (HTTP Injector):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 318),
            Size = new Size(170, 20)
        };
        panelServerControlCard.Controls.Add(lblSshPort);

        numServerSshPort = new NumericUpDown
        {
            Location = new Point(16, 340),
            Size = new Size(170, 27),
            Minimum = 1,
            Maximum = 65535,
            Value = 2222,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        numServerSshPort.ValueChanged += (s, e) => UpdateConnectionStringPreview();
        panelServerControlCard.Controls.Add(numServerSshPort);

        var lblProxyPort = new Label
        {
            Text = "Puerto Proxy (HTTP/SOCKS):",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(206, 318),
            Size = new Size(178, 20)
        };
        panelServerControlCard.Controls.Add(lblProxyPort);

        numServerProxyPort = new NumericUpDown
        {
            Location = new Point(206, 340),
            Size = new Size(178, 27),
            Minimum = 1,
            Maximum = 65535,
            Value = 1080,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        panelServerControlCard.Controls.Add(numServerProxyPort);

        // Modo de Red
        var lblModeTitle = new Label
        {
            Text = "🌐 Modo de Red / Alcance",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            Location = new Point(16, 385),
            Size = new Size(368, 24)
        };
        panelServerControlCard.Controls.Add(lblModeTitle);

        rbServerLan = new RadioButton
        {
            Text = "📶 Red Local (Wi-Fi / LAN / Hotspot Celular)",
            Location = new Point(16, 412),
            Size = new Size(368, 26),
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
        panelServerControlCard.Controls.Add(rbServerLan);

        var lblLanDesc = new Label
        {
            Text = "Para celulares conectados a la misma red Wi-Fi o compartiendo zona Wi-Fi a tu laptop.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(36, 438),
            Size = new Size(348, 32)
        };
        panelServerControlCard.Controls.Add(lblLanDesc);

        rbServerRemote = new RadioButton
        {
            Text = "🌍 Acceso Remoto (Túnel Inverso / Internet)",
            Location = new Point(16, 475),
            Size = new Size(368, 26),
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
                    StartRemoteTunnel((int)numServerSshPort.Value);
                }
            }
        };
        panelServerControlCard.Controls.Add(rbServerRemote);

        var lblRemoteDesc = new Label
        {
            Text = "Permite conectar desde datos móviles a través de un túnel inverso sin abrir puertos.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(100, 116, 139),
            Location = new Point(36, 501),
            Size = new Size(348, 32)
        };
        panelServerControlCard.Controls.Add(lblRemoteDesc);

        panelModoServidor.Controls.Add(panelServerControlCard);
    }

    private void BuildServerInfoCard()
    {
        panelServerInfoCard = new Panel
        {
            Location = new Point(435, 75),
            Size = new Size(400, 580),
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
                ShowNotificationTip("¡Configuración copiada! Pégala en tu celular o TatoVPN.");
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
            Size = new Size(368, 268),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.FromArgb(148, 163, 184),
            BorderStyle = BorderStyle.None,
            Font = new Font("Consolas", 8.5F),
            ReadOnly = true
        };
        panelServerInfoCard.Controls.Add(rtbServerLogs);

        panelModoServidor.Controls.Add(panelServerInfoCard);
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
                int.TryParse(parts[1], out port);
                if (port == 0) port = (int)numServerSshPort.Value;
            }
            else
            {
                host = txtServerLocalIp.Text;
                port = (int)numServerSshPort.Value;
            }
        }
        else
        {
            host = txtServerLocalIp.Text;
            port = (int)numServerSshPort.Value;
        }

        string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? "tatouser" : txtServerUser.Text.Trim();
        string pass = txtServerPass.Text.Trim();

        txtServerConnectionString.Text = $"{host}:{port}@{user}:{pass}";
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

            int sshPort = (int)numServerSshPort.Value;
            int proxyPort = (int)numServerProxyPort.Value;
            string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? "tatouser" : txtServerUser.Text.Trim();
            string pass = txtServerPass.Text.Trim();

            if (string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Por favor ingresa una contraseña para el servidor SSH.", "Contraseña requerida",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

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

            // Bloquear edición mientras corre
            txtServerUser.ReadOnly = true;
            txtServerPass.ReadOnly = true;
            numServerSshPort.Enabled = false;
            numServerProxyPort.Enabled = false;

            if (rbServerRemote.Checked)
            {
                StartRemoteTunnel(sshPort);
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
                AppendServerLog("🎮 SOPORTE JUEGOS Y VIDEOLLAMADAS (UDP via UDPGW):");
                AppendServerLog("   En HTTP Injector → SSH Settings → habilita:");
                AppendServerLog("   ✅ Enable UDP (BadVPN)");
                AppendServerLog($"   • UDPGW Host : {localIp}");
                AppendServerLog("   • UDPGW Port : 7300");
                AppendServerLog("   Esto permite Free Fire, Among Us, WhatsApp/Meet, etc.");
                AppendServerLog("==================================================");
                AppendServerLog("⏳ Servidor listo. Esperando conexión desde tu celular...");
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

    private void StartRemoteTunnel(int sshPort)
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
                    BeginInvoke(() => ApplyRemoteTunnelInfo(host, port));
                    return;
                }
                ApplyRemoteTunnelInfo(host, port);
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

            _remoteTunnel.Start(sshPort);
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

    private void ApplyRemoteTunnelInfo(string host, int port)
    {
        _remotePublicHost = host;
        _remotePublicPort = port;
        txtServerPublicHost.Text = $"{host}:{port}";
        txtServerPublicHost.ForeColor = Color.FromArgb(34, 197, 94);
        UpdateConnectionStringPreview();

        string user = string.IsNullOrWhiteSpace(txtServerUser.Text) ? "tatouser" : txtServerUser.Text.Trim();
        string pass = txtServerPass.Text.Trim();

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
            int sshPort = (int)numServerSshPort.Value;
            int proxyPort = (int)numServerProxyPort.Value;
            lblServerStatusDesc.Text = $"SSH: {sshPort} ({_sshActiveTunnels} túneles) | Proxy: {proxyPort} ({_proxyActiveConnections} conex.)";
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

            if (txtServerUser != null && !txtServerUser.IsDisposed) txtServerUser.ReadOnly = false;
            if (txtServerPass != null && !txtServerPass.IsDisposed) txtServerPass.ReadOnly = false;
            if (numServerSshPort != null && !numServerSshPort.IsDisposed) numServerSshPort.Enabled = true;
            if (numServerProxyPort != null && !numServerProxyPort.IsDisposed) numServerProxyPort.Enabled = true;

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
        UpdateConnectionStringPreview();
    }
}
