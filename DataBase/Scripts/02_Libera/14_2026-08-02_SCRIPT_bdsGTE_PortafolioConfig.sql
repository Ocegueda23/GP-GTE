USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
/* =====================================================================
   Script:      14_2026-08-02_SCRIPT_bdsGTE_PortafolioConfig.sql
   Autor:       Equipo GTE
   Descripcion: Modulo Portafolio (A5) -- Costeo real y OKRs. Las tablas
                (tblTarifaNivel, tblPresupuestoProyecto, tblObjetivoOkr,
                tblResultadoClave) ya existen desde el despliegue inicial
                (03_2026-07-30_SCRIPT_bdsGTE_Portafolio.sql), sin permiso
                ni objeto programable todavia.
                Parte 1 (transaccional): permisos POR.GestionarCosteo y
                POR.GestionarOkr + autogrant al rol Administrador.
                Parte 2 (batch aparte, patron 08_..._Programables.sql):
                vista vwCostoRegistroTiempo -- costo real por registro de
                tiempo, resolviendo la tarifa vigente del nivel del
                usuario a la fecha del registro con OUTER APPLY (no existe
                columna VigenciaHasta: vigente = mayor VigenciaDesde <=
                fecha). Documento Maestro seccion 2.4: el costo nunca se
                almacena duplicado, se calcula siempre de
                tblRegistroTiempo.Minutos / 60 * tarifa vigente.
   Requiere:    01-13 aplicados (en particular 02, 03).
   ===================================================================== */

/* ---------- Parte 1: permisos ---------- */
SET XACT_ABORT ON
BEGIN TRANSACTION
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'POR.GestionarCosteo', N'Portafolio', N'Gestionar tarifas por nivel, presupuesto y reporte de costo'),
        (N'POR.GestionarOkr',    N'Portafolio', N'Gestionar objetivos y resultados clave (OKR)')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos POR.* sembrados'

    /* El rol Administrador recibe todos los permisos (mismo patron del script 02) */
    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'POR.GestionarCosteo', N'POR.GestionarOkr')
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos POR.* otorgados al rol Administrador'

    COMMIT TRANSACTION
    PRINT '===== Parte 1 (permisos) ejecutada correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK (Parte 1) ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO

/* ---------- Parte 2: vwCostoRegistroTiempo ---------- */
CREATE OR ALTER VIEW dbo.vwCostoRegistroTiempo
AS
SELECT rt.IdRegistroTiempo,
       wi.IdProyecto,
       rt.IdUsuario,
       rt.Fecha,
       rt.Minutos,
       tn.CostoHora,
       CAST(rt.Minutos AS DECIMAL(18,4)) / 60.0 * ISNULL(tn.CostoHora, 0) AS Costo
FROM dbo.tblRegistroTiempo rt
INNER JOIN dbo.tblWorkItem wi ON wi.IdWorkItem = rt.IdWorkItem
INNER JOIN dbo.tblUsuario u ON u.IdUsuario = rt.IdUsuario
OUTER APPLY (
    SELECT TOP (1) t.CostoHora
    FROM dbo.tblTarifaNivel t
    WHERE t.IdNivel = u.IdNivel
      AND t.Activo = 1
      AND t.VigenciaDesde <= rt.Fecha
    ORDER BY t.VigenciaDesde DESC
) tn
WHERE rt.Activo = 1
GO
PRINT 'OK: vwCostoRegistroTiempo'
GO
PRINT '===== Script ejecutado correctamente ====='
GO
