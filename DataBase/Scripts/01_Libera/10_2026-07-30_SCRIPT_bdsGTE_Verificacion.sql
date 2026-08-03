USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
/* =====================================================================
   Script:      10_2026-07-30_SCRIPT_bdsGTE_Verificacion.sql
   Autor:       Equipo GTE
   Descripcion: Verificacion post-despliegue (SOLO LECTURA, sin
                transaccion): comprueba que todos los objetos de la tanda
                01-08 existan y que los seeds de contrato tengan filas.
                Usar tambien para comparar dev/preprod/prod.
   ===================================================================== */
SET NOCOUNT ON

DECLARE @esperados TABLE (Nombre SYSNAME, Tipo NVARCHAR(2))
INSERT INTO @esperados (Nombre, Tipo) VALUES
    -- Catalogos (01)
    (N'tblEstatusSolicitud','U'),(N'tblEstatusWorkItem','U'),(N'tblEstatusSprint','U'),
    (N'tblEstatusRelease','U'),(N'tblEstatusDespliegue','U'),(N'tblEstatusIncidente','U'),
    (N'tblEstatusTicket','U'),(N'tblEstatusRiesgo','U'),(N'tblEstatusRevision','U'),
    (N'tblEstatusAprobacion','U'),(N'tblEstatusProyecto','U'),(N'tblEstatusAusencia','U'),
    (N'tblTipoWorkItem','U'),(N'tblTipoVinculo','U'),(N'tblPrioridad','U'),(N'tblSeveridad','U'),
    (N'tblResultadoPrueba','U'),(N'tblTipoPrueba','U'),(N'tblTipoArtefacto','U'),
    (N'tblTipoAusencia','U'),(N'tblTipoSolicitud','U'),(N'tblCategoriaProyecto','U'),
    (N'tblNivel','U'),(N'tblComplejidad','U'),(N'tblMatrizPresupuesto','U'),
    (N'tblEtiqueta','U'),(N'tblCategoriaTicket','U'),
    -- Administracion (02)
    (N'tblArea','U'),(N'tblPuesto','U'),(N'tblHorario','U'),(N'tblHorarioTramo','U'),
    (N'tblDiaFestivo','U'),(N'tblUsuario','U'),(N'tblRol','U'),(N'tblPermiso','U'),
    (N'tblRolPermiso','U'),(N'tblUsuarioRol','U'),(N'tblEquipo','U'),(N'tblEquipoMiembro','U'),
    (N'tblAusencia','U'),
    -- Portafolio (03)
    (N'tblPortafolio','U'),(N'tblPrograma','U'),(N'tblProyecto','U'),(N'tblHito','U'),
    (N'tblRiesgo','U'),(N'tblObjetivoOkr','U'),(N'tblResultadoClave','U'),
    (N'tblTarifaNivel','U'),(N'tblPresupuestoProyecto','U'),(N'tblAmbiente','U'),(N'tblRepositorio','U'),
    -- Nucleo (04)
    (N'tblSprint','U'),(N'tblCapacidadSprint','U'),(N'tblTablero','U'),(N'tblTableroColumna','U'),
    (N'tblSolicitud','U'),(N'tblWorkItem','U'),(N'tblRegistroTiempo','U'),(N'tblRevision','U'),
    (N'tblWorkItemVinculo','U'),(N'tblComentario','U'),(N'tblArchivo','U'),(N'tblArchivoVinculo','U'),
    (N'tblHistorialEstatus','U'),(N'tblHistorialCampo','U'),
    -- Desarrollo/QA/Releases (05)
    (N'tblCommit','U'),(N'tblCommitWorkItem','U'),(N'tblPullRequest','U'),(N'tblPipelineEjecucion','U'),
    (N'tblArtefacto','U'),(N'tblRelease','U'),(N'tblReleaseArtefacto','U'),(N'tblDespliegue','U'),
    (N'tblAprobacion','U'),(N'tblPlanPrueba','U'),(N'tblCasoPrueba','U'),(N'tblCasoPruebaPaso','U'),
    (N'tblCicloPrueba','U'),(N'tblEjecucionPrueba','U'),
    -- Operacion/Soporte (06)
    (N'tblIncidente','U'),(N'tblBitacoraCambio','U'),(N'tblSla','U'),(N'tblTicket','U'),
    (N'tblEncuestaSatisfaccion','U'),(N'tblArticuloConocimiento','U'),(N'tblArticuloVersion','U'),
    -- Transversales (07) - incluye el motor de estatus y folios propios
    (N'tblProceso','U'),(N'tblTransicion','U'),(N'tblFolio','U'),
    (N'tblBitacora','U'),(N'tblNotificacion','U'),(N'tblPlantillaNotificacion','U'),
    (N'tblReglaAutomatizacion','U'),(N'tblEventoDominio','U'),(N'tblKpiDefinicion','U'),
    (N'tblKpiValor','U'),(N'tblVersionSistema','U'),(N'tblTransicionConfig','U'),
    -- Programables (08)
    (N'fnMinutosLaborales','IF'),(N'spCambiarEstatus','P'),(N'spGenerarFolio','P'),
    (N'spRegistrarBitacora','P'),(N'spSnapshotKpi','P'),(N'trWorkItemHistorialCampo','TR'),
    (N'vwTiempoInvertido','V'),(N'vwBandejaTrabajo','V')

DECLARE @faltantes INT = 0, @total INT = 0
DECLARE @nombre SYSNAME, @tipo NVARCHAR(2)

DECLARE curVerifica CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre, Tipo FROM @esperados
OPEN curVerifica
FETCH NEXT FROM curVerifica INTO @nombre, @tipo
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @total = @total + 1
    IF NOT EXISTS (SELECT 1 FROM sys.objects o
                   WHERE o.name = @nombre AND o.type = @tipo AND SCHEMA_NAME(o.schema_id) = 'dbo')
    BEGIN
        SET @faltantes = @faltantes + 1
        PRINT 'FALTA: dbo.' + @nombre + ' (' + @tipo + ')'
    END
    FETCH NEXT FROM curVerifica INTO @nombre, @tipo
END
CLOSE curVerifica
DEALLOCATE curVerifica

/* Seeds de contrato: los catalogos de estatus y enumerados no pueden estar vacios */
DECLARE @seedsVacios INT = 0
IF NOT EXISTS (SELECT 1 FROM dbo.tblEstatusWorkItem)  BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblEstatusWorkItem' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblTipoWorkItem)     BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblTipoWorkItem' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblPrioridad)        BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblPrioridad' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblNivel)            BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblNivel' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblRol)              BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblRol' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblPermiso)          BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblPermiso' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblHorarioTramo)     BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblHorarioTramo' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblAmbiente)         BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblAmbiente' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblProceso)          BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblProceso (script 09)' END
IF NOT EXISTS (SELECT 1 FROM dbo.tblTransicion)       BEGIN SET @seedsVacios += 1; PRINT 'FALTA SEED: tblTransicion (script 09)' END

PRINT '---------------------------------------------'
PRINT 'Objetos esperados : ' + CAST(@total AS NVARCHAR(10))
PRINT 'Objetos faltantes : ' + CAST(@faltantes AS NVARCHAR(10))
PRINT 'Seeds vacios      : ' + CAST(@seedsVacios AS NVARCHAR(10))
IF @faltantes = 0 AND @seedsVacios = 0
    PRINT '===== VERIFICACION EXITOSA: despliegue completo ====='
ELSE
    PRINT '===== VERIFICACION CON PENDIENTES: revisar FALTA arriba ====='
GO
