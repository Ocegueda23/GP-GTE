namespace GTE.Domain.Autenticacion;

/// <summary>Parametros del mecanismo propio de autenticacion (sin proveedor externo).</summary>
public static class ConstantesAutenticacion
{
    public const int IntentosMaximos = 5;
    public const int MinutosBloqueo = 15;
    public const int MinutosVigenciaAcceso = 15;
    public const int HorasVigenciaRefresh = 8;
}
