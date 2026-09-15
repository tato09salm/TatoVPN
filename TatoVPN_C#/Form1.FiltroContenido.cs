using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using miVPN.Services.Interfaces;

namespace miVPN;

public partial class Form1
{
    private Panel panelFiltroContenido = null!;
    private FlowLayoutPanel _panelCategorias = null!;
    private ListBox lbDominios = null!;
    private TextBox txtNuevoDominio = null!;
    private CheckBox chkHabilitarFiltro = null!;
    private bool _isBuildingUi = false;

    private void InitializeFiltroContenido()
    {
        panelFiltroContenido = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(11, 15, 25),
        };

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
            Text = "Filtro de Contenido",
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.White
        };

        var lblSub = new Label
        {
            Text = "Bloquea sitios web de forma selectiva. Si un sitio está en la lista, su tráfico (DNS) no saldrá por la VPN.",
            AutoSize = true,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = Color.FromArgb(148, 163, 184),
            Margin = new Padding(0, 5, 0, 15)
        };

        chkHabilitarFiltro = new CheckBox
        {
            Text = "Activar Filtrado de Contenido globalmente",
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(16, 185, 129),
            Cursor = Cursors.Hand
        };
        chkHabilitarFiltro.CheckedChanged += (s, e) => {
            if (!_isBuildingUi)
            {
                _contentFilterService.HabilitarFiltrado = chkHabilitarFiltro.Checked;
                GuardarCambiosSilencioso();
            }
        };

        flowHeader.Controls.Add(lblTitle);
        flowHeader.Controls.Add(lblSub);
        flowHeader.Controls.Add(chkHabilitarFiltro);

        var tableMain = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(15)
        };
        tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        tableMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

        // Left Col Row 1
        var panelLeft = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 15, 10) };
        var lblCategorias = new Label
        {
            Text = "Categorías Predefinidas",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 10)
        };
        _panelCategorias = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 5, 0, 0)
        };
        
        panelLeft.Controls.Add(_panelCategorias);
        panelLeft.Controls.Add(lblCategorias);
        _panelCategorias.BringToFront();

        // Right Col Row 1
        var panelRight = new Panel { Dock = DockStyle.Fill, Margin = new Padding(15, 0, 0, 10) };
        var lblDominios = new Label
        {
            Text = "Mis Bloqueos Personalizados",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 10)
        };

        var panelInput = new Panel { Dock = DockStyle.Top, Height = 35 };
        txtNuevoDominio = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "ejemplo.com"
        };
        var btnAgregarDominio = new Button
        {
            Text = "Agregar",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10, 0, 10, 0),
            Dock = DockStyle.Right,
            BackColor = Color.FromArgb(37, 99, 235),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnAgregarDominio.FlatAppearance.BorderSize = 0;
        btnAgregarDominio.Click += BtnAgregarDominio_Click;
        panelInput.Controls.Add(txtNuevoDominio);
        panelInput.Controls.Add(btnAgregarDominio);
        txtNuevoDominio.BringToFront();

        lbDominios = new ListBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(15, 23, 42),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10F),
            BorderStyle = BorderStyle.None
        };

        panelRight.Controls.Add(lbDominios);
        panelRight.Controls.Add(panelInput);
        panelRight.Controls.Add(lblDominios);
        lbDominios.BringToFront();

        // Left Col Row 2
        var btnGuardar = new Button
        {
            Text = "💾 Guardar Cambios",
            Size = new Size(200, 40),
            BackColor = Color.FromArgb(16, 185, 129),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 5, 0, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        btnGuardar.FlatAppearance.BorderSize = 0;
        btnGuardar.Click += BtnGuardar_Click;

        // Right Col Row 2
        var btnQuitarDominio = new Button
        {
            Text = "Quitar Seleccionado",
            Size = new Size(180, 40),
            BackColor = Color.FromArgb(220, 38, 38),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(15, 5, 0, 0),
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        btnQuitarDominio.FlatAppearance.BorderSize = 0;
        btnQuitarDominio.Click += BtnQuitarDominio_Click;

        tableMain.Controls.Add(panelLeft, 0, 0);
        tableMain.Controls.Add(panelRight, 1, 0);
        tableMain.Controls.Add(btnGuardar, 0, 1);
        tableMain.Controls.Add(btnQuitarDominio, 1, 1);

        panelFiltroContenido.Controls.Add(tableMain);
        panelFiltroContenido.Controls.Add(flowHeader);
        
        tableMain.BringToFront();

        var panelMain = this.Controls.Find("panelMain", true).FirstOrDefault() as Panel;
        if (panelMain != null)
        {
            panelMain.Controls.Add(panelFiltroContenido);
        }

        CargarUI();
    }

    private void CargarUI()
    {
        _isBuildingUi = true;
        try
        {
            chkHabilitarFiltro.Checked = _contentFilterService.HabilitarFiltrado;
            _panelCategorias.Controls.Clear();
            lbDominios.Items.Clear();

            string catPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "categorias_filtro.json");
            if (File.Exists(catPath))
            {
                var activados = new HashSet<string>(_contentFilterService.GetSitiosActivados(), StringComparer.OrdinalIgnoreCase);
                using var doc = JsonDocument.Parse(File.ReadAllText(catPath));
                if (doc.RootElement.TryGetProperty("categorias", out var cats))
                {
                    foreach (var cat in cats.EnumerateArray())
                    {
                        string cNombre = cat.GetProperty("nombre").GetString() ?? "";
                        string cIcon = cat.TryGetProperty("icono", out var propIcon) ? propIcon.GetString() : "";
                        
                        var gb = new GroupBox
                        {
                            Text = $"{cIcon} {cNombre}",
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            MinimumSize = new Size(280, 0),
                            Margin = new Padding(0, 0, 15, 15)
                        };
                        
                        var flowChecks = new FlowLayoutPanel
                        {
                            Dock = DockStyle.Fill,
                            FlowDirection = FlowDirection.TopDown,
                            WrapContents = false,
                            AutoSize = true,
                            Padding = new Padding(10, 20, 10, 10)
                        };

                        if (cat.TryGetProperty("sitios", out var sitios))
                        {
                            foreach (var sitio in sitios.EnumerateArray())
                            {
                                string sNombre = sitio.GetProperty("nombre").GetString() ?? "";
                                var chk = new CheckBox
                                {
                                    Text = sNombre,
                                    ForeColor = Color.FromArgb(200, 200, 200),
                                    Font = new Font("Segoe UI", 9.5F),
                                    AutoSize = true,
                                    Checked = activados.Contains(sNombre),
                                    Tag = cNombre + "|" + sNombre
                                };
                                chk.CheckedChanged += ChkSitio_CheckedChanged;
                                flowChecks.Controls.Add(chk);
                            }
                        }
                        
                        gb.Controls.Add(flowChecks);
                        _panelCategorias.Controls.Add(gb);
                    }
                }
            }
            else
            {
                _logger.Log("⚠️ No se encontró categorias_filtro.json en el directorio de ejecución.");
            }

            foreach (var d in _contentFilterService.GetDominiosPersonalizados())
            {
                lbDominios.Items.Add(d);
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al generar UI de filtrado: {ex.Message}");
        }
        finally
        {
            _isBuildingUi = false;
        }
    }

    private void ChkSitio_CheckedChanged(object? sender, EventArgs e)
    {
        if (_isBuildingUi) return;
        if (sender is CheckBox chk && chk.Tag is string tagStr)
        {
            var parts = tagStr.Split('|');
            if (parts.Length == 2)
            {
                string categoria = parts[0];
                string sitio = parts[1];
                _contentFilterService.ActivarSitio(categoria, sitio, chk.Checked);
                GuardarCambiosSilencioso();
            }
        }
    }

    private void BtnAgregarDominio_Click(object? sender, EventArgs e)
    {
        string dom = txtNuevoDominio.Text.Trim();
        if (!string.IsNullOrEmpty(dom))
        {
            _contentFilterService.AgregarDominioPersonalizado(dom);
            if (!lbDominios.Items.Contains(dom))
                lbDominios.Items.Add(dom);
            txtNuevoDominio.Clear();
            GuardarCambiosSilencioso();
        }
    }

    private void BtnQuitarDominio_Click(object? sender, EventArgs e)
    {
        if (lbDominios.SelectedItem != null)
        {
            string dom = lbDominios.SelectedItem.ToString();
            _contentFilterService.QuitarDominioPersonalizado(dom);
            lbDominios.Items.Remove(lbDominios.SelectedItem);
            GuardarCambiosSilencioso();
        }
    }
    
    private void GuardarCambiosSilencioso()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filtro_contenido.json");
            _contentFilterService.GuardarConfiguracion(configPath);
        }
        catch (Exception ex)
        {
            _logger.Log($"⚠️ Error al auto-guardar cambios: {ex.Message}");
        }
    }

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "filtro_contenido.json");
        _contentFilterService.GuardarConfiguracion(configPath);
        MessageBox.Show("Configuración de filtro guardada exitosamente.", "Filtro de Contenido", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnNavFiltro_Click(object? sender, EventArgs e)
    {
        var panelMain = this.Controls.Find("panelMain", true).FirstOrDefault() as Panel;
        if (panelMain != null)
        {
            foreach (Control c in panelMain.Controls)
            {
                c.Visible = (c == panelFiltroContenido);
            }
        }
        
        SetNavButtonActive(btnNavFiltro);
    }
}
