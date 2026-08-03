USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      18_2026-08-03_ALTER_tblLocacion.sql
   Autor:       Equipo GTE
   Descripcion: Bitacora estandar (InterfloClaude.md seccion 10.1) en
                dbo.tblLocacion -- tabla nueva creada manualmente por el
                equipo, a la que le faltaban las columnas de auditoria:

                - FechaRegistro DATETIME2 NOT NULL DEFAULT SYSDATETIME()
                - UsuarioRegistro NVARCHAR(200) NOT NULL DEFAULT
                  'script-despliegue' (las filas precargadas no tienen
                  quien las dio de alta; las filas nuevas desde la app
                  siempre mandan el usuario real del token)
                - UsuarioMovto NVARCHAR(50) NULL
                - FechaMovto DATETIME NULL
                - Activo BIT NOT NULL DEFAULT 1

                No se asume ninguna otra columna de la tabla: el ADD
                COLUMN es independiente del resto del esquema.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion'
    )
    BEGIN
        RAISERROR('No existe dbo.tblLocacion en esta base. Revisar antes de continuar.', 16, 1)
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion' AND COLUMN_NAME = 'FechaRegistro'
    )
    BEGIN
        ALTER TABLE dbo.tblLocacion
            ADD FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_tblLocacion_FechaRegistro DEFAULT (SYSDATETIME())
        PRINT 'OK: tblLocacion.FechaRegistro agregada -> DATETIME2 NOT NULL DEFAULT SYSDATETIME()'
    END
    ELSE
        PRINT 'SKIP: tblLocacion.FechaRegistro ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion' AND COLUMN_NAME = 'UsuarioRegistro'
    )
    BEGIN
        ALTER TABLE dbo.tblLocacion
            ADD UsuarioRegistro NVARCHAR(200) NOT NULL CONSTRAINT DF_tblLocacion_UsuarioRegistro DEFAULT (N'script-despliegue')
        PRINT 'OK: tblLocacion.UsuarioRegistro agregada -> NVARCHAR(200) NOT NULL'
    END
    ELSE
        PRINT 'SKIP: tblLocacion.UsuarioRegistro ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion' AND COLUMN_NAME = 'UsuarioMovto'
    )
    BEGIN
        ALTER TABLE dbo.tblLocacion ADD UsuarioMovto NVARCHAR(50) NULL
        PRINT 'OK: tblLocacion.UsuarioMovto agregada -> NVARCHAR(50) NULL'
    END
    ELSE
        PRINT 'SKIP: tblLocacion.UsuarioMovto ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion' AND COLUMN_NAME = 'FechaMovto'
    )
    BEGIN
        ALTER TABLE dbo.tblLocacion ADD FechaMovto DATETIME NULL
        PRINT 'OK: tblLocacion.FechaMovto agregada -> DATETIME NULL'
    END
    ELSE
        PRINT 'SKIP: tblLocacion.FechaMovto ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion' AND COLUMN_NAME = 'Activo'
    )
    BEGIN
        ALTER TABLE dbo.tblLocacion
            ADD Activo BIT NOT NULL CONSTRAINT DF_tblLocacion_Activo DEFAULT (1)
        PRINT 'OK: tblLocacion.Activo agregada -> BIT NOT NULL DEFAULT 1'
    END
    ELSE
        PRINT 'SKIP: tblLocacion.Activo ya existe'

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
