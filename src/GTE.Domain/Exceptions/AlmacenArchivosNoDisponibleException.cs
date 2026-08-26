using GTE.Domain.Common;

namespace GTE.Domain.Exceptions;

/// <summary>
/// El almacen de archivos configurado no se puede escribir: la ruta de
/// AlmacenArchivos:Ruta apunta a una unidad o share que no existe en el servidor, o la
/// cuenta del servicio no tiene permiso ahi. Es un problema de instalacion, no del dato
/// que mando el usuario, asi que se mapea a 500 -- pero con mensaje propio, porque el
/// INTERNAL_ERROR genrico no dejaba ver que lo que fallaba era la ruta.
/// </summary>
public class AlmacenArchivosNoDisponibleException(string raiz, Exception causa)
    : ExcepcionDominio($"No se pudo escribir en el almacen de archivos '{raiz}': {causa.Message}")
{
    public string Raiz { get; } = raiz;
}

/// <summary>
/// El archivo esta registrado en BD pero su binario no esta en el almacen. Tipico cuando
/// la ruta del almacen cambio entre despliegues y los archivos viejos quedaron en la
/// anterior. Es un 404 y no un error interno: el sistema funciona, ese archivo falta.
/// </summary>
public class ArchivoNoEncontradoException(Guid guidArchivo, string rutaEsperada)
    : NotFoundException(
        $"El archivo {guidArchivo} esta registrado pero su contenido no esta en el almacen.")
{
    public Guid GuidArchivo { get; } = guidArchivo;

    /// <summary>Solo para el log: nunca se manda al cliente.</summary>
    public string RutaEsperada { get; } = rutaEsperada;
}
