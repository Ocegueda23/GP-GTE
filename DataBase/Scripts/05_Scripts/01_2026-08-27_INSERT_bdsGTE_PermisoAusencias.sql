USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-27_INSERT_bdsGTE_PermisoAusencias.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Habilita el flujo de registro de ausencias (P.. Ausencias).

                Siembra el permiso ADM.Ausencias, que gobierna la bandeja
                de aprobacion: quien lo tenga aprueba o rechaza cualquier
                ausencia solicitada, ademas de poder cancelar la de otro.
                Registrar la propia ausencia y cancelarla NO exige permiso.

                POR QUE por permiso y no por jefe directo: el Documento
                Maestro (A12) preveia al jefe directo, pero la estructura
                de equipos no modela hoy una jefatura unica por persona
                (tblUsuarioRol admite varios equipos/proyectos), asi que
                resolver "el jefe" seria ambiguo. Decision del equipo:
                empezar por permiso; migrar a jefe directo mas adelante
                solo si el negocio lo pide.

                El grafo del proceso Ausencia (tblProceso/tblTransicion:
                APROBAR, RECHAZAR, CANCELAR desde Solicitada) ya quedo
                sembrado en el script 09 de 01_Libera; este script no lo
                toca.
   Requiere:    tblPermiso, tblRol, tblRolPermiso.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'ADM.Ausencias', N'Administracion',
         N'Aprobar o rechazar las ausencias solicitadas')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso ADM.Ausencias sembrado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'ADM.Ausencias'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: ADM.Ausencias asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
