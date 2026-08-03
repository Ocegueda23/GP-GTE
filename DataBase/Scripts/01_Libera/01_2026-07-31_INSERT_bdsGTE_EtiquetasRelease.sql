USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-07-31_INSERT_bdsGTE_EtiquetasRelease.sql
   Autor:       Equipo GTE
   Descripcion: Tanda 3. Etiquetas de boton, permisos y motivos de las
                transiciones del proceso Release, para que la interfaz
                muestre "Solicitar aprobacion" en vez de
                SOLICITAR_APROBACION.
   Requiere:    Tandas 1 y 2 aplicadas.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblTransicionConfig
        (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
         RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro)
    SELECT v.Proceso, v.Origen, v.Accion, v.Etiqueta, v.Permiso,
           v.RequiereMotivo, v.EsPrincipal, v.Orden, N'script-despliegue'
    FROM (VALUES
        /* Release: 1 En Preparacion, 2 En Aprobacion, 3 Aprobado,
                    4 Liberado, 5 Revertido, 6 Cancelado */
        (N'Release', 1, N'SOLICITAR_APROBACION', N'Solicitar aprobacion', N'REL.Crear',     0, 1, 10),
        (N'Release', 2, N'APROBAR',              N'Aprobar release',      N'REL.Aprobar',   0, 1, 10),
        (N'Release', 2, N'RECHAZAR',             N'Rechazar release',     N'REL.Aprobar',   1, 0, 20),
        (N'Release', 3, N'DESPLEGAR_PROD',       N'Desplegar a produccion', N'REL.Desplegar', 0, 1, 10),
        (N'Release', 4, N'ROLLBACK',             N'Revertir despliegue',  N'REL.Desplegar', 1, 0, 20),
        (N'Release', 1, N'CANCELAR',             N'Cancelar release',     N'REL.Crear',     1, 0, 90)
        ) v(Proceso, Origen, Accion, Etiqueta, Permiso, RequiereMotivo, EsPrincipal, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig c
                      WHERE c.Proceso = v.Proceso
                        AND c.IdEstatusOrigen = v.Origen
                        AND c.Accion = v.Accion)
    PRINT 'OK: etiquetas de transiciones de Release sembradas'

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
