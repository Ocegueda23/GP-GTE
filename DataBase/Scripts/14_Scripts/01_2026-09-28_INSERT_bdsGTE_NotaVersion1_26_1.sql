USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-28_INSERT_bdsGTE_NotaVersion1_26_1.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Nota de version 1.26.1.0: las tres correcciones al reporte
                R16 (Gantt de actividades) que se reportaron despues de
                liberarlo en la 1.26.0.0.

                Los tres renglones entran como Defecto, que es coherente
                con que solo subiera el tercer digito de la version: el
                reporte ya existia y lo que se libera es su correccion.

                Redactado en voz de usuario final (lo que ve al hacer click
                en el sello de version), no como peticion ni como detalle
                tecnico -- mismo criterio que el script de notas historicas
                (06_Scripts/02_..._NotasVersionHistoricas.sql).

                Entra como BORRADOR (Publicada = 0) a proposito: publicar la
                nota la pone frente a toda la empresa, y esa decision es de
                quien libera. Al final esta el UPDATE para publicarla, o se
                hace desde Administracion / Notas de version.
   Requiere:    06_Scripts/01_2026-08-28_SCRIPT_bdsGTE_NotasVersion.sql aplicado.
   ===================================================================== */
BEGIN TRY

    DECLARE @Version NVARCHAR(20) = N'1.26.1.0'
    DECLARE @Fecha   DATE         = '2026-09-28'
    DECLARE @Defecto INT          = 3   -- tblTipoCambioVersion: 1 Proyecto, 2 Mejora, 3 Defecto

    DECLARE @Datos TABLE
    (
        IdTipoCambio INT           NOT NULL,
        Modulo       NVARCHAR(100) NULL,
        Descripcion  NVARCHAR(500) NOT NULL,
        Orden        INT           NOT NULL
    )

    INSERT INTO @Datos (IdTipoCambio, Modulo, Descripcion, Orden)
    VALUES
    (@Defecto, N'Reportes',
        N'El Gantt de actividades ya no muestra las actividades canceladas: el diagrama refleja solo el trabajo que se realizo en el periodo.', 0),
    (@Defecto, N'Reportes',
        N'El Excel del Gantt de actividades ahora incluye el diagrama dibujado junto a la tabla, con una barra de color por actividad, para poder revisarlo e imprimirlo sin entrar al sistema.', 1),
    (@Defecto, N'Reportes',
        N'La columna de descripcion de los reportes exportados a Excel ya sale como texto legible, sin las etiquetas del editor.', 2)

    /* ---------- Encabezado ---------- */
    INSERT INTO dbo.tblNotaVersion (Version, FechaLiberacion, Resumen, Publicada, UsuarioRegistro, Activo)
    SELECT @Version, @Fecha,
           N'Correcciones al reporte de Gantt de actividades y a la exportacion a Excel.',
           0,                       -- borrador: publicar al liberar
           N'script-despliegue',
           1
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblNotaVersion n WHERE n.Version = @Version)
    PRINT 'OK: encabezado de la nota de version 1.26.1.0'

    /* ---------- Detalle ---------- */
    INSERT INTO dbo.tblNotaVersionDetalle
        (IdNotaVersion, IdTipoCambioVersion, Modulo, Descripcion, Orden, UsuarioRegistro, Activo)
    SELECT n.IdNotaVersion, d.IdTipoCambio, d.Modulo, d.Descripcion, d.Orden, N'script-despliegue', 1
    FROM @Datos d
    INNER JOIN dbo.tblNotaVersion n ON n.Version = @Version
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblNotaVersionDetalle det
                      WHERE det.IdNotaVersion = n.IdNotaVersion
                        AND det.Descripcion   = d.Descripcion)
    PRINT 'OK: detalle de la nota de version 1.26.1.0'

    SELECT n.Version, n.FechaLiberacion,
           CASE WHEN n.Publicada = 1 THEN 'Publicada' ELSE 'Borrador' END AS Estado,
           COUNT(det.IdNotaVersionDetalle) AS Renglones
    FROM dbo.tblNotaVersion n
    LEFT JOIN dbo.tblNotaVersionDetalle det ON det.IdNotaVersion = n.IdNotaVersion AND det.Activo = 1
    WHERE n.Activo = 1 AND n.Version = @Version
    GROUP BY n.Version, n.FechaLiberacion, n.Publicada

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
    PRINT 'La nota quedo como BORRADOR. Publicala desde Administracion /'
    PRINT 'Notas de version, o corriendo:'
    PRINT '    UPDATE dbo.tblNotaVersion SET Publicada = 1 WHERE Version = N''1.26.1.0'''
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
