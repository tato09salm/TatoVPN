using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using miVPN.Enums;
using miVPN.Models;
using miVPN.Services.Implementations;
using miVPN.Services.Interfaces;

namespace miVPN;

public partial class Form1 : Form
{
    private readonly ILoggerService _logger;
    private readonly ISshService _sshService;
    private readonly ISocksProxyService _socksProxyService;
    private readonly ITlsService _tlsService;
    private readonly IConfigService _configService;
    private readonly ITunVpnService _tunVpnService;
    private CancellationTokenSource? _connectionCts;
    private ConnectionState _currentState = ConnectionState.Disconnected;
    private ConnectionSettings? _lastSettings;
    private int _logLineCount;
    private const int MaxReconnectionAttempts = 15;
    private const int ReconnectionDelayMs = 3000;
    private bool _systemProxyActive;
    private bool _forceExit;
    private readonly System.Windows.Forms.Timer _uptimeTimer;
    private DateTime _connectionStartTime;
    private CancellationTokenSource? _watchdogCts;
    private volatile bool _isAutoReconnecting;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int InternetOptionSettingsChanged = 39;
    private const int InternetOptionRefresh = 37;
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const uint SMTO_BLOCK = 0x0001;

    private const string RegInternetSettingsPath =
        @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
    private const string RegConnectionsPath =
        @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Connections";

    private readonly IContentFilterService _contentFilterService;

    public Form1()
    {
        InitializeComponent();
        InitializeModoServidor();

        _logger = new LoggerService();
        _sshService = new SshService(_logger);
        _socksProxyService = new SocksProxyService(_logger);
        _tlsService = new TlsService(_logger);
        _configService = new ConfigService();
        
        _contentFilterService = new ContentFilterService(_logger);
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filtro_contenido.json");
        string catPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "categorias_filtro.json");
        _contentFilterService.CargarCategorias(catPath);
        _contentFilterService.CargarConfiguracion(configPath);
        
        var dnsProxyService = new DnsProxyService(_logger, _contentFilterService);
        _tunVpnService = new TunVpnService(_logger, dnsProxyService);

        InitializeFiltroContenido();

        _uptimeTimer = new System.Windows.Forms.Timer();
        _uptimeTimer.Interval = 1000;
        _uptimeTimer.Tick += (s, e) =>
        {
            if (_currentState == ConnectionState.SshAuthenticated || _currentState == ConnectionState.SocksProxyActive)
            {
                var elapsed = DateTime.Now - _connectionStartTime;
                lblInfoUptimeVal.Text = elapsed.ToString(@"hh\:mm\:ss");
            }
        };

        _logger.OnLog += HandleLog;
        _configService.OnConfigsChanged += HandleConfigsChanged;

        _sshService.OnConnectionDropped += reason =>
        {
            if (_currentState == ConnectionState.SshAuthenticated || _currentState == ConnectionState.SocksProxyActive)
            {
                _ = Task.Run(() => HandleTunnelDropAsync(_lastSettings, reason));
            }
        };

        _tunVpnService.OnTunProcessExited += reason =>
        {
            if (_currentState == ConnectionState.SocksProxyActive)
            {
                _ = Task.Run(() => HandleTunnelDropAsync(_lastSettings, reason));
            }
        };

        try { LoadAppLogo(); } catch { }
        try { RefreshConfigsGrid(); } catch { }
        try { LoadLastUsedFormValues(); } catch { }
        try { UpdateLogCountLabel(); } catch { }
        try { UpdateTunnelTypeButtonText(); } catch { }

        _systemProxyActive = true;
        try { DisableSystemProxy(); } catch { }
        try { _ = _tunVpnService.StopAsync(); } catch { }
        try
        {
            _ = _socksProxyService.StopAsync();
        }
        catch { }

        this.FormClosed += Form1_FormClosed;

        try { UpdateTrayTooltipAndIcon(); } catch { }
    }

    private Icon? _appIcon;

    private string? FindAssetPath(string fileName)
    {
        try
        {
            var searchPaths = new List<string>();
            string[] searchRoots = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            };

            foreach (var root in searchRoots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                var cur = new DirectoryInfo(root);
                for (int i = 0; i < 5 && cur != null; i++)
                {
                    searchPaths.Add(Path.Combine(cur.FullName, "Assets", fileName));
                    searchPaths.Add(Path.Combine(cur.FullName, "Assets", "bin", fileName));
                    searchPaths.Add(Path.Combine(cur.FullName, "bin", fileName));
                    searchPaths.Add(Path.Combine(cur.FullName, fileName));
                    cur = cur.Parent;
                }
            }

            foreach (var p in searchPaths.Distinct())
            {
                if (File.Exists(p)) return p;
            }
        }
        catch { }
        return null;
    }

    private void LoadAppLogo()
    {
        try
        {
            string? logoPath = FindAssetPath("TatoLogoNegro.png") ?? FindAssetPath("TatoLogoBlanco.png");
            if (logoPath != null)
            {
                using (var fs = new FileStream(logoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var img = Image.FromStream(fs))
                    {
                        var bmp = new Bitmap(img);
                        picLogo.Image = bmp;
                        picAcercaLogo.Image = bmp;
                    }
                }
            }

            picAcercaYape.Image = LoadYapeImage();

            string? icoPath = FindAssetPath("app.ico");
            if (icoPath != null)
            {
                using (var fs = new FileStream(icoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    _appIcon = new Icon(fs);
                    this.Icon = _appIcon;
                    notifyIcon1.Icon = _appIcon;
                }
            }
            else
            {
                _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (_appIcon != null)
                {
                    this.Icon = _appIcon;
                    notifyIcon1.Icon = _appIcon;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Error al cargar recursos gráficos: {ex.Message}");
        }
    }

    private static readonly System.Text.RegularExpressions.Regex HtmlTagRegex = new(
        @"<\s*(?<closing>/)?\s*(?<tag>[a-zA-Z0-9]+)(?<attrs>[^>]*)?>|(?<text>[^<]+)",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex ColorAttrRegex = new(
        @"color\s*=\s*[""']?(?<val>#[0-9a-fA-F]{3,8}|[a-zA-Z]+|rgb\([^)]+\))[""']?",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static string SanitizeLogText(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        // Strip all ANSI escape sequences (\x1b[...m, etc.)
        input = System.Text.RegularExpressions.Regex.Replace(input, @"\x1B\[[0-9;?]*[a-zA-Z]|\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~])", string.Empty);
        // Strip unprintable control characters except \t and \n
        input = System.Text.RegularExpressions.Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F]", string.Empty);
        return input;
    }

    private void AppendFormattedLog(string message)
    {
        message = SanitizeLogText(message);

        if (!message.Contains('<') || !message.Contains('>'))
        {
            rtbLogs.SelectionStart = rtbLogs.TextLength;
            rtbLogs.SelectionLength = 0;
            rtbLogs.SelectionColor = Color.FromArgb(56, 189, 248);
            rtbLogs.SelectionFont = rtbLogs.Font;
            rtbLogs.AppendText(message + Environment.NewLine);
            return;
        }

        // Clean up redundant control characters and empty tag wrappers like <h1><font></font></h1>
        string cleanedHtml = message.Replace("\r", "");
        cleanedHtml = System.Text.RegularExpressions.Regex.Replace(cleanedHtml, @"<(h[1-6]|font|span|p|b|i|u)[^>]*>\s*</\1>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleanedHtml = System.Text.RegularExpressions.Regex.Replace(cleanedHtml, @"<(h[1-6]|p)[^>]*>\s*<font[^>]*>\s*</font>\s*</\1>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        var baseFont = rtbLogs.Font;
        var defaultColor = Color.FromArgb(56, 189, 248);

        var styleStack = new Stack<(Color color, FontStyle style, float size)>();
        var currentStyle = (color: defaultColor, style: FontStyle.Regular, size: baseFont.Size);

        // Add a clean blank line before banner if needed
        if (rtbLogs.TextLength > 0 && !rtbLogs.Text.EndsWith("\n\n") && !rtbLogs.Text.EndsWith("\r\n\r\n"))
        {
            rtbLogs.AppendText(Environment.NewLine);
        }

        var matches = HtmlTagRegex.Matches(cleanedHtml);
        bool hasEmittedText = false;

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups["text"].Success)
            {
                string rawText = match.Groups["text"].Value;
                string text = SanitizeLogText(System.Net.WebUtility.HtmlDecode(rawText)).Trim();

                // Trim leading/trailing blank lines within pure HTML text chunks
                if (string.IsNullOrWhiteSpace(text)) continue;

                rtbLogs.SelectionStart = rtbLogs.TextLength;
                rtbLogs.SelectionLength = 0;
                rtbLogs.SelectionColor = currentStyle.color;
                try
                {
                    rtbLogs.SelectionFont = new Font(baseFont.FontFamily, currentStyle.size, currentStyle.style);
                }
                catch
                {
                    rtbLogs.SelectionFont = baseFont;
                }

                rtbLogs.AppendText(text);
                hasEmittedText = true;
            }
            else if (match.Groups["tag"].Success)
            {
                string tag = match.Groups["tag"].Value.ToLowerInvariant();
                bool isClosing = match.Groups["closing"].Success;
                string attrs = match.Groups["attrs"].Value;

                if (isClosing)
                {
                    if (styleStack.Count > 0)
                    {
                        currentStyle = styleStack.Pop();
                    }
                    else
                    {
                        currentStyle = (defaultColor, FontStyle.Regular, baseFont.Size);
                    }

                    if (tag is "h1" or "h2" or "h3" or "h4" or "p" or "div" or "center")
                    {
                        if (hasEmittedText && !rtbLogs.Text.EndsWith("\n"))
                        {
                            rtbLogs.AppendText(Environment.NewLine);
                        }
                    }
                }
                else
                {
                    styleStack.Push(currentStyle);

                    var newColor = currentStyle.color;
                    var newFontStyle = currentStyle.style;
                    var newSize = currentStyle.size;

                    switch (tag)
                    {
                        case "font":
                        case "span":
                            var colorMatch = ColorAttrRegex.Match(attrs);
                            if (colorMatch.Success)
                            {
                                string colorVal = colorMatch.Groups["val"].Value;
                                try
                                {
                                    if (colorVal.Equals("red", StringComparison.OrdinalIgnoreCase))
                                    {
                                        newColor = Color.FromArgb(239, 68, 68); // vibrant red
                                    }
                                    else
                                    {
                                        newColor = ColorTranslator.FromHtml(colorVal);
                                    }
                                }
                                catch
                                {
                                    newColor = Color.FromName(colorVal);
                                    if (newColor.A == 0) newColor = currentStyle.color;
                                }
                            }
                            break;

                        case "b":
                        case "strong":
                            newFontStyle |= FontStyle.Bold;
                            break;

                        case "i":
                        case "em":
                            newFontStyle |= FontStyle.Italic;
                            break;

                        case "u":
                            newFontStyle |= FontStyle.Underline;
                            break;

                        case "s":
                        case "strike":
                            newFontStyle |= FontStyle.Strikeout;
                            break;

                        case "h1":
                            newSize = baseFont.Size + 3.5f;
                            newFontStyle |= FontStyle.Bold;
                            break;

                        case "h2":
                            newSize = baseFont.Size + 2.5f;
                            newFontStyle |= FontStyle.Bold;
                            break;

                        case "h3":
                            newSize = baseFont.Size + 1.5f;
                            newFontStyle |= FontStyle.Bold;
                            break;

                        case "br":
                        case "hr":
                            if (hasEmittedText && !rtbLogs.Text.EndsWith("\n"))
                            {
                                rtbLogs.AppendText(Environment.NewLine);
                            }
                            break;
                    }

                    currentStyle = (newColor, newFontStyle, newSize);
                }
            }
        }

        // Add a clean blank line after banner
        rtbLogs.AppendText(Environment.NewLine + Environment.NewLine);
    }

    private void HandleLog(string message)
    {
        if (rtbLogs.InvokeRequired)
        {
            rtbLogs.Invoke(new Action<string>(HandleLog), message);
            return;
        }

        message = SanitizeLogText(message);
        if (string.IsNullOrWhiteSpace(message)) return;

        bool wasAtBottom = true;
        try
        {
            if (rtbLogs.TextLength > 0 && rtbLogs.ClientSize.Height > 0)
            {
                int visibleBottomChar = rtbLogs.GetCharIndexFromPosition(new Point(10, rtbLogs.ClientSize.Height - 15));
                wasAtBottom = (visibleBottomChar >= rtbLogs.TextLength - 60);
            }
        }
        catch
        {
            wasAtBottom = true;
        }

        AppendFormattedLog(message);

        if (wasAtBottom)
        {
            rtbLogs.SelectionStart = rtbLogs.TextLength;
            rtbLogs.ScrollToCaret();
        }

        _logLineCount++;
        UpdateLogCountLabel();
    }

    private void HandleConfigsChanged()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(RefreshConfigsGrid));
            return;
        }

        RefreshConfigsGrid();
    }

    private void UpdateLogCountLabel()
    {
        if (lblHistoryCount.InvokeRequired)
        {
            lblHistoryCount.Invoke(new Action(UpdateLogCountLabel));
            return;
        }

        lblHistoryCount.Text = $"Líneas: {_logLineCount}";
    }

    private void RefreshConfigsGrid()
    {
        var configs = _configService.GetAll();
        dgvConfigs.AutoGenerateColumns = false;
        dgvConfigs.DataSource = null;
        dgvConfigs.DataSource = configs.ToList();
        lblConfigsCount.Text = $"Configuraciones guardadas: {configs.Count}";
    }

    private ConnectionSettings BuildSettings()
    {
        _lastSettings = new ConnectionSettings
        {
            SshHost = txtSshHost.Text.Trim(),
            SshPort = (int)numSshPort.Value,
            Username = txtUsername.Text.Trim(),
            Password = txtPassword.Text,
            SocksLocalIp = txtSocksIp.Text.Trim(),
            SocksLocalPort = (int)numSocksPort.Value,
            UseSslTls = chkUseTls.Checked,
            TlsPort = (int)numTlsPort.Value,
            TlsServerName = string.IsNullOrWhiteSpace(txtTlsSni.Text) ? "m.facebook.com" : txtTlsSni.Text.Trim(),
            TlsVersion = _selectedTlsVersion,
            EnableTunMode = chkEnableTun.Checked
        };
        return _lastSettings;
    }

    private void ApplySettingsToForm(ConnectionSettings settings)
    {
        if (settings == null) return;

        txtSshHost.Text = settings.SshHost ?? string.Empty;
        numSshPort.Value = Math.Clamp(settings.SshPort > 0 ? settings.SshPort : 22, 1, 65535);
        numTlsPort.Value = Math.Clamp(settings.TlsPort > 0 ? settings.TlsPort : 443, 1, 65535);
        txtUsername.Text = settings.Username ?? string.Empty;
        txtPassword.Text = settings.Password ?? string.Empty;
        txtSocksIp.Text = string.IsNullOrEmpty(settings.SocksLocalIp) ? "127.0.0.1" : settings.SocksLocalIp;
        numSocksPort.Value = Math.Clamp(settings.SocksLocalPort > 0 ? settings.SocksLocalPort : 1080, 1, 65535);
        chkUseTls.Checked = settings.UseSslTls;
        txtTlsSni.Text = string.IsNullOrWhiteSpace(settings.TlsServerName) ? "m.facebook.com" : settings.TlsServerName;
        _selectedTlsVersion = string.IsNullOrWhiteSpace(settings.TlsVersion) ? "Default" : settings.TlsVersion;
        chkEnableTun.Checked = settings.EnableTunMode;

        numTlsPort.Enabled = true;
        txtTlsSni.Enabled = true;
        UpdateTunnelTypeButtonText();
    }

    private void SaveLastUsedFormValues()
    {
        try
        {
            var settings = BuildSettings();
            _configService.SaveLastUsed(settings);
        }
        catch { }
    }

    private void LoadLastUsedFormValues()
    {
        try
        {
            var last = _configService.LoadLastUsed();
            if (last != null)
            {
                ApplySettingsToForm(last);
                _lastSettings = last;
            }
        }
        catch { }
    }

    private bool ValidateInput(ConnectionSettings settings, bool requirePassword = true)
    {
        if (string.IsNullOrEmpty(settings.SshHost))
        {
            MessageBox.Show("Debe ingresar el host del servidor SSH.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtSshHost.Focus();
            return false;
        }

        if (string.IsNullOrEmpty(settings.Username))
        {
            MessageBox.Show("Debe ingresar el nombre de usuario.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtUsername.Focus();
            return false;
        }

        if (requirePassword && string.IsNullOrEmpty(settings.Password))
        {
            MessageBox.Show("Debe ingresar la contraseña.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Focus();
            return false;
        }

        if (string.IsNullOrEmpty(settings.SocksLocalIp))
        {
            MessageBox.Show("Debe ingresar la IP local para el proxy SOCKS5.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtSocksIp.Focus();
            return false;
        }

        return true;
    }

    private void UpdateState(ConnectionState state)
    {
        _currentState = state;
        string text;
        System.Drawing.Color color;

        switch (state)
        {
            case ConnectionState.Disconnected:
                text = "⚪ Desconectado";
                color = System.Drawing.Color.FromArgb(148, 163, 184);
                DisableSystemProxy();
                _uptimeTimer.Stop();
                lblInfoUptimeVal.Text = "00:00:00";
                break;
            case ConnectionState.Connecting:
                text = "🟡 Conectando...";
                color = System.Drawing.Color.FromArgb(234, 179, 8);
                break;
            case ConnectionState.Reconnecting:
                text = "🟠 Reconectando...";
                color = System.Drawing.Color.FromArgb(249, 115, 22);
                break;
            case ConnectionState.SshAuthenticated:
                text = "🔵 SSH autenticado";
                color = System.Drawing.Color.FromArgb(96, 165, 250);
                if (!_uptimeTimer.Enabled)
                {
                    _connectionStartTime = DateTime.Now;
                    _uptimeTimer.Start();
                }
                break;
            case ConnectionState.SocksProxyActive:
                bool isTunActive = _tunVpnService?.IsRunning ?? false;
                text = isTunActive ? "🟢 VPN de sistema (TUN)" : "🟢 Proxy SOCKS activo";
                color = System.Drawing.Color.FromArgb(34, 197, 94);
                if (_lastSettings != null && !_lastSettings.EnableTunMode)
                {
                    EnableSystemProxy(_lastSettings.SocksLocalIp, _lastSettings.SocksLocalPort);
                }
                if (!_uptimeTimer.Enabled)
                {
                    _connectionStartTime = DateTime.Now;
                    _uptimeTimer.Start();
                }
                break;
            case ConnectionState.Error:
                text = "🔴 Error";
                color = System.Drawing.Color.FromArgb(239, 68, 68);
                DisableSystemProxy();
                _uptimeTimer.Stop();
                break;
            default:
                text = "⚪ Desconectado";
                color = System.Drawing.Color.FromArgb(148, 163, 184);
                DisableSystemProxy();
                _uptimeTimer.Stop();
                break;
        }

        bool isConnected = state == ConnectionState.SshAuthenticated || state == ConnectionState.SocksProxyActive;
        bool isConnecting = state == ConnectionState.Connecting || state == ConnectionState.Reconnecting;
        bool isReconnecting = state == ConnectionState.Reconnecting;

        void ApplyUIState()
        {
            lblStateValue.Text = text;
            lblStateValue.ForeColor = color;

            lblSidebarDot.ForeColor = isConnected ? System.Drawing.Color.FromArgb(34, 197, 94) : (isReconnecting ? System.Drawing.Color.FromArgb(249, 115, 22) : System.Drawing.Color.FromArgb(100, 116, 139));
            lblSidebarStatusState.Text = isConnected ? "Conectado" : (isReconnecting ? "Reconectando..." : (state == ConnectionState.Connecting ? "Conectando..." : "Desconectado"));
            lblSidebarStatusState.ForeColor = isConnected ? System.Drawing.Color.FromArgb(34, 197, 94) : (isReconnecting ? System.Drawing.Color.FromArgb(249, 115, 22) : (state == ConnectionState.Connecting ? System.Drawing.Color.FromArgb(234, 179, 8) : System.Drawing.Color.FromArgb(148, 163, 184)));
            lblSidebarStatusSub.Text = isConnected ? "Túnel VPN activo" : (isReconnecting ? "Recuperando túnel caído..." : (state == ConnectionState.Connecting ? "Intento de conexión en curso..." : "No hay conexión activa"));

            // Desconectado: Candado abierto 🔓 anaranjado #EA580C; Conectado: Candado cerrado 🔒 verde #22C55E; Conectando: Candado 🔓 amarillo #EAB308
            lblRingLockIcon.Text = isConnected ? "🔒" : "🔓";
            lblRingLockIcon.ForeColor = isConnected ? System.Drawing.Color.FromArgb(34, 197, 94) : (isReconnecting ? System.Drawing.Color.FromArgb(249, 115, 22) : (state == ConnectionState.Connecting ? System.Drawing.Color.FromArgb(234, 179, 8) : System.Drawing.Color.FromArgb(234, 88, 12)));

            lblRingStatusText.Text = isConnected ? "CONECTADO" : (isReconnecting ? "RECONECTANDO..." : (state == ConnectionState.Connecting ? "CONECTANDO..." : "DESCONECTADO"));
            lblRingStatusText.ForeColor = isConnected ? System.Drawing.Color.FromArgb(34, 197, 94) : (isReconnecting ? System.Drawing.Color.FromArgb(249, 115, 22) : (state == ConnectionState.Connecting ? System.Drawing.Color.FromArgb(234, 179, 8) : System.Drawing.Color.FromArgb(234, 88, 12)));

            lblRingStatusSub.Text = isConnected
                ? ((_tunVpnService?.IsRunning ?? false) ? "VPN de sistema activa y protegida (Wintun)" : "Túnel VPN activo y protegido")
                : (isReconnecting ? "Caída detectada. Restableciendo conexión protegida..." : (state == ConnectionState.Connecting ? "Estableciendo túnel SSH seguro (puedes cancelar)..." : "Haz clic en el candado o en el botón para conectar"));

            if (isConnecting)
            {
                btnConnect.Visible = false;
                btnDisconnect.Visible = true;
                btnDisconnect.Text = "⏹  Cancelar";
                btnDisconnect.BackColor = System.Drawing.Color.FromArgb(225, 29, 72);
            }
            else if (isConnected)
            {
                btnConnect.Visible = false;
                btnDisconnect.Visible = true;
                btnDisconnect.Text = "⏹  Desconectar";
                btnDisconnect.BackColor = System.Drawing.Color.FromArgb(153, 27, 27);
            }
            else
            {
                btnConnect.Visible = true;
                btnDisconnect.Visible = false;
            }

            lblInfoTunnelVal.Text = isConnected ? "Activo" : (isReconnecting ? "Reconectando..." : (state == ConnectionState.Connecting ? "Conectando..." : "Inactivo"));
            lblInfoTunnelVal.ForeColor = isConnected ? System.Drawing.Color.FromArgb(34, 197, 94) : (isReconnecting ? System.Drawing.Color.FromArgb(249, 115, 22) : (state == ConnectionState.Connecting ? System.Drawing.Color.FromArgb(234, 179, 8) : System.Drawing.Color.FromArgb(148, 163, 184)));

            picStatusRing.Invalidate();
            UpdateQuickConfigSummaryLabel();
        }

        if (InvokeRequired)
            Invoke(ApplyUIState);
        else
            ApplyUIState();

        UpdateButtonsEnabled(state);
        UpdateTrayTooltipAndIcon();
    }

    private void UpdateQuickConfigSummaryLabel()
    {
        string host = string.IsNullOrEmpty(txtSshHost.Text) ? "Sin host" : txtSshHost.Text.Trim();
        int port = (int)numSshPort.Value;
        string user = string.IsNullOrEmpty(txtUsername.Text) ? "sin usuario" : txtUsername.Text.Trim();
        lblQuickConfigSummary.Text = $"Servidor actual: {host}:{port} ({user})";
    }

    private void UpdateButtonsEnabled(ConnectionState state)
    {
        bool isBusy = state == ConnectionState.Connecting || state == ConnectionState.Reconnecting;
        bool isConnected = state == ConnectionState.SshAuthenticated || state == ConnectionState.SocksProxyActive;

        void SetUI()
        {
            btnConnect.Enabled = !isBusy && !isConnected;
            btnDisconnect.Enabled = isConnected || isBusy;
            if (btnConnectFromConfig != null) btnConnectFromConfig.Enabled = !isBusy && !isConnected;
            btnSaveConfig.Enabled = !isBusy;

            groupSsh.Enabled = !isBusy && !isConnected;
            groupCredentials.Enabled = !isBusy && !isConnected;
            groupSocks.Enabled = !isBusy && !isConnected;
            groupTls.Enabled = !isBusy && !isConnected;
        }

        if (InvokeRequired)
            Invoke(SetUI);
        else
            SetUI();
    }

    private void UpdateTrayTooltipAndIcon()
    {
        string tip = "TatoVPN - ";

        switch (_currentState)
        {
            case ConnectionState.Connecting:
                tip += "🟡 Conectando...";
                break;
            case ConnectionState.Reconnecting:
                tip += "🟠 Reconectando...";
                break;
            case ConnectionState.SshAuthenticated:
                tip += "🔵 SSH autenticado";
                break;
            case ConnectionState.SocksProxyActive:
                tip += "🟢 Túnel activo (Proxy ON)";
                break;
            case ConnectionState.Error:
                tip += "🔴 Error";
                break;
            case ConnectionState.Disconnected:
            default:
                tip += "⚪ Desconectado";
                break;
        }

        if (_systemProxyActive) tip += " · Proxy ON";

        try
        {
            if (_appIcon != null)
            {
                notifyIcon1.Icon = _appIcon;
            }
            else if (this.Icon != null)
            {
                notifyIcon1.Icon = this.Icon;
            }
        }
        catch { }

        if (tip.Length > 63) tip = tip.Substring(0, 63);
        notifyIcon1.Text = tip;
    }

    private void EnableSystemProxy(string ip, int port)
    {
        if (_systemProxyActive) return;
        try
        {
            string proxyEntry = $"socks={ip}:{port}";
            string proxyServerFull = $"{ip}:{port}";

            using (var key = Registry.CurrentUser.OpenSubKey(RegInternetSettingsPath, true))
            {
                if (key != null)
                {
                    key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
                    key.SetValue("ProxyServer", proxyEntry, RegistryValueKind.String);
                    key.SetValue("ProxyOverride", "<local>;-localhost", RegistryValueKind.String);
                    try
                    {
                        key.SetValue("SocksProxyServer", proxyServerFull, RegistryValueKind.String);
                        key.SetValue("SocksProxyVersion", 5, RegistryValueKind.DWord);
                        key.SetValue("MigrateProxy", 1, RegistryValueKind.DWord);
                    }
                    catch { }
                    key.Flush();
                    key.Close();
                }
            }

            try
            {
                using (var connKey = Registry.CurrentUser.CreateSubKey(RegConnectionsPath, true))
                {
                    if (connKey != null)
                    {
                        byte[] defaultConnectionSettings = BuildWinHttpProxySettings(true, ip, port);
                        connKey.SetValue("DefaultConnectionSettings", defaultConnectionSettings, RegistryValueKind.Binary);
                        connKey.SetValue("SavedLegacySettings", defaultConnectionSettings, RegistryValueKind.Binary);
                        connKey.Flush();
                        connKey.Close();
                    }
                }
            }
            catch { }

            NotifyInternetSettingsChanged();

            _systemProxyActive = true;
            _logger.Log($"🌐 Proxy global de Windows ACTIVADO: socks={ip}:{port} (Todo el tráfico pasará por el túnel)");
            _logger.Log("💡 Abre https://fast.com para comprobar tu nueva IP. Si no navega, cierra y abre el navegador.");
            UpdateTrayTooltipAndIcon();

            notifyIcon1.BalloonTipTitle = "miVPN - Túnel listo";
            notifyIcon1.BalloonTipText = $"Proxy Windows activado: socks={ip}:{port}\nTodo el tráfico de la laptop pasa por el túnel.";
            notifyIcon1.ShowBalloonTip(4000);
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️  No se pudo activar el proxy global: {ex.Message}");
        }
    }

    private void DisableSystemProxy(bool force = false)
    {
        if (!_systemProxyActive && !force) return;
        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegInternetSettingsPath, true))
            {
                if (key != null)
                {
                    key.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
                    try { key.DeleteValue("ProxyServer", false); } catch { }
                    try { key.DeleteValue("ProxyOverride", false); } catch { }
                    try { key.DeleteValue("SocksProxyServer", false); } catch { }
                    try { key.DeleteValue("SocksProxyVersion", false); } catch { }
                    key.Flush();
                    key.Close();
                }
            }

            try
            {
                using (var connKey = Registry.CurrentUser.CreateSubKey(RegConnectionsPath, true))
                {
                    if (connKey != null)
                    {
                        byte[] defaultConnectionSettings = BuildWinHttpProxySettings(false, "0.0.0.0", 0);
                        connKey.SetValue("DefaultConnectionSettings", defaultConnectionSettings, RegistryValueKind.Binary);
                        connKey.SetValue("SavedLegacySettings", defaultConnectionSettings, RegistryValueKind.Binary);
                        connKey.Flush();
                        connKey.Close();
                    }
                }
            }
            catch { }

            NotifyInternetSettingsChanged();

            _systemProxyActive = false;
            _logger.Log("🌐 Proxy global de Windows DESACTIVADO (restaurada configuración normal)");
            UpdateTrayTooltipAndIcon();

            notifyIcon1.BalloonTipTitle = "miVPN - Desconectado";
            notifyIcon1.BalloonTipText = "Proxy Windows desactivado. Tu red ha vuelto a la normalidad.";
            notifyIcon1.ShowBalloonTip(3000);
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️  No se pudo desactivar el proxy global: {ex.Message}");
        }
    }

    private static void NotifyInternetSettingsChanged()
    {
        try
        {
            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
        }
        catch { }
        try
        {
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
        }
        catch { }

        try
        {
            IntPtr result;
            EnumWindows((hWnd, lParam) =>
            {
                try
                {
                    SendMessageTimeout(hWnd, WM_SETTINGCHANGE, IntPtr.Zero, IntPtr.Zero,
                        SMTO_ABORTIFHUNG | SMTO_BLOCK, 150, out result);
                }
                catch { }
                return true;
            }, IntPtr.Zero);
        }
        catch { }
    }

    private static byte[] BuildWinHttpProxySettings(bool enabled, string ip, int port)
    {
        string proxyStr = enabled ? $"socks={ip}:{port}" : string.Empty;
        string extraStr = enabled ? "<local>;-localhost" : string.Empty;

        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);

        bw.Write((uint)0);
        uint flags = enabled ? 3u : 1u;
        bw.Write(flags);

        byte[] proxyBytes = System.Text.Encoding.ASCII.GetBytes(proxyStr);
        bw.Write((uint)proxyBytes.Length);
        bw.Write(proxyBytes);

        byte[] extraBytes = System.Text.Encoding.ASCII.GetBytes(extraStr);
        bw.Write((uint)extraBytes.Length);
        bw.Write(extraBytes);

        bw.Write((uint)0);
        bw.Write((uint)0);
        bw.Write((uint)0);
        bw.Write((uint)0);

        return ms.ToArray();
    }

    private static bool VerifySystemProxyIsOn()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegInternetSettingsPath, false);
            if (key == null) return false;
            var v = key.GetValue("ProxyEnable");
            if (v == null) return false;
            int enableValue = Convert.ToInt32(v);
            return enableValue == 1;
        }
        catch
        {
            return false;
        }
    }

    private string TranslateExceptionMessage(Exception ex)
    {
        string msg = ex.Message;

        if (ex is SocketException se)
        {
            switch (se.SocketErrorCode)
            {
                case SocketError.HostNotFound:
                    return "No se pudo resolver el nombre del host (verifica DNS, VPN o que el nombre esté bien escrito).";
                case SocketError.TimedOut:
                    return "Tiempo de espera agotado (host inalcanzable, revisa IP/puerto y firewall).";
                case SocketError.ConnectionRefused:
                    return "Conexión rechazada (el servidor no está escuchando en ese puerto o el firewall lo bloquea).";
                case SocketError.NetworkUnreachable:
                    return "Red inalcanzable (revisa tu conexión a Internet o la ruta de red).";
                case SocketError.ConnectionReset:
                    return "Conexión reiniciada por el host remoto (posible reinicio del servidor SSH).";
                case SocketError.NoBufferSpaceAvailable:
                    return "Espacio de búfer o cola de sockets del sistema temporalmente saturada (WSAENOBUFS 10055).";
            }
        }

        if (msg.Contains("No such host is known") || msg.Contains("HostNotFound") || msg.Contains("host desconocido"))
        {
            return "Host desconocido: no se pudo resolver el nombre del servidor. Revisa que el Host/IP esté correctamente escrito, que tengas conexión a Internet y/o que el servidor exista.";
        }
        if (msg.Contains("Permission denied") || msg.Contains("Authentication failed"))
        {
            return "Fallo de autenticación: usuario o contraseña incorrectos.";
        }
        if (msg.Contains("refused") || msg.Contains("actively refused"))
        {
            return "Conexión rechazada: el host no acepta conexiones en ese puerto. Revisa puerto, firewall del servidor y que sshd esté corriendo.";
        }
        if (msg.Contains("timed out") || msg.Contains("timeout"))
        {
            return "Tiempo de espera agotado: el host no respondió. Revisa IP, puerto, firewall local o de red.";
        }

        return msg;
    }

    private async void btnConnect_Click(object sender, EventArgs e)
    {
        var settings = BuildSettings();
        if (!ValidateInput(settings))
            return;

        if (settings.EnableTunMode && !TunVpnService.IsAdministrator())
        {
            var dialogResult = MessageBox.Show(
                "El modo VPN de sistema (TUN/Wintun) requiere permisos de Administrador para crear la interfaz virtual de red y redirigir el tráfico de la PC.\n\n¿Deseas reiniciar TatoVPN como Administrador?",
                "Permisos de Administrador requeridos",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    string exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "--restart",
                        UseShellExecute = true,
                        Verb = "runas",
                        WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
                    };

                    Program.ReleaseMutex();
                    CleanupAndExit();

                    System.Diagnostics.Process.Start(psi);
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    _logger.Log($"⚠️ No se pudo elevar permisos: {ex.Message}");
                }
            }
            else
            {
                _logger.Log("⚠️ Conexión cancelada: Se requieren permisos de Administrador para el modo VPN TUN.");
            }
            return;
        }

        SaveLastUsedFormValues();

        _connectionCts = new CancellationTokenSource();
        UpdateState(ConnectionState.Connecting);

        string localIp = "127.0.0.1";
        try
        {
            var hostEntry = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            var ip = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ip != null) localIp = ip.ToString();
        }
        catch { }

        string osArch = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        string devName = Environment.MachineName;

        _logger.Log($"{devName} (Windows {osArch}), Win32NT");
        _logger.Log("App version: 2.0.0 Build 2026");
        _logger.Log($"IP local: {localIp}");
        _logger.Log($"Tipo de túnel {(settings.UseSslTls ? "SSL/TLS ➔ SSH" : "Direct ➔ SSH")}");
        _logger.Log("[START] Servicio Solicitado");
        _logger.Log($"Estado de la Red: CONNECTED ({localIp})");
        _logger.Log("Iniciar servicio de túnel");
        _logger.Log("Compresión de SSH Habilitada");
        _logger.Log("Tipo de túnel Probar conexión SSH");

        string errorMsg = string.Empty;
        bool connectedOk = false;
        bool isCancelled = false;

        var token = _connectionCts?.Token ?? CancellationToken.None;

        for (int attempt = 1; attempt <= MaxReconnectionAttempts; attempt++)
        {
            if (_connectionCts != null && _connectionCts.IsCancellationRequested)
            {
                isCancelled = true;
                break;
            }

            try
            {
                if (attempt > 1)
                {
                    _logger.Log($"🔄 Reintento {attempt}/{MaxReconnectionAttempts} - Esperando {ReconnectionDelayMs / 1000}s...");
                    await Task.Delay(ReconnectionDelayMs, token);
                    _logger.Log($"🔄 Reintento {attempt}/{MaxReconnectionAttempts} - Reintentando conexión...");
                }

                if (settings.UseSslTls)
                {
                    int bridgePort = await _tlsService.StartBridgeAsync(settings, token);
                    var sshSettings = new ConnectionSettings
                    {
                        SshHost = "127.0.0.1",
                        SshPort = bridgePort,
                        Username = settings.Username,
                        Password = settings.Password,
                        SocksLocalIp = settings.SocksLocalIp,
                        SocksLocalPort = settings.SocksLocalPort,
                        UseSslTls = true,
                        TlsPort = settings.TlsPort,
                        TlsServerName = settings.TlsServerName,
                        TlsVersion = settings.TlsVersion,
                        EnableTunMode = settings.EnableTunMode
                    };
                    await _sshService.ConnectAsync(sshSettings, token);
                }
                else
                {
                    await _sshService.ConnectAsync(settings, token);
                }
                UpdateState(ConnectionState.SshAuthenticated);

                try
                {
                    await _socksProxyService.StartAsync(settings, _sshService, token);
                    
                    if (settings.EnableTunMode)
                    {
                        await _tunVpnService.StartAsync(settings, token);
                    }

                    _logger.Log("Iniciando Inyección con Servicio VPN");
                    _logger.Log("[VPN] Conectado");

                    UpdateState(ConnectionState.SocksProxyActive);
                    StartTunnelWatchdog(settings);

                    if (_connectionCts != null && !settings.EnableTunMode)
                    {
                        var delayCts = CancellationTokenSource.CreateLinkedTokenSource(_connectionCts.Token);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Delay(2500, delayCts.Token);
                                if (_currentState == ConnectionState.SocksProxyActive)
                                {
                                    bool proxyOn = VerifySystemProxyIsOn();
                                    if (!proxyOn)
                                    {
                                        _logger.Log("⚠️  Detectado: El proxy no se reflejó en el sistema. Reaplicando configuración...");
                                        EnableSystemProxy(settings.SocksLocalIp, settings.SocksLocalPort);
                                    }
                                    else
                                    {
                                        _logger.Log("✅ Proxy global de Windows verificado correctamente.");
                                    }
                                }
                            }
                            catch (OperationCanceledException) { }
                            finally { delayCts.Dispose(); }
                        }, delayCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    isCancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"Error al iniciar SOCKS5: {ex.Message}");
                    UpdateState(ConnectionState.Error);
                }

                connectedOk = true;
                if (attempt > 1)
                {
                    _logger.Log($"✅ Conexión establecida en el intento {attempt}/{MaxReconnectionAttempts}");
                }
                break;
            }
            catch (OperationCanceledException)
            {
                isCancelled = true;
                break;
            }
            catch (Exception ex)
            {
                if (_connectionCts != null && _connectionCts.IsCancellationRequested)
                {
                    isCancelled = true;
                    break;
                }

                errorMsg = TranslateExceptionMessage(ex);
                await FullDisconnectAsync();

                if (attempt < MaxReconnectionAttempts)
                {
                    _logger.Log($"⚠️  Intento {attempt}/{MaxReconnectionAttempts} fallido: {errorMsg}");
                }
                else
                {
                    _logger.Log($"❌ Error (intento {attempt}/{MaxReconnectionAttempts}): {errorMsg}");
                }
            }
        }

        if (isCancelled || (_connectionCts != null && _connectionCts.IsCancellationRequested))
        {
            await FullDisconnectAsync();
            UpdateState(ConnectionState.Disconnected);
            _logger.Log("🚫 Conexión cancelada por el usuario.");
            return;
        }

        if (!connectedOk)
        {
            UpdateState(ConnectionState.Error);
            await FullDisconnectAsync();

            Show15AttemptsFailedDialog(errorMsg);

            UpdateState(ConnectionState.Disconnected);
        }
    }

    private async void btnDisconnect_Click(object sender, EventArgs e)
    {
        if (_currentState == ConnectionState.Connecting || _currentState == ConnectionState.Reconnecting)
        {
            _logger.Log("⏹️ Cancelando intento de conexión...");
            StopTunnelWatchdog();
            _isAutoReconnecting = false;
            _connectionCts?.Cancel();
            await FullDisconnectAsync();
            UpdateState(ConnectionState.Disconnected);
            _logger.Log("🚫 Conexión cancelada por el usuario.");
            return;
        }

        _logger.Log("Iniciando desconexión...");
        StopTunnelWatchdog();
        _isAutoReconnecting = false;
        _connectionCts?.Cancel();
        await FullDisconnectAsync();
        UpdateState(ConnectionState.Disconnected);
    }

    private Image? LoadGatoSadImage()
    {
        try
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string imgPath = Path.Combine(baseDir, "Assets", "Gato15intentos.png");
            if (!File.Exists(imgPath))
            {
                imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Gato15intentos.png");
            }
            if (!File.Exists(imgPath))
            {
                imgPath = Path.Combine(Directory.GetCurrentDirectory(), "Gato15intentos.png");
            }

            if (File.Exists(imgPath))
            {
                using var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var img = Image.FromStream(fs);
                return new Bitmap(img);
            }
        }
        catch { }
        return null;
    }

    private void Show15AttemptsFailedDialog(string errorDetails)
    {
        using var form = new Form();
        form.Text = "No se pudo establecer una conexión";
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new System.Drawing.Size(460, 480);
        form.BackColor = System.Drawing.Color.FromArgb(11, 15, 25);
        form.ForeColor = System.Drawing.Color.White;
        form.Font = new System.Drawing.Font("Segoe UI", 9F);

        var picCat = new PictureBox
        {
            Location = new System.Drawing.Point(20, 20),
            Size = new System.Drawing.Size(420, 250),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = System.Drawing.Color.Transparent
        };

        Image? catImg = LoadGatoSadImage();
        if (catImg != null)
        {
            picCat.Image = catImg;
        }

        var lblTitle = new Label
        {
            Text = "😿  No se pudo establecer una conexión",
            Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(239, 68, 68),
            Location = new System.Drawing.Point(20, 282),
            Size = new System.Drawing.Size(420, 30),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        var lblSub = new Label
        {
            Text = $"Se alcanzaron los {MaxReconnectionAttempts} intentos de conexión sin éxito.",
            Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(226, 232, 240),
            Location = new System.Drawing.Point(20, 314),
            Size = new System.Drawing.Size(420, 24),
            TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        };

        string msgText = string.IsNullOrWhiteSpace(errorDetails)
            ? "Revisa tu conexión a Internet o los datos del servidor SSH."
            : $"Detalle: {errorDetails}";

        var lblDetails = new Label
        {
            Text = msgText,
            Font = new System.Drawing.Font("Segoe UI", 8.5F),
            ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
            Location = new System.Drawing.Point(20, 340),
            Size = new System.Drawing.Size(420, 58),
            TextAlign = System.Drawing.ContentAlignment.TopCenter
        };

        var btnOk = new Button
        {
            Text = "Entendido",
            Size = new System.Drawing.Size(160, 40),
            Location = new System.Drawing.Point(150, 415),
            FlatStyle = FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(239, 68, 68),
            ForeColor = System.Drawing.Color.White,
            Cursor = Cursors.Hand,
            Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;

        form.AcceptButton = btnOk;
        form.Controls.AddRange(new Control[] { picCat, lblTitle, lblSub, lblDetails, btnOk });

        form.ShowDialog(this);
    }

    private async Task FullDisconnectAsync()
    {
        StopTunnelWatchdog();
        try { await _tunVpnService.StopAsync(); } catch { }
        try { await _socksProxyService.StopAsync(); } catch { }
        try { await _sshService.DisconnectAsync(); } catch { }
        try { await _tlsService.ShutdownAsync(); } catch { }
        try { await Task.Delay(400); } catch { }
    }

    private void StartTunnelWatchdog(ConnectionSettings settings)
    {
        StopTunnelWatchdog();
        _watchdogCts = new CancellationTokenSource();
        var token = _watchdogCts.Token;

        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (_currentState != ConnectionState.SocksProxyActive || _isAutoReconnecting)
                {
                    continue;
                }

                // 1. Validar conexión SSH
                if (!_sshService.IsConnected)
                {
                    _ = HandleTunnelDropAsync(settings, "Se perdió la conexión con el servidor SSH");
                    break;
                }

                // 2. En modo TUN, validar proceso tun2socks
                if (settings.EnableTunMode && !_tunVpnService.IsProcessAlive)
                {
                    _ = HandleTunnelDropAsync(settings, "El proceso del adaptador TUN (tun2socks) se detuvo");
                    break;
                }
            }
        }, token);
    }

    private void StopTunnelWatchdog()
    {
        try
        {
            _watchdogCts?.Cancel();
            _watchdogCts?.Dispose();
        }
        catch { }
        finally
        {
            _watchdogCts = null;
        }
    }

    private async Task HandleTunnelDropAsync(ConnectionSettings? settings, string reason)
    {
        if (_isAutoReconnecting || _currentState == ConnectionState.Disconnected || _currentState == ConnectionState.Connecting)
            return;

        _isAutoReconnecting = true;
        StopTunnelWatchdog();

        try
        {
            _logger.Log($"⚠️ Caída del túnel detectada: {reason}");
            _logger.Log("🛡️ Protección activa: Bloqueando tráfico y preparando reconexión automática...");

            UpdateState(ConnectionState.Reconnecting);

            if (settings == null)
            {
                settings = BuildSettings();
            }

            const int maxRetries = 5;
            bool reconnected = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                if (_currentState == ConnectionState.Disconnected)
                {
                    _logger.Log("⏹️ Reconexión cancelada por el usuario.");
                    break;
                }

                int backoffDelay = Math.Min(attempt * 2000, 8000);
                _logger.Log($"🔄 [Auto-Reconexión] Intento {attempt}/{maxRetries} en {backoffDelay / 1000}s...");

                try
                {
                    await Task.Delay(backoffDelay);
                }
                catch { break; }

                if (_currentState == ConnectionState.Disconnected)
                {
                    _logger.Log("⏹️ Reconexión cancelada por el usuario.");
                    break;
                }

                try
                {
                    // Limpieza previa segura de sockets y procesos caídos
                    try { await _tunVpnService.StopAsync(); } catch { }
                    try { await _socksProxyService.StopAsync(); } catch { }
                    try { await _sshService.DisconnectAsync(); } catch { }
                    try { await _tlsService.ShutdownAsync(); } catch { }

                    using var attemptCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    var token = attemptCts.Token;

                    if (settings.UseSslTls)
                    {
                        int bridgePort = await _tlsService.StartBridgeAsync(settings, token);
                        var sshSettings = new ConnectionSettings
                        {
                            SshHost = "127.0.0.1",
                            SshPort = bridgePort,
                            Username = settings.Username,
                            Password = settings.Password,
                            SocksLocalIp = settings.SocksLocalIp,
                            SocksLocalPort = settings.SocksLocalPort,
                            UseSslTls = true,
                            TlsPort = settings.TlsPort,
                            TlsServerName = settings.TlsServerName,
                            TlsVersion = settings.TlsVersion,
                            EnableTunMode = settings.EnableTunMode
                        };
                        await _sshService.ConnectAsync(sshSettings, token);
                    }
                    else
                    {
                        await _sshService.ConnectAsync(settings, token);
                    }

                    await _socksProxyService.StartAsync(settings, _sshService, token);

                    if (settings.EnableTunMode)
                    {
                        await _tunVpnService.StartAsync(settings, token);
                    }

                    _logger.Log("✅ [Auto-Reconexión] ¡Túnel restablecido y protegido exitosamente!");
                    reconnected = true;
                    UpdateState(ConnectionState.SocksProxyActive);
                    StartTunnelWatchdog(settings);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log($"⚠️ Intento {attempt}/{maxRetries} de reconexión fallido: {ex.Message}");
                }
            }

            if (!reconnected && _currentState != ConnectionState.Disconnected)
            {
                _logger.Log("❌ No se pudo recuperar el túnel tras los intentos automáticos.");
                _logger.Log("🛡️ Activando fail-safe: Restaurando adaptadores, DNS y red física a valores originales...");

                await FullDisconnectAsync();

                // Restauración de emergencia a prueba de fallos
                try { TunVpnService.EmergencyCleanup(); } catch { }

                UpdateState(ConnectionState.Disconnected);
                _logger.Log("✅ Red y DNS restaurados automáticamente por DHCP. Navegación normal restablecida.");
            }
        }
        finally
        {
            _isAutoReconnecting = false;
        }
    }

    private async void btnTestSsh_Click(object sender, EventArgs e)
    {
        var settings = BuildSettings();
        if (!ValidateInput(settings))
            return;

        SaveLastUsedFormValues();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        bool result = false;
        string detail = string.Empty;

        try
        {
            if (settings.UseSslTls)
            {
                int bridgePort = await _tlsService.StartBridgeAsync(settings, cts.Token);
                var testSettings = new ConnectionSettings
                {
                    SshHost = "127.0.0.1",
                    SshPort = bridgePort,
                    Username = settings.Username,
                    Password = settings.Password,
                    SocksLocalIp = settings.SocksLocalIp,
                    SocksLocalPort = settings.SocksLocalPort,
                    UseSslTls = true,
                    TlsPort = settings.TlsPort,
                    TlsServerName = settings.TlsServerName,
                    TlsVersion = settings.TlsVersion,
                    EnableTunMode = settings.EnableTunMode
                };
                result = await _sshService.TestConnectionAsync(testSettings, cts.Token);
            }
            else
            {
                result = await _sshService.TestConnectionAsync(settings, cts.Token);
            }
            detail = result ? "Prueba SSH satisfactoria" : "No se pudo completar la conexión";
        }
        catch (Exception ex)
        {
            detail = TranslateExceptionMessage(ex);
        }
        finally
        {
            if (settings.UseSslTls)
            {
                await _tlsService.ShutdownAsync();
            }
        }

        if (result)
        {
            MessageBox.Show("La conexión de prueba SSH fue exitosa.", "Prueba exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(
                $"No se pudo establecer la conexión SSH:\n\n{detail}\n\nRevisa los datos y el registro (pestaña 📋 Registro).",
                "Prueba fallida",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void btnSaveConfig_Click(object sender, EventArgs e)
    {
        var settings = BuildSettings();
        if (!ValidateInput(settings, requirePassword: false))
            return;

        using var form = new Form();
        form.Text = "Guardar configuración";
        form.StartPosition = FormStartPosition.CenterParent;
        form.FormBorderStyle = FormBorderStyle.FixedDialog;
        form.MaximizeBox = false;
        form.MinimizeBox = false;
        form.ClientSize = new System.Drawing.Size(420, 160);
        form.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
        form.Font = new System.Drawing.Font("Segoe UI", 9F);

        var lbl = new Label
        {
            Text = "Nombre para esta configuración:",
            AutoSize = true,
            Location = new System.Drawing.Point(24, 24),
            ForeColor = System.Drawing.Color.FromArgb(30, 58, 95),
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
        };

        var txt = new TextBox
        {
            Location = new System.Drawing.Point(24, 52),
            Size = new System.Drawing.Size(372, 27),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "ej: Servidor Producción, VPN Casa, etc."
        };

        var existingList = _configService.GetAll().Select(c => c.Name).ToList();
        if (existingList.Count > 0)
        {
            txt.Text = existingList.Contains("Mi configuración")
                ? $"Configuración {existingList.Count + 1}"
                : "Mi configuración";
        }
        else
        {
            txt.Text = "Mi configuración";
        }

        var btnCancel = new Button
        {
            Text = "Cancelar",
            Size = new System.Drawing.Size(120, 38),
            Location = new System.Drawing.Point(160, 98),
            FlatStyle = FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(140, 140, 145),
            ForeColor = System.Drawing.Color.White,
            Cursor = Cursors.Hand,
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Click += (s, ev) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

        var btnOk = new Button
        {
            Text = "💾 Guardar",
            Size = new System.Drawing.Size(120, 38),
            Location = new System.Drawing.Point(286, 98),
            FlatStyle = FlatStyle.Flat,
            BackColor = System.Drawing.Color.FromArgb(46, 139, 87),
            ForeColor = System.Drawing.Color.White,
            Cursor = Cursors.Hand,
            Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, ev) =>
        {
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                MessageBox.Show("Debe ingresar un nombre para la configuración.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt.Focus();
                return;
            }
            form.DialogResult = DialogResult.OK;
            form.Close();
        };

        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;
        form.Controls.AddRange(new Control[] { lbl, txt, btnCancel, btnOk });

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            string name = txt.Text.Trim();
            var existing = _configService.GetByName(name);
            if (existing != null)
            {
                var overwriteResult = MessageBox.Show(
                    $"Ya existe una configuración con el nombre '{name}'. ¿Deseas sobrescribirla?",
                    "Confirmar actualización",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (overwriteResult != DialogResult.Yes) return;
            }

            bool ok = _configService.Save(name, settings);
            if (ok)
            {
                SaveLastUsedFormValues();
                RefreshConfigsGrid();
                MessageBox.Show($"Configuración '{name}' guardada correctamente.", "Guardado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _logger.Log($"💾 Configuración guardada: '{name}' (Host: {settings.SshHost})");
            }
            else
            {
                MessageBox.Show("No se pudo guardar la configuración. Intenta con otro nombre.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void dgvConfigs_CellContentClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvConfigs.Rows.Count) return;

        var row = dgvConfigs.Rows[e.RowIndex];
        string name = (row.DataBoundItem as SavedConfiguration)?.Name
            ?? row.Cells[colCfgName.Name]?.Value?.ToString()
            ?? row.Cells[0]?.Value?.ToString()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name)) return;

        if (e.ColumnIndex == colCfgUse.Index)
        {
            UseConfiguration(name);
        }
        else if (e.ColumnIndex == colCfgDelete.Index)
        {
            DeleteConfiguration(name);
        }
    }

    private void dgvConfigs_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgvConfigs.Rows.Count) return;
        var row = dgvConfigs.Rows[e.RowIndex];
        string name = (row.DataBoundItem as SavedConfiguration)?.Name
            ?? row.Cells[colCfgName.Name]?.Value?.ToString()
            ?? row.Cells[0]?.Value?.ToString()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name)) return;

        UseConfiguration(name);
    }

    private void dgvConfigs_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0) return;

        if (e.ColumnIndex == colCfgUse.Index || e.ColumnIndex == colCfgDelete.Index)
        {
            e.PaintBackground(e.ClipBounds, true);

            bool isUse = e.ColumnIndex == colCfgUse.Index;
            Color btnColor = isUse ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
            Color borderColor = isUse ? Color.FromArgb(34, 197, 94) : Color.FromArgb(239, 68, 68);
            string text = isUse ? "⚡ Usar" : "🗑️ Eliminar";

            int padH = 6;
            int padV = 5;
            var btnRect = new Rectangle(
                e.CellBounds.X + padH,
                e.CellBounds.Y + padV,
                e.CellBounds.Width - (padH * 2),
                e.CellBounds.Height - (padV * 2));

            if (btnRect.Width > 0 && btnRect.Height > 0)
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var path = CreateRoundedRectanglePath(btnRect, 6))
                {
                    using (var brush = new SolidBrush(btnColor))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(borderColor, 1))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(text, font, textBrush, btnRect, sf);
                }
            }

            e.Handled = true;
        }
    }

    private void dgvConfigs_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex >= 0 && (e.ColumnIndex == colCfgUse.Index || e.ColumnIndex == colCfgDelete.Index))
        {
            if (dgvConfigs.Cursor != Cursors.Hand)
                dgvConfigs.Cursor = Cursors.Hand;
        }
        else
        {
            if (dgvConfigs.Cursor != Cursors.Default)
                dgvConfigs.Cursor = Cursors.Default;
        }
    }

    private void dgvConfigs_CellMouseLeave(object sender, EventArgs e)
    {
        if (dgvConfigs.Cursor != Cursors.Default)
            dgvConfigs.Cursor = Cursors.Default;
    }

    private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        if (rect.Width <= 0 || rect.Height <= 0) return path;
        int d = radius * 2;
        if (d > rect.Width) d = rect.Width;
        if (d > rect.Height) d = rect.Height;

        var arc = new Rectangle(rect.X, rect.Y, d, d);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - d;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - d;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void UseConfiguration(string name)
    {
        var applied = _configService.Apply(name, out var cfg);
        if (applied == null || cfg == null)
        {
            MessageBox.Show("No se pudo cargar la configuración seleccionada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (_currentState != ConnectionState.Disconnected)
        {
            var r = MessageBox.Show(
                "Hay una conexión activa. ¿Deseas aplicar la configuración a la pantalla de Inicio de todos modos?",
                "Conexión activa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (r != DialogResult.Yes) return;
        }

        ApplySettingsToForm(applied);
        SaveLastUsedFormValues();

        _logger.Log($"⚡ Configuración '{cfg.Name}' cargada en Inicio. Host: {applied.SshHost}:{applied.SshPort}, Usuario: {applied.Username}");

        btnNavInicio_Click(this, EventArgs.Empty);
        txtSshHost.Focus();

        MessageBox.Show(
            $"Configuración '{cfg.Name}' aplicada correctamente en Inicio.\n\n" +
            $"• Host SSH: {applied.SshHost}:{applied.SshPort}\n" +
            $"• Usuario: {applied.Username}\n" +
            $"• Puerto SOCKS: {applied.SocksLocalPort}",
            "Configuración Cargada",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void DeleteConfiguration(string name)
    {
        var confirm = MessageBox.Show(
            $"¿Estás seguro de que deseas eliminar la configuración '{name}'?",
            "Confirmar eliminación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm == DialogResult.Yes)
        {
            bool ok = _configService.Delete(name);
            if (ok)
            {
                _logger.Log($"🗑️ Configuración eliminada: '{name}'");
                RefreshConfigsGrid();
                MessageBox.Show($"La configuración '{name}' fue eliminada.", "Eliminado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No se pudo eliminar la configuración.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void chkUseTls_CheckedChanged(object sender, EventArgs e)
    {
        bool enabled = chkUseTls.Checked;
        numTlsPort.Enabled = enabled;
        txtTlsSni.Enabled = enabled;

        if (enabled)
        {
            _logger.Log("SSL/TLS habilitado (modo preparación). Se podrá encapsular el tráfico cuando se implemente completamente.");
        }
        UpdateTunnelTypeButtonText();
    }

    private void PicStatusRing_Paint(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        bool isConnected = _currentState == ConnectionState.SshAuthenticated || _currentState == ConnectionState.SocksProxyActive;
        bool isConnecting = _currentState == ConnectionState.Connecting;

        var ringColor = isConnected
            ? System.Drawing.Color.FromArgb(34, 197, 94)  // Emerald Green
            : isConnecting
                ? System.Drawing.Color.FromArgb(234, 179, 8) // Yellow
                : System.Drawing.Color.FromArgb(234, 88, 12); // Orange Glow

        int w = picStatusRing.Width;
        int h = picStatusRing.Height;
        int thickness = 8;
        int margin = thickness / 2 + 2; // 6px inset to keep pen stroke completely inside bounds
        var rect = new System.Drawing.Rectangle(margin, margin, w - margin * 2, h - margin * 2);

        using (var penBg = new System.Drawing.Pen(System.Drawing.Color.FromArgb(30, 41, 59), thickness))
        {
            e.Graphics.DrawEllipse(penBg, rect);
        }

        using (var penRing = new System.Drawing.Pen(ringColor, thickness))
        {
            e.Graphics.DrawArc(penRing, rect, -90, 360);
        }

        string lockEmoji = isConnected ? "🔒" : "🔓";
        using (var emojiFont = new System.Drawing.Font("Segoe UI Emoji", 38F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point))
        {
            var textSize = e.Graphics.MeasureString(lockEmoji, emojiFont);
            float x = (w - textSize.Width) / 2f;
            float y = (h - textSize.Height) / 2f;
            using (var brush = new System.Drawing.SolidBrush(ringColor))
            {
                e.Graphics.DrawString(lockEmoji, emojiFont, brush, x, y);
            }
        }
    }

    private void btnTogglePass_Click(object sender, EventArgs e)
    {
        txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
        btnTogglePass.Text = txtPassword.UseSystemPasswordChar ? "👁️" : "🙈";
    }

    private bool _customPayloadEnabled = false;
    private string _selectedTlsVersion = "Default";

    private void UpdateTunnelTypeButtonText()
    {
        if (btnTunnelType == null) return;
        bool isTls = chkUseTls.Checked;
        string method = isTls ? "TLS/SSL" : "Direct";
        string icon = isTls ? "🔒" : "🛡️";
        string extra = _customPayloadEnabled ? " [Payload]" : "";
        btnTunnelType.Text = $"{icon} SSH · {method}{extra}  ▼";

        if (panelSniRow != null)
        {
            panelSniRow.Visible = isTls;
            string sni = string.IsNullOrWhiteSpace(txtTlsSni.Text) ? "m.facebook.com" : txtTlsSni.Text.Trim();
            lblSniValue.Text = sni;
        }
    }

    private void btnEditSni_Click(object? sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = true;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelAcercaDe.Visible = false;
        panelModoServidor.Visible = false;
        SetNavButtonActive(null);

        txtSniHostInput.Text = string.IsNullOrWhiteSpace(txtTlsSni.Text) ? "m.facebook.com" : txtTlsSni.Text.Trim();
        cmbSniVersionInput.SelectedItem = cmbSniVersionInput.Items.Contains(_selectedTlsVersion) ? _selectedTlsVersion : "Default";
    }

    private void btnSaveSni_Click(object? sender, EventArgs e)
    {
        string host = string.IsNullOrWhiteSpace(txtSniHostInput.Text) ? "m.facebook.com" : txtSniHostInput.Text.Trim();
        txtTlsSni.Text = host;
        _selectedTlsVersion = cmbSniVersionInput.SelectedItem?.ToString() ?? "Default";
        UpdateTunnelTypeButtonText();
        SaveLastUsedFormValues();
        _logger.Log($"🔒 SNI configurado: {host} (TLS: {_selectedTlsVersion})");
        btnNavInicio_Click(this, EventArgs.Empty);
    }

    private void btnCancelSni_Click(object? sender, EventArgs e)
    {
        btnNavInicio_Click(this, EventArgs.Empty);
    }

    private void btnTunnelType_Click(object? sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelAcercaDe.Visible = false;
        panelModoServidor.Visible = false;
        panelTunnelType.Visible = true;
        SetNavButtonActive(null);

        rbTunnelSsh.Checked = true;
        rbTunnelDirect.Checked = !chkUseTls.Checked;
        rbTunnelTls.Checked = chkUseTls.Checked;
        chkCustomPayload.Checked = _customPayloadEnabled;
    }

    private void btnSaveTunnel_Click(object? sender, EventArgs e)
    {
        chkUseTls.Checked = rbTunnelTls.Checked;
        _customPayloadEnabled = chkCustomPayload.Checked;
        UpdateTunnelTypeButtonText();
        SaveLastUsedFormValues();
        _logger.Log($"⚡ Tipo de túnel configurado: SSH ({(rbTunnelTls.Checked ? "TLS/SSL" : "Directo")}){(_customPayloadEnabled ? " [Custom Payload ON]" : "")}");
        btnNavInicio_Click(this, EventArgs.Empty);
    }

    private void btnCancelTunnel_Click(object? sender, EventArgs e)
    {
        btnNavInicio_Click(this, EventArgs.Empty);
    }

    private void StatusRing_Click(object? sender, EventArgs e)
    {
        if (_currentState == ConnectionState.SshAuthenticated || _currentState == ConnectionState.SocksProxyActive)
        {
            btnDisconnect_Click(this, e);
        }
        else if (_currentState == ConnectionState.Disconnected || _currentState == ConnectionState.Error)
        {
            btnConnect_Click(this, e);
        }
    }

    private void SetNavButtonActive(Button? activeBtn)
    {
        var allNav = new[] { btnNavInicio, btnNavConfigSsh, btnNavConfigs, btnNavRegistro, btnNavModoServidor, btnNavAcerca, btnNavFiltro };
        foreach (var b in allNav)
        {
            if (b == null) continue; // Por si acaso no se inicializó aún
            bool isActive = b == activeBtn;
            b.BackColor = isActive ? System.Drawing.Color.FromArgb(234, 88, 12) : System.Drawing.Color.Transparent;
            b.ForeColor = isActive ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(148, 163, 184);
            b.Font = new System.Drawing.Font("Segoe UI", 9.5F,
                isActive ? System.Drawing.FontStyle.Bold : System.Drawing.FontStyle.Regular);
        }
        
        if (activeBtn != btnNavFiltro && panelFiltroContenido != null)
        {
            panelFiltroContenido.Visible = false;
        }
    }

    private void btnNavInicio_Click(object sender, EventArgs e)
    {
        panelInicio.Visible = true;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = false;
        SetNavButtonActive(btnNavInicio);
        UpdateQuickConfigSummaryLabel();
    }

    private void btnNavConfigSsh_Click(object sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = true;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = false;
        SetNavButtonActive(btnNavConfigSsh);
    }

    private void btnNavRegistro_Click(object sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = true;
        panelConfigs.Visible = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = false;
        SetNavButtonActive(btnNavRegistro);
    }

    private void btnNavConfigs_Click(object sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = true;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = false;
        RefreshConfigsGrid();
        SetNavButtonActive(btnNavConfigs);
    }

    private void btnNavModoServidor_Click(object? sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelAcercaDe.Visible = false;
        panelModoServidor.Visible = true;
        SetNavButtonActive(btnNavModoServidor);
        RefreshModoServidorUi();
    }

    private Image? LoadYapeImage()
    {
        try
        {
            string[] candidateFiles = new[] { "yape.jpeg", "yape.jpg", "yape.png" };
            foreach (var candidate in candidateFiles)
            {
                string? imgPath = FindAssetPath(candidate);
                if (imgPath != null)
                {
                    using var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var img = Image.FromStream(fs);
                    return new Bitmap(img);
                }
            }
        }
        catch { }
        return null;
    }

    private void btnNavAcerca_Click(object sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = true;

        if (picAcercaYape.Image == null)
        {
            picAcercaYape.Image = LoadYapeImage();
        }

        SetNavButtonActive(btnNavAcerca);
    }

    private void btnClearHistory_Click(object sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "¿Estás seguro de que quieres limpiar el registro en vivo? Esta acción no se puede deshacer.",
            "Confirmación",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (confirm == DialogResult.Yes)
        {
            rtbLogs.Clear();
            _logLineCount = 0;
            UpdateLogCountLabel();
            MessageBox.Show("Registro limpiado correctamente.", "Registro", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void Form1_Resize(object sender, EventArgs e)
    {
    }

    private void HideToTray()
    {
        this.Hide();
        this.ShowInTaskbar = false;

        notifyIcon1.BalloonTipTitle = "TatoVPN corriendo en segundo plano";
        notifyIcon1.BalloonTipText = "La app sigue activa en la bandeja. Haz doble clic o usa el menú derecho para abrir.";
        notifyIcon1.ShowBalloonTip(2500);
    }

    protected override void WndProc(ref Message m)
    {
        if (Program.WM_SHOWME != 0 && m.Msg == (int)Program.WM_SHOWME)
        {
            ShowFromTray();
        }
        base.WndProc(ref m);
    }

    private void ShowFromTray()
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(ShowFromTray));
            return;
        }

        if (!this.Visible)
        {
            this.Show();
        }
        this.ShowInTaskbar = true;
        if (this.WindowState == FormWindowState.Minimized)
        {
            this.WindowState = FormWindowState.Normal;
        }
        ShowWindow(this.Handle, SW_RESTORE);
        SetForegroundWindow(this.Handle);
        this.Activate();
        this.BringToFront();
    }

    private void notifyIcon1_DoubleClick(object sender, EventArgs e)
    {
        ShowFromTray();
    }

    private void trayMenuOpen_Click(object sender, EventArgs e)
    {
        ShowFromTray();
    }

    private async void trayMenuDisconnect_Click(object sender, EventArgs e)
    {
        ShowFromTray();
        if (_currentState != ConnectionState.Disconnected)
        {
            btnDisconnect_Click(sender, EventArgs.Empty);
        }
    }

    private void trayMenuExit_Click(object sender, EventArgs e)
    {
        _forceExit = true;
        ShowFromTray();
        this.Close();
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        CleanupAndExit();
    }

    private bool _isCleaningUp = false;

    private void CleanupAndExit()
    {
        if (_isCleaningUp) return;
        _isCleaningUp = true;

        try { StopServerMode(); } catch { }
        try { notifyIcon1.Visible = false; } catch { }
        try { SaveLastUsedFormValues(); } catch { }
        try { _connectionCts?.Cancel(); } catch { }

        try { _tunVpnService.Dispose(); } catch { }
        try { _socksProxyService.Dispose(); } catch { }
        try { _sshService.Dispose(); } catch { }
        try { _tlsService.Dispose(); } catch { }

        try { DisableSystemProxyFast(); } catch { }
        try { TunVpnService.EmergencyCleanup(); } catch { }

        try
        {
            notifyIcon1.Dispose();
        }
        catch { }

        try { Program.ReleaseMutex(); } catch { }
    }

    private void DisableSystemProxyFast()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegInternetSettingsPath, true);
            if (key != null)
            {
                key.SetValue("ProxyEnable", 0, Microsoft.Win32.RegistryValueKind.DWord);
                try { key.DeleteValue("ProxyServer", false); } catch { }
                try { key.DeleteValue("ProxyOverride", false); } catch { }
                try { key.DeleteValue("SocksProxyServer", false); } catch { }
                try { key.DeleteValue("SocksProxyVersion", false); } catch { }
                key.Flush();
            }
        }
        catch { }

        try { InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0); } catch { }
        try { InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0); } catch { }
    }

    private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
    {
        CleanupAndExit();
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        catch
        {
            Environment.Exit(0);
        }
    }
}
