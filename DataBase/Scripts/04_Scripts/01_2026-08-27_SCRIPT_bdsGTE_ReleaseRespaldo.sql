USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-27_SCRIPT_bdsGTE_ReleaseRespaldo.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Respaldos previos al despliegue de un release.

                La Solicitud de despliegue (formato que se manda a
                firmar) tiene que decir explicitamente QUE se respalda
                antes de tocar produccion: cuales bases de datos, que
                servicios, que sitios y que carpetas o ubicaciones
                especificas. Antes eso viajaba, cuando viajaba, dentro
                del texto libre del instructivo, asi que no habia forma
                de exigirlo ni de imprimirlo como apartado propio.

                dbo.tblTipoRespaldo es un catalogo enumerado de ID fijo
                (sin IDENTITY): los IDs los referencia el backend.
                dbo.tblReleaseRespaldo guarda un renglon por respaldo,
                con el tipo y la descripcion o ubicacion exacta.

                La regla "al menos un respaldo" NO se pone como
                constraint: se valida en el backend al SOLICITAR
                APROBACION, junto con las demas reglas del gate
                (RN-GTE-025 y RN-GTE-032). Un CHECK a nivel tabla
                impediria crear el release antes de saber que se va a
                desplegar.
   Requiere:    tblRelease.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTipoRespaldo'
    )
    BEGIN
        CREATE TABLE dbo.tblTipoRespaldo
        (
            Id              INT           NOT NULL,
            Nombre          NVARCHAR(100) NOT NULL,
            Orden           INT           NOT NULL CONSTRAINT DF_tblTipoRespaldo_Orden  DEFAULT (0),
            Activo          BIT           NOT NULL CONSTRAINT DF_tblTipoRespaldo_Activo DEFAULT (1),
            CONSTRAINT PK_tblTipoRespaldo PRIMARY KEY (Id),
            CONSTRAINT UQ_tblTipoRespaldo_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: dbo.tblTipoRespaldo creada'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: dbo.tblTipoRespaldo ya existe'
    END

    -- Catalogo fijo. Se ejecuta con EXEC para diferir la compilacion: la tabla puede
    -- haberse creado en este mismo batch (leccion de InterfloClaude seccion 12).
    EXEC(N'
        MERGE dbo.tblTipoRespaldo AS destino
        USING (VALUES
            (1, N''Base de datos'', 1),
            (2, N''Servicio'',      2),
            (3, N''Sitio web'',     3),
            (4, N''Ubicacion'',     4),
            (5, N''Otro'',          5)
        ) AS origen (Id, Nombre, Orden)
            ON destino.Id = origen.Id
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (Id, Nombre, Orden, Activo) VALUES (origen.Id, origen.Nombre, origen.Orden, 1);
    ')
    PRINT 'OK: catalogo dbo.tblTipoRespaldo sembrado'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblReleaseRespaldo'
    )
    BEGIN
        CREATE TABLE dbo.tblReleaseRespaldo
        (
            IdReleaseRespaldo INT           IDENTITY(1,1) NOT NULL,
            IdRelease         INT                         NOT NULL,
            IdTipoRespaldo    INT                         NOT NULL,
            Descripcion       NVARCHAR(500)               NOT NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblReleaseRespaldo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblReleaseRespaldo_Activo DEFAULT (1),
            CONSTRAINT PK_tblReleaseRespaldo PRIMARY KEY (IdReleaseRespaldo),
            CONSTRAINT FK_tblReleaseRespaldo_tblRelease
                FOREIGN KEY (IdRelease) REFERENCES dbo.tblRelease (IdRelease),
            CONSTRAINT FK_tblReleaseRespaldo_tblTipoRespaldo
                FOREIGN KEY (IdTipoRespaldo) REFERENCES dbo.tblTipoRespaldo (Id)
        )
        PRINT 'OK: dbo.tblReleaseRespaldo creada'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: dbo.tblReleaseRespaldo ya existe'
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_tblReleaseRespaldo_IdRelease'
          AND object_id = OBJECT_ID('dbo.tblReleaseRespaldo')
    )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_tblReleaseRespaldo_IdRelease
            ON dbo.tblReleaseRespaldo (IdRelease) INCLUDE (Activo)
        PRINT 'OK: IX_tblReleaseRespaldo_IdRelease creado'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: IX_tblReleaseRespaldo_IdRelease ya existe'
    END

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
