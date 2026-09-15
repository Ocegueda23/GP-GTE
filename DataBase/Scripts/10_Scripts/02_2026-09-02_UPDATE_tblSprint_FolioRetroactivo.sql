USE [bdsGTE]
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-09-02_UPDATE_tblSprint_FolioRetroactivo.sql
   Autor:       Ana Viramontes
   Descripcion: Asigna folio a los sprints que quedaron sin el. El script
                09_Scripts/01 agrego tblSprint.Folio como NULL y solo los
                sprints creados despues lo reciben (spGenerarFolio, serie
                SPR); los anteriores se quedaron en blanco y la bandeja de
                trabajo y el detalle del workitem los muestran sin folio.
                Se recorren en orden de alta (IdSprint) para que el
                consecutivo respete la antiguedad, y se usa spGenerarFolio
                -- no un numero calculado a mano -- para que tblFolio quede
                sincronizada y el siguiente sprint nuevo no repita folio.
   Idempotente: solo toca las filas con Folio NULL; correrlo de nuevo no
                consume folios ni cambia los ya asignados.
   Requiere:    09_Scripts/01_2026-09-02_ALTER_tblSprint.sql aplicado.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblSprint'
                     AND COLUMN_NAME = 'Folio')
    BEGIN
        PRINT 'SKIP: tblSprint.Folio no existe; corre antes 09_Scripts/01.'
    END
    ELSE
    BEGIN
        DECLARE @IdSprint INT, @Folio NVARCHAR(50), @Mensaje NVARCHAR(4000), @Asignados INT = 0

        DECLARE @Pendientes TABLE (Orden INT IDENTITY(1,1), IdSprint INT)
        INSERT INTO @Pendientes (IdSprint)
        SELECT IdSprint FROM dbo.tblSprint WHERE Folio IS NULL ORDER BY IdSprint

        WHILE EXISTS (SELECT 1 FROM @Pendientes)
        BEGIN
            SELECT TOP (1) @IdSprint = IdSprint FROM @Pendientes ORDER BY Orden

            EXEC dbo.spGenerarFolio
                 @Serie   = N'SPR',
                 @Digitos = 4,
                 @Usuario = N'ScriptFolioRetroactivo',
                 @Folio   = @Folio OUTPUT,
                 @Mensaje = @Mensaje OUTPUT

            UPDATE dbo.tblSprint
               SET Folio        = @Folio,
                   UsuarioMovto = N'ScriptFolioRetroactivo',
                   FechaMovto   = GETDATE()
             WHERE IdSprint = @IdSprint

            SET @Asignados = @Asignados + 1
            DELETE FROM @Pendientes WHERE IdSprint = @IdSprint
        END

        PRINT 'OK: sprints con folio asignado -> ' + CAST(@Asignados AS NVARCHAR(10))
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
