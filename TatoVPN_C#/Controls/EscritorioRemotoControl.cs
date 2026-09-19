using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;

namespace miVPN.Controls;

/// <summary>
/// Canvas interactivo para el visor de escritorio remoto.
/// Es un PictureBox con soporte nativo de foco para recibir tanto eventos de mouse
/// como eventos de teclado (KeyDown/KeyUp) sin requerir paneles superpuestos.
/// </summary>
public class RemoteDesktopCanvas : PictureBox
{
    public RemoteDesktopCanvas()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = true;
    }

    protected override bool IsInputKey(Keys keyData)
    {
        // Evita que WinForms intercepte flechas, Tab, Enter, Escape, Backspace, etc.
        return true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        this.Focus();
        base.OnMouseDown(e);
    }
}

/// <summary>
/// Control de cliente para Escritorio Remoto TatoVPN.
///
/// Se conecta al RemoteDesktopServerService que corre en la laptop remota,
/// recibe frames JPEG comprimidos y los muestra en un PictureBox en tiempo real.
/// Captura los eventos de mouse y teclado del usuario sobre el área del escritorio
/// y los reenvía al servidor para reproducirlos allá.
///
/// PROTOCOLO: TatoVPN Desktop Protocol v1 (binario simple sobre TCP).
/// TODO el tráfico viaja dentro del túnel SSH cifrado (Pinggy/RemoteTunnelService).
/// </summary>
public class EscritorioRemotoControl : UserControl
{
    // ── Sub-vistas ──────────────────────────────────────────────────────
    private Panel panelBody         = null!;
    private Panel panelConexion     = null!;
    private Panel panelDesktop      = null!;

    // ── Header superior y botón de escape rápido ────────────────────────
    private TableLayoutPanel panelHeaderTable     = null!;
    private Panel            panelHeaderSep       = null!;
    private Button           btnHeaderDesconectar = null!;

    // ── Vista 1: Formulario de conexión ─────────────────────────────────
    private TableLayoutPanel gridCenter = null!;
    private Panel  panelCardConexion   = null!;
    private Panel  pnlStatusBox        = null!;
    private Label  lblStatusDot        = null!;
    private Label  lblStatusTitle      = null!;
    private Label  lblStatusState      = null!;
    private Label  lblCardConexionHost = null!;
    private Label  lblCardConexionHint = null!;
    private TextBox txtHost            = null!;
    private NumericUpDown numPort      = null!;
    private Button btnConectar         = null!;
    private Button btnPegar            = null!;

    // ── Vista 2: Escritorio activo ───────────────────────────────────────
    private Panel     panelToolbar     = null!;
    private Label     lblDesktopStatus = null!;
    private Button    btnDesconectar   = null!;
    private Button    btnFullscreen    = null!;
    private Label     lblFps           = null!;
    private TrackBar  sliderQuality    = null!;
    private Label     lblQualityVal    = null!;
    private RemoteDesktopCanvas pbPantalla = null!;
    private bool      _firstFrameRendered;

    // ── Estado TCP ───────────────────────────────────────────────────────
    private TcpClient?    _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private bool _isConnecting;
    private bool _isDisconnecting;
    private string _currentHost = "";
    private int    _currentPort = 0;

    /// <summary>
    /// Evento emitido cuando el cliente se conecta o desconecta del host remoto.
    /// (bool isConnected, string info)
    /// </summary>
    public event Action<bool, string>? ConnectionStateChanged;

    // ── Dimensiones de la pantalla remota ────────────────────────────────
    private int _remoteWidth;
    private int _remoteHeight;

    // ── Throttling de mouse (30ms = ~33 eventos/segundo máx.) ────────────
    private DateTime _lastMouseSent = DateTime.MinValue;
    private const int MouseThrottleMs = 30;

    // ── Config de captura (se envía al servidor) ─────────────────────────
    private int _requestedQuality    = 35;
    private int _requestedIntervalMs = 200;

    // ── Estadísticas en tiempo real ──────────────────────────────────────
    private int    _framesReceived  = 0;
    private DateTime _lastFpsMeasure = DateTime.Now;

    // ── Form de pantalla completa (opcional) ─────────────────────────────
    private FullscreenDesktopForm? _fullscreenForm;

    // ── Persistencia de última conexión ──────────────────────────────────
    private static readonly string LastDesktopConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "miVPN", "desktop_last_connection.json");

    // ════════════════════════════════════════════════════════════════════
    // Constructor
    // ════════════════════════════════════════════════════════════════════

    public EscritorioRemotoControl()
    {
        InitializeComponent();
        CargarUltimaConexion();
        ShowConnectionView();
    }

    // ════════════════════════════════════════════════════════════════════
    // Construcción de UI
    // ════════════════════════════════════════════════════════════════════

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Dock      = DockStyle.Fill;
        this.BackColor = Color.FromArgb(11, 15, 25);
        this.Font      = new Font("Segoe UI", 9F);

        BuildBodyPanel();
        BuildHeader();
        BuildConnectionView();
        BuildDesktopView();

        // En WinForms, el layout de docking procesa los controles en orden inverso de Controls (de mayor a menor índice).
        // panelHeaderTable (Dock=Top) debe estar atrás (SendToBack) para evaluarse primero y ocupar el tope (Y=0..H).
        // panelHeaderSep (Dock=Top) se evalúa segundo debajo de la cabecera (Y=H..H+1).
        // panelBody (Dock=Fill) debe estar al frente (BringToFront) para evaluarse al final y ocupar solo el espacio restante.
        panelHeaderTable.SendToBack();
        panelBody.BringToFront();

        CenterConnectionCard();
        this.ResumeLayout(false);
    }

    private void BuildHeader()
    {
        panelHeaderTable = new TableLayoutPanel
        {
            Dock              = DockStyle.Top,
            AutoSize          = true,
            AutoSizeMode      = AutoSizeMode.GrowAndShrink,
            ColumnCount       = 2,
            RowCount          = 2,
            Padding           = new Padding(20, 8, 20, 6),
            BackColor         = Color.FromArgb(11, 15, 25)
        };
        panelHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        panelHeaderTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelHeaderTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panelHeaderTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblHeaderTitle = new Label
        {
            Text      = "🖥️  Escritorio Remoto",
            Font      = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock      = DockStyle.Fill,
            AutoSize  = true,
            Padding   = new Padding(0, 4, 0, 2)
        };
        panelHeaderTable.Controls.Add(lblHeaderTitle, 0, 0);

        btnHeaderDesconectar = new Button
        {
            Text      = "⏹  Desconectar",
            Height    = 30,
            AutoSize  = true,
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Visible   = false,
            Anchor    = AnchorStyles.Right,
            Margin    = new Padding(8, 2, 0, 2)
        };
        btnHeaderDesconectar.FlatAppearance.BorderSize = 0;
        btnHeaderDesconectar.Click += (s, e) => Disconnect();
        panelHeaderTable.Controls.Add(btnHeaderDesconectar, 1, 0);

        var lblHeaderSub = new Label
        {
            Text      = "Controla la pantalla de una laptop remota en tiempo real a través del túnel SSH.",
            Font      = new Font("Segoe UI", 8.8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock      = DockStyle.Fill,
            AutoSize  = true,
            Padding   = new Padding(0, 0, 0, 4)
        };
        panelHeaderTable.Controls.Add(lblHeaderSub, 0, 1);
        panelHeaderTable.SetColumnSpan(lblHeaderSub, 2);

        panelHeaderSep = new Panel
        {
            Height    = 1,
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(30, 41, 59)
        };

        this.Controls.Add(panelHeaderSep);
        this.Controls.Add(panelHeaderTable);
    }

    private void BuildBodyPanel()
    {
        panelBody = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = Color.FromArgb(11, 15, 25),
            Padding    = new Padding(0),
            AutoScroll = false
        };
        this.Controls.Add(panelBody);
    }

    private void BuildConnectionView()
    {
        panelConexion = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = Color.FromArgb(11, 15, 25),
            AutoScroll = true
        };
        panelConexion.Resize += (s, e) => UpdateCardSizing();

        // ── Grilla 3x3 para centrado automático sin cálculos manuales ───
        gridCenter = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 3,
            RowCount    = 3,
            BackColor   = Color.Transparent,
            Margin      = new Padding(0),
            Padding     = new Padding(0)
        };
        gridCenter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        gridCenter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        gridCenter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        gridCenter.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        gridCenter.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        gridCenter.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        // ── Card de conexión: TableLayoutPanel vertical responsivo ──────
        var cardLayout = new TableLayoutPanel
        {
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor    = Color.FromArgb(22, 32, 48),
            Padding      = new Padding(24, 20, 24, 20),
            ColumnCount  = 1,
            Anchor       = AnchorStyles.None,
            Margin       = new Padding(12)
        };
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        cardLayout.Resize += (s, e) => AdjustConnectionCardControls();
        panelCardConexion = cardLayout;

        // Fila 0: Título de la tarjeta
        var lblCardTitle = new Label
        {
            Text      = "🖥️  Conexión de Escritorio Remoto",
            Font      = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 3)
        };
        cardLayout.Controls.Add(lblCardTitle);

        // Fila 1: Subtítulo de la tarjeta
        var lblCardSub = new Label
        {
            Text      = "Ingresa el host y puerto provistos por el Modo Servidor en la laptop remota.",
            Font      = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 14)
        };
        cardLayout.Controls.Add(lblCardSub);

        // Fila 2: Banner de Estado (TableLayoutPanel interno estructurado)
        var tableStatus = new TableLayoutPanel
        {
            Dock         = DockStyle.Top,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount  = 1,
            RowCount     = 2,
            BackColor    = Color.FromArgb(15, 23, 42),
            Padding      = new Padding(12, 10, 12, 10),
            Margin       = new Padding(0, 0, 0, 16)
        };
        tableStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableStatus.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableStatus.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var flowStatusHeader = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            Margin        = new Padding(0, 0, 0, 4)
        };

        lblStatusDot = new Label
        {
            Text      = "●",
            Font      = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            AutoSize  = true,
            Margin    = new Padding(0, 1, 6, 0)
        };

        lblStatusTitle = new Label
        {
            Text      = "ESCRITORIO DESCONECTADO",
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            AutoSize  = true,
            Margin    = new Padding(0, 1, 0, 0)
        };

        flowStatusHeader.Controls.Add(lblStatusDot);
        flowStatusHeader.Controls.Add(lblStatusTitle);
        tableStatus.Controls.Add(flowStatusHeader, 0, 0);

        lblStatusState = new Label
        {
            Text      = "Ingresa el host y puerto del túnel para conectarte.",
            Font      = new Font("Segoe UI", 8.3F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 0)
        };
        tableStatus.Controls.Add(lblStatusState, 0, 1);

        pnlStatusBox = tableStatus;
        cardLayout.Controls.Add(pnlStatusBox);

        // Fila 3: Host Label
        lblCardConexionHost = new Label
        {
            Text      = "Host Público / Túnel Remoto:",
            Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 4)
        };
        cardLayout.Controls.Add(lblCardConexionHost);

        // Fila 4: Host TextBox
        txtHost = new TextBox
        {
            Dock            = DockStyle.Top,
            Height          = 30,
            BackColor       = Color.FromArgb(15, 23, 42),
            ForeColor       = Color.FromArgb(56, 189, 248),
            BorderStyle     = BorderStyle.FixedSingle,
            Font            = new Font("Segoe UI", 9.5F),
            PlaceholderText = "ej: txsaw-190-xxx.run.pinggy-free.link",
            Margin          = new Padding(0, 0, 0, 12)
        };
        cardLayout.Controls.Add(txtHost);

        // Fila 5: Puerto del Túnel Label
        var lblPort = new Label
        {
            Text      = "Puerto del Túnel:",
            Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 4)
        };
        cardLayout.Controls.Add(lblPort);

        // Fila 6: Puerto NumericUpDown
        numPort = new NumericUpDown
        {
            Minimum     = 1,
            Maximum     = 65535,
            Value       = 5900,
            BackColor   = Color.FromArgb(15, 23, 42),
            ForeColor   = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Segoe UI", 9.5F),
            Width       = 175,
            Height      = 30,
            Margin      = new Padding(0, 0, 0, 14)
        };
        cardLayout.Controls.Add(numPort);

        // Fila 7: Hint Box informativa
        lblCardConexionHint = new Label
        {
            Text      = "💡 Obtén el host y puerto del log en Modo Servidor → Escritorio Remoto + Acceso Remoto.\n" +
                        "   O usa '📋 Copiar Configuración Completa' y pulsa el botón de abajo para autocompletar.",
            Font      = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(250, 204, 21),
            BackColor = Color.FromArgb(30, 41, 59),
            Padding   = new Padding(10, 8, 10, 8),
            AutoSize  = true,
            Dock      = DockStyle.Top,
            Margin    = new Padding(0, 0, 0, 14)
        };
        cardLayout.Controls.Add(lblCardConexionHint);

        // Fila 8: Botón Pegar desde portapapeles
        btnPegar = new Button
        {
            Text      = "📋  Pegar Datos desde Portapapeles",
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 0, 10)
        };
        btnPegar.FlatAppearance.BorderSize = 1;
        btnPegar.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
        btnPegar.Click += BtnPegar_Click;
        cardLayout.Controls.Add(btnPegar);

        // Fila 9: Botón Conectar principal
        btnConectar = new Button
        {
            Text      = "🖥️  Conectar Escritorio Remoto",
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 0, 4)
        };
        btnConectar.FlatAppearance.BorderSize = 0;
        btnConectar.Click += BtnConectar_Click;
        cardLayout.Controls.Add(btnConectar);

        gridCenter.Controls.Add(panelCardConexion, 1, 1);
        panelConexion.Controls.Add(gridCenter);
        panelBody.Controls.Add(panelConexion);
    }

    private void BuildDesktopView()
    {
        panelDesktop = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Black,
            Visible   = false
        };

        // ── Toolbar superior responsivo ─────────────────────────────────
        panelToolbar = new Panel
        {
            Dock        = DockStyle.Top,
            Height      = 44,
            MinimumSize = new Size(0, 44),
            BackColor   = Color.FromArgb(15, 23, 42),
            Padding     = new Padding(12, 6, 12, 6)
        };

        var panelToolbarLeft = new FlowLayoutPanel
        {
            Dock          = DockStyle.Left,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0)
        };

        lblDesktopStatus = new Label
        {
            Text      = "● Conectando...",
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 197, 94),
            AutoSize  = true,
            Margin    = new Padding(0, 6, 12, 0)
        };
        panelToolbarLeft.Controls.Add(lblDesktopStatus);

        var panelToolbarRight = new FlowLayoutPanel
        {
            Dock          = DockStyle.Right,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0)
        };

        lblFps = new Label
        {
            Text      = "— FPS",
            Font      = new Font("Consolas", 8.5F),
            ForeColor = Color.FromArgb(34, 197, 94),
            AutoSize  = true,
            Margin    = new Padding(0, 7, 10, 0)
        };
        panelToolbarRight.Controls.Add(lblFps);

        panelToolbarRight.Controls.Add(new Label
        {
            Text      = "Cal.:",
            Font      = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Margin    = new Padding(0, 7, 2, 0)
        });

        sliderQuality = new TrackBar
        {
            Size          = new Size(85, 28),
            Minimum       = 1,
            Maximum       = 9,
            Value         = 4,
            TickFrequency = 1,
            SmallChange   = 1,
            BackColor     = Color.FromArgb(15, 23, 42),
            Margin        = new Padding(0, 1, 2, 0)
        };
        sliderQuality.ValueChanged += SliderQuality_ValueChanged;
        panelToolbarRight.Controls.Add(sliderQuality);

        lblQualityVal = new Label
        {
            Text      = "40%",
            Font      = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Margin    = new Padding(0, 7, 12, 0)
        };
        panelToolbarRight.Controls.Add(lblQualityVal);

        btnFullscreen = new Button
        {
            Text      = "⤢ Pantalla Completa",
            Size      = new Size(160, 32),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 8, 0)
        };
        btnFullscreen.FlatAppearance.BorderSize = 0;
        btnFullscreen.Click += BtnFullscreen_Click;
        panelToolbarRight.Controls.Add(btnFullscreen);

        btnDesconectar = new Button
        {
            Text      = "⏹  Desconectar",
            Size      = new Size(135, 32),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 0, 0)
        };
        btnDesconectar.FlatAppearance.BorderSize = 0;
        btnDesconectar.Click += (s, e) => Disconnect();
        panelToolbarRight.Controls.Add(btnDesconectar);

        panelToolbar.Controls.Add(panelToolbarRight);
        panelToolbar.Controls.Add(panelToolbarLeft);
        panelDesktop.Controls.Add(panelToolbar);

        // ── PictureBox Canvas interactivo (vista de pantalla remota) ───
        pbPantalla = new RemoteDesktopCanvas
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Black,
            SizeMode  = PictureBoxSizeMode.Zoom,
            Cursor    = Cursors.Cross
        };
        pbPantalla.MouseMove  += PbPantalla_MouseMove;
        pbPantalla.MouseDown  += PbPantalla_MouseDown;
        pbPantalla.MouseUp    += PbPantalla_MouseUp;
        pbPantalla.MouseWheel += PbPantalla_MouseWheel;
        pbPantalla.KeyDown    += PbPantalla_KeyDown;
        pbPantalla.KeyUp      += PbPantalla_KeyUp;
        panelDesktop.Controls.Add(pbPantalla);

        // En WinForms, el layout de docking procesa los controles en orden inverso de Controls.
        // panelToolbar (Dock=Top) debe estar atrás (SendToBack) para evaluarse primero y reservar sus 44px arriba.
        // pbPantalla (Dock=Fill) debe estar al frente (BringToFront) para evaluarse al final y ocupar solo el espacio restante.
        panelToolbar.SendToBack();
        pbPantalla.BringToFront();

        panelBody.Controls.Add(panelDesktop);
    }

    // ════════════════════════════════════════════════════════════════════
    // Lógica de conexión
    // ════════════════════════════════════════════════════════════════════

    private async void BtnConectar_Click(object? sender, EventArgs e)
    {
        if (_isConnecting) return;
        string host = txtHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            SetStatus("⚠️ Ingresa el host del túnel.", Color.FromArgb(251, 191, 36),
                      "El campo 'Host Público' no puede estar vacío.");
            return;
        }
        await ConnectAsync(host, (int)numPort.Value);
    }

    private async Task ConnectAsync(string host, int port)
    {
        _isConnecting = true;
        _isDisconnecting = false;
        _currentHost = host;
        _currentPort = port;
        _cts          = new CancellationTokenSource();
        var ct        = _cts.Token;
        _firstFrameRendered = false;

        try
        {
            Debug.WriteLine($"[EscritorioRemoto] Conectando a {host}:{port}...");
            SetStatus("⏳ CONECTANDO...", Color.FromArgb(251, 191, 36), $"Conectando a {host}:{port}...");
            btnConectar.Enabled = false;

            _tcpClient = new TcpClient { NoDelay = true };
            await _tcpClient.ConnectAsync(host, port, ct);
            _stream       = _tcpClient.GetStream();
            _isConnected  = true;
            _isConnecting = false;

            Debug.WriteLine($"[EscritorioRemoto] ✅ Conexión TCP establecida con éxito hacia {host}:{port}!");
            GuardarUltimaConexion(host, port);
            ShowDesktopView();
            ConnectionStateChanged?.Invoke(true, $"{host}:{port}");

            _ = Task.Run(() => ReceiveLoopAsync(ct), ct);
            _ = Task.Run(() => KeepaliveLoopAsync(ct), ct);
            await SendConfigAsync(ct);
        }
        catch (OperationCanceledException) { _isConnecting = false; }
        catch (Exception ex)
        {
            _isConnecting    = false;
            _isConnected     = false;
            btnConectar.Enabled = true;
            Debug.WriteLine($"[EscritorioRemoto] ❌ Error conectando a {host}:{port}: {ex.Message}");
            SetStatus("❌ ERROR DE CONEXIÓN", Color.FromArgb(239, 68, 68), ex.Message);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Bucle de recepción de frames del servidor
    // ════════════════════════════════════════════════════════════════════

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buf = new byte[8];
        bool firstFrameLogged = false;
        try
        {
            Debug.WriteLine("[EscritorioRemoto] 🔄 ReceiveLoopAsync iniciado.");
            while (!ct.IsCancellationRequested && _isConnected)
            {
                // Leer tipo (1 byte)
                await ReadExactAsync(buf, 0, 1, ct);

                switch (buf[0])
                {
                    // ── Info de pantalla remota ──────────────────────
                    case 0xF0:
                        await ReadExactAsync(buf, 0, 4, ct);
                        _remoteWidth  = buf[0] | (buf[1] << 8);
                        _remoteHeight = buf[2] | (buf[3] << 8);
                        Debug.WriteLine($"[EscritorioRemoto] 🤝 Handshake 0xF0 recibido: Resolución remota = {_remoteWidth}×{_remoteHeight} px");
                        SafeInvoke(() =>
                        {
                            lblDesktopStatus.Text      = $"● Conectado a {_currentHost}:{_currentPort} ({_remoteWidth}×{_remoteHeight})";
                            lblDesktopStatus.ForeColor = Color.FromArgb(34, 197, 94);
                            lblStatusDot.ForeColor     = Color.FromArgb(34, 197, 94);
                        });
                        break;

                    // ── Frame JPEG ───────────────────────────────────
                    case 0x01:
                        await ReadExactAsync(buf, 0, 4, ct);
                        int len = buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24);
                        if (len <= 0 || len > 25 * 1024 * 1024)
                            throw new InvalidDataException($"Longitud de frame inválida: {len}");

                        var frameData = new byte[len];
                        await ReadExactAsync(frameData, 0, len, ct);

                        if (!firstFrameLogged)
                        {
                            firstFrameLogged = true;
                            Debug.WriteLine($"[EscritorioRemoto] 📸 Primer frame 0x01 recibido ({len:N0} bytes).");
                        }

                        DisplayFrame(frameData);
                        break;

                    // ── Keepalive ────────────────────────────────────
                    case 0xFF:
                        break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Debug.WriteLine($"[EscritorioRemoto] ⚠️ Excepción en ReceiveLoopAsync: {ex.Message}");
            SafeInvoke(() => HandleDisconnection($"Sesión interrumpida: {ex.Message}"));
        }
    }

    private void DisplayFrame(byte[] jpegData)
    {
        try
        {
            using var ms = new MemoryStream(jpegData);
            using var temp = Image.FromStream(ms);
            // new Bitmap(temp) crea una copia profunda GDI+ en memoria desacoplada del MemoryStream
            var newImage = new Bitmap(temp);

            SafeInvoke(() =>
            {
                try
                {
                    var old = pbPantalla.Image;
                    pbPantalla.Image = newImage;
                    old?.Dispose();
                    pbPantalla.Invalidate();

                    if (!_firstFrameRendered)
                    {
                        _firstFrameRendered = true;
                        Debug.WriteLine($"[EscritorioRemoto] 🖼️ Primer frame decodificado y renderizado: {newImage.Width}×{newImage.Height} px!");
                    }

                    // Actualizar fullscreen si está abierto
                    _fullscreenForm?.UpdateFrame(newImage);

                    // Calcular FPS cada 2 segundos
                    _framesReceived++;
                    double secs = (DateTime.Now - _lastFpsMeasure).TotalSeconds;
                    if (secs >= 2.0)
                    {
                        lblFps.Text   = $"{_framesReceived / secs:F1} FPS";
                        _framesReceived = 0;
                        _lastFpsMeasure = DateTime.Now;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[EscritorioRemoto] Error en SafeInvoke DisplayFrame: {ex.Message}");
                    newImage.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EscritorioRemoto] Error decodificando frame JPEG ({jpegData?.Length ?? 0} bytes): {ex.Message}");
        }
    }

    private async Task KeepaliveLoopAsync(CancellationToken ct)
    {
        var ping = new byte[] { 0xFF };
        while (!ct.IsCancellationRequested && _isConnected)
        {
            try
            {
                await Task.Delay(10_000, ct);
                if (_stream != null && _isConnected)
                    await _stream.WriteAsync(ping, ct);
            }
            catch { break; }
        }
    }

    private async Task SendConfigAsync(CancellationToken ct)
    {
        try
        {
            if (_stream == null) return;
            // [0x30][quality:1][interval_ms:2LE]
            var msg = new byte[4];
            msg[0] = 0x30;
            msg[1] = (byte)_requestedQuality;
            msg[2] = (byte)(_requestedIntervalMs & 0xFF);
            msg[3] = (byte)((_requestedIntervalMs >> 8) & 0xFF);
            await _stream.WriteAsync(msg, ct);
        }
        catch { }
    }

    private async Task ReadExactAsync(byte[] buf, int offset, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int r = await _stream!.ReadAsync(buf.AsMemory(offset + total, count - total), ct);
            if (r == 0) throw new EndOfStreamException("El servidor cerró la conexión.");
            total += r;
        }
    }

    public void Disconnect()
    {
        if (_isDisconnecting) return;
        _isDisconnecting = true;
        if (btnDesconectar != null) btnDesconectar.Enabled = false;
        if (btnHeaderDesconectar != null) btnHeaderDesconectar.Enabled = false;

        HandleDisconnection("Desconectado por el usuario.");
    }

    private void HandleDisconnection(string reason)
    {
        _isConnected = false;
        try { _cts?.Cancel(); } catch { }
        try { _stream?.Close(); } catch { }
        try { _tcpClient?.Close(); } catch { }
        try { _stream?.Dispose(); } catch { }
        try { _tcpClient?.Dispose(); } catch { }
        _tcpClient = null;
        _stream    = null;

        if (!this.IsHandleCreated) return;
        SafeInvoke(() =>
        {
            _isDisconnecting = false;
            if (btnDesconectar != null) btnDesconectar.Enabled = true;
            if (btnHeaderDesconectar != null)
            {
                btnHeaderDesconectar.Enabled = true;
                btnHeaderDesconectar.Visible = false;
            }

            var old = pbPantalla?.Image;
            if (pbPantalla != null) pbPantalla.Image = null;
            old?.Dispose();

            if (_fullscreenForm != null && !_fullscreenForm.IsDisposed)
            {
                _fullscreenForm.Close();
                _fullscreenForm = null;
            }

            ShowConnectionView();
            SetStatus("ESCRITORIO DESCONECTADO", Color.FromArgb(239, 68, 68), reason);
            if (btnConectar != null) btnConectar.Enabled = true;

            ConnectionStateChanged?.Invoke(false, reason);
        });
    }

    // ════════════════════════════════════════════════════════════════════
    // Eventos de mouse sobre el PictureBox
    // ════════════════════════════════════════════════════════════════════

    private async void PbPantalla_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isConnected || _stream == null) return;
        if ((DateTime.Now - _lastMouseSent).TotalMilliseconds < MouseThrottleMs) return;
        _lastMouseSent = DateTime.Now;

        var remote = TranslateToRemote(e.Location);
        if (remote.IsEmpty) return;

        try
        {
            // [0x10][x:2LE][y:2LE]
            var msg = new byte[5];
            msg[0] = 0x10;
            msg[1] = (byte)(remote.X & 0xFF);
            msg[2] = (byte)((remote.X >> 8) & 0xFF);
            msg[3] = (byte)(remote.Y & 0xFF);
            msg[4] = (byte)((remote.Y >> 8) & 0xFF);
            await _stream.WriteAsync(msg);
        }
        catch { }
    }

    private async void PbPantalla_MouseDown(object? sender, MouseEventArgs e)
    {
        if (!_isConnected || _stream == null) return;
        pbPantalla.Focus();

        var remote = TranslateToRemote(e.Location);
        if (remote.IsEmpty) return;

        byte btn = e.Button switch
        {
            MouseButtons.Left   => 0,
            MouseButtons.Right  => 1,
            MouseButtons.Middle => 2,
            _ => 255
        };
        if (btn == 255) return;

        try
        {
            // Primero mover, luego presionar botón
            var move = new byte[5] { 0x10,
                (byte)(remote.X & 0xFF), (byte)((remote.X >> 8) & 0xFF),
                (byte)(remote.Y & 0xFF), (byte)((remote.Y >> 8) & 0xFF) };
            await _stream.WriteAsync(move);
            // [0x12][btn:1]
            await _stream.WriteAsync(new byte[] { 0x12, btn });
        }
        catch { }
    }

    private async void PbPantalla_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isConnected || _stream == null) return;

        byte btn = e.Button switch
        {
            MouseButtons.Left   => 0,
            MouseButtons.Right  => 1,
            MouseButtons.Middle => 2,
            _ => 255
        };
        if (btn == 255) return;

        try
        {
            // [0x13][btn:1]
            await _stream.WriteAsync(new byte[] { 0x13, btn });
        }
        catch { }
    }

    private async void PbPantalla_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (!_isConnected || _stream == null) return;
        short delta = (short)(e.Delta / 120);
        try
        {
            // [0x11][delta:2LE signed]
            var msg = new byte[3];
            msg[0] = 0x11;
            msg[1] = (byte)(delta & 0xFF);
            msg[2] = (byte)((delta >> 8) & 0xFF);
            await _stream.WriteAsync(msg);
        }
        catch { }
    }

    // ════════════════════════════════════════════════════════════════════
    // Eventos de teclado
    // ════════════════════════════════════════════════════════════════════

    private async void PbPantalla_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isConnected || _stream == null) return;

        // Atajo local seguro de desconexión rápida (no se envía al host)
        if (e.Control && e.Alt && e.Shift && e.KeyCode == Keys.D)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            Disconnect();
            return;
        }

        e.Handled         = true;
        e.SuppressKeyPress = true;

        ushort vk = (ushort)e.KeyCode;
        try
        {
            // [0x20][vk:2LE]
            await _stream.WriteAsync(new byte[] { 0x20, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EscritorioRemoto] Error enviando KeyDown {vk}: {ex.Message}");
        }
    }

    private async void PbPantalla_KeyUp(object? sender, KeyEventArgs e)
    {
        if (!_isConnected || _stream == null) return;

        if (e.Control && e.Alt && e.Shift && e.KeyCode == Keys.D)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;

        ushort vk = (ushort)e.KeyCode;
        try
        {
            // [0x21][vk:2LE]
            await _stream.WriteAsync(new byte[] { 0x21, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EscritorioRemoto] Error enviando KeyUp {vk}: {ex.Message}");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Traducción de coordenadas PictureBox → pantalla remota
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Traduce un punto en coordenadas del PictureBox (modo Zoom) al espacio de
    /// píxeles de la pantalla remota, compensando el letterboxing del escalado.
    /// </summary>
    private Point TranslateToRemote(Point pbPoint)
    {
        if (pbPantalla.Image == null || _remoteWidth == 0 || _remoteHeight == 0)
            return Point.Empty;

        float imgAspect = (float)_remoteWidth / _remoteHeight;
        float ctrlAspect = (float)pbPantalla.Width / pbPantalla.Height;

        float scale = imgAspect > ctrlAspect
            ? (float)pbPantalla.Width  / _remoteWidth
            : (float)pbPantalla.Height / _remoteHeight;

        float imgW = _remoteWidth  * scale;
        float imgH = _remoteHeight * scale;
        float imgX = (pbPantalla.Width  - imgW) / 2f;
        float imgY = (pbPantalla.Height - imgH) / 2f;

        // Si el punto está fuera del área de imagen (barras negras), retornar vacío
        if (pbPoint.X < imgX || pbPoint.Y < imgY ||
            pbPoint.X > imgX + imgW || pbPoint.Y > imgY + imgH)
            return Point.Empty;

        int rx = (int)(((pbPoint.X - imgX) / imgW) * _remoteWidth);
        int ry = (int)(((pbPoint.Y - imgY) / imgH) * _remoteHeight);
        return new Point(Math.Clamp(rx, 0, _remoteWidth - 1), Math.Clamp(ry, 0, _remoteHeight - 1));
    }

    // ════════════════════════════════════════════════════════════════════
    // Estado de UI
    // ════════════════════════════════════════════════════════════════════

    private void ShowConnectionView()
    {
        if (panelConexion == null) return;
        panelConexion.Visible = true;
        panelConexion.BringToFront();
        if (panelDesktop != null) panelDesktop.Visible = false;
        if (panelToolbar != null) panelToolbar.Visible = false;
        if (btnDesconectar != null) btnDesconectar.Visible = false;
        if (btnHeaderDesconectar != null) btnHeaderDesconectar.Visible = false;
        CenterConnectionCard();
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (this.Visible)
        {
            UpdateCardSizing();
        }
    }

    private void CenterConnectionCard()
    {
        UpdateCardSizing();
    }

    private void UpdateCardSizing()
    {
        if (panelConexion == null || panelCardConexion == null || gridCenter == null) return;

        int containerW = panelConexion.ClientSize.Width;
        int containerH = panelConexion.ClientSize.Height;
        if (containerW <= 0 || containerH <= 0) return;

        // Proporcional: ~60% del contenedor en pantallas medianas/grandes, acotado entre 380px y 720px
        int proportionalW = (int)(containerW * 0.60);
        int targetWidth = Math.Clamp(proportionalW, 380, 720);

        // Si el contenedor disponible es menor a 380px, ajustar para no desbordar
        int maxAvailableW = Math.Max(280, containerW - 32);
        if (targetWidth > maxAvailableW)
            targetWidth = maxAvailableW;

        panelCardConexion.MinimumSize = new Size(targetWidth, 0);
        panelCardConexion.MaximumSize = new Size(targetWidth, 0);
        panelCardConexion.Width = targetWidth;

        AdjustConnectionCardControls();

        // Calcular altura real requerida por el contenido de la tarjeta
        int cardHeight = panelCardConexion.GetPreferredSize(new Size(targetWidth, 0)).Height;
        if (cardHeight <= 0) cardHeight = panelCardConexion.Height;

        int minH = cardHeight + 32;
        gridCenter.MinimumSize = new Size(targetWidth + 24, minH);

        // Si el contenedor no alcanza para toda la tarjeta + margen,
        // fijar la fila superior para alinear arriba (no cortarse) y permitir scroll hacia abajo
        if (containerH < minH)
        {
            gridCenter.RowStyles[0] = new RowStyle(SizeType.Absolute, 12f);
            gridCenter.RowStyles[2] = new RowStyle(SizeType.Absolute, 12f);
        }
        else
        {
            gridCenter.RowStyles[0] = new RowStyle(SizeType.Percent, 50f);
            gridCenter.RowStyles[2] = new RowStyle(SizeType.Percent, 50f);
            panelConexion.AutoScrollPosition = Point.Empty;
        }
    }

    private void AdjustConnectionCardControls()
    {
        if (panelCardConexion == null) return;
        int innerW = panelCardConexion.ClientSize.Width - panelCardConexion.Padding.Horizontal;
        if (innerW <= 100) return;

        // Configurar ancho máximo de textos multilínea para que calculen su altura automáticamente sin truncar
        if (lblStatusState != null)
        {
            lblStatusState.MaximumSize = new Size(innerW - 24, 0);
        }

        if (lblCardConexionHint != null)
        {
            lblCardConexionHint.MaximumSize = new Size(innerW, 0);
        }
    }

    private void ShowDesktopView()
    {
        if (panelDesktop == null) return;
        panelConexion.Visible = false;
        panelDesktop.Visible  = true;
        panelDesktop.BringToFront();
        if (panelToolbar != null) panelToolbar.Visible = true;
        if (btnDesconectar != null)
        {
            btnDesconectar.Visible = true;
            btnDesconectar.Enabled = true;
        }
        if (btnHeaderDesconectar != null)
        {
            btnHeaderDesconectar.Visible = true;
            btnHeaderDesconectar.Enabled = true;
        }
        panelDesktop.PerformLayout();
        lblDesktopStatus.Text      = string.IsNullOrEmpty(_currentHost)
            ? "● Conectado"
            : $"● Conectado a {_currentHost}:{_currentPort}";
        lblDesktopStatus.ForeColor = Color.FromArgb(34, 197, 94);
        pbPantalla.Focus();
    }

    private void SetStatus(string title, Color color, string sub = "")
    {
        if (lblStatusTitle == null) return;
        lblStatusTitle.Text      = title;
        lblStatusTitle.ForeColor = color;
        lblStatusDot.ForeColor   = color;
        if (!string.IsNullOrEmpty(sub))
        {
            lblStatusState.Text  = sub;
            CenterConnectionCard();
        }
    }

    private void SafeInvoke(Action action)
    {
        if (!this.IsHandleCreated) return;
        if (this.InvokeRequired) this.BeginInvoke(action);
        else action();
    }

    // ════════════════════════════════════════════════════════════════════
    // Slider de calidad / FPS
    // ════════════════════════════════════════════════════════════════════

    private void SliderQuality_ValueChanged(object? sender, EventArgs e)
    {
        // Valor 1-9 → calidad 10-90% y ajuste de intervalo
        _requestedQuality    = sliderQuality.Value * 10;
        _requestedIntervalMs = sliderQuality.Value switch
        {
            >= 8 => 100,  // 80-90% calidad → ~10 FPS
            >= 6 => 133,  // 60-70% → ~7.5 FPS
            >= 4 => 200,  // 40-50% → ~5 FPS
            _ => 300       // 10-30% → ~3 FPS (mínimo ancho de banda)
        };
        lblQualityVal.Text = $"{_requestedQuality}%";

        if (_isConnected)
            _ = SendConfigAsync(_cts?.Token ?? CancellationToken.None);
    }

    // ════════════════════════════════════════════════════════════════════
    // Pantalla completa
    // ════════════════════════════════════════════════════════════════════

    private void BtnFullscreen_Click(object? sender, EventArgs e)
    {
        if (_fullscreenForm != null && !_fullscreenForm.IsDisposed)
        {
            _fullscreenForm.Close();
            return;
        }

        string connInfo = string.IsNullOrEmpty(_currentHost) ? "" : $"{_currentHost}:{_currentPort}";
        _fullscreenForm = new FullscreenDesktopForm(_stream, this, connInfo);
        _fullscreenForm.FormClosed += (s, ev) =>
        {
            _fullscreenForm = null;
            if (btnFullscreen != null) btnFullscreen.Text = "⤢ Pantalla Completa";
        };
        _fullscreenForm.Show(this.ParentForm);
        btnFullscreen.Text = "✕ Salir";
    }

    // ════════════════════════════════════════════════════════════════════
    // Pegar desde portapapeles (formato: "host:puerto@escritorio" o "host:puerto")
    // ════════════════════════════════════════════════════════════════════

    private void BtnPegar_Click(object? sender, EventArgs e)
    {
        try
        {
            string text = Clipboard.GetText().Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            // Parsear "host:puerto@escritorio" → tomar solo la parte antes de '@'
            string connPart = text.Contains('@') ? text.Split('@')[0].Trim() : text;
            int colonIdx = connPart.LastIndexOf(':');
            if (colonIdx > 0)
            {
                txtHost.Text = connPart[..colonIdx].Trim();
                if (int.TryParse(connPart[(colonIdx + 1)..].Trim(), out int port) && port is > 0 and <= 65535)
                    numPort.Value = port;
            }
            else
            {
                txtHost.Text = connPart;
            }
        }
        catch { }
    }

    // ════════════════════════════════════════════════════════════════════
    // Persistencia de última conexión
    // ════════════════════════════════════════════════════════════════════

    private void GuardarUltimaConexion(string host, int port)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LastDesktopConfigPath)!);
            File.WriteAllText(LastDesktopConfigPath,
                JsonSerializer.Serialize(new { Host = host, Port = port }));
        }
        catch { }
    }

    private void CargarUltimaConexion()
    {
        try
        {
            if (!File.Exists(LastDesktopConfigPath)) return;
            using var doc = JsonDocument.Parse(File.ReadAllText(LastDesktopConfigPath));
            if (doc.RootElement.TryGetProperty("Host", out var h) && h.GetString() is { } hs)
                txtHost.Text = hs;
            if (doc.RootElement.TryGetProperty("Port", out var p))
                numPort.Value = Math.Clamp(p.GetInt32(), 1, 65535);
        }
        catch { }
    }

    // ════════════════════════════════════════════════════════════════════
    // Dispose
    // ════════════════════════════════════════════════════════════════════

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Disconnect();
            pbPantalla?.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}

// ══════════════════════════════════════════════════════════════════════════
// Formulario de pantalla completa
// ══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Ventana maximizada sin bordes que muestra el escritorio remoto en pantalla completa.
/// Recibe frames del EscritorioRemotoControl y reenvía eventos de teclado/mouse.
/// Presionar Escape o F11 cierra la ventana.
/// </summary>
public class FullscreenDesktopForm : Form
{
    private readonly RemoteDesktopCanvas _pb;
    private readonly EscritorioRemotoControl _parent;
    private readonly NetworkStream? _stream;

    public FullscreenDesktopForm(NetworkStream? stream, EscritorioRemotoControl parent, string connectionInfo = "")
    {
        _stream = stream;
        _parent = parent;

        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState     = FormWindowState.Maximized;
        this.BackColor       = Color.Black;
        this.KeyPreview      = true;
        this.Text            = "TatoVPN — Escritorio Remoto (Pantalla completa | ESC para salir)";

        // ── Barra superior en pantalla completa ─────────────────────────
        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 44,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding   = new Padding(16, 6, 16, 6)
        };

        var leftBox = new FlowLayoutPanel
        {
            Dock          = DockStyle.Left,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0)
        };

        var lblStatus = new Label
        {
            Text      = string.IsNullOrEmpty(connectionInfo)
                ? "● Conectado (Pantalla Completa)"
                : $"● Conectado a {connectionInfo} (Pantalla Completa)",
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 197, 94),
            AutoSize  = true,
            Margin    = new Padding(0, 6, 12, 0)
        };
        leftBox.Controls.Add(lblStatus);

        var rightBox = new FlowLayoutPanel
        {
            Dock          = DockStyle.Right,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Margin        = new Padding(0)
        };

        var btnSalirFs = new Button
        {
            Text      = "✕ Salir Pantalla Completa",
            Size      = new Size(185, 32),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 8, 0)
        };
        btnSalirFs.FlatAppearance.BorderSize = 0;
        btnSalirFs.Click += (s, e) => this.Close();
        rightBox.Controls.Add(btnSalirFs);

        var btnDesconectarFs = new Button
        {
            Text      = "⏹  Desconectar",
            Size      = new Size(135, 32),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0)
        };
        btnDesconectarFs.FlatAppearance.BorderSize = 0;
        btnDesconectarFs.Click += (s, e) =>
        {
            this.Close();
            _parent.Disconnect();
        };
        rightBox.Controls.Add(btnDesconectarFs);

        topBar.Controls.Add(rightBox);
        topBar.Controls.Add(leftBox);
        this.Controls.Add(topBar);

        // ── Canvas PictureBox (Dock=Fill) ──────────────────────────────
        _pb = new RemoteDesktopCanvas
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Black,
            SizeMode  = PictureBoxSizeMode.Zoom
        };
        _pb.MouseMove  += Parent_MouseMove;
        _pb.MouseDown  += Parent_MouseDown;
        _pb.MouseUp    += Parent_MouseUp;
        _pb.MouseWheel += Parent_MouseWheel;
        _pb.KeyDown    += Parent_KeyDown;
        _pb.KeyUp      += Parent_KeyUp;
        this.Controls.Add(_pb);

        topBar.BringToFront();
        _pb.SendToBack();

        this.KeyDown += (s, e) =>
        {
            if (e.Control && e.Alt && e.Shift && e.KeyCode == Keys.D)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                this.Close();
                _parent.Disconnect();
                return;
            }
            if (e.KeyCode is Keys.Escape or Keys.F11) this.Close();
        };

        this.Shown += (s, e) => _pb.Focus();
    }

    public void UpdateFrame(Image frame)
    {
        if (!this.IsHandleCreated || this.IsDisposed) return;
        if (this.InvokeRequired) { this.BeginInvoke(() => UpdateFrame(frame)); return; }
        var old = _pb.Image;
        _pb.Image = (Image)frame.Clone();
        old?.Dispose();
    }

    private async void Parent_MouseMove(object? s, MouseEventArgs e)
    {
        if (_stream == null) return;
        var pt = _parent.GetType()
            .GetMethod("TranslateToRemote", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_parent, new object[] { e.Location }) as Point? ?? Point.Empty;
        // Simple passthrough: just send raw move
        if (pt.IsEmpty) return;
        try
        {
            var msg = new byte[5] { 0x10,
                (byte)(pt.X & 0xFF), (byte)((pt.X >> 8) & 0xFF),
                (byte)(pt.Y & 0xFF), (byte)((pt.Y >> 8) & 0xFF) };
            await _stream.WriteAsync(msg);
        }
        catch { }
    }

    private async void Parent_MouseDown(object? s, MouseEventArgs e)
    {
        if (_stream == null) return;
        byte btn = e.Button switch { MouseButtons.Left => 0, MouseButtons.Right => 1, MouseButtons.Middle => 2, _ => 255 };
        if (btn == 255) return;
        try { await _stream.WriteAsync(new byte[] { 0x12, btn }); } catch { }
    }

    private async void Parent_MouseUp(object? s, MouseEventArgs e)
    {
        if (_stream == null) return;
        byte btn = e.Button switch { MouseButtons.Left => 0, MouseButtons.Right => 1, MouseButtons.Middle => 2, _ => 255 };
        if (btn == 255) return;
        try { await _stream.WriteAsync(new byte[] { 0x13, btn }); } catch { }
    }

    private async void Parent_MouseWheel(object? s, MouseEventArgs e)
    {
        if (_stream == null) return;
        short delta = (short)(e.Delta / 120);
        try { await _stream.WriteAsync(new byte[] { 0x11, (byte)(delta & 0xFF), (byte)((delta >> 8) & 0xFF) }); }
        catch { }
    }

    private async void Parent_KeyDown(object? s, KeyEventArgs e)
    {
        if (e.Control && e.Alt && e.Shift && e.KeyCode == Keys.D)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            this.Close();
            _parent.Disconnect();
            return;
        }

        if (e.KeyCode is Keys.Escape or Keys.F11) { this.Close(); return; }
        e.Handled         = true;
        e.SuppressKeyPress = true;
        if (_stream == null) return;
        ushort vk = (ushort)e.KeyCode;
        try { await _stream.WriteAsync(new byte[] { 0x20, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) }); } catch { }
    }

    private async void Parent_KeyUp(object? s, KeyEventArgs e)
    {
        if (e.Control && e.Alt && e.Shift && e.KeyCode == Keys.D)
        {
            e.Handled = true;
            return;
        }

        e.Handled = true;
        if (_stream == null) return;
        ushort vk = (ushort)e.KeyCode;
        try { await _stream.WriteAsync(new byte[] { 0x21, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) }); } catch { }
    }
}
