USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      31_2026-08-13_INSERT_bdsGTE_PermisosReportes.sql
   Autor:       Equipo GTE
   Descripcion: Permisos nuevos para el catalogo de reportes R01-R14
                (Doctos/GTE-DocumentoMaestro.md seccion 13):

                - RPT.Ver: catalogo general (Productividad, Horas
                  registradas, Retrabajo, Bugs y defectos, Releases,
                  Riesgos, Solicitantes, SLA, KPIs/DORA, Carga de
                  trabajo, Flujo/CFD).
                - RPT.Auditoria: R14 (bitacora tblBitacora), sensible,
                  solo Administrador.

                RPT.Costos ya estaba sembrado (script 02) sin
                consumidor -- ahora lo usan R08 Costos y R09
                Rentabilidad, no requiere script nuevo.

                Sembrados solo para el rol Administrador (que ya los
                recibe por el seed general de todos los permisos); el
                equipo asigna a otros roles despues desde
                Administracion > Roles si lo decide, la matriz los
                toma automatico (mismo patron que los scripts 23/26/29).
   Requiere:    01, 02 (tblRol, tblPermiso, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'RPT.Ver', N'Indicadores', N'Ver el catalogo general de reportes (Productividad, Horas, Retrabajo, Bugs, Releases, Riesgos, Solicitantes, SLA, KPIs, Carga de trabajo, Flujo)'),
        (N'RPT.Auditoria', N'Indicadores', N'Ver el reporte de auditoria (bitacora de movimientos por usuario/entidad/rango)')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos RPT.Ver / RPT.Auditoria sembrados (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'RPT.Ver', N'RPT.Auditoria')
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
