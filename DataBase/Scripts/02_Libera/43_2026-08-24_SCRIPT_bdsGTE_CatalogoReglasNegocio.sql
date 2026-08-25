USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      43_2026-08-24_SCRIPT_bdsGTE_CatalogoReglasNegocio.sql
   Autor:       Equipo GTE
   Descripcion: Crea el modulo Catalogo de Reglas de Negocio.

                Modelo (decidido con el equipo 2026-08-24):

                1. Toda regla de negocio NACE en un proyecto y ese
                   proyecto es su DUENO (tblReglaNegocio.IdProyecto,
                   NOT NULL). No existen reglas globales ni reglas por
                   criterio (categoria, programa): se descarto a
                   proposito porque una regla siempre se piensa desde
                   un sistema concreto.

                2. Una regla puede AFECTAR a otros proyectos. Esos
                   proyectos secundarios se enlistan uno por uno en
                   tblReglaNegocioImpacto -- captura explicita, nunca
                   derivada. Al consultar las reglas de un proyecto
                   secundario, la regla se muestra con la etiqueta de
                   su proyecto de origen y en solo lectura: el
                   enunciado se edita unicamente en el proyecto dueno,
                   de modo que nunca existan dos versiones del mismo
                   texto.

                3. Dentro de su proyecto, la regla se ubica en UN
                   flujo de operacion O en UNA caracteristica del
                   sistema (tblAmbitoRegla, FK simple y nullable en la
                   regla). Nunca en ambos a la vez: si aplica a los
                   dos casos se dan de alta dos reglas. tblAmbitoRegla
                   es catalogo PROPIO de cada proyecto -- los flujos
                   de un sistema no son los de otro.

                4. El enunciado se versiona en tblReglaNegocioVersion
                   (mismo patron que tblArticuloVersion de la Base de
                   conocimiento): una regla de negocio que cambia sin
                   dejar rastro de que decia antes es un problema de
                   auditoria.

                Integridad del punto 2: una regla no puede impactar a
                su propio proyecto dueno. Se garantiza de forma
                declarativa (sin trigger) desnormalizando IdProyectoDueno
                en la fila de impacto, amarrandolo con FK compuesta a
                tblReglaNegocio (IdReglaNegocio, IdProyecto) y un CHECK
                IdProyectoAfectado <> IdProyectoDueno.

                Permisos nuevos RGN.Ver / RGN.Administrar, sembrados
                solo al rol Administrador (mismo patron que los scripts
                23, 36 y 42); el equipo los asigna a otros roles desde
                Administracion > Roles.

   Requiere:    01, 02 y 03 aplicados (tblProyecto, tblPermiso, tblRol,
                tblRolPermiso).
   ===================================================================== */
BEGIN TRY

    /* ---------------------------------------------------------------
       1. Enumerados de ID fijo (los IDs son CONTRATO: los referencia
          el backend en GTE.Domain/ReglasNegocio). No reordenar.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTipoAmbitoRegla')
    BEGIN
        CREATE TABLE dbo.tblTipoAmbitoRegla
        (
            Id              INT           NOT NULL,
            Nombre          NVARCHAR(100) NOT NULL,
            FechaRegistro   DATETIME2     NOT NULL CONSTRAINT DF_tblTipoAmbitoRegla_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200) NOT NULL,
            UsuarioMovto    NVARCHAR(50)  NULL,
            FechaMovto      DATETIME      NULL,
            Activo          BIT           NOT NULL CONSTRAINT DF_tblTipoAmbitoRegla_Activo DEFAULT (1),
            CONSTRAINT PK_tblTipoAmbitoRegla PRIMARY KEY (Id),
            CONSTRAINT UQ_tblTipoAmbitoRegla_Nombre UNIQUE (Nombre)
        );
        PRINT 'OK: tblTipoAmbitoRegla creada'
    END
    ELSE
        PRINT 'SKIP: tblTipoAmbitoRegla ya existe'

    INSERT INTO dbo.tblTipoAmbitoRegla (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES
        (1, N'Flujo de operacion'),
        (2, N'Caracteristica del sistema')
        ) v(Id, Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoAmbitoRegla t WHERE t.Id = v.Id)
    PRINT 'OK: tblTipoAmbitoRegla sembrada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEstadoReglaNegocio')
    BEGIN
        CREATE TABLE dbo.tblEstadoReglaNegocio
        (
            Id              INT           NOT NULL,
            Nombre          NVARCHAR(100) NOT NULL,
            FechaRegistro   DATETIME2     NOT NULL CONSTRAINT DF_tblEstadoReglaNegocio_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200) NOT NULL,
            UsuarioMovto    NVARCHAR(50)  NULL,
            FechaMovto      DATETIME      NULL,
            Activo          BIT           NOT NULL CONSTRAINT DF_tblEstadoReglaNegocio_Activo DEFAULT (1),
            CONSTRAINT PK_tblEstadoReglaNegocio PRIMARY KEY (Id),
            CONSTRAINT UQ_tblEstadoReglaNegocio_Nombre UNIQUE (Nombre)
        );
        PRINT 'OK: tblEstadoReglaNegocio creada'
    END
    ELSE
        PRINT 'SKIP: tblEstadoReglaNegocio ya existe'

    INSERT INTO dbo.tblEstadoReglaNegocio (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES
        (1, N'Vigente'),
        (2, N'Implementada parcialmente'),
        (3, N'Documentada sin implementar'),
        (4, N'Derogada')
        ) v(Id, Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstadoReglaNegocio e WHERE e.Id = v.Id)
    PRINT 'OK: tblEstadoReglaNegocio sembrada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTipoRelacionRegla')
    BEGIN
        CREATE TABLE dbo.tblTipoRelacionRegla
        (
            Id              INT           NOT NULL,
            Nombre          NVARCHAR(100) NOT NULL,
            FechaRegistro   DATETIME2     NOT NULL CONSTRAINT DF_tblTipoRelacionRegla_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200) NOT NULL,
            UsuarioMovto    NVARCHAR(50)  NULL,
            FechaMovto      DATETIME      NULL,
            Activo          BIT           NOT NULL CONSTRAINT DF_tblTipoRelacionRegla_Activo DEFAULT (1),
            CONSTRAINT PK_tblTipoRelacionRegla PRIMARY KEY (Id),
            CONSTRAINT UQ_tblTipoRelacionRegla_Nombre UNIQUE (Nombre)
        );
        PRINT 'OK: tblTipoRelacionRegla creada'
    END
    ELSE
        PRINT 'SKIP: tblTipoRelacionRegla ya existe'

    INSERT INTO dbo.tblTipoRelacionRegla (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES
        (1, N'Complementa a'),
        (2, N'Depende de'),
        (3, N'En conflicto con'),
        (4, N'Sustituye a')
        ) v(Id, Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoRelacionRegla t WHERE t.Id = v.Id)
    PRINT 'OK: tblTipoRelacionRegla sembrada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    /* ---------------------------------------------------------------
       2. tblAmbitoRegla: flujos de operacion y caracteristicas del
          sistema, PROPIOS de cada proyecto.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblAmbitoRegla')
    BEGIN
        CREATE TABLE dbo.tblAmbitoRegla
        (
            IdAmbitoRegla      INT IDENTITY(1,1) NOT NULL,
            IdProyecto         INT               NOT NULL,
            IdTipoAmbitoRegla  INT               NOT NULL,
            Nombre             NVARCHAR(200)     NOT NULL,
            Descripcion        NVARCHAR(1000)    NULL,
            FechaRegistro      DATETIME2         NOT NULL CONSTRAINT DF_tblAmbitoRegla_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)     NOT NULL,
            UsuarioMovto       NVARCHAR(50)      NULL,
            FechaMovto         DATETIME          NULL,
            Activo             BIT               NOT NULL CONSTRAINT DF_tblAmbitoRegla_Activo DEFAULT (1),
            CONSTRAINT PK_tblAmbitoRegla PRIMARY KEY (IdAmbitoRegla),
            CONSTRAINT FK_tblAmbitoRegla_Proyecto
                FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblAmbitoRegla_TipoAmbito
                FOREIGN KEY (IdTipoAmbitoRegla) REFERENCES dbo.tblTipoAmbitoRegla (Id),
            CONSTRAINT UQ_tblAmbitoRegla_ProyectoTipoNombre
                UNIQUE (IdProyecto, IdTipoAmbitoRegla, Nombre)
        );
        CREATE INDEX IX_tblAmbitoRegla_Proyecto ON dbo.tblAmbitoRegla (IdProyecto, Activo);
        PRINT 'OK: tblAmbitoRegla creada'
    END
    ELSE
        PRINT 'SKIP: tblAmbitoRegla ya existe'

    /* ---------------------------------------------------------------
       3. tblReglaNegocio: la regla. Un dueno, una clave unica dentro
          de ese dueno, un ambito opcional.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReglaNegocio')
    BEGIN
        CREATE TABLE dbo.tblReglaNegocio
        (
            IdReglaNegocio         INT IDENTITY(1,1) NOT NULL,
            IdProyecto             INT               NOT NULL,
            Clave                  NVARCHAR(30)      NOT NULL,
            Nombre                 NVARCHAR(300)     NOT NULL,
            Enunciado              NVARCHAR(MAX)     NOT NULL,
            Justificacion          NVARCHAR(MAX)     NULL,
            IdAmbitoRegla          INT               NULL,
            IdEstadoReglaNegocio   INT               NOT NULL,
            MensajeError           NVARCHAR(500)     NULL,
            PermisoBypass          NVARCHAR(100)     NULL,
            UbicacionCodigo        NVARCHAR(400)     NULL,
            FechaVigenciaDesde     DATE              NULL,
            FechaVigenciaHasta     DATE              NULL,
            VersionActual          INT               NOT NULL CONSTRAINT DF_tblReglaNegocio_VersionActual DEFAULT (1),
            FechaRegistro          DATETIME2         NOT NULL CONSTRAINT DF_tblReglaNegocio_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro        NVARCHAR(200)     NOT NULL,
            UsuarioMovto           NVARCHAR(50)      NULL,
            FechaMovto             DATETIME          NULL,
            Activo                 BIT               NOT NULL CONSTRAINT DF_tblReglaNegocio_Activo DEFAULT (1),
            CONSTRAINT PK_tblReglaNegocio PRIMARY KEY (IdReglaNegocio),
            CONSTRAINT FK_tblReglaNegocio_Proyecto
                FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblReglaNegocio_AmbitoRegla
                FOREIGN KEY (IdAmbitoRegla) REFERENCES dbo.tblAmbitoRegla (IdAmbitoRegla),
            CONSTRAINT FK_tblReglaNegocio_Estado
                FOREIGN KEY (IdEstadoReglaNegocio) REFERENCES dbo.tblEstadoReglaNegocio (Id),
            CONSTRAINT UQ_tblReglaNegocio_ProyectoClave UNIQUE (IdProyecto, Clave),
            /* Clave alterna que hace posible la FK compuesta desde
               tblReglaNegocioImpacto (ver bloque 5). */
            CONSTRAINT UQ_tblReglaNegocio_IdProyecto UNIQUE (IdReglaNegocio, IdProyecto),
            CONSTRAINT CK_tblReglaNegocio_Vigencias
                CHECK (FechaVigenciaHasta IS NULL
                       OR FechaVigenciaDesde IS NULL
                       OR FechaVigenciaHasta >= FechaVigenciaDesde)
        );
        CREATE INDEX IX_tblReglaNegocio_Proyecto ON dbo.tblReglaNegocio (IdProyecto, Activo)
            INCLUDE (Clave, Nombre, IdEstadoReglaNegocio);
        CREATE INDEX IX_tblReglaNegocio_Ambito ON dbo.tblReglaNegocio (IdAmbitoRegla);
        PRINT 'OK: tblReglaNegocio creada'
    END
    ELSE
        PRINT 'SKIP: tblReglaNegocio ya existe'

    /* ---------------------------------------------------------------
       4. tblReglaNegocioVersion: historial del enunciado. Guarda
          TODAS las versiones incluida la vigente; VersionActual de la
          regla apunta a ella (patron ya probado en tblArticuloVersion).
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReglaNegocioVersion')
    BEGIN
        CREATE TABLE dbo.tblReglaNegocioVersion
        (
            IdReglaNegocioVersion INT IDENTITY(1,1) NOT NULL,
            IdReglaNegocio        INT               NOT NULL,
            NumeroVersion         INT               NOT NULL,
            Enunciado             NVARCHAR(MAX)     NOT NULL,
            Justificacion         NVARCHAR(MAX)     NULL,
            MotivoCambio          NVARCHAR(500)     NULL,
            FechaRegistro         DATETIME2         NOT NULL CONSTRAINT DF_tblReglaNegocioVersion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)     NOT NULL,
            CONSTRAINT PK_tblReglaNegocioVersion PRIMARY KEY (IdReglaNegocioVersion),
            CONSTRAINT FK_tblReglaNegocioVersion_Regla
                FOREIGN KEY (IdReglaNegocio) REFERENCES dbo.tblReglaNegocio (IdReglaNegocio),
            CONSTRAINT UQ_tblReglaNegocioVersion_ReglaNumero UNIQUE (IdReglaNegocio, NumeroVersion)
        );
        PRINT 'OK: tblReglaNegocioVersion creada'
    END
    ELSE
        PRINT 'SKIP: tblReglaNegocioVersion ya existe'

    /* ---------------------------------------------------------------
       5. tblReglaNegocioImpacto: proyectos secundarios afectados.
          Captura explicita, uno por uno.

          IdProyectoDueno esta desnormalizado A PROPOSITO: es lo que
          permite prohibir de forma declarativa que una regla se
          impacte a si misma (CHECK) sin recurrir a un trigger. La FK
          compuesta contra UQ_tblReglaNegocio_IdProyecto garantiza que
          la copia no pueda desalinearse del dueno real.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReglaNegocioImpacto')
    BEGIN
        CREATE TABLE dbo.tblReglaNegocioImpacto
        (
            IdReglaNegocioImpacto INT IDENTITY(1,1) NOT NULL,
            IdReglaNegocio        INT               NOT NULL,
            IdProyectoDueno       INT               NOT NULL,
            IdProyectoAfectado    INT               NOT NULL,
            DescripcionImpacto    NVARCHAR(1000)    NULL,
            IdAmbitoRegla         INT               NULL,
            FechaRegistro         DATETIME2         NOT NULL CONSTRAINT DF_tblReglaNegocioImpacto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)     NOT NULL,
            UsuarioMovto          NVARCHAR(50)      NULL,
            FechaMovto            DATETIME          NULL,
            Activo                BIT               NOT NULL CONSTRAINT DF_tblReglaNegocioImpacto_Activo DEFAULT (1),
            CONSTRAINT PK_tblReglaNegocioImpacto PRIMARY KEY (IdReglaNegocioImpacto),
            CONSTRAINT FK_tblReglaNegocioImpacto_Regla
                FOREIGN KEY (IdReglaNegocio, IdProyectoDueno)
                REFERENCES dbo.tblReglaNegocio (IdReglaNegocio, IdProyecto),
            CONSTRAINT FK_tblReglaNegocioImpacto_ProyectoAfectado
                FOREIGN KEY (IdProyectoAfectado) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblReglaNegocioImpacto_AmbitoRegla
                FOREIGN KEY (IdAmbitoRegla) REFERENCES dbo.tblAmbitoRegla (IdAmbitoRegla),
            CONSTRAINT UQ_tblReglaNegocioImpacto_ReglaProyecto
                UNIQUE (IdReglaNegocio, IdProyectoAfectado),
            CONSTRAINT CK_tblReglaNegocioImpacto_NoAutoImpacto
                CHECK (IdProyectoAfectado <> IdProyectoDueno)
        );
        CREATE INDEX IX_tblReglaNegocioImpacto_ProyectoAfectado
            ON dbo.tblReglaNegocioImpacto (IdProyectoAfectado, Activo)
            INCLUDE (IdReglaNegocio);
        PRINT 'OK: tblReglaNegocioImpacto creada'
    END
    ELSE
        PRINT 'SKIP: tblReglaNegocioImpacto ya existe'

    /* ---------------------------------------------------------------
       6. tblReglaNegocioRelacion: reglas encadenadas o en conflicto.
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReglaNegocioRelacion')
    BEGIN
        CREATE TABLE dbo.tblReglaNegocioRelacion
        (
            IdReglaNegocioRelacion    INT IDENTITY(1,1) NOT NULL,
            IdReglaNegocio            INT               NOT NULL,
            IdReglaNegocioRelacionada INT               NOT NULL,
            IdTipoRelacionRegla       INT               NOT NULL,
            Nota                      NVARCHAR(500)     NULL,
            FechaRegistro             DATETIME2         NOT NULL CONSTRAINT DF_tblReglaNegocioRelacion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro           NVARCHAR(200)     NOT NULL,
            UsuarioMovto              NVARCHAR(50)      NULL,
            FechaMovto                DATETIME          NULL,
            Activo                    BIT               NOT NULL CONSTRAINT DF_tblReglaNegocioRelacion_Activo DEFAULT (1),
            CONSTRAINT PK_tblReglaNegocioRelacion PRIMARY KEY (IdReglaNegocioRelacion),
            CONSTRAINT FK_tblReglaNegocioRelacion_Regla
                FOREIGN KEY (IdReglaNegocio) REFERENCES dbo.tblReglaNegocio (IdReglaNegocio),
            CONSTRAINT FK_tblReglaNegocioRelacion_Relacionada
                FOREIGN KEY (IdReglaNegocioRelacionada) REFERENCES dbo.tblReglaNegocio (IdReglaNegocio),
            CONSTRAINT FK_tblReglaNegocioRelacion_Tipo
                FOREIGN KEY (IdTipoRelacionRegla) REFERENCES dbo.tblTipoRelacionRegla (Id),
            CONSTRAINT UQ_tblReglaNegocioRelacion_Par
                UNIQUE (IdReglaNegocio, IdReglaNegocioRelacionada, IdTipoRelacionRegla),
            CONSTRAINT CK_tblReglaNegocioRelacion_NoAutoRelacion
                CHECK (IdReglaNegocio <> IdReglaNegocioRelacionada)
        );
        CREATE INDEX IX_tblReglaNegocioRelacion_Regla ON dbo.tblReglaNegocioRelacion (IdReglaNegocio, Activo);
        PRINT 'OK: tblReglaNegocioRelacion creada'
    END
    ELSE
        PRINT 'SKIP: tblReglaNegocioRelacion ya existe'

    /* ---------------------------------------------------------------
       7. Permisos.
       --------------------------------------------------------------- */
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'RGN.Ver', N'ReglasNegocio',
         N'Consultar el catalogo de reglas de negocio de los proyectos con alcance'),
        (N'RGN.Administrar', N'ReglasNegocio',
         N'Crear, editar, derogar reglas de negocio y administrar flujos, caracteristicas e impactos')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos RGN sembrados (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'RGN.Ver', N'RGN.Administrar')
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos RGN asignados al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
