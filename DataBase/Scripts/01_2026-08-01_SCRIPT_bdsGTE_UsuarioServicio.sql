/* =====================================================================
   Script:      01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql
   Autor:       Equipo GTE
   Descripcion: Crea (si no existe) el login de Windows de la cuenta de
                servicio bajo la que corre el Windows Service de
                GTE.WebApi en el servidor de destino, y le concede en
                bdsGTE solo los permisos minimos que el proceso necesita:
                lectura/escritura de datos (db_datareader/db_datawriter)
                y EXECUTE sobre los stored procedures del motor propio
                (spCambiarEstatus, spGenerarFolio, spRegistrarBitacora,
                spSnapshotKpi). Nunca db_owner ni sysadmin.

                EXCEPCION deliberada a "todos los scripts de esta carpeta
                corren solo contra bdsGTE" (ver DataBase/Scripts/README.md):
                el login es un principal de SERVIDOR y vive en [master];
                el resto del script (USER + permisos) si corre contra
                bdsGTE, como el resto de la tanda.

                AJUSTAR la variable @NombreLogin (mas abajo, en los DOS
                bloques) con la cuenta de servicio real del ambiente de
                destino antes de correr este script -- formato
                DOMINIO\cuenta si es cuenta de dominio, o
                NOMBREEQUIPO\cuenta si es una cuenta local de ese
                servidor. Ver Doctos/MANUAL_INSTALACION_GTE.md paso 1.
   =====================================================================
   Modificacion:                                                 Rev_00
   Fecha: 01 ago 2026
   Descripcion: Version inicial.
   ===================================================================== */

-- =========================================================================
-- Bloque 1: login a nivel de servidor (master), autenticacion de Windows
-- =========================================================================
USE [master]
GO
SET XACT_ABORT ON
GO
BEGIN TRANSACTION
BEGIN TRY

    DECLARE @NombreLogin SYSNAME = N'AJUSTAR\svc.gte';  -- <-- AJUSTAR antes de correr
    DECLARE @Sql NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @NombreLogin)
    BEGIN
        SET @Sql = N'CREATE LOGIN ' + QUOTENAME(@NombreLogin) + N' FROM WINDOWS
                     WITH DEFAULT_DATABASE = [bdsGTE]';
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
-- Bloque 2: usuario de base de datos + permisos minimos, dentro de bdsGTE
-- =========================================================================
USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
BEGIN TRANSACTION
BEGIN TRY

    DECLARE @NombreLogin SYSNAME = N'AJUSTAR\svc.gte';  -- <-- mismo valor que el Bloque 1
    DECLARE @Sql NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @NombreLogin)
    BEGIN
        SET @Sql = N'CREATE USER ' + QUOTENAME(@NombreLogin) + N' FOR LOGIN ' + QUOTENAME(@NombreLogin);
        EXEC sp_executesql @Sql;
        PRINT 'OK: usuario ' + @NombreLogin + ' creado en bdsGTE';
    END
    ELSE
        PRINT 'SKIP: el usuario ' + @NombreLogin + ' ya existe en bdsGTE';

    -- Lectura/escritura de datos sobre todas las tablas y vistas de bdsGTE.
    -- No es un privilegio table-por-tabla porque GTE es dueno absoluto de su
    -- unica base (ADR-03): no hay otra API ni proceso que comparta bdsGTE.
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

    IF NOT EXISTS (SELECT 1 FROM sys.database_role_members rm
                   JOIN sys.database_principals r ON r.principal_id = rm.role_principal_id
                   JOIN sys.database_principals m ON m.principal_id = rm.member_principal_id
                   WHERE r.name = 'db_datawriter' AND m.name = @NombreLogin)
    BEGIN
        SET @Sql = N'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@NombreLogin);
        EXEC sp_executesql @Sql;
        PRINT 'OK: ' + @NombreLogin + ' agregado a db_datawriter';
    END
    ELSE
        PRINT 'SKIP: ' + @NombreLogin + ' ya es miembro de db_datawriter';

    -- EXECUTE explicito solo sobre los stored procedures del motor propio
    -- (db_datareader/db_datawriter NO incluyen EXECUTE). Nada de db_owner.
    DECLARE @Procs TABLE (Nombre SYSNAME);
    INSERT INTO @Procs (Nombre) VALUES
        (N'spCambiarEstatus'), (N'spGenerarFolio'),
        (N'spRegistrarBitacora'), (N'spSnapshotKpi');

    DECLARE @Proc SYSNAME;
    DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @Procs;
    OPEN cur;
    FETCH NEXT FROM cur INTO @Proc;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (
            SELECT 1
            FROM sys.database_permissions dp
            JOIN sys.database_principals p ON p.principal_id = dp.grantee_principal_id
            WHERE dp.major_id = OBJECT_ID(N'dbo.' + @Proc)
              AND dp.permission_name = 'EXECUTE'
              AND p.name = @NombreLogin)
        BEGIN
            SET @Sql = N'GRANT EXECUTE ON dbo.' + QUOTENAME(@Proc) + N' TO ' + QUOTENAME(@NombreLogin);
            EXEC sp_executesql @Sql;
            PRINT 'OK: EXECUTE sobre dbo.' + @Proc + ' concedido a ' + @NombreLogin;
        END
        ELSE
            PRINT 'SKIP: ' + @NombreLogin + ' ya tiene EXECUTE sobre dbo.' + @Proc;

        FETCH NEXT FROM cur INTO @Proc;
    END
    CLOSE cur;
    DEALLOCATE cur;

    COMMIT TRANSACTION
    PRINT '===== Bloque 2 (usuario y permisos) ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'cur') >= -1
    BEGIN
        CLOSE cur;
        DEALLOCATE cur;
    END
    PRINT '===== ERROR en Bloque 2 (usuario y permisos) -- Se hizo ROLLBACK =====';
    PRINT 'Mensaje : ' + ERROR_MESSAGE();
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10));
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10));
    THROW;
END CATCH
GO
