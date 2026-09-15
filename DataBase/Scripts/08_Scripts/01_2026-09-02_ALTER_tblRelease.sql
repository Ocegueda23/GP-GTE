USE [bdsGTE]
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-02_ALTER_tblRelease.sql
   Autor:       Ana Viramontes
   Descripcion: Agrega el lider asignado del release. Es un usuario del
                sistema (no texto libre) para que el listado de releases
                pueda filtrarse por lider de forma confiable. Se sigue el
                mismo patron que tblEquipo.IdLider: columna INT NULL con
                FK a tblUsuario, nulable porque un release recien creado
                todavia no tiene lider capturado.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRelease'
                     AND COLUMN_NAME = 'IdLiderAsignado')
    BEGIN
        ALTER TABLE [dbo].[tblRelease]
            ADD [IdLiderAsignado] INT NULL
        PRINT 'OK: tblRelease.IdLiderAsignado agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblRelease.IdLiderAsignado ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_tblRelease_tblUsuario')
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[tblRelease]
                   ADD CONSTRAINT FK_tblRelease_tblUsuario
                       FOREIGN KEY ([IdLiderAsignado]) REFERENCES [dbo].[tblUsuario]([IdUsuario])')
        PRINT 'OK: FK_tblRelease_tblUsuario creada'
    END
    ELSE
        PRINT 'SKIP: FK_tblRelease_tblUsuario ya existe'

    -- El listado filtra por lider asignado, asi que la columna se indexa.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_tblRelease_IdLiderAsignado'
                     AND object_id = OBJECT_ID('dbo.tblRelease'))
    BEGIN
        EXEC(N'CREATE NONCLUSTERED INDEX IX_tblRelease_IdLiderAsignado
                   ON [dbo].[tblRelease]([IdLiderAsignado])')
        PRINT 'OK: IX_tblRelease_IdLiderAsignado creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblRelease_IdLiderAsignado ya existe'

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
