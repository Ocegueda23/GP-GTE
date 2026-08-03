/* =====================================================================
   Script:      01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql
   Autor:       Equipo GTE
   Descripcion: Crea (si no existe) el LOGIN de SQL Server (usuario +
                contrasena propios de SQL Server, sin depender de ningun
                dominio/Active Directory/cuenta de Windows) bajo el cual
                se conecta el Windows Service de GTE.WebApi en el
                servidor de destino, y le concede en bdsGTE solo los
                permisos minimos que el proceso necesita:
                lectura/escritura de datos (db_datareader/db_datawriter)
                y EXECUTE sobre los stored procedures del motor propio
                (spCambiarEstatus, spGenerarFolio, spRegistrarBitacora,
                spSnapshotKpi). Nunca db_owner ni sysadmin.

                Decision deliberada (2026-08-01): autenticacion de SQL
                Server, NO de Windows. GTE no depende de que el SQL
                Server destino este en la misma maquina/dominio que el
                servidor de aplicaciones, ni de coordinar una cuenta de
                Windows con un administrador de AD -- coherente con que
                GTE tampoco depende de Entra ID para su propia
                autenticacion de aplicacion (ver Doctos/PENDIENTES.md
                seccion 4). Requisito: el SQL Server destino debe tener
                habilitado el modo mixto ("SQL Server and Windows
                Authentication mode"), no solo Windows Authentication
                (ver Doctos/MANUAL_INSTALACION_GTE.md paso 1).

                EXCEPCION deliberada a "todos los scripts de esta carpeta
                corren solo contra bdsGTE" (ver DataBase/Scripts/README.md):
                el login es un principal de SERVIDOR y vive en [master];
                el resto del script (USER + permisos) si corre contra
                bdsGTE, como el resto de la tanda.

                AJUSTAR @Password (mas abajo, en el Bloque 1) con una
                contrasena real antes de correr este script -- el script
                se detiene con error si se deja el valor de ejemplo.
                @NombreLogin ya trae un valor por default (svc_gte);
                cambiarlo solo si se necesita otro nombre, en AMBOS
                bloques. Ver Doctos/MANUAL_INSTALACION_GTE.md paso 1.
   =====================================================================
   Modificacion:                                                 Rev_01
   Fecha: 01 ago 2026
   Descripcion: Cambio de autenticacion de Windows a SQL Server (login
                propio con password, sin depender de dominio/AD).
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

    DECLARE @NombreLogin SYSNAME = N'svc_gte';
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
-- Bloque 2: usuario de base de datos + permisos minimos, dentro de bdsGTE
-- =========================================================================
USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
BEGIN TRANSACTION
BEGIN TRY

    DECLARE @NombreLogin SYSNAME = N'svc_gte';  -- <-- mismo valor que el Bloque 1
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
