USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      16_2026-08-03_ALTER_tblTicket.sql
   Autor:       Equipo GTE
   Descripcion: Dos columnas nuevas en tblTicket para el cierre de un
                ticket resuelto por el propio ingeniero de soporte:

                - Solucion NVARCHAR(MAX) NULL: descripcion de la solucion
                  aplicada, capturada al ejecutar la accion RESOLVER.

                - MinutosSolucion INT NULL: tiempo invertido en resolver
                  el ticket, capturado junto con Solucion en RESOLVER (un
                  solo valor total, no un log repetido como
                  tblRegistroTiempo de WorkItem -- un ticket se resuelve
                  una sola vez).

                Ambas quedan NULL para tickets en Nuevo/Asignado/En
                Atencion/Esperando Usuario; RESOLVER las exige (validado
                en CambiarEstatusTicketHandler, no en la BD).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTicket' AND COLUMN_NAME = 'Solucion'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket ADD Solucion NVARCHAR(MAX) NULL
        PRINT 'OK: tblTicket.Solucion agregada -> NVARCHAR(MAX) NULL'
    END
    ELSE
        PRINT 'SKIP: tblTicket.Solucion ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTicket' AND COLUMN_NAME = 'MinutosSolucion'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket ADD MinutosSolucion INT NULL
        PRINT 'OK: tblTicket.MinutosSolucion agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblTicket.MinutosSolucion ya existe'

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
