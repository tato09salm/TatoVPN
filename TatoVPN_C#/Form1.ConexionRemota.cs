using miVPN.Controls;

namespace miVPN;

public partial class Form1
{
    private ConexionRemotaControl _conexionRemotaControl = null!;

    private void InitializeConexionRemota()
    {
        _conexionRemotaControl = new ConexionRemotaControl(_configService)
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        panelMain.Controls.Add(_conexionRemotaControl);
    }

    private void btnNavConexionRemota_Click(object? sender, EventArgs e)
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
        if (panelDashboard != null) panelDashboard.Visible = false;

        if (_conexionRemotaControl != null)
        {
            _conexionRemotaControl.Visible = true;
            _conexionRemotaControl.BringToFront();
        }

        SetNavButtonActive(btnNavConexionRemota);
    }
}
