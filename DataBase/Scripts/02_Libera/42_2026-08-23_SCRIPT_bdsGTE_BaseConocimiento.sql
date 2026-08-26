USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      42_2026-08-23_SCRIPT_bdsGTE_BaseConocimiento.sql
   Autor:       Equipo GTE
   Descripcion: Habilita el modulo Base de conocimiento (P23, Fase 4).
                Las tablas tblArticuloConocimiento y tblArticuloVersion
                ya existen desde el script 06 del despliegue inicial;
                este script agrega lo que faltaba para construir el
                modulo:

                1. Columna EsPublico BIT NOT NULL DEFAULT 0 en
                   tblArticuloConocimiento. Un articulo marcado como
                   publico se puede leer SIN iniciar sesion, por los
                   endpoints anonimos /api/v1/publico/conocimiento.
                   Arranca en 0 (privado) a proposito: exponer contenido
                   es una decision explicita por articulo, nunca el
                   default. OJO (trampa EF ya documentada en CLAUDE.md):
                   el DEFAULT de BD no aplica de forma confiable en los
                   INSERT de EF, asi que el codigo fija EsPublico
                   explicitamente en cada alta.

                2. Permiso CON.Administrar: crear, editar y dar de baja
                   articulos y terminos del glosario. La LECTURA interna
                   no exige permiso (P23 es "Todos" en el Documento
                   Maestro, seccion 5.1). Sembrado solo para el rol
                   Administrador; el equipo lo asigna a otros roles
                   despues desde Administracion > Roles (mismo patron
                   que los scripts 23 y 36).

                3. Indice de apoyo para el listado publico y el filtro
                   de glosario (la busqueda por texto sigue siendo un
                   scan; si crece el volumen se evalua Full-Text, fuera
                   de alcance de esta pasada).
   Requiere:    01, 02 y 06 (tblArticuloConocimiento, tblPermiso,
                tblRol, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblArticuloConocimiento' AND COLUMN_NAME = 'EsPublico'
    )
    BEGIN
        ALTER TABLE dbo.tblArticuloConocimiento
            ADD EsPublico BIT NOT NULL CONSTRAINT DF_tblArticuloConocimiento_EsPublico DEFAULT (0)
        PRINT 'OK: tblArticuloConocimiento.EsPublico agregada -> BIT NOT NULL DEFAULT 0'
    END
    ELSE
        PRINT 'SKIP: tblArticuloConocimiento.EsPublico ya existe'

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'CON.Administrar', N'Conocimiento',
         N'Crear, editar y dar de baja articulos y terminos de la base de conocimiento')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso CON.Administrar sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'CON.Administrar'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: CON.Administrar asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_tblArticuloConocimiento_PublicoGlosario'
          AND object_id = OBJECT_ID('dbo.tblArticuloConocimiento')
    )
    BEGIN
        CREATE INDEX IX_tblArticuloConocimiento_PublicoGlosario
            ON dbo.tblArticuloConocimiento (Activo, EsPublico, EsGlosario)
            INCLUDE (Titulo)
        PRINT 'OK: IX_tblArticuloConocimiento_PublicoGlosario creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblArticuloConocimiento_PublicoGlosario ya existe'

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
