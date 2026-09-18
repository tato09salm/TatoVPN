using miVPN.Controls;

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
        panelMain.Controls.Add(_escritorioRemotoControl);
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
