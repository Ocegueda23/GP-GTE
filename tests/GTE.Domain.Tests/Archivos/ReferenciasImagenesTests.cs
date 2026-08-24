using GTE.Domain.Archivos;
using Xunit;

namespace GTE.Domain.Tests.Archivos;

/// <summary>
/// De lo que lee este extractor depende que las imagenes pegadas en un formulario de ALTA
/// queden adjuntas al guardar: los comandos de alta vinculan justo los GUID que aparecen
/// aqui. Si deja de reconocer el marcado que emite el editor (img[data-guid], el unico
/// atributo de imagen que permite SanitizadorHtmlGanss), el alta con imagen vuelve a
/// perderla y el usuario acaba guardando dos veces.
/// </summary>
public class ReferenciasImagenesTests
{
    private const string Guid1 = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
    private const string Guid2 = "a1b2c3d4-1111-2222-3333-444455556666";

    [Fact]
    public void ObtenerGuids_LeeElMarcadoQueEmiteElEditor()
    {
        var html = $"<p>Antes</p><img data-guid=\"{Guid1}\"><p>Despues</p>";

        var guids = ReferenciasImagenes.ObtenerGuids(html);

        Assert.Equal(new[] { System.Guid.Parse(Guid1) }, guids);
    }

    [Fact]
    public void ObtenerGuids_NoRepiteLaMismaImagen()
    {
        var html = $"<img data-guid=\"{Guid1}\"><img data-guid=\"{Guid1}\"><img data-guid=\"{Guid2}\">";

        var guids = ReferenciasImagenes.ObtenerGuids(html);

        Assert.Equal(2, guids.Count);
        Assert.Contains(System.Guid.Parse(Guid1), guids);
        Assert.Contains(System.Guid.Parse(Guid2), guids);
    }

    [Fact]
    public void ObtenerGuids_AceptaComillaSimpleYEspacios()
    {
        var html = $"<img data-guid = '{Guid1}' >";

        var guids = ReferenciasImagenes.ObtenerGuids(html);

        Assert.Equal(new[] { System.Guid.Parse(Guid1) }, guids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p>Contenido sin imagenes</p>")]
    [InlineData("<img data-guid=\"no-es-un-guid\">")]
    [InlineData("<img src=\"http://ejemplo/foto.png\">")]
    public void ObtenerGuids_DevuelveVacioCuandoNoHayReferenciasValidas(string? html)
    {
        Assert.Empty(ReferenciasImagenes.ObtenerGuids(html));
    }
}
