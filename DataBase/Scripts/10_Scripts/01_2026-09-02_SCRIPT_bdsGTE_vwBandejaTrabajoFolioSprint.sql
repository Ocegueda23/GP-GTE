USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-02_SCRIPT_bdsGTE_vwBandejaTrabajoFolioSprint.sql
   Autor:       Ana Viramontes
   Version:     1.0.1
   Descripcion: Redefine dbo.vwBandejaTrabajo (CREATE OR ALTER, idempotente)
                para exponer tambien el Folio del sprint (tblSprint.Folio,
                serie SPR agregada en 09_Scripts/01_2026-09-02_ALTER_tblSprint.sql).
                La bandeja de trabajo y el detalle del workitem muestran el
                folio del sprint, no su nombre/descripcion. La columna Sprint
                (nombre) se conserva porque otras consultas la usan.
   Requiere:    02_Libera/27_2026-08-07_SCRIPT_bdsGTE_vwBandejaTrabajoAjustes.sql
                y 09_Scripts/01_2026-09-02_ALTER_tblSprint.sql aplicados.
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
           sp.Folio                      AS FolioSprint,
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
    PRINT 'OK: vwBandejaTrabajo redefinida (FolioSprint expuesto)'

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
