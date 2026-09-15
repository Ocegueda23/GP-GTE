namespace GTE.Domain.Ausencias;

/// <summary>IDs de dbo.tblEstatusAusencia (contrato de seeds del script 01).</summary>
public static class EstatusAusencia
{
    public const int Solicitada = 1;
    public const int Aprobada = 2;
    public const int Rechazada = 3;
    public const int Cancelada = 4;

    /// <summary>Estatus que siguen ocupando el calendario de la persona (bloquean traslape).</summary>
    public static readonly IReadOnlyList<int> Vigentes = [Solicitada, Aprobada];
}

/// <summary>Acciones del grafo del proceso Ausencia (dbo.tblTransicion).</summary>
public static class AccionesAusencia
{
    public const string Aprobar = "APROBAR";
    public const string Rechazar = "RECHAZAR";
    public const string Cancelar = "CANCELAR";

    /// <summary>Acciones reservadas a quien aprueba (permiso ADM.Ausencias).</summary>
    public static readonly IReadOnlySet<string> DeAprobador =
        new HashSet<string> { Aprobar, Rechazar };

    /// <summary>Acciones que exigen motivo capturado (se lo lleva la notificacion).</summary>
    public static readonly IReadOnlySet<string> ConMotivo =
        new HashSet<string> { Rechazar };
}

public static class PermisosAusencia
{
    /// <summary>Aprobar/rechazar ausencias ajenas y ver la bandeja completa.</summary>
    public const string Gestionar = "ADM.Ausencias";
}
