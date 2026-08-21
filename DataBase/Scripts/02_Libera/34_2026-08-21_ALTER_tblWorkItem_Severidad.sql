USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      34_2026-08-21_ALTER_tblWorkItem_Severidad.sql
   Autor:       Equipo GTE
   Descripcion: Columna IdSeveridad NULL en tblWorkItem (FK a
                tblSeveridad, mismo catalogo que ya usa tblIncidente).

                Paridad funcional con el modulo de pruebas del GT
                (WinForms): tblErrores.IdServeridad se capturaba por cada
                defecto. En GTE el defecto es un WorkItem tipo Bug creado
                desde una ejecucion fallida (CrearBugDesdeEjecucionCommand),
                asi que la severidad se guarda ahi -- no en tblEjecucionPrueba
                ni en tblCasoPrueba, que no son el defecto en si.

                Queda NULL para WorkItems que no son bugs de QA; se llena
                en el mismo momento en que se vincula el bug a su ejecucion
                de origen (CalidadRepository.VincularBugAsync).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'IdSeveridad'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem ADD IdSeveridad INT NULL
        PRINT 'OK: tblWorkItem.IdSeveridad agregada -> INT NULL'
    END
    ELSE
        PRINT 'SKIP: tblWorkItem.IdSeveridad ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblSeveridad')
    BEGIN
        ALTER TABLE dbo.tblWorkItem
            ADD CONSTRAINT FK_tblWorkItem_tblSeveridad FOREIGN KEY (IdSeveridad) REFERENCES dbo.tblSeveridad (Id)
        PRINT 'OK: FK_tblWorkItem_tblSeveridad agregada'
    END
    ELSE
        PRINT 'SKIP: FK_tblWorkItem_tblSeveridad ya existe'

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
