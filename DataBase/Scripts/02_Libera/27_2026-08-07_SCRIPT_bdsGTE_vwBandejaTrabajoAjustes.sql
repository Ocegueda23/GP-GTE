USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      27_2026-08-07_SCRIPT_bdsGTE_vwBandejaTrabajoAjustes.sql
   Autor:       Equipo GTE
   Descripcion: Redefine dbo.vwBandejaTrabajo (CREATE OR ALTER, idempotente):
                1. EsVencida comparaba FechaCompromiso (datetime) contra
                   SYSDATETIME(): un elemento con compromiso HOY quedaba
                   marcado vencido desde la medianoche, sin haberse vencido
                   realmente todavia. Se compara solo la parte de fecha.
                2. Agrega Complejidad (nombre resuelto de tblComplejidad,
                   join opcional porque IdComplejidad admite NULL) para que
                   WorkItemQueryService/PlaneacionQueryService puedan
                   exponerla sin una consulta aparte.
   Requiere:    08_2026-07-30_SCRIPT_bdsGTE_Programables.sql aplicado.
   ===================================================================== */
BEGIN TRY

    EXEC(N'
    CREATE OR ALTER VIEW dbo.vwBandejaTrabajo
    AS
    SELECT wi.IdWorkItem,
           wi.Folio,
           twi.Nombre                    AS Tipo,
           wi.Titulo,
           p.Clave                       AS ClaveProyecto,
           p.Nombre                      AS Proyecto,
           p.EsMantenimiento,
           wi.IdEstatusWorkItem,
           e.Descripcion                 AS Estatus,
           wi.IdPrioridad,
           pr.Nombre                     AS Prioridad,
           wi.IdComplejidad,
           co.Nombre                     AS Complejidad,
           wi.IdAsignado,
           ua.Nombre                     AS Asignado,
           us.Nombre                     AS Solicitante,
           wi.IdSprint,
           sp.Nombre                     AS Sprint,
           wi.PuntosHistoria,
           wi.MinutosPresupuesto,
           ti.MinutosInvertidos,
           wi.FechaCompromiso,
           wi.FechaInicio,
           wi.FechaFin,
           wi.FechaRegistro,
           CASE WHEN CAST(wi.FechaCompromiso AS DATE) < CAST(SYSDATETIME() AS DATE)
                 AND wi.IdEstatusWorkItem NOT IN (6, 7)   -- 6 Terminado, 7 Cancelado
                THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS EsVencida,
           (SELECT COUNT(*) FROM dbo.tblRevision r
            WHERE r.IdWorkItem = wi.IdWorkItem AND r.Corregido = 0 AND r.Activo = 1) AS RevisionesPendientes
    FROM dbo.tblWorkItem wi
    INNER JOIN dbo.tblTipoWorkItem twi ON twi.Id = wi.IdTipoWorkItem
    INNER JOIN dbo.tblProyecto p ON p.IdProyecto = wi.IdProyecto
    INNER JOIN dbo.tblEstatusWorkItem e ON e.Id = wi.IdEstatusWorkItem
    INNER JOIN dbo.tblPrioridad pr ON pr.Id = wi.IdPrioridad
    LEFT JOIN dbo.tblComplejidad co ON co.IdComplejidad = wi.IdComplejidad
    LEFT JOIN dbo.tblUsuario ua ON ua.IdUsuario = wi.IdAsignado
    LEFT JOIN dbo.tblUsuario us ON us.IdUsuario = wi.IdSolicitante
    LEFT JOIN dbo.tblSprint sp ON sp.IdSprint = wi.IdSprint
    LEFT JOIN dbo.vwTiempoInvertido ti ON ti.IdWorkItem = wi.IdWorkItem
    WHERE wi.Activo = 1')
    PRINT 'OK: vwBandejaTrabajo (EsVencida por fecha, Complejidad expuesta)'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
