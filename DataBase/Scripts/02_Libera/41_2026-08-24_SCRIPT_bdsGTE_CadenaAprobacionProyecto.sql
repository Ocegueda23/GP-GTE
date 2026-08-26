USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      41_2026-08-24_SCRIPT_bdsGTE_CadenaAprobacionProyecto.sql
   Autor:       Equipo GTE
   Descripcion: Cierra el pendiente "Cadena de aprobacion de releases fija
                (QA, Lider, Negocio); el diseño la quiere configurable por
                proyecto" (Doctos/PENDIENTES.md, Documento Maestro §7.2/
                linea 1101). Tabla de configuracion 1-a-N por proyecto: si
                un proyecto no tiene filas aqui, CambiarEstatusReleaseHandler
                sigue usando el default fijo (GTE.Domain.Entregas.
                RolesAprobacion.Cadena = QA, Lider, Negocio) -- ningun
                proyecto existente cambia de comportamiento con este script.
                Se reemplaza completa en cada guardado desde Admin >
                Workflows (DELETE + INSERT atomico, sin baja logica: es
                configuracion vigente, no un registro de negocio con
                historial que valga la pena conservar inactivo).
   Requiere:    01_2026-07-30_SCRIPT_bdsGTE_Catalogos.sql (tblProyecto).
   ===================================================================== */
BEGIN TRY

    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tblCadenaAprobacionProyecto')
    BEGIN
        CREATE TABLE dbo.tblCadenaAprobacionProyecto (
            IdCadenaAprobacionProyecto INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
            IdProyecto                 INT NOT NULL,
            Orden                      INT NOT NULL,
            Rol                        NVARCHAR(50) NOT NULL,
            FechaRegistro              DATETIME2 NOT NULL CONSTRAINT DF_tblCadenaAprobacionProyecto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro            NVARCHAR(200) NOT NULL,
            CONSTRAINT FK_tblCadenaAprobacionProyecto_Proyecto
                FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto),
            CONSTRAINT UQ_tblCadenaAprobacionProyecto_ProyectoOrden UNIQUE (IdProyecto, Orden)
        );
        CREATE INDEX IX_tblCadenaAprobacionProyecto_Proyecto ON dbo.tblCadenaAprobacionProyecto (IdProyecto);
        PRINT 'OK: tabla tblCadenaAprobacionProyecto creada'
    END
    ELSE
    BEGIN
        PRINT 'SKIP: tabla tblCadenaAprobacionProyecto ya existia'
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
