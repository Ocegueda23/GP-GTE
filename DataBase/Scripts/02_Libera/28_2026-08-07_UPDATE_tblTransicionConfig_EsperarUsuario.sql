USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      28_2026-08-07_UPDATE_tblTransicionConfig_EsperarUsuario.sql
   Autor:       Equipo GTE
   Descripcion: La transicion ESPERAR_USUARIO del proceso Ticket (origen 3,
                En Atencion) pasa a exigir motivo -- el agente debe explicar
                que espera del solicitante. CambiarEstatusTicketHandler
                publica ese motivo como comentario visible en el hilo del
                ticket (Entidad='Ticket') y notifica al solicitante; antes
                el boton disparaba la transicion sin capturar nada.
   Requiere:    12_2026-08-02_INSERT_bdsGTE_TicketsConfig.sql aplicado.
   ===================================================================== */
BEGIN TRY

    IF EXISTS (SELECT 1 FROM dbo.tblTransicionConfig
               WHERE Proceso = N'Ticket' AND IdEstatusOrigen = 3 AND Accion = N'ESPERAR_USUARIO'
                 AND RequiereMotivo = 0)
    BEGIN
        UPDATE dbo.tblTransicionConfig
            SET RequiereMotivo = 1,
                UsuarioMovto = N'script-despliegue',
                FechaMovto = SYSDATETIME()
        WHERE Proceso = N'Ticket' AND IdEstatusOrigen = 3 AND Accion = N'ESPERAR_USUARIO'
        PRINT 'OK: Ticket.ESPERAR_USUARIO ahora exige motivo'
    END
    ELSE
        PRINT 'SKIP: Ticket.ESPERAR_USUARIO ya exige motivo o no existe la fila'

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
