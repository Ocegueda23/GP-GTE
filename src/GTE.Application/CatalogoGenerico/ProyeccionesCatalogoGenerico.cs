using GTE.Application.DTOs.Responses.CatalogoGenerico;
using GTE.Domain.CatalogoGenerico;

namespace GTE.Application.CatalogoGenerico;

/// <summary>Mapeo manual dominio -> DTO de respuesta (el proyecto no usa AutoMapper en la practica).</summary>
internal static class ProyeccionesCatalogoGenerico
{
    public static ConfiguracionCatalogoResponse Proyectar(ConfiguracionCatalogo config) => new()
    {
        IdCatalogo = config.IdCatalogo,
        Clave = config.Clave,
        NombreTabla = config.NombreTabla,
        Titulo = config.Titulo,
        Columnas = config.Columnas.Select(c => new ColumnaConfigResponse
        {
            NombreColumna = c.NombreColumna,
            TipoSql = c.TipoSql,
            EsNulable = c.EsNulable,
            LongitudMaxima = c.LongitudMaxima,
            EsPk = c.EsPk,
            EsIdentity = c.EsIdentity,
            DisplayName = c.DisplayName,
            EsVisible = c.EsVisible,
            EsSoloLectura = c.EsSoloLectura,
            EsRequerido = c.EsRequerido,
            OrdinalPos = c.OrdinalPos,
            TablaFk = c.TablaFk,
            ColumnaClaveFk = c.ColumnaClaveFk,
            ColumnaMostrarFk = c.ColumnaMostrarFk,
            EsCifrado = c.EsCifrado,
            AutoFechaAlta = c.AutoFechaAlta,
            AutoFechaEdicion = c.AutoFechaEdicion,
            AutoUsuarioAlta = c.AutoUsuarioAlta,
            AutoUsuarioEdicion = c.AutoUsuarioEdicion
        }).ToList()
    };
}
