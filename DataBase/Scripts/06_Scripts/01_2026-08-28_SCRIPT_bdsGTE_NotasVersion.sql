USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-28_SCRIPT_bdsGTE_NotasVersion.sql
   Autor:       Equipo GTE
   Descripcion: Notas de version visibles para el usuario final. Hasta hoy
                el historial de lo liberado se escribia a mano al pie de
                Directory.Build.props, lo cual rompia el XML que lee
                publicar.bat (un comentario XML no admite dos guiones
                seguidos) y ademas nunca llegaba al usuario.

                Estructura padre/hijo:
                  tblNotaVersion       una fila por version liberada
                  tblNotaVersionDetalle  los renglones de esa version,
                                       clasificados por tipo de cambio.

                El tipo de cambio no es decorativo: es el mismo eje del
                estandar de versionado de Interflo (Proyecto.Mejora.Defecto),
                asi que el detalle deja ver POR QUE subio el digito que subio.

                Solo las notas con Publicada = 1 se muestran al usuario;
                mientras se redactan viven como borrador.
   Requiere:    01-02 aplicados (tblRol, tblPermiso, tblRolPermiso).
   ===================================================================== */
BEGIN TRY

    /* ---------- Catalogo de tipo de cambio (enumerado de ID fijo) ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblTipoCambioVersion')
    BEGIN
        CREATE TABLE [dbo].[tblTipoCambioVersion]
        (
            IdTipoCambioVersion INT           NOT NULL,
            Nombre              NVARCHAR(100) NOT NULL,
            Orden               INT           NOT NULL,
            FechaRegistro       DATETIME2     NOT NULL CONSTRAINT DF_tblTipoCambioVersion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200) NOT NULL,
            UsuarioMovto        NVARCHAR(50)  NULL,
            FechaMovto          DATETIME      NULL,
            Activo              BIT           NOT NULL CONSTRAINT DF_tblTipoCambioVersion_Activo DEFAULT (1),
            CONSTRAINT PK_tblTipoCambioVersion PRIMARY KEY (IdTipoCambioVersion)
        )
        PRINT 'OK: tblTipoCambioVersion creada correctamente'
    END
    ELSE
        PRINT 'SKIP: tblTipoCambioVersion ya existe'

    /* IDs fijos: los consume el backend como constantes, no se renumeran. */
    INSERT INTO dbo.tblTipoCambioVersion (IdTipoCambioVersion, Nombre, Orden, UsuarioRegistro)
    SELECT v.Id, v.Nombre, v.Orden, N'script-despliegue'
    FROM (VALUES
        (1, N'Proyecto', 1),
        (2, N'Mejora',   2),
        (3, N'Defecto',  3)
        ) v(Id, Nombre, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoCambioVersion t WHERE t.IdTipoCambioVersion = v.Id)
    PRINT 'OK: catalogo tblTipoCambioVersion sembrado'

    /* ---------- Encabezado de la nota ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblNotaVersion')
    BEGIN
        CREATE TABLE [dbo].[tblNotaVersion]
        (
            IdNotaVersion    INT           IDENTITY(1,1) NOT NULL,
            /* Mismo numero que estampa publicar.bat desde Directory.Build.props. */
            Version          NVARCHAR(20)                NOT NULL,
            FechaLiberacion  DATE                        NOT NULL,
            /* Frase corta que encabeza la version; el detalle va en la tabla hija. */
            Resumen          NVARCHAR(500)               NULL,
            /* 0 = borrador en redaccion, 1 = visible para el usuario final. */
            Publicada        BIT                         NOT NULL CONSTRAINT DF_tblNotaVersion_Publicada DEFAULT (0),
            FechaRegistro    DATETIME2                   NOT NULL CONSTRAINT DF_tblNotaVersion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro  NVARCHAR(200)               NOT NULL,
            UsuarioMovto     NVARCHAR(50)                NULL,
            FechaMovto       DATETIME                    NULL,
            Activo           BIT                         NOT NULL CONSTRAINT DF_tblNotaVersion_Activo DEFAULT (1),
            CONSTRAINT PK_tblNotaVersion PRIMARY KEY (IdNotaVersion),
            CONSTRAINT UQ_tblNotaVersion_Version UNIQUE (Version)
        )
        PRINT 'OK: tblNotaVersion creada correctamente'
    END
    ELSE
        PRINT 'SKIP: tblNotaVersion ya existe'

    /* ---------- Renglones de la nota ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblNotaVersionDetalle')
    BEGIN
        CREATE TABLE [dbo].[tblNotaVersionDetalle]
        (
            IdNotaVersionDetalle INT           IDENTITY(1,1) NOT NULL,
            IdNotaVersion        INT                         NOT NULL,
            IdTipoCambioVersion  INT                         NOT NULL,
            /* Modulo al que pertenece el cambio (WorkItems, Releases...), texto libre
               porque el menu se reagrupa seguido y no queremos una FK que se rompa. */
            Modulo               NVARCHAR(100)               NULL,
            /* Lo que el usuario puede hacer ahora, redactado para el usuario final. */
            Descripcion          NVARCHAR(500)               NOT NULL,
            Orden                INT                         NOT NULL CONSTRAINT DF_tblNotaVersionDetalle_Orden DEFAULT (0),
            FechaRegistro        DATETIME2                   NOT NULL CONSTRAINT DF_tblNotaVersionDetalle_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)               NOT NULL,
            UsuarioMovto         NVARCHAR(50)                NULL,
            FechaMovto           DATETIME                    NULL,
            Activo               BIT                         NOT NULL CONSTRAINT DF_tblNotaVersionDetalle_Activo DEFAULT (1),
            CONSTRAINT PK_tblNotaVersionDetalle PRIMARY KEY (IdNotaVersionDetalle),
            CONSTRAINT FK_tblNotaVersionDetalle_tblNotaVersion
                FOREIGN KEY (IdNotaVersion) REFERENCES dbo.tblNotaVersion (IdNotaVersion),
            CONSTRAINT FK_tblNotaVersionDetalle_tblTipoCambioVersion
                FOREIGN KEY (IdTipoCambioVersion) REFERENCES dbo.tblTipoCambioVersion (IdTipoCambioVersion)
        )
        PRINT 'OK: tblNotaVersionDetalle creada correctamente'
    END
    ELSE
        PRINT 'SKIP: tblNotaVersionDetalle ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.indexes
                   WHERE name = 'IX_tblNotaVersionDetalle_IdNotaVersion'
                     AND object_id = OBJECT_ID('dbo.tblNotaVersionDetalle'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_tblNotaVersionDetalle_IdNotaVersion
            ON dbo.tblNotaVersionDetalle (IdNotaVersion)
        PRINT 'OK: IX_tblNotaVersionDetalle_IdNotaVersion creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblNotaVersionDetalle_IdNotaVersion ya existe'

    /* ---------- Permiso ----------
       Solo se necesita permiso para ADMINISTRAR las notas. Leer las notas ya
       publicadas no lleva permiso: cualquier usuario autenticado debe poder ver
       que trae la version que esta usando. */
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'ADM.NotasVersion', N'Administracion', N'Redactar y publicar las notas de version que ve el usuario')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso ADM.NotasVersion sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'ADM.NotasVersion'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permiso ADM.NotasVersion asignado al rol Administrador'

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
