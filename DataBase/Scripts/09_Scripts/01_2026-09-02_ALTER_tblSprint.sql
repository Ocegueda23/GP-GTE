USE [bdsGTE]
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-02_ALTER_tblSprint.sql
   Autor:       Ana Viramontes
   Descripcion: El sprint deja de asignarse por Equipo y pasa a asignarse
                por Lider (un usuario del sistema, no texto libre), mismo
                patron que tblEquipo.IdLider / tblRelease.IdLiderAsignado.
                Se agrega tambien el Folio del sprint (serie SPR via
                spGenerarFolio). IdEquipo se conserva NULLABLE (no se
                dropea la columna ni la FK) por trazabilidad historica de
                los sprints ya cerrados; queda deprecated, candidato a
                DROP en una limpieza futura una vez confirmado que nada
                mas la usa.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSprint'
                     AND COLUMN_NAME = 'Folio')
    BEGIN
        ALTER TABLE [dbo].[tblSprint]
            ADD [Folio] NVARCHAR(20) NULL
        PRINT 'OK: tblSprint.Folio agregada -> NVARCHAR(20) NULL'
    END
    ELSE
        PRINT 'SKIP: tblSprint.Folio ya existe'

    -- Filtrado (WHERE Folio IS NOT NULL): los sprints existentes no llevan folio
    -- retroactivo y conviven varios NULL sin violar la unicidad.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'UQ_tblSprint_Folio'
                     AND object_id = OBJECT_ID('dbo.tblSprint'))
    BEGIN
        EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UQ_tblSprint_Folio
                   ON [dbo].[tblSprint]([Folio]) WHERE [Folio] IS NOT NULL')
        PRINT 'OK: UQ_tblSprint_Folio creado'
    END
    ELSE
        PRINT 'SKIP: UQ_tblSprint_Folio ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSprint'
                     AND COLUMN_NAME = 'IdLider')
    BEGIN
        ALTER TABLE [dbo].[tblSprint]
            ADD [IdLider] INT NULL
        PRINT 'OK: tblSprint.IdLider agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblSprint.IdLider ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
                   WHERE name = 'FK_tblSprint_tblUsuario_IdLider')
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[tblSprint]
                   ADD CONSTRAINT FK_tblSprint_tblUsuario_IdLider
                       FOREIGN KEY ([IdLider]) REFERENCES [dbo].[tblUsuario]([IdUsuario])')
        PRINT 'OK: FK_tblSprint_tblUsuario_IdLider creada'
    END
    ELSE
        PRINT 'SKIP: FK_tblSprint_tblUsuario_IdLider ya existe'

    -- El listado y la regla "un sprint Activo por lider" filtran por esta columna.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_tblSprint_IdLider'
                     AND object_id = OBJECT_ID('dbo.tblSprint'))
    BEGIN
        EXEC(N'CREATE NONCLUSTERED INDEX IX_tblSprint_IdLider
                   ON [dbo].[tblSprint]([IdLider])')
        PRINT 'OK: IX_tblSprint_IdLider creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblSprint_IdLider ya existe'

    -- IdEquipo deja de ser obligatorio: los sprints nuevos ya no lo capturan.
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
              WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSprint'
                AND COLUMN_NAME = 'IdEquipo' AND IS_NULLABLE = 'NO')
    BEGIN
        ALTER TABLE [dbo].[tblSprint]
            ALTER COLUMN [IdEquipo] INT NULL
        PRINT 'OK: tblSprint.IdEquipo -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblSprint.IdEquipo ya es nullable'

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
