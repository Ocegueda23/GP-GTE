USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      04_2026-08-27_SCRIPT_bdsGTE_EquipoAmbito.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Agrega dbo.tblEquipo.AmbitoCentroMando: que bloque
                tecnico de indicadores le toca a cada equipo dentro del
                Centro de Mando TI (Desarrollo, Infraestructura o
                Soporte).

                POR QUE UNA COLUMNA Y NO INFERIRLO: el ambito no se puede
                deducir de forma confiable del nombre del equipo ni del
                area del puesto de su lider (tblArea es un catalogo libre
                y sin responsable). Adivinarlo por texto seria fragil y
                silencioso -- un equipo renombrado dejaria de evaluarse
                bien sin que nadie se entere. Asi es explicito y lo
                designa un administrador desde la pantalla de equipos.

                NULL es valido y significa "solo bloque comun": el equipo
                se evalua con los 16 indicadores transversales y no se le
                exige el bloque tecnico de ningun area. Es el default a
                proposito: un equipo nuevo no deberia empezar con 12
                indicadores en rojo por falta de captura.
   Requiere:    01 (tblEquipo) y el script 02 de esta tanda.
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEquipo' AND COLUMN_NAME = 'AmbitoCentroMando'
    )
    BEGIN
        ALTER TABLE dbo.tblEquipo ADD AmbitoCentroMando NVARCHAR(30) NULL
        PRINT 'OK: tblEquipo.AmbitoCentroMando agregada'
    END
    ELSE
        PRINT 'SKIP: tblEquipo.AmbitoCentroMando ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints WHERE name = 'CK_tblEquipo_AmbitoCentroMando'
    )
    BEGIN
        EXEC('ALTER TABLE dbo.tblEquipo ADD CONSTRAINT CK_tblEquipo_AmbitoCentroMando
              CHECK (AmbitoCentroMando IS NULL OR AmbitoCentroMando IN
                     (N''Desarrollo'', N''Infraestructura'', N''Soporte''))')
        PRINT 'OK: CK_tblEquipo_AmbitoCentroMando creado'
    END
    ELSE
        PRINT 'SKIP: CK_tblEquipo_AmbitoCentroMando ya existe'

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
