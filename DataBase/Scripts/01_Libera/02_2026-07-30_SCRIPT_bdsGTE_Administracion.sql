USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-07-30_SCRIPT_bdsGTE_Administracion.sql
   Autor:       Equipo GTE
   Descripcion: Modulo de administracion: areas, puestos, horarios (con
                tramos y festivos), usuarios (jerarquia por IdJefe),
                RBAC (roles, permisos, asignaciones con alcance),
                equipos y ausencias.
   Requiere:    01 (tblNivel, tblEstatusAusencia, tblTipoAusencia)
   Nota:        tblUsuarioRol.IdProyecto queda sin FK aqui; la FK fisica
                se agrega en el script 03 al crear tblProyecto.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArea')
    BEGIN
        CREATE TABLE dbo.tblArea
        (
            IdArea          INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblArea_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblArea_Activo DEFAULT (1),
            CONSTRAINT PK_tblArea PRIMARY KEY (IdArea),
            CONSTRAINT UQ_tblArea_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblArea creada'
    END
    ELSE PRINT 'SKIP: tblArea ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPuesto')
    BEGIN
        CREATE TABLE dbo.tblPuesto
        (
            IdPuesto        INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            IdArea          INT                         NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPuesto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblPuesto_Activo DEFAULT (1),
            CONSTRAINT PK_tblPuesto PRIMARY KEY (IdPuesto),
            CONSTRAINT UQ_tblPuesto_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_tblPuesto_tblArea FOREIGN KEY (IdArea) REFERENCES dbo.tblArea (IdArea)
        )
        PRINT 'OK: tblPuesto creada'
    END
    ELSE PRINT 'SKIP: tblPuesto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblHorario')
    BEGIN
        CREATE TABLE dbo.tblHorario
        (
            IdHorario       INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblHorario_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblHorario_Activo DEFAULT (1),
            CONSTRAINT PK_tblHorario PRIMARY KEY (IdHorario),
            CONSTRAINT UQ_tblHorario_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblHorario creada'
    END
    ELSE PRINT 'SKIP: tblHorario ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblHorarioTramo')
    BEGIN
        CREATE TABLE dbo.tblHorarioTramo
        (
            IdHorarioTramo  INT           IDENTITY(1,1) NOT NULL,
            IdHorario       INT                         NOT NULL,
            DiaSemana       TINYINT                     NOT NULL,
            HoraInicio      TIME(0)                     NOT NULL,
            HoraFin         TIME(0)                     NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblHorarioTramo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblHorarioTramo PRIMARY KEY (IdHorarioTramo),
            CONSTRAINT FK_tblHorarioTramo_tblHorario FOREIGN KEY (IdHorario) REFERENCES dbo.tblHorario (IdHorario),
            CONSTRAINT UQ_tblHorarioTramo_HorarioDiaInicio UNIQUE (IdHorario, DiaSemana, HoraInicio),
            CONSTRAINT CK_tblHorarioTramo_DiaSemana CHECK (DiaSemana BETWEEN 1 AND 7),
            CONSTRAINT CK_tblHorarioTramo_Horas CHECK (HoraFin > HoraInicio)
        )
        PRINT 'OK: tblHorarioTramo creada (DiaSemana: 1=lunes ... 7=domingo)'
    END
    ELSE PRINT 'SKIP: tblHorarioTramo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblDiaFestivo')
    BEGIN
        CREATE TABLE dbo.tblDiaFestivo
        (
            IdDiaFestivo    INT           IDENTITY(1,1) NOT NULL,
            Fecha           DATE                        NOT NULL,
            Descripcion     NVARCHAR(200)               NOT NULL,
            IdHorario       INT                         NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblDiaFestivo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblDiaFestivo_Activo DEFAULT (1),
            CONSTRAINT PK_tblDiaFestivo PRIMARY KEY (IdDiaFestivo),
            CONSTRAINT FK_tblDiaFestivo_tblHorario FOREIGN KEY (IdHorario) REFERENCES dbo.tblHorario (IdHorario),
            CONSTRAINT UQ_tblDiaFestivo_FechaHorario UNIQUE (Fecha, IdHorario)
        )
        PRINT 'OK: tblDiaFestivo creada (IdHorario NULL = festivo global)'
    END
    ELSE PRINT 'SKIP: tblDiaFestivo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuario')
    BEGIN
        CREATE TABLE dbo.tblUsuario
        (
            IdUsuario       INT           IDENTITY(1,1) NOT NULL,
            Dominio         NVARCHAR(100)               NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Correo          NVARCHAR(200)               NULL,
            IdPuesto        INT                         NULL,
            IdNivel         INT                         NULL,
            IdHorario       INT                         NULL,
            IdJefe          INT                         NULL,
            EsExterno       BIT                         NOT NULL CONSTRAINT DF_tblUsuario_EsExterno DEFAULT (0),
            FechaAlta       DATETIME2                   NULL,
            FechaBaja       DATETIME2                   NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblUsuario_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblUsuario_Activo DEFAULT (1),
            CONSTRAINT PK_tblUsuario PRIMARY KEY (IdUsuario),
            CONSTRAINT UQ_tblUsuario_Dominio UNIQUE (Dominio),
            CONSTRAINT FK_tblUsuario_tblPuesto FOREIGN KEY (IdPuesto) REFERENCES dbo.tblPuesto (IdPuesto),
            CONSTRAINT FK_tblUsuario_tblNivel FOREIGN KEY (IdNivel) REFERENCES dbo.tblNivel (IdNivel),
            CONSTRAINT FK_tblUsuario_tblHorario FOREIGN KEY (IdHorario) REFERENCES dbo.tblHorario (IdHorario),
            CONSTRAINT FK_tblUsuario_tblUsuario FOREIGN KEY (IdJefe) REFERENCES dbo.tblUsuario (IdUsuario)
        )
        CREATE INDEX IX_tblUsuario_Jefe ON dbo.tblUsuario (IdJefe) WHERE IdJefe IS NOT NULL
        PRINT 'OK: tblUsuario creada'
    END
    ELSE PRINT 'SKIP: tblUsuario ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRol')
    BEGIN
        CREATE TABLE dbo.tblRol
        (
            IdRol           INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            EsSistema       BIT                         NOT NULL CONSTRAINT DF_tblRol_EsSistema DEFAULT (0),
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblRol_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblRol_Activo DEFAULT (1),
            CONSTRAINT PK_tblRol PRIMARY KEY (IdRol),
            CONSTRAINT UQ_tblRol_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblRol creada'
    END
    ELSE PRINT 'SKIP: tblRol ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPermiso')
    BEGIN
        CREATE TABLE dbo.tblPermiso
        (
            IdPermiso       INT           IDENTITY(1,1) NOT NULL,
            Clave           NVARCHAR(100)               NOT NULL,
            Modulo          NVARCHAR(100)               NOT NULL,
            Descripcion     NVARCHAR(200)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPermiso_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblPermiso_Activo DEFAULT (1),
            CONSTRAINT PK_tblPermiso PRIMARY KEY (IdPermiso),
            CONSTRAINT UQ_tblPermiso_Clave UNIQUE (Clave)
        )
        PRINT 'OK: tblPermiso creada'
    END
    ELSE PRINT 'SKIP: tblPermiso ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRolPermiso')
    BEGIN
        CREATE TABLE dbo.tblRolPermiso
        (
            IdRolPermiso    INT           IDENTITY(1,1) NOT NULL,
            IdRol           INT                         NOT NULL,
            IdPermiso       INT                         NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblRolPermiso_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblRolPermiso PRIMARY KEY (IdRolPermiso),
            CONSTRAINT UQ_tblRolPermiso_RolPermiso UNIQUE (IdRol, IdPermiso),
            CONSTRAINT FK_tblRolPermiso_tblRol FOREIGN KEY (IdRol) REFERENCES dbo.tblRol (IdRol),
            CONSTRAINT FK_tblRolPermiso_tblPermiso FOREIGN KEY (IdPermiso) REFERENCES dbo.tblPermiso (IdPermiso)
        )
        PRINT 'OK: tblRolPermiso creada'
    END
    ELSE PRINT 'SKIP: tblRolPermiso ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEquipo')
    BEGIN
        CREATE TABLE dbo.tblEquipo
        (
            IdEquipo        INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            IdLider         INT                         NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblEquipo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblEquipo_Activo DEFAULT (1),
            CONSTRAINT PK_tblEquipo PRIMARY KEY (IdEquipo),
            CONSTRAINT UQ_tblEquipo_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_tblEquipo_tblUsuario FOREIGN KEY (IdLider) REFERENCES dbo.tblUsuario (IdUsuario)
        )
        PRINT 'OK: tblEquipo creada'
    END
    ELSE PRINT 'SKIP: tblEquipo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEquipoMiembro')
    BEGIN
        CREATE TABLE dbo.tblEquipoMiembro
        (
            IdEquipoMiembro       INT           IDENTITY(1,1) NOT NULL,
            IdEquipo              INT                         NOT NULL,
            IdUsuario             INT                         NOT NULL,
            RolEquipo             NVARCHAR(100)               NULL,
            PorcentajeDedicacion  DECIMAL(5,2)                NOT NULL CONSTRAINT DF_tblEquipoMiembro_PorcentajeDedicacion DEFAULT (100),
            FechaRegistro         DATETIME2                   NOT NULL CONSTRAINT DF_tblEquipoMiembro_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)               NOT NULL,
            UsuarioMovto          NVARCHAR(50)                NULL,
            FechaMovto            DATETIME                    NULL,
            Activo                BIT                         NOT NULL CONSTRAINT DF_tblEquipoMiembro_Activo DEFAULT (1),
            CONSTRAINT PK_tblEquipoMiembro PRIMARY KEY (IdEquipoMiembro),
            CONSTRAINT UQ_tblEquipoMiembro_EquipoUsuario UNIQUE (IdEquipo, IdUsuario),
            CONSTRAINT FK_tblEquipoMiembro_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo),
            CONSTRAINT FK_tblEquipoMiembro_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT CK_tblEquipoMiembro_Porcentaje CHECK (PorcentajeDedicacion > 0 AND PorcentajeDedicacion <= 100)
        )
        PRINT 'OK: tblEquipoMiembro creada'
    END
    ELSE PRINT 'SKIP: tblEquipoMiembro ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioRol')
    BEGIN
        CREATE TABLE dbo.tblUsuarioRol
        (
            IdUsuarioRol    INT           IDENTITY(1,1) NOT NULL,
            IdUsuario       INT                         NOT NULL,
            IdRol           INT                         NOT NULL,
            IdProyecto      INT                         NULL,   -- FK fisica en script 03
            IdEquipo        INT                         NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblUsuarioRol_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblUsuarioRol_Activo DEFAULT (1),
            CONSTRAINT PK_tblUsuarioRol PRIMARY KEY (IdUsuarioRol),
            CONSTRAINT FK_tblUsuarioRol_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblUsuarioRol_tblRol FOREIGN KEY (IdRol) REFERENCES dbo.tblRol (IdRol),
            CONSTRAINT FK_tblUsuarioRol_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo)
        )
        CREATE INDEX IX_tblUsuarioRol_Usuario ON dbo.tblUsuarioRol (IdUsuario)
        PRINT 'OK: tblUsuarioRol creada (alcance: NULL/NULL = global)'
    END
    ELSE PRINT 'SKIP: tblUsuarioRol ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblAusencia')
    BEGIN
        CREATE TABLE dbo.tblAusencia
        (
            IdAusencia        INT           IDENTITY(1,1) NOT NULL,
            IdUsuario         INT                         NOT NULL,
            IdTipoAusencia    INT                         NOT NULL,
            IdEstatusAusencia INT                         NOT NULL,
            FechaInicio       DATE                        NOT NULL,
            FechaFin          DATE                        NOT NULL,
            Motivo            NVARCHAR(500)               NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblAusencia_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblAusencia_Activo DEFAULT (1),
            CONSTRAINT PK_tblAusencia PRIMARY KEY (IdAusencia),
            CONSTRAINT FK_tblAusencia_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblAusencia_tblTipoAusencia FOREIGN KEY (IdTipoAusencia) REFERENCES dbo.tblTipoAusencia (Id),
            CONSTRAINT FK_tblAusencia_tblEstatusAusencia FOREIGN KEY (IdEstatusAusencia) REFERENCES dbo.tblEstatusAusencia (Id),
            CONSTRAINT CK_tblAusencia_Fechas CHECK (FechaFin >= FechaInicio)
        )
        CREATE INDEX IX_tblAusencia_UsuarioFechas ON dbo.tblAusencia (IdUsuario, FechaInicio, FechaFin)
        PRINT 'OK: tblAusencia creada'
    END
    ELSE PRINT 'SKIP: tblAusencia ya existe'

    COMMIT TRANSACTION
    PRINT '===== Parte 1 (DDL) ejecutada correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK (Parte 1) ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO

/* ---------- Parte 2: Seeds (roles, permisos, horarios heredados del GT) ---------- */
SET XACT_ABORT ON
BEGIN TRANSACTION
BEGIN TRY

    /* Roles semilla */
    INSERT INTO dbo.tblRol (Nombre, Descripcion, EsSistema, UsuarioRegistro)
    SELECT v.Nombre, v.Descripcion, 1, N'script-despliegue'
    FROM (VALUES
        (N'Administrador', N'Acceso total al sistema'),
        (N'Lider',         N'Lider de equipo: planeacion, triage, aprobaciones'),
        (N'Coordinador',   N'Coordinacion de proyectos y seguimiento'),
        (N'Desarrollador', N'Trabajo sobre items asignados'),
        (N'QA',            N'Planes, casos y ejecuciones de prueba'),
        (N'Soporte',       N'Mesa de ayuda y tickets'),
        (N'Solicitante',   N'Portal de solicitudes y consulta de sus peticiones'),
        (N'Ejecutivo',     N'Dashboard e indicadores')) v(Nombre, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblRol r WHERE r.Nombre = v.Nombre)
    PRINT 'OK: roles semilla'

    /* Permisos semilla */
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'WI.Crear',                 N'Requerimientos', N'Crear elementos de trabajo'),
        (N'WI.Editar',                N'Requerimientos', N'Editar elementos propios'),
        (N'WI.Eliminar',              N'Requerimientos', N'Eliminar elementos (borrador/pendiente)'),
        (N'WI.ModificarCompromiso',   N'Requerimientos', N'Fijar fecha compromiso en el pasado'),
        (N'WI.ModificarTerminado',    N'Requerimientos', N'Editar elementos terminados'),
        (N'WI.ModificarAjeno',        N'Requerimientos', N'Editar elementos de otras personas'),
        (N'WI.TerminarMantenimiento', N'Requerimientos', N'Terminar items de proyectos de mantenimiento'),
        (N'WI.ModificarComplejidad',  N'Requerimientos', N'Cambiar la complejidad'),
        (N'WI.ModificarTiempo',       N'Requerimientos', N'Corregir registros de tiempo'),
        (N'REV.Activar',              N'Calidad',        N'Cierre masivo de revisiones'),
        (N'REV.Reabrir',              N'Calidad',        N'Reabrir hallazgos corregidos'),
        (N'QA.GestionarPlanes',       N'Calidad',        N'Crear planes/casos de prueba'),
        (N'QA.Ejecutar',              N'Calidad',        N'Ejecutar ciclos de prueba'),
        (N'PLA.GestionarSprints',     N'Planeacion',     N'Crear/activar/cerrar sprints'),
        (N'PLA.SaltarWip',            N'Planeacion',     N'Exceder limite WIP con registro'),
        (N'SOL.Triage',               N'Solicitudes',    N'Aprobar/rechazar/convertir solicitudes'),
        (N'REL.Crear',                N'Releases',       N'Armar releases'),
        (N'REL.Aprobar',              N'Releases',       N'Firmar aprobaciones de release'),
        (N'REL.Desplegar',            N'Releases',       N'Registrar despliegues y rollbacks'),
        (N'TKT.Atender',              N'Soporte',        N'Atender tickets de la mesa'),
        (N'INC.Gestionar',            N'Operacion',      N'Gestionar incidentes'),
        (N'ADM.Usuarios',             N'Administracion', N'Gestionar usuarios'),
        (N'ADM.Roles',                N'Administracion', N'Gestionar roles y permisos'),
        (N'ADM.Workflows',            N'Administracion', N'Editar procesos y transiciones'),
        (N'ADM.Automatizaciones',     N'Administracion', N'Gestionar reglas de automatizacion'),
        (N'ADM.Suplantar',            N'Administracion', N'Iniciar sesion como otro usuario (auditado)'),
        (N'DASH.Ejecutivo',           N'Indicadores',    N'Ver dashboard ejecutivo'),
        (N'RPT.Costos',               N'Indicadores',    N'Ver reportes de costos y rentabilidad')) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos semilla'

    /* El rol Administrador recibe todos los permisos */
    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos del rol Administrador'

    /* Horarios heredados del GT (HorariosLaborales) */
    INSERT INTO dbo.tblHorario (Nombre, UsuarioRegistro)
    SELECT v.Nombre, N'script-despliegue'
    FROM (VALUES (N'BANSI'),(N'EXALXKA'),(N'EXITSEEKER'),(N'BECARIO')) v(Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblHorario h WHERE h.Nombre = v.Nombre)

    /* Tramos L-V (DiaSemana 1-5) por horario */
    INSERT INTO dbo.tblHorarioTramo (IdHorario, DiaSemana, HoraInicio, HoraFin, UsuarioRegistro)
    SELECT h.IdHorario, d.Dia, CAST(t.Ini AS TIME(0)), CAST(t.Fin AS TIME(0)), N'script-despliegue'
    FROM (VALUES
        (N'BANSI',      '08:30', '14:30'), (N'BANSI',      '17:00', '19:30'),
        (N'EXALXKA',    '08:30', '14:30'), (N'EXALXKA',    '17:00', '19:30'),
        (N'EXITSEEKER', '09:00', '14:00'), (N'EXITSEEKER', '15:00', '18:00'),
        (N'BECARIO',    '09:00', '14:00')) t(Horario, Ini, Fin)
    JOIN dbo.tblHorario h ON h.Nombre = t.Horario
    CROSS JOIN (VALUES (1),(2),(3),(4),(5)) d(Dia)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblHorarioTramo x
                      WHERE x.IdHorario = h.IdHorario AND x.DiaSemana = d.Dia
                        AND x.HoraInicio = CAST(t.Ini AS TIME(0)))
    PRINT 'OK: horarios y tramos heredados del GT'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK (Parte 2 seeds) ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
