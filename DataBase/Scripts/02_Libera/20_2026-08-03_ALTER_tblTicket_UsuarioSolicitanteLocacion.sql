USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      20_2026-08-03_ALTER_tblTicket_UsuarioSolicitanteLocacion.sql
   Autor:       Equipo GTE
   Descripcion: Dos columnas nuevas en tblTicket para que el ingeniero
                de soporte capture, al registrar el ticket, quien es el
                solicitante real (catalogo tblUsuarioSolicitante -- gente
                que puede no tener cuenta de GTE, distinto de
                tblTicket.IdSolicitante que es la cuenta interna de
                quien da de alta el ticket) y la locacion (catalogo
                tblLocacion). Ambas opcionales (NULL): un ticket de
                autoservicio del portal no necesita capturarlas.

                - IdUsuarioSolicitante INT NULL, FK a tblUsuarioSolicitante
                - IdLocacion INT NULL, FK a tblLocacion

                Requiere: 17, 18 y 19 aplicados antes (tblUsuarioSolicitante
                y tblLocacion con su bitacora).

                TRAMPA encontrada probando en LocalDB antes de tocar
                produccion: el FK existente IdSolicitante->tblUsuario ya
                se llama "FK_tblTicket_tblUsuarioSolicitante" (nombrado
                por el ROL, no por la tabla destino -- ver
                DbContextGTE.cs). Un IF NOT EXISTS con ese mismo nombre
                para el FK nuevo (IdUsuarioSolicitante->tblUsuarioSolicitante)
                choca y se salta SIN avisar, dejando la columna nueva sin
                FK real. Por eso el FK de este script usa el sufijo
                "Catalogo" para no colisionar.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTicket' AND COLUMN_NAME = 'IdUsuarioSolicitante'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket ADD IdUsuarioSolicitante INT NULL
        PRINT 'OK: tblTicket.IdUsuarioSolicitante agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblTicket.IdUsuarioSolicitante ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTicket' AND COLUMN_NAME = 'IdLocacion'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket ADD IdLocacion INT NULL
        PRINT 'OK: tblTicket.IdLocacion agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblTicket.IdLocacion ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTicket_tblUsuarioSolicitanteCatalogo'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket
            ADD CONSTRAINT FK_tblTicket_tblUsuarioSolicitanteCatalogo FOREIGN KEY (IdUsuarioSolicitante)
                REFERENCES dbo.tblUsuarioSolicitante (IdUsuarioSolicitante)
        PRINT 'OK: FK_tblTicket_tblUsuarioSolicitanteCatalogo agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblTicket_tblUsuarioSolicitanteCatalogo ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblTicket_tblLocacion'
    )
    BEGIN
        ALTER TABLE dbo.tblTicket
            ADD CONSTRAINT FK_tblTicket_tblLocacion FOREIGN KEY (IdLocacion)
                REFERENCES dbo.tblLocacion (IdLocacion)
        PRINT 'OK: FK_tblTicket_tblLocacion agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblTicket_tblLocacion ya existe'

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
