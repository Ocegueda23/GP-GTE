USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      15_2026-08-02_INSERT_bdsGTE_PermisoAprobarPruebas.sql
   Autor:       Equipo GTE
   Descripcion: Reglas de negocio pedidas 2026-08-02 sobre el mini-flujo de
                QA que ya vive en el estatus del WorkItem (En Pruebas ->
                TERMINAR/RECHAZAR_QA):
                - Nuevo permiso WI.AprobarPruebas, sembrado para el rol QA
                  (Administrador ya lo tiene por el seed general de todos
                  los permisos).
                - tblTransicionConfig: las transiciones TERMINAR y
                  RECHAZAR_QA con IdEstatusOrigen = En Pruebas (3) ahora
                  exigen WI.AprobarPruebas (antes NULL, cualquiera podia
                  aprobar/rechazar). El TERMINAR desde En Proceso (2, la
                  ruta "proyectos sin fase QA") NO se toca, sigue libre.
                Autoaprobacion (no aprobar/rechazar el propio elemento) y
                rechazo-exige-hallazgo se validan en codigo
                (CambiarEstatusWorkItemHandler.ValidarRevisionPruebasAsync),
                no aqui -- este script solo cubre el permiso.
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) y 07/09
                (tblTransicionConfig, transiciones de WorkItem) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'WI.AprobarPruebas', N'Requerimientos', N'Aprobar o rechazar la fase de pruebas de un WorkItem (En Pruebas)')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso WI.AprobarPruebas sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'QA'
      AND p.Clave = N'WI.AprobarPruebas'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permiso WI.AprobarPruebas asignado al rol QA'

    UPDATE dbo.tblTransicionConfig
        SET RequierePermiso = N'WI.AprobarPruebas'
    WHERE Proceso = N'WorkItem'
      AND IdEstatusOrigen = 3
      AND Accion IN (N'TERMINAR', N'RECHAZAR_QA')
      AND (RequierePermiso IS NULL OR RequierePermiso <> N'WI.AprobarPruebas')
    PRINT 'OK: tblTransicionConfig actualizada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas) -- TERMINAR/RECHAZAR_QA desde En Pruebas exigen WI.AprobarPruebas'

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
