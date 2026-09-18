using miVPN.Controls;
using miVPN.Enums;

namespace miVPN;

public partial class Form1
{
    private EscritorioRemotoControl _escritorioRemotoControl = null!;

    private void InitializeEscritorioRemoto()
    {
        _escritorioRemotoControl = new EscritorioRemotoControl
        {
            Dock    = DockStyle.Fill,
            Visible = false
        };
        _escritorioRemotoControl.ConnectionStateChanged += EscritorioRemoto_ConnectionStateChanged;
        panelMain.Controls.Add(_escritorioRemotoControl);
    }

    private void EscritorioRemoto_ConnectionStateChanged(bool isConnected, string info)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => EscritorioRemoto_ConnectionStateChanged(isConnected, info));
            return;
        }

        if (isConnected)
        {
            lblSidebarDot.ForeColor = Color.FromArgb(34, 197, 94);
            lblSidebarStatusState.Text = "Conectado";
            lblSidebarStatusState.ForeColor = Color.FromArgb(34, 197, 94);
            lblSidebarStatusSub.Text = $"Escritorio: {info}";
        }
        else
        {
            // Restaurar estado visual según VPN
            bool vpnConnected = _currentState == ConnectionState.SshAuthenticated || _currentState == ConnectionState.SocksProxyActive;
            bool vpnReconnecting = _currentState == ConnectionState.Reconnecting;
            bool vpnConnecting = _currentState == ConnectionState.Connecting;

            lblSidebarDot.ForeColor = vpnConnected
                ? Color.FromArgb(34, 197, 94)
                : (vpnReconnecting ? Color.FromArgb(249, 115, 22) : Color.FromArgb(100, 116, 139));

            lblSidebarStatusState.Text = vpnConnected
                ? "Conectado"
                : (vpnReconnecting ? "Reconectando..." : (vpnConnecting ? "Conectando..." : "Desconectado"));

            lblSidebarStatusState.ForeColor = vpnConnected
                ? Color.FromArgb(34, 197, 94)
                : (vpnReconnecting ? Color.FromArgb(249, 115, 22) : (vpnConnecting ? Color.FromArgb(234, 179, 8) : Color.FromArgb(148, 163, 184)));

            lblSidebarStatusSub.Text = vpnConnected
                ? "Túnel VPN activo"
                : (vpnReconnecting ? "Recuperando túnel caído..." : (vpnConnecting ? "Intento de conexión en curso..." : "No hay conexión activa"));
        }
    }

    private void btnNavEscritorioRemoto_Click(object? sender, EventArgs e)
    {
        panelInicio.Visible     = false;
        panelTunnelType.Visible = false;
        panelSniConfig.Visible  = false;
        panelConfigSsh.Visible  = false;
        panelRegistro.Visible   = false;
        panelConfigs.Visible    = false;
        panelModoServidor.Visible = false;
        panelAcercaDe.Visible   = false;
        if (panelFiltroContenido != null) panelFiltroContenido.Visible = false;
        if (panelDashboard       != null) panelDashboard.Visible       = false;
        if (_conexionRemotaControl != null) _conexionRemotaControl.Visible = false;

        if (_escritorioRemotoControl != null)
        {
            _escritorioRemotoControl.Visible = true;
            _escritorioRemotoControl.BringToFront();
        }

        SetNavButtonActive(btnNavEscritorioRemoto);
    }
}
