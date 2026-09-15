#nullable enable

namespace GTE.Instalador;

public sealed partial class FrmPrincipal
{
    private System.ComponentModel.IContainer? components = null;

    private Label lblAvisoAdmin = null!;
    private TextBox txtNombreServicio = null!;
    private Button btnCargar = null!;
    private Label lblEstadoServicio = null!;
    private Button btnIniciar = null!;
    private Button btnDetener = null!;
    private Button btnReiniciar = null!;
    private TextBox txtRutaExe = null!;
    private Button btnExaminarExe = null!;
    private TextBox txtNombreMostrar = null!;
    private Button btnCrearServicio = null!;
    private ComboBox cboAmbiente = null!;
    private TextBox txtServidorSql = null!;
    private TextBox txtUsuarioSql = null!;
    private TextBox txtPasswordSql = null!;
    private CheckBox chkMostrarPassword = null!;
    private Button btnProbarConexion = null!;
    private Label lblResultadoConexion = null!;
    private TextBox txtJwtClave = null!;
    private CheckBox chkMostrarJwt = null!;
    private Button btnGenerarJwt = null!;
    private Button btnCopiarJwt = null!;
    private TextBox txtRutaAlmacen = null!;
    private Button btnExaminarCarpeta = null!;
    private Button btnProbarEscritura = null!;
    private Label lblResultadoAlmacen = null!;
    private CheckBox chkReiniciarAlGuardar = null!;
    private Button btnGuardar = null!;
    private TextBox txtLog = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        Text = "GTE - Instalador y configuracion";
        Font = new Font("Segoe UI", 9F);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 700);
        Size = new Size(880, 820);

        var tablaPrincipal = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(12)
        };
        tablaPrincipal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tablaPrincipal.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablaPrincipal.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablaPrincipal.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablaPrincipal.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tablaPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(tablaPrincipal);

        lblAvisoAdmin = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.MistyRose,
            ForeColor = Color.Firebrick,
            Padding = new Padding(8),
            Text = "Esta herramienta necesita ejecutarse como Administrador para poder guardar cambios.",
            Visible = false
        };
        tablaPrincipal.Controls.Add(lblAvisoAdmin, 0, 0);

        tablaPrincipal.Controls.Add(CrearGrupoServicio(), 0, 1);
        tablaPrincipal.Controls.Add(CrearGrupoConfiguracion(), 0, 2);
        tablaPrincipal.Controls.Add(CrearPanelGuardar(), 0, 3);
        tablaPrincipal.Controls.Add(CrearGrupoLog(), 0, 4);
    }

    private static TableLayoutPanel CrearTablaFormulario(int columnas)
    {
        var tabla = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columnas,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        for (var i = 1; i < columnas; i++)
        {
            tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / (columnas - 1)));
        }

        return tabla;
    }

    private static Label CrearEtiqueta(string texto) => new()
    {
        Text = texto,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 8, 3, 3)
    };

    private GroupBox CrearGrupoServicio()
    {
        var grupo = new GroupBox
        {
            Text = "Servicio de Windows",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 10)
        };

        var tabla = CrearTablaFormulario(2);
        var fila = 0;

        txtNombreServicio = new TextBox { Text = "GTE", Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5) };
        btnCargar = new Button { Text = "Cargar configuracion", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnCargar.Click += BtnCargar_Click;
        var panelNombre = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
        panelNombre.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelNombre.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelNombre.Controls.Add(txtNombreServicio, 0, 0);
        panelNombre.Controls.Add(btnCargar, 1, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Nombre del servicio:"), 0, fila);
        tabla.Controls.Add(panelNombre, 1, fila);
        fila++;

        lblEstadoServicio = new Label { Text = "Estado: (sin consultar)", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 3) };
        var panelAcciones = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(3) };
        btnIniciar = new Button { Text = "Iniciar", AutoSize = true };
        btnDetener = new Button { Text = "Detener", AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
        btnReiniciar = new Button { Text = "Reiniciar", AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
        btnIniciar.Click += BtnIniciar_Click;
        btnDetener.Click += BtnDetener_Click;
        btnReiniciar.Click += BtnReiniciar_Click;
        panelAcciones.Controls.Add(btnIniciar);
        panelAcciones.Controls.Add(btnDetener);
        panelAcciones.Controls.Add(btnReiniciar);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(lblEstadoServicio, 0, fila);
        tabla.Controls.Add(panelAcciones, 1, fila);
        fila++;

        var lblCrear = new Label
        {
            Text = "Crear servicio nuevo (solo la primera vez, si todavia no existe):",
            AutoSize = true,
            Margin = new Padding(3, 12, 3, 3)
        };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(lblCrear, 0, fila);
        tabla.SetColumnSpan(lblCrear, 2);
        fila++;

        txtRutaExe = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5) };
        btnExaminarExe = new Button { Text = "Examinar...", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnExaminarExe.Click += BtnExaminarExe_Click;
        var panelExe = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
        panelExe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelExe.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelExe.Controls.Add(txtRutaExe, 0, 0);
        panelExe.Controls.Add(btnExaminarExe, 1, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Ejecutable (.exe):"), 0, fila);
        tabla.Controls.Add(panelExe, 1, fila);
        fila++;

        txtNombreMostrar = new TextBox { Text = "GTE - Gestor Tecnologico Empresarial", Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5) };
        btnCrearServicio = new Button { Text = "Crear servicio", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnCrearServicio.Click += BtnCrearServicio_Click;
        var panelCrear = new TableLayoutPanel { ColumnCount = 2, Dock = DockStyle.Fill, AutoSize = true };
        panelCrear.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelCrear.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelCrear.Controls.Add(txtNombreMostrar, 0, 0);
        panelCrear.Controls.Add(btnCrearServicio, 1, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Nombre para mostrar:"), 0, fila);
        tabla.Controls.Add(panelCrear, 1, fila);

        grupo.Controls.Add(tabla);
        return grupo;
    }

    private GroupBox CrearGrupoConfiguracion()
    {
        var grupo = new GroupBox
        {
            Text = "Configuracion de la aplicacion (variables de entorno del servicio)",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 10)
        };

        var tabla = CrearTablaFormulario(2);
        var fila = 0;

        cboAmbiente = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, Margin = new Padding(3, 5, 3, 5) };
        cboAmbiente.Items.AddRange(["Production", "Development", "Staging"]);
        cboAmbiente.Text = "Production";
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("ASPNETCORE_ENVIRONMENT:"), 0, fila);
        tabla.Controls.Add(cboAmbiente, 1, fila);
        fila++;

        txtServidorSql = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5), PlaceholderText = @"SRVPROD\NASA" };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Servidor SQL:"), 0, fila);
        tabla.Controls.Add(txtServidorSql, 1, fila);
        fila++;

        txtUsuarioSql = new TextBox { Text = "svc_gte", Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5) };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Usuario SQL:"), 0, fila);
        tabla.Controls.Add(txtUsuarioSql, 1, fila);
        fila++;

        txtPasswordSql = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5), UseSystemPasswordChar = true };
        chkMostrarPassword = new CheckBox { Text = "Mostrar", AutoSize = true, Margin = new Padding(6, 6, 3, 3) };
        chkMostrarPassword.CheckedChanged += ChkMostrarPassword_CheckedChanged;
        btnProbarConexion = new Button { Text = "Probar conexion", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnProbarConexion.Click += BtnProbarConexion_Click;
        var panelPassword = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, AutoSize = true };
        panelPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelPassword.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelPassword.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelPassword.Controls.Add(txtPasswordSql, 0, 0);
        panelPassword.Controls.Add(chkMostrarPassword, 1, 0);
        panelPassword.Controls.Add(btnProbarConexion, 2, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Password SQL:"), 0, fila);
        tabla.Controls.Add(panelPassword, 1, fila);
        fila++;

        lblResultadoConexion = new Label { Text = string.Empty, AutoSize = true, Margin = new Padding(3, 3, 3, 8) };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(new Label(), 0, fila);
        tabla.Controls.Add(lblResultadoConexion, 1, fila);
        fila++;

        txtJwtClave = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5), UseSystemPasswordChar = true };
        chkMostrarJwt = new CheckBox { Text = "Mostrar", AutoSize = true, Margin = new Padding(6, 6, 3, 3) };
        chkMostrarJwt.CheckedChanged += ChkMostrarJwt_CheckedChanged;
        btnGenerarJwt = new Button { Text = "Generar nueva", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnGenerarJwt.Click += BtnGenerarJwt_Click;
        btnCopiarJwt = new Button { Text = "Copiar", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnCopiarJwt.Click += BtnCopiarJwt_Click;
        var panelJwt = new TableLayoutPanel { ColumnCount = 4, Dock = DockStyle.Fill, AutoSize = true };
        panelJwt.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelJwt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelJwt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelJwt.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelJwt.Controls.Add(txtJwtClave, 0, 0);
        panelJwt.Controls.Add(chkMostrarJwt, 1, 0);
        panelJwt.Controls.Add(btnGenerarJwt, 2, 0);
        panelJwt.Controls.Add(btnCopiarJwt, 3, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Clave de firma JWT:"), 0, fila);
        tabla.Controls.Add(panelJwt, 1, fila);
        fila++;

        txtRutaAlmacen = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 3, 5), PlaceholderText = @"C:\GTE\Archivos" };
        btnExaminarCarpeta = new Button { Text = "Examinar...", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnExaminarCarpeta.Click += BtnExaminarCarpeta_Click;
        btnProbarEscritura = new Button { Text = "Crear y probar escritura", AutoSize = true, Margin = new Padding(6, 3, 3, 3) };
        btnProbarEscritura.Click += BtnProbarEscritura_Click;
        var panelAlmacen = new TableLayoutPanel { ColumnCount = 3, Dock = DockStyle.Fill, AutoSize = true };
        panelAlmacen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panelAlmacen.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelAlmacen.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panelAlmacen.Controls.Add(txtRutaAlmacen, 0, 0);
        panelAlmacen.Controls.Add(btnExaminarCarpeta, 1, 0);
        panelAlmacen.Controls.Add(btnProbarEscritura, 2, 0);

        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(CrearEtiqueta("Ruta de almacen de archivos:"), 0, fila);
        tabla.Controls.Add(panelAlmacen, 1, fila);
        fila++;

        lblResultadoAlmacen = new Label { Text = string.Empty, AutoSize = true, MaximumSize = new Size(560, 0), Margin = new Padding(3, 3, 3, 3) };
        tabla.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tabla.Controls.Add(new Label(), 0, fila);
        tabla.Controls.Add(lblResultadoAlmacen, 1, fila);

        grupo.Controls.Add(tabla);
        return grupo;
    }

    private Panel CrearPanelGuardar()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 0, 10)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        chkReiniciarAlGuardar = new CheckBox
        {
            Text = "Reiniciar el servicio al guardar (recomendado: sin reinicio, el cambio no aplica)",
            AutoSize = true,
            Checked = true,
            Anchor = AnchorStyles.Left
        };
        panel.Controls.Add(chkReiniciarAlGuardar, 0, 0);

        btnGuardar = new Button
        {
            Text = "Guardar y aplicar",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };
        btnGuardar.Click += BtnGuardar_Click;
        panel.Controls.Add(btnGuardar, 1, 0);

        return panel;
    }

    private GroupBox CrearGrupoLog()
    {
        var grupo = new GroupBox
        {
            Text = "Registro de actividad",
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };

        txtLog = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9F)
        };
        grupo.Controls.Add(txtLog);
        return grupo;
    }
}
