USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-08-31_INSERT_bdsGTE_TransicionAutorizarRelease.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Nueva transicion AUTORIZAR (En Aprobacion -> Aprobado,
                2 -> 3) del proceso Release, mas su etiqueta de boton.
                Es el paso de autorizacion: quien la ejecuta da por
                cubiertas las firmas de la cadena que seguian pendientes,
                sin recabarlas una por una. Usa el mismo motor de estatus
                (dbo.spCambiarEstatus, sin tocar el SP generico); el
                backend (CambiarEstatusReleaseHandler) exige REL.Autorizar
                -- NO REL.Aprobar -- y marca las firmas pendientes como
                Omitidas (tblEstatusAprobacion 4) con el autorizador, su
                motivo y su firma electronica.
                RequiereMotivo = 1: saltarse la cadena siempre tiene que
                quedar justificado en la bitacora.
   Requiere:    Tanda 01 (07 tblTransicion, 09 proceso Release) y el
                script 01 de esta tanda (permiso REL.Autorizar).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblTransicion (IdProceso, IdEstatusOrigen, Accion, IdEstatusDestino, UsuarioRegistro)
    SELECT p.IdProceso, v.Origen, v.Accion, v.Destino, N'script-despliegue'
    FROM (VALUES
        (2, N'AUTORIZAR', 3)
        ) v(Origen, Accion, Destino)
    INNER JOIN dbo.tblProceso p ON p.Proceso = N'Release'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicion t
                      WHERE t.IdProceso = p.IdProceso
                        AND t.IdEstatusOrigen = v.Origen
                        AND t.Accion = v.Accion)
    PRINT 'OK: transicion Release AUTORIZAR (En Aprobacion -> Aprobado) registrada'

    INSERT INTO dbo.tblTransicionConfig
        (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
         RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro)
    SELECT v.Proceso, v.Origen, v.Accion, v.Etiqueta, v.Permiso,
           v.RequiereMotivo, v.EsPrincipal, v.Orden, N'script-despliegue'
    FROM (VALUES
        (N'Release', 2, N'AUTORIZAR', N'Autorizar sin firmas', N'REL.Autorizar', 1, 0, 15)
        ) v(Proceso, Origen, Accion, Etiqueta, Permiso, RequiereMotivo, EsPrincipal, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig c
                      WHERE c.Proceso = v.Proceso
                        AND c.IdEstatusOrigen = v.Origen
                        AND c.Accion = v.Accion)
    PRINT 'OK: etiqueta de la transicion AUTORIZAR sembrada'

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
