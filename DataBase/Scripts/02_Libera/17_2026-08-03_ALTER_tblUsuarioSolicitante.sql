USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      17_2026-08-03_ALTER_tblUsuarioSolicitante.sql
   Autor:       Equipo GTE
   Descripcion: Bitacora estandar (InterfloClaude.md seccion 10.1) en
                dbo.tblUsuarioSolicitante -- tabla nueva creada
                manualmente por el equipo, con datos precargados, a la
                que le faltaban las columnas de auditoria:

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
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante'
    )
    BEGIN
        RAISERROR('No existe dbo.tblUsuarioSolicitante en esta base. Revisar antes de continuar.', 16, 1)
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante' AND COLUMN_NAME = 'FechaRegistro'
    )
    BEGIN
        ALTER TABLE dbo.tblUsuarioSolicitante
            ADD FechaRegistro DATETIME2 NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_FechaRegistro DEFAULT (SYSDATETIME())
        PRINT 'OK: tblUsuarioSolicitante.FechaRegistro agregada -> DATETIME2 NOT NULL DEFAULT SYSDATETIME()'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante.FechaRegistro ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante' AND COLUMN_NAME = 'UsuarioRegistro'
    )
    BEGIN
        ALTER TABLE dbo.tblUsuarioSolicitante
            ADD UsuarioRegistro NVARCHAR(200) NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_UsuarioRegistro DEFAULT (N'script-despliegue')
        PRINT 'OK: tblUsuarioSolicitante.UsuarioRegistro agregada -> NVARCHAR(200) NOT NULL'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante.UsuarioRegistro ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante' AND COLUMN_NAME = 'UsuarioMovto'
    )
    BEGIN
        ALTER TABLE dbo.tblUsuarioSolicitante ADD UsuarioMovto NVARCHAR(50) NULL
        PRINT 'OK: tblUsuarioSolicitante.UsuarioMovto agregada -> NVARCHAR(50) NULL'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante.UsuarioMovto ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante' AND COLUMN_NAME = 'FechaMovto'
    )
    BEGIN
        ALTER TABLE dbo.tblUsuarioSolicitante ADD FechaMovto DATETIME NULL
        PRINT 'OK: tblUsuarioSolicitante.FechaMovto agregada -> DATETIME NULL'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante.FechaMovto ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante' AND COLUMN_NAME = 'Activo'
    )
    BEGIN
        ALTER TABLE dbo.tblUsuarioSolicitante
            ADD Activo BIT NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_Activo DEFAULT (1)
        PRINT 'OK: tblUsuarioSolicitante.Activo agregada -> BIT NOT NULL DEFAULT 1'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante.Activo ya existe'

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
