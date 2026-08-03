USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      06_2026-08-02_UPDATE_tblHistorialEstatus.sql
   Autor:       Equipo GTE
   Descripcion: Backfill de tblHistorialEstatus.MinutosLaborales para
                intervalos cerrados que quedaron en NULL -- incluye las
                filas migradas del GT (05_2026-08-01_SCRIPT_bdsGTE_MigracionNucleo.sql,
                que deliberadamente no reprodujo fnMinutosLaborales
                historico, ver B3 en Doctos/PENDIENTES.md) y cualquier otra
                fila anterior a esa pasada con el mismo hueco.

                Sin este backfill, vwTiempoInvertido (filtra
                MinutosLaborales IS NOT NULL) muestra 0 minutos invertidos
                para esas filas, aunque el tiempo crudo si viva en
                tblRegistroTiempo -- se percibe como "no se migraron los
                tiempos" en Bandeja/Detalle aunque a nivel de tabla la
                migracion si trajo los registros.

                Recalcula con la MISMA logica que usa spCambiarEstatus en
                vivo (dbo.fnMinutosLaborales sobre el intervalo
                FechaInicio-FechaFin de cada fila), usando el IdHorario del
                asignado ACTUAL del WorkItem -- misma simplificacion que ya
                usa CambiarEstatusWorkItemHandler.ObtenerHorarioAsignadoAsync
                en el backend (no se reconstruye el asignado historico por
                fila). Filas sin asignado o cuyo asignado no tiene horario
                capturado se quedan en NULL a proposito, igual que en vivo.

                Idempotente por construccion: filtra MinutosLaborales IS
                NULL, asi que una segunda corrida no encuentra filas que
                actualizar.
   ===================================================================== */
BEGIN TRY

    DECLARE @Filas INT;

    UPDATE h
        SET h.MinutosLaborales = m.Minutos
    FROM dbo.tblHistorialEstatus h
    JOIN dbo.tblWorkItem w ON w.IdWorkItem = h.IdRegistro
    JOIN dbo.tblUsuario u ON u.IdUsuario = w.IdAsignado
    CROSS APPLY dbo.fnMinutosLaborales(h.FechaInicio, h.FechaFin, u.IdHorario) m
    WHERE h.Proceso = N'WorkItem'
      AND h.MinutosLaborales IS NULL
      AND h.FechaFin IS NOT NULL
      AND u.IdHorario IS NOT NULL

    SET @Filas = @@ROWCOUNT;
    PRINT 'OK: tblHistorialEstatus.MinutosLaborales recalculado en ' + CAST(@Filas AS NVARCHAR(10)) + ' filas'

    DECLARE @Pendientes INT;
    SELECT @Pendientes = COUNT(*)
    FROM dbo.tblHistorialEstatus
    WHERE Proceso = N'WorkItem' AND MinutosLaborales IS NULL AND FechaFin IS NOT NULL;
    PRINT 'INFO: siguen en NULL (WorkItem sin asignado o asignado sin horario) = ' + CAST(@Pendientes AS NVARCHAR(10))

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
