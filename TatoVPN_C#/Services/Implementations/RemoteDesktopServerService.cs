using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace miVPN.Services.Implementations;

/// <summary>
/// Servicio de Escritorio Remoto — lado Servidor (Tato Host).
///
/// Captura la pantalla de esta laptop en intervalos configurables usando
/// Graphics.CopyFromScreen (GDI), comprime cada frame como JPEG y lo envía
/// por TCP al cliente conectado. Recibe eventos de mouse y teclado del cliente
/// y los reproduce localmente mediante SendInput de la API Win32.
///
/// NOTA ACADÉMICA (para paper):
/// Este es un protocolo simplificado de streaming de pantalla implementado
/// desde cero como prueba de concepto. No usa compresión diferencial (delta
/// encoding) ni codificación de video (H.264/H.265) como sí hacen RDP/VNC
/// maduros. El límite de rendimiento es el ancho de banda del túnel SSH
/// (~1-2 Mbps con Pinggy gratuito), no el protocolo en sí. Todo el tráfico
/// viaja DENTRO del túnel SSH ya cifrado por RemoteTunnelService.cs, sin
/// cifrado adicional propio (el cifrado lo provee OpenSSH end-to-end).
///
/// PROTOCOLO BINARIO (TatoVPN Desktop Protocol v1):
///   Servidor → Cliente:
///     [0xF0][width:2LE][height:2LE]         — Info de resolución de pantalla
///     [0x01][len:4LE][JPEG bytes]            — Frame de pantalla comprimido
///     [0xFF]                                 — Keepalive/ping
///
///   Cliente → Servidor:
///     [0x10][x:2LE][y:2LE]                  — Mouse move (coords en px remotos)
///     [0x12][btn:1]                          — Mouse button down (0=izq,1=der,2=med)
///     [0x13][btn:1]                          — Mouse button up
///     [0x11][delta:2LE signed]               — Mouse scroll
///     [0x20][vk:2LE]                         — Key down (Virtual Key code)
///     [0x21][vk:2LE]                         — Key up
///     [0x30][quality:1][interval_ms:2LE]     — Actualizar config de captura
///     [0xFF]                                 — Keepalive/ping
/// </summary>
public class RemoteDesktopServerService : IDisposable
{
    // ═══════════════════════════════════════════════════════════════════
    // P/Invoke — SendInput (user32.dll) para reproducir mouse y teclado
    // ═══════════════════════════════════════════════════════════════════

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint MOUSEEVENTF_MOVE      = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN  = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP    = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP   = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP   = 0x0040;
    private const uint MOUSEEVENTF_WHEEL     = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE  = 0x8000;

    private const uint KEYEVENTF_KEYDOWN     = 0x0000;
    private const uint KEYEVENTF_KEYUP       = 0x0002;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int    dx;
        public int    dy;
        public uint   mouseData;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint   dwFlags;
        public uint   time;
        public IntPtr dwExtraInfo;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Constantes y estado
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Puerto TCP local donde el servidor escucha (127.0.0.1 — solo accesible vía túnel SSH).</summary>
    public const int DefaultPort = 5900;

    private TcpListener? _listener;
    private TcpClient?   _activeClient;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private bool _disposed;

    // Config de captura (actualizable en caliente desde el cliente)
    private volatile int _captureIntervalMs = 200; // ~5 FPS default
    private volatile int _jpegQuality       = 35;  // 35% — equilibrio ancho de banda / calidad

    // Dimensiones de la pantalla principal (enviadas al cliente al conectar)
    private int _screenWidth;
    private int _screenHeight;

    public bool IsRunning => _isRunning;

    public event Action<string>? OnLog;
    public event Action? OnClientConnected;
    public event Action? OnClientDisconnected;

    // ═══════════════════════════════════════════════════════════════════
    // Start / Stop
    // ═══════════════════════════════════════════════════════════════════

    public void Start()
    {
        if (_isRunning || _disposed) return;

        try
        {
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
            _screenWidth  = bounds.Width;
            _screenHeight = bounds.Height;

            _cts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Loopback, DefaultPort);
            _listener.Start();
            _isRunning = true;

            OnLog?.Invoke($"🖥️ Servidor Escritorio Remoto escuchando en 127.0.0.1:{DefaultPort}");
            OnLog?.Invoke($"   Resolución: {_screenWidth}×{_screenHeight} | Calidad JPEG: {_jpegQuality}% | Intervalo: {_captureIntervalMs}ms");
            OnLog?.Invoke("⏳ Esperando que el cliente de escritorio remoto se conecte...");

            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"❌ Error iniciando servidor de escritorio: {ex.Message}");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning && _listener == null) return;
        _isRunning = false;
        _cts?.Cancel();
        try { _activeClient?.Close(); } catch { }
        try { _listener?.Stop(); }       catch { }
        _activeClient = null;
        _listener     = null;
        OnLog?.Invoke("🛑 Servidor Escritorio Remoto detenido.");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bucle de aceptación de clientes
    // ═══════════════════════════════════════════════════════════════════

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                client.NoDelay           = true;
                client.ReceiveBufferSize = 4096;
                client.SendBufferSize    = 512 * 1024;

                // Cerrar cliente anterior si hubiera uno colgado
                var prev = _activeClient;
                _activeClient = client;
                try { prev?.Close(); } catch { }

                string ep = client.Client.RemoteEndPoint?.ToString() ?? "desconocido";
                OnLog?.Invoke($"✅ Cliente conectado al escritorio remoto desde: {ep}");
                OnClientConnected?.Invoke();

                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                OnLog?.Invoke($"⚠️ Error aceptando cliente: {ex.Message}");
                await Task.Delay(2000, ct).ConfigureAwait(false);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Manejo de sesión de cliente
    // ═══════════════════════════════════════════════════════════════════

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            using var stream = client.GetStream();

            // Enviar dimensiones de pantalla al cliente
            await SendScreenInfoAsync(stream, ct);

            // Dos tareas paralelas: enviar frames + recibir eventos de input
            var sendTask = SendFrameLoopAsync(stream, ct);
            var recvTask = ReceiveEventLoopAsync(stream, ct);
            await Task.WhenAny(sendTask, recvTask).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnLog?.Invoke($"⚠️ Sesión interrumpida: {ex.Message}");
        }
        finally
        {
            try { client.Close(); } catch { }
            OnClientDisconnected?.Invoke();
            OnLog?.Invoke("🔌 Cliente de escritorio remoto desconectado.");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Envío de info de pantalla
    // ═══════════════════════════════════════════════════════════════════

    private async Task SendScreenInfoAsync(NetworkStream stream, CancellationToken ct)
    {
        // [0xF0][width:2LE][height:2LE]
        var msg = new byte[5];
        msg[0] = 0xF0;
        msg[1] = (byte)(_screenWidth & 0xFF);
        msg[2] = (byte)((_screenWidth  >> 8) & 0xFF);
        msg[3] = (byte)(_screenHeight & 0xFF);
        msg[4] = (byte)((_screenHeight >> 8) & 0xFF);
        await stream.WriteAsync(msg, ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bucle de envío de frames (captura GDI → JPEG → TCP)
    // ═══════════════════════════════════════════════════════════════════

    private async Task SendFrameLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        var jpegEncoder = ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == ImageFormat.Jpeg.Guid);

        if (jpegEncoder == null)
        {
            OnLog?.Invoke("❌ No se encontró el codificador JPEG del sistema.");
            return;
        }

        var sw = Stopwatch.StartNew();
        int frameCount = 0;
        long lastFpsReport = 0;
        var header = new byte[5];

        while (!ct.IsCancellationRequested && !_disposed)
        {
            long frameStart = sw.ElapsedMilliseconds;
            try
            {
                // 1. Capturar pantalla y comprimir como JPEG
                byte[] jpegData = CaptureScreenAsJpeg(jpegEncoder, _jpegQuality);

                // 2. Enviar: [0x01][len:4LE][datos JPEG]
                int len = jpegData.Length;
                header[0] = 0x01;
                header[1] = (byte)( len        & 0xFF);
                header[2] = (byte)((len >>  8) & 0xFF);
                header[3] = (byte)((len >> 16) & 0xFF);
                header[4] = (byte)((len >> 24) & 0xFF);

                await stream.WriteAsync(header, ct);
                await stream.WriteAsync(jpegData, ct);

                frameCount++;

                // Log cada ~10 segundos
                if (sw.ElapsedMilliseconds - lastFpsReport >= 10_000)
                {
                    double fps = frameCount * 1000.0 / (sw.ElapsedMilliseconds - lastFpsReport + 1);
                    OnLog?.Invoke($"📡 [Escritorio] ~{fps:F1} FPS | Frame: {len / 1024}KB | Cal: {_jpegQuality}%");
                    frameCount    = 0;
                    lastFpsReport = sw.ElapsedMilliseconds;
                }

                // Esperar resto del intervalo configurado
                long elapsed = sw.ElapsedMilliseconds - frameStart;
                int  wait    = (int)Math.Max(0, _captureIntervalMs - elapsed);
                if (wait > 0) await Task.Delay(wait, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                OnLog?.Invoke($"⚠️ Error enviando frame: {ex.Message}");
                break;
            }
        }
    }

    /// <summary>
    /// Captura la pantalla principal y la devuelve como array de bytes JPEG comprimido.
    /// Usa Graphics.CopyFromScreen (GDI) — disponible en todas las ediciones de Windows.
    /// </summary>
    private byte[] CaptureScreenAsJpeg(ImageCodecInfo encoder, int quality)
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, _screenWidth, _screenHeight);
        using var bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppRgb);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);

        var ep = new EncoderParameters(1);
        ep.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
        using var ms = new MemoryStream();
        bmp.Save(ms, encoder, ep);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Bucle de recepción de eventos (mouse / teclado / config)
    // ═══════════════════════════════════════════════════════════════════

    private async Task ReceiveEventLoopAsync(NetworkStream stream, CancellationToken ct)
    {
        var buf = new byte[8];

        while (!ct.IsCancellationRequested && !_disposed)
        {
            try
            {
                // Leer tipo de mensaje (1 byte)
                int r = await stream.ReadAsync(buf.AsMemory(0, 1), ct);
                if (r == 0) break; // Desconexión limpia

                switch (buf[0])
                {
                    // ── Mouse move: [x:2LE][y:2LE] ──────────────────
                    case 0x10:
                        await ReadExactAsync(stream, buf, 0, 4, ct);
                        int mx = buf[0] | (buf[1] << 8);
                        int my = buf[2] | (buf[3] << 8);
                        MoveMouseAbsolute(mx, my);
                        break;

                    // ── Mouse button down: [btn:1] ───────────────────
                    case 0x12:
                        await ReadExactAsync(stream, buf, 0, 1, ct);
                        MouseButtonDown(buf[0]);
                        break;

                    // ── Mouse button up: [btn:1] ─────────────────────
                    case 0x13:
                        await ReadExactAsync(stream, buf, 0, 1, ct);
                        MouseButtonUp(buf[0]);
                        break;

                    // ── Mouse scroll: [delta:2LE signed] ────────────
                    case 0x11:
                        await ReadExactAsync(stream, buf, 0, 2, ct);
                        short delta = (short)(buf[0] | (buf[1] << 8));
                        MouseScroll(delta);
                        break;

                    // ── Key down: [vk:2LE] ───────────────────────────
                    case 0x20:
                        await ReadExactAsync(stream, buf, 0, 2, ct);
                        ushort vkDown = (ushort)(buf[0] | (buf[1] << 8));
                        SendKey(vkDown, keyUp: false);
                        break;

                    // ── Key up: [vk:2LE] ─────────────────────────────
                    case 0x21:
                        await ReadExactAsync(stream, buf, 0, 2, ct);
                        ushort vkUp = (ushort)(buf[0] | (buf[1] << 8));
                        SendKey(vkUp, keyUp: true);
                        break;

                    // ── Config: [quality:1][interval_ms:2LE] ─────────
                    case 0x30:
                        await ReadExactAsync(stream, buf, 0, 3, ct);
                        _jpegQuality       = Math.Clamp((int)buf[0], 10, 90);
                        _captureIntervalMs = Math.Clamp(buf[1] | (buf[2] << 8), 66, 2000);
                        OnLog?.Invoke($"⚙️ Config actualizada: Calidad={_jpegQuality}% | Intervalo={_captureIntervalMs}ms (~{1000/_captureIntervalMs} FPS)");
                        break;

                    // ── Keepalive ────────────────────────────────────
                    case 0xFF:
                        break;

                    default:
                        // Tipo desconocido — ignorar sin crashear
                        break;
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                OnLog?.Invoke($"⚠️ Error recibiendo evento de input: {ex.Message}");
                break;
            }
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buf, int offset, int count, CancellationToken ct)
    {
        int total = 0;
        while (total < count)
        {
            int r = await stream.ReadAsync(buf.AsMemory(offset + total, count - total), ct);
            if (r == 0) throw new EndOfStreamException("El cliente cerró la conexión inesperadamente.");
            total += r;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Reproducción de input con SendInput (Win32)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Convierte coordenadas de píxel (espacio de pantalla remota) al rango 0–65535
    /// que usa SendInput en modo ABSOLUTE. Para monitor único, la conversión es lineal.
    /// </summary>
    private void MoveMouseAbsolute(int pixelX, int pixelY)
    {
        int absX = (int)((double)pixelX / _screenWidth  * 65535 + 0.5);
        int absY = (int)((double)pixelY / _screenHeight * 65535 + 0.5);
        absX = Math.Clamp(absX, 0, 65535);
        absY = Math.Clamp(absY, 0, 65535);

        SendInput(1, new[]
        {
            new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT { dx = absX, dy = absY, dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE }
                }
            }
        }, Marshal.SizeOf<INPUT>());
    }

    private void MouseButtonDown(byte btn)
    {
        uint flag = btn switch
        {
            0 => MOUSEEVENTF_LEFTDOWN,
            1 => MOUSEEVENTF_RIGHTDOWN,
            2 => MOUSEEVENTF_MIDDLEDOWN,
            _ => 0
        };
        if (flag == 0) return;
        SendInput(1, new[]
        {
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flag | MOUSEEVENTF_ABSOLUTE } } }
        }, Marshal.SizeOf<INPUT>());
    }

    private void MouseButtonUp(byte btn)
    {
        uint flag = btn switch
        {
            0 => MOUSEEVENTF_LEFTUP,
            1 => MOUSEEVENTF_RIGHTUP,
            2 => MOUSEEVENTF_MIDDLEUP,
            _ => 0
        };
        if (flag == 0) return;
        SendInput(1, new[]
        {
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = flag | MOUSEEVENTF_ABSOLUTE } } }
        }, Marshal.SizeOf<INPUT>());
    }

    private void MouseScroll(short delta)
    {
        // delta es en "clics de rueda" (1 = un tick estándar hacia arriba)
        // Windows usa WHEEL_DELTA=120 por tick
        SendInput(1, new[]
        {
            new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = (uint)(delta * 120),
                        dwFlags   = MOUSEEVENTF_WHEEL
                    }
                }
            }
        }, Marshal.SizeOf<INPUT>());
    }

    private static void SendKey(ushort vk, bool keyUp)
    {
        // Detectar teclas extendidas (flechas, Ins, Del, Home, End, PgUp/Dn, teclas Win, etc.)
        bool extended = vk is (>= 0x21 and <= 0x2E) or (>= 0x5B and <= 0x5D)
                        or 0x2C or (>= 0x70 and <= 0x87);

        SendInput(1, new[]
        {
            new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk     = vk,
                        dwFlags = (keyUp ? KEYEVENTF_KEYUP : KEYEVENTF_KEYDOWN)
                                | (extended ? KEYEVENTF_EXTENDEDKEY : 0u)
                    }
                }
            }
        }, Marshal.SizeOf<INPUT>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // IDisposable
    // ═══════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
