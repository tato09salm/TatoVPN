using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using miVPN.Models;
using miVPN.Services.Implementations;
using miVPN.Services.Interfaces;

namespace miVPN.Controls;

public class ConexionRemotaControl : UserControl
{
    private readonly IRemoteFileService _remoteFileService;
    private readonly IConfigService? _configService;

    // Sub-vistas principales
    private Panel panelHeader = null!;
    private Label lblHeaderTitle = null!;
    private Label lblHeaderSub = null!;
    private Panel panelBody = null!;

    // Vista 1: Formulario de Conexión
    private Panel panelConexion = null!;
    private Panel panelCardConexion = null!;
    private Panel pnlStatusBox = null!;
    private Label lblStatusDot = null!;
    private Label lblStatusTitle = null!;
    private Label lblStatusState = null!;
    private Label lblStatusSub = null!;

    private TextBox txtHost = null!;
    private NumericUpDown numPort = null!;
    private TextBox txtUser = null!;
    private TextBox txtPass = null!;
    private Button btnTogglePass = null!;
    private CheckBox chkSaveConnection = null!;
    private Button btnConectar = null!;

    // Vista 2: Explorador de Archivos
    private Panel panelExplorador = null!;
    private TableLayoutPanel panelExploradorTop = null!;
    private Button btnUp = null!;
    private TextBox txtRutaActual = null!;
    private Button btnIr = null!;
    private Button btnRefresh = null!;

    private Button btnDownload = null!;
    private Button btnUpload = null!;
    private Button btnNewFolder = null!;
    private Button btnRename = null!;
    private Button btnDelete = null!;
    private Button btnDisconnect = null!;

    private Panel panelProgress = null!;
    private Label lblProgressText = null!;
    private ProgressBar progressBarTransfer = null!;
    private Button btnCancelTransfer = null!;
    private CancellationTokenSource? _transferCts;

    private Panel panelListContainer = null!;
    private ListView lvFiles = null!;
    private ContextMenuStrip ctxMenuFiles = null!;

    private Panel panelExploradorBottom = null!;
    private Label lblFooterStatus = null!;

    // Datos de sesión activa
    private string _activeHost = string.Empty;
    private int _activePort = 22;
    private string _activeUser = string.Empty;
    private bool _isConnecting = false;
    private bool _isBusyTransfer = false;

    private static readonly string LastRemoteConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "miVPN", "remote_last_connection.json");

    public ConexionRemotaControl() : this(new RemoteFileService(), null)
    {
    }

    public ConexionRemotaControl(IConfigService? configService) : this(new RemoteFileService(), configService)
    {
    }

    public ConexionRemotaControl(IRemoteFileService remoteFileService, IConfigService? configService)
    {
        _remoteFileService = remoteFileService;
        _configService = configService;

        InitializeComponent();
        CargarUltimaConexion();
        ShowConnectionView();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(11, 15, 25);
        this.Font = new Font("Segoe UI", 9F);

        // Header superior
        var tableHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 8, 20, 6),
            BackColor = Color.FromArgb(11, 15, 25)
        };
        tableHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblHeaderTitle = new Label
        {
            Text = "📁  Conexión Remota (Explorador SFTP)",
            Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };

        lblHeaderSub = new Label
        {
            Text = "Explora y transfiere archivos de una laptop remota que tenga activo el Modo Servidor.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 0)
        };

        tableHeader.Controls.Add(lblHeaderTitle, 0, 0);
        tableHeader.Controls.Add(lblHeaderSub, 0, 1);
        panelHeader = tableHeader;

        // Body contenedor
        panelBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(11, 15, 25)
        };

        // Construir ambas subvistas
        BuildConnectionView();
        BuildExplorerView();

        panelBody.Controls.Add(panelExplorador);
        panelBody.Controls.Add(panelConexion);

        this.Controls.Add(panelBody);
        this.Controls.Add(panelHeader);

        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #region Vista 1: Conexión

    private void BuildConnectionView()
    {
        panelConexion = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(11, 15, 25)
        };
        panelConexion.Resize += (s, e) => CenterConnectionCard();

        // Card de conexión: TableLayoutPanel vertical con ancho controlado y alto automático
        var cardLayout = new TableLayoutPanel
        {
            MinimumSize = new Size(500, 0),
            MaximumSize = new Size(500, 0),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(22, 32, 48),
            Padding = new Padding(22, 16, 22, 16),
            ColumnCount = 1,
            RowCount = 10
        };
        cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 10; i++)
        {
            cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        cardLayout.Resize += (s, e) => CenterConnectionCard();
        panelCardConexion = cardLayout;

        // Fila 0: Título de la tarjeta
        var lblCardTitle = new Label
        {
            Text = "🔑  Conexión a Laptop Remota",
            Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        cardLayout.Controls.Add(lblCardTitle, 0, 0);

        // Fila 1: Subtítulo de la tarjeta
        var lblCardSub = new Label
        {
            Text = "Ingresa los datos provistos en la sección Modo Servidor de la otra laptop.",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(456, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        cardLayout.Controls.Add(lblCardSub, 0, 1);

        // Fila 2: Bloque de Estado actual (panel interior estructurado)
        var tableStatus = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.FromArgb(30, 41, 59),
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(0, 0, 0, 8)
        };
        tableStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableStatus.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableStatus.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tableStatus.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var flowStatusHeader = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 2)
        };

        lblStatusDot = new Label
        {
            Text = "●",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 116, 139),
            AutoSize = true,
            Margin = new Padding(0, 1, 5, 0)
        };

        lblStatusTitle = new Label
        {
            Text = "Estado de sesión",
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(226, 232, 240),
            AutoSize = true,
            Margin = new Padding(0, 1, 0, 0)
        };

        flowStatusHeader.Controls.Add(lblStatusDot);
        flowStatusHeader.Controls.Add(lblStatusTitle);
        tableStatus.Controls.Add(flowStatusHeader, 0, 0);

        lblStatusState = new Label
        {
            Text = "Desconectado",
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        tableStatus.Controls.Add(lblStatusState, 0, 1);

        lblStatusSub = new Label
        {
            Text = "Listo para iniciar conexión SFTP.",
            Font = new Font("Segoe UI", 8F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(430, 0),
            Margin = new Padding(0, 0, 0, 0)
        };
        tableStatus.Controls.Add(lblStatusSub, 0, 2);

        pnlStatusBox = tableStatus;
        cardLayout.Controls.Add(pnlStatusBox, 0, 2);

        // Fila 3: Host Label
        var lblHost = new Label
        {
            Text = "Host / Túnel Remoto:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        cardLayout.Controls.Add(lblHost, 0, 3);

        // Fila 4: Host TextBox
        txtHost = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0, 0, 0, 8)
        };
        txtHost.TextChanged += TxtHost_TextChanged;
        cardLayout.Controls.Add(txtHost, 0, 4);

        // Fila 5: Puerto & Usuario (En dos columnas con nested TableLayoutPanel)
        var tablePortUser = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 8)
        };
        tablePortUser.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130f));
        tablePortUser.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tablePortUser.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablePortUser.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblPort = new Label
        {
            Text = "Puerto:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        tablePortUser.Controls.Add(lblPort, 0, 0);

        numPort = new NumericUpDown
        {
            Dock = DockStyle.Top,
            Height = 28,
            Minimum = 1,
            Maximum = 65535,
            Value = 2222,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 10, 0)
        };
        tablePortUser.Controls.Add(numPort, 0, 1);

        var lblUser = new Label
        {
            Text = "Usuario:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        tablePortUser.Controls.Add(lblUser, 1, 0);

        txtUser = new TextBox
        {
            Text = "tatouser",
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(0)
        };
        tablePortUser.Controls.Add(txtUser, 1, 1);
        cardLayout.Controls.Add(tablePortUser, 0, 5);

        // Fila 6: Contraseña Label
        var lblPass = new Label
        {
            Text = "Contraseña:",
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 2)
        };
        cardLayout.Controls.Add(lblPass, 0, 6);

        // Fila 7: Contraseña Input (TextBox + Botón Toggle)
        var tablePassInput = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 8)
        };
        tablePassInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tablePassInput.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44f));
        tablePassInput.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        txtPass = new TextBox
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F),
            UseSystemPasswordChar = true,
            Margin = new Padding(0, 0, 6, 0)
        };
        tablePassInput.Controls.Add(txtPass, 0, 0);

        btnTogglePass = new Button
        {
            Text = "👁️",
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        btnTogglePass.FlatAppearance.BorderSize = 0;
        btnTogglePass.Click += (s, e) =>
        {
            txtPass.UseSystemPasswordChar = !txtPass.UseSystemPasswordChar;
            btnTogglePass.Text = txtPass.UseSystemPasswordChar ? "👁️" : "🔒";
        };
        tablePassInput.Controls.Add(btnTogglePass, 1, 0);
        cardLayout.Controls.Add(tablePassInput, 0, 7);

        // Fila 8: Checkbox guardar
        chkSaveConnection = new CheckBox
        {
            Text = "Guardar esta conexión para futuras sesiones",
            ForeColor = Color.FromArgb(203, 213, 225),
            Font = new Font("Segoe UI", 8.5F),
            Cursor = Cursors.Hand,
            Checked = true,
            AutoSize = true,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 12)
        };
        cardLayout.Controls.Add(chkSaveConnection, 0, 8);

        // Fila 9: Botón Conectar (verde idéntico al botón de Inicio)
        btnConectar = new Button
        {
            Text = "▶  Conectar",
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 0)
        };
        btnConectar.FlatAppearance.BorderSize = 0;
        btnConectar.Click += BtnConectar_Click;
        cardLayout.Controls.Add(btnConectar, 0, 9);

        panelConexion.Controls.Add(panelCardConexion);
    }

    private void TxtHost_TextChanged(object? sender, EventArgs e)
    {
        // Auto-parse inteligente si el usuario pega string de conexión del Modo Servidor:
        // Ejemplos: "host:port", "host:port@user:pass", "tcp://host:port"
        string input = txtHost.Text.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        if (input.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring(6);
            txtHost.Text = input;
            return;
        }

        if (input.Contains('@'))
        {
            // Formato host:port@user:pass
            var atParts = input.Split('@');
            if (atParts.Length == 2)
            {
                string hostPortPart = atParts[0];
                string userPassPart = atParts[1];

                if (hostPortPart.Contains(':'))
                {
                    var hp = hostPortPart.Split(':');
                    txtHost.Text = hp[0];
                    if (int.TryParse(hp[1], out int p)) numPort.Value = Math.Clamp(p, 1, 65535);
                }

                if (userPassPart.Contains(':'))
                {
                    var up = userPassPart.Split(':');
                    txtUser.Text = up[0];
                    txtPass.Text = up[1];
                }
            }
        }
        else if (input.Contains(':') && !input.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            // Formato host:port
            var parts = input.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int parsedPort))
            {
                txtHost.Text = parts[0];
                numPort.Value = Math.Clamp(parsedPort, 1, 65535);
            }
        }
    }

    private void CenterConnectionCard()
    {
        if (panelConexion == null || panelCardConexion == null) return;
        int x = Math.Max(20, (panelConexion.ClientSize.Width - panelCardConexion.Width) / 2);
        int y = Math.Max(15, (panelConexion.ClientSize.Height - panelCardConexion.Height) / 2);
        panelCardConexion.Location = new Point(x, y);
    }

    private void UpdateConnectionStatus(string state, string sub, Color dotColor, Color stateColor)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => UpdateConnectionStatus(state, sub, dotColor, stateColor));
            return;
        }

        lblStatusDot.ForeColor = dotColor;
        lblStatusState.Text = state;
        lblStatusState.ForeColor = stateColor;
        lblStatusSub.Text = sub;
    }

    private async void BtnConectar_Click(object? sender, EventArgs e)
    {
        if (_isConnecting) return;

        string host = txtHost.Text.Trim();
        int port = (int)numPort.Value;
        string user = string.IsNullOrWhiteSpace(txtUser.Text) ? "tatouser" : txtUser.Text.Trim();
        string pass = txtPass.Text;

        if (string.IsNullOrWhiteSpace(host))
        {
            UpdateConnectionStatus("Error de validación", "Ingresa el host o túnel remoto antes de conectar.",
                Color.FromArgb(239, 68, 68), Color.FromArgb(239, 68, 68));
            txtHost.Focus();
            return;
        }

        _isConnecting = true;
        btnConectar.Enabled = false;
        btnConectar.Text = "⏳  Conectando...";
        UpdateConnectionStatus("Conectando...", "Negociando túnel SSH e iniciando sesión SFTP...",
            Color.FromArgb(234, 179, 8), Color.FromArgb(234, 179, 8));

        try
        {
            var (success, errorMsg) = await _remoteFileService.ConectarAsync(host, port, user, pass);

            if (success)
            {
                _activeHost = host;
                _activePort = port;
                _activeUser = user;

                UpdateConnectionStatus("Conectado", $"Sesión activa en {host}:{port}",
                    Color.FromArgb(16, 185, 129), Color.FromArgb(16, 185, 129));

                if (chkSaveConnection.Checked)
                {
                    GuardarUltimaConexion(host, port, user, pass);
                }

                ShowExplorerView();
                await CargarDirectorioAsync(_remoteFileService.CurrentPath);
            }
            else
            {
                UpdateConnectionStatus("Error de conexión",
                    string.IsNullOrWhiteSpace(errorMsg) ? "No se pudo conectar. Verifica que el Modo Servidor esté encendido y el túnel activo." : errorMsg,
                    Color.FromArgb(239, 68, 68), Color.FromArgb(239, 68, 68));
            }
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus("Error inesperado", ex.Message,
                Color.FromArgb(239, 68, 68), Color.FromArgb(239, 68, 68));
        }
        finally
        {
            _isConnecting = false;
            btnConectar.Enabled = true;
            btnConectar.Text = "▶  Conectar";
        }
    }

    #endregion

    #region Vista 2: Explorador de Archivos

    private void BuildExplorerView()
    {
        panelExplorador = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(11, 15, 25),
            Visible = false
        };

        // Barra superior de navegación y acciones responsiva
        panelExploradorTop = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.FromArgb(18, 26, 41),
            Padding = new Padding(15, 8, 15, 8),
            Margin = new Padding(0)
        };
        panelExploradorTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        panelExploradorTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panelExploradorTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Fila 1: Ruta y Navegación
        var tableNavRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 5,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 8)
        };
        tableNavRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableNavRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableNavRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableNavRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableNavRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableNavRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        btnUp = new Button
        {
            Text = "⬆  Subir nivel",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 2, 8, 2),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F)
        };
        btnUp.FlatAppearance.BorderSize = 0;
        btnUp.Click += BtnUp_Click;
        tableNavRow.Controls.Add(btnUp, 0, 0);

        var lblRuta = new Label
        {
            Text = "Ruta:",
            AutoSize = true,
            Margin = new Padding(0, 8, 6, 0),
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 9F)
        };
        tableNavRow.Controls.Add(lblRuta, 1, 0);

        txtRutaActual = new TextBox
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 5, 8, 0),
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };
        txtRutaActual.KeyDown += async (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await CargarDirectorioAsync(txtRutaActual.Text.Trim());
            }
        };
        tableNavRow.Controls.Add(txtRutaActual, 2, 0);

        btnIr = new Button
        {
            Text = "Ir",
            Size = new Size(45, 30),
            Margin = new Padding(0, 3, 6, 3),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnIr.FlatAppearance.BorderSize = 0;
        btnIr.Click += async (s, e) => await CargarDirectorioAsync(txtRutaActual.Text.Trim());
        tableNavRow.Controls.Add(btnIr, 3, 0);

        btnRefresh = new Button
        {
            Text = "🔄",
            Size = new Size(40, 30),
            Margin = new Padding(0, 3, 0, 3),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (s, e) => await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        tableNavRow.Controls.Add(btnRefresh, 4, 0);

        // Fila 2: FlowLayoutPanel de Acciones (responsive wrapping)
        var flowActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 2, 0, 0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };

        btnDownload = new Button
        {
            Text = "⬇  Descargar",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 4),
            BackColor = Color.FromArgb(14, 116, 144),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDownload.FlatAppearance.BorderSize = 0;
        btnDownload.Click += BtnDownload_Click;
        flowActions.Controls.Add(btnDownload);

        btnUpload = new Button
        {
            Text = "⬆  Subir archivo",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 4),
            BackColor = Color.FromArgb(234, 88, 12),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnUpload.FlatAppearance.BorderSize = 0;
        btnUpload.Click += BtnUpload_Click;
        flowActions.Controls.Add(btnUpload);

        btnNewFolder = new Button
        {
            Text = "+  Nueva carpeta",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 4),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnNewFolder.FlatAppearance.BorderSize = 0;
        btnNewFolder.Click += BtnNewFolder_Click;
        flowActions.Controls.Add(btnNewFolder);

        btnRename = new Button
        {
            Text = "✎  Renombrar",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 4),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRename.FlatAppearance.BorderSize = 0;
        btnRename.Click += BtnRename_Click;
        flowActions.Controls.Add(btnRename);

        btnDelete = new Button
        {
            Text = "✕  Eliminar",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 8, 4),
            BackColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += BtnDelete_Click;
        flowActions.Controls.Add(btnDelete);

        btnDisconnect = new Button
        {
            Text = "■  Desconectar",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 4),
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnDisconnect.FlatAppearance.BorderSize = 0;
        btnDisconnect.Click += BtnDisconnect_Click;
        flowActions.Controls.Add(btnDisconnect);

        panelExploradorTop.Controls.Add(tableNavRow, 0, 0);
        panelExploradorTop.Controls.Add(flowActions, 0, 1);

        // Barra de progreso (para transferencias)
        panelProgress = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(22, 32, 48),
            Padding = new Padding(15, 6, 15, 6),
            Visible = false
        };

        var tableProgress = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        tableProgress.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableProgress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        tableProgress.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tableProgress.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        lblProgressText = new Label
        {
            Text = "Transfiriendo archivo... (0%)",
            AutoSize = true,
            Margin = new Padding(0, 4, 12, 0),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5F),
            Anchor = AnchorStyles.Left
        };
        tableProgress.Controls.Add(lblProgressText, 0, 0);

        progressBarTransfer = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Height = 20,
            Margin = new Padding(0, 2, 10, 2),
            Style = ProgressBarStyle.Continuous
        };
        tableProgress.Controls.Add(progressBarTransfer, 1, 0);

        btnCancelTransfer = new Button
        {
            Text = "✕",
            Size = new Size(30, 22),
            Margin = new Padding(0, 1, 0, 1),
            BackColor = Color.FromArgb(185, 28, 28),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Right
        };
        btnCancelTransfer.FlatAppearance.BorderSize = 0;
        btnCancelTransfer.Click += (s, e) => _transferCts?.Cancel();
        tableProgress.Controls.Add(btnCancelTransfer, 2, 0);

        panelProgress.Controls.Add(tableProgress);

        // Contenedor de ListView
        panelListContainer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(15, 8, 15, 5),
            BackColor = Color.FromArgb(11, 15, 25)
        };

        lvFiles = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            GridLines = true,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5F),
            BorderStyle = BorderStyle.FixedSingle
        };

        lvFiles.Columns.Add("Nombre", 340);
        lvFiles.Columns.Add("Tipo", 160);
        lvFiles.Columns.Add("Tamaño", 110, HorizontalAlignment.Right);
        lvFiles.Columns.Add("Fecha de Modificación", 170);

        lvFiles.DoubleClick += LvFiles_DoubleClick;
        lvFiles.KeyDown += LvFiles_KeyDown;
        lvFiles.Resize += (s, e) => AdjustListViewColumns();

        // Context Menu
        ctxMenuFiles = new ContextMenuStrip();
        ctxMenuFiles.Items.Add("⬇  Descargar", null, BtnDownload_Click);
        ctxMenuFiles.Items.Add("⬆  Subir archivo aquí", null, BtnUpload_Click);
        ctxMenuFiles.Items.Add(new ToolStripSeparator());
        ctxMenuFiles.Items.Add("+  Nueva carpeta", null, BtnNewFolder_Click);
        ctxMenuFiles.Items.Add("✎  Renombrar", null, BtnRename_Click);
        ctxMenuFiles.Items.Add("✕  Eliminar", null, BtnDelete_Click);
        ctxMenuFiles.Items.Add(new ToolStripSeparator());
        ctxMenuFiles.Items.Add("🔄 Actualizar", null, async (s, e) => await CargarDirectorioAsync(_remoteFileService.CurrentPath));

        lvFiles.ContextMenuStrip = ctxMenuFiles;
        panelListContainer.Controls.Add(lvFiles);

        // Barra inferior de estado
        panelExploradorBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            BackColor = Color.FromArgb(18, 26, 41),
            Padding = new Padding(15, 6, 15, 6)
        };

        lblFooterStatus = new Label
        {
            Text = "Conectado.",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8.5F)
        };
        panelExploradorBottom.Controls.Add(lblFooterStatus);

        panelExplorador.Controls.Add(panelListContainer);
        panelExplorador.Controls.Add(panelProgress);
        panelExplorador.Controls.Add(panelExploradorTop);
        panelExplorador.Controls.Add(panelExploradorBottom);
    }

    private void AdjustListViewColumns()
    {
        if (lvFiles == null || lvFiles.Columns.Count < 4) return;
        int totalWidth = lvFiles.ClientSize.Width;
        if (totalWidth <= 0) return;

        int colTipo = Math.Clamp(totalWidth / 5, 120, 160);
        int colTamano = 100;
        int colFecha = 160;
        int colNombre = Math.Max(180, totalWidth - colTipo - colTamano - colFecha - 25);

        lvFiles.Columns[0].Width = colNombre;
        lvFiles.Columns[1].Width = colTipo;
        lvFiles.Columns[2].Width = colTamano;
        lvFiles.Columns[3].Width = colFecha;
    }

    private void ShowConnectionView()
    {
        panelExplorador.Visible = false;
        panelConexion.Visible = true;
        panelConexion.BringToFront();
        CenterConnectionCard();
    }

    private void ShowExplorerView()
    {
        panelConexion.Visible = false;
        panelExplorador.Visible = true;
        panelExplorador.BringToFront();
        AdjustListViewColumns();
    }

    private async Task CargarDirectorioAsync(string? ruta)
    {
        if (!_remoteFileService.IsConnected)
        {
            HandleDisconnectUnexpected("Se perdió la conexión con el servidor remoto.");
            return;
        }

        try
        {
            lvFiles.Items.Clear();
            lblFooterStatus.Text = "Cargando directorio...";

            var items = await _remoteFileService.ListarDirectorioAsync(ruta);
            txtRutaActual.Text = _remoteFileService.CurrentPath;

            int carpetas = 0;
            int archivos = 0;

            lvFiles.BeginUpdate();
            foreach (var item in items)
            {
                string iconPrefix = item.EsCarpeta ? "📁  " : "📄  ";
                var lvi = new ListViewItem(iconPrefix + item.Nombre)
                {
                    Tag = item
                };
                lvi.SubItems.Add(item.Tipo);
                lvi.SubItems.Add(item.TamanoFormateado);
                lvi.SubItems.Add(item.FechaModificacion.ToString("yyyy-MM-dd HH:mm:ss"));

                if (item.EsCarpeta)
                {
                    carpetas++;
                    lvi.ForeColor = Color.FromArgb(248, 250, 252);
                    lvi.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    archivos++;
                    lvi.ForeColor = Color.FromArgb(203, 213, 225);
                }

                lvFiles.Items.Add(lvi);
            }
            lvFiles.EndUpdate();

            lblFooterStatus.Text = $"Conectado a {_activeHost}:{_activePort} como {_activeUser} | Total: {items.Count} elementos ({carpetas} carpetas, {archivos} archivos)";
        }
        catch (Exception ex)
        {
            if (!_remoteFileService.IsConnected)
            {
                HandleDisconnectUnexpected($"Se perdió la conexión SFTP: {ex.Message}");
            }
            else
            {
                MessageBox.Show($"No se pudo acceder al directorio: {ex.Message}", "Error de Directorio",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private async void LvFiles_DoubleClick(object? sender, EventArgs e)
    {
        if (lvFiles.SelectedItems.Count == 0) return;
        var item = lvFiles.SelectedItems[0].Tag as ArchivoRemoto;
        if (item == null) return;

        if (item.EsCarpeta)
        {
            await CargarDirectorioAsync(item.RutaCompleta);
        }
        else
        {
            // Sugerir descarga directa al hacer doble clic en un archivo
            BtnDownload_Click(sender, e);
        }
    }

    private async void LvFiles_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            LvFiles_DoubleClick(sender, e);
        }
        else if (e.KeyCode == Keys.Back)
        {
            e.SuppressKeyPress = true;
            BtnUp_Click(sender, e);
        }
        else if (e.KeyCode == Keys.F5)
        {
            e.SuppressKeyPress = true;
            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        }
        else if (e.KeyCode == Keys.Delete)
        {
            e.SuppressKeyPress = true;
            BtnDelete_Click(sender, e);
        }
    }

    private async void BtnUp_Click(object? sender, EventArgs e)
    {
        if (!_remoteFileService.IsConnected)
        {
            HandleDisconnectUnexpected("No hay sesión activa.");
            return;
        }

        try
        {
            await _remoteFileService.SubirDirectorioPadreAsync();
            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al subir de nivel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnDownload_Click(object? sender, EventArgs e)
    {
        if (lvFiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Por favor selecciona un archivo para descargar.", "Descargar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = lvFiles.SelectedItems[0].Tag as ArchivoRemoto;
        if (item == null) return;

        if (item.EsCarpeta)
        {
            MessageBox.Show("Por favor selecciona un archivo para descargar. (Para descargar carpetas completas, ingresa a ella y descarga sus archivos).",
                "Descarga de carpetas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            FileName = item.Nombre,
            Title = "Guardar archivo descargado en...",
            Filter = "Todos los archivos (*.*)|*.*"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        string localPath = sfd.FileName;
        _transferCts = new CancellationTokenSource();
        SetTransferProgress(true, $"Descargando {item.Nombre}...");

        try
        {
            await _remoteFileService.DescargarArchivoAsync(item.RutaCompleta, localPath, pct =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => UpdateTransferProgress(pct, $"Descargando {item.Nombre}... ({pct:F0}%)"));
                }
                else
                {
                    UpdateTransferProgress(pct, $"Descargando {item.Nombre}... ({pct:F0}%)");
                }
            }, _transferCts.Token);

            MessageBox.Show($"Archivo descargado exitosamente en:\n{localPath}", "Descarga completada",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Descarga cancelada por el usuario.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try { if (File.Exists(localPath)) File.Delete(localPath); } catch { }
        }
        catch (Exception ex)
        {
            if (!_remoteFileService.IsConnected)
            {
                HandleDisconnectUnexpected("Se perdió la conexión durante la descarga.");
            }
            else
            {
                MessageBox.Show($"Error al descargar el archivo: {ex.Message}", "Error de descarga",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetTransferProgress(false);
            _transferCts = null;
        }
    }

    private async void BtnUpload_Click(object? sender, EventArgs e)
    {
        if (!_remoteFileService.IsConnected)
        {
            HandleDisconnectUnexpected("No hay sesión activa.");
            return;
        }

        using var ofd = new OpenFileDialog
        {
            Title = "Selecciona el archivo local a subir al servidor remoto",
            Filter = "Todos los archivos (*.*)|*.*",
            Multiselect = false
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        string localPath = ofd.FileName;
        string fileName = Path.GetFileName(localPath);
        string remoteDir = _remoteFileService.CurrentPath.TrimEnd('/');
        string remotePath = $"{remoteDir}/{fileName}";

        _transferCts = new CancellationTokenSource();
        SetTransferProgress(true, $"Subiendo {fileName}...");

        try
        {
            await _remoteFileService.SubirArchivoAsync(localPath, remotePath, pct =>
            {
                if (InvokeRequired)
                {
                    BeginInvoke(() => UpdateTransferProgress(pct, $"Subiendo {fileName}... ({pct:F0}%)"));
                }
                else
                {
                    UpdateTransferProgress(pct, $"Subiendo {fileName}... ({pct:F0}%)");
                }
            }, _transferCts.Token);

            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
            MessageBox.Show($"Archivo '{fileName}' subido exitosamente.", "Subida completada",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Subida cancelada por el usuario.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            if (!_remoteFileService.IsConnected)
            {
                HandleDisconnectUnexpected("Se perdió la conexión durante la subida.");
            }
            else
            {
                MessageBox.Show($"Error al subir el archivo: {ex.Message}", "Error de subida",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            SetTransferProgress(false);
            _transferCts = null;
        }
    }

    private async void BtnNewFolder_Click(object? sender, EventArgs e)
    {
        if (!_remoteFileService.IsConnected)
        {
            HandleDisconnectUnexpected("No hay sesión activa.");
            return;
        }

        string nombreCarpeta = ShowPromptDialog("Nueva Carpeta", "Ingresa el nombre de la nueva carpeta:", "NuevaCarpeta");
        if (string.IsNullOrWhiteSpace(nombreCarpeta)) return;

        string remoteDir = _remoteFileService.CurrentPath.TrimEnd('/');
        string target = $"{remoteDir}/{nombreCarpeta.Trim()}";

        try
        {
            await _remoteFileService.CrearCarpetaAsync(target);
            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo crear la carpeta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnRename_Click(object? sender, EventArgs e)
    {
        if (lvFiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Selecciona el archivo o carpeta que deseas renombrar.", "Renombrar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = lvFiles.SelectedItems[0].Tag as ArchivoRemoto;
        if (item == null) return;

        string nuevoNombre = ShowPromptDialog("Renombrar", $"Nuevo nombre para '{item.Nombre}':", item.Nombre);
        if (string.IsNullOrWhiteSpace(nuevoNombre) || nuevoNombre == item.Nombre) return;

        string remoteDir = _remoteFileService.CurrentPath.TrimEnd('/');
        string newPath = $"{remoteDir}/{nuevoNombre.Trim()}";

        try
        {
            await _remoteFileService.RenombrarAsync(item.RutaCompleta, newPath);
            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo renombrar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (lvFiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Selecciona el archivo o carpeta que deseas eliminar.", "Eliminar",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = lvFiles.SelectedItems[0].Tag as ArchivoRemoto;
        if (item == null) return;

        string tipoTexto = item.EsCarpeta ? "la carpeta (y todo su contenido)" : "el archivo";
        var confirm = MessageBox.Show($"¿Estás seguro de que deseas eliminar permanentemente {tipoTexto} '{item.Nombre}'?",
            "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await _remoteFileService.EliminarAsync(item.RutaCompleta, item.EsCarpeta);
            await CargarDirectorioAsync(_remoteFileService.CurrentPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No se pudo eliminar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        if (_isBusyTransfer)
        {
            var r = MessageBox.Show("Hay una transferencia en progreso. ¿Deseas cancelarla y desconectar?",
                "Transferencia activa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;
            _transferCts?.Cancel();
        }

        _remoteFileService.Desconectar();
        UpdateConnectionStatus("Desconectado", "La sesión SFTP ha sido finalizada correctamente.",
            Color.FromArgb(100, 116, 139), Color.FromArgb(148, 163, 184));
        ShowConnectionView();
    }

    private void HandleDisconnectUnexpected(string mensaje)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => HandleDisconnectUnexpected(mensaje));
            return;
        }

        _remoteFileService.Desconectar();
        UpdateConnectionStatus("Desconectado por error", mensaje,
            Color.FromArgb(239, 68, 68), Color.FromArgb(239, 68, 68));
        ShowConnectionView();
    }

    private void SetTransferProgress(bool visible, string initialText = "")
    {
        panelProgress.Visible = visible;
        _isBusyTransfer = visible;
        btnDownload.Enabled = !visible;
        btnUpload.Enabled = !visible;
        btnDelete.Enabled = !visible;
        btnRename.Enabled = !visible;
        btnNewFolder.Enabled = !visible;
        if (visible)
        {
            lblProgressText.Text = initialText;
            progressBarTransfer.Value = 0;
        }
    }

    private void UpdateTransferProgress(double pct, string text)
    {
        lblProgressText.Text = text;
        progressBarTransfer.Value = Math.Clamp((int)pct, 0, 100);
    }

    private static string ShowPromptDialog(string title, string prompt, string defaultValue = "")
    {
        using var form = new Form
        {
            Width = 420,
            Height = 180,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = Color.FromArgb(22, 32, 48),
            ForeColor = Color.White
        };

        var label = new Label
        {
            Left = 20,
            Top = 15,
            Width = 360,
            Text = prompt,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 9F)
        };

        var textBox = new TextBox
        {
            Left = 20,
            Top = 45,
            Width = 360,
            Text = defaultValue,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 9.5F)
        };

        var buttonOk = new Button
        {
            Text = "Aceptar",
            Left = 200,
            Width = 85,
            Top = 85,
            Height = 32,
            DialogResult = DialogResult.OK,
            BackColor = Color.FromArgb(22, 163, 74),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        buttonOk.FlatAppearance.BorderSize = 0;

        var buttonCancel = new Button
        {
            Text = "Cancelar",
            Left = 295,
            Width = 85,
            Top = 85,
            Height = 32,
            DialogResult = DialogResult.Cancel,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        buttonCancel.FlatAppearance.BorderSize = 0;

        form.Controls.Add(label);
        form.Controls.Add(textBox);
        form.Controls.Add(buttonOk);
        form.Controls.Add(buttonCancel);
        form.AcceptButton = buttonOk;
        form.CancelButton = buttonCancel;

        return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : string.Empty;
    }

    #endregion

    #region Persistencia de Configuración

    private void CargarUltimaConexion()
    {
        try
        {
            if (File.Exists(LastRemoteConfigPath))
            {
                string json = File.ReadAllText(LastRemoteConfigPath);
                var config = JsonSerializer.Deserialize<RemoteConnectionData>(json);
                if (config != null)
                {
                    txtHost.Text = config.Host;
                    numPort.Value = Math.Clamp(config.Port, 1, 65535);
                    txtUser.Text = string.IsNullOrWhiteSpace(config.Username) ? "tatouser" : config.Username;
                    txtPass.Text = DecryptPassword(config.Password);
                }
            }
        }
        catch { }
    }

    private void GuardarUltimaConexion(string host, int port, string user, string pass)
    {
        try
        {
            string dir = Path.GetDirectoryName(LastRemoteConfigPath)!;
            Directory.CreateDirectory(dir);

            var data = new RemoteConnectionData
            {
                Host = host,
                Port = port,
                Username = user,
                Password = EncryptPassword(pass)
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LastRemoteConfigPath, json);

            // Guardar también en "Mis configuraciones" a través de IConfigService
            if (_configService != null)
            {
                string configName = $"[Remoto] {host}:{port}";
                var settings = new ConnectionSettings
                {
                    SshHost = host,
                    SshPort = port,
                    Username = user,
                    Password = pass
                };
                _configService.Save(configName, settings);
            }
        }
        catch { }
    }

    private static string EncryptPassword(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;
        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return "enc:" + Convert.ToBase64String(cipherBytes);
        }
        catch
        {
            return plainText;
        }
    }

    private static string DecryptPassword(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;
        if (!cipherText.StartsWith("enc:")) return cipherText;
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText.Substring(4));
            byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private class RemoteConnectionData
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 2222;
        public string Username { get; set; } = "tatouser";
        public string Password { get; set; } = string.Empty;
    }

    #endregion
}
