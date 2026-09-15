USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-28_INSERT_bdsGTE_PermisoAprobarPropio.sql
   Autor:       Equipo GTE
   Descripcion: Nuevo permiso WI.AprobarPropio: levanta de forma ACOTADA la
                regla de que nadie aprueba ni rechaza las pruebas de su
                propio elemento (autoaprobacion).

                Antes esa regla solo se podia saltar con
                WI.OmitirValidacionCierre, que es un martillo: omite ademas
                el gate de cierre RN-GTE-010 y esta pensado solo para
                Administrador. Quien necesitaba revisar su propio trabajo
                (equipo de una sola persona, o sin segundo revisor
                disponible) tenia que recibir ese bypass completo.

                Se exige ADEMAS de WI.AprobarPruebas, no en su lugar: sigue
                haciendo falta ser quien puede aprobar la fase de pruebas.

                NO se asigna a ningun rol a proposito. Levantar el control
                de "cuatro ojos" es una decision de negocio, no un default:
                asignalo desde Administracion / Roles a quien lo necesite.
                Mientras nadie lo tenga, el comportamiento es el de siempre.
   Requiere:    01-02 aplicados (tblRol, tblPermiso, tblRolPermiso).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'WI.AprobarPropio', N'Requerimientos',
         N'Aprobar o rechazar las pruebas de su propio elemento (levanta la regla de autoaprobacion)')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso WI.AprobarPropio sembrado (sin asignar a ningun rol)'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
    PRINT 'Asigna WI.AprobarPropio desde Administracion / Roles a quien deba'
    PRINT 'poder revisar su propio trabajo. Sin asignarlo, nada cambia.'
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
