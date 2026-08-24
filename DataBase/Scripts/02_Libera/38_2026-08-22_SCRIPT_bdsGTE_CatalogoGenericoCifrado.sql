USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      38_2026-08-22_SCRIPT_bdsGTE_CatalogoGenericoCifrado.sql
   Autor:       Equipo GTE
   Descripcion: Cifrado de columnas del motor de catalogos genericos
                (script 37). Se usa Microsoft.AspNetCore.DataProtection
                (IDataProtector por purpose = nombre de columna) en vez de
                gestionar claves AES a mano; su key ring se persiste aqui
                en vez del filesystem por defecto (ADR-03: base unica,
                mismo principio que el schema propio de Hangfire dentro
                de bdsGTE). Tabla de sistema, fuera del scaffold de EF:
                solo la toca RepositorioClavesProteccionSql via ADO.NET.
   Requiere:    37 (tblCatalogoGenerico/tblCatalogoGenericoColumna) aplicado.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSysDataProtectionKeys')
    BEGIN
        CREATE TABLE dbo.tblSysDataProtectionKeys
        (
            Id              INT           IDENTITY(1,1) NOT NULL,
            FriendlyName    NVARCHAR(400)               NULL,
            Xml             NVARCHAR(MAX)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblSysDataProtectionKeys_FechaRegistro DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblSysDataProtectionKeys PRIMARY KEY (Id)
        )
        PRINT 'OK: tblSysDataProtectionKeys creada'
    END
    ELSE PRINT 'SKIP: tblSysDataProtectionKeys ya existe'

    -- Los permisos CAT.<CLAVE>.Descifrar no se siembran aqui: son dinamicos por catalogo,
    -- igual que Ver/Crear/Editar/Eliminar (ver CrearCatalogoCommand/SembrarPermisosAsync).
    -- Los catalogos ya existentes antes de este script reciben su permiso Descifrar la
    -- proxima vez que se guarde su configuracion de columnas (ActualizarConfigColumnas
    -- vuelve a llamar SembrarPermisosAsync, que es idempotente).

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
