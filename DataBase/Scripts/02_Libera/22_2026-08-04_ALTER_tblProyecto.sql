USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      22_2026-08-04_ALTER_tblProyecto.sql
   Autor:       Equipo GTE
   Descripcion: Columna nueva tblProyecto.Administrado (BIT, mismo patron
                que EsMantenimiento), pedida por el negocio 2026-08-04:
                un proyecto "administrado" se marca a mano desde
                Administracion > Proyectos. En proyectos administrados,
                crear un WorkItem nuevo o cancelarlo (baja logica de un
                elemento pendiente, permiso WI.Eliminar ya existente)
                exige ademas un permiso especifico (WI.CrearEnAdministrado
                / WI.EliminarEnAdministrado, ver script 23) -- el resto de
                usuarios solo puede cambiar estatus. Proyectos NO
                administrados (default, Administrado = 0) no cambian de
                comportamiento.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblProyecto' AND COLUMN_NAME = 'Administrado'
    )
    BEGIN
        ALTER TABLE dbo.tblProyecto
            ADD Administrado BIT NOT NULL CONSTRAINT DF_tblProyecto_Administrado DEFAULT (0)
        PRINT 'OK: tblProyecto.Administrado agregada -> BIT NOT NULL DEFAULT 0'
    END
    ELSE
        PRINT 'SKIP: tblProyecto.Administrado ya existe'

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
