USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      29_2026-08-12_INSERT_bdsGTE_PermisoActividadUsuarios.sql
   Autor:       Equipo GTE
   Descripcion: Permiso nuevo para el reporte de actividad diaria por
                usuario (seccion Reportes, menu nuevo): ver, para un
                usuario y rango de fechas, el detalle de tblRegistroTiempo
                (folio/titulo del work item, minutos, descripcion) con el
                total de horas reales trabajadas. Pensado para
                administrador/gerente, no para el usuario dueno del dato
                (eso ya lo cubre Mi dia).

                - RPT.Actividad: ver el reporte de actividad diaria de
                  cualquier usuario en un rango de fechas.

                Sembrado solo para el rol Administrador (que ya lo recibe
                por el seed general de todos los permisos); el equipo
                asigna a otros roles despues desde Administracion > Roles
                si lo decide, la matriz los toma automatico en cuanto
                existen en tblPermiso (mismo patron que los scripts 23 y 26).
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'RPT.Actividad', N'Indicadores', N'Ver el reporte de actividad diaria y horas reales de los usuarios')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso RPT.Actividad sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'RPT.Actividad'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permiso nuevo asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
