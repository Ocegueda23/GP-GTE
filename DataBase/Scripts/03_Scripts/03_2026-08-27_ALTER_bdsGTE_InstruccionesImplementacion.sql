USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-08-27_ALTER_bdsGTE_InstruccionesImplementacion.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Instrucciones de implementacion del release y de cada
                artefacto, en HTML enriquecido (mismo formato que las
                descripciones y comentarios del sistema: formato de
                texto, tablas e imagenes pegadas del portapapeles, que
                se guardan por GUID de adjunto, nunca como URL).

                POR QUE NVARCHAR(MAX) Y NO UNA TABLA APARTE: es un solo
                bloque de texto por release y por artefacto, siempre se
                lee junto con su dueno y no se consulta ni se filtra por
                su contenido. Mismo criterio que tblRelease.NotasVersion.

                DESTINO: alimentan la "Solicitud de despliegue" -- el
                reporte imprimible que se genera una vez que el release
                se mando a aprobacion, equivalente al formato de Excel
                que se llenaba a mano (Doctos/Solicitud de despliegue.xlsx).

   Tablas:      dbo.tblRelease            + InstruccionesImplementacion
                dbo.tblReleaseArtefacto   + InstruccionesImplementacion
   ===================================================================== */
BEGIN TRY

    IF COL_LENGTH('dbo.tblRelease', 'InstruccionesImplementacion') IS NULL
    BEGIN
        ALTER TABLE dbo.tblRelease ADD InstruccionesImplementacion NVARCHAR(MAX) NULL
        PRINT 'OK: tblRelease.InstruccionesImplementacion agregada'
    END
    ELSE
        PRINT 'SKIP: tblRelease.InstruccionesImplementacion ya existe'

    IF COL_LENGTH('dbo.tblReleaseArtefacto', 'InstruccionesImplementacion') IS NULL
    BEGIN
        ALTER TABLE dbo.tblReleaseArtefacto ADD InstruccionesImplementacion NVARCHAR(MAX) NULL
        PRINT 'OK: tblReleaseArtefacto.InstruccionesImplementacion agregada'
    END
    ELSE
        PRINT 'SKIP: tblReleaseArtefacto.InstruccionesImplementacion ya existe'

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
