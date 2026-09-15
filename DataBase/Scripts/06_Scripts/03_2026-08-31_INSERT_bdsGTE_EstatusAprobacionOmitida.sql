USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-08-31_INSERT_bdsGTE_EstatusAprobacionOmitida.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Estatus de aprobacion 4 'Omitida': la firma que ya no se
                pidio porque alguien autorizo el release (AUTORIZAR).
                Se separa de 'Aprobada' a proposito -- la Solicitud de
                despliegue impresa tiene que poder distinguir a quien
                firmo de verdad de quien quedo cubierto por la
                autorizacion; si se reusara 'Aprobada' el formato diria
                que seis personas firmaron cuando firmo una.
   Requiere:    Tanda 01 (tblEstatusAprobacion).
   ===================================================================== */
BEGIN TRY

    INSERT INTO dbo.tblEstatusAprobacion (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (4, N'Omitida', 4)) v(Id, Descripcion, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusAprobacion e WHERE e.Id = v.Id)
    PRINT 'OK: estatus de aprobacion Omitida sembrado'

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
