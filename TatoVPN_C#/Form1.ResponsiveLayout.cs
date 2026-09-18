namespace miVPN;

public partial class Form1
{
    private void SetupResponsiveConfigSshLayout()
    {
        panelConfigSsh.AutoScroll = true;

        // Anclar paneles de configuración para que se adapten con el ancho del formulario
        groupSsh.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupSocks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        groupAdvancedSsh.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panelConfigSshButtons.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Subtítulo expandible
        lblConfigSshSub.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblConfigSshSub.AutoSize = false;
        lblConfigSshSub.Height = 26;

        // Controles de grupo SSH
        lblTlsPort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        numTlsPort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnTogglePass.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        groupSsh.Resize += (s, e) => AdjustSshGroupInputs();

        // Controles de grupo Socks
        lblSocksPort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        numSocksPort.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        txtSocksIp.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        chkEnableTun.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Controles de grupo avanzado
        lblInternalSshDesc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // Botones de acción
        btnConnectFromConfig.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSaveConfig.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        panelConfigSshButtons.Resize += (s, e) => AdjustConfigSshButtons();

        // Aplicar ajustes iniciales
        AdjustSshGroupInputs();
        AdjustConfigSshButtons();
    }

    private void AdjustSshGroupInputs()
    {
        int totalWidth = groupSsh.ClientSize.Width;
        if (totalWidth <= 100) return;

        int spacing = 16;
        int portWidth = 125;

        // Host y Puerto
        int hostWidth = Math.Max(160, totalWidth - portWidth - (spacing * 3));
        txtSshHost.Left = spacing;
        txtSshHost.Width = hostWidth;

        int portLeft = totalWidth - portWidth - spacing;
        lblTlsPort.Left = portLeft;
        numTlsPort.Left = portLeft;
        numTlsPort.Width = portWidth;

        // Usuario y Contraseña proporcionales (50% / 50%)
        int eyeBtnWidth = 40;
        int availableForInputs = totalWidth - (spacing * 3) - eyeBtnWidth - 8;
        int userWidth = Math.Max(120, availableForInputs / 2);
        int passWidth = Math.Max(120, availableForInputs - userWidth);

        lblUsername.Left = spacing;
        txtUsername.Left = spacing;
        txtUsername.Width = userWidth;

        int passLeft = spacing + userWidth + spacing;
        lblPassword.Left = passLeft;
        txtPassword.Left = passLeft;
        txtPassword.Width = passWidth;

        btnTogglePass.Left = passLeft + passWidth + 8;
        btnTogglePass.Width = eyeBtnWidth;
    }

    private void AdjustConfigSshButtons()
    {
        int totalWidth = panelConfigSshButtons.ClientSize.Width;
        if (totalWidth <= 150) return;

        int btnConnectWidth = 210;
        int btnSaveWidth = 185;
        int spacing = 10;
        int rightMargin = 0;

        btnConnectFromConfig.Width = btnConnectWidth;
        btnConnectFromConfig.Left = totalWidth - btnConnectWidth - rightMargin;

        btnSaveConfig.Width = btnSaveWidth;
        btnSaveConfig.Left = btnConnectFromConfig.Left - btnSaveWidth - spacing;
    }

    private void SetupResponsiveRegistroLayout()
    {
        panelRegistro.Dock = DockStyle.Fill;
        panelRegistroTop.Dock = DockStyle.Top;
        rtbLogs.Dock = DockStyle.Fill;

        panelRegistroTop.Resize += (s, e) =>
        {
            int topWidth = panelRegistroTop.ClientSize.Width;
            if (topWidth <= 0) return;

            // Mantener el botón de limpiar a la derecha asegurando no solapar el título
            int btnWidth = btnClearHistory.Width;
            int rightPos = topWidth - btnWidth - 16;
            int minLeft = lblHistoryTitle.Right + 16;
            btnClearHistory.Left = Math.Max(minLeft, rightPos);
        };
    }
}
