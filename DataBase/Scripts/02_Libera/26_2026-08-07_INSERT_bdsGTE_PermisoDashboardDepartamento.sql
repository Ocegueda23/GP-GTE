USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      26_2026-08-07_INSERT_bdsGTE_PermisoDashboardDepartamento.sql
   Autor:       Equipo GTE
   Descripcion: Permiso nuevo para el Dashboard Ejecutivo de Metricas
                (modulo nuevo, ver Doctos/GTE-DocumentoMaestro.md 3.10):

                - DASH.VerDepartamento: ver el dashboard con alcance de
                  TODOS los usuarios del area/departamento propio (rol
                  tipo Gerente). Complementa a DASH.Ejecutivo (ya sembrado
                  en el script 02, alcance TODO el sistema, rol tipo
                  Director/Ejecutivo) que no tenia ningun consumidor
                  todavia. Sin este permiso ni DASH.Ejecutivo, el
                  dashboard resuelve el alcance por datos (lider de
                  equipo/jefe directo via tblUsuario.IdJefe, o el propio
                  usuario si no lidera a nadie) -- no requiere permiso
                  adicional, ver DashboardQueryService.

                Sembrado solo para el rol Administrador (que ya lo recibe
                por el seed general de todos los permisos); el equipo
                asigna a otros roles despues desde Administracion > Roles
                si lo decide, la matriz los toma automatico en cuanto
                existen en tblPermiso (mismo patron que el script 23).
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'DASH.VerDepartamento', N'Indicadores', N'Ver dashboard ejecutivo del area/departamento propio')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso DASH.VerDepartamento sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'DASH.VerDepartamento'
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
