USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-07-30_INSERT_bdsGTE_TransicionesYEtiquetas.sql
   Autor:       Equipo GTE
   Descripcion: Tanda 2 (posterior a la tanda inicial 01-10).
                1. Agrega la transicion que permite REABRIR por calidad un
                   elemento ya Terminado (Terminado -> Correccion): la
                   necesita el modulo de Revisiones (RN-QA-03).
                2. Siembra dbo.tblTransicionConfig con las etiquetas de
                   boton, permisos y motivos obligatorios de cada accion,
                   para que la interfaz muestre "Enviar a pruebas" en vez
                   del codigo crudo ENVIAR_PRUEBAS.
   Requiere:    Tanda inicial 01-10 aplicada.
   Nota:        Alta de transiciones = DATOS, nunca codigo (seccion 9 del
                estandar). No se toca spCambiarEstatus.
   ===================================================================== */
BEGIN TRY

    /* ---------- 1. Transicion nueva: Terminado -> Correccion ---------- */
    INSERT INTO dbo.tblTransicion (IdProceso, IdEstatusOrigen, Accion, IdEstatusDestino, UsuarioRegistro)
    SELECT p.IdProceso, 6, N'RECHAZAR_QA', 4, N'script-despliegue'
    FROM dbo.tblProceso p
    WHERE p.Proceso = N'WorkItem'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblTransicion t
                      WHERE t.IdProceso = p.IdProceso
                        AND t.IdEstatusOrigen = 6
                        AND t.Accion = N'RECHAZAR_QA')
    PRINT 'OK: transicion WorkItem Terminado -> Correccion (RECHAZAR_QA)'

    /* ---------- 2. Metadatos de UI de las transiciones ---------- */
    INSERT INTO dbo.tblTransicionConfig
        (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
         RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro)
    SELECT v.Proceso, v.Origen, v.Accion, v.Etiqueta, v.Permiso,
           v.RequiereMotivo, v.EsPrincipal, v.Orden, N'script-despliegue'
    FROM (VALUES
        /* WorkItem: 1 Pendiente, 2 En Proceso, 3 En Pruebas, 4 Correccion,
                     5 Suspendido, 6 Terminado, 7 Cancelado */
        (N'WorkItem', 1, N'INICIAR',             N'Iniciar',            NULL,                        0, 1, 10),
        (N'WorkItem', 4, N'INICIAR',             N'Retomar correccion', NULL,                        0, 1, 10),
        (N'WorkItem', 5, N'REANUDAR',            N'Reanudar',           NULL,                        0, 1, 10),
        (N'WorkItem', 2, N'SUSPENDER',           N'Suspender',          NULL,                        0, 0, 30),
        (N'WorkItem', 2, N'ENVIAR_PRUEBAS',      N'Enviar a pruebas',   NULL,                        0, 1, 20),
        (N'WorkItem', 3, N'RECHAZAR_QA',         N'Rechazar por QA',    NULL,                        1, 0, 20),
        (N'WorkItem', 6, N'RECHAZAR_QA',         N'Reabrir por QA',     N'REV.Reabrir',              1, 0, 20),
        (N'WorkItem', 3, N'TERMINAR',            N'Terminar',           NULL,                        0, 1, 10),
        (N'WorkItem', 2, N'TERMINAR',            N'Terminar',           NULL,                        0, 0, 40),
        (N'WorkItem', 6, N'REVERTIR',            N'Revertir cierre',    N'WI.ModificarTerminado',    1, 0, 30),
        (N'WorkItem', 1, N'CANCELAR',            N'Cancelar',           N'WI.Eliminar',              1, 0, 90),
        /* Solicitud: 1 Borrador, 2 Enviada, 3 En Analisis, 4 Aprobada,
                      5 Rechazada, 6 Convertida, 7 Cancelada */
        (N'Solicitud', 1, N'ENVIAR',             N'Enviar',             NULL,                        0, 1, 10),
        (N'Solicitud', 2, N'TOMAR',              N'Tomar para analisis', N'SOL.Triage',              0, 1, 10),
        (N'Solicitud', 3, N'APROBAR',            N'Aprobar',            N'SOL.Triage',               0, 1, 10),
        (N'Solicitud', 3, N'RECHAZAR',           N'Rechazar',           N'SOL.Triage',               1, 0, 30),
        (N'Solicitud', 3, N'DEVOLVER',           N'Pedir mas informacion', N'SOL.Triage',            1, 0, 20),
        (N'Solicitud', 4, N'CONVERTIR',          N'Convertir en trabajo', N'SOL.Triage',             0, 1, 10),
        (N'Solicitud', 2, N'CANCELAR',           N'Cancelar',           NULL,                        0, 0, 90),
        /* Revision: 1 Pendiente, 2 En Proceso, 3 Terminada */
        (N'Revision', 1, N'INICIAR',             N'Atender hallazgo',   NULL,                        0, 1, 10),
        (N'Revision', 2, N'TERMINAR',            N'Marcar corregido',   NULL,                        0, 1, 10),
        (N'Revision', 3, N'REABRIR',             N'Reabrir hallazgo',   N'REV.Reabrir',              1, 0, 20)
        ) v(Proceso, Origen, Accion, Etiqueta, Permiso, RequiereMotivo, EsPrincipal, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig c
                      WHERE c.Proceso = v.Proceso
                        AND c.IdEstatusOrigen = v.Origen
                        AND c.Accion = v.Accion)
    PRINT 'OK: etiquetas y permisos de transiciones sembrados'

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
