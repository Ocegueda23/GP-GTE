namespace GTE.Domain.Soporte;

/// <summary>IDs de dbo.tblEstatusTicket (contrato de seeds del script 01).</summary>
public static class EstatusTicket
{
    public const int Nuevo = 1;
    public const int Asignado = 2;
    public const int EnAtencion = 3;
    public const int EsperandoUsuario = 4;
    public const int Resuelto = 5;
    public const int Cerrado = 6;
}

/// <summary>Acciones del grafo del proceso Ticket (dbo.tblTransicion).</summary>
public static class AccionesTicket
{
    public const string Asignar = "ASIGNAR";
    public const string IniciarAtencion = "INICIAR_ATENCION";
    public const string EsperarUsuario = "ESPERAR_USUARIO";
    public const string Reanudar = "REANUDAR";
    public const string Resolver = "RESOLVER";
    public const string Cerrar = "CERRAR";
    public const string Reabrir = "REABRIR";

    /// <summary>
    /// Escalar a WorkItem: no esta en dbo.tblTransicion (no cambia el estatus del
    /// ticket), asi que nunca aparece en el listado dinamico de acciones del motor de
    /// workflow. Se resuelve con su propio comando (EscalarTicketCommand).
    /// </summary>
    public const string Escalar = "ESCALAR";
}

public static class PermisosTicket
{
    public const string Atender = "TKT.Atender";
}
