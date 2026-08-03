USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      19_2026-08-03_SCRIPT_bdsGTE_CatalogosSoporteAdicionales.sql
   Autor:       Equipo GTE
   Descripcion: dbo.tblUsuarioSolicitante y dbo.tblLocacion se crearon
                manualmente en produccion (no via script), asi que un
                ambiente nuevo (LocalDB, preprod) no las tenia. Este
                script las crea SOLO SI NO EXISTEN -- en produccion es
                no-op total (las columnas ya existen desde los scripts
                17 y 18); en un ambiente fresco reproduce el esquema
                real verificado contra produccion (INFORMATION_SCHEMA.
                COLUMNS, 2026-08-03), bitacora incluida desde el alta.

                Nota de fidelidad (leccion de la seccion 5 general de
                InterfloClaude.md, "Drift fresh-CREATE vs legacy"):
                tblLocacion.Activo en produccion es BIT NULL sin
                default (no sigue el estandar NOT NULL DEFAULT 1) --
                se replica tal cual, no se "corrige" aqui.

                Supuesto no verificable desde aqui (las tablas viven
                solo en produccion): IDENTITY(1,1) en ambas PK, patron
                estandar de catalogo gestionado (InterfloClaude.md
                10.2 clase 3). Si en produccion NO es IDENTITY, avisar
                para corregir este script antes de usarlo en otro
                ambiente.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblUsuarioSolicitante'
    )
    BEGIN
        CREATE TABLE [dbo].[tblUsuarioSolicitante]
        (
            IdUsuarioSolicitante INT           IDENTITY(1,1) NOT NULL,
            Usuario              NVARCHAR(50)                NULL,
            Nombre               NVARCHAR(500)               NULL,
            Correo               NVARCHAR(150)               NULL,
            FechaRegistro        DATETIME2                   NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)                NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_UsuarioRegistro DEFAULT (N'script-despliegue'),
            UsuarioMovto         NVARCHAR(50)                 NULL,
            FechaMovto           DATETIME                     NULL,
            Activo               BIT                          NOT NULL CONSTRAINT DF_tblUsuarioSolicitante_Activo DEFAULT (1),
            CONSTRAINT PK_tblUsuarioSolicitante PRIMARY KEY (IdUsuarioSolicitante)
        )
        PRINT 'OK: tblUsuarioSolicitante creada (ambiente fresco)'
    END
    ELSE
        PRINT 'SKIP: tblUsuarioSolicitante ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblLocacion'
    )
    BEGIN
        CREATE TABLE [dbo].[tblLocacion]
        (
            IdLocacion      INT           IDENTITY(1,1) NOT NULL,
            Locacion        NVARCHAR(50)                NULL,
            Descripcion     NVARCHAR(150)               NULL,
            Activo          BIT                          NULL,
            FechaRegistro   DATETIME2                    NOT NULL CONSTRAINT DF_tblLocacion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)                NOT NULL CONSTRAINT DF_tblLocacion_UsuarioRegistro DEFAULT (N'script-despliegue'),
            UsuarioMovto    NVARCHAR(50)                 NULL,
            FechaMovto      DATETIME                     NULL,
            CONSTRAINT PK_tblLocacion PRIMARY KEY (IdLocacion)
        )
        PRINT 'OK: tblLocacion creada (ambiente fresco)'
    END
    ELSE
        PRINT 'SKIP: tblLocacion ya existe'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
