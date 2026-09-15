USE [bdsGTE]
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-09-02_ALTER_tblReleaseArtefacto.sql
   Autor:       Ana Viramontes
   Descripcion: Agrega la version que se libera de cada artefacto. Va en
                tblReleaseArtefacto y no en tblArtefacto porque el mismo
                artefacto se libera con versiones distintas en cada
                release: la version pertenece a la entrega, no al objeto.
                Texto libre porque el estandar Interflo usa 4 digitos para
                aplicaciones e instaladores y 3 para procedimientos
                almacenados, y ambos conviven en un mismo release.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseArtefacto'
                     AND COLUMN_NAME = 'VersionArtefacto')
    BEGIN
        ALTER TABLE [dbo].[tblReleaseArtefacto]
            ADD [VersionArtefacto] NVARCHAR(50) NULL
        PRINT 'OK: tblReleaseArtefacto.VersionArtefacto agregada -> NVARCHAR(50) NULL'
    END
    ELSE
        PRINT 'SKIP: tblReleaseArtefacto.VersionArtefacto ya existe'

    -- Los artefactos ahora se pueden editar mientras el release esta En
    -- Preparacion, asi que la tabla necesita auditoria de movimiento.
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseArtefacto'
                     AND COLUMN_NAME = 'UsuarioMovto')
    BEGIN
        ALTER TABLE [dbo].[tblReleaseArtefacto]
            ADD [UsuarioMovto] NVARCHAR(50) NULL
        PRINT 'OK: tblReleaseArtefacto.UsuarioMovto agregada -> NVARCHAR(50) NULL'
    END
    ELSE
        PRINT 'SKIP: tblReleaseArtefacto.UsuarioMovto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseArtefacto'
                     AND COLUMN_NAME = 'FechaMovto')
    BEGIN
        ALTER TABLE [dbo].[tblReleaseArtefacto]
            ADD [FechaMovto] DATETIME NULL
        PRINT 'OK: tblReleaseArtefacto.FechaMovto agregada -> DATETIME NULL'
    END
    ELSE
        PRINT 'SKIP: tblReleaseArtefacto.FechaMovto ya existe'

    -- Misma razon en respaldos: tambien pasan a ser editables.
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseRespaldo'
                     AND COLUMN_NAME = 'UsuarioMovto')
    BEGIN
        ALTER TABLE [dbo].[tblReleaseRespaldo]
            ADD [UsuarioMovto] NVARCHAR(50) NULL
        PRINT 'OK: tblReleaseRespaldo.UsuarioMovto agregada -> NVARCHAR(50) NULL'
    END
    ELSE
        PRINT 'SKIP: tblReleaseRespaldo.UsuarioMovto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseRespaldo'
                     AND COLUMN_NAME = 'FechaMovto')
    BEGIN
        ALTER TABLE [dbo].[tblReleaseRespaldo]
            ADD [FechaMovto] DATETIME NULL
        PRINT 'OK: tblReleaseRespaldo.FechaMovto agregada -> DATETIME NULL'
    END
    ELSE
        PRINT 'SKIP: tblReleaseRespaldo.FechaMovto ya existe'

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
