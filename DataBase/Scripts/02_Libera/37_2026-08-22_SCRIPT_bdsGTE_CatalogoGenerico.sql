USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      37_2026-08-22_SCRIPT_bdsGTE_CatalogoGenerico.sql
   Autor:       Equipo GTE
   Descripcion: Motor CRUD administrativo generico ("Catalogos"): permite
                administrar tablas simples de bdsGTE sin escribir un
                Controller/Command/Query/pantalla nuevos por cada una.
                - tblCatalogoGenerico: registro de que tabla real de bdsGTE
                  se administra bajo que clave/titulo (una fila = una
                  pantalla de catalogo).
                - tblCatalogoGenericoColumna: configuracion de presentacion
                  por columna (nombre visible, visibilidad, solo-lectura,
                  requerido, orden, combo FK, cifrado, auto-fecha/auto-
                  usuario). El esquema real (tipo, nulabilidad, PK) SIEMPRE
                  se lee en vivo de INFORMATION_SCHEMA, nunca se duplica aqui.
                - Permiso ADM.CatalogoGenerico: protege el alta de catalogos
                  nuevos y la edicion de su configuracion de columnas. Los
                  permisos CAT.<CLAVE>.Ver/Crear/Editar/Eliminar se siembran
                  en runtime (por CrearCatalogoCommand) al dar de alta cada
                  catalogo, no aqui.
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    /* ---------- 1. tblCatalogoGenerico ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCatalogoGenerico')
    BEGIN
        CREATE TABLE dbo.tblCatalogoGenerico
        (
            IdCatalogo      INT           IDENTITY(1,1) NOT NULL,
            Clave           NVARCHAR(50)                NOT NULL,
            NombreTabla     NVARCHAR(128)               NOT NULL,
            Titulo          NVARCHAR(200)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblCatalogoGenerico_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)                NOT NULL,
            UsuarioMovto    NVARCHAR(200)                NULL,
            FechaMovto      DATETIME2                    NULL,
            Activo          BIT                          NOT NULL CONSTRAINT DF_tblCatalogoGenerico_Activo DEFAULT (1),
            CONSTRAINT PK_tblCatalogoGenerico PRIMARY KEY (IdCatalogo)
        )
        CREATE UNIQUE INDEX UQ_tblCatalogoGenerico_Clave ON dbo.tblCatalogoGenerico (Clave) WHERE Activo = 1
        PRINT 'OK: tblCatalogoGenerico creada'
    END
    ELSE PRINT 'SKIP: tblCatalogoGenerico ya existe'

    /* ---------- 2. tblCatalogoGenericoColumna ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCatalogoGenericoColumna')
    BEGIN
        CREATE TABLE dbo.tblCatalogoGenericoColumna
        (
            IdColumna           INT           IDENTITY(1,1) NOT NULL,
            IdCatalogo          INT                         NOT NULL,
            NombreColumna       NVARCHAR(128)               NOT NULL,
            DisplayName         NVARCHAR(200)               NOT NULL,
            EsVisible           BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_EsVisible DEFAULT (1),
            EsSoloLectura       BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_EsSoloLectura DEFAULT (0),
            EsRequerido         BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_EsRequerido DEFAULT (0),
            OrdinalPos          INT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_OrdinalPos DEFAULT (0),
            TablaFk             NVARCHAR(128)               NULL,
            ColumnaClaveFk      NVARCHAR(128)               NULL,
            ColumnaMostrarFk    NVARCHAR(128)               NULL,
            EsCifrado           BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_EsCifrado DEFAULT (0),
            AutoFechaAlta       BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_AutoFechaAlta DEFAULT (0),
            AutoFechaEdicion    BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_AutoFechaEdicion DEFAULT (0),
            AutoUsuarioAlta     BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_AutoUsuarioAlta DEFAULT (0),
            AutoUsuarioEdicion  BIT                         NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_AutoUsuarioEdicion DEFAULT (0),
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)                NOT NULL,
            UsuarioMovto        NVARCHAR(200)                NULL,
            FechaMovto          DATETIME2                    NULL,
            Activo              BIT                          NOT NULL CONSTRAINT DF_tblCatalogoGenericoColumna_Activo DEFAULT (1),
            CONSTRAINT PK_tblCatalogoGenericoColumna PRIMARY KEY (IdColumna),
            CONSTRAINT FK_tblCatalogoGenericoColumna_tblCatalogoGenerico FOREIGN KEY (IdCatalogo) REFERENCES dbo.tblCatalogoGenerico (IdCatalogo)
        )
        CREATE UNIQUE INDEX UQ_tblCatalogoGenericoColumna_CatalogoColumna ON dbo.tblCatalogoGenericoColumna (IdCatalogo, NombreColumna) WHERE Activo = 1
        PRINT 'OK: tblCatalogoGenericoColumna creada'
    END
    ELSE PRINT 'SKIP: tblCatalogoGenericoColumna ya existe'

    /* ---------- 3. Permiso ADM.CatalogoGenerico (alta de catalogos + config de columnas) ---------- */
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'ADM.CatalogoGenerico', N'Administracion', N'Dar de alta catalogos genericos nuevos y configurar sus columnas')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso ADM.CatalogoGenerico sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'ADM.CatalogoGenerico'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permiso ADM.CatalogoGenerico asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
