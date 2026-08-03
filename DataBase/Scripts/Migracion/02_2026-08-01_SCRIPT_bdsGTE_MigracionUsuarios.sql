USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-08-01_SCRIPT_bdsGTE_MigracionUsuarios.sql
   Autor:       Equipo GTE
   Descripcion: Migracion B3 (Doctos/PENDIENTES.md 3.1), bloque Usuarios.
                Origen: bdsApollo.sysUsr (misma base que bdsInfo, solo
                renombrada -- ver Doctos/PENDIENTES.md). Alcance: SOLO 5
                de los 6 usuarios reales (David Altamirano, Analista de
                Datos, se excluye por decision explicita del negocio).

                Ana Viramontes YA EXISTE en bdsGTE como 'aviramontes'
                (IdUsuario = 1, rol Administrador ya asignado desde el
                bootstrap) -- este script NO la toca, solo la referencia
                como IdJefe/IdLider de los demas.

                Incluye tambien:
                - Horario 'INTERFLO' (no existia en el seed original;
                  los 6 usuarios reales lo usan, con tramos distintos a
                  los que el seed de bdsGTE atribuye a 'BANSI').
                - Usuario placeholder 'Solicitante Externo' (EsExterno=1),
                  usado como respaldo del FK obligatorio
                  tblSolicitud.IdSolicitante para las solicitudes cuyo
                  solicitante real no es ninguno de los usuarios migrados
                  (ver script 04, migracion de Solicitudes).

                Repetible: cada INSERT esta guardado por Dominio (UNIQUE),
                se puede correr contra un bdsGTE recien restaurado tantas
                veces como haga falta sin duplicar.
   ===================================================================== */
BEGIN TRY

    -- =================================================================
    -- Horario INTERFLO (nuevo; no se toca el seed existente BANSI/
    -- EXALXKA/EXITSEEKER/BECARIO)
    -- =================================================================
    DECLARE @IdHorarioInterflo INT;

    IF NOT EXISTS (SELECT 1 FROM dbo.tblHorario WHERE Nombre = N'INTERFLO')
    BEGIN
        INSERT INTO dbo.tblHorario (Nombre, FechaRegistro, UsuarioRegistro, Activo)
        VALUES (N'INTERFLO', SYSDATETIME(), N'migracion-gt', 1)
        PRINT 'OK: tblHorario INTERFLO creado'
    END
    ELSE
        PRINT 'SKIP: tblHorario INTERFLO ya existe'

    SELECT @IdHorarioInterflo = IdHorario FROM dbo.tblHorario WHERE Nombre = N'INTERFLO'

    IF NOT EXISTS (SELECT 1 FROM dbo.tblHorarioTramo WHERE IdHorario = @IdHorarioInterflo AND DiaSemana = 1 AND HoraInicio = '08:30:00')
    BEGIN
        INSERT INTO dbo.tblHorarioTramo (IdHorario, DiaSemana, HoraInicio, HoraFin, FechaRegistro, UsuarioRegistro)
        SELECT @IdHorarioInterflo, dia, '08:30:00', '14:00:00', SYSDATETIME(), N'migracion-gt'
        FROM (VALUES (1),(2),(3),(4),(5)) d(dia)
        PRINT 'OK: tramos 08:30-14:00 (L-V) de INTERFLO creados'
    END
    ELSE
        PRINT 'SKIP: tramos 08:30-14:00 de INTERFLO ya existen'

    IF NOT EXISTS (SELECT 1 FROM dbo.tblHorarioTramo WHERE IdHorario = @IdHorarioInterflo AND DiaSemana = 1 AND HoraInicio = '15:00:00')
    BEGIN
        INSERT INTO dbo.tblHorarioTramo (IdHorario, DiaSemana, HoraInicio, HoraFin, FechaRegistro, UsuarioRegistro)
        SELECT @IdHorarioInterflo, dia, '15:00:00', '18:30:00', SYSDATETIME(), N'migracion-gt'
        FROM (VALUES (1),(2),(3),(4),(5)) d(dia)
        PRINT 'OK: tramos 15:00-18:30 (L-V) de INTERFLO creados'
    END
    ELSE
        PRINT 'SKIP: tramos 15:00-18:30 de INTERFLO ya existen'

    -- =================================================================
    -- Usuarios migrados (4 nuevos; Ana Viramontes ya existe, David
    -- Altamirano excluido por decision del negocio)
    -- =================================================================
    DECLARE @Usuarios TABLE (
        Dominio NVARCHAR(100), Nombre NVARCHAR(200), Correo NVARCHAR(200),
        NivelOrigenGT INT, RolDestino NVARCHAR(100)
    );
    INSERT INTO @Usuarios (Dominio, Nombre, Correo, NivelOrigenGT, RolDestino) VALUES
        (N'Antonio.Ochoa',   N'Jose Antonio Ochoa Torres', N'desarrollador@interflo.com.mx',  3, N'Desarrollador'),
        (N'Roberto.Gonzalez',N'Roberto Gonzalez',          N'informatica2@interflo.com.mx',   3, N'Lider'),
        (N'Roberto.Lopez',   N'Roberto Lopez',             N'informatica@interflo.com.mx',    3, N'Lider'),
        (N'Jose.Hernandez',  N'Jose Carlos Hernandez',     N'Jose.Hernandez@Interflo.com.mx', 3, N'Desarrollador');
    -- NivelOrigenGT usa el codigo REAL del EAV de origen (Tabla='Nivel'):
    -- 1=SENIOR, 2=MASTER, 3=JUNIOR (invertido respecto al seed de GTE
    -- Junior=1/Senior=2/Master=3) -- los 4 usuarios migrados son JUNIOR(3)
    -- en el origen, que mapea a tblNivel.Junior (Id=1) en GTE.
    -- Dominio usa NikName del origen, no la columna Dominio cruda: la de
    -- Jose Hernandez venia corrupta ('Gerencia RH' en vez de su usuario).

    DECLARE @Dominio NVARCHAR(100), @Nombre NVARCHAR(200), @Correo NVARCHAR(200),
            @NivelOrigen INT, @RolDestino NVARCHAR(100), @IdNivelGTE INT, @IdUsuarioNuevo INT, @IdRol INT;

    DECLARE curUsr CURSOR LOCAL FAST_FORWARD FOR SELECT Dominio, Nombre, Correo, NivelOrigenGT, RolDestino FROM @Usuarios;
    OPEN curUsr;
    FETCH NEXT FROM curUsr INTO @Dominio, @Nombre, @Correo, @NivelOrigen, @RolDestino;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @IdNivelGTE = CASE @NivelOrigen WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 1 END; -- SENIOR->2, MASTER->3, JUNIOR->1

        IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuario WHERE Dominio = @Dominio)
        BEGIN
            INSERT INTO dbo.tblUsuario
                (Dominio, Nombre, Correo, IdNivel, IdHorario, IdJefe, EsExterno,
                 RequiereCambioPassword, IntentosFallidos,
                 FechaRegistro, UsuarioRegistro, Activo)
            VALUES
                (@Dominio, @Nombre, @Correo, @IdNivelGTE, @IdHorarioInterflo, NULL, 0,
                 1, 0,
                 SYSDATETIME(), N'migracion-gt', 1)
            PRINT 'OK: usuario ' + @Dominio + ' creado'
        END
        ELSE
            PRINT 'SKIP: usuario ' + @Dominio + ' ya existe'

        SELECT @IdUsuarioNuevo = IdUsuario FROM dbo.tblUsuario WHERE Dominio = @Dominio;

        -- Jefe: los 4 reportan a Ana Viramontes (aviramontes, IdUsuario=1 en bdsGTE)
        UPDATE dbo.tblUsuario SET IdJefe = 1 WHERE IdUsuario = @IdUsuarioNuevo AND (IdJefe IS NULL OR IdJefe <> 1)

        -- Rol
        SELECT @IdRol = IdRol FROM dbo.tblRol WHERE Nombre = @RolDestino;
        IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuarioRol WHERE IdUsuario = @IdUsuarioNuevo AND IdRol = @IdRol AND IdProyecto IS NULL AND IdEquipo IS NULL)
        BEGIN
            INSERT INTO dbo.tblUsuarioRol (IdUsuario, IdRol, IdProyecto, IdEquipo, FechaRegistro, UsuarioRegistro, Activo)
            VALUES (@IdUsuarioNuevo, @IdRol, NULL, NULL, SYSDATETIME(), N'migracion-gt', 1)
            PRINT 'OK: rol ' + @RolDestino + ' asignado a ' + @Dominio
        END
        ELSE
            PRINT 'SKIP: ' + @Dominio + ' ya tiene el rol ' + @RolDestino

        FETCH NEXT FROM curUsr INTO @Dominio, @Nombre, @Correo, @NivelOrigen, @RolDestino;
    END
    CLOSE curUsr;
    DEALLOCATE curUsr;

    -- =================================================================
    -- Placeholder "Solicitante Externo" (ancla de FK para Solicitudes
    -- cuyo solicitante real no esta entre los usuarios migrados; ver
    -- script 04)
    -- =================================================================
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUsuario WHERE Dominio = N'solicitante-externo-gt')
    BEGIN
        INSERT INTO dbo.tblUsuario
            (Dominio, Nombre, EsExterno, RequiereCambioPassword, IntentosFallidos,
             FechaRegistro, UsuarioRegistro, Activo)
        VALUES
            (N'solicitante-externo-gt', N'Solicitante Externo (migracion GT)', 1, 1, 0,
             SYSDATETIME(), N'migracion-gt', 1)
        PRINT 'OK: usuario placeholder solicitante-externo-gt creado'
    END
    ELSE
        PRINT 'SKIP: usuario placeholder solicitante-externo-gt ya existe'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'curUsr') >= -1
    BEGIN
        CLOSE curUsr;
        DEALLOCATE curUsr;
    END
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
