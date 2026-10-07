USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-10-07_UPDATE_tblTransicionConfig_EtiquetaCorregir.sql
   Autor:       Ana Viramontes
   Descripcion: Un WorkItem rechazado por QA (RECHAZAR_QA) queda en Correccion
                (4), y la accion que lo regresa a En Proceso es INICIAR. Esa
                accion mostraba el boton "Iniciar" en lugar de uno que diga
                que se va a corregir: la etiqueta sale de
                tblTransicionConfig.EtiquetaBoton y, si la fila falta o esta
                inactiva, Mi dia cae al texto por omision "Iniciar" (y el
                detalle del WorkItem muestra la clave cruda de la accion).
                Este script deja la fila (WorkItem, 4, INICIAR) activa y con
                la etiqueta "Corregir": la crea si no existe o la corrige si
                existe con otra etiqueta o inactiva. Solo datos de UI; el
                grafo de transiciones (tblTransicion) no cambia.
   Requiere:    01_Libera/01_2026-07-30_INSERT_bdsGTE_TransicionesYEtiquetas.sql
                aplicado (crea tblTransicionConfig y la siembra original).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig
                   WHERE Proceso = N'WorkItem' AND IdEstatusOrigen = 4 AND Accion = N'INICIAR')
    BEGIN
        INSERT INTO dbo.tblTransicionConfig
            (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
             RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro, Activo)
        VALUES
            (N'WorkItem', 4, N'INICIAR', N'Corregir', NULL,
             0, 1, 10, N'script-despliegue', 1)
        PRINT 'OK: fila WorkItem.Correccion.INICIAR creada con etiqueta "Corregir"'
    END
    ELSE IF EXISTS (SELECT 1 FROM dbo.tblTransicionConfig
                    WHERE Proceso = N'WorkItem' AND IdEstatusOrigen = 4 AND Accion = N'INICIAR'
                      AND (EtiquetaBoton <> N'Corregir' OR Activo = 0))
    BEGIN
        UPDATE dbo.tblTransicionConfig
            SET EtiquetaBoton = N'Corregir',
                Activo = 1,
                UsuarioMovto = N'script-despliegue',
                FechaMovto = SYSDATETIME()
        WHERE Proceso = N'WorkItem' AND IdEstatusOrigen = 4 AND Accion = N'INICIAR'
        PRINT 'OK: etiqueta de WorkItem.Correccion.INICIAR cambiada a "Corregir"'
    END
    ELSE
        PRINT 'SKIP: WorkItem.Correccion.INICIAR ya esta activa con etiqueta "Corregir"'

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
