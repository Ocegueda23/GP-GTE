USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-31_INSERT_bdsGTE_PermisoAutorizarRelease.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Permiso REL.Autorizar: autoriza un release saltandose la
                cadena de firmas. Se siembra aparte de REL.Aprobar a
                proposito -- firmar una aprobacion y dispensar todas las
                firmas son accesos distintos, y quien puede lo segundo
                tiene que poder nombrarse por separado en el rol.
                Solo se asigna al rol Administrador; el resto de los roles
                se configura a mano en Admin > Roles.
   Requiere:    Tanda 01 (tblPermiso, tblRol, tblRolPermiso).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'REL.Autorizar', N'Releases', N'Autorizar un release omitiendo las firmas')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso REL.Autorizar sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'REL.Autorizar'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: REL.Autorizar asignado al rol Administrador'

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
