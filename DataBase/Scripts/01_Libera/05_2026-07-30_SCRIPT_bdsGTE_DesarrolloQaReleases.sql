USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      05_2026-07-30_SCRIPT_bdsGTE_DesarrolloQaReleases.sql
   Autor:       Equipo GTE
   Descripcion: Desarrollo (commits, PRs, pipelines, artefactos),
                QA (planes, casos, pasos, ciclos, ejecuciones) y
                Releases (versiones, contenido, despliegues, aprobaciones).
                Agrega las FKs pendientes de tblWorkItem (IdRelease,
                IdEjecucionPruebaOrigen).
   Requiere:    01-04
   ===================================================================== */
BEGIN TRY

    /* ---------- Desarrollo ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCommit')
    BEGIN
        CREATE TABLE dbo.tblCommit
        (
            IdCommit        INT           IDENTITY(1,1) NOT NULL,
            IdRepositorio   INT                         NOT NULL,
            Sha             NVARCHAR(64)                NOT NULL,
            Autor           NVARCHAR(200)               NOT NULL,
            FechaCommit     DATETIME2                   NOT NULL,
            Mensaje         NVARCHAR(MAX)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblCommit_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblCommit PRIMARY KEY (IdCommit),
            CONSTRAINT UQ_tblCommit_RepositorioSha UNIQUE (IdRepositorio, Sha),
            CONSTRAINT FK_tblCommit_tblRepositorio FOREIGN KEY (IdRepositorio) REFERENCES dbo.tblRepositorio (IdRepositorio)
        )
        PRINT 'OK: tblCommit creada'
    END
    ELSE PRINT 'SKIP: tblCommit ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCommitWorkItem')
    BEGIN
        CREATE TABLE dbo.tblCommitWorkItem
        (
            IdCommitWorkItem INT          IDENTITY(1,1) NOT NULL,
            IdCommit         INT                        NOT NULL,
            IdWorkItem       INT                        NOT NULL,
            FechaRegistro    DATETIME2                  NOT NULL CONSTRAINT DF_tblCommitWorkItem_FechaRegistro DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblCommitWorkItem PRIMARY KEY (IdCommitWorkItem),
            CONSTRAINT UQ_tblCommitWorkItem_CommitWorkItem UNIQUE (IdCommit, IdWorkItem),
            CONSTRAINT FK_tblCommitWorkItem_tblCommit FOREIGN KEY (IdCommit) REFERENCES dbo.tblCommit (IdCommit),
            CONSTRAINT FK_tblCommitWorkItem_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem)
        )
        PRINT 'OK: tblCommitWorkItem creada (vinculo por folio en el mensaje)'
    END
    ELSE PRINT 'SKIP: tblCommitWorkItem ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPullRequest')
    BEGIN
        CREATE TABLE dbo.tblPullRequest
        (
            IdPullRequest   INT           IDENTITY(1,1) NOT NULL,
            IdRepositorio   INT                         NOT NULL,
            Numero          INT                         NOT NULL,
            Titulo          NVARCHAR(200)               NOT NULL,
            IdWorkItem      INT                         NULL,
            Autor           NVARCHAR(200)               NOT NULL,
            EstatusPr       NVARCHAR(20)                NOT NULL,   -- estado espejo de Gitea, no del motor
            RamaOrigen      NVARCHAR(200)               NULL,
            RamaDestino     NVARCHAR(200)               NULL,
            Url             NVARCHAR(500)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPullRequest_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            CONSTRAINT PK_tblPullRequest PRIMARY KEY (IdPullRequest),
            CONSTRAINT UQ_tblPullRequest_RepositorioNumero UNIQUE (IdRepositorio, Numero),
            CONSTRAINT FK_tblPullRequest_tblRepositorio FOREIGN KEY (IdRepositorio) REFERENCES dbo.tblRepositorio (IdRepositorio),
            CONSTRAINT FK_tblPullRequest_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT CK_tblPullRequest_EstatusPr CHECK (EstatusPr IN (N'Abierto', N'Fusionado', N'Cerrado'))
        )
        PRINT 'OK: tblPullRequest creada'
    END
    ELSE PRINT 'SKIP: tblPullRequest ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPipelineEjecucion')
    BEGIN
        CREATE TABLE dbo.tblPipelineEjecucion
        (
            IdPipelineEjecucion INT           IDENTITY(1,1) NOT NULL,
            IdRepositorio       INT                         NOT NULL,
            Numero              INT                         NOT NULL,
            Tipo                NVARCHAR(20)                NOT NULL,
            Resultado           NVARCHAR(20)                NOT NULL,
            IdAmbiente          INT                         NULL,
            DuracionSegundos    INT                         NULL,
            Url                 NVARCHAR(500)               NULL,
            FechaInicio         DATETIME2                   NOT NULL,
            FechaFin            DATETIME2                   NULL,
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblPipelineEjecucion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblPipelineEjecucion PRIMARY KEY (IdPipelineEjecucion),
            CONSTRAINT UQ_tblPipelineEjecucion_RepositorioNumero UNIQUE (IdRepositorio, Numero),
            CONSTRAINT FK_tblPipelineEjecucion_tblRepositorio FOREIGN KEY (IdRepositorio) REFERENCES dbo.tblRepositorio (IdRepositorio),
            CONSTRAINT FK_tblPipelineEjecucion_tblAmbiente FOREIGN KEY (IdAmbiente) REFERENCES dbo.tblAmbiente (IdAmbiente),
            CONSTRAINT CK_tblPipelineEjecucion_Tipo CHECK (Tipo IN (N'Build', N'Deploy')),
            CONSTRAINT CK_tblPipelineEjecucion_Resultado CHECK (Resultado IN (N'En Ejecucion', N'Exitoso', N'Fallido'))
        )
        PRINT 'OK: tblPipelineEjecucion creada'
    END
    ELSE PRINT 'SKIP: tblPipelineEjecucion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArtefacto')
    BEGIN
        CREATE TABLE dbo.tblArtefacto
        (
            IdArtefacto         INT           IDENTITY(1,1) NOT NULL,
            IdPipelineEjecucion INT                         NULL,
            IdArchivo           INT                         NULL,
            Nombre              NVARCHAR(200)               NOT NULL,
            IdTipoArtefacto     INT                         NOT NULL,
            HashSha256          NVARCHAR(100)               NULL,
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblArtefacto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            UsuarioMovto        NVARCHAR(50)                NULL,
            FechaMovto          DATETIME                    NULL,
            Activo              BIT                         NOT NULL CONSTRAINT DF_tblArtefacto_Activo DEFAULT (1),
            CONSTRAINT PK_tblArtefacto PRIMARY KEY (IdArtefacto),
            CONSTRAINT FK_tblArtefacto_tblPipelineEjecucion FOREIGN KEY (IdPipelineEjecucion) REFERENCES dbo.tblPipelineEjecucion (IdPipelineEjecucion),
            CONSTRAINT FK_tblArtefacto_tblArchivo FOREIGN KEY (IdArchivo) REFERENCES dbo.tblArchivo (IdArchivo),
            CONSTRAINT FK_tblArtefacto_tblTipoArtefacto FOREIGN KEY (IdTipoArtefacto) REFERENCES dbo.tblTipoArtefacto (Id)
        )
        PRINT 'OK: tblArtefacto creada'
    END
    ELSE PRINT 'SKIP: tblArtefacto ya existe'

    /* ---------- Releases ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRelease')
    BEGIN
        CREATE TABLE dbo.tblRelease
        (
            IdRelease        INT           IDENTITY(1,1) NOT NULL,
            IdProyecto       INT                         NOT NULL,
            Version          NVARCHAR(50)                NOT NULL,
            Folio            NVARCHAR(50)                NULL,
            NotasVersion     NVARCHAR(MAX)               NULL,
            IdEstatusRelease INT                         NOT NULL,
            FechaPlan        DATE                        NULL,
            FechaLiberacion  DATETIME2                   NULL,
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblRelease_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            UsuarioMovto     NVARCHAR(50)                NULL,
            FechaMovto       DATETIME                    NULL,
            Activo           BIT                         NOT NULL CONSTRAINT DF_tblRelease_Activo DEFAULT (1),
            CONSTRAINT PK_tblRelease PRIMARY KEY (IdRelease),
            CONSTRAINT UQ_tblRelease_ProyectoVersion UNIQUE (IdProyecto, Version),
            CONSTRAINT FK_tblRelease_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblRelease_tblEstatusRelease FOREIGN KEY (IdEstatusRelease) REFERENCES dbo.tblEstatusRelease (Id)
        )
        CREATE UNIQUE INDEX UQ_tblRelease_Folio ON dbo.tblRelease (Folio) WHERE Folio IS NOT NULL
        PRINT 'OK: tblRelease creada'
    END
    ELSE PRINT 'SKIP: tblRelease ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseArtefacto')
    BEGIN
        CREATE TABLE dbo.tblReleaseArtefacto
        (
            IdReleaseArtefacto        INT           IDENTITY(1,1) NOT NULL,
            IdRelease                 INT                         NOT NULL,
            IdArtefacto               INT                         NOT NULL,
            OrdenEjecucion            INT                         NULL,   -- scripts SQL: orden de corrida
            IdArtefactoRollback       INT                         NULL,   -- RN-REL-02: rollback pareado
            JustificacionIrreversible NVARCHAR(500)               NULL,   -- obligatoria si no hay rollback (valida el backend)
            FechaRegistro             DATETIME2                   NOT NULL CONSTRAINT DF_tblReleaseArtefacto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro           NVARCHAR(200)               NOT NULL,
            Activo                    BIT                         NOT NULL CONSTRAINT DF_tblReleaseArtefacto_Activo DEFAULT (1),
            CONSTRAINT PK_tblReleaseArtefacto PRIMARY KEY (IdReleaseArtefacto),
            CONSTRAINT UQ_tblReleaseArtefacto_ReleaseArtefacto UNIQUE (IdRelease, IdArtefacto),
            CONSTRAINT FK_tblReleaseArtefacto_tblRelease FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease),
            CONSTRAINT FK_tblReleaseArtefacto_tblArtefacto FOREIGN KEY (IdArtefacto) REFERENCES dbo.tblArtefacto (IdArtefacto),
            CONSTRAINT FK_tblReleaseArtefacto_tblArtefactoRollback FOREIGN KEY (IdArtefactoRollback) REFERENCES dbo.tblArtefacto (IdArtefacto)
        )
        PRINT 'OK: tblReleaseArtefacto creada'
    END
    ELSE PRINT 'SKIP: tblReleaseArtefacto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblDespliegue')
    BEGIN
        CREATE TABLE dbo.tblDespliegue
        (
            IdDespliegue        INT           IDENTITY(1,1) NOT NULL,
            IdRelease           INT                         NOT NULL,
            IdAmbiente          INT                         NOT NULL,
            IdEstatusDespliegue INT                         NOT NULL,
            FechaInicio         DATETIME2                   NOT NULL CONSTRAINT DF_tblDespliegue_FechaInicio DEFAULT (SYSDATETIME()),
            FechaFin            DATETIME2                   NULL,
            IdEjecutor          INT                         NULL,
            EsRollback          BIT                         NOT NULL CONSTRAINT DF_tblDespliegue_EsRollback DEFAULT (0),
            Bitacora            NVARCHAR(MAX)               NULL,
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblDespliegue_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblDespliegue PRIMARY KEY (IdDespliegue),
            CONSTRAINT FK_tblDespliegue_tblRelease FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease),
            CONSTRAINT FK_tblDespliegue_tblAmbiente FOREIGN KEY (IdAmbiente) REFERENCES dbo.tblAmbiente (IdAmbiente),
            CONSTRAINT FK_tblDespliegue_tblEstatusDespliegue FOREIGN KEY (IdEstatusDespliegue) REFERENCES dbo.tblEstatusDespliegue (Id),
            CONSTRAINT FK_tblDespliegue_tblUsuario FOREIGN KEY (IdEjecutor) REFERENCES dbo.tblUsuario (IdUsuario)
        )
        CREATE INDEX IX_tblDespliegue_Ambiente ON dbo.tblDespliegue (IdAmbiente, FechaInicio)
        PRINT 'OK: tblDespliegue creada'
    END
    ELSE PRINT 'SKIP: tblDespliegue ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblAprobacion')
    BEGIN
        CREATE TABLE dbo.tblAprobacion
        (
            IdAprobacion        INT           IDENTITY(1,1) NOT NULL,
            Entidad             NVARCHAR(100)               NOT NULL,   -- Release, Solicitud, Ausencia...
            IdEntidad           INT                         NOT NULL,
            IdAprobador         INT                         NOT NULL,
            RolAprobacion       NVARCHAR(100)               NOT NULL,   -- QA, Lider, Negocio
            IdEstatusAprobacion INT                         NOT NULL,
            FechaResolucion     DATETIME2                   NULL,
            Comentario          NVARCHAR(500)               NULL,
            FirmaHash           NVARCHAR(200)               NULL,       -- SHA-256(usuario+fechaUTC+entidad+folio+decision)
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblAprobacion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            Activo              BIT                         NOT NULL CONSTRAINT DF_tblAprobacion_Activo DEFAULT (1),
            CONSTRAINT PK_tblAprobacion PRIMARY KEY (IdAprobacion),
            CONSTRAINT FK_tblAprobacion_tblUsuario FOREIGN KEY (IdAprobador) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblAprobacion_tblEstatusAprobacion FOREIGN KEY (IdEstatusAprobacion) REFERENCES dbo.tblEstatusAprobacion (Id)
        )
        CREATE INDEX IX_tblAprobacion_Entidad ON dbo.tblAprobacion (Entidad, IdEntidad)
        PRINT 'OK: tblAprobacion creada'
    END
    ELSE PRINT 'SKIP: tblAprobacion ya existe'

    /* ---------- QA ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPlanPrueba')
    BEGIN
        CREATE TABLE dbo.tblPlanPrueba
        (
            IdPlanPrueba    INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NOT NULL,
            IdRelease       INT                         NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPlanPrueba_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblPlanPrueba_Activo DEFAULT (1),
            CONSTRAINT PK_tblPlanPrueba PRIMARY KEY (IdPlanPrueba),
            CONSTRAINT FK_tblPlanPrueba_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblPlanPrueba_tblRelease FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease)
        )
        PRINT 'OK: tblPlanPrueba creada'
    END
    ELSE PRINT 'SKIP: tblPlanPrueba ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPrueba')
    BEGIN
        CREATE TABLE dbo.tblCasoPrueba
        (
            IdCasoPrueba      INT           IDENTITY(1,1) NOT NULL,
            Folio             NVARCHAR(50)                NULL,
            IdPlanPrueba      INT                         NOT NULL,
            Titulo            NVARCHAR(200)               NOT NULL,
            Precondiciones    NVARCHAR(MAX)               NULL,
            ResultadoEsperado NVARCHAR(MAX)               NULL,
            IdTipoPrueba      INT                         NOT NULL,
            IdWorkItem        INT                         NULL,   -- requisito cubierto (trazabilidad)
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblCasoPrueba_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblCasoPrueba_Activo DEFAULT (1),
            CONSTRAINT PK_tblCasoPrueba PRIMARY KEY (IdCasoPrueba),
            CONSTRAINT FK_tblCasoPrueba_tblPlanPrueba FOREIGN KEY (IdPlanPrueba) REFERENCES dbo.tblPlanPrueba (IdPlanPrueba),
            CONSTRAINT FK_tblCasoPrueba_tblTipoPrueba FOREIGN KEY (IdTipoPrueba) REFERENCES dbo.tblTipoPrueba (Id),
            CONSTRAINT FK_tblCasoPrueba_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem)
        )
        CREATE UNIQUE INDEX UQ_tblCasoPrueba_Folio ON dbo.tblCasoPrueba (Folio) WHERE Folio IS NOT NULL
        CREATE INDEX IX_tblCasoPrueba_WorkItem ON dbo.tblCasoPrueba (IdWorkItem) WHERE IdWorkItem IS NOT NULL
        PRINT 'OK: tblCasoPrueba creada'
    END
    ELSE PRINT 'SKIP: tblCasoPrueba ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPruebaPaso')
    BEGIN
        CREATE TABLE dbo.tblCasoPruebaPaso
        (
            IdCasoPruebaPaso  INT           IDENTITY(1,1) NOT NULL,
            IdCasoPrueba      INT                         NOT NULL,
            NumeroPaso        INT                         NOT NULL,
            Accion            NVARCHAR(MAX)               NOT NULL,
            ResultadoEsperado NVARCHAR(MAX)               NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblCasoPruebaPaso_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblCasoPruebaPaso PRIMARY KEY (IdCasoPruebaPaso),
            CONSTRAINT UQ_tblCasoPruebaPaso_CasoNumero UNIQUE (IdCasoPrueba, NumeroPaso),
            CONSTRAINT FK_tblCasoPruebaPaso_tblCasoPrueba FOREIGN KEY (IdCasoPrueba) REFERENCES dbo.tblCasoPrueba (IdCasoPrueba)
        )
        PRINT 'OK: tblCasoPruebaPaso creada'
    END
    ELSE PRINT 'SKIP: tblCasoPruebaPaso ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCicloPrueba')
    BEGIN
        CREATE TABLE dbo.tblCicloPrueba
        (
            IdCicloPrueba   INT           IDENTITY(1,1) NOT NULL,
            IdPlanPrueba    INT                         NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            FechaInicio     DATE                        NULL,
            FechaFin        DATE                        NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblCicloPrueba_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblCicloPrueba_Activo DEFAULT (1),
            CONSTRAINT PK_tblCicloPrueba PRIMARY KEY (IdCicloPrueba),
            CONSTRAINT FK_tblCicloPrueba_tblPlanPrueba FOREIGN KEY (IdPlanPrueba) REFERENCES dbo.tblPlanPrueba (IdPlanPrueba)
        )
        PRINT 'OK: tblCicloPrueba creada'
    END
    ELSE PRINT 'SKIP: tblCicloPrueba ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEjecucionPrueba')
    BEGIN
        CREATE TABLE dbo.tblEjecucionPrueba
        (
            IdEjecucionPrueba INT           IDENTITY(1,1) NOT NULL,
            IdCasoPrueba      INT                         NOT NULL,
            IdCicloPrueba     INT                         NOT NULL,
            IdEjecutor        INT                         NOT NULL,
            IdResultadoPrueba INT                         NOT NULL,
            FechaEjecucion    DATETIME2                   NOT NULL CONSTRAINT DF_tblEjecucionPrueba_FechaEjecucion DEFAULT (SYSDATETIME()),
            Observaciones     NVARCHAR(MAX)               NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblEjecucionPrueba_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblEjecucionPrueba PRIMARY KEY (IdEjecucionPrueba),
            CONSTRAINT FK_tblEjecucionPrueba_tblCasoPrueba FOREIGN KEY (IdCasoPrueba) REFERENCES dbo.tblCasoPrueba (IdCasoPrueba),
            CONSTRAINT FK_tblEjecucionPrueba_tblCicloPrueba FOREIGN KEY (IdCicloPrueba) REFERENCES dbo.tblCicloPrueba (IdCicloPrueba),
            CONSTRAINT FK_tblEjecucionPrueba_tblUsuario FOREIGN KEY (IdEjecutor) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblEjecucionPrueba_tblResultadoPrueba FOREIGN KEY (IdResultadoPrueba) REFERENCES dbo.tblResultadoPrueba (Id)
        )
        CREATE INDEX IX_tblEjecucionPrueba_Ciclo ON dbo.tblEjecucionPrueba (IdCicloPrueba)
        PRINT 'OK: tblEjecucionPrueba creada'
    END
    ELSE PRINT 'SKIP: tblEjecucionPrueba ya existe'

    /* ---------- FKs pendientes de tblWorkItem ---------- */
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblRelease')
    BEGIN
        ALTER TABLE dbo.tblWorkItem
            ADD CONSTRAINT FK_tblWorkItem_tblRelease FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease)
        PRINT 'OK: FK_tblWorkItem_tblRelease agregada'
    END
    ELSE PRINT 'SKIP: FK_tblWorkItem_tblRelease ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblEjecucionPrueba')
    BEGIN
        ALTER TABLE dbo.tblWorkItem
            ADD CONSTRAINT FK_tblWorkItem_tblEjecucionPrueba FOREIGN KEY (IdEjecucionPruebaOrigen) REFERENCES dbo.tblEjecucionPrueba (IdEjecucionPrueba)
        PRINT 'OK: FK_tblWorkItem_tblEjecucionPrueba agregada'
    END
    ELSE PRINT 'SKIP: FK_tblWorkItem_tblEjecucionPrueba ya existe'

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
