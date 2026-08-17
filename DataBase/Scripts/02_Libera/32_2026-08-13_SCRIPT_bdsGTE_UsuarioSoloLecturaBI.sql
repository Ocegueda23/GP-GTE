/* =====================================================================
   Script:      32_2026-08-13_SCRIPT_bdsGTE_UsuarioSoloLecturaBI.sql
   Autor:       Equipo GTE
   Descripcion: Crea (si no existe) un LOGIN de SQL Server de SOLO
                LECTURA para que Power BI (u otra herramienta de BI)
                consuma bdsGTE directamente -- Doctos/GTE-DocumentoMaestro.md
                seccion 13: "vistas vwBI* con contrato estable + usuario
                SQL de solo lectura". Miembro UNICAMENTE de
                db_datareader (nunca db_datawriter, nunca EXECUTE sobre
                los SPs del motor) -- puede leer toda la base, pero no
                escribir nada ni ejecutar logica de negocio. Mismo
                patron/blindaje que 01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql
                (login propio de SQL Server, sin depender de dominio/AD),
                pero sin ninguno de los permisos de escritura de ese otro
                usuario (son cuentas distintas con proposito distinto:
                svc_gte es el proceso de la API, este es solo para BI).

                AJUSTAR @Password (Bloque 1) con una contrasena real
                antes de correr este script -- el script se detiene con
                error si se deja el valor de ejemplo. @NombreLogin ya
                trae un valor por default (bi_gte_sololectura); cambiarlo
                solo si se necesita otro nombre, en AMBOS bloques.
   ===================================================================== */

-- =========================================================================
-- Bloque 1: login a nivel de servidor (master), autenticacion de SQL Server
-- =========================================================================
USE [master]
GO
SET XACT_ABORT ON
GO
BEGIN TRANSACTION
BEGIN TRY

    DECLARE @NombreLogin SYSNAME = N'bi_gte_sololectura';
    DECLARE @Password NVARCHAR(128) = N'CAMBIAR-ESTA-CONTRASENA';  -- <-- AJUSTAR antes de correr
    DECLARE @Sql NVARCHAR(MAX);

    IF @Password = N'CAMBIAR-ESTA-CONTRASENA'
    BEGIN
        RAISERROR(N'Editar la variable @Password del Bloque 1 antes de correr este script (no dejar el valor de ejemplo).', 16, 1);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @NombreLogin)
    BEGIN
        SET @Sql = N'CREATE LOGIN ' + QUOTENAME(@NombreLogin) + N'
                     WITH PASSWORD = ' + QUOTENAME(@Password, N'''') + N',
                     CHECK_POLICY = ON, CHECK_EXPIRATION = OFF,
                     DEFAULT_DATABASE = [bdsGTE]';
        EXEC sp_executesql @Sql;
        PRINT 'OK: login ' + @NombreLogin + ' creado';
    END
    ELSE
        PRINT 'SKIP: el login ' + @NombreLogin + ' ya existe';

    COMMIT TRANSACTION
    PRINT '===== Bloque 1 (login) ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    PRINT '===== ERROR en Bloque 1 (login) -- Se hizo ROLLBACK =====';
    PRINT 'Mensaje : ' + ERROR_MESSAGE();
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10));
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    THROW;
END CATCH
GO

-- =========================================================================
-- Bloque 2: usuario de base de datos, SOLO db_datareader, dentro de bdsGTE
-- =========================================================================
USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
BEGIN TRANSACTION
BEGIN TRY

    DECLARE @NombreLogin SYSNAME = N'bi_gte_sololectura';  -- <-- mismo valor que el Bloque 1
    DECLARE @Sql NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @NombreLogin)
    BEGIN
        SET @Sql = N'CREATE USER ' + QUOTENAME(@NombreLogin) + N' FOR LOGIN ' + QUOTENAME(@NombreLogin);
        EXEC sp_executesql @Sql;
        PRINT 'OK: usuario ' + @NombreLogin + ' creado en bdsGTE';
    END
    ELSE
        PRINT 'SKIP: el usuario ' + @NombreLogin + ' ya existe en bdsGTE';

    IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm
                   JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
                   JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
                   WHERE r.name = 'db_datareader' AND m.name = @NombreLogin)
    BEGIN
        SET @Sql = N'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@NombreLogin);
        EXEC sp_executesql @Sql;
        PRINT 'OK: ' + @NombreLogin + ' agregado a db_datareader';
    END
    ELSE
        PRINT 'SKIP: ' + @NombreLogin + ' ya es miembro de db_datareader';

    COMMIT TRANSACTION
    PRINT '===== Bloque 2 (usuario solo lectura) ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    PRINT '===== ERROR en Bloque 2 (usuario solo lectura) -- Se hizo ROLLBACK =====';
    PRINT 'Mensaje : ' + ERROR_MESSAGE();
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10));
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    THROW;
END CATCH
GO
