USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      30_2026-08-12_SCRIPT_bdsGTE_DashboardLayoutUsuario.sql
   Autor:       Equipo GTE
   Descripcion: Tabla nueva para el Dashboard Ejecutivo P18 (Doctos/
                GTE-DocumentoMaestro.md 3.10/5.10): persiste el layout de
                widgets (orden/visibilidad) que cada usuario configura en
                su propio dashboard. Una fila por usuario (upsert desde
                backend via EF, no via MERGE de SP -- mismo criterio que
                el resto de GTE, sin stored procedures para CRUD simple).
                No se crea permiso nuevo: el acceso al modulo se gobierna
                con los permisos ya sembrados DASH.Ejecutivo (script 02) y
                DASH.VerDepartamento (script 26).
   Requiere:    01 (tblUsuario).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                   WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblDashboardLayoutUsuario')
    BEGIN
        CREATE TABLE [dbo].[tblDashboardLayoutUsuario]
        (
            IdUsuario     INT                         NOT NULL,
            LayoutJson    NVARCHAR(MAX)               NOT NULL,
            FechaRegistro DATETIME2                   NOT NULL CONSTRAINT DF_tblDashboardLayoutUsuario_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioMovto  NVARCHAR(50)                NULL,
            FechaMovto    DATETIME                    NULL,
            CONSTRAINT PK_tblDashboardLayoutUsuario PRIMARY KEY (IdUsuario),
            CONSTRAINT FK_tblDashboardLayoutUsuario_tblUsuario FOREIGN KEY (IdUsuario)
                REFERENCES [dbo].[tblUsuario] (IdUsuario)
        )
        PRINT 'OK: tblDashboardLayoutUsuario creada correctamente'
    END
    ELSE
        PRINT 'SKIP: tblDashboardLayoutUsuario ya existe'

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
