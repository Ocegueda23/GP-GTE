USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      36_2026-08-22_INSERT_bdsGTE_PermisosSprintYReversa.sql
   Autor:       Equipo GTE
   Descripcion: Pedido de negocio 2026-08-22 sobre Backlog/Sprint:
                1. El permiso PLA.GestionarSprints (crear/activar/cerrar)
                   se separa por accion: se queda solo para CREAR. Se
                   agregan PLA.CerrarSprint (cerrar un sprint Activo) y
                   PLA.CambiarEstatusSprint (activar un Planeado, o la
                   reversa nueva Activo -> Planeado). Editar datos del
                   sprint (nombre/objetivo/fechas) usa un permiso nuevo,
                   PLA.ModificarSprint, exigido por EditarSprintCommand
                   (nuevo endpoint PUT /sprints/{id}); un sprint Cerrado
                   no se puede editar (validado en codigo).
                   Sembrados solo para el rol Administrador (que ya los
                   recibe por el seed general); el equipo asigna a otros
                   roles despues desde Administracion > Roles si lo
                   decide (mismo patron que el script 23).
                2. Nueva transicion VOLVER_PLANEADO (Activo -> Planeado,
                   2 -> 1) del proceso Sprint: permite revertir un sprint
                   activado por error o que necesita replanearse, sin
                   pasar por Cerrar. Usa el mismo motor de estatus
                   (dbo.spCambiarEstatus, sin tocar el SP generico).
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso, tblProceso,
                tblTransicion) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'PLA.CerrarSprint',         N'Planeacion', N'Cerrar un sprint activo'),
        (N'PLA.CambiarEstatusSprint', N'Planeacion', N'Activar un sprint planeado o revertirlo a planeado'),
        (N'PLA.ModificarSprint',      N'Planeacion', N'Editar nombre, objetivo y fechas de un sprint no cerrado')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos PLA.CerrarSprint / PLA.CambiarEstatusSprint / PLA.ModificarSprint sembrados'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'PLA.CerrarSprint', N'PLA.CambiarEstatusSprint', N'PLA.ModificarSprint')
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos nuevos asignados al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblTransicion (IdProceso, IdEstatusOrigen, Accion, IdEstatusDestino, UsuarioRegistro)
    SELECT p.IdProceso, v.Origen, v.Accion, v.Destino, N'script-despliegue'
    FROM (VALUES
        (2, N'VOLVER_PLANEADO', 1)
        ) v(Origen, Accion, Destino)
    INNER JOIN dbo.tblProceso p ON p.Proceso = N'Sprint'
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicion t
                      WHERE t.IdProceso = p.IdProceso
                        AND t.IdEstatusOrigen = v.Origen
                        AND t.Accion = v.Accion)
    PRINT 'OK: transicion Sprint VOLVER_PLANEADO (Activo -> Planeado) registrada'

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
