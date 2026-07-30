USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-07-30_SCRIPT_bdsGTE_Portafolio.sql
   Autor:       Equipo GTE
   Descripcion: Portafolio: portafolios, programas, proyectos, hitos,
                riesgos (exposicion computada), OKRs, tarifas por nivel,
                presupuestos, ambientes y repositorios.
                Agrega la FK pendiente tblUsuarioRol.IdProyecto.
   Requiere:    01 (catalogos), 02 (usuarios, equipos)
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPortafolio')
    BEGIN
        CREATE TABLE dbo.tblPortafolio
        (
            IdPortafolio    INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPortafolio_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblPortafolio_Activo DEFAULT (1),
            CONSTRAINT PK_tblPortafolio PRIMARY KEY (IdPortafolio),
            CONSTRAINT UQ_tblPortafolio_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblPortafolio creada'
    END
    ELSE PRINT 'SKIP: tblPortafolio ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPrograma')
    BEGIN
        CREATE TABLE dbo.tblPrograma
        (
            IdPrograma      INT           IDENTITY(1,1) NOT NULL,
            IdPortafolio    INT                         NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblPrograma_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblPrograma_Activo DEFAULT (1),
            CONSTRAINT PK_tblPrograma PRIMARY KEY (IdPrograma),
            CONSTRAINT UQ_tblPrograma_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_tblPrograma_tblPortafolio FOREIGN KEY (IdPortafolio) REFERENCES dbo.tblPortafolio (IdPortafolio)
        )
        PRINT 'OK: tblPrograma creada'
    END
    ELSE PRINT 'SKIP: tblPrograma ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblProyecto')
    BEGIN
        CREATE TABLE dbo.tblProyecto
        (
            IdProyecto          INT           IDENTITY(1,1) NOT NULL,
            Folio               NVARCHAR(50)                NULL,   -- dbo.spGenerarFolio al autorizar
            Clave               NVARCHAR(20)                NOT NULL, -- prefijo de folios hijos (GTE-123)
            Nombre              NVARCHAR(200)               NOT NULL,
            IdPrograma          INT                         NULL,
            IdCategoriaProyecto INT                         NOT NULL,
            IdEstatusProyecto   INT                         NOT NULL,
            IdResponsable       INT                         NULL,
            IdEquipo            INT                         NULL,
            FechaInicioPlan     DATETIME2                   NULL,
            FechaFinPlan        DATETIME2                   NULL,
            FechaInicioReal     DATETIME2                   NULL,
            FechaFinReal        DATETIME2                   NULL,
            EsMantenimiento     BIT                         NOT NULL CONSTRAINT DF_tblProyecto_EsMantenimiento DEFAULT (0),
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblProyecto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            UsuarioMovto        NVARCHAR(50)                NULL,
            FechaMovto          DATETIME                    NULL,
            Activo              BIT                         NOT NULL CONSTRAINT DF_tblProyecto_Activo DEFAULT (1),
            CONSTRAINT PK_tblProyecto PRIMARY KEY (IdProyecto),
            CONSTRAINT UQ_tblProyecto_Clave UNIQUE (Clave),
            CONSTRAINT FK_tblProyecto_tblPrograma FOREIGN KEY (IdPrograma) REFERENCES dbo.tblPrograma (IdPrograma),
            CONSTRAINT FK_tblProyecto_tblCategoriaProyecto FOREIGN KEY (IdCategoriaProyecto) REFERENCES dbo.tblCategoriaProyecto (Id),
            CONSTRAINT FK_tblProyecto_tblEstatusProyecto FOREIGN KEY (IdEstatusProyecto) REFERENCES dbo.tblEstatusProyecto (Id),
            CONSTRAINT FK_tblProyecto_tblUsuario FOREIGN KEY (IdResponsable) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblProyecto_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo)
        )
        CREATE UNIQUE INDEX UQ_tblProyecto_Folio ON dbo.tblProyecto (Folio) WHERE Folio IS NOT NULL
        PRINT 'OK: tblProyecto creada'
    END
    ELSE PRINT 'SKIP: tblProyecto ya existe'

    /* FK pendiente del script 02 */
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblUsuarioRol_tblProyecto')
    BEGIN
        ALTER TABLE dbo.tblUsuarioRol
            ADD CONSTRAINT FK_tblUsuarioRol_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto)
        PRINT 'OK: FK_tblUsuarioRol_tblProyecto agregada'
    END
    ELSE PRINT 'SKIP: FK_tblUsuarioRol_tblProyecto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblHito')
    BEGIN
        CREATE TABLE dbo.tblHito
        (
            IdHito          INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            FechaPlan       DATE                        NOT NULL,
            FechaReal       DATE                        NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblHito_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblHito_Activo DEFAULT (1),
            CONSTRAINT PK_tblHito PRIMARY KEY (IdHito),
            CONSTRAINT FK_tblHito_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto)
        )
        PRINT 'OK: tblHito creada'
    END
    ELSE PRINT 'SKIP: tblHito ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRiesgo')
    BEGIN
        CREATE TABLE dbo.tblRiesgo
        (
            IdRiesgo        INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NOT NULL,
            Descripcion     NVARCHAR(500)               NOT NULL,
            Probabilidad    TINYINT                     NOT NULL,
            Impacto         TINYINT                     NOT NULL,
            Exposicion      AS (Probabilidad * Impacto) PERSISTED,
            PlanMitigacion  NVARCHAR(500)               NULL,
            IdResponsable   INT                         NULL,
            IdEstatusRiesgo INT                         NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblRiesgo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblRiesgo_Activo DEFAULT (1),
            CONSTRAINT PK_tblRiesgo PRIMARY KEY (IdRiesgo),
            CONSTRAINT FK_tblRiesgo_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblRiesgo_tblUsuario FOREIGN KEY (IdResponsable) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT FK_tblRiesgo_tblEstatusRiesgo FOREIGN KEY (IdEstatusRiesgo) REFERENCES dbo.tblEstatusRiesgo (Id),
            CONSTRAINT CK_tblRiesgo_Probabilidad CHECK (Probabilidad BETWEEN 1 AND 5),
            CONSTRAINT CK_tblRiesgo_Impacto CHECK (Impacto BETWEEN 1 AND 5)
        )
        PRINT 'OK: tblRiesgo creada (Exposicion = computada persistida)'
    END
    ELSE PRINT 'SKIP: tblRiesgo ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblObjetivoOkr')
    BEGIN
        CREATE TABLE dbo.tblObjetivoOkr
        (
            IdObjetivoOkr   INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NULL,
            IdEquipo        INT                         NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Descripcion     NVARCHAR(500)               NULL,
            Anio            INT                         NOT NULL,
            Trimestre       TINYINT                     NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblObjetivoOkr_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblObjetivoOkr_Activo DEFAULT (1),
            CONSTRAINT PK_tblObjetivoOkr PRIMARY KEY (IdObjetivoOkr),
            CONSTRAINT FK_tblObjetivoOkr_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblObjetivoOkr_tblEquipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo),
            CONSTRAINT CK_tblObjetivoOkr_Trimestre CHECK (Trimestre BETWEEN 1 AND 4)
        )
        PRINT 'OK: tblObjetivoOkr creada'
    END
    ELSE PRINT 'SKIP: tblObjetivoOkr ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblResultadoClave')
    BEGIN
        CREATE TABLE dbo.tblResultadoClave
        (
            IdResultadoClave INT           IDENTITY(1,1) NOT NULL,
            IdObjetivoOkr    INT                         NOT NULL,
            Nombre           NVARCHAR(200)               NOT NULL,
            ValorMeta        DECIMAL(18,4)               NOT NULL,
            ValorActual      DECIMAL(18,4)               NOT NULL CONSTRAINT DF_tblResultadoClave_ValorActual DEFAULT (0),
            ClaveKpi         NVARCHAR(100)               NULL,   -- vinculo opcional a tblKpiDefinicion.Clave (script 07)
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblResultadoClave_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            UsuarioMovto     NVARCHAR(50)                NULL,
            FechaMovto       DATETIME                    NULL,
            Activo           BIT                         NOT NULL CONSTRAINT DF_tblResultadoClave_Activo DEFAULT (1),
            CONSTRAINT PK_tblResultadoClave PRIMARY KEY (IdResultadoClave),
            CONSTRAINT FK_tblResultadoClave_tblObjetivoOkr FOREIGN KEY (IdObjetivoOkr) REFERENCES dbo.tblObjetivoOkr (IdObjetivoOkr)
        )
        PRINT 'OK: tblResultadoClave creada'
    END
    ELSE PRINT 'SKIP: tblResultadoClave ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTarifaNivel')
    BEGIN
        CREATE TABLE dbo.tblTarifaNivel
        (
            IdTarifaNivel   INT           IDENTITY(1,1) NOT NULL,
            IdNivel         INT                         NOT NULL,
            CostoHora       DECIMAL(18,2)               NOT NULL,
            VigenciaDesde   DATE                        NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblTarifaNivel_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblTarifaNivel_Activo DEFAULT (1),
            CONSTRAINT PK_tblTarifaNivel PRIMARY KEY (IdTarifaNivel),
            CONSTRAINT UQ_tblTarifaNivel_NivelVigencia UNIQUE (IdNivel, VigenciaDesde),
            CONSTRAINT FK_tblTarifaNivel_tblNivel FOREIGN KEY (IdNivel) REFERENCES dbo.tblNivel (IdNivel),
            CONSTRAINT CK_tblTarifaNivel_CostoHora CHECK (CostoHora >= 0)
        )
        PRINT 'OK: tblTarifaNivel creada'
    END
    ELSE PRINT 'SKIP: tblTarifaNivel ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPresupuestoProyecto')
    BEGIN
        CREATE TABLE dbo.tblPresupuestoProyecto
        (
            IdPresupuestoProyecto INT           IDENTITY(1,1) NOT NULL,
            IdProyecto            INT                         NOT NULL,
            Anio                  INT                         NOT NULL,
            MontoAutorizado       DECIMAL(18,2)               NOT NULL CONSTRAINT DF_tblPresupuestoProyecto_MontoAutorizado DEFAULT (0),
            HorasAutorizadas      DECIMAL(18,2)               NOT NULL CONSTRAINT DF_tblPresupuestoProyecto_HorasAutorizadas DEFAULT (0),
            FechaRegistro         DATETIME2                   NOT NULL CONSTRAINT DF_tblPresupuestoProyecto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro       NVARCHAR(200)               NOT NULL,
            UsuarioMovto          NVARCHAR(50)                NULL,
            FechaMovto            DATETIME                    NULL,
            Activo                BIT                         NOT NULL CONSTRAINT DF_tblPresupuestoProyecto_Activo DEFAULT (1),
            CONSTRAINT PK_tblPresupuestoProyecto PRIMARY KEY (IdPresupuestoProyecto),
            CONSTRAINT UQ_tblPresupuestoProyecto_ProyectoAnio UNIQUE (IdProyecto, Anio),
            CONSTRAINT FK_tblPresupuestoProyecto_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto)
        )
        PRINT 'OK: tblPresupuestoProyecto creada'
    END
    ELSE PRINT 'SKIP: tblPresupuestoProyecto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblAmbiente')
    BEGIN
        CREATE TABLE dbo.tblAmbiente
        (
            IdAmbiente      INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NULL,   -- NULL = ambiente global
            Nombre          NVARCHAR(100)               NOT NULL,
            Url             NVARCHAR(500)               NULL,
            Servidor        NVARCHAR(200)               NULL,
            BaseDatos       NVARCHAR(200)               NULL,
            IdResponsable   INT                         NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblAmbiente_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblAmbiente_Activo DEFAULT (1),
            CONSTRAINT PK_tblAmbiente PRIMARY KEY (IdAmbiente),
            CONSTRAINT UQ_tblAmbiente_NombreProyecto UNIQUE (Nombre, IdProyecto),
            CONSTRAINT FK_tblAmbiente_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT FK_tblAmbiente_tblUsuario FOREIGN KEY (IdResponsable) REFERENCES dbo.tblUsuario (IdUsuario)
        )
        PRINT 'OK: tblAmbiente creada'
    END
    ELSE PRINT 'SKIP: tblAmbiente ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRepositorio')
    BEGIN
        CREATE TABLE dbo.tblRepositorio
        (
            IdRepositorio   INT           IDENTITY(1,1) NOT NULL,
            IdProyecto      INT                         NOT NULL,
            Nombre          NVARCHAR(200)               NOT NULL,
            Url             NVARCHAR(500)               NOT NULL,
            SecretoWebhook  NVARCHAR(200)               NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblRepositorio_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblRepositorio_Activo DEFAULT (1),
            CONSTRAINT PK_tblRepositorio PRIMARY KEY (IdRepositorio),
            CONSTRAINT UQ_tblRepositorio_Url UNIQUE (Url),
            CONSTRAINT FK_tblRepositorio_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto)
        )
        PRINT 'OK: tblRepositorio creada'
    END
    ELSE PRINT 'SKIP: tblRepositorio ya existe'

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

/* ---------- Parte 2: Seeds (ambientes globales) ---------- */
SET XACT_ABORT ON
BEGIN TRANSACTION
BEGIN TRY

    INSERT INTO dbo.tblAmbiente (IdProyecto, Nombre, UsuarioRegistro)
    SELECT NULL, v.Nombre, N'script-despliegue'
    FROM (VALUES (N'DEV'),(N'QA'),(N'PREPROD'),(N'PROD')) v(Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblAmbiente a WHERE a.Nombre = v.Nombre AND a.IdProyecto IS NULL)
    PRINT 'OK: ambientes globales DEV/QA/PREPROD/PROD'

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
