USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
/* =====================================================================
   Script:      08_2026-07-30_SCRIPT_bdsGTE_Programables.sql
   Autor:       Equipo GTE
   Descripcion: Objetos programables:
                - fnMinutosLaborales: motor UNICO de tiempo laborable
                  (tramos por dia + festivos, sin cursores)
                - spCambiarEstatus: motor de estatus PROPIO de bdsGTE
                  (clona el patron transversal seccion 9 del estandar:
                  UPDATE dinamico blindado + guard de concurrencia) y
                  materializa el historial con minutos laborales
                - spGenerarFolio: folios propios (ROWLOCK/UPDLOCK/HOLDLOCK)
                - spRegistrarBitacora, spSnapshotKpi
                - trWorkItemHistorialCampo (red de seguridad de auditoria)
                - vwTiempoInvertido, vwBandejaTrabajo
   Requiere:    01-07 (tblProceso, tblTransicion y tblFolio viven en bdsGTE:
                GTE es totalmente independiente de otras bases)
   Nota:        Los objetos programables deben ser la primera instruccion
                de su batch: este script usa batches GO con CREATE OR ALTER
                (idempotente por naturaleza) en vez de la transaccion unica
                de la plantilla de datos. Cada objeto es atomico por si mismo.
   ===================================================================== */
PRINT 'Inicia creacion de objetos programables...'
GO

/* ---------- fnMinutosLaborales ----------
   Minutos laborables entre dos fechas segun los tramos del horario
   (tblHorarioTramo, DiaSemana 1=lunes) excluyendo festivos
   (tblDiaFestivo: IdHorario NULL = global). Sustituye a los 4 motores
   inconsistentes del GT. Funcion inline (sin cursor): apta para APPLY.
   Limite practico: intervalos de hasta 10,000 dias (~27 anios).        */
CREATE OR ALTER FUNCTION dbo.fnMinutosLaborales
(
    @Inicio    DATETIME2,
    @Fin       DATETIME2,
    @IdHorario INT
)
RETURNS TABLE
AS
RETURN
WITH n10 AS (
    SELECT v FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) t(v)
),
nums AS (
    SELECT TOP (CASE WHEN @Fin >= @Inicio THEN DATEDIFF(DAY, @Inicio, @Fin) + 1 ELSE 0 END)
           ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS n
    FROM n10 a CROSS JOIN n10 b CROSS JOIN n10 c CROSS JOIN n10 d
),
dias AS (
    SELECT DATEADD(DAY, n, CAST(@Inicio AS DATE)) AS Dia
    FROM nums
),
tramos AS (
    SELECT d.Dia, t.HoraInicio, t.HoraFin
    FROM dias d
    INNER JOIN dbo.tblHorarioTramo t
        ON t.IdHorario = @IdHorario
       AND t.DiaSemana = (DATEDIFF(DAY, '19000101', d.Dia) % 7) + 1   -- 1=lunes, independiente de DATEFIRST
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.tblDiaFestivo f
        WHERE f.Fecha = d.Dia
          AND (f.IdHorario IS NULL OR f.IdHorario = @IdHorario)
          AND f.Activo = 1)
)
SELECT ISNULL(SUM(
           CASE WHEN ef.FinEfectivo > ef.IniEfectivo
                THEN DATEDIFF(MINUTE, ef.IniEfectivo, ef.FinEfectivo)
                ELSE 0 END), 0) AS Minutos
FROM tramos tr
CROSS APPLY (SELECT
    TramoIni = CAST(tr.Dia AS DATETIME) + CAST(tr.HoraInicio AS DATETIME),
    TramoFin = CAST(tr.Dia AS DATETIME) + CAST(tr.HoraFin AS DATETIME)) lim
CROSS APPLY (SELECT
    IniEfectivo = CASE WHEN CAST(@Inicio AS DATETIME) > lim.TramoIni THEN CAST(@Inicio AS DATETIME) ELSE lim.TramoIni END,
    FinEfectivo = CASE WHEN CAST(@Fin AS DATETIME) < lim.TramoFin THEN CAST(@Fin AS DATETIME) ELSE lim.TramoFin END) ef
GO
PRINT 'OK: fnMinutosLaborales'
GO

/* ---------- spCambiarEstatus ---------- */
CREATE OR ALTER PROCEDURE dbo.spCambiarEstatus
    @Proceso         NVARCHAR(100),
    @IdRegistro      INT,
    @IdEstatusActual INT,
    @Accion          NVARCHAR(50),
    @Usuario         NVARCHAR(200),
    @Motivo          NVARCHAR(500) = NULL,
    @IdHorario       INT = NULL,
    @Mensaje         NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
-- Autor: Equipo GTE
-- Fecha Creacion: 30 Jul 2026
-- Descripcion: Unica puerta de cambio de estatus de GTE. Motor PROPIO de bdsGTE
--              (patron transversal seccion 9 del estandar, clonado por independencia
--              total - ADR-03): lee la configuracion de dbo.tblProceso, busca la
--              transicion en dbo.tblTransicion (el destino SE LEE, no se calcula),
--              ejecuta UPDATE dinamico blindado (PARSENAME + QUOTENAME +
--              sp_executesql) con guard de concurrencia, y materializa
--              tblHistorialEstatus: cierra el intervalo abierto calculando
--              MinutosLaborales con fnMinutosLaborales (si se recibe @IdHorario)
--              y abre el intervalo nuevo.
-- Describe cada uno de los parametros
-- @Proceso         Nombre del proceso en dbo.tblProceso (ej. 'WorkItem')
-- @IdRegistro      PK del registro a mover
-- @IdEstatusActual Estatus que el llamador cree vigente (guard de concurrencia)
-- @Accion          Accion del grafo (INICIAR, TERMINAR, RECHAZAR...)
-- @Usuario         Usuario que ejecuta (del token, nunca del payload)
-- @Motivo          Motivo (obligatorio en rechazos; lo valida el backend)
-- @IdHorario       Horario para materializar minutos laborales del intervalo cerrado
-- @Mensaje         Mensaje de salida
-- Codigos RETURN (contrato heredado del patron transversal): 0 OK, 50 proceso
-- inexistente, 51 config invalida, 52 conflicto de concurrencia, 53 transicion no permitida
-- =====================================================================================
-- Modificacion:                                                                 Rev_00
-- Fecha: 30 Jul 2026
-- Descripcion: Version inicial
-- =====================================================================================
    BEGIN TRY
        DECLARE @IdProceso INT, @IdEstatusDestino INT,
                @TablaTransaccional NVARCHAR(300), @ColumnaEstatus SYSNAME, @ColumnaPK SYSNAME,
                @Sql NVARCHAR(MAX), @Filas INT

        SELECT @IdProceso = p.IdProceso,
               @TablaTransaccional = p.TablaTransaccional,
               @ColumnaEstatus = p.ColumnaEstatus,
               @ColumnaPK = p.ColumnaPK
        FROM dbo.tblProceso p
        WHERE p.Proceso = @Proceso AND p.Activo = 1

        IF @IdProceso IS NULL
        BEGIN
            SET @Mensaje = 'Proceso inexistente o inactivo: ' + @Proceso;
            RETURN 50;
        END

        SELECT @IdEstatusDestino = t.IdEstatusDestino
        FROM dbo.tblTransicion t
        WHERE t.IdProceso = @IdProceso
          AND t.IdEstatusOrigen = @IdEstatusActual
          AND t.Accion = @Accion
          AND t.Activo = 1

        IF @IdEstatusDestino IS NULL
        BEGIN
            SET @Mensaje = 'Transicion no permitida: ' + @Accion + ' desde el estatus actual.';
            RETURN 53;
        END

        -- Valida la tabla transaccional (codigo 51)
        IF OBJECT_ID(@TablaTransaccional) IS NULL
        BEGIN
            SET @Mensaje = 'Configuracion invalida del proceso: no existe la tabla ' + @TablaTransaccional + '.';
            RETURN 51;
        END

        BEGIN TRANSACTION;

        -- UPDATE dinamico blindado con guard de concurrencia:
        -- si otro usuario ya movio el registro, 0 filas afectadas -> rollback -> 52
        SET @Sql = N'UPDATE ' + QUOTENAME(ISNULL(PARSENAME(@TablaTransaccional, 2), N'dbo'))
                 + N'.' + QUOTENAME(PARSENAME(@TablaTransaccional, 1))
                 + N' SET ' + QUOTENAME(@ColumnaEstatus) + N' = @Destino'
                 + N' WHERE ' + QUOTENAME(@ColumnaPK) + N' = @IdRegistro'
                 + N' AND ' + QUOTENAME(@ColumnaEstatus) + N' = @Actual;'
                 + N' SET @FilasOut = @@ROWCOUNT;';

        EXEC sp_executesql @Sql,
             N'@Destino INT, @IdRegistro INT, @Actual INT, @FilasOut INT OUTPUT',
             @Destino = @IdEstatusDestino, @IdRegistro = @IdRegistro,
             @Actual = @IdEstatusActual, @FilasOut = @Filas OUTPUT;

        IF ISNULL(@Filas, 0) = 0
        BEGIN
            ROLLBACK TRANSACTION;
            SET @Mensaje = 'Conflicto de concurrencia: otro usuario ya movio el registro.';
            RETURN 52;
        END

        -- Cierra el intervalo abierto materializando minutos laborales
        UPDATE h
           SET h.FechaFin = SYSDATETIME(),
               h.MinutosLaborales = CASE WHEN @IdHorario IS NULL THEN NULL ELSE m.Minutos END
        FROM dbo.tblHistorialEstatus h
        OUTER APPLY dbo.fnMinutosLaborales(h.FechaInicio, SYSDATETIME(), ISNULL(@IdHorario, 0)) m
        WHERE h.Proceso = @Proceso
          AND h.IdRegistro = @IdRegistro
          AND h.FechaFin IS NULL;

        -- Abre el intervalo nuevo
        INSERT INTO dbo.tblHistorialEstatus (Proceso, IdRegistro, IdEstatus, Accion, FechaInicio, Usuario, Motivo)
        VALUES (@Proceso, @IdRegistro, @IdEstatusDestino, @Accion, SYSDATETIME(), @Usuario, @Motivo);

        COMMIT TRANSACTION;
        SET @Mensaje = 'Operacion realizada correctamente';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        DECLARE
            @ErrorNumber INT = ERROR_NUMBER(),
            @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE(),
            @ErrorSeverity INT = ERROR_SEVERITY(),
            @ErrorState INT = ERROR_STATE(),
            @SPName NVARCHAR(255) = OBJECT_NAME(@@PROCID),
            @Parametros NVARCHAR(MAX);
        SET @Parametros = 'Proceso=' + ISNULL(@Proceso, 'NULL') +
                          '; IdRegistro=' + ISNULL(CAST(@IdRegistro AS NVARCHAR(20)), 'NULL') +
                          '; IdEstatusActual=' + ISNULL(CAST(@IdEstatusActual AS NVARCHAR(20)), 'NULL') +
                          '; Accion=' + ISNULL(@Accion, 'NULL') +
                          '; Usuario=' + ISNULL(@Usuario, 'NULL');
        INSERT INTO dbo.tblBitacora (Usuario, Entidad, Accion, Detalle)
        VALUES (ISNULL(@Usuario, 'desconocido'), 'spCambiarEstatus', 'ERROR',
                @SPName + ' | ' + @Parametros + ' | Error #' + CAST(@ErrorNumber AS NVARCHAR(10)) + ': ' + @ErrorMessage);
        SET @Mensaje = 'Error #' + CAST(@ErrorNumber AS NVARCHAR(10)) + ': ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END
GO
PRINT 'OK: spCambiarEstatus'
GO

/* ---------- spGenerarFolio ---------- */
CREATE OR ALTER PROCEDURE dbo.spGenerarFolio
    @Serie   NVARCHAR(50),
    @Digitos INT = 4,
    @Usuario NVARCHAR(200) = NULL,
    @Folio   NVARCHAR(50) OUTPUT,
    @Mensaje NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
-- Autor: Equipo GTE
-- Fecha Creacion: 30 Jul 2026
-- Descripcion: Genera el siguiente folio de una serie de forma segura ante
--              concurrencia (ROWLOCK, UPDLOCK, HOLDLOCK - patron seccion 4.1 del
--              estandar). Folios propios de bdsGTE: GTE no depende de otras bases.
-- Describe cada uno de los parametros
-- @Serie    Serie del folio (ej. 'SOL-2026', 'GTE', 'TKT-2026'); se crea si no existe
-- @Digitos  Ancho minimo del consecutivo con ceros a la izquierda (default 4)
-- @Usuario  Usuario que solicita (auditoria de movimiento)
-- @Folio    Salida: folio generado (ej. 'SOL-2026-0001')
-- @Mensaje  Salida: mensaje de exito o error
-- =====================================================================================
-- Modificacion:                                                                 Rev_00
-- Fecha: 30 Jul 2026
-- Descripcion: Version inicial
-- =====================================================================================
    BEGIN TRY
        DECLARE @Consecutivo INT;

        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM dbo.tblFolio WITH (ROWLOCK, UPDLOCK, HOLDLOCK)
                       WHERE Serie = @Serie)
        BEGIN
            INSERT INTO dbo.tblFolio (Serie, UltimoConsecutivo) VALUES (@Serie, 0);
        END

        UPDATE dbo.tblFolio WITH (ROWLOCK, UPDLOCK, HOLDLOCK)
           SET @Consecutivo = UltimoConsecutivo = UltimoConsecutivo + 1,
               UsuarioMovto = LEFT(@Usuario, 50),
               FechaMovto = GETDATE()
         WHERE Serie = @Serie;

        COMMIT TRANSACTION;

        DECLARE @Numero NVARCHAR(20) = CAST(@Consecutivo AS NVARCHAR(20));
        SET @Folio = @Serie + N'-' +
            CASE WHEN LEN(@Numero) >= @Digitos THEN @Numero
                 ELSE RIGHT(REPLICATE(N'0', @Digitos) + @Numero, @Digitos) END;
        SET @Mensaje = 'Operacion realizada correctamente';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        DECLARE
            @ErrorNumber INT = ERROR_NUMBER(),
            @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE(),
            @ErrorSeverity INT = ERROR_SEVERITY(),
            @ErrorState INT = ERROR_STATE();
        SET @Mensaje = 'Error #' + CAST(@ErrorNumber AS NVARCHAR(10)) + ': ' + @ErrorMessage;
        RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
        RETURN -1;
    END CATCH
END
GO
PRINT 'OK: spGenerarFolio'
GO

/* ---------- spRegistrarBitacora ---------- */
CREATE OR ALTER PROCEDURE dbo.spRegistrarBitacora
    @Usuario   NVARCHAR(200),
    @Ip        NVARCHAR(50) = NULL,
    @Endpoint  NVARCHAR(500) = NULL,
    @Entidad   NVARCHAR(100),
    @IdEntidad INT = NULL,
    @Accion    NVARCHAR(100),
    @Detalle   NVARCHAR(MAX) = NULL,
    @Mensaje   NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
-- Autor: Equipo GTE
-- Fecha Creacion: 30 Jul 2026
-- Descripcion: Inserta un registro de bitacora. Se invoca con conexion de vida
--              corta para que persista aunque la transaccion de negocio haga rollback.
-- @Usuario/@Ip/@Endpoint: del AuditContext (token). @Entidad/@IdEntidad/@Accion/@Detalle: la operacion.
-- @Mensaje: salida
-- =====================================================================================
-- Modificacion:                                                                 Rev_00
-- Fecha: 30 Jul 2026
-- Descripcion: Version inicial
-- =====================================================================================
    BEGIN TRY
        INSERT INTO dbo.tblBitacora (Usuario, Ip, Endpoint, Entidad, IdEntidad, Accion, Detalle)
        VALUES (@Usuario, @Ip, @Endpoint, @Entidad, @IdEntidad, @Accion, @Detalle);
        SET @Mensaje = 'Operacion realizada correctamente';
        RETURN 0;
    END TRY
    BEGIN CATCH
        SET @Mensaje = 'Error #' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + ': ' + ERROR_MESSAGE();
        RETURN -1;
    END CATCH
END
GO
PRINT 'OK: spRegistrarBitacora'
GO

/* ---------- spSnapshotKpi ---------- */
CREATE OR ALTER PROCEDURE dbo.spSnapshotKpi
    @Fecha   DATE = NULL,
    @Mensaje NVARCHAR(4000) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
-- Autor: Equipo GTE
-- Fecha Creacion: 30 Jul 2026
-- Descripcion: Materializa el snapshot diario de KPIs en tblKpiValor (job nocturno
--              de Hangfire). Series estables: nunca se recalculan hacia atras.
-- @Fecha: dia del snapshot (NULL = hoy). @Mensaje: salida.
-- =====================================================================================
-- Modificacion:                                                                 Rev_00
-- Fecha: 30 Jul 2026
-- Descripcion: Version inicial con KPIs base (WIP y tickets abiertos)
-- =====================================================================================
    BEGIN TRY
        SET @Fecha = ISNULL(@Fecha, CAST(SYSDATETIME() AS DATE));

        BEGIN TRANSACTION;

        -- Definiciones base (idempotente)
        INSERT INTO dbo.tblKpiDefinicion (Clave, Nombre, Direccion, UsuarioRegistro)
        SELECT v.Clave, v.Nombre, v.Direccion, N'spSnapshotKpi'
        FROM (VALUES
            (N'wip.workitems',    N'Elementos de trabajo abiertos', N'Bajar'),
            (N'tickets.abiertos', N'Tickets abiertos',              N'Bajar')) v(Clave, Nombre, Direccion)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.tblKpiDefinicion d WHERE d.Clave = v.Clave);

        -- Re-emision idempotente del dia
        DELETE kv
        FROM dbo.tblKpiValor kv
        INNER JOIN dbo.tblKpiDefinicion kd ON kd.IdKpiDefinicion = kv.IdKpiDefinicion
        WHERE kv.Fecha = @Fecha AND kv.Alcance = N'global'
          AND kd.Clave IN (N'wip.workitems', N'tickets.abiertos');

        INSERT INTO dbo.tblKpiValor (IdKpiDefinicion, Fecha, Alcance, Valor)
        SELECT kd.IdKpiDefinicion, @Fecha, N'global',
               (SELECT COUNT(*) FROM dbo.tblWorkItem w
                WHERE w.Activo = 1 AND w.IdEstatusWorkItem NOT IN (6, 7))   -- 6 Terminado, 7 Cancelado
        FROM dbo.tblKpiDefinicion kd WHERE kd.Clave = N'wip.workitems';

        INSERT INTO dbo.tblKpiValor (IdKpiDefinicion, Fecha, Alcance, Valor)
        SELECT kd.IdKpiDefinicion, @Fecha, N'global',
               (SELECT COUNT(*) FROM dbo.tblTicket t
                WHERE t.Activo = 1 AND t.IdEstatusTicket NOT IN (6))        -- 6 Cerrado
        FROM dbo.tblKpiDefinicion kd WHERE kd.Clave = N'tickets.abiertos';

        COMMIT TRANSACTION;
        SET @Mensaje = 'Operacion realizada correctamente';
        RETURN 0;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        SET @Mensaje = 'Error #' + CAST(ERROR_NUMBER() AS NVARCHAR(10)) + ': ' + ERROR_MESSAGE();
        RAISERROR(@Mensaje, 16, 1);
        RETURN -1;
    END CATCH
END
GO
PRINT 'OK: spSnapshotKpi'
GO

/* ---------- trWorkItemHistorialCampo ----------
   Red de seguridad de auditoria por debajo de la API: captura cambios de
   campos sensibles de tblWorkItem hacia tblHistorialCampo.                 */
CREATE OR ALTER TRIGGER dbo.trWorkItemHistorialCampo
ON dbo.tblWorkItem
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM inserted)
        RETURN;

    INSERT INTO dbo.tblHistorialCampo (Entidad, IdEntidad, Campo, ValorAnterior, ValorNuevo, Usuario)
    SELECT N'WorkItem', i.IdWorkItem, c.Campo, c.ValorAnterior, c.ValorNuevo,
           COALESCE(i.UsuarioMovto, i.UsuarioRegistro)
    FROM inserted i
    INNER JOIN deleted d ON d.IdWorkItem = i.IdWorkItem
    CROSS APPLY (VALUES
        (N'IdAsignado',         CAST(d.IdAsignado         AS NVARCHAR(50)),  CAST(i.IdAsignado         AS NVARCHAR(50))),
        (N'FechaCompromiso',    CONVERT(NVARCHAR(30), d.FechaCompromiso, 126), CONVERT(NVARCHAR(30), i.FechaCompromiso, 126)),
        (N'IdPrioridad',        CAST(d.IdPrioridad        AS NVARCHAR(50)),  CAST(i.IdPrioridad        AS NVARCHAR(50))),
        (N'IdComplejidad',      CAST(d.IdComplejidad      AS NVARCHAR(50)),  CAST(i.IdComplejidad      AS NVARCHAR(50))),
        (N'MinutosPresupuesto', CAST(d.MinutosPresupuesto AS NVARCHAR(50)),  CAST(i.MinutosPresupuesto AS NVARCHAR(50))),
        (N'IdSprint',           CAST(d.IdSprint           AS NVARCHAR(50)),  CAST(i.IdSprint           AS NVARCHAR(50))),
        (N'IdRelease',          CAST(d.IdRelease          AS NVARCHAR(50)),  CAST(i.IdRelease          AS NVARCHAR(50)))
        ) c(Campo, ValorAnterior, ValorNuevo)
    WHERE ISNULL(c.ValorAnterior, N'') <> ISNULL(c.ValorNuevo, N'');
END
GO
PRINT 'OK: trWorkItemHistorialCampo'
GO

/* ---------- vwTiempoInvertido ----------
   Tiempo invertido por WorkItem = suma de minutos laborales materializados
   de los intervalos cerrados en estatus En Proceso (Id 2). El intervalo
   abierto lo agrega el backend en runtime si se requiere.                  */
CREATE OR ALTER VIEW dbo.vwTiempoInvertido
AS
SELECT h.IdRegistro AS IdWorkItem,
       SUM(h.MinutosLaborales) AS MinutosInvertidos
FROM dbo.tblHistorialEstatus h
WHERE h.Proceso = N'WorkItem'
  AND h.IdEstatus = 2               -- En Proceso (contrato de seeds del script 01)
  AND h.MinutosLaborales IS NOT NULL
GROUP BY h.IdRegistro
GO
PRINT 'OK: vwTiempoInvertido'
GO

/* ---------- vwBandejaTrabajo ----------
   Proyeccion de la bandeja principal. Sin funciones escalares por fila:
   consume los minutos ya materializados (corrige el cuello de botella
   estructural de vw_Detalle/vw_Tiempos del GT).                            */
CREATE OR ALTER VIEW dbo.vwBandejaTrabajo
AS
SELECT wi.IdWorkItem,
       wi.Folio,
       twi.Nombre                    AS Tipo,
       wi.Titulo,
       p.Clave                       AS ClaveProyecto,
       p.Nombre                      AS Proyecto,
       p.EsMantenimiento,
       wi.IdEstatusWorkItem,
       e.Descripcion                 AS Estatus,
       wi.IdPrioridad,
       pr.Nombre                     AS Prioridad,
       wi.IdAsignado,
       ua.Nombre                     AS Asignado,
       us.Nombre                     AS Solicitante,
       wi.IdSprint,
       sp.Nombre                     AS Sprint,
       wi.PuntosHistoria,
       wi.MinutosPresupuesto,
       ti.MinutosInvertidos,
       wi.FechaCompromiso,
       wi.FechaInicio,
       wi.FechaFin,
       wi.FechaRegistro,
       CASE WHEN wi.FechaCompromiso < SYSDATETIME()
             AND wi.IdEstatusWorkItem NOT IN (6, 7)   -- 6 Terminado, 7 Cancelado
            THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS EsVencida,
       (SELECT COUNT(*) FROM dbo.tblRevision r
        WHERE r.IdWorkItem = wi.IdWorkItem AND r.Corregido = 0 AND r.Activo = 1) AS RevisionesPendientes
FROM dbo.tblWorkItem wi
INNER JOIN dbo.tblTipoWorkItem twi ON twi.Id = wi.IdTipoWorkItem
INNER JOIN dbo.tblProyecto p ON p.IdProyecto = wi.IdProyecto
INNER JOIN dbo.tblEstatusWorkItem e ON e.Id = wi.IdEstatusWorkItem
INNER JOIN dbo.tblPrioridad pr ON pr.Id = wi.IdPrioridad
LEFT JOIN dbo.tblUsuario ua ON ua.IdUsuario = wi.IdAsignado
LEFT JOIN dbo.tblUsuario us ON us.IdUsuario = wi.IdSolicitante
LEFT JOIN dbo.tblSprint sp ON sp.IdSprint = wi.IdSprint
LEFT JOIN dbo.vwTiempoInvertido ti ON ti.IdWorkItem = wi.IdWorkItem
WHERE wi.Activo = 1
GO
PRINT 'OK: vwBandejaTrabajo'
GO
PRINT '===== Script ejecutado correctamente ====='
GO
