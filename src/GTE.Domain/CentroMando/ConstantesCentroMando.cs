namespace GTE.Domain.CentroMando;

/// <summary>Claves de permisos del Centro de Mando TI (dbo.tblPermiso, script 03_Scripts/02).</summary>
public static class PermisosCentroMando
{
    /// <summary>Consultar scores, evaluaciones, diagnostico y alertas.</summary>
    public const string Ver = "GES.Ver";

    /// <summary>Editar el catalogo: metas, umbrales, pesos y acciones sugeridas.</summary>
    public const string Administrar = "GES.Administrar";
}

/// <summary>
/// Bloque tecnico de indicadores que le toca a un equipo, ademas del bloque comun.
/// Se guarda en dbo.tblEquipo.AmbitoCentroMando; null = solo bloque comun.
/// </summary>
public static class AmbitoCentroMando
{
    public const string Comun = "Comun";
    public const string Desarrollo = "Desarrollo";
    public const string Infraestructura = "Infraestructura";
    public const string Soporte = "Soporte";

    public static readonly IReadOnlyList<string> AmbitosTecnicos = [Desarrollo, Infraestructura, Soporte];

    public static bool EsTecnicoValido(string? ambito) => ambito is not null && AmbitosTecnicos.Contains(ambito);
}

/// <summary>Direccion deseada de un indicador (dbo.tblIndicadorGestion.Direccion).</summary>
public static class DireccionIndicador
{
    public const string Subir = "Subir";
    public const string Bajar = "Bajar";
}

/// <summary>Si el indicador lo calcula el motor o requiere captura manual.</summary>
public static class OrigenIndicador
{
    public const string Automatico = "Automatico";
    public const string Manual = "Manual";
}

/// <summary>
/// Las cinco causas del modelo de diagnostico. El orden importa: se evaluan en esta
/// secuencia y <see cref="Persona"/> es el ultimo recurso, solo cuando ningun indice
/// de proceso, recursos, dependencia o prioridad esta fuera de umbral.
/// </summary>
public static class CausaRaiz
{
    public const string Dependencia = "Dependencia";
    public const string Prioridad = "Prioridad";
    public const string Proceso = "Proceso";
    public const string Recursos = "Recursos";
    public const string Persona = "Persona";
}

public static class SemaforoCentroMando
{
    public const string Verde = "Verde";
    public const string Amarillo = "Amarillo";
    public const string Rojo = "Rojo";
}

public static class SeveridadAlerta
{
    public const string Critica = "Critica";
    public const string Atencion = "Atencion";
    public const string Positiva = "Positiva";
}

/// <summary>
/// Bandas de la evaluacion mensual. "Critico" se parte en dos a proposito: un 68 y un 20
/// exigen respuestas gerenciales distintas (seguimiento cercano contra intervencion
/// inmediata), y un solo corte en 70 los vuelve indistinguibles.
/// </summary>
public static class NivelDesempeno
{
    public const string Excelente = "Excelente";
    public const string Optimo = "Optimo";
    public const string RequiereAtencion = "RequiereAtencion";
    public const string Critico = "Critico";
    public const string CriticoSevero = "CriticoSevero";

    /// <summary>
    /// No es una banda de desempeno: es la ausencia de evaluacion. Se usa cuando hay tan
    /// pocos indicadores con dato que cualquier score seria una casualidad estadistica, no
    /// una medicion (ver CalculadoraCentroMando.CoberturaMinima).
    /// </summary>
    public const string SinDatosSuficientes = "SinDatosSuficientes";
}
