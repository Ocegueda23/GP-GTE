USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-08-28_INSERT_bdsGTE_NotasVersionHistoricas.sql
   Autor:       Equipo GTE
   Descripcion: Carga inicial de las notas de version 1.18 a 1.21, migradas
                del bloque de texto que vivia al pie de Directory.Build.props
                (donde rompia el XML que lee publicar.bat).

                El texto original estaba redactado como PETICIONES ("agregar
                un permiso especial para eliminar articulos"). Aqui se
                reescribio en voz de usuario final, que es lo que la
                aplicacion muestra al hacer click en el sello de version.

                Las notas entran como BORRADOR (Publicada = 0) a proposito:
                la redaccion es una interpretacion de las peticiones y hay
                que revisarla antes de que la vea toda la empresa. Al final
                del script esta el UPDATE para publicarlas de golpe.

                Todos los renglones quedaron como Mejora, lo cual concuerda
                con que en cada liberacion solo subio el segundo digito.
   Requiere:    01_2026-08-28_SCRIPT_bdsGTE_NotasVersion.sql aplicado.
   ===================================================================== */
BEGIN TRY

    /* ---------------------------------------------------------------
       AJUSTAR ANTES DE CORRER.
       El versionado se estreno el 2026-08-26 (commit e063479, version
       1.17.0.0) y las versiones 1.18 a 1.20 nunca se commitearon, asi que
       su fecha real de liberacion no existe en el repositorio. Estos
       valores son tentativos dentro del unico rango posible. La 1.21 si
       viene de la nota original ("28 ago 26").
       --------------------------------------------------------------- */
    DECLARE @Fecha118 DATE = '2026-08-26'
    DECLARE @Fecha119 DATE = '2026-08-27'
    DECLARE @Fecha120 DATE = '2026-08-27'
    DECLARE @Fecha121 DATE = '2026-08-28'

    DECLARE @Mejora INT = 2   -- tblTipoCambioVersion: 1 Proyecto, 2 Mejora, 3 Defecto

    DECLARE @Datos TABLE
    (
        Version         NVARCHAR(20)  NOT NULL,
        FechaLiberacion DATE          NOT NULL,
        Resumen         NVARCHAR(500) NULL,
        IdTipoCambio    INT           NOT NULL,
        Modulo          NVARCHAR(100) NULL,
        Descripcion     NVARCHAR(500) NOT NULL,
        Orden           INT           NOT NULL
    )

    INSERT INTO @Datos (Version, FechaLiberacion, Resumen, IdTipoCambio, Modulo, Descripcion, Orden)
    VALUES
    /* ---------- 1.18.0.0 ---------- */
    (N'1.18.0.0', @Fecha118, N'Mejoras al flujo de releases y a la captura de instrucciones de implementacion.',
        @Mejora, N'Releases', N'Las instrucciones de implementacion de un artefacto se capturan con texto enriquecido: puedes darle formato y pegar imagenes.', 0),
    (N'1.18.0.0', @Fecha118, NULL,
        @Mejora, N'Releases', N'Puedes agregar tablas a las instrucciones de implementacion, creandolas o pegandolas desde el portapapeles.', 1),
    (N'1.18.0.0', @Fecha118, NULL,
        @Mejora, N'Releases', N'Una vez enviado el release a aprobacion, puedes generar un reporte imprimible con el formato de solicitud de despliegue.', 2),
    (N'1.18.0.0', @Fecha118, NULL,
        @Mejora, N'Releases', N'La aprobacion de un release ahora permite rechazarlo para correccion, en lugar de solo aprobarlo.', 3),

    /* ---------- 1.19.0.0 ---------- */
    (N'1.19.0.0', @Fecha119, N'El artefacto del centro de mando queda disponible desde la ayuda.',
        @Mejora, N'Centro de mando', N'El artefacto del centro de mando se consulta desde la ayuda. Solo los administradores tienen acceso a leerlo.', 0),

    /* ---------- 1.20.0.0 ---------- */
    (N'1.20.0.0', @Fecha120, N'Catalogos mas legibles, acciones uniformes en los grids y ajustes de navegacion.',
        @Mejora, N'WorkItems', N'Cuando abres una subtarea, ahora ves la tarea padre en sus datos, con una liga para ir directo a ella.', 0),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Catalogos', N'El detalle de los catalogos configurables muestra los nombres en lugar de los identificadores.', 1),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Catalogos', N'Los grids de catalogos administrados muestran la descripcion de cada clave en lugar del identificador.', 2),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Catalogos', N'Se renombraron las etiquetas de configuracion para que digan lo que hacen: Fecha registro auto, Usuario registro auto, Fecha edicion auto y Usuario edicion auto.', 3),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Controles', N'Las acciones de todos los grids del sistema se unificaron en iconos, para que funcionen igual en cualquier pantalla.', 4),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Base de conocimiento', N'Eliminar articulos ahora requiere un permiso propio, separado del de edicion.', 5),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Pruebas', N'Ademas de marcar un hallazgo como corregido, puedes marcarlo como que no es un error. En ese caso se pide capturar la razon.', 6),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Calidad', N'El catalogo de casos de prueba salio del menu de administracion y vive con el resto de calidad.', 7),
    (N'1.20.0.0', @Fecha120, NULL,
        @Mejora, N'Navegacion', N'El menu se reagrupo en secciones ordenadas para encontrar las cosas mas rapido.', 8),

    /* ---------- 1.21.0.0 ---------- */
    (N'1.21.0.0', @Fecha121, N'Nuevo reporte de actividades terminadas y registro de ausencias.',
        @Mejora, N'Reportes', N'Nuevo reporte de actividades terminadas: el detalle de cada tarea cerrada con su tiempo invertido, sus fechas y cuanto tardo en resolverse, filtrable por equipo, persona, proyecto, tipo y folio.', 0),
    (N'1.21.0.0', @Fecha121, NULL,
        @Mejora, N'Ausencias', N'Ya puedes registrar tus ausencias y darles seguimiento con su flujo de aprobacion.', 1)

    /* ---------- Encabezados ---------- */
    INSERT INTO dbo.tblNotaVersion (Version, FechaLiberacion, Resumen, Publicada, UsuarioRegistro, Activo)
    SELECT d.Version,
           MIN(d.FechaLiberacion),
           MAX(d.Resumen),          -- el resumen va solo en el primer renglon de cada version
           0,                       -- borrador: revisar la redaccion antes de publicar
           N'script-despliegue',
           1
    FROM @Datos d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblNotaVersion n WHERE n.Version = d.Version)
    GROUP BY d.Version
    PRINT 'OK: encabezados de notas de version sembrados'

    /* ---------- Detalle ---------- */
    INSERT INTO dbo.tblNotaVersionDetalle
        (IdNotaVersion, IdTipoCambioVersion, Modulo, Descripcion, Orden, UsuarioRegistro, Activo)
    SELECT n.IdNotaVersion, d.IdTipoCambio, d.Modulo, d.Descripcion, d.Orden, N'script-despliegue', 1
    FROM @Datos d
    INNER JOIN dbo.tblNotaVersion n ON n.Version = d.Version
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblNotaVersionDetalle det
                      WHERE det.IdNotaVersion = n.IdNotaVersion
                        AND det.Descripcion   = d.Descripcion)
    PRINT 'OK: detalle de notas de version sembrado'

    SELECT n.Version, n.FechaLiberacion,
           CASE WHEN n.Publicada = 1 THEN 'Publicada' ELSE 'Borrador' END AS Estado,
           COUNT(det.IdNotaVersionDetalle) AS Renglones
    FROM dbo.tblNotaVersion n
    LEFT JOIN dbo.tblNotaVersionDetalle det ON det.IdNotaVersion = n.IdNotaVersion AND det.Activo = 1
    WHERE n.Activo = 1
    GROUP BY n.Version, n.FechaLiberacion, n.Publicada
    ORDER BY n.Version

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
    PRINT 'Las notas quedaron como BORRADOR. Revisa la redaccion en'
    PRINT 'Administracion / Notas de version y publicalas desde ahi,'
    PRINT 'o corre el UPDATE comentado al final de este script.'
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO

/* Publicar las cuatro de golpe, ya que revisaste la redaccion:

UPDATE dbo.tblNotaVersion
   SET Publicada    = 1,
       UsuarioMovto = SYSTEM_USER,
       FechaMovto   = GETDATE()
 WHERE Version IN (N'1.18.0.0', N'1.19.0.0', N'1.20.0.0', N'1.21.0.0')
   AND Publicada = 0

*/
