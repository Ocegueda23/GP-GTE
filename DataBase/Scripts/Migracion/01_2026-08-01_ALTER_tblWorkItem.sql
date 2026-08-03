USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-01_ALTER_tblWorkItem.sql
   Autor:       Equipo GTE
   Descripcion: Dos columnas nuevas en tblWorkItem, detectadas como gap
                de esquema al disenar la migracion de datos del GT (B3):

                - Locacion NVARCHAR(100) NULL: el Documento Maestro §15.4
                  preveia que el campo "Release" del GT, reutilizado como
                  locacion en tareas categoria TI, se separara en un
                  campo propio -- nunca se creo la columna real.

                - IdEquipo INT NULL (FK a tblEquipo): al perfilar los
                  datos reales del GT se encontro que la division/equipo
                  que ejecuta una tarea es un atributo POR TAREA, no por
                  proyecto (45% de los proyectos reales mezclan varias
                  divisiones en sus tareas, incluyendo los dos mas
                  grandes) -- tblProyecto.IdEquipo (FK unico) no puede
                  representarlo. Se agrega el vinculo directo en
                  tblWorkItem en vez de en tblProyecto.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'Locacion'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem ADD Locacion NVARCHAR(100) NULL
        PRINT 'OK: tblWorkItem.Locacion agregada -> NVARCHAR(100) NULL'
    END
    ELSE
        PRINT 'SKIP: tblWorkItem.Locacion ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'IdEquipo'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem ADD IdEquipo INT NULL
        PRINT 'OK: tblWorkItem.IdEquipo agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblWorkItem.IdEquipo ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblEquipo'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem
            ADD CONSTRAINT FK_tblWorkItem_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo)
        PRINT 'OK: FK_tblWorkItem_tblEquipo agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblWorkItem_tblEquipo ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes WHERE name = 'IX_tblWorkItem_Equipo'
    )
    BEGIN
        CREATE INDEX IX_tblWorkItem_Equipo ON dbo.tblWorkItem (IdEquipo)
        PRINT 'OK: IX_tblWorkItem_Equipo agregado'
    END
    ELSE
        PRINT 'SKIP: IX_tblWorkItem_Equipo ya existe'

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
