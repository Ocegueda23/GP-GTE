using GTE.Domain.Revisiones;
using Xunit;

namespace GTE.Domain.Tests.Revisiones;

/// <summary>
/// Los IDs y acciones de revision son contrato con el grafo sembrado en
/// dbo.tblTransicion: si cambian aqui sin cambiar los datos, el motor rechaza
/// las transiciones con codigo 53.
/// </summary>
public class SecuenciaRevisionTests
{
    [Fact]
    public void EstatusRevision_MantieneLosIdsDelCatalogo()
    {
        Assert.Equal(1, EstatusRevision.Pendiente);
        Assert.Equal(2, EstatusRevision.EnProceso);
        Assert.Equal(3, EstatusRevision.Terminada);
    }

    [Fact]
    public void AccionesRevision_CoincidenConElGrafo()
    {
        Assert.Equal("INICIAR", AccionesRevision.Iniciar);
        Assert.Equal("TERMINAR", AccionesRevision.Terminar);
        Assert.Equal("REABRIR", AccionesRevision.Reabrir);
    }

    [Fact]
    public void PermisoReabrir_EsElRegistradoEnLaMatrizDeRoles()
    {
        Assert.Equal("REV.Reabrir", PermisosRevision.Reabrir);
    }
}
