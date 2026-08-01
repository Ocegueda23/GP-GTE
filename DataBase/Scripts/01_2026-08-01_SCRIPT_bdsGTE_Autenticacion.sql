USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-01_SCRIPT_bdsGTE_Autenticacion.sql
   Autor:       Equipo GTE
   Descripcion: Autenticacion propia de GTE (sin proveedor externo): hash
                de contrasena, bloqueo temporal por intentos fallidos y
                refresh tokens rotativos para la sesion de la API.
   Requiere:    02 (tblUsuario)
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario' AND COLUMN_NAME = 'PasswordHash')
    BEGIN
        ALTER TABLE dbo.tblUsuario
            ADD PasswordHash NVARCHAR(200) NULL
        PRINT 'OK: tblUsuario.PasswordHash agregada'
    END
    ELSE PRINT 'SKIP: tblUsuario.PasswordHash ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario' AND COLUMN_NAME = 'RequiereCambioPassword')
    BEGIN
        ALTER TABLE dbo.tblUsuario
            ADD RequiereCambioPassword BIT NULL
                CONSTRAINT DF_tblUsuario_RequiereCambioPassword DEFAULT (1)
        PRINT 'OK: tblUsuario.RequiereCambioPassword agregada'
    END
    ELSE PRINT 'SKIP: tblUsuario.RequiereCambioPassword ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario' AND COLUMN_NAME = 'IntentosFallidos')
    BEGIN
        ALTER TABLE dbo.tblUsuario
            ADD IntentosFallidos INT NULL
                CONSTRAINT DF_tblUsuario_IntentosFallidos DEFAULT (0)
        PRINT 'OK: tblUsuario.IntentosFallidos agregada'
    END
    ELSE PRINT 'SKIP: tblUsuario.IntentosFallidos ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario' AND COLUMN_NAME = 'BloqueadoHasta')
    BEGIN
        ALTER TABLE dbo.tblUsuario
            ADD BloqueadoHasta DATETIME2 NULL
        PRINT 'OK: tblUsuario.BloqueadoHasta agregada'
    END
    ELSE PRINT 'SKIP: tblUsuario.BloqueadoHasta ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario' AND COLUMN_NAME = 'FechaUltimoCambioPassword')
    BEGIN
        ALTER TABLE dbo.tblUsuario
            ADD FechaUltimoCambioPassword DATETIME2 NULL
        PRINT 'OK: tblUsuario.FechaUltimoCambioPassword agregada'
    END
    ELSE PRINT 'SKIP: tblUsuario.FechaUltimoCambioPassword ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRefreshToken')
    BEGIN
        CREATE TABLE dbo.tblRefreshToken
        (
            IdRefreshToken   INT           IDENTITY(1,1) NOT NULL,
            IdUsuario        INT                         NOT NULL,
            TokenHash        NVARCHAR(100)               NOT NULL,
            FechaExpiracion  DATETIME2                   NOT NULL,
            FechaRevocado    DATETIME2                   NULL,
            IdReemplazadoPor INT                         NULL,
            IpOrigen         NVARCHAR(50)                NULL,
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblRefreshToken_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)                NOT NULL,
            CONSTRAINT PK_tblRefreshToken PRIMARY KEY (IdRefreshToken),
            CONSTRAINT UQ_tblRefreshToken_TokenHash UNIQUE (TokenHash),
            CONSTRAINT FK_tblRefreshToken_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblRefreshToken_tblRefreshToken FOREIGN KEY (IdReemplazadoPor) REFERENCES dbo.tblRefreshToken (IdRefreshToken)
        )
        CREATE INDEX IX_tblRefreshToken_Usuario ON dbo.tblRefreshToken (IdUsuario)
        CREATE INDEX IX_tblRefreshToken_Vigentes ON dbo.tblRefreshToken (IdUsuario, FechaExpiracion) WHERE FechaRevocado IS NULL
        PRINT 'OK: tblRefreshToken creada'
    END
    ELSE PRINT 'SKIP: tblRefreshToken ya existe'

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
GO
