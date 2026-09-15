USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-08-27_INSERT_bdsGTE_PermisoConocimientoEliminar.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Separa la baja de articulos de la base de conocimiento
                del permiso general de administracion.

                POR QUE: CON.Administrar cubria crear, editar y eliminar
                en un solo permiso, asi que cualquiera que pudiera
                redactar un articulo podia borrar los de los demas. Un
                articulo es memoria acumulada del equipo y su baja no se
                deshace desde la interfaz.

                CON.Eliminar se asigna SOLO al rol Administrador. Quien
                tenga CON.Administrar sigue creando y editando igual que
                antes; lo unico que pierde es la baja.
   Requiere:    tblPermiso, tblRol, tblRolPermiso.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'CON.Eliminar', N'Conocimiento',
         N'Dar de baja articulos de la base de conocimiento')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso CON.Eliminar sembrado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'CON.Eliminar'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: CON.Eliminar asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
