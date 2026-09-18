using System.Net.Sockets;
using System.Text.Json;

namespace miVPN.Controls;

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

    // ── Vista 1: Formulario de conexión ─────────────────────────────────
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
    private PictureBox pbPantalla      = null!;
    private Panel     pnlKeyCapture    = null!;

    // ── Estado TCP ───────────────────────────────────────────────────────
    private TcpClient?    _tcpClient;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private bool _isConnected;
    private bool _isConnecting;

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

        BuildHeader();
        BuildBodyPanel();
        BuildConnectionView();
        BuildDesktopView();

        CenterConnectionCard();
        this.ResumeLayout(false);
    }

    private void BuildHeader()
    {
        var tbl = new TableLayoutPanel
        {
            Dock              = DockStyle.Top,
            AutoSize          = true,
            AutoSizeMode      = AutoSizeMode.GrowAndShrink,
            ColumnCount       = 1,
            RowCount          = 2,
            Padding           = new Padding(20, 8, 20, 6),
            BackColor         = Color.FromArgb(11, 15, 25)
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        tbl.Controls.Add(new Label
        {
            Text     = "🖥️  Escritorio Remoto",
            Font     = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            Dock     = DockStyle.Fill,
            AutoSize = true,
            Padding  = new Padding(0, 4, 0, 2)
        }, 0, 0);

        tbl.Controls.Add(new Label
        {
            Text      = "Controla la pantalla de una laptop remota en tiempo real a través del túnel SSH.",
            Font      = new Font("Segoe UI", 8.8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock      = DockStyle.Fill,
            AutoSize  = true,
            Padding   = new Padding(0, 0, 0, 4)
        }, 0, 1);

        this.Controls.Add(tbl);
        this.Controls.Add(new Panel
        {
            Height    = 1,
            Dock      = DockStyle.Top,
            BackColor = Color.FromArgb(30, 41, 59)
        });
    }

    private void BuildBodyPanel()
    {
        panelBody = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = Color.FromArgb(11, 15, 25),
            Padding    = new Padding(20, 12, 20, 12),
            AutoScroll = true
        };
        this.Controls.Add(panelBody);
    }

    private void BuildConnectionView()
    {
        panelConexion = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, AutoScroll = true };
        panelConexion.Resize += (s, e) => CenterConnectionCard();

        panelCardConexion = new Panel
        {
            Location  = new Point(20, 20),
            Size      = new Size(460, 380),
            BackColor = Color.FromArgb(22, 32, 48)
        };
        panelCardConexion.Resize += (s, e) => AdjustConnectionCardControls();

        // ── Status badge ────────────────────────────────────────────────
        pnlStatusBox = new Panel
        {
            Location  = new Point(16, 14),
            Size      = new Size(428, 62),
            BackColor = Color.FromArgb(15, 23, 42)
        };
        lblStatusDot = new Label
        {
            Text      = "●",
            Font      = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            Location  = new Point(12, 14),
            Size      = new Size(22, 22),
            AutoSize  = false
        };
        lblStatusTitle = new Label
        {
            Text      = "ESCRITORIO DESCONECTADO",
            Font      = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(239, 68, 68),
            Location  = new Point(36, 10),
            Size      = new Size(378, 20)
        };
        lblStatusState = new Label
        {
            Text      = "Ingresa el host y puerto del túnel para conectarte.",
            Font      = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Location  = new Point(36, 32),
            Size      = new Size(378, 20)
        };
        pnlStatusBox.Controls.AddRange(new Control[] { lblStatusDot, lblStatusTitle, lblStatusState });
        panelCardConexion.Controls.Add(pnlStatusBox);

        // ── Host ────────────────────────────────────────────────────────
        lblCardConexionHost = new Label
        {
            Text = "Host Público / Túnel Remoto:",
            Font = new Font("Segoe UI", 8.2F), ForeColor = Color.FromArgb(148, 163, 184),
            Location = new Point(16, 88), Size = new Size(428, 18)
        };
        panelCardConexion.Controls.Add(lblCardConexionHost);

        txtHost = new TextBox
        {
            Location        = new Point(16, 108),
            Size            = new Size(428, 27),
            BackColor       = Color.FromArgb(15, 23, 42),
            ForeColor       = Color.FromArgb(56, 189, 248),
            BorderStyle     = BorderStyle.FixedSingle,
            Font            = new Font("Segoe UI", 9.5F),
            PlaceholderText = "ej: txsaw-190-xxx.run.pinggy-free.link"
        };
        panelCardConexion.Controls.Add(txtHost);

        // ── Puerto ──────────────────────────────────────────────────────
        panelCardConexion.Controls.Add(new Label
        {
            Text = "Puerto del Túnel:", Font = new Font("Segoe UI", 8.2F),
            ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(16, 142), Size = new Size(200, 18)
        });
        numPort = new NumericUpDown
        {
            Location    = new Point(16, 162),
            Size        = new Size(175, 27),
            Minimum     = 1,
            Maximum     = 65535,
            Value       = 5900,
            BackColor   = Color.FromArgb(15, 23, 42),
            ForeColor   = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font        = new Font("Segoe UI", 9.5F)
        };
        panelCardConexion.Controls.Add(numPort);

        // ── Hint ────────────────────────────────────────────────────────
        lblCardConexionHint = new Label
        {
            Text = "💡 Obtén el host y puerto del log en Modo Servidor → Escritorio Remoto + Acceso Remoto.\n" +
                   "   O usa el botón '📋 Copiar Configuración Completa' y pégalo aquí con el botón de abajo.",
            Font     = new Font("Segoe UI", 7.8F),
            ForeColor = Color.FromArgb(250, 204, 21),
            Location = new Point(16, 196),
            Size     = new Size(428, 44),
            AutoSize = false
        };
        panelCardConexion.Controls.Add(lblCardConexionHint);

        // ── Pegar portapapeles ──────────────────────────────────────────
        btnPegar = new Button
        {
            Text      = "📋  Pegar desde Portapapeles",
            Location  = new Point(16, 248),
            Size      = new Size(220, 32),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.FromArgb(226, 232, 240),
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8.5F),
            Cursor    = Cursors.Hand
        };
        btnPegar.FlatAppearance.BorderSize = 0;
        btnPegar.Click += BtnPegar_Click;
        panelCardConexion.Controls.Add(btnPegar);

        // ── Conectar ────────────────────────────────────────────────────
        btnConectar = new Button
        {
            Text      = "🖥️  Conectar Escritorio Remoto",
            Location  = new Point(16, 290),
            Size      = new Size(428, 42),
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        btnConectar.FlatAppearance.BorderSize = 0;
        btnConectar.Click += BtnConectar_Click;
        panelCardConexion.Controls.Add(btnConectar);

        panelConexion.Controls.Add(panelCardConexion);
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
            Dock      = DockStyle.Top,
            Height    = 42,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding   = new Padding(6, 4, 6, 4)
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
            Font      = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(251, 191, 36),
            AutoSize  = true,
            Margin    = new Padding(4, 7, 10, 0)
        };
        panelToolbarLeft.Controls.Add(lblDesktopStatus);

        btnDesconectar = new Button
        {
            Text      = "⏹ Desconectar",
            Size      = new Size(125, 28),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 8F, FontStyle.Bold),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 2, 8, 0)
        };
        btnDesconectar.FlatAppearance.BorderSize = 0;
        btnDesconectar.Click += (s, e) => Disconnect();
        panelToolbarLeft.Controls.Add(btnDesconectar);

        btnFullscreen = new Button
        {
            Text      = "⤢",
            Size      = new Size(36, 28),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 11F),
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 2, 0, 0)
        };
        btnFullscreen.FlatAppearance.BorderSize = 0;
        btnFullscreen.Click += BtnFullscreen_Click;
        panelToolbarLeft.Controls.Add(btnFullscreen);

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

        panelToolbarRight.Controls.Add(new Label
        {
            Text      = "Cal.:",
            Font      = new Font("Segoe UI", 7.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Margin    = new Padding(0, 9, 2, 0)
        });

        sliderQuality = new TrackBar
        {
            Size          = new Size(95, 28),
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
            Font      = new Font("Segoe UI", 7.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize  = true,
            Margin    = new Padding(0, 9, 10, 0)
        };
        panelToolbarRight.Controls.Add(lblQualityVal);

        lblFps = new Label
        {
            Text      = "— FPS",
            Font      = new Font("Consolas", 8F),
            ForeColor = Color.FromArgb(34, 197, 94),
            AutoSize  = true,
            Margin    = new Padding(0, 9, 4, 0)
        };
        panelToolbarRight.Controls.Add(lblFps);

        panelToolbar.Controls.Add(panelToolbarRight);
        panelToolbar.Controls.Add(panelToolbarLeft);
        panelDesktop.Controls.Add(panelToolbar);

        // ── PictureBox (vista de pantalla remota) ───────────────────────
        pbPantalla = new PictureBox
        {
            Dock     = DockStyle.Fill,
            BackColor = Color.Black,
            SizeMode = PictureBoxSizeMode.Zoom,
            Cursor   = Cursors.Cross
        };
        pbPantalla.MouseMove  += PbPantalla_MouseMove;
        pbPantalla.MouseDown  += PbPantalla_MouseDown;
        pbPantalla.MouseUp    += PbPantalla_MouseUp;
        pbPantalla.MouseWheel += PbPantalla_MouseWheel;
        pbPantalla.Click      += (s, e) => pnlKeyCapture.Focus();
        panelDesktop.Controls.Add(pbPantalla);

        // ── Panel transparente de captura de teclado ────────────────────
        // Overlay sobre pbPantalla con TabStop=true para recibir KeyDown/KeyUp.
        // Invisible para el usuario, pero captura todas las pulsaciones de tecla.
        pnlKeyCapture = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            TabStop   = true
        };
        pnlKeyCapture.KeyDown        += PnlKeyCapture_KeyDown;
        pnlKeyCapture.KeyUp          += PnlKeyCapture_KeyUp;
        pnlKeyCapture.PreviewKeyDown += (s, e) =>
        {
            // Marcar teclas especiales como "input keys" para que WinForms no las consuma
            if (e.KeyCode is Keys.Tab or Keys.Return or Keys.Escape
                or Keys.Up or Keys.Down or Keys.Left or Keys.Right
                or Keys.Back or Keys.Delete)
                e.IsInputKey = true;
        };
        panelDesktop.Controls.Add(pnlKeyCapture);
        pnlKeyCapture.BringToFront();

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
        _cts          = new CancellationTokenSource();
        var ct        = _cts.Token;

        try
        {
            SetStatus("⏳ CONECTANDO...", Color.FromArgb(251, 191, 36), $"Conectando a {host}:{port}...");
            btnConectar.Enabled = false;

            _tcpClient = new TcpClient { NoDelay = true };
            await _tcpClient.ConnectAsync(host, port, ct);
            _stream       = _tcpClient.GetStream();
            _isConnected  = true;
            _isConnecting = false;

            GuardarUltimaConexion(host, port);
            ShowDesktopView();

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
            SetStatus("❌ ERROR DE CONEXIÓN", Color.FromArgb(239, 68, 68),
                      ex.Message.Length > 80 ? ex.Message[..80] + "…" : ex.Message);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Bucle de recepción de frames del servidor
    // ════════════════════════════════════════════════════════════════════

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buf = new byte[8];
        try
        {
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
                        SafeInvoke(() =>
                        {
                            lblDesktopStatus.Text      = $"● Activo | {_remoteWidth}×{_remoteHeight}";
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
            SafeInvoke(() => HandleDisconnection($"Sesión interrumpida: {ex.Message}"));
        }
    }

    private void DisplayFrame(byte[] jpegData)
    {
        try
        {
            using var ms  = new MemoryStream(jpegData);
            var newImage  = Image.FromStream(ms);

            SafeInvoke(() =>
            {
                try
                {
                    var old = pbPantalla.Image;
                    pbPantalla.Image = newImage;
                    old?.Dispose();

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
                catch { newImage.Dispose(); }
            });
        }
        catch { /* Frame corrupto — ignorar */ }
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
        _isConnected = false;
        _cts?.Cancel();
        try { _tcpClient?.Close(); } catch { }
        _tcpClient = null;
        _stream    = null;
        HandleDisconnection("Desconectado por el usuario.");
    }

    private void HandleDisconnection(string reason)
    {
        if (!this.IsHandleCreated) return;
        SafeInvoke(() =>
        {
            _isConnected = false;
            var old = pbPantalla?.Image;
            if (pbPantalla != null) pbPantalla.Image = null;
            old?.Dispose();
            _fullscreenForm?.Close();
            ShowConnectionView();
            SetStatus("ESCRITORIO DESCONECTADO", Color.FromArgb(239, 68, 68),
                      reason.Length > 80 ? reason[..80] : reason);
            if (btnConectar != null) btnConectar.Enabled = true;
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
        pnlKeyCapture.Focus();

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

    private async void PnlKeyCapture_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_isConnected || _stream == null) return;
        e.Handled         = true;
        e.SuppressKeyPress = true;

        ushort vk = (ushort)e.KeyCode;
        try
        {
            // [0x20][vk:2LE]
            await _stream.WriteAsync(new byte[] { 0x20, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) });
        }
        catch { }
    }

    private async void PnlKeyCapture_KeyUp(object? sender, KeyEventArgs e)
    {
        if (!_isConnected || _stream == null) return;
        e.Handled = true;

        ushort vk = (ushort)e.KeyCode;
        try
        {
            // [0x21][vk:2LE]
            await _stream.WriteAsync(new byte[] { 0x21, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) });
        }
        catch { }
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
        CenterConnectionCard();
    }

    private void CenterConnectionCard()
    {
        if (panelConexion == null || panelCardConexion == null) return;
        int targetWidth = Math.Clamp(panelConexion.ClientSize.Width - 40, 380, 560);
        panelCardConexion.Width = targetWidth;
        int x = Math.Max(15, (panelConexion.ClientSize.Width - panelCardConexion.Width) / 2);
        int y = Math.Max(15, (panelConexion.ClientSize.Height - panelCardConexion.Height) / 2);
        panelCardConexion.Location = new Point(x, y);
        AdjustConnectionCardControls();
    }

    private void AdjustConnectionCardControls()
    {
        if (panelCardConexion == null) return;
        int innerW = panelCardConexion.ClientSize.Width - 32;
        if (innerW <= 100) return;

        if (pnlStatusBox != null)
        {
            pnlStatusBox.Width = innerW;
            if (lblStatusTitle != null) lblStatusTitle.Width = Math.Max(120, innerW - 48);
            if (lblStatusState != null) lblStatusState.Width = Math.Max(120, innerW - 48);
        }

        if (lblCardConexionHost != null) lblCardConexionHost.Width = innerW;
        if (txtHost != null) txtHost.Width = innerW;

        if (lblCardConexionHint != null) lblCardConexionHint.Width = innerW;
        if (btnConectar != null) btnConectar.Width = innerW;
    }

    private void ShowDesktopView()
    {
        if (panelDesktop == null) return;
        panelConexion.Visible = false;
        panelDesktop.Visible  = true;
        panelDesktop.BringToFront();
        lblDesktopStatus.Text      = "● Conectando...";
        lblDesktopStatus.ForeColor = Color.FromArgb(251, 191, 36);
        pnlKeyCapture.Focus();
    }

    private void SetStatus(string title, Color color, string sub = "")
    {
        if (lblStatusTitle == null) return;
        lblStatusTitle.Text      = title;
        lblStatusTitle.ForeColor = color;
        lblStatusDot.ForeColor   = color;
        if (!string.IsNullOrEmpty(sub))
            lblStatusState.Text  = sub;
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

        _fullscreenForm = new FullscreenDesktopForm(_stream, this);
        _fullscreenForm.FormClosed += (s, ev) => _fullscreenForm = null;
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
    private readonly PictureBox _pb;
    private readonly Panel _overlay;
    private readonly EscritorioRemotoControl _parent;
    private readonly NetworkStream? _stream;

    public FullscreenDesktopForm(NetworkStream? stream, EscritorioRemotoControl parent)
    {
        _stream = stream;
        _parent = parent;

        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState     = FormWindowState.Maximized;
        this.BackColor       = Color.Black;
        this.KeyPreview      = true;
        this.Text            = "TatoVPN — Escritorio Remoto (Pantalla completa | ESC para salir)";

        _pb = new PictureBox
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Black,
            SizeMode  = PictureBoxSizeMode.Zoom
        };
        this.Controls.Add(_pb);

        // Barra flotante: "Esc = Salir"
        var hint = new Label
        {
            Text      = "[ ESC o F11 = Salir de pantalla completa ]",
            Font      = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(200, 148, 163, 184),
            BackColor = Color.FromArgb(180, 15, 23, 42),
            AutoSize  = true,
            Padding   = new Padding(8, 4, 8, 4),
            Location  = new Point(20, 12)
        };
        this.Controls.Add(hint);
        hint.BringToFront();

        _overlay = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.Transparent,
            TabStop   = true
        };
        _overlay.PreviewKeyDown += (s, e) =>
        {
            if (e.KeyCode is Keys.Tab or Keys.Return or Keys.Escape or Keys.Back or Keys.Delete
                or Keys.Up or Keys.Down or Keys.Left or Keys.Right)
                e.IsInputKey = true;
        };
        _overlay.MouseMove  += Parent_MouseMove;
        _overlay.MouseDown  += Parent_MouseDown;
        _overlay.MouseUp    += Parent_MouseUp;
        _overlay.MouseWheel += Parent_MouseWheel;
        _overlay.KeyDown    += Parent_KeyDown;
        _overlay.KeyUp      += Parent_KeyUp;
        this.Controls.Add(_overlay);
        _overlay.BringToFront();
        hint.BringToFront();

        this.KeyDown += (s, e) =>
        {
            if (e.KeyCode is Keys.Escape or Keys.F11) this.Close();
        };

        this.Shown += (s, e) => _overlay.Focus();
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
        if (e.KeyCode is Keys.Escape or Keys.F11) { this.Close(); return; }
        e.Handled         = true;
        e.SuppressKeyPress = true;
        if (_stream == null) return;
        ushort vk = (ushort)e.KeyCode;
        try { await _stream.WriteAsync(new byte[] { 0x20, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) }); } catch { }
    }

    private async void Parent_KeyUp(object? s, KeyEventArgs e)
    {
        e.Handled = true;
        if (_stream == null) return;
        ushort vk = (ushort)e.KeyCode;
        try { await _stream.WriteAsync(new byte[] { 0x21, (byte)(vk & 0xFF), (byte)((vk >> 8) & 0xFF) }); } catch { }
    }
}
