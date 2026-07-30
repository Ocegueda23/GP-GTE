USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      09_2026-07-30_INSERT_bdsGTE_Procesos.sql
   Autor:       Equipo GTE
   Descripcion: Alta de los 11 procesos GTE en el motor de estatus PROPIO
                (dbo.tblProceso) y de todas sus transiciones
                (dbo.tblTransicion), segun los workflows de la seccion 4
                del Documento Maestro. Los IDs de estatus referencian los
                seeds del script 01 (contrato fijo).
                GTE es totalmente independiente: el motor vive en bdsGTE
                (ADR-03, decision del equipo 2026-07-30).
   Requiere:    01-08
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblProceso (Proceso, TablaEstatus, TablaTransaccional, ColumnaEstatus, ColumnaPK, UsuarioRegistro)
    SELECT v.Proceso, v.TablaEstatus, v.TablaTransaccional, v.ColumnaEstatus, v.ColumnaPK, N'script-despliegue'
    FROM (VALUES
        (N'WorkItem',   N'dbo.tblEstatusWorkItem',   N'dbo.tblWorkItem',   N'IdEstatusWorkItem',   N'IdWorkItem'),
        (N'Solicitud',  N'dbo.tblEstatusSolicitud',  N'dbo.tblSolicitud',  N'IdEstatusSolicitud',  N'IdSolicitud'),
        (N'Ticket',     N'dbo.tblEstatusTicket',     N'dbo.tblTicket',     N'IdEstatusTicket',     N'IdTicket'),
        (N'Release',    N'dbo.tblEstatusRelease',    N'dbo.tblRelease',    N'IdEstatusRelease',    N'IdRelease'),
        (N'Incidente',  N'dbo.tblEstatusIncidente',  N'dbo.tblIncidente',  N'IdEstatusIncidente',  N'IdIncidente'),
        (N'Ausencia',   N'dbo.tblEstatusAusencia',   N'dbo.tblAusencia',   N'IdEstatusAusencia',   N'IdAusencia'),
        (N'Riesgo',     N'dbo.tblEstatusRiesgo',     N'dbo.tblRiesgo',     N'IdEstatusRiesgo',     N'IdRiesgo'),
        (N'Sprint',     N'dbo.tblEstatusSprint',     N'dbo.tblSprint',     N'IdEstatusSprint',     N'IdSprint'),
        (N'Revision',   N'dbo.tblEstatusRevision',   N'dbo.tblRevision',   N'IdEstatusRevision',   N'IdRevision'),
        (N'Aprobacion', N'dbo.tblEstatusAprobacion', N'dbo.tblAprobacion', N'IdEstatusAprobacion', N'IdAprobacion'),
        (N'Proyecto',   N'dbo.tblEstatusProyecto',   N'dbo.tblProyecto',   N'IdEstatusProyecto',   N'IdProyecto')
        ) v(Proceso, TablaEstatus, TablaTransaccional, ColumnaEstatus, ColumnaPK)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblProceso p WHERE p.Proceso = v.Proceso)
    PRINT 'OK: procesos GTE registrados'

    /* Transiciones: cada fila es una flecha del diagrama de estados.
       Lo terminal no lleva flag: un estatus es terminal si no aparece como origen. */
    INSERT INTO dbo.tblTransicion (IdProceso, IdEstatusOrigen, Accion, IdEstatusDestino, UsuarioRegistro)
    SELECT p.IdProceso, v.Origen, v.Accion, v.Destino, N'script-despliegue'
    FROM (VALUES
        /* WorkItem: 1 Pendiente, 2 En Proceso, 3 En Pruebas, 4 Correccion, 5 Suspendido, 6 Terminado, 7 Cancelado */
        (N'WorkItem', 1, N'INICIAR',              2),
        (N'WorkItem', 4, N'INICIAR',              2),
        (N'WorkItem', 5, N'REANUDAR',             2),
        (N'WorkItem', 2, N'SUSPENDER',            5),
        (N'WorkItem', 2, N'ENVIAR_PRUEBAS',       3),
        (N'WorkItem', 3, N'RECHAZAR_QA',          4),
        (N'WorkItem', 3, N'TERMINAR',             6),
        (N'WorkItem', 2, N'TERMINAR',             6),
        (N'WorkItem', 6, N'REVERTIR',             5),
        (N'WorkItem', 1, N'CANCELAR',             7),
        /* Solicitud: 1 Borrador, 2 Enviada, 3 En Analisis, 4 Aprobada, 5 Rechazada, 6 Convertida, 7 Cancelada */
        (N'Solicitud', 1, N'ENVIAR',              2),
        (N'Solicitud', 2, N'TOMAR',               3),
        (N'Solicitud', 3, N'APROBAR',             4),
        (N'Solicitud', 3, N'RECHAZAR',            5),
        (N'Solicitud', 3, N'DEVOLVER',            2),
        (N'Solicitud', 4, N'CONVERTIR',           6),
        (N'Solicitud', 2, N'CANCELAR',            7),
        /* Ticket: 1 Nuevo, 2 Asignado, 3 En Atencion, 4 Esperando Usuario, 5 Resuelto, 6 Cerrado */
        (N'Ticket', 1, N'ASIGNAR',                2),
        (N'Ticket', 2, N'INICIAR_ATENCION',       3),
        (N'Ticket', 3, N'ESPERAR_USUARIO',        4),
        (N'Ticket', 4, N'REANUDAR',               3),
        (N'Ticket', 3, N'RESOLVER',               5),
        (N'Ticket', 5, N'CERRAR',                 6),
        (N'Ticket', 5, N'REABRIR',                3),
        /* Release: 1 En Preparacion, 2 En Aprobacion, 3 Aprobado, 4 Liberado, 5 Revertido, 6 Cancelado */
        (N'Release', 1, N'SOLICITAR_APROBACION',  2),
        (N'Release', 2, N'APROBAR',               3),
        (N'Release', 2, N'RECHAZAR',              1),
        (N'Release', 3, N'DESPLEGAR_PROD',        4),
        (N'Release', 4, N'ROLLBACK',              5),
        (N'Release', 1, N'CANCELAR',              6),
        /* Incidente: 1 Detectado, 2 En Atencion, 3 Mitigado, 4 Resuelto, 5 Cerrado */
        (N'Incidente', 1, N'ATENDER',             2),
        (N'Incidente', 2, N'MITIGAR',             3),
        (N'Incidente', 2, N'RESOLVER',            4),
        (N'Incidente', 3, N'RESOLVER',            4),
        (N'Incidente', 4, N'CERRAR',              5),
        /* Ausencia: 1 Solicitada, 2 Aprobada, 3 Rechazada, 4 Cancelada */
        (N'Ausencia', 1, N'APROBAR',              2),
        (N'Ausencia', 1, N'RECHAZAR',             3),
        (N'Ausencia', 1, N'CANCELAR',             4),
        /* Riesgo: 1 Identificado, 2 En Mitigacion, 3 Materializado, 4 Cerrado */
        (N'Riesgo', 1, N'MITIGAR',                2),
        (N'Riesgo', 1, N'MATERIALIZAR',           3),
        (N'Riesgo', 2, N'MATERIALIZAR',           3),
        (N'Riesgo', 1, N'CERRAR',                 4),
        (N'Riesgo', 2, N'CERRAR',                 4),
        (N'Riesgo', 3, N'CERRAR',                 4),
        /* Sprint: 1 Planeado, 2 Activo, 3 Cerrado */
        (N'Sprint', 1, N'ACTIVAR',                2),
        (N'Sprint', 2, N'CERRAR',                 3),
        /* Revision: 1 Pendiente, 2 En Proceso, 3 Terminada */
        (N'Revision', 1, N'INICIAR',              2),
        (N'Revision', 2, N'TERMINAR',             3),
        (N'Revision', 3, N'REABRIR',              1),
        /* Aprobacion: 1 Pendiente, 2 Aprobada, 3 Rechazada */
        (N'Aprobacion', 1, N'APROBAR',            2),
        (N'Aprobacion', 1, N'RECHAZAR',           3),
        /* Proyecto: 1 Propuesto, 2 Autorizado, 3 En Ejecucion, 4 En Pausa, 5 Cerrado, 6 Cancelado */
        (N'Proyecto', 1, N'AUTORIZAR',            2),
        (N'Proyecto', 2, N'INICIAR',              3),
        (N'Proyecto', 3, N'PAUSAR',               4),
        (N'Proyecto', 4, N'REANUDAR',             3),
        (N'Proyecto', 3, N'CERRAR',               5),
        (N'Proyecto', 1, N'CANCELAR',             6),
        (N'Proyecto', 2, N'CANCELAR',             6)
        ) v(Proceso, Origen, Accion, Destino)
    INNER JOIN dbo.tblProceso p ON p.Proceso = v.Proceso
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicion t
                      WHERE t.IdProceso = p.IdProceso
                        AND t.IdEstatusOrigen = v.Origen
                        AND t.Accion = v.Accion)
    PRINT 'OK: transiciones GTE registradas'

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
