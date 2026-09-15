using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts.WinForms;
using miVPN.Enums;
using WpfColor = System.Windows.Media.Color;
using WpfBrush = System.Windows.Media.SolidColorBrush;

namespace miVPN;

public partial class Form1
{
    private Panel panelDashboard = null!;
    private Panel panelDashboardCardsContainer = null!;
    private TableLayoutPanel tableDashboardCards = null!;

    // Card 1: Latencia
    private Panel cardLatency = null!;
    private Label lblLatencyTitle = null!;
    private Label lblLatencyValue = null!;
    private LiveCharts.WinForms.CartesianChart chartLatency = null!;
    private ChartValues<ObservableValue> _latencyValues = null!;
    private LineSeries _seriesLatency = null!;

    // Card 2: CPU
    private Panel cardCpu = null!;
    private Label lblCpuTitle = null!;
    private Label lblCpuValue = null!;
    private LiveCharts.WinForms.CartesianChart chartCpu = null!;
    private ChartValues<ObservableValue> _cpuValues = null!;
    private LineSeries _seriesCpu = null!;
    private DateTime _lastCpuMeasurementTime = DateTime.UtcNow;
    private TimeSpan _lastCpuTotalProcessorTime = TimeSpan.Zero;

    // Card 3: Dominios Bloqueados vs Permitidos
    private Panel cardFilter = null!;
    private Label lblFilterTitle = null!;
    private LiveCharts.WinForms.PieChart chartDomainRatio = null!;
    private Label lblRatioBlocked = null!;
    private Label lblRatioAllowed = null!;
    private Label lblRatioTotal = null!;
    private ObservableValue _valBloqueados = null!;
    private ObservableValue _valPermitidos = null!;
    private PieSeries _seriesBloqueados = null!;
    private PieSeries _seriesPermitidos = null!;
    private PieSeries _seriesNeutro = null!;
    private bool _isPieInNeutralState = true;

    // Timer & concurrency
    private System.Windows.Forms.Timer? _dashboardTimer;
    private bool _isUpdatingDashboard = false;
    private const int MaxSlidingWindowPoints = 40; // 40 muestras x 1.5s = 60 segundos de historial

    private void InitializeDashboard()
    {
        panelDashboard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(11, 15, 25),
            Visible = false
        };

        // Header (mismo estilo y estructura que Filtrado de Contenido)
        var flowHeader = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(20, 20, 20, 10)
        };

        var lblTitle = new Label
        {
            Text = "Dashboard Visual (KPIs)",
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.White
        };

        var lblSub = new Label
        {
            Text = "Monitor de rendimiento y estadísticas de filtrado en tiempo real.",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
            Margin = new Padding(0, 5, 0, 15)
        };

        flowHeader.Controls.Add(lblTitle);
        flowHeader.Controls.Add(lblSub);

        // Contenedor principal de tarjetas con scroll si la ventana es pequeña verticalmente
        panelDashboardCardsContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(15, 5, 15, 15),
            BackColor = System.Drawing.Color.FromArgb(11, 15, 25)
        };

        tableDashboardCards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = System.Drawing.Color.Transparent
        };
        tableDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        tableDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        tableDashboardCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
        tableDashboardCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        // Construcción de las 3 tarjetas KPIs
        BuildLatencyCard();
        BuildCpuCard();
        BuildFilterRatioCard();

        tableDashboardCards.Controls.Add(cardLatency, 0, 0);
        tableDashboardCards.Controls.Add(cardCpu, 1, 0);
        tableDashboardCards.Controls.Add(cardFilter, 2, 0);

        panelDashboardCardsContainer.Controls.Add(tableDashboardCards);
        panelDashboard.Controls.Add(panelDashboardCardsContainer);
        panelDashboard.Controls.Add(flowHeader);

        // Suscribir resize para acomodo responsivo (fila horizontal vs columna vertical)
        panelDashboardCardsContainer.Resize += (s, e) => UpdateDashboardCardsLayout();

        // Agregar panelDashboard a panelMain
        panelMain.Controls.Add(panelDashboard);

        // Inicializar timer cada 1.5 segundos
        try
        {
            _lastCpuMeasurementTime = DateTime.UtcNow;
            _lastCpuTotalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;
        }
        catch { }

        _dashboardTimer = new System.Windows.Forms.Timer
        {
            Interval = 1500
        };
        _dashboardTimer.Tick += async (s, e) => await OnDashboardTimerTickAsync();
    }

    private void UpdateDashboardCardsLayout()
    {
        // Las 3 tarjetas se mantienen permanentemente en 3 columnas proporcionales (33.33% cada una)
    }

    private void BuildLatencyCard()
    {
        cardLatency = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39),
            Margin = new Padding(6),
            Padding = new Padding(12, 10, 16, 10),
            AutoScroll = true
        };

        var pnlHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 66,
            BackColor = System.Drawing.Color.Transparent
        };

        lblLatencyTitle = new Label
        {
            Text = "Latencia DNS sobre SSH (RTT ms)",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.White,
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 42,
            MaximumSize = new Size(0, 0)
        };

        lblLatencyValue = new Label
        {
            Text = "N/A - Desconectado",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(249, 115, 22), // Naranja TatoVPN
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 22,
            MaximumSize = new Size(0, 0)
        };

        pnlHead.Controls.Add(lblLatencyValue);
        pnlHead.Controls.Add(lblLatencyTitle);

        // Gráfico cartesiano de latencia
        _latencyValues = new ChartValues<ObservableValue>();
        for (int i = 0; i < 10; i++) _latencyValues.Add(new ObservableValue(0));

        _seriesLatency = new LineSeries
        {
            Title = "RTT ms",
            Values = _latencyValues,
            PointGeometry = DefaultGeometries.Circle,
            PointGeometrySize = 5,
            StrokeThickness = 2,
            Stroke = new WpfBrush(WpfColor.FromRgb(249, 115, 22)),
            Fill = new WpfBrush(WpfColor.FromArgb(35, 249, 115, 22))
        };

        chartLatency = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Transparent,
            DisableAnimations = true,
            Hoverable = false
        };

        chartLatency.AxisX.Add(new Axis
        {
            ShowLabels = false,
            Separator = new Separator
            {
                StrokeThickness = 0.5,
                Stroke = new WpfBrush(WpfColor.FromArgb(50, 148, 163, 184))
            }
        });

        chartLatency.AxisY.Add(new Axis
        {
            MinValue = 0,
            Foreground = new WpfBrush(WpfColor.FromRgb(148, 163, 184)),
            Separator = new Separator
            {
                StrokeThickness = 0.5,
                Stroke = new WpfBrush(WpfColor.FromArgb(50, 148, 163, 184))
            }
        });

        chartLatency.Series = new SeriesCollection { _seriesLatency };

        cardLatency.Controls.Add(chartLatency);
        cardLatency.Controls.Add(pnlHead);
    }

    private void BuildCpuCard()
    {
        cardCpu = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39),
            Margin = new Padding(6),
            Padding = new Padding(12, 10, 16, 10),
            AutoScroll = true
        };

        var pnlHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 66,
            BackColor = System.Drawing.Color.Transparent
        };

        lblCpuTitle = new Label
        {
            Text = "Uso de CPU (TatoVPN)",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.White,
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 42,
            MaximumSize = new Size(0, 0)
        };

        lblCpuValue = new Label
        {
            Text = "0.0 % (Proceso actual)",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(59, 130, 246), // Azul
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 22,
            MaximumSize = new Size(0, 0)
        };

        pnlHead.Controls.Add(lblCpuValue);
        pnlHead.Controls.Add(lblCpuTitle);

        _cpuValues = new ChartValues<ObservableValue>();
        for (int i = 0; i < 10; i++) _cpuValues.Add(new ObservableValue(0));

        _seriesCpu = new LineSeries
        {
            Title = "CPU %",
            Values = _cpuValues,
            PointGeometry = DefaultGeometries.Circle,
            PointGeometrySize = 5,
            StrokeThickness = 2,
            Stroke = new WpfBrush(WpfColor.FromRgb(59, 130, 246)),
            Fill = new WpfBrush(WpfColor.FromArgb(35, 59, 130, 246))
        };

        chartCpu = new LiveCharts.WinForms.CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Transparent,
            DisableAnimations = true,
            Hoverable = false
        };

        chartCpu.AxisX.Add(new Axis
        {
            ShowLabels = false,
            Separator = new Separator
            {
                StrokeThickness = 0.5,
                Stroke = new WpfBrush(WpfColor.FromArgb(50, 148, 163, 184))
            }
        });

        chartCpu.AxisY.Add(new Axis
        {
            MinValue = 0,
            MaxValue = 100,
            Foreground = new WpfBrush(WpfColor.FromRgb(148, 163, 184)),
            Separator = new Separator
            {
                StrokeThickness = 0.5,
                Stroke = new WpfBrush(WpfColor.FromArgb(50, 148, 163, 184))
            }
        });

        chartCpu.Series = new SeriesCollection { _seriesCpu };

        cardCpu.Controls.Add(chartCpu);
        cardCpu.Controls.Add(pnlHead);
    }

    private void BuildFilterRatioCard()
    {
        cardFilter = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.FromArgb(17, 24, 39),
            Margin = new Padding(6),
            Padding = new Padding(12, 10, 16, 10),
            AutoScroll = true
        };

        var pnlHead = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = System.Drawing.Color.Transparent
        };

        lblFilterTitle = new Label
        {
            Text = "Proporción Bloqueados vs Permitidos",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.White,
            AutoSize = false,
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 42,
            MaximumSize = new Size(0, 0)
        };
        pnlHead.Controls.Add(lblFilterTitle);

        // Panel inferior con resumen y etiquetas numéricas exactas
        var pnlBottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = System.Drawing.Color.Transparent,
            Padding = new Padding(0, 4, 0, 4)
        };

        lblRatioBlocked = new Label
        {
            Text = "🔴 Bloqueados: 0",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(239, 68, 68),
            AutoSize = true,
            Margin = new Padding(0, 2, 8, 2)
        };

        lblRatioAllowed = new Label
        {
            Text = "🟢 Permitidos: 0",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = System.Drawing.Color.FromArgb(34, 197, 94),
            AutoSize = true,
            Margin = new Padding(0, 2, 8, 2)
        };

        lblRatioTotal = new Label
        {
            Text = "(Sin tráfico aún)",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = System.Drawing.Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Margin = new Padding(0, 3, 0, 2)
        };

        pnlBottom.Controls.Add(lblRatioBlocked);
        pnlBottom.Controls.Add(lblRatioAllowed);
        pnlBottom.Controls.Add(lblRatioTotal);

        // Instanciar valores y series del PieChart
        _valBloqueados = new ObservableValue(0);
        _valPermitidos = new ObservableValue(0);

        _seriesBloqueados = new PieSeries
        {
            Title = "Bloqueados",
            Values = new ChartValues<ObservableValue> { _valBloqueados },
            Fill = new WpfBrush(WpfColor.FromRgb(220, 53, 69)), // Rojo explícito
            DataLabels = true,
            Foreground = new WpfBrush(WpfColor.FromRgb(255, 255, 255))
        };

        _seriesPermitidos = new PieSeries
        {
            Title = "Permitidos",
            Values = new ChartValues<ObservableValue> { _valPermitidos },
            Fill = new WpfBrush(WpfColor.FromRgb(40, 167, 69)), // Verde explícito
            DataLabels = true,
            Foreground = new WpfBrush(WpfColor.FromRgb(255, 255, 255))
        };

        _seriesNeutro = new PieSeries
        {
            Title = "Sin tráfico aún",
            Values = new ChartValues<ObservableValue> { new ObservableValue(1) },
            Fill = new WpfBrush(WpfColor.FromRgb(100, 116, 139)), // Gris/slate neutro
            DataLabels = false
        };

        chartDomainRatio = new LiveCharts.WinForms.PieChart
        {
            Dock = DockStyle.Fill,
            BackColor = System.Drawing.Color.Transparent,
            InnerRadius = 50, // Efecto doughnut
            LegendLocation = LegendLocation.None
        };

        // Estado inicial neutro (0 consultas)
        chartDomainRatio.Series = new SeriesCollection { _seriesNeutro };
        _isPieInNeutralState = true;

        cardFilter.Controls.Add(chartDomainRatio);
        cardFilter.Controls.Add(pnlBottom);
        cardFilter.Controls.Add(pnlHead);
    }

    private void ResumeDashboardTimer()
    {
        if (_dashboardTimer != null && !_dashboardTimer.Enabled)
        {
            _dashboardTimer.Start();
            _ = Task.Run(async () => await OnDashboardTimerTickAsync());
        }
    }

    private void PauseDashboardTimer()
    {
        if (_dashboardTimer != null && _dashboardTimer.Enabled)
        {
            _dashboardTimer.Stop();
        }
    }

    private async Task OnDashboardTimerTickAsync()
    {
        if (_isUpdatingDashboard) return;
        _isUpdatingDashboard = true;

        try
        {
            // 1. CPU de TatoVPN
            double cpuPercent = CalculateCurrentProcessCpuPercent();

            // 2. Latencia (RTT Ping TCP)
            bool isVpnActive = (_currentState == ConnectionState.SshAuthenticated ||
                               _currentState == ConnectionState.SocksProxyActive ||
                               (_sshService != null && _sshService.IsConnected));

            string host = string.Empty;
            int port = 22;

            if (isVpnActive)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(() =>
                    {
                        host = txtSshHost?.Text?.Trim() ?? string.Empty;
                        port = (chkUseTls != null && chkUseTls.Checked && numTlsPort != null && numTlsPort.Value > 0)
                            ? (int)numTlsPort.Value
                            : (numSshPort != null ? (int)numSshPort.Value : 22);
                    });
                }
                else
                {
                    host = txtSshHost?.Text?.Trim() ?? string.Empty;
                    port = (chkUseTls != null && chkUseTls.Checked && numTlsPort != null && numTlsPort.Value > 0)
                        ? (int)numTlsPort.Value
                        : (numSshPort != null ? (int)numSshPort.Value : 22);
                }
            }

            double latencyMs = -1;
            if (isVpnActive && !string.IsNullOrWhiteSpace(host))
            {
                latencyMs = await MeasureTcpPingLatencyAsync(host, port);
            }

            // 3. Contadores de dominios
            long totalBloqueados = _contentFilterService?.TotalBloqueados ?? 0;
            long totalPermitidos = _contentFilterService?.TotalPermitidos ?? 0;

            // Actualizar controles UI
            if (!this.IsDisposed && this.IsHandleCreated)
            {
                this.BeginInvoke(() =>
                {
                    UpdateDashboardUi(latencyMs, isVpnActive, cpuPercent, totalBloqueados, totalPermitidos);
                });
            }
        }
        catch
        {
            // Fail-safe: el dashboard nunca debe lanzar excepciones hacia la app
        }
        finally
        {
            _isUpdatingDashboard = false;
        }
    }

    private double CalculateCurrentProcessCpuPercent()
    {
        try
        {
            var now = DateTime.UtcNow;
            var totalProcessorTime = Process.GetCurrentProcess().TotalProcessorTime;

            var timeDiff = (now - _lastCpuMeasurementTime).TotalMilliseconds;
            var cpuDiff = (totalProcessorTime - _lastCpuTotalProcessorTime).TotalMilliseconds;

            _lastCpuMeasurementTime = now;
            _lastCpuTotalProcessorTime = totalProcessorTime;

            if (timeDiff <= 0) return 0.0;

            double cpu = (cpuDiff / (timeDiff * Environment.ProcessorCount)) * 100.0;
            return Math.Clamp(Math.Round(cpu, 1), 0.0, 100.0);
        }
        catch
        {
            return 0.0;
        }
    }

    private async Task<double> MeasureTcpPingLatencyAsync(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var sw = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource(1200);

            var connectTask = client.ConnectAsync(host, port, cts.Token).AsTask();
            var completedTask = await Task.WhenAny(connectTask, Task.Delay(1200, cts.Token));

            if (completedTask == connectTask && client.Connected)
            {
                sw.Stop();
                return Math.Round(sw.Elapsed.TotalMilliseconds, 1);
            }
        }
        catch
        {
            // Timeout o error de conexión
        }
        return -1;
    }

    private void UpdateDashboardUi(double latencyMs, bool isVpnActive, double cpuPercent, long totalBloqueados, long totalPermitidos)
    {
        if (this.IsDisposed) return;

        // 1. UI Latencia
        if (isVpnActive && latencyMs >= 0)
        {
            lblLatencyValue.Text = $"⚡ {latencyMs:0.0} ms (RTT)";
            lblLatencyValue.ForeColor = latencyMs < 100
                ? System.Drawing.Color.FromArgb(34, 197, 94) // Verde
                : (latencyMs < 250
                    ? System.Drawing.Color.FromArgb(249, 115, 22) // Naranja
                    : System.Drawing.Color.FromArgb(239, 68, 68)); // Rojo

            _latencyValues.Add(new ObservableValue(latencyMs));
        }
        else
        {
            lblLatencyValue.Text = isVpnActive ? "⏳ Calculando RTT..." : "N/A - Desconectado";
            lblLatencyValue.ForeColor = System.Drawing.Color.FromArgb(148, 163, 184);
            _latencyValues.Add(new ObservableValue(0));
        }

        while (_latencyValues.Count > MaxSlidingWindowPoints)
        {
            _latencyValues.RemoveAt(0);
        }

        // 2. UI CPU
        lblCpuValue.Text = $"{cpuPercent:0.0} % (Uso de CPU)";
        lblCpuValue.ForeColor = cpuPercent < 50
            ? System.Drawing.Color.FromArgb(59, 130, 246)
            : (cpuPercent < 80
                ? System.Drawing.Color.FromArgb(249, 115, 22)
                : System.Drawing.Color.FromArgb(239, 68, 68));

        _cpuValues.Add(new ObservableValue(cpuPercent));
        while (_cpuValues.Count > MaxSlidingWindowPoints)
        {
            _cpuValues.RemoveAt(0);
        }

        // 3. UI Proporción de dominios
        long totalConsultas = totalBloqueados + totalPermitidos;
        lblRatioBlocked.Text = $"🔴 Bloqueados: {totalBloqueados}";
        lblRatioAllowed.Text = $"🟢 Permitidos: {totalPermitidos}";

        if (totalConsultas == 0)
        {
            lblRatioTotal.Text = "(Sin tráfico aún)";
            if (!_isPieInNeutralState)
            {
                chartDomainRatio.Series = new SeriesCollection { _seriesNeutro };
                _isPieInNeutralState = true;
            }
        }
        else
        {
            lblRatioTotal.Text = $"(Total: {totalConsultas})";
            if (_isPieInNeutralState)
            {
                _valBloqueados.Value = totalBloqueados;
                _valPermitidos.Value = totalPermitidos;
                _seriesBloqueados.Title = $"Bloqueados: {totalBloqueados}";
                _seriesPermitidos.Title = $"Permitidos: {totalPermitidos}";
                chartDomainRatio.Series = new SeriesCollection { _seriesBloqueados, _seriesPermitidos };
                _isPieInNeutralState = false;
            }
            else
            {
                _valBloqueados.Value = totalBloqueados;
                _valPermitidos.Value = totalPermitidos;
                _seriesBloqueados.Title = $"Bloqueados: {totalBloqueados}";
                _seriesPermitidos.Title = $"Permitidos: {totalPermitidos}";
            }
        }
    }

    private void btnNavDashboard_Click(object? sender, EventArgs e)
    {
        panelInicio.Visible = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible = false;
        panelConfigSsh.Visible = false;
        panelRegistro.Visible = false;
        panelConfigs.Visible = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible = false;
        if (panelFiltroContenido != null) panelFiltroContenido.Visible = false;

        if (panelDashboard != null)
        {
            panelDashboard.Visible = true;
            panelDashboard.BringToFront();
            UpdateDashboardCardsLayout();
        }

        SetNavButtonActive(btnNavDashboard);
        ResumeDashboardTimer();
    }
}
