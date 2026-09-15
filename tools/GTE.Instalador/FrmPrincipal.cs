using System.Security.Principal;
using GTE.Instalador.Servicios;
using Microsoft.Data.SqlClient;

namespace GTE.Instalador;

/// <summary>
/// Unica pantalla de la herramienta. Reemplaza a configurar-servicio-completo.bat/.ps1,
/// configurar-variable-servicio.bat/.ps1 y configurar-almacen-archivos.bat/.ps1: lee,
/// crea y modifica desde aqui las variables de entorno del servicio de Windows de GTE
/// (ver Doctos/MANUAL_INSTALACION_GTE.md, Paso 3).
/// </summary>
public sealed partial class FrmPrincipal : Form
{
    private readonly ConfiguracionServicioWindows _configuracion = new();

    public FrmPrincipal()
    {
        InitializeComponent();
        ActualizarAvisoAdministrador();
        ActualizarEstadoServicio();
    }

    private static bool EsAdministrador()
    {
        using var identidad = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identidad);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void ActualizarAvisoAdministrador()
    {
        var esAdmin = EsAdministrador();
        lblAvisoAdmin.Visible = !esAdmin;
        if (!esAdmin)
        {
            RegistrarLog(
                "Esta consola NO es de Administrador. Cierra la aplicacion y vuelve a abrirla " +
                "(el UAC deberia pedirlo solo); sin permisos de administrador, Guardar y las " +
                "acciones del servicio van a fallar.");
        }
    }

    private void RegistrarLog(string mensaje)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {mensaje}{Environment.NewLine}");
    }

    // ------------------------------------------------------------------ Servicio de Windows

    private void BtnCargar_Click(object? sender, EventArgs e)
    {
        var nombreServicio = txtNombreServicio.Text.Trim();
        if (nombreServicio.Length == 0)
        {
            MessageBox.Show(this, "Escribe el nombre del servicio.", "GTE - Instalador",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _configuracion.Cargar(nombreServicio);
        ActualizarEstadoServicio();

        if (!_configuracion.ServicioExiste)
        {
            RegistrarLog($"El servicio '{nombreServicio}' no existe todavia en esta maquina. " +
                "Crealo en la seccion de abajo antes de guardar configuracion.");
            LimpiarCamposConfiguracion();
            return;
        }

        cboAmbiente.Text = _configuracion.Ambiente ?? "Production";
        (txtServidorSql.Text, txtUsuarioSql.Text, txtPasswordSql.Text) =
            DescomponerCadenaConexion(_configuracion.CadenaConexion);
        txtJwtClave.Text = _configuracion.JwtClaveFirma ?? string.Empty;
        txtRutaAlmacen.Text = _configuracion.AlmacenArchivosRuta ?? string.Empty;

        RegistrarLog($"Configuracion cargada del servicio '{nombreServicio}'.");
    }

    private void LimpiarCamposConfiguracion()
    {
        cboAmbiente.Text = "Production";
        txtServidorSql.Text = string.Empty;
        txtUsuarioSql.Text = "svc_gte";
        txtPasswordSql.Text = string.Empty;
        txtJwtClave.Text = string.Empty;
        txtRutaAlmacen.Text = string.Empty;
    }

    private void ActualizarEstadoServicio()
    {
        var nombreServicio = txtNombreServicio.Text.Trim();
        if (nombreServicio.Length == 0)
        {
            lblEstadoServicio.Text = "Estado: (sin nombre de servicio)";
            return;
        }

        var estado = ServicioWindowsHelper.ObtenerEstado(nombreServicio);
        (lblEstadoServicio.Text, lblEstadoServicio.ForeColor) = estado switch
        {
            EstadoServicioWindows.EnEjecucion => ($"Estado: en ejecucion ({nombreServicio})", Color.DarkGreen),
            EstadoServicioWindows.Detenido => ($"Estado: detenido ({nombreServicio})", Color.DarkOrange),
            EstadoServicioWindows.Transicion => ($"Estado: cambiando... ({nombreServicio})", Color.DarkOrange),
            EstadoServicioWindows.NoExiste => ($"Estado: el servicio '{nombreServicio}' no existe", Color.Firebrick),
            _ => ($"Estado: desconocido ({nombreServicio})", Color.DimGray)
        };

        var existe = estado != EstadoServicioWindows.NoExiste;
        btnIniciar.Enabled = existe;
        btnDetener.Enabled = existe;
        btnReiniciar.Enabled = existe;
    }

    private void BtnIniciar_Click(object? sender, EventArgs e) => EjecutarAccionServicio(ServicioWindowsHelper.Iniciar, "Iniciando");

    private void BtnDetener_Click(object? sender, EventArgs e) => EjecutarAccionServicio(ServicioWindowsHelper.Detener, "Deteniendo");

    private void BtnReiniciar_Click(object? sender, EventArgs e) => EjecutarAccionServicio(ServicioWindowsHelper.Reiniciar, "Reiniciando");

    private void EjecutarAccionServicio(Func<string, (bool Exito, string Mensaje)> accion, string verbo)
    {
        var nombreServicio = txtNombreServicio.Text.Trim();
        if (nombreServicio.Length == 0)
        {
            return;
        }

        RegistrarLog($"{verbo} el servicio '{nombreServicio}'...");
        UseWaitCursor = true;
        try
        {
            var (exito, mensaje) = accion(nombreServicio);
            RegistrarLog(exito ? "OK." : $"ERROR: {mensaje}");
        }
        finally
        {
            UseWaitCursor = false;
            ActualizarEstadoServicio();
        }
    }

    private void BtnExaminarExe_Click(object? sender, EventArgs e)
    {
        using var dialogo = new OpenFileDialog
        {
            Title = "Selecciona GTE.WebApi.exe publicado",
            Filter = "Ejecutables (*.exe)|*.exe|Todos los archivos (*.*)|*.*"
        };

        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            txtRutaExe.Text = dialogo.FileName;
        }
    }

    private void BtnCrearServicio_Click(object? sender, EventArgs e)
    {
        var nombreServicio = txtNombreServicio.Text.Trim();
        var rutaExe = txtRutaExe.Text.Trim();
        var nombreMostrar = txtNombreMostrar.Text.Trim();

        if (nombreServicio.Length == 0 || rutaExe.Length == 0)
        {
            MessageBox.Show(this, "Escribe el nombre del servicio y la ruta del ejecutable.",
                "GTE - Instalador", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!File.Exists(rutaExe))
        {
            MessageBox.Show(this, $"No se encontro el archivo:\n{rutaExe}", "GTE - Instalador",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (nombreMostrar.Length == 0)
        {
            nombreMostrar = "GTE - Gestor Tecnologico Empresarial";
        }

        RegistrarLog($"Creando el servicio '{nombreServicio}' (binPath={rutaExe})...");
        var (exito, mensaje) = ServicioWindowsHelper.Crear(nombreServicio, rutaExe, nombreMostrar);
        RegistrarLog(exito ? "OK: servicio creado." : $"ERROR: {mensaje}");
        ActualizarEstadoServicio();
    }

    // ------------------------------------------------------------------ Configuracion de la app

    private static string ConstruirCadenaConexion(string servidor, string usuario, string password) =>
        $"Server={servidor};Database=bdsGTE;User Id={usuario};Password={password};" +
        "TrustServerCertificate=True;Application Name=GTE.WebApi";

    private static (string Servidor, string Usuario, string Password) DescomponerCadenaConexion(string? cadena)
    {
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return (string.Empty, "svc_gte", string.Empty);
        }

        string? servidor = null;
        string? usuario = null;
        string? password = null;

        foreach (var parte in cadena.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separador = parte.IndexOf('=');
            if (separador < 0)
            {
                continue;
            }

            var clave = parte[..separador].Trim();
            var valor = parte[(separador + 1)..].Trim();

            if (clave.Equals("Server", StringComparison.OrdinalIgnoreCase))
            {
                servidor = valor;
            }
            else if (clave.Equals("User Id", StringComparison.OrdinalIgnoreCase) || clave.Equals("UserId", StringComparison.OrdinalIgnoreCase))
            {
                usuario = valor;
            }
            else if (clave.Equals("Password", StringComparison.OrdinalIgnoreCase))
            {
                password = valor;
            }
        }

        return (servidor ?? string.Empty, usuario ?? "svc_gte", password ?? string.Empty);
    }

    private void ChkMostrarPassword_CheckedChanged(object? sender, EventArgs e) =>
        txtPasswordSql.UseSystemPasswordChar = !chkMostrarPassword.Checked;

    private void ChkMostrarJwt_CheckedChanged(object? sender, EventArgs e) =>
        txtJwtClave.UseSystemPasswordChar = !chkMostrarJwt.Checked;

    private async void BtnProbarConexion_Click(object? sender, EventArgs e)
    {
        var cadena = ConstruirCadenaConexion(txtServidorSql.Text.Trim(), txtUsuarioSql.Text.Trim(), txtPasswordSql.Text);

        btnProbarConexion.Enabled = false;
        lblResultadoConexion.Text = "Probando conexion...";
        lblResultadoConexion.ForeColor = Color.DimGray;
        UseWaitCursor = true;
        try
        {
            using var conexion = new SqlConnection(cadena);
            await conexion.OpenAsync();
            lblResultadoConexion.Text = "OK: conexion exitosa.";
            lblResultadoConexion.ForeColor = Color.DarkGreen;
            RegistrarLog($"Conexion a '{txtServidorSql.Text.Trim()}' exitosa.");
        }
        catch (Exception ex)
        {
            lblResultadoConexion.Text = $"ERROR: {ex.Message}";
            lblResultadoConexion.ForeColor = Color.Firebrick;
            RegistrarLog($"Fallo la conexion: {ex.Message}");
        }
        finally
        {
            UseWaitCursor = false;
            btnProbarConexion.Enabled = true;
        }
    }

    private void BtnGenerarJwt_Click(object? sender, EventArgs e)
    {
        txtJwtClave.Text = GeneradorClaveJwt.Generar();
        RegistrarLog("Se genero una clave de firma JWT nueva (todavia no se guarda hasta hacer clic en Guardar).");
    }

    private void BtnCopiarJwt_Click(object? sender, EventArgs e)
    {
        if (txtJwtClave.Text.Length == 0)
        {
            return;
        }

        Clipboard.SetText(txtJwtClave.Text);
        RegistrarLog("Clave de firma JWT copiada al portapapeles.");
    }

    private void BtnExaminarCarpeta_Click(object? sender, EventArgs e)
    {
        using var dialogo = new FolderBrowserDialog { Description = "Selecciona la carpeta del almacen de archivos" };
        if (dialogo.ShowDialog(this) == DialogResult.OK)
        {
            txtRutaAlmacen.Text = dialogo.SelectedPath;
        }
    }

    private void BtnProbarEscritura_Click(object? sender, EventArgs e)
    {
        var (exito, mensaje) = AlmacenArchivosHelper.CrearYProbarEscritura(txtRutaAlmacen.Text.Trim());
        lblResultadoAlmacen.Text = mensaje;
        lblResultadoAlmacen.ForeColor = exito ? Color.DarkGreen : Color.Firebrick;
        RegistrarLog(mensaje);
    }

    // ------------------------------------------------------------------ Guardar

    private void BtnGuardar_Click(object? sender, EventArgs e)
    {
        var nombreServicio = txtNombreServicio.Text.Trim();
        if (nombreServicio.Length == 0)
        {
            MessageBox.Show(this, "Escribe el nombre del servicio.", "GTE - Instalador",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtRutaAlmacen.Text))
        {
            var respuesta = MessageBox.Show(this,
                "AlmacenArchivos__Ruta esta vacio: si el servidor no tiene la unidad de desarrollo " +
                "(D:\\GTE\\Archivos), TODA subida de archivos fallara con INTERNAL_ERROR.\n\n" +
                "Guardar de todas formas?",
                "GTE - Instalador", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (respuesta == DialogResult.No)
            {
                return;
            }
        }

        _configuracion.Ambiente = cboAmbiente.Text.Trim();
        _configuracion.CadenaConexion = ConstruirCadenaConexion(
            txtServidorSql.Text.Trim(), txtUsuarioSql.Text.Trim(), txtPasswordSql.Text);
        _configuracion.JwtClaveFirma = txtJwtClave.Text;
        _configuracion.AlmacenArchivosRuta = txtRutaAlmacen.Text.Trim();

        try
        {
            // Releer justo antes de guardar: evita pisar con _otrasVariables vacio si el
            // usuario nunca dio clic en "Cargar configuracion" para este nombre de servicio.
            if (!_configuracion.ServicioExiste)
            {
                _configuracion.Cargar(nombreServicio);
            }

            _configuracion.Guardar(nombreServicio);
            RegistrarLog($"Configuracion guardada en el registro del servicio '{nombreServicio}'.");
        }
        catch (Exception ex)
        {
            RegistrarLog($"ERROR al guardar: {ex.Message}");
            MessageBox.Show(this, ex.Message, "GTE - Instalador", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (chkReiniciarAlGuardar.Checked)
        {
            RegistrarLog("Reiniciando el servicio para que tome la configuracion nueva...");
            var (exito, mensaje) = ServicioWindowsHelper.Reiniciar(nombreServicio);
            RegistrarLog(exito ? "OK: servicio reiniciado." : $"ERROR al reiniciar: {mensaje}");
        }
        else
        {
            RegistrarLog("Reinicio omitido: el cambio no aplica hasta reiniciar el servicio a mano.");
        }

        ActualizarEstadoServicio();
    }
}
