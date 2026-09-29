using GTE.Application.Common;
using Xunit;

namespace GTE.Application.Tests;

/// <summary>
/// La descripcion de un WorkItem se guarda como HTML del editor enriquecido. A Excel tiene que
/// llegar como texto: el defecto que origino esta clase era que la celda mostraba las etiquetas
/// y las entidades crudas ("los simbolos del xml", segun quien lo reporto).
/// </summary>
public class TextoPlanoTests
{
    [Fact]
    public void QuitaLasEtiquetasDelEditor()
    {
        Assert.Equal("Ajustar el reporte", TextoPlano.DesdeHtml("<p>Ajustar el reporte</p>"));
    }

    [Fact]
    public void SeparaLosParrafosEnVezDePegarlos()
    {
        Assert.Equal("uno dos", TextoPlano.DesdeHtml("<p>uno</p><p>dos</p>"));
    }

    [Fact]
    public void SeparaLosRenglonesDeUnaLista()
    {
        Assert.Equal("uno dos tres", TextoPlano.DesdeHtml("<ul><li>uno</li><li>dos</li><li>tres</li></ul>"));
    }

    [Fact]
    public void ConvierteElSaltoDeLinea()
    {
        Assert.Equal("uno dos", TextoPlano.DesdeHtml("uno<br>dos"));
    }

    [Fact]
    public void ResuelveLasEntidadesYColapsaElEspacioDuro()
    {
        Assert.Equal("Pagos & cobros", TextoPlano.DesdeHtml("<p>Pagos&nbsp;&amp;&nbsp;cobros</p>"));
    }

    /// <summary>
    /// Las entidades se decodifican DESPUES de borrar etiquetas: si se hiciera al reves, un
    /// "&lt;b&gt;" que el usuario escribio como texto se volveria etiqueta y desapareceria.
    /// </summary>
    [Fact]
    public void ConservaElMarcadoQueElUsuarioEscribioComoTexto()
    {
        Assert.Equal("usar <b> para negritas", TextoPlano.DesdeHtml("<p>usar &lt;b&gt; para negritas</p>"));
    }

    /// <summary>El editor vacio no manda cadena vacia sino marcado suelto; la celda queda en blanco.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p></p>")]
    [InlineData("<p><br></p>")]
    public void DevuelveNuloCuandoNoHayTextoVisible(string? html)
    {
        Assert.Null(TextoPlano.DesdeHtml(html));
    }

    /// <summary>Compatibilidad con lo capturado antes del editor enriquecido: texto plano tal cual.</summary>
    [Fact]
    public void DejaPasarElTextoPlanoLegado()
    {
        Assert.Equal("Descripcion vieja sin HTML", TextoPlano.DesdeHtml("Descripcion vieja sin HTML"));
    }
}
