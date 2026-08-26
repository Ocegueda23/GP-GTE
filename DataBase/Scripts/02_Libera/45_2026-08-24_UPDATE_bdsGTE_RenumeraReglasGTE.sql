USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      45_2026-08-24_UPDATE_bdsGTE_RenumeraReglasGTE.sql
   Autor:       Equipo GTE
   Descripcion: Renumera las 40 reglas de negocio del proyecto GTE al
                formato uniforme RN-GTE-NNN (decision del equipo
                2026-08-24: se prefiere uniformidad sobre conservar las
                claves heredadas).

                Contexto: el script 44 las sembro con las claves
                historicas del Documento Maestro (RN-REQ-01, RN-QA-06...),
                que traian el modulo en la clave. Desde el ajuste de este
                mismo dia, la clave la genera el sistema como
                RN-{CLAVE DEL PROYECTO}-{CONSECUTIVO}, asi que esas 40
                quedaban como excepcion permanente.

                El modulo NO se pierde: sigue en el ambito de cada regla
                (tblAmbitoRegla: "Requerimientos", "Calidad (QA)"...),
                que es donde el catalogo lo muestra y agrupa.

                El numero sigue el ORDEN DE LAS SECCIONES 3.x del
                Documento Maestro (Administracion, Portafolio,
                Requerimientos, Planeacion, Desarrollo, QA, Releases,
                Operacion, Soporte), asi que las reglas de un mismo
                modulo quedan consecutivas.

                Este script tambien:
                - Reescribe las referencias cruzadas dentro de los
                  ENUNCIADOS (varios se citan entre si por clave).
                - Deja la serie de folio RN-GTE en 40, para que la
                  siguiente regla nueva salga RN-GTE-041 y no colisione.

                Los comentarios RN-* del codigo fuente y el
                Documento Maestro se actualizaron en el mismo commit:
                la prueba CatalogoReglasSincronizadoTests falla si se
                desincronizan.

   Idempotente: renombra solo si la clave vieja todavia existe.
   Requiere:    43 y 44 aplicados.
   ===================================================================== */
BEGIN TRY

    DECLARE @IdProyectoGTE INT = (SELECT IdProyecto FROM dbo.tblProyecto WHERE Clave = N'GTE')

    IF @IdProyectoGTE IS NULL
    BEGIN
        PRINT 'SKIP: no existe el proyecto GTE, no hay nada que renumerar'
    END
    ELSE
    BEGIN
        DECLARE @Mapeo TABLE (ClaveVieja NVARCHAR(30), ClaveNueva NVARCHAR(30))
        INSERT INTO @Mapeo (ClaveVieja, ClaveNueva)
        VALUES
        (N'RN-ADM-01', N'RN-GTE-001'),
        (N'RN-ADM-02', N'RN-GTE-002'),
        (N'RN-ADM-03', N'RN-GTE-003'),
        (N'RN-ADM-04', N'RN-GTE-004'),
        (N'RN-PRY-01', N'RN-GTE-005'),
        (N'RN-PRY-02', N'RN-GTE-006'),
        (N'RN-PRY-03', N'RN-GTE-007'),
        (N'RN-REQ-01', N'RN-GTE-008'),
        (N'RN-REQ-02', N'RN-GTE-009'),
        (N'RN-REQ-03', N'RN-GTE-010'),
        (N'RN-REQ-04', N'RN-GTE-011'),
        (N'RN-REQ-05', N'RN-GTE-012'),
        (N'RN-REQ-06', N'RN-GTE-013'),
        (N'RN-REQ-07', N'RN-GTE-014'),
        (N'RN-REQ-08', N'RN-GTE-015'),
        (N'RN-REQ-09', N'RN-GTE-016'),
        (N'RN-PLA-01', N'RN-GTE-017'),
        (N'RN-PLA-02', N'RN-GTE-018'),
        (N'RN-PLA-03', N'RN-GTE-019'),
        (N'RN-PLA-04', N'RN-GTE-020'),
        (N'RN-PLA-05', N'RN-GTE-021'),
        (N'RN-DEV-01', N'RN-GTE-022'),
        (N'RN-DEV-02', N'RN-GTE-023'),
        (N'RN-DEV-03', N'RN-GTE-024'),
        (N'RN-QA-01', N'RN-GTE-025'),
        (N'RN-QA-02', N'RN-GTE-026'),
        (N'RN-QA-03', N'RN-GTE-027'),
        (N'RN-QA-04', N'RN-GTE-028'),
        (N'RN-QA-05', N'RN-GTE-029'),
        (N'RN-QA-06', N'RN-GTE-030'),
        (N'RN-REL-01', N'RN-GTE-031'),
        (N'RN-REL-02', N'RN-GTE-032'),
        (N'RN-REL-03', N'RN-GTE-033'),
        (N'RN-REL-04', N'RN-GTE-034'),
        (N'RN-OPS-01', N'RN-GTE-035'),
        (N'RN-OPS-02', N'RN-GTE-036'),
        (N'RN-OPS-03', N'RN-GTE-037'),
        (N'RN-SUP-01', N'RN-GTE-038'),
        (N'RN-SUP-02', N'RN-GTE-039'),
        (N'RN-SUP-03', N'RN-GTE-040')

        /* 1. Renumerar las claves. Solo toca las que siguen con la clave vieja,
              asi una segunda corrida no hace nada. */
        UPDATE r
           SET r.Clave = m.ClaveNueva,
               r.UsuarioMovto = N'script-despliegue',
               r.FechaMovto = GETDATE()
        FROM dbo.tblReglaNegocio r
        INNER JOIN @Mapeo m ON m.ClaveVieja = r.Clave
        WHERE r.IdProyecto = @IdProyectoGTE
        PRINT 'OK: reglas renumeradas (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

        /* 2. Reescribir las referencias cruzadas dentro de los enunciados y las
              justificaciones: varias reglas se citan entre si por clave, y si el texto
              conserva la clave vieja el catalogo apunta a algo que ya no existe.
              Se recorre el mapeo fila por fila porque REPLACE no acepta un join. */
        DECLARE @Vieja NVARCHAR(30), @Nueva NVARCHAR(30), @Textos INT = 0
        DECLARE curMapeo CURSOR LOCAL FAST_FORWARD FOR SELECT ClaveVieja, ClaveNueva FROM @Mapeo
        OPEN curMapeo
        FETCH NEXT FROM curMapeo INTO @Vieja, @Nueva
        WHILE @@FETCH_STATUS = 0
        BEGIN
            UPDATE dbo.tblReglaNegocio
               SET Enunciado = REPLACE(Enunciado, @Vieja, @Nueva),
                   Justificacion = REPLACE(ISNULL(Justificacion, N''), @Vieja, @Nueva)
             WHERE IdProyecto = @IdProyectoGTE
               AND (Enunciado LIKE N'%' + @Vieja + N'%' OR Justificacion LIKE N'%' + @Vieja + N'%')
            SET @Textos = @Textos + @@ROWCOUNT

            UPDATE dbo.tblReglaNegocioVersion
               SET Enunciado = REPLACE(Enunciado, @Vieja, @Nueva)
             WHERE IdReglaNegocio IN (SELECT IdReglaNegocio FROM dbo.tblReglaNegocio
                                       WHERE IdProyecto = @IdProyectoGTE)
               AND Enunciado LIKE N'%' + @Vieja + N'%'

            UPDATE dbo.tblReglaNegocioRelacion
               SET Nota = REPLACE(Nota, @Vieja, @Nueva)
             WHERE Nota LIKE N'%' + @Vieja + N'%'

            FETCH NEXT FROM curMapeo INTO @Vieja, @Nueva
        END
        CLOSE curMapeo
        DEALLOCATE curMapeo
        PRINT 'OK: referencias cruzadas reescritas en enunciados (' + CAST(@Textos AS NVARCHAR(10)) + ' actualizaciones)'

        /* 3. Serie de folio: la siguiente regla nueva debe salir RN-GTE-041.
              Sin esto, spGenerarFolio empezaria en 001 y chocaria con la UNIQUE
              (el handler reintenta, pero consumiria 40 numeros a lo tonto). */
        DECLARE @Serie NVARCHAR(50) = N'RN-GTE'
        DECLARE @Maximo INT = (
            SELECT ISNULL(MAX(TRY_CONVERT(INT, RIGHT(Clave, 3))), 0)
            FROM dbo.tblReglaNegocio
            WHERE IdProyecto = @IdProyectoGTE AND Clave LIKE N'RN-GTE-[0-9][0-9][0-9]'
        )

        IF NOT EXISTS (SELECT 1 FROM dbo.tblFolio WHERE Serie = @Serie)
        BEGIN
            INSERT INTO dbo.tblFolio (Serie, UltimoConsecutivo) VALUES (@Serie, @Maximo)
            PRINT 'OK: serie de folio RN-GTE creada en ' + CAST(@Maximo AS NVARCHAR(10))
        END
        ELSE
        BEGIN
            UPDATE dbo.tblFolio
               SET UltimoConsecutivo = @Maximo,
                   UsuarioMovto = N'script-despliegue',
                   FechaMovto = GETDATE()
             WHERE Serie = @Serie AND UltimoConsecutivo < @Maximo
            PRINT 'OK: serie de folio RN-GTE alineada en ' + CAST(@Maximo AS NVARCHAR(10))
        END
    END

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
