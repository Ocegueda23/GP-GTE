USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-08-27_ALTER_bdsGTE_RevisionNoEsError.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Permite cerrar un hallazgo como "No es un error" en vez
                de como corregido.

                POR QUE: hasta ahora la unica salida de un hallazgo era
                marcarlo corregido, asi que cuando el tester reportaba
                algo por una mala interpretacion, el desarrollador tenia
                que mentir (marcarlo corregido sin haber cambiado nada)
                o dejarlo abierto bloqueando el cierre del WorkItem. Las
                dos opciones ensucian las metricas de calidad: la
                primera infla los defectos corregidos, la segunda los
                defectos abiertos.

                EsFalsoPositivo distingue las dos salidas SIN tocar
                Corregido: el hallazgo se sigue marcando Corregido = 1
                para que deje de bloquear (RN-GTE-025 y RN-GTE-031 se
                evaluan sobre esa columna y no hay que tocarlas), y la
                bandera nueva es la que separa "se arreglo" de "no habia
                nada que arreglar" al momento de contar defectos.

                MotivoDescarte es obligatorio por regla de negocio, no
                por constraint: la columna es NULL porque los hallazgos
                que ya existen no tienen motivo y no se les puede
                inventar uno. El backend exige el texto al marcar.
   Requiere:    tblRevision, tblPermiso, tblRol, tblRolPermiso.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRevision' AND COLUMN_NAME = 'EsFalsoPositivo'
    )
    BEGIN
        ALTER TABLE dbo.tblRevision
            ADD EsFalsoPositivo BIT NOT NULL
                CONSTRAINT DF_tblRevision_EsFalsoPositivo DEFAULT (0)
        PRINT 'OK: tblRevision.EsFalsoPositivo agregada -> BIT NOT NULL DEFAULT 0'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: tblRevision.EsFalsoPositivo ya existe'
    END

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRevision' AND COLUMN_NAME = 'MotivoDescarte'
    )
    BEGIN
        ALTER TABLE dbo.tblRevision ADD MotivoDescarte NVARCHAR(500) NULL
        PRINT 'OK: tblRevision.MotivoDescarte agregada -> NVARCHAR(500) NULL'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: tblRevision.MotivoDescarte ya existe'
    END

    -- Descartar un hallazgo ajeno es facultad del lider, igual que reabrirlo
    -- (RN-GTE-026): si lo pudiera hacer cualquiera, el desarrollador cerraria
    -- sus propios hallazgos declarandolos falsos positivos.
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'REV.Descartar', N'Revisiones',
         N'Cerrar un hallazgo como "No es un error" (falso positivo) capturando la razon')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permiso REV.Descartar sembrado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre IN (N'Administrador', N'Lider')
      AND p.Clave = N'REV.Descartar'
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: REV.Descartar asignado a Administrador y Lider (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
