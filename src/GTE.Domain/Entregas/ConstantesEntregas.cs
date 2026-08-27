namespace GTE.Domain.Entregas;

/// <summary>IDs de dbo.tblEstatusRelease (contrato de seeds del script 01).</summary>
public static class EstatusRelease
{
    public const int EnPreparacion = 1;
    public const int EnAprobacion = 2;
    public const int Aprobado = 3;
    public const int Liberado = 4;
    public const int Revertido = 5;
    public const int Cancelado = 6;
}

public static class AccionesRelease
{
    public const string SolicitarAprobacion = "SOLICITAR_APROBACION";
    public const string Aprobar = "APROBAR";
    public const string Rechazar = "RECHAZAR";
    public const string DesplegarProd = "DESPLEGAR_PROD";
    public const string Rollback = "ROLLBACK";
    public const string Cancelar = "CANCELAR";
    public const string Reabrir = "REABRIR";
}

/// <summary>IDs de dbo.tblEstatusAprobacion.</summary>
public static class EstatusAprobacion
{
    public const int Pendiente = 1;
    public const int Aprobada = 2;
    public const int Rechazada = 3;
}

/// <summary>IDs de dbo.tblEstatusDespliegue.</summary>
public static class EstatusDespliegue
{
    public const int EnEjecucion = 1;
    public const int Exitoso = 2;
    public const int Fallido = 3;
}

/// <summary>IDs de dbo.tblTipoArtefacto. Los scripts SQL exigen rollback pareado (RN-GTE-032).</summary>
public static class TipoArtefacto
{
    public const int Paquete = 1;
    public const int ScriptSql = 2;
    public const int Configuracion = 3;
    public const int Otro = 4;
}

/// <summary>IDs de dbo.tblTipoRespaldo (catalogo enumerado de ID fijo).</summary>
public static class TipoRespaldo
{
    public const int BaseDeDatos = 1;
    public const int Servicio = 2;
    public const int SitioWeb = 3;
    public const int Ubicacion = 4;
    public const int Otro = 5;
}

public static class PermisosEntregas
{
    public const string Crear = "REL.Crear";
    public const string Aprobar = "REL.Aprobar";
    public const string Desplegar = "REL.Desplegar";
}

/// <summary>Cadena de aprobacion estandar de un release.</summary>
public static class RolesAprobacion
{
    public const string GerenteAreaSolicitante = "Gerente del area solicitante";
    public const string Solicitante = "Solicitante";
    public const string UsuarioQuePrueba = "Usuario que prueba";
    public const string GerenteTi = "Gerente de TI";
    public const string Lider = "Lider";
    public const string IngSoporte = "Ing. Soporte";

    /// <summary>
    /// Orden en que se pintan las firmas en la Solicitud de despliegue, igual que el
    /// formato que se llenaba a mano (Doctos/Solicitud de despliegue.xlsx).
    /// </summary>
    public static readonly string[] Cadena =
        [GerenteAreaSolicitante, Solicitante, UsuarioQuePrueba, GerenteTi, Lider, IngSoporte];
}
