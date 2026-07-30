USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      04_2026-07-30_SCRIPT_bdsGTE_Nucleo.sql
   Autor:       Equipo GTE
   Descripcion: Planeacion (sprints, capacidad, tableros) y nucleo
                transaccional: solicitudes, WorkItems (entidad unificada),
                registro de tiempo, revisiones, vinculos, comentarios,
                archivos e historiales (estatus y campos).
   Requiere:    01, 02, 03
   Notas:       - tblWorkItem.IdRelease e IdEjecucionPruebaOrigen quedan
                  sin FK aqui; se agregan en el script 05.
                - tblHistorialEstatus usa el NOMBRE del proceso (contrato
                  estable con dbo.tblProceso.Proceso del script 07).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSprint')
    BEGIN
        CREATE TABLE dbo.tblSprint
        (
            IdSprint        INT           IDENTITY(1,1) NOT NULL,
            IdEquipo        INT                         NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            Objetivo        NVARCHAR(500)               NULL,
            FechaInicio     DATE                        NOT NULL,
            FechaFin        DATE                        NOT NULL,
            IdEstatusSprint INT                         NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblSprint_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblSprint_Activo DEFAULT (1),
            CONSTRAINT PK_tblSprint PRIMARY KEY (IdSprint),
            CONSTRAINT FK_tblSprint_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo),
            CONSTRAINT FK_tblSprint_tblEstatusSprint FOREIGN KEY (IdEstatusSprint) REFERENCES dbo.tblEstatusSprint (Id),
            CONSTRAINT CK_tblSprint_Fechas CHECK (FechaFin > FechaInicio)
        )
        PRINT 'OK: tblSprint creada'
    END
    ELSE PRINT 'SKIP: tblSprint ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCapacidadSprint')
    BEGIN
        CREATE TABLE dbo.tblCapacidadSprint
        (
            IdCapacidadSprint    INT          IDENTITY(1,1) NOT NULL,
            IdSprint             INT                        NOT NULL,
            IdUsuario            INT                        NOT NULL,
            HorasPorDia          DECIMAL(4,2)               NOT NULL,
            PorcentajeDedicacion DECIMAL(5,2)               NOT NULL CONSTRAINT DF_tblCapacidadSprint_PorcentajeDedicacion DEFAULT (100),
            FechaRegistro        DATETIME2                  NOT NULL CONSTRAINT DF_tblCapacidadSprint_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)              NOT NULL,
            CONSTRAINT PK_tblCapacidadSprint PRIMARY KEY (IdCapacidadSprint),
            CONSTRAINT UQ_tblCapacidadSprint_SprintUsuario UNIQUE (IdSprint, IdUsuario),
            CONSTRAINT FK_tblCapacidadSprint_tblSprint FOREIGN KEY (IdSprint) REFERENCES dbo.tblSprint (IdSprint),
            CONSTRAINT FK_tblCapacidadSprint_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT CK_tblCapacidadSprint_Horas CHECK (HorasPorDia > 0 AND HorasPorDia <= 24)
        )
        PRINT 'OK: tblCapacidadSprint creada'
    END
    ELSE PRINT 'SKIP: tblCapacidadSprint ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTablero')
    BEGIN
        CREATE TABLE dbo.tblTablero
        (
            IdTablero       INT           IDENTITY(1,1) NOT NULL,
            IdEquipo        INT                         NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblTablero_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblTablero_Activo DEFAULT (1),
            CONSTRAINT PK_tblTablero PRIMARY KEY (IdTablero),
            CONSTRAINT FK_tblTablero_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo)
        )
        PRINT 'OK: tblTablero creada'
    END
    ELSE PRINT 'SKIP: tblTablero ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTableroColumna')
    BEGIN
        CREATE TABLE dbo.tblTableroColumna
        (
            IdTableroColumna  INT           IDENTITY(1,1) NOT NULL,
            IdTablero         INT                         NOT NULL,
            Nombre            NVARCHAR(100)               NOT NULL,
            IdEstatusWorkItem INT                         NOT NULL,
            Orden             INT                         NOT NULL,
            LimiteWip         INT                         NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblTableroColumna_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblTableroColumna_Activo DEFAULT (1),
            CONSTRAINT PK_tblTableroColumna PRIMARY KEY (IdTableroColumna),
            CONSTRAINT UQ_tblTableroColumna_TableroEstatus UNIQUE (IdTablero, IdEstatusWorkItem),
            CONSTRAINT FK_tblTableroColumna_tblTablero FOREIGN KEY (IdTablero) REFERENCES dbo.tblTablero (IdTablero),
            CONSTRAINT FK_tblTableroColumna_tblEstatusWorkItem FOREIGN KEY (IdEstatusWorkItem) REFERENCES dbo.tblEstatusWorkItem (Id),
            CONSTRAINT CK_tblTableroColumna_LimiteWip CHECK (LimiteWip IS NULL OR LimiteWip > 0)
        )
        PRINT 'OK: tblTableroColumna creada'
    END
    ELSE PRINT 'SKIP: tblTableroColumna ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSolicitud')
    BEGIN
        CREATE TABLE dbo.tblSolicitud
        (
            IdSolicitud          INT           IDENTITY(1,1) NOT NULL,
            Folio                NVARCHAR(50)                NULL,   -- se asigna al ENVIAR (dbo.spGenerarFolio)
            IdSolicitante        INT                         NOT NULL,
            IdProyecto           INT                         NULL,
            Titulo               NVARCHAR(200)               NOT NULL,
            Descripcion          NVARCHAR(MAX)               NULL,
            IdTipoSolicitud      INT                         NOT NULL,
            IdPrioridad          INT                         NOT NULL,
            IdEstatusSolicitud   INT                         NOT NULL,
            FechaDeseada         DATE                        NULL,
            JustificacionNegocio NVARCHAR(500)               NULL,
            FechaRegistro        DATETIME2                   NOT NULL CONSTRAINT DF_tblSolicitud_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)               NOT NULL,
            UsuarioMovto         NVARCHAR(50)                NULL,
            FechaMovto           DATETIME                    NULL,
            Activo               BIT                         NOT NULL CONSTRAINT DF_tblSolicitud_Activo DEFAULT (1),
            CONSTRAINT PK_tblSolicitud PRIMARY KEY (IdSolicitud),
            CONSTRAINT FK_tblSolicitud_tblUsuario FOREIGN KEY (IdSolicitante) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblSolicitud_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblSolicitud_tblTipoSolicitud FOREIGN KEY (IdTipoSolicitud) REFERENCES dbo.tblTipoSolicitud (Id),
            CONSTRAINT FK_tblSolicitud_tblPrioridad FOREIGN KEY (IdPrioridad) REFERENCES dbo.tblPrioridad (Id),
            CONSTRAINT FK_tblSolicitud_tblEstatusSolicitud FOREIGN KEY (IdEstatusSolicitud) REFERENCES dbo.tblEstatusSolicitud (Id)
        )
        CREATE UNIQUE INDEX UQ_tblSolicitud_Folio ON dbo.tblSolicitud (Folio) WHERE Folio IS NOT NULL
        CREATE INDEX IX_tblSolicitud_Triage ON dbo.tblSolicitud (IdEstatusSolicitud, Activo)
        PRINT 'OK: tblSolicitud creada'
    END
    ELSE PRINT 'SKIP: tblSolicitud ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem')
    BEGIN
        CREATE TABLE dbo.tblWorkItem
        (
            IdWorkItem              INT           IDENTITY(1,1) NOT NULL,
            Folio                   NVARCHAR(50)                NOT NULL,
            IdTipoWorkItem          INT                         NOT NULL,
            IdPadre                 INT                         NULL,
            IdProyecto              INT                         NOT NULL,
            IdSolicitud             INT                         NULL,
            Titulo                  NVARCHAR(200)               NOT NULL,
            Descripcion             NVARCHAR(MAX)               NULL,   -- HTML sanitizado
            CriteriosAceptacion     NVARCHAR(MAX)               NULL,
            IdEstatusWorkItem       INT                         NOT NULL,
            IdPrioridad             INT                         NOT NULL,
            IdComplejidad           INT                         NULL,
            IdAsignado              INT                         NULL,
            IdSolicitante           INT                         NULL,
            IdSprint                INT                         NULL,
            IdRelease               INT                         NULL,   -- FK fisica en script 05
            PuntosHistoria          DECIMAL(6,2)                NULL,
            MinutosPresupuesto      INT                         NULL,   -- congelado al asignar (matriz complejidad x nivel)
            FechaCompromiso         DATETIME2                   NULL,
            FechaInicio             DATETIME2                   NULL,
            FechaFin                DATETIME2                   NULL,
            OrdenBacklog            INT                         NULL,
            Revisado                BIT                         NOT NULL CONSTRAINT DF_tblWorkItem_Revisado DEFAULT (0),
            IdEjecucionPruebaOrigen INT                         NULL,   -- FK fisica en script 05
            ClaveJira               NVARCHAR(50)                NULL,   -- idempotencia de migracion
            FechaRegistro           DATETIME2                   NOT NULL CONSTRAINT DF_tblWorkItem_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro         NVARCHAR(200)               NOT NULL,
            UsuarioMovto            NVARCHAR(50)                NULL,
            FechaMovto              DATETIME                    NULL,
            Activo                  BIT                         NOT NULL CONSTRAINT DF_tblWorkItem_Activo DEFAULT (1),
            CONSTRAINT PK_tblWorkItem PRIMARY KEY (IdWorkItem),
            CONSTRAINT UQ_tblWorkItem_Folio UNIQUE (Folio),
            CONSTRAINT FK_tblWorkItem_tblTipoWorkItem FOREIGN KEY (IdTipoWorkItem) REFERENCES dbo.tblTipoWorkItem (Id),
            CONSTRAINT FK_tblWorkItem_tblWorkItem FOREIGN KEY (IdPadre) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblWorkItem_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblWorkItem_tblSolicitud FOREIGN KEY (IdSolicitud) REFERENCES dbo.tblSolicitud (IdSolicitud),
            CONSTRAINT FK_tblWorkItem_tblEstatusWorkItem FOREIGN KEY (IdEstatusWorkItem) REFERENCES dbo.tblEstatusWorkItem (Id),
            CONSTRAINT FK_tblWorkItem_tblPrioridad FOREIGN KEY (IdPrioridad) REFERENCES dbo.tblPrioridad (Id),
            CONSTRAINT FK_tblWorkItem_tblComplejidad FOREIGN KEY (IdComplejidad) REFERENCES dbo.tblComplejidad (IdComplejidad),
            CONSTRAINT FK_tblWorkItem_tblUsuarioAsignado FOREIGN KEY (IdAsignado) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblWorkItem_tblUsuarioSolicitante FOREIGN KEY (IdSolicitante) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblWorkItem_tblSprint FOREIGN KEY (IdSprint) REFERENCES dbo.tblSprint (IdSprint),
            CONSTRAINT CK_tblWorkItem_Puntos CHECK (PuntosHistoria IS NULL OR PuntosHistoria >= 0),
            CONSTRAINT CK_tblWorkItem_MinutosPresupuesto CHECK (MinutosPresupuesto IS NULL OR MinutosPresupuesto > 0)
        )
        CREATE UNIQUE INDEX UQ_tblWorkItem_ClaveJira ON dbo.tblWorkItem (ClaveJira) WHERE ClaveJira IS NOT NULL
        CREATE INDEX IX_tblWorkItem_Bandeja ON dbo.tblWorkItem (IdAsignado, IdEstatusWorkItem, Activo)
            INCLUDE (IdProyecto, FechaCompromiso, IdPrioridad, Titulo)
        CREATE INDEX IX_tblWorkItem_Proyecto ON dbo.tblWorkItem (IdProyecto, IdEstatusWorkItem)
        CREATE INDEX IX_tblWorkItem_Sprint ON dbo.tblWorkItem (IdSprint) WHERE IdSprint IS NOT NULL
        CREATE INDEX IX_tblWorkItem_Padre ON dbo.tblWorkItem (IdPadre) WHERE IdPadre IS NOT NULL
        PRINT 'OK: tblWorkItem creada'
    END
    ELSE PRINT 'SKIP: tblWorkItem ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRegistroTiempo')
    BEGIN
        CREATE TABLE dbo.tblRegistroTiempo
        (
            IdRegistroTiempo INT           IDENTITY(1,1) NOT NULL,
            IdWorkItem       INT                         NOT NULL,
            IdUsuario        INT                         NOT NULL,
            Fecha            DATE                        NOT NULL,
            Minutos          INT                         NOT NULL,
            Descripcion      NVARCHAR(500)               NULL,
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblRegistroTiempo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            UsuarioMovto     NVARCHAR(50)                NULL,
            FechaMovto       DATETIME                    NULL,
            Activo           BIT                         NOT NULL CONSTRAINT DF_tblRegistroTiempo_Activo DEFAULT (1),
            CONSTRAINT PK_tblRegistroTiempo PRIMARY KEY (IdRegistroTiempo),
            CONSTRAINT FK_tblRegistroTiempo_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblRegistroTiempo_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT CK_tblRegistroTiempo_Minutos CHECK (Minutos BETWEEN 1 AND 1440)
        )
        CREATE INDEX IX_tblRegistroTiempo_UsuarioFecha ON dbo.tblRegistroTiempo (IdUsuario, Fecha)
        CREATE INDEX IX_tblRegistroTiempo_WorkItem ON dbo.tblRegistroTiempo (IdWorkItem)
        PRINT 'OK: tblRegistroTiempo creada'
    END
    ELSE PRINT 'SKIP: tblRegistroTiempo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRevision')
    BEGIN
        CREATE TABLE dbo.tblRevision
        (
            IdRevision        INT           IDENTITY(1,1) NOT NULL,
            IdWorkItem        INT                         NOT NULL,
            IdRevisor         INT                         NOT NULL,
            Comentarios       NVARCHAR(MAX)               NULL,
            IdEstatusRevision INT                         NOT NULL,
            Corregido         BIT                         NOT NULL CONSTRAINT DF_tblRevision_Corregido DEFAULT (0),
            FechaCorreccion   DATETIME2                   NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblRevision_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblRevision_Activo DEFAULT (1),
            CONSTRAINT PK_tblRevision PRIMARY KEY (IdRevision),
            CONSTRAINT FK_tblRevision_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblRevision_tblUsuario FOREIGN KEY (IdRevisor) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblRevision_tblEstatusRevision FOREIGN KEY (IdEstatusRevision) REFERENCES dbo.tblEstatusRevision (Id)
        )
        CREATE INDEX IX_tblRevision_Pendientes ON dbo.tblRevision (IdWorkItem) WHERE Corregido = 0
        PRINT 'OK: tblRevision creada'
    END
    ELSE PRINT 'SKIP: tblRevision ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItemVinculo')
    BEGIN
        CREATE TABLE dbo.tblWorkItemVinculo
        (
            IdWorkItemVinculo  INT           IDENTITY(1,1) NOT NULL,
            IdWorkItemOrigen   INT                         NOT NULL,
            IdWorkItemDestino  INT                         NOT NULL,
            IdTipoVinculo      INT                         NOT NULL,
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblWorkItemVinculo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblWorkItemVinculo PRIMARY KEY (IdWorkItemVinculo),
            CONSTRAINT UQ_tblWorkItemVinculo_OrigenDestinoTipo UNIQUE (IdWorkItemOrigen, IdWorkItemDestino, IdTipoVinculo),
            CONSTRAINT FK_tblWorkItemVinculo_tblWorkItemOrigen FOREIGN KEY (IdWorkItemOrigen) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblWorkItemVinculo_tblWorkItemDestino FOREIGN KEY (IdWorkItemDestino) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblWorkItemVinculo_tblTipoVinculo FOREIGN KEY (IdTipoVinculo) REFERENCES dbo.tblTipoVinculo (Id),
            CONSTRAINT CK_tblWorkItemVinculo_NoAutoVinculo CHECK (IdWorkItemOrigen <> IdWorkItemDestino)
        )
        PRINT 'OK: tblWorkItemVinculo creada'
    END
    ELSE PRINT 'SKIP: tblWorkItemVinculo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblComentario')
    BEGIN
        CREATE TABLE dbo.tblComentario
        (
            IdComentario       INT           IDENTITY(1,1) NOT NULL,
            Entidad            NVARCHAR(100)               NOT NULL,
            IdEntidad          INT                         NOT NULL,
            Contenido          NVARCHAR(MAX)               NOT NULL,   -- HTML sanitizado
            IdComentarioPadre  INT                         NULL,
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblComentario_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            UsuarioMovto       NVARCHAR(50)                NULL,
            FechaMovto         DATETIME                    NULL,
            Activo             BIT                         NOT NULL CONSTRAINT DF_tblComentario_Activo DEFAULT (1),
            CONSTRAINT PK_tblComentario PRIMARY KEY (IdComentario),
            CONSTRAINT FK_tblComentario_tblComentario FOREIGN KEY (IdComentarioPadre) REFERENCES dbo.tblComentario (IdComentario)
        )
        CREATE INDEX IX_tblComentario_Entidad ON dbo.tblComentario (Entidad, IdEntidad)
        PRINT 'OK: tblComentario creada'
    END
    ELSE PRINT 'SKIP: tblComentario ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArchivo')
    BEGIN
        CREATE TABLE dbo.tblArchivo
        (
            IdArchivo       INT              IDENTITY(1,1) NOT NULL,
            GuidArchivo     UNIQUEIDENTIFIER              NOT NULL CONSTRAINT DF_tblArchivo_GuidArchivo DEFAULT (NEWID()),
            NombreArchivo   NVARCHAR(200)                 NOT NULL,
            Extension       NVARCHAR(20)                  NULL,
            TamanoBytes     BIGINT                        NOT NULL CONSTRAINT DF_tblArchivo_TamanoBytes DEFAULT (0),
            RutaRelativa    NVARCHAR(500)                 NOT NULL,
            HashSha256      NVARCHAR(100)                 NULL,
            FechaRegistro   DATETIME2                     NOT NULL CONSTRAINT DF_tblArchivo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)                 NOT NULL,
            UsuarioMovto    NVARCHAR(50)                  NULL,
            FechaMovto      DATETIME                      NULL,
            Activo          BIT                           NOT NULL CONSTRAINT DF_tblArchivo_Activo DEFAULT (1),
            CONSTRAINT PK_tblArchivo PRIMARY KEY (IdArchivo),
            CONSTRAINT UQ_tblArchivo_GuidArchivo UNIQUE (GuidArchivo)
        )
        PRINT 'OK: tblArchivo creada'
    END
    ELSE PRINT 'SKIP: tblArchivo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArchivoVinculo')
    BEGIN
        CREATE TABLE dbo.tblArchivoVinculo
        (
            IdArchivoVinculo INT           IDENTITY(1,1) NOT NULL,
            IdArchivo        INT                         NOT NULL,
            Entidad          NVARCHAR(100)               NOT NULL,
            IdEntidad        INT                         NOT NULL,
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblArchivoVinculo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            Activo           BIT                         NOT NULL CONSTRAINT DF_tblArchivoVinculo_Activo DEFAULT (1),
            CONSTRAINT PK_tblArchivoVinculo PRIMARY KEY (IdArchivoVinculo),
            CONSTRAINT FK_tblArchivoVinculo_tblArchivo FOREIGN KEY (IdArchivo) REFERENCES dbo.tblArchivo (IdArchivo)
        )
        CREATE INDEX IX_tblArchivoVinculo_Entidad ON dbo.tblArchivoVinculo (Entidad, IdEntidad)
        PRINT 'OK: tblArchivoVinculo creada'
    END
    ELSE PRINT 'SKIP: tblArchivoVinculo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblHistorialEstatus')
    BEGIN
        CREATE TABLE dbo.tblHistorialEstatus
        (
            IdHistorialEstatus BIGINT        IDENTITY(1,1) NOT NULL,
            Proceso            NVARCHAR(50)                NOT NULL,   -- nombre en dbo.tblProceso (script 07)
            IdRegistro         INT                         NOT NULL,
            IdEstatus          INT                         NOT NULL,
            Accion             NVARCHAR(50)                NULL,
            FechaInicio        DATETIME2                   NOT NULL CONSTRAINT DF_tblHistorialEstatus_FechaInicio DEFAULT (SYSDATETIME()),
            FechaFin           DATETIME2                   NULL,       -- NULL = estatus vigente
            MinutosLaborales   INT                         NULL,       -- materializado al cerrar el intervalo
            Usuario            NVARCHAR(200)               NOT NULL,
            Motivo             NVARCHAR(500)               NULL,
            CONSTRAINT PK_tblHistorialEstatus PRIMARY KEY (IdHistorialEstatus)
        )
        CREATE INDEX IX_tblHistorialEstatus_Registro ON dbo.tblHistorialEstatus (Proceso, IdRegistro, FechaInicio)
        CREATE INDEX IX_tblHistorialEstatus_Abiertos ON dbo.tblHistorialEstatus (Proceso, IdEstatus) WHERE FechaFin IS NULL
        PRINT 'OK: tblHistorialEstatus creada (tabla de hechos temporal)'
    END
    ELSE PRINT 'SKIP: tblHistorialEstatus ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblHistorialCampo')
    BEGIN
        CREATE TABLE dbo.tblHistorialCampo
        (
            IdHistorialCampo BIGINT        IDENTITY(1,1) NOT NULL,
            Entidad          NVARCHAR(100)               NOT NULL,
            IdEntidad        INT                         NOT NULL,
            Campo            NVARCHAR(100)               NOT NULL,
            ValorAnterior    NVARCHAR(MAX)               NULL,
            ValorNuevo       NVARCHAR(MAX)               NULL,
            Usuario          NVARCHAR(200)               NOT NULL,
            Fecha            DATETIME2                   NOT NULL CONSTRAINT DF_tblHistorialCampo_Fecha DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblHistorialCampo PRIMARY KEY (IdHistorialCampo)
        )
        CREATE INDEX IX_tblHistorialCampo_Entidad ON dbo.tblHistorialCampo (Entidad, IdEntidad, Fecha)
        PRINT 'OK: tblHistorialCampo creada'
    END
    ELSE PRINT 'SKIP: tblHistorialCampo ya existe'

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
