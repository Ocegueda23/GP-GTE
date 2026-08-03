USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      13_2026-08-02_INSERT_bdsGTE_IncidentesConfig.sql
   Autor:       Equipo GTE
   Descripcion: Modulo Incidentes (Operacion), Fase 4. Etiquetas/permisos de
                UI para las 5 transiciones del proceso Incidente, ya
                sembradas en 09_2026-07-30_INSERT_bdsGTE_Procesos.sql --
                aqui solo se agrega su tblTransicionConfig, mismo patron
                que 12_2026-08-02_INSERT_bdsGTE_TicketsConfig.sql.
   Requiere:    01-12 aplicados (en particular 01, 02, 06, 09).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblTransicionConfig
        (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
         RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro)
    SELECT v.Proceso, v.Origen, v.Accion, v.Etiqueta, v.Permiso,
           v.RequiereMotivo, v.EsPrincipal, v.Orden, N'script-despliegue'
    FROM (VALUES
        /* Incidente: 1 Detectado, 2 En Atencion, 3 Mitigado, 4 Resuelto, 5 Cerrado */
        (N'Incidente', 1, N'ATENDER',   N'Atender',   N'INC.Gestionar', 0, 1, 10),
        (N'Incidente', 2, N'MITIGAR',   N'Mitigar',   N'INC.Gestionar', 0, 0, 20),
        (N'Incidente', 2, N'RESOLVER',  N'Resolver',  N'INC.Gestionar', 0, 1, 10),
        (N'Incidente', 3, N'RESOLVER',  N'Resolver',  N'INC.Gestionar', 0, 1, 10),
        (N'Incidente', 4, N'CERRAR',    N'Cerrar',    N'INC.Gestionar', 0, 1, 10)
        ) v(Proceso, Origen, Accion, Etiqueta, Permiso, RequiereMotivo, EsPrincipal, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig c
                      WHERE c.Proceso = v.Proceso
                        AND c.IdEstatusOrigen = v.Origen
                        AND c.Accion = v.Accion)
    PRINT 'OK: etiquetas y permisos de transiciones de Incidente sembrados'

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
