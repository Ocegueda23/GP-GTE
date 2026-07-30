namespace GTE.Domain.Solicitudes;

/// <summary>IDs de dbo.tblEstatusSolicitud (contrato de seeds del script 01).</summary>
public static class EstatusSolicitud
{
    public const int Borrador = 1;
    public const int Enviada = 2;
    public const int EnAnalisis = 3;
    public const int Aprobada = 4;
    public const int Rechazada = 5;
    public const int Convertida = 6;
    public const int Cancelada = 7;
}

/// <summary>Acciones del grafo del proceso Solicitud (dbo.tblTransicion).</summary>
public static class AccionesSolicitud
{
    public const string Enviar = "ENVIAR";
    public const string Tomar = "TOMAR";
    public const string Aprobar = "APROBAR";
    public const string Rechazar = "RECHAZAR";
    public const string Devolver = "DEVOLVER";
    public const string Convertir = "CONVERTIR";
    public const string Cancelar = "CANCELAR";

    /// <summary>Acciones que solo puede ejecutar quien hace triage (permiso SOL.Triage).</summary>
    public static readonly IReadOnlySet<string> DeTriage =
        new HashSet<string> { Tomar, Aprobar, Rechazar, Devolver, Convertir };

    /// <summary>Acciones que exigen motivo capturado.</summary>
    public static readonly IReadOnlySet<string> ConMotivo =
        new HashSet<string> { Rechazar, Devolver };
}

public static class PermisosSolicitud
{
    public const string Triage = "SOL.Triage";
}
