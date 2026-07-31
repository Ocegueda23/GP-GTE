using GTE.Domain.Entregas;
using Xunit;

namespace GTE.Domain.Tests.Entregas;

public class FirmaAprobacionTests
{
    [Fact]
    public void Firma_EsHashDe64CaracteresHexadecimales()
    {
        var firma = FirmaElectronica.Calcular("ana", "REL-GTE-2026-001", "QA", true);

        Assert.Equal(64, firma.Length);
        Assert.Matches("^[0-9A-F]+$", firma);
    }

    [Fact]
    public void Firma_CambiaConLaDecision()
    {
        var aprobada = FirmaElectronica.Calcular("ana", "REL-1", "QA", true);
        var rechazada = FirmaElectronica.Calcular("ana", "REL-1", "QA", false);

        Assert.NotEqual(aprobada, rechazada);
    }

    [Fact]
    public void Firma_CambiaConElUsuario()
    {
        var deAna = FirmaElectronica.Calcular("ana", "REL-1", "Lider", true);
        var deLuis = FirmaElectronica.Calcular("luis", "REL-1", "Lider", true);

        Assert.NotEqual(deAna, deLuis);
    }

    [Fact]
    public void CadenaDeAprobacion_TieneLosTresRolesEnOrden()
    {
        Assert.Equal(["QA", "Lider", "Negocio"], RolesAprobacion.Cadena);
    }

    [Fact]
    public void EstatusRelease_MantieneLosIdsDelCatalogo()
    {
        Assert.Equal(1, EstatusRelease.EnPreparacion);
        Assert.Equal(2, EstatusRelease.EnAprobacion);
        Assert.Equal(3, EstatusRelease.Aprobado);
        Assert.Equal(4, EstatusRelease.Liberado);
        Assert.Equal(5, EstatusRelease.Revertido);
    }
}
