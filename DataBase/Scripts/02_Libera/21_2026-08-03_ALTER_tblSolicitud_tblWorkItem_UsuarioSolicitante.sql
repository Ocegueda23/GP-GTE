USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      21_2026-08-03_ALTER_tblSolicitud_tblWorkItem_UsuarioSolicitante.sql
   Autor:       Equipo GTE
   Descripcion: Extiende el patron de "Usuario solicitante" (catalogo
                tblUsuarioSolicitante, gente que puede no tener cuenta
                de GTE -- ver script 17/20 para Tickets) a Solicitudes:

                - tblSolicitud.IdUsuarioSolicitante INT NULL: capturado
                  opcionalmente al crear la solicitud (quien la registra
                  puede estar levantandola a nombre de otra persona).
                - tblWorkItem.IdUsuarioSolicitante INT NULL: se copia
                  desde la Solicitud al convertir (mismo patron que
                  IdSolicitante/IdSolicitud ya se copian hoy en
                  ConvertirSolicitudHandler), para que quede visible en
                  el WorkItem resultante sin tener que navegar a la
                  Solicitud origen.

                TRAMPA ya conocida (ver script 20): tblWorkItem ya tiene
                un FK llamado "FK_tblWorkItem_tblUsuarioSolicitante"
                para IdSolicitante->tblUsuario (nombrado por el ROL, no
                la tabla). El FK nuevo usa el sufijo "Catalogo" para no
                colisionar, igual que se hizo en tblTicket.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSolicitud' AND COLUMN_NAME = 'IdUsuarioSolicitante'
    )
    BEGIN
        ALTER TABLE dbo.tblSolicitud ADD IdUsuarioSolicitante INT NULL
        PRINT 'OK: tblSolicitud.IdUsuarioSolicitante agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblSolicitud.IdUsuarioSolicitante ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'IdUsuarioSolicitante'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem ADD IdUsuarioSolicitante INT NULL
        PRINT 'OK: tblWorkItem.IdUsuarioSolicitante agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblWorkItem.IdUsuarioSolicitante ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblSolicitud_tblUsuarioSolicitanteCatalogo'
    )
    BEGIN
        ALTER TABLE dbo.tblSolicitud
            ADD CONSTRAINT FK_tblSolicitud_tblUsuarioSolicitanteCatalogo FOREIGN KEY (IdUsuarioSolicitante)
                REFERENCES dbo.tblUsuarioSolicitante (IdUsuarioSolicitante)
        PRINT 'OK: FK_tblSolicitud_tblUsuarioSolicitanteCatalogo agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblSolicitud_tblUsuarioSolicitanteCatalogo ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblUsuarioSolicitanteCatalogo'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem
            ADD CONSTRAINT FK_tblWorkItem_tblUsuarioSolicitanteCatalogo FOREIGN KEY (IdUsuarioSolicitante)
                REFERENCES dbo.tblUsuarioSolicitante (IdUsuarioSolicitante)
        PRINT 'OK: FK_tblWorkItem_tblUsuarioSolicitanteCatalogo agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblWorkItem_tblUsuarioSolicitanteCatalogo ya existe'

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
