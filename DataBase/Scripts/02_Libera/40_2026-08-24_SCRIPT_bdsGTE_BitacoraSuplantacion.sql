USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      40_2026-08-24_SCRIPT_bdsGTE_BitacoraSuplantacion.sql
   Autor:       Equipo GTE
   Descripcion: Cierra el pendiente "Suplantacion auditada" del Documento
                Maestro §8.1 (permiso ADM.Suplantar ya sembrado desde el
                script 02, sin consumidor hasta ahora). Doble identidad en
                bitacora: tblBitacora.Usuario sigue siendo el SUPLANTADO
                (para saber sobre quien se actuo, igual que cualquier otro
                registro), y esta columna nueva guarda el SUPLANTADOR real
                (via AuditContext.UsuarioReal, llenado del claim "actor_real"
                del token que emite AuthController.IniciarSuplantacion).
                NULL en el 100% de la bitacora existente y en toda accion
                fuera de una suplantacion activa.
   Requiere:    07_2026-07-30_SCRIPT_bdsGTE_Transversales.sql (tblBitacora).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.tblBitacora') AND name = 'UsuarioReal'
    )
    BEGIN
        ALTER TABLE dbo.tblBitacora ADD UsuarioReal NVARCHAR(200) NULL;
        PRINT 'OK: columna UsuarioReal agregada a tblBitacora'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: columna UsuarioReal ya existia en tblBitacora'
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
