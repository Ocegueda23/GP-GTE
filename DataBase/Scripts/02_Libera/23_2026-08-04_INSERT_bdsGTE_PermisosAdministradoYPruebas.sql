USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      23_2026-08-04_INSERT_bdsGTE_PermisosAdministradoYPruebas.sql
   Autor:       Equipo GTE
   Descripcion: Tres permisos nuevos pedidos por el negocio 2026-08-04,
                sembrados solo para el rol Administrador (que ya los
                recibe por el seed general de todos los permisos); el
                equipo asigna a otros roles despues desde Administracion
                > Roles si lo decide, la matriz los toma automatico en
                cuanto existen en tblPermiso:

                - WI.CrearEnAdministrado: crear un WorkItem en un
                  proyecto marcado Administrado (tblProyecto.Administrado,
                  script 22). En proyectos NO administrados no se exige
                  (sin cambio de comportamiento).
                - WI.EliminarEnAdministrado: cancelar (= "eliminar", ver
                  WI.Eliminar ya existente en tblTransicionConfig para la
                  accion CANCELAR) un WorkItem en un proyecto Administrado.
                  Se exige ADEMAS de WI.Eliminar, no lo sustituye.
                - WI.SaltarPruebas: terminar un WorkItem directo desde
                  En Proceso (sin pasar por En Pruebas) cuando el proyecto
                  es categoria Desarrollo (tblCategoriaProyecto.Id = 1).
                  Categorias TI (2) y Mantenimiento (3) quedan libres,
                  igual que hoy.

                Los tres se validan en codigo (CrearWorkItemHandler /
                CambiarEstatusWorkItemHandler), no via
                tblTransicionConfig.RequierePermiso -- esa columna no
                puede expresar "solo si el proyecto es Administrado/
                categoria X", es una condicion de negocio sobre el
                proyecto, no del grafo de estatus.
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'WI.CrearEnAdministrado',    N'Requerimientos', N'Crear elementos de trabajo en un proyecto administrado'),
        (N'WI.EliminarEnAdministrado', N'Requerimientos', N'Eliminar elementos de trabajo en un proyecto administrado'),
        (N'WI.SaltarPruebas',          N'Requerimientos', N'Terminar un elemento de proyecto Desarrollo sin pasar por En Pruebas')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos WI.CrearEnAdministrado / WI.EliminarEnAdministrado / WI.SaltarPruebas sembrados'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'WI.CrearEnAdministrado', N'WI.EliminarEnAdministrado', N'WI.SaltarPruebas')
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos nuevos asignados al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
