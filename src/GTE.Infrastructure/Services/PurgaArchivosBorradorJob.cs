using GTE.Application.Interfaces;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Job recurrente de Hangfire (registrado en Program.cs) que recoge los archivos en borrador
/// que nadie llego a vincular: imagenes pegadas en un formulario de alta que despues se
/// cancelo. Un archivo de tblArchivo sin ninguna fila en tblArchivoVinculo y con mas de
/// <see cref="HorasDeGracia"/> horas de antiguedad ya no puede vincularse a nada (el modal que
/// lo subio no existe), asi que se borra el binario del almacen y se da de baja el registro.
///
/// La ventana de gracia protege el caso normal: alguien pega una imagen y tarda en llenar el
/// resto del formulario antes de guardar. Clase plana sin dependencia de Hangfire, mismo
/// patron que SnapshotKpiJob.
/// </summary>
public class PurgaArchivosBorradorJob(
    FabricaContexto fabrica,
    IAlmacenArchivos almacen,
    ILogger<PurgaArchivosBorradorJob> logger)
{
    /// <summary>Margen antes de considerar abandonado un borrador.</summary>
    public const int HorasDeGracia = 24;

    public async Task EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var corte = DateTime.Now.AddHours(-HorasDeGracia);

        await using var contexto = fabrica.ConectarContexto<DbContextGTE>();
        var huerfanos = await contexto.TblArchivo
            .Where(a => a.Activo && a.FechaRegistro < corte && !a.TblArchivoVinculo.Any())
            .ToListAsync(cancellationToken);

        if (huerfanos.Count == 0)
        {
            logger.LogInformation("Purga de archivos en borrador: nada que borrar.");
            return;
        }

        var borrados = 0;
        foreach (var archivo in huerfanos)
        {
            try
            {
                await almacen.EliminarAsync(archivo.GuidArchivo, cancellationToken);
            }
            catch (Exception ex)
            {
                // El binario puede haber desaparecido del share por fuera del sistema: eso no
                // debe impedir dar de baja el registro ni detener la purga de los demas.
                logger.LogWarning(
                    ex, "No se pudo borrar del almacen el archivo en borrador {Guid}.", archivo.GuidArchivo);
            }

            archivo.Activo = false;
            borrados++;
        }

        await contexto.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Purga de archivos en borrador: {Borrados} archivos huerfanos dados de baja.", borrados);
    }
}
