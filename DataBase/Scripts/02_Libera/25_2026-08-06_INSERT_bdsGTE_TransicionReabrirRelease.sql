USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      25_2026-08-06_INSERT_bdsGTE_TransicionReabrirRelease.sql
   Autor:       Equipo GTE
   Descripcion: Nueva transicion REABRIR (Aprobado -> En Preparacion, 3 -> 1)
                del proceso Release: hasta hoy, un release con toda la
                cadena de aprobacion firmada (estatus 3) no tenia forma de
                regresar a preparacion para agregar contenido/artefactos
                que hicieron falta -- la unica reversa (RECHAZAR, 2 -> 1)
                exige que el release siga en aprobacion (estatus 2).
                REABRIR usa el mismo motor de estatus (dbo.spCambiarEstatus,
                sin tocar el SP generico). El backend (CambiarEstatusReleaseHandler)
                exige el permiso REL.Aprobar (misma puerta que firmar, porque
                reabrir invalida firmas ya puestas) e invalida la cadena de
                aprobacion existente para forzar una firma nueva de QA/Lider/
                Negocio la proxima vez que se solicite aprobacion.
   Requiere:    07 (tblTransicion), 09 (proceso Release ya dado de alta).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblTransicion (IdProceso, IdEstatusOrigen, Accion, IdEstatusDestino, UsuarioRegistro)
    SELECT p.IdProceso, v.Origen, v.Accion, v.Destino, N'script-despliegue'
    FROM (VALUES
        (3, N'REABRIR', 1)
        ) v(Origen, Accion, Destino)
    INNER JOIN dbo.tblProceso p ON p.Proceso = N'Release'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicion t
                      WHERE t.IdProceso = p.IdProceso
                        AND t.IdEstatusOrigen = v.Origen
                        AND t.Accion = v.Accion)
    PRINT 'OK: transicion Release REABRIR (Aprobado -> En Preparacion) registrada'

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
