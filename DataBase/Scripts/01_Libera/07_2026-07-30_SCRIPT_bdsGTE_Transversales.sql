USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      07_2026-07-30_SCRIPT_bdsGTE_Transversales.sql
   Autor:       Equipo GTE
   Descripcion: Transversales: MOTOR DE ESTATUS PROPIO (tblProceso,
                tblTransicion - GTE es independiente de toda otra base),
                folios (tblFolio), bitacora de API, notificaciones,
                plantillas, reglas de automatizacion, outbox de eventos
                de dominio, KPIs, versiones del sistema y metadatos de
                UI del workflow (tblTransicionConfig).
   Requiere:    01, 02
   ===================================================================== */
BEGIN TRY

    /* ---------- Motor de estatus propio de GTE (patron transversal seccion 9
       del estandar, clonado dentro de bdsGTE por decision de independencia
       total: ADR-03 del Documento Maestro, 2026-07-30) ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblProceso')
    BEGIN
        CREATE TABLE dbo.tblProceso
        (
            IdProceso          INT           IDENTITY(1,1) NOT NULL,
            Proceso            NVARCHAR(100)               NOT NULL,
            TablaEstatus       NVARCHAR(300)               NOT NULL,   -- esquema.tabla (dbo.tblEstatusWorkItem)
            TablaTransaccional NVARCHAR(300)               NOT NULL,   -- esquema.tabla (dbo.tblWorkItem)
            ColumnaEstatus     NVARCHAR(128)               NOT NULL,
            ColumnaPK          NVARCHAR(128)               NOT NULL,
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblProceso_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            UsuarioMovto       NVARCHAR(50)                NULL,
            FechaMovto         DATETIME                    NULL,
            Activo             BIT                         NOT NULL CONSTRAINT DF_tblProceso_Activo DEFAULT (1),
            CONSTRAINT PK_tblProceso PRIMARY KEY (IdProceso),
            CONSTRAINT UQ_tblProceso_Proceso UNIQUE (Proceso)
        )
        PRINT 'OK: tblProceso creada (motor de estatus propio de bdsGTE)'
    END
    ELSE PRINT 'SKIP: tblProceso ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTransicion')
    BEGIN
        CREATE TABLE dbo.tblTransicion
        (
            IdTransicion     INT          IDENTITY(1,1) NOT NULL,
            IdProceso        INT                        NOT NULL,
            IdEstatusOrigen  INT                        NOT NULL,
            Accion           NVARCHAR(50)               NOT NULL,
            IdEstatusDestino INT                        NOT NULL,
            FechaRegistro    DATETIME2                  NOT NULL CONSTRAINT DF_tblTransicion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)              NOT NULL,
            UsuarioMovto     NVARCHAR(50)               NULL,
            FechaMovto       DATETIME                   NULL,
            Activo           BIT                        NOT NULL CONSTRAINT DF_tblTransicion_Activo DEFAULT (1),
            CONSTRAINT PK_tblTransicion PRIMARY KEY (IdTransicion),
            CONSTRAINT UQ_tblTransicion_ProcesoOrigenAccion UNIQUE (IdProceso, IdEstatusOrigen, Accion),
            CONSTRAINT FK_tblTransicion_tblProceso FOREIGN KEY (IdProceso) REFERENCES dbo.tblProceso (IdProceso)
        )
        PRINT 'OK: tblTransicion creada (dado origen + accion, el destino es unico)'
    END
    ELSE PRINT 'SKIP: tblTransicion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblFolio')
    BEGIN
        CREATE TABLE dbo.tblFolio
        (
            IdFolio           INT           IDENTITY(1,1) NOT NULL,
            Serie             NVARCHAR(50)                NOT NULL,   -- ej. SOL-2026, GTE, TKT-2026
            UltimoConsecutivo INT                         NOT NULL CONSTRAINT DF_tblFolio_UltimoConsecutivo DEFAULT (0),
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblFolio_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            CONSTRAINT PK_tblFolio PRIMARY KEY (IdFolio),
            CONSTRAINT UQ_tblFolio_Serie UNIQUE (Serie)
        )
        PRINT 'OK: tblFolio creada (generacion de folios propia de bdsGTE)'
    END
    ELSE PRINT 'SKIP: tblFolio ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblBitacora')
    BEGIN
        CREATE TABLE dbo.tblBitacora
        (
            IdBitacora BIGINT        IDENTITY(1,1) NOT NULL,
            Usuario    NVARCHAR(200)               NOT NULL,
            Ip         NVARCHAR(50)                NULL,
            Endpoint   NVARCHAR(500)               NULL,
            Entidad    NVARCHAR(100)               NOT NULL,
            IdEntidad  INT                         NULL,
            Accion     NVARCHAR(100)               NOT NULL,
            Detalle    NVARCHAR(MAX)               NULL,
            Fecha      DATETIME2                   NOT NULL CONSTRAINT DF_tblBitacora_Fecha DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblBitacora PRIMARY KEY (IdBitacora)
        )
        CREATE INDEX IX_tblBitacora_EntidadFecha ON dbo.tblBitacora (Entidad, IdEntidad, Fecha)
        CREATE INDEX IX_tblBitacora_UsuarioFecha ON dbo.tblBitacora (Usuario, Fecha)
        PRINT 'OK: tblBitacora creada (coincide con la entidad EF Bitacora)'
    END
    ELSE PRINT 'SKIP: tblBitacora ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblNotificacion')
    BEGIN
        CREATE TABLE dbo.tblNotificacion
        (
            IdNotificacion  BIGINT        IDENTITY(1,1) NOT NULL,
            IdUsuario       INT                         NOT NULL,
            Titulo          NVARCHAR(200)               NOT NULL,
            Mensaje         NVARCHAR(500)               NULL,
            Entidad         NVARCHAR(100)               NULL,
            IdEntidad       INT                         NULL,
            Url             NVARCHAR(500)               NULL,
            Leida           BIT                         NOT NULL CONSTRAINT DF_tblNotificacion_Leida DEFAULT (0),
            FechaLeida      DATETIME2                   NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblNotificacion_FechaRegistro DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblNotificacion PRIMARY KEY (IdNotificacion),
            CONSTRAINT FK_tblNotificacion_tblUsuario FOREIGN KEY (IdUsuario) REFERENCES dbo.tblUsuario (IdUsuario)
        )
        CREATE INDEX IX_tblNotificacion_NoLeidas ON dbo.tblNotificacion (IdUsuario) WHERE Leida = 0
        PRINT 'OK: tblNotificacion creada'
    END
    ELSE PRINT 'SKIP: tblNotificacion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPlantillaNotificacion')
    BEGIN
        CREATE TABLE dbo.tblPlantillaNotificacion
        (
            IdPlantillaNotificacion INT           IDENTITY(1,1) NOT NULL,
            Clave                   NVARCHAR(100)               NOT NULL,
            Asunto                  NVARCHAR(200)               NOT NULL,
            Cuerpo                  NVARCHAR(MAX)               NOT NULL,   -- placeholders tipados: {folio}, {titulo}, {url}
            Canal                   NVARCHAR(50)                NOT NULL,   -- InApp, Correo, Teams, WhatsApp
            FechaRegistro           DATETIME2                   NOT NULL CONSTRAINT DF_tblPlantillaNotificacion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro         NVARCHAR(200)               NOT NULL,
            UsuarioMovto            NVARCHAR(50)                NULL,
            FechaMovto              DATETIME                    NULL,
            Activo                  BIT                         NOT NULL CONSTRAINT DF_tblPlantillaNotificacion_Activo DEFAULT (1),
            CONSTRAINT PK_tblPlantillaNotificacion PRIMARY KEY (IdPlantillaNotificacion),
            CONSTRAINT UQ_tblPlantillaNotificacion_ClaveCanal UNIQUE (Clave, Canal)
        )
        PRINT 'OK: tblPlantillaNotificacion creada'
    END
    ELSE PRINT 'SKIP: tblPlantillaNotificacion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReglaAutomatizacion')
    BEGIN
        CREATE TABLE dbo.tblReglaAutomatizacion
        (
            IdReglaAutomatizacion INT           IDENTITY(1,1) NOT NULL,
            Nombre                NVARCHAR(200)               NOT NULL,
            Evento                NVARCHAR(100)               NOT NULL,   -- WorkItem.EstatusCambiado, Ticket.Creado...
            CondicionJson         NVARCHAR(MAX)               NULL,
            AccionJson            NVARCHAR(MAX)               NOT NULL,
            ContadorEjecuciones   INT                         NOT NULL CONSTRAINT DF_tblReglaAutomatizacion_ContadorEjecuciones DEFAULT (0),
            FechaRegistro         DATETIME2                   NOT NULL CONSTRAINT DF_tblReglaAutomatizacion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)               NOT NULL,
            UsuarioMovto          NVARCHAR(50)                NULL,
            FechaMovto            DATETIME                    NULL,
            Activo                BIT                         NOT NULL CONSTRAINT DF_tblReglaAutomatizacion_Activo DEFAULT (1),
            CONSTRAINT PK_tblReglaAutomatizacion PRIMARY KEY (IdReglaAutomatizacion),
            CONSTRAINT UQ_tblReglaAutomatizacion_Nombre UNIQUE (Nombre),
            CONSTRAINT CK_tblReglaAutomatizacion_CondicionJson CHECK (CondicionJson IS NULL OR ISJSON(CondicionJson) = 1),
            CONSTRAINT CK_tblReglaAutomatizacion_AccionJson CHECK (ISJSON(AccionJson) = 1)
        )
        PRINT 'OK: tblReglaAutomatizacion creada (JSON validado, nunca SQL en datos)'
    END
    ELSE PRINT 'SKIP: tblReglaAutomatizacion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEventoDominio')
    BEGIN
        CREATE TABLE dbo.tblEventoDominio
        (
            IdEventoDominio BIGINT        IDENTITY(1,1) NOT NULL,
            TipoEvento      NVARCHAR(100)               NOT NULL,
            PayloadJson     NVARCHAR(MAX)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblEventoDominio_FechaRegistro DEFAULT (SYSDATETIME()),
            FechaProcesado  DATETIME2                   NULL,
            Intentos        INT                         NOT NULL CONSTRAINT DF_tblEventoDominio_Intentos DEFAULT (0),
            UltimoError     NVARCHAR(MAX)               NULL,
            CONSTRAINT PK_tblEventoDominio PRIMARY KEY (IdEventoDominio),
            CONSTRAINT CK_tblEventoDominio_PayloadJson CHECK (ISJSON(PayloadJson) = 1)
        )
        CREATE INDEX IX_tblEventoDominio_Pendientes ON dbo.tblEventoDominio (FechaRegistro) WHERE FechaProcesado IS NULL
        PRINT 'OK: tblEventoDominio creada (outbox: Hangfire despacha los pendientes)'
    END
    ELSE PRINT 'SKIP: tblEventoDominio ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblKpiDefinicion')
    BEGIN
        CREATE TABLE dbo.tblKpiDefinicion
        (
            IdKpiDefinicion INT           IDENTITY(1,1) NOT NULL,
            Clave           NVARCHAR(100)               NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            Meta            DECIMAL(18,4)               NULL,
            Direccion       NVARCHAR(10)                NOT NULL,   -- Subir o Bajar (que es mejor)
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblKpiDefinicion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblKpiDefinicion_Activo DEFAULT (1),
            CONSTRAINT PK_tblKpiDefinicion PRIMARY KEY (IdKpiDefinicion),
            CONSTRAINT UQ_tblKpiDefinicion_Clave UNIQUE (Clave),
            CONSTRAINT CK_tblKpiDefinicion_Direccion CHECK (Direccion IN (N'Subir', N'Bajar'))
        )
        PRINT 'OK: tblKpiDefinicion creada'
    END
    ELSE PRINT 'SKIP: tblKpiDefinicion ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblKpiValor')
    BEGIN
        CREATE TABLE dbo.tblKpiValor
        (
            IdKpiValor      BIGINT        IDENTITY(1,1) NOT NULL,
            IdKpiDefinicion INT                         NOT NULL,
            Fecha           DATE                        NOT NULL,
            Alcance         NVARCHAR(100)               NOT NULL CONSTRAINT DF_tblKpiValor_Alcance DEFAULT (N'global'),
            Valor           DECIMAL(18,4)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblKpiValor_FechaRegistro DEFAULT (SYSDATETIME()),
            CONSTRAINT PK_tblKpiValor PRIMARY KEY (IdKpiValor),
            CONSTRAINT UQ_tblKpiValor_KpiFechaAlcance UNIQUE (IdKpiDefinicion, Fecha, Alcance),
            CONSTRAINT FK_tblKpiValor_tblKpiDefinicion FOREIGN KEY (IdKpiDefinicion) REFERENCES dbo.tblKpiDefinicion (IdKpiDefinicion)
        )
        PRINT 'OK: tblKpiValor creada (series historicas estables)'
    END
    ELSE PRINT 'SKIP: tblKpiValor ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblVersionSistema')
    BEGIN
        CREATE TABLE dbo.tblVersionSistema
        (
            IdVersionSistema INT           IDENTITY(1,1) NOT NULL,
            Version          NVARCHAR(50)                NOT NULL,
            FechaLiberacion  DATETIME2                   NOT NULL,
            Notas            NVARCHAR(MAX)               NULL,
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblVersionSistema_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblVersionSistema PRIMARY KEY (IdVersionSistema),
            CONSTRAINT UQ_tblVersionSistema_Version UNIQUE (Version)
        )
        PRINT 'OK: tblVersionSistema creada (sucesora de tblVersion del GT)'
    END
    ELSE PRINT 'SKIP: tblVersionSistema ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTransicionConfig')
    BEGIN
        CREATE TABLE dbo.tblTransicionConfig
        (
            IdTransicionConfig INT           IDENTITY(1,1) NOT NULL,
            Proceso            NVARCHAR(50)                NOT NULL,   -- nombre en dbo.tblProceso
            IdEstatusOrigen    INT                         NOT NULL,
            Accion             NVARCHAR(50)                NOT NULL,
            EtiquetaBoton      NVARCHAR(100)               NOT NULL,
            IconoAccion        NVARCHAR(50)                NULL,
            RequierePermiso    NVARCHAR(100)               NULL,       -- clave de tblPermiso
            RequiereMotivo     BIT                         NOT NULL CONSTRAINT DF_tblTransicionConfig_RequiereMotivo DEFAULT (0),
            EsAccionPrincipal  BIT                         NOT NULL CONSTRAINT DF_tblTransicionConfig_EsAccionPrincipal DEFAULT (0),
            Orden              INT                         NOT NULL CONSTRAINT DF_tblTransicionConfig_Orden DEFAULT (0),
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblTransicionConfig_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            UsuarioMovto       NVARCHAR(50)                NULL,
            FechaMovto         DATETIME                    NULL,
            Activo             BIT                         NOT NULL CONSTRAINT DF_tblTransicionConfig_Activo DEFAULT (1),
            CONSTRAINT PK_tblTransicionConfig PRIMARY KEY (IdTransicionConfig),
            CONSTRAINT UQ_tblTransicionConfig_ProcesoOrigenAccion UNIQUE (Proceso, IdEstatusOrigen, Accion)
        )
        PRINT 'OK: tblTransicionConfig creada (metadatos de UI; el grafo vive en dbo.tblTransicion)'
    END
    ELSE PRINT 'SKIP: tblTransicionConfig ya existe'

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
