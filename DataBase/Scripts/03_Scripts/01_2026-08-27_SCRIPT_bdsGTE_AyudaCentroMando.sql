USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-08-27_SCRIPT_bdsGTE_AyudaCentroMando.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Siembra el permiso AYU.CentroMando, que habilita el
                documento "Centro de Mando TI" dentro de la seccion de
                Ayuda.

                A diferencia del Manual de usuario (estatico en
                wwwroot, para todos), este documento describe el modelo
                con el que se evalua el desempeno de cada responsable de
                area, asi que NO es para todos: se sirve por el endpoint
                autenticado GET /api/v1/ayuda/centro-mando-ti, cuyo
                handler exige este permiso. El archivo HTML vive junto al
                binario (Contenido/Ayuda), no en wwwroot, justamente para
                que no sea legible por quien adivine la URL.

                Se siembra solo para el rol Administrador (mismo patron
                de los scripts 23, 36 y 42). Si el equipo decide que el
                gerente de TI u otro rol tambien deben leerlo, se asigna
                despues desde Administracion > Roles, sin tocar codigo.
   Requiere:    01 y 02 (tblPermiso, tblRol, tblRolPermiso) aplicados.
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'AYU.CentroMando', N'Ayuda',
         N'Leer el Centro de Mando TI: modelo de indicadores y evaluacion de los responsables de area')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso AYU.CentroMando sembrado'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave = N'AYU.CentroMando'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: AYU.CentroMando asignado al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
