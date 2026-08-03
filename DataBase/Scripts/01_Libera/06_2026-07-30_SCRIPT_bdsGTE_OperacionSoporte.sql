USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      06_2026-07-30_SCRIPT_bdsGTE_OperacionSoporte.sql
   Autor:       Equipo GTE
   Descripcion: Operacion (incidentes, bitacora de cambios en ambientes)
                y Soporte (SLA, tickets, encuestas, base de conocimiento
                con versionado de articulos).
   Requiere:    01-05
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblIncidente')
    BEGIN
        CREATE TABLE dbo.tblIncidente
        (
            IdIncidente             INT           IDENTITY(1,1) NOT NULL,
            Folio                   NVARCHAR(50)                NULL,
            IdProyecto              INT                         NOT NULL,
            IdSeveridad             INT                         NOT NULL,
            IdEstatusIncidente      INT                         NOT NULL,
            Titulo                  NVARCHAR(200)               NOT NULL,
            Descripcion             NVARCHAR(MAX)               NULL,
            FechaOcurrencia         DATETIME2                   NOT NULL,
            FechaDeteccion          DATETIME2                   NULL,
            FechaResolucion         DATETIME2                   NULL,
            MinutosIndisponibilidad INT                         NULL,
            CausaRaiz               NVARCHAR(MAX)               NULL,   -- obligatoria en S1/S2 (valida el backend)
            IdWorkItemCorrectivo    INT                         NULL,
            IdReleaseCausante       INT                         NULL,
            FechaRegistro           DATETIME2                   NOT NULL CONSTRAINT DF_tblIncidente_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro         NVARCHAR(200)               NOT NULL,
            UsuarioMovto            NVARCHAR(50)                NULL,
            FechaMovto              DATETIME                    NULL,
            Activo                  BIT                         NOT NULL CONSTRAINT DF_tblIncidente_Activo DEFAULT (1),
            CONSTRAINT PK_tblIncidente PRIMARY KEY (IdIncidente),
            CONSTRAINT FK_tblIncidente_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblIncidente_tblSeveridad FOREIGN KEY (IdSeveridad) REFERENCES dbo.tblSeveridad (Id),
            CONSTRAINT FK_tblIncidente_tblEstatusIncidente FOREIGN KEY (IdEstatusIncidente) REFERENCES dbo.tblEstatusIncidente (Id),
            CONSTRAINT FK_tblIncidente_tblWorkItem FOREIGN KEY (IdWorkItemCorrectivo) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblIncidente_tblRelease FOREIGN KEY (IdReleaseCausante) REFERENCES dbo.tblRelease (IdRelease),
            CONSTRAINT CK_tblIncidente_MinutosIndisponibilidad CHECK (MinutosIndisponibilidad IS NULL OR MinutosIndisponibilidad >= 0)
        )
        CREATE UNIQUE INDEX UQ_tblIncidente_Folio ON dbo.tblIncidente (Folio) WHERE Folio IS NOT NULL
        CREATE INDEX IX_tblIncidente_Abiertos ON dbo.tblIncidente (IdEstatusIncidente, IdSeveridad)
        PRINT 'OK: tblIncidente creada'
    END
    ELSE PRINT 'SKIP: tblIncidente ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblBitacoraCambio')
    BEGIN
        CREATE TABLE dbo.tblBitacoraCambio
        (
            IdBitacoraCambio INT           IDENTITY(1,1) NOT NULL,
            IdAmbiente       INT                         NOT NULL,
            Descripcion      NVARCHAR(500)               NOT NULL,
            IdRelease        INT                         NULL,
            Usuario          NVARCHAR(200)               NOT NULL,
            Fecha            DATETIME2                   NOT NULL CONSTRAINT DF_tblBitacoraCambio_Fecha DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblBitacoraCambio PRIMARY KEY (IdBitacoraCambio),
            CONSTRAINT FK_tblBitacoraCambio_tblAmbiente FOREIGN KEY (IdAmbiente) REFERENCES dbo.tblAmbiente (IdAmbiente),
            CONSTRAINT FK_tblBitacoraCambio_tblRelease FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease)
        )
        CREATE INDEX IX_tblBitacoraCambio_AmbienteFecha ON dbo.tblBitacoraCambio (IdAmbiente, Fecha)
        PRINT 'OK: tblBitacoraCambio creada'
    END
    ELSE PRINT 'SKIP: tblBitacoraCambio ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSla')
    BEGIN
        CREATE TABLE dbo.tblSla
        (
            IdSla              INT           IDENTITY(1,1) NOT NULL,
            Nombre             NVARCHAR(100)               NOT NULL,
            IdPrioridad        INT                         NOT NULL,
            MinutosRespuesta   INT                         NOT NULL,   -- en minutos laborales
            MinutosResolucion  INT                         NOT NULL,   -- en minutos laborales
            IdHorario          INT                         NOT NULL,   -- calendario que rige el reloj
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblSla_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            UsuarioMovto       NVARCHAR(50)                NULL,
            FechaMovto         DATETIME                    NULL,
            Activo             BIT                         NOT NULL CONSTRAINT DF_tblSla_Activo DEFAULT (1),
            CONSTRAINT PK_tblSla PRIMARY KEY (IdSla),
            CONSTRAINT UQ_tblSla_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_tblSla_tblPrioridad FOREIGN KEY (IdPrioridad) REFERENCES dbo.tblPrioridad (Id),
            CONSTRAINT FK_tblSla_tblHorario FOREIGN KEY (IdHorario) REFERENCES dbo.tblHorario (IdHorario),
            CONSTRAINT CK_tblSla_Minutos CHECK (MinutosRespuesta > 0 AND MinutosResolucion >= MinutosRespuesta)
        )
        PRINT 'OK: tblSla creada'
    END
    ELSE PRINT 'SKIP: tblSla ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTicket')
    BEGIN
        CREATE TABLE dbo.tblTicket
        (
            IdTicket              INT           IDENTITY(1,1) NOT NULL,
            Folio                 NVARCHAR(50)                NULL,
            IdSolicitante         INT                         NOT NULL,
            IdCategoriaTicket     INT                         NULL,
            IdPrioridad           INT                         NOT NULL,
            IdEstatusTicket       INT                         NOT NULL,
            IdAsignado            INT                         NULL,
            IdSla                 INT                         NULL,
            Titulo                NVARCHAR(200)               NOT NULL,
            Descripcion           NVARCHAR(MAX)               NULL,
            FechaLimiteRespuesta  DATETIME2                   NULL,   -- calculada por el backend (minutos laborales del SLA)
            FechaLimiteResolucion DATETIME2                   NULL,
            FechaPrimeraRespuesta DATETIME2                   NULL,
            FechaResolucion       DATETIME2                   NULL,
            IdWorkItemDerivado    INT                         NULL,
            FechaRegistro         DATETIME2                   NOT NULL CONSTRAINT DF_tblTicket_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)               NOT NULL,
            UsuarioMovto          NVARCHAR(50)                NULL,
            FechaMovto            DATETIME                    NULL,
            Activo                BIT                         NOT NULL CONSTRAINT DF_tblTicket_Activo DEFAULT (1),
            CONSTRAINT PK_tblTicket PRIMARY KEY (IdTicket),
            CONSTRAINT FK_tblTicket_tblUsuarioSolicitante FOREIGN KEY (IdSolicitante) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblTicket_tblCategoriaTicket FOREIGN KEY (IdCategoriaTicket) REFERENCES dbo.tblCategoriaTicket (IdCategoriaTicket),
            CONSTRAINT FK_tblTicket_tblPrioridad FOREIGN KEY (IdPrioridad) REFERENCES dbo.tblPrioridad (Id),
            CONSTRAINT FK_tblTicket_tblEstatusTicket FOREIGN KEY (IdEstatusTicket) REFERENCES dbo.tblEstatusTicket (Id),
            CONSTRAINT FK_tblTicket_tblUsuarioAsignado FOREIGN KEY (IdAsignado) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblTicket_tblSla FOREIGN KEY (IdSla) REFERENCES dbo.tblSla (IdSla),
            CONSTRAINT FK_tblTicket_tblWorkItem FOREIGN KEY (IdWorkItemDerivado) REFERENCES dbo.tblWorkItem (IdWorkItem)
        )
        CREATE UNIQUE INDEX UQ_tblTicket_Folio ON dbo.tblTicket (Folio) WHERE Folio IS NOT NULL
        CREATE INDEX IX_tblTicket_SlaVigilancia ON dbo.tblTicket (IdEstatusTicket, FechaLimiteResolucion) WHERE FechaResolucion IS NULL
        CREATE INDEX IX_tblTicket_Asignado ON dbo.tblTicket (IdAsignado, IdEstatusTicket)
        PRINT 'OK: tblTicket creada'
    END
    ELSE PRINT 'SKIP: tblTicket ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEncuestaSatisfaccion')
    BEGIN
        CREATE TABLE dbo.tblEncuestaSatisfaccion
        (
            IdEncuestaSatisfaccion INT           IDENTITY(1,1) NOT NULL,
            IdTicket               INT                         NOT NULL,
            Calificacion           TINYINT                     NOT NULL,
            Comentario             NVARCHAR(500)               NULL,
            FechaRegistro          DATETIME2                   NOT NULL CONSTRAINT DF_tblEncuestaSatisfaccion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro        NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblEncuestaSatisfaccion PRIMARY KEY (IdEncuestaSatisfaccion),
            CONSTRAINT UQ_tblEncuestaSatisfaccion_Ticket UNIQUE (IdTicket),
            CONSTRAINT FK_tblEncuestaSatisfaccion_tblTicket FOREIGN KEY (IdTicket) REFERENCES dbo.tblTicket (IdTicket),
            CONSTRAINT CK_tblEncuestaSatisfaccion_Calificacion CHECK (Calificacion BETWEEN 1 AND 5)
        )
        PRINT 'OK: tblEncuestaSatisfaccion creada'
    END
    ELSE PRINT 'SKIP: tblEncuestaSatisfaccion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArticuloConocimiento')
    BEGIN
        CREATE TABLE dbo.tblArticuloConocimiento
        (
            IdArticuloConocimiento INT           IDENTITY(1,1) NOT NULL,
            Titulo                 NVARCHAR(200)               NOT NULL,
            Contenido              NVARCHAR(MAX)               NOT NULL,   -- HTML sanitizado
            VersionActual          INT                         NOT NULL CONSTRAINT DF_tblArticuloConocimiento_VersionActual DEFAULT (1),
            EsGlosario             BIT                         NOT NULL CONSTRAINT DF_tblArticuloConocimiento_EsGlosario DEFAULT (0),   -- termino migrado del Glosario GT
            FechaRegistro          DATETIME2                   NOT NULL CONSTRAINT DF_tblArticuloConocimiento_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro        NVARCHAR(200)               NOT NULL,
            UsuarioMovto           NVARCHAR(50)                NULL,
            FechaMovto             DATETIME                    NULL,
            Activo                 BIT                         NOT NULL CONSTRAINT DF_tblArticuloConocimiento_Activo DEFAULT (1),
            CONSTRAINT PK_tblArticuloConocimiento PRIMARY KEY (IdArticuloConocimiento),
            CONSTRAINT UQ_tblArticuloConocimiento_Titulo UNIQUE (Titulo)
        )
        PRINT 'OK: tblArticuloConocimiento creada'
    END
    ELSE PRINT 'SKIP: tblArticuloConocimiento ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArticuloVersion')
    BEGIN
        CREATE TABLE dbo.tblArticuloVersion
        (
            IdArticuloVersion      INT           IDENTITY(1,1) NOT NULL,
            IdArticuloConocimiento INT                         NOT NULL,
            Version                INT                         NOT NULL,
            Contenido              NVARCHAR(MAX)               NOT NULL,
            FechaRegistro          DATETIME2                   NOT NULL CONSTRAINT DF_tblArticuloVersion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro        NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblArticuloVersion PRIMARY KEY (IdArticuloVersion),
            CONSTRAINT UQ_tblArticuloVersion_ArticuloVersion UNIQUE (IdArticuloConocimiento, Version),
            CONSTRAINT FK_tblArticuloVersion_tblArticuloConocimiento FOREIGN KEY (IdArticuloConocimiento) REFERENCES dbo.tblArticuloConocimiento (IdArticuloConocimiento)
        )
        PRINT 'OK: tblArticuloVersion creada'
    END
    ELSE PRINT 'SKIP: tblArticuloVersion ya existe'

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
