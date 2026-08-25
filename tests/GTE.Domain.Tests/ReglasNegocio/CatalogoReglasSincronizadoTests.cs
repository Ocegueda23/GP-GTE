using System.Text.RegularExpressions;
using GTE.Domain.ReglasNegocio;
using Xunit;

namespace GTE.Domain.Tests.ReglasNegocio;

/// <summary>
/// La defensa que evita que el Catalogo de reglas de negocio se pudra.
///
/// Un catalogo desincronizado del codigo es peor que no tener catalogo: da confianza falsa.
/// Esta prueba escanea las claves RN-* que aparecen en src/ y exige que todas existan en el
/// script de semilla versionado (44_..._INSERT_bdsGTE_ReglasNegocioGTE.sql). Si alguien
/// etiqueta una regla nueva en el codigo y no la da de alta en el catalogo, la prueba falla.
///
/// Se compara contra el SCRIPT y no contra la base de datos a proposito: el script es lo que
/// esta versionado en el repo, corre en cualquier maquina sin depender de LocalDB y es lo
/// que se aplica a los servidores. Las reglas capturadas a mano desde la UI (las de los
/// sistemas del departamento) no viven aqui y no se validan con esta prueba -- esta cubre
/// la sincronia entre el codigo de GTE y las reglas de GTE.
///
/// OJO al escribir comentarios: el escaneo no distingue una referencia real de un ejemplo.
/// Una clave inventada dentro de un comentario (por ejemplo al documentar el formato) hace
/// fallar la prueba. Para ilustrar el formato usar el marcador RN-XXX-NN, que no coincide
/// con el patron porque NN no son digitos.
/// </summary>
public class CatalogoReglasSincronizadoTests
{
    /// <summary>
    /// Cualquier clave que aparezca en los scripts de reglas cuenta como "en el catalogo".
    /// Deliberadamente amplio: el script 45 (renumeracion) contiene tanto la clave vieja
    /// como la nueva en su tabla de mapeo, y ambas deben considerarse conocidas.
    /// </summary>
    private static readonly Regex PatronInsertClave = ClaveReglaNegocio.PatronBusqueda();

    [Fact]
    public void TodaClaveReferenciadaEnElCodigoExisteEnElCatalogo()
    {
        var raiz = ObtenerRaizRepositorio();
        var enCodigo = ObtenerClavesDelCodigo(raiz);
        var enCatalogo = ObtenerClavesDelCatalogo(raiz);

        Assert.NotEmpty(enCodigo);
        Assert.NotEmpty(enCatalogo);

        var faltantes = enCodigo.Except(enCatalogo).OrderBy(c => c, StringComparer.Ordinal).ToList();

        Assert.True(
            faltantes.Count == 0,
            "Hay reglas etiquetadas en el codigo que no estan en el catalogo de reglas de negocio. "
            + "Agregalas al script de semilla o desde el modulo antes de continuar: "
            + string.Join(", ", faltantes));
    }

    [Fact]
    public void TodaClaveDelCatalogoTieneFormatoValido()
    {
        var raiz = ObtenerRaizRepositorio();

        var invalidas = ObtenerClavesDelCatalogo(raiz)
            .Where(clave => !ClaveReglaNegocio.EsValida(clave))
            .ToList();

        Assert.True(
            invalidas.Count == 0,
            "Claves con formato invalido en el catalogo (se espera RN-XXX-NN): "
            + string.Join(", ", invalidas));
    }

    private static HashSet<string> ObtenerClavesDelCodigo(string raiz)
    {
        var claves = new HashSet<string>(StringComparer.Ordinal);
        var src = Path.Combine(raiz, "src");

        foreach (var archivo in Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories))
        {
            // bin y obj traen copias generadas que ensucian el conteo.
            if (archivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || archivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            foreach (Match coincidencia in ClaveReglaNegocio.PatronBusqueda().Matches(File.ReadAllText(archivo)))
            {
                claves.Add(coincidencia.Value);
            }
        }

        return claves;
    }

    private static HashSet<string> ObtenerClavesDelCatalogo(string raiz)
    {
        var scripts = Path.Combine(raiz, "DataBase", "Scripts");
        var claves = new HashSet<string>(StringComparer.Ordinal);

        // Glob amplio a proposito: las claves no viven solo en el script de semilla, tambien
        // en el de renumeracion (45_..._RenumeraReglasGTE.sql). Un glob pegado al nombre del
        // seed dejaria fuera cualquier script posterior que introduzca o cambie claves, que
        // es exactamente el caso que esta prueba debe cubrir.
        foreach (var archivo in Directory.EnumerateFiles(
                     scripts, "*Regla*.sql", SearchOption.AllDirectories))
        {
            foreach (Match coincidencia in PatronInsertClave.Matches(File.ReadAllText(archivo)))
            {
                claves.Add(coincidencia.Value);
            }
        }

        return claves;
    }

    /// <summary>Sube desde el directorio del ensamblado hasta encontrar GTE.sln.</summary>
    private static string ObtenerRaizRepositorio()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);

        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "GTE.sln")))
        {
            directorio = directorio.Parent;
        }

        Assert.NotNull(directorio);
        return directorio!.FullName;
    }
}
