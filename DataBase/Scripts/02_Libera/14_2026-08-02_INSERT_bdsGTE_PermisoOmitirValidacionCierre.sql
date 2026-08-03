USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      14_2026-08-02_INSERT_bdsGTE_PermisoOmitirValidacionCierre.sql
   Autor:       Equipo GTE
   Descripcion: Nuevo permiso WI.OmitirValidacionCierre: bypass ACOTADO
                (decision del equipo, 2026-08-02) para que el rol
                Administrador pueda terminar un WorkItem y cambiar su
                estatus sin las validaciones normales de cierre (RN-REQ-03:
                hallazgos pendientes, avance registrado) ni el gate de
                ownership (WI.ModificarAjeno, que Administrador ya tiene
                por el seed general). El resto de reglas de negocio (ej.
                RN-REQ-01 una sola tarea En Proceso por persona) NO se
                saltan -- ver CambiarEstatusWorkItemHandler en el backend.
                Sigue el mismo patron RBAC data-driven que el resto del
                sistema (RN-ADM-02: sin cortocircuitos de codigo por rol):
                es un permiso mas, sembrado solo para Administrador, no una
                excepcion especial en el motor de estatus.
   Requiere:    01-02 aplicados (tblRol, tblPermiso, tblRolPermiso).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'WI.OmitirValidacionCierre', N'Requerimientos', N'Terminar WorkItems sin las validaciones normales de cierre')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso WI.OmitirValidacionCierre sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'WI.OmitirValidacionCierre'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permiso WI.OmitirValidacionCierre asignado al rol Administrador'

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
