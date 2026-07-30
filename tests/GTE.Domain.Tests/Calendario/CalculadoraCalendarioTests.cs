using GTE.Domain.Calendario;
using GTE.Domain.Exceptions;
using Xunit;

namespace GTE.Domain.Tests.Calendario;

/// <summary>
/// Vectores compartidos con la validacion de dbo.fnMinutosLaborales (2026-07-30):
/// ambos motores (SQL y C#) deben dar EXACTAMENTE los mismos resultados.
/// Horarios heredados del GT: BANSI turno partido, BECARIO turno simple.
/// </summary>
public class CalculadoraCalendarioTests
{
    private static readonly IReadOnlyList<TramoHorario> Bansi = CrearTramosLunesAViernes(
        (new TimeOnly(8, 30), new TimeOnly(14, 30)),
        (new TimeOnly(17, 0), new TimeOnly(19, 30)));

    private static readonly IReadOnlyList<TramoHorario> Becario = CrearTramosLunesAViernes(
        (new TimeOnly(9, 0), new TimeOnly(14, 0)));

    private static readonly IReadOnlySet<DateOnly> SinFestivos = new HashSet<DateOnly>();

    private static IReadOnlyList<TramoHorario> CrearTramosLunesAViernes(
        params (TimeOnly Inicio, TimeOnly Fin)[] tramos)
    {
        var lista = new List<TramoHorario>();
        for (byte dia = 1; dia <= 5; dia++)
        {
            foreach (var (inicio, fin) in tramos)
            {
                lista.Add(new TramoHorario(dia, inicio, fin));
            }
        }
        return lista;
    }

    [Fact]
    public void LunesCompletoBansi_Devuelve510()
    {
        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 7, 27, 0, 0, 0), new DateTime(2026, 7, 27, 23, 59, 0),
            Bansi, SinFestivos);

        Assert.Equal(510, minutos);
    }

    [Fact]
    public void ParcialDentroDelTramo_Devuelve180()
    {
        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 7, 27, 10, 0, 0), new DateTime(2026, 7, 27, 13, 0, 0),
            Bansi, SinFestivos);

        Assert.Equal(180, minutos);
    }

    [Fact]
    public void Sabado_DevuelveCero()
    {
        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 8, 1, 8, 0, 0), new DateTime(2026, 8, 1, 20, 0, 0),
            Bansi, SinFestivos);

        Assert.Equal(0, minutos);
    }

    [Fact]
    public void SemanaCompletaBecario_Devuelve1500()
    {
        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 7, 27, 0, 0, 0), new DateTime(2026, 7, 31, 23, 59, 0),
            Becario, SinFestivos);

        Assert.Equal(1500, minutos);
    }

    [Fact]
    public void SemanaBecarioConFestivo_Devuelve1200()
    {
        var festivos = new HashSet<DateOnly> { new(2026, 7, 28) };

        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 7, 27, 0, 0, 0), new DateTime(2026, 7, 31, 23, 59, 0),
            Becario, festivos);

        Assert.Equal(1200, minutos);
    }

    [Fact]
    public void CruzaCorteDeComidaBansi_Devuelve60()
    {
        var minutos = CalculadoraCalendario.CalcularMinutosLaborales(
            new DateTime(2026, 7, 27, 14, 0, 0), new DateTime(2026, 7, 27, 17, 30, 0),
            Bansi, SinFestivos);

        Assert.Equal(60, minutos);
    }

    [Fact]
    public void SumarDiaCompletoBansi_TerminaALas1930()
    {
        var resultado = CalculadoraCalendario.SumarMinutosLaborales(
            new DateTime(2026, 7, 27, 8, 30, 0), 510, Bansi, SinFestivos);

        Assert.Equal(new DateTime(2026, 7, 27, 19, 30, 0), resultado);
    }

    [Fact]
    public void SumarDiaCompletoMasUnMinuto_CaeAlMartes()
    {
        var resultado = CalculadoraCalendario.SumarMinutosLaborales(
            new DateTime(2026, 7, 27, 8, 30, 0), 511, Bansi, SinFestivos);

        Assert.Equal(new DateTime(2026, 7, 28, 8, 31, 0), resultado);
    }

    [Fact]
    public void SumarDesdeViernesEnLaTarde_CaeAlLunes()
    {
        // Viernes 18:00 con horario BECARIO (09:00-14:00): el tramo ya paso,
        // el fin de semana no cuenta -> lunes 09:00 + 60 = 10:00
        var resultado = CalculadoraCalendario.SumarMinutosLaborales(
            new DateTime(2026, 7, 31, 18, 0, 0), 60, Becario, SinFestivos);

        Assert.Equal(new DateTime(2026, 8, 3, 10, 0, 0), resultado);
    }

    [Fact]
    public void SumarConFestivoIntermedio_SaltaElFestivo()
    {
        // Lunes 13:30 BECARIO + 60: quedan 30 el lunes; martes es festivo -> miercoles 09:30
        var festivos = new HashSet<DateOnly> { new(2026, 7, 28) };

        var resultado = CalculadoraCalendario.SumarMinutosLaborales(
            new DateTime(2026, 7, 27, 13, 30, 0), 60, Becario, festivos);

        Assert.Equal(new DateTime(2026, 7, 29, 9, 30, 0), resultado);
    }

    [Fact]
    public void SumarSinTramos_LanzaBusinessException()
    {
        Assert.Throws<BusinessException>(() =>
            CalculadoraCalendario.SumarMinutosLaborales(
                new DateTime(2026, 7, 27, 8, 0, 0), 60, [], SinFestivos));
    }

    [Fact]
    public void EsDiaLaborable_DistingueLaborableFinDeSemanaYFestivo()
    {
        var festivos = new HashSet<DateOnly> { new(2026, 7, 28) };

        Assert.True(CalculadoraCalendario.EsDiaLaborable(new DateOnly(2026, 7, 27), Bansi, festivos));
        Assert.False(CalculadoraCalendario.EsDiaLaborable(new DateOnly(2026, 7, 28), Bansi, festivos));
        Assert.False(CalculadoraCalendario.EsDiaLaborable(new DateOnly(2026, 8, 1), Bansi, festivos));
    }
}
