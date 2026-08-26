USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      39_2026-08-24_INSERT_bdsGTE_ComplejidadSeed.sql
   Autor:       Equipo GTE
   Descripcion: Semilla de tblComplejidad para ambientes NUEVOS sin
                migracion del GT legacy (la migracion real -- script
                03_2026-08-01_SCRIPT_bdsGTE_MigracionCatalogos.sql, bloque
                Complejidad -- ya siembra sus propios nombres reales
                'Basica'/'Media'/etc. leidos de bdsApollo.dbo.tblComplejidad
                y no corre en estos ambientes). Sin este script, un
                despliegue nuevo deja el combo de Complejidad vacio en el
                modal de alta de WorkItem (RN-REQ-08 la exige desde
                2026-08-13, ver Doctos/PENDIENTES.md).
                tblComplejidad es catalogo administrable (IDENTITY, ver
                Documento Maestro L845): estos 3 valores (Baja/Media/Alta)
                son un default generico, no un catalogo fijo de negocio --
                administracion puede agregar/renombrar despues via BD.
                NO se siembra tblMatrizPresupuesto (Complejidad x Nivel ->
                Minutos/Puntos) porque esos valores son una decision de
                negocio real (cuantos minutos presupuestar por nivel de
                senioridad), no algo que este script deba inventar; sin
                esas filas, RN-REQ-08 simplemente no calcula presupuesto
                automatico para WorkItems con estas complejidades hasta
                que administracion capture la matriz real.
   Requiere:    01_2026-07-30_SCRIPT_bdsGTE_Catalogos.sql (tblComplejidad).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblComplejidad (Nombre, IdCategoriaProyecto, Orden, FechaRegistro, UsuarioRegistro, Activo)
    SELECT v.Nombre, NULL, v.Orden, SYSDATETIME(), N'script-despliegue', 1
    FROM (VALUES
        (N'Baja',  1),
        (N'Media', 2),
        (N'Alta',  3)
        ) v(Nombre, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblComplejidad c WHERE c.Nombre = v.Nombre)
    PRINT 'OK: tblComplejidad sembrada (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

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
