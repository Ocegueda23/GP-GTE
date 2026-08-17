USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      24_2026-08-06_UPDATE_tblProyecto_ClaveMigracionGT.sql
   Autor:       Equipo GTE
   Descripcion: Corrige tblProyecto.Clave en los proyectos que trajo la
                migracion del GT (scripts 03/05 de Migracion, 2026-08-01):
                el script 03 poblo Clave = LEFT(NombreProyectoOrigen, 20)
                porque el origen no tenia un codigo corto propio, asi que
                a los proyectos con nombre largo les quedo el nombre
                completo (o cortado a la mitad de una palabra) como
                Clave, en vez de un codigo corto (ej. "MODULO DE
                ADMINISTRA" en vez de "MAA"). Mapeo Clave vieja -> Clave
                nueva definido y validado por el negocio (34 proyectos,
                lista cerrada, NO se procesa "todo lo migrado" a ciegas).

                Por que un UPDATE directo y no la app: ProyectoEditarRequest
                (PUT /api/v1/proyectos/{id}) no expone Clave -- solo se fija
                al crear (ProyectoCrearRequest). No existe hoy un camino de
                aplicacion para corregirla, un script es el unico camino.

                Efectos colaterales cubiertos aqui (Clave es UNIQUE y es la
                Serie de folios de estos proyectos):
                - tblFolio.Serie: si el proyecto ya genero folios bajo la
                  Serie vieja (la propia Clave, WorkItem), se renombra la
                  fila de tblFolio a la Serie nueva para que la numeracion
                  consecutiva continue donde se quedo (decision del
                  negocio: NO arrancar en 0).
                - tblWorkItem.Folio (CrearWorkItemCommand.cs: Serie =
                  proyecto.Clave, aplica igual a tareas raiz y subtareas,
                  ambas viven en tblWorkItem): folios YA EMITIDOS SI se
                  reescriben -- se sustituye solo el prefijo (los primeros
                  LEN(ClaveVieja) caracteres) por la Clave nueva, el resto
                  del folio ("-NNNN") queda intacto. Pedido explicito del
                  negocio 2026-08-06.
                - tblRelease.Folio + su fila de tblFolio (Serie =
                  "REL-Clave-Anio", ReleaseCommands.cs -- una fila de
                  tblFolio POR ANIO, no una sola): mismo tratamiento que
                  WorkItem, se reescribe el tramo Clave dentro del patron
                  "REL-Clave-Anio-NNN" sin tocar "REL-"/Anio/el consecutivo.
                - Tickets/Incidentes/Solicitudes/folio propio de Proyecto
                  usan una Serie FIJA sin relacion con Clave (TKT-Anio,
                  INC-Anio, SOL-Anio, PRY-Anio -- ver CrearTicketCommand.cs,
                  CrearIncidenteCommand.cs, CrearSolicitudCommand.cs,
                  CambiarEstatusProyectoCommand.cs): no les aplica nada de
                  este script, no hay folio que corregir ahi.
                - ClaveProyecto en vwBandejaTrabajo y las consultas es
                  "p.Clave" derivado; se actualiza solo, no se toca aparte.

                Blindaje (nada se asume, todo se valida antes de tocar):
                - Si la Clave vieja de una fila del mapeo ya no existe (ya
                  se corrio antes, o el texto no calza con lo real en BD),
                  esa fila se SALTA con SKIP, no se aborta el script.
                - Si la Clave nueva ya la trae OTRO proyecto (conflicto
                  real con UQ_tblProyecto_Clave), esa fila se SALTA con
                  CONFLICTO para resolver a mano -- NO se fuerza.
                - Pares donde Clave vieja = Clave nueva (ya estaba bien)
                  se saltan como SKIP-NOOP, no se tocan.
                - Idempotente: una segunda corrida es SKIP en las 34 filas
                  (la Clave vieja ya no existe, quedo renombrada).

                Probar primero contra LocalDB/preprod con datos migrados
                reales antes de correr contra produccion (SRVPROD\NASA).
   ===================================================================== */
BEGIN TRY

    -- =================================================================
    -- Mapeo cerrado (34 proyectos), decidido y validado por el negocio.
    -- =================================================================
    DECLARE @Mapeo TABLE (ClaveVieja NVARCHAR(20) NOT NULL, ClaveNueva NVARCHAR(20) NOT NULL);
    INSERT INTO @Mapeo (ClaveVieja, ClaveNueva) VALUES
        (N'SERVICE DESK',          N'SERVICE DESK'),
        (N'AUDITORIA TI',          N'AD'),
        (N'BITACORA RESPALDOS',    N'BT'),
        (N'MCI',                   N'MCI'),
        (N'ADM-BALTICO',           N'AB'),
        (N'MÓDULO DE ADMINISTRA',  N'MAA'),
        (N'MÓDULO DE SEGURIDAD',   N'MS'),
        (N'MÓDULO DE USUARIOS',    N'MU'),
        (N'MOTOR DE FORMULARIOS',  N'MF'),
        (N'REPOSITORIO DE CONEX',  N'RC'),
        (N'CALCULADORA DE COMIS',  N'CC'),
        (N'MACRO ADMIN PROYECTO',  N'AP'),
        (N'REPOSITORIO DE CÓDIG',  N'RCO'),
        (N'INVENTORY',             N'INV'),
        (N'FINANZAS',              N'FIN'),
        (N'INFRA DESARROLLO',      N'INFRA'),
        (N'INTERFLO-API',          N'IAPI'),
        (N'INDICADORES',           N'IND'),
        (N'REDES',                 N'REDES'),
        (N'INVENTARIO DE DOCUME',  N'ID'),
        (N'2CAD',                  N'2CAD'),
        (N'REESTRUCTURACIÓN TI',   N'RTI'),
        (N'PIPELINE VENTAS',       N'PV'),
        (N'EXTRALABORAL',          N'EXTRALABORAL'),
        (N'MANTENIMIENTO',         N'MANTENIMIENTO'),
        (N'INTRANET',              N'INTRANET'),
        (N'PLANTILLA ANGULAR',     N'PA'),
        (N'HSIS',                  N'HSIS'),
        (N'STS',                   N'STS'),
        (N'PORTAL WEB',            N'PW'),
        (N'CAPACITACIÓN',          N'CAPACITACIÓN'),
        (N'HELPDESK',              N'HELPDESK'),
        (N'AUTOMATIZACIÓN DE CO',  N'AC'),
        (N'COMISIONES',            N'COM');

    DECLARE @UsuarioEjecuta NVARCHAR(50) = N'correccion-clave-gt';
    DECLARE @ClaveVieja NVARCHAR(20), @ClaveNueva NVARCHAR(20);
    DECLARE @IdProyecto INT, @NEncontrados INT, @IdProyectoConflicto INT;
    DECLARE @NOk INT = 0, @NNoop INT = 0, @NNoEncontrado INT = 0, @NConflicto INT = 0;
    DECLARE @NFolioOk INT = 0, @NFolioConflicto INT = 0, @NFolioSinDato INT = 0;
    DECLARE @NWiCandidatos INT, @NWiActualizados INT, @NWiOkTotal INT = 0, @NWiConflictoTotal INT = 0;
    DECLARE @NFolioRelCandidatos INT, @NFolioRelActualizados INT, @NFolioRelOkTotal INT = 0, @NFolioRelConflictoTotal INT = 0;
    DECLARE @NRelCandidatos INT, @NRelActualizados INT, @NRelOkTotal INT = 0, @NRelConflictoTotal INT = 0;

    DECLARE curMapeo CURSOR LOCAL FAST_FORWARD FOR
        SELECT ClaveVieja, ClaveNueva FROM @Mapeo ORDER BY ClaveVieja;
    OPEN curMapeo;
    FETCH NEXT FROM curMapeo INTO @ClaveVieja, @ClaveNueva;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @ClaveVieja = @ClaveNueva
        BEGIN
            PRINT 'SKIP-NOOP: ''' + @ClaveVieja + ''' ya tiene la clave correcta, no se toca';
            SET @NNoop = @NNoop + 1;
        END
        ELSE
        BEGIN
            SELECT @NEncontrados = COUNT(*), @IdProyecto = MIN(IdProyecto)
            FROM dbo.tblProyecto WHERE Clave = @ClaveVieja;

            IF @NEncontrados = 0
            BEGIN
                PRINT 'SKIP: no se encontro ningun proyecto con Clave = ''' + @ClaveVieja + ''' (ya corregido antes, o el texto no calza con lo real en BD)';
                SET @NNoEncontrado = @NNoEncontrado + 1;
            END
            ELSE IF @NEncontrados > 1
            BEGIN
                PRINT 'SKIP: hay MAS DE UN proyecto con Clave = ''' + @ClaveVieja + ''' (dato inconsistente, revisar a mano antes de continuar)';
                SET @NNoEncontrado = @NNoEncontrado + 1;
            END
            ELSE IF EXISTS (SELECT 1 FROM dbo.tblProyecto WHERE Clave = @ClaveNueva)
            BEGIN
                SELECT @IdProyectoConflicto = IdProyecto FROM dbo.tblProyecto WHERE Clave = @ClaveNueva;
                PRINT 'CONFLICTO: la clave nueva ''' + @ClaveNueva + ''' ya la tiene el proyecto IdProyecto=' + CAST(@IdProyectoConflicto AS NVARCHAR(10))
                    + ' -- NO se aplico el cambio para ''' + @ClaveVieja + ''' (IdProyecto=' + CAST(@IdProyecto AS NVARCHAR(10)) + '), resolver a mano';
                SET @NConflicto = @NConflicto + 1;
            END
            ELSE
            BEGIN
                UPDATE dbo.tblProyecto
                   SET Clave = @ClaveNueva,
                       UsuarioMovto = @UsuarioEjecuta,
                       FechaMovto = GETDATE()
                 WHERE IdProyecto = @IdProyecto;

                PRINT 'OK: proyecto IdProyecto=' + CAST(@IdProyecto AS NVARCHAR(10)) + ' renombrado de ''' + @ClaveVieja + ''' a ''' + @ClaveNueva + '''';
                SET @NOk = @NOk + 1;

                -- ---------------------------------------------------------
                -- tblFolio: migrar el consecutivo de la Serie vieja a la
                -- nueva para no perder la numeracion (decision del negocio).
                -- ---------------------------------------------------------
                IF NOT EXISTS (SELECT 1 FROM dbo.tblFolio WHERE Serie = @ClaveVieja)
                BEGIN
                    PRINT '  INFO-FOLIO: ''' + @ClaveVieja + ''' aun no habia generado ningun folio, nada que migrar en tblFolio';
                    SET @NFolioSinDato = @NFolioSinDato + 1;
                END
                ELSE IF EXISTS (SELECT 1 FROM dbo.tblFolio WHERE Serie = @ClaveNueva)
                BEGIN
                    PRINT '  CONFLICTO-FOLIO: ya existe una Serie ''' + @ClaveNueva + ''' en tblFolio -- NO se migro el consecutivo de ''' + @ClaveVieja + ''' (folios nuevos de este proyecto arrancaran numeracion aparte hasta resolverlo a mano)';
                    SET @NFolioConflicto = @NFolioConflicto + 1;
                END
                ELSE
                BEGIN
                    UPDATE dbo.tblFolio
                       SET Serie = @ClaveNueva,
                           UsuarioMovto = @UsuarioEjecuta,
                           FechaMovto = GETDATE()
                     WHERE Serie = @ClaveVieja;

                    PRINT '  OK-FOLIO: consecutivo de folios migrado de Serie ''' + @ClaveVieja + ''' a ''' + @ClaveNueva + '''';
                    SET @NFolioOk = @NFolioOk + 1;
                END

                -- ---------------------------------------------------------
                -- tblWorkItem.Folio: reescribir los folios YA EMITIDOS bajo
                -- la Serie vieja (folio = Serie + '-' + digitos, tal cual lo
                -- arma spGenerarFolio). Cubre tareas raiz y subtareas por
                -- igual (ambas viven en tblWorkItem). Solo se toca el
                -- prefijo (los primeros LEN(ClaveVieja) caracteres); el
                -- resto del folio ("-NNNN") se preserva intacto.
                -- ---------------------------------------------------------
                SELECT @NWiCandidatos = COUNT(*)
                FROM dbo.tblWorkItem
                WHERE IdProyecto = @IdProyecto AND Folio LIKE @ClaveVieja + N'-%';

                UPDATE wi
                   SET Folio = @ClaveNueva + SUBSTRING(wi.Folio, LEN(@ClaveVieja) + 1, 4000),
                       UsuarioMovto = @UsuarioEjecuta,
                       FechaMovto = GETDATE()
                FROM dbo.tblWorkItem wi
                WHERE wi.IdProyecto = @IdProyecto
                  AND wi.Folio LIKE @ClaveVieja + N'-%'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.tblWorkItem wi2
                      WHERE wi2.Folio = @ClaveNueva + SUBSTRING(wi.Folio, LEN(@ClaveVieja) + 1, 4000)
                        AND wi2.IdWorkItem <> wi.IdWorkItem
                  );
                SET @NWiActualizados = @@ROWCOUNT;

                IF @NWiCandidatos = 0
                    PRINT '  INFO-WORKITEM: ''' + @ClaveVieja + ''' no tenia folios emitidos en tblWorkItem, nada que reescribir';
                ELSE IF @NWiActualizados < @NWiCandidatos
                    PRINT '  ATENCION-WORKITEM: ' + CAST(@NWiActualizados AS NVARCHAR(10)) + ' de ' + CAST(@NWiCandidatos AS NVARCHAR(10))
                        + ' folios de ''' + @ClaveVieja + ''' se reescribieron; ' + CAST(@NWiCandidatos - @NWiActualizados AS NVARCHAR(10))
                        + ' se saltaron por chocar con un folio ya existente bajo ''' + @ClaveNueva + ''' -- revisar a mano';
                ELSE
                    PRINT '  OK-WORKITEM: ' + CAST(@NWiActualizados AS NVARCHAR(10)) + ' folios reescritos de ''' + @ClaveVieja + ''' a ''' + @ClaveNueva + '''';

                SET @NWiOkTotal = @NWiOkTotal + @NWiActualizados;
                SET @NWiConflictoTotal = @NWiConflictoTotal + (@NWiCandidatos - @NWiActualizados);

                -- ---------------------------------------------------------
                -- tblFolio: consecutivo(s) de Release (Serie = "REL-Clave-
                -- Anio", UNA FILA POR ANIO -- distinta de la Serie propia
                -- del proyecto ya migrada arriba). Se renombra el tramo
                -- Clave dentro del patron, preservando "REL-"/Anio.
                -- ---------------------------------------------------------
                SELECT @NFolioRelCandidatos = COUNT(*)
                FROM dbo.tblFolio
                WHERE Serie LIKE N'REL-' + @ClaveVieja + N'-%';

                UPDATE f
                   SET Serie = N'REL-' + @ClaveNueva + SUBSTRING(f.Serie, LEN(N'REL-' + @ClaveVieja) + 1, 4000),
                       UsuarioMovto = @UsuarioEjecuta,
                       FechaMovto = GETDATE()
                FROM dbo.tblFolio f
                WHERE f.Serie LIKE N'REL-' + @ClaveVieja + N'-%'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.tblFolio f2
                      WHERE f2.Serie = N'REL-' + @ClaveNueva + SUBSTRING(f.Serie, LEN(N'REL-' + @ClaveVieja) + 1, 4000)
                        AND f2.IdFolio <> f.IdFolio
                  );
                SET @NFolioRelActualizados = @@ROWCOUNT;

                IF @NFolioRelCandidatos > 0 AND @NFolioRelActualizados < @NFolioRelCandidatos
                    PRINT '  ATENCION-FOLIO-REL: ' + CAST(@NFolioRelCandidatos - @NFolioRelActualizados AS NVARCHAR(10))
                        + ' fila(s) de tblFolio de Release para ''' + @ClaveVieja + ''' no se pudieron renombrar por conflicto -- revisar a mano';
                ELSE IF @NFolioRelActualizados > 0
                    PRINT '  OK-FOLIO-REL: ' + CAST(@NFolioRelActualizados AS NVARCHAR(10)) + ' fila(s) de tblFolio de Release migradas de ''' + @ClaveVieja + ''' a ''' + @ClaveNueva + '''';

                SET @NFolioRelOkTotal = @NFolioRelOkTotal + @NFolioRelActualizados;
                SET @NFolioRelConflictoTotal = @NFolioRelConflictoTotal + (@NFolioRelCandidatos - @NFolioRelActualizados);

                -- ---------------------------------------------------------
                -- tblRelease.Folio: mismo tratamiento que tblWorkItem.Folio,
                -- reescribe el tramo Clave dentro de "REL-Clave-Anio-NNN".
                -- ---------------------------------------------------------
                SELECT @NRelCandidatos = COUNT(*)
                FROM dbo.tblRelease
                WHERE IdProyecto = @IdProyecto AND Folio LIKE N'REL-' + @ClaveVieja + N'-%';

                UPDATE r
                   SET Folio = N'REL-' + @ClaveNueva + SUBSTRING(r.Folio, LEN(N'REL-' + @ClaveVieja) + 1, 4000),
                       UsuarioMovto = @UsuarioEjecuta,
                       FechaMovto = GETDATE()
                FROM dbo.tblRelease r
                WHERE r.IdProyecto = @IdProyecto
                  AND r.Folio LIKE N'REL-' + @ClaveVieja + N'-%'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.tblRelease r2
                      WHERE r2.Folio = N'REL-' + @ClaveNueva + SUBSTRING(r.Folio, LEN(N'REL-' + @ClaveVieja) + 1, 4000)
                        AND r2.IdRelease <> r.IdRelease
                  );
                SET @NRelActualizados = @@ROWCOUNT;

                IF @NRelCandidatos = 0
                    PRINT '  INFO-RELEASE: ''' + @ClaveVieja + ''' no tenia releases emitidos, nada que reescribir';
                ELSE IF @NRelActualizados < @NRelCandidatos
                    PRINT '  ATENCION-RELEASE: ' + CAST(@NRelActualizados AS NVARCHAR(10)) + ' de ' + CAST(@NRelCandidatos AS NVARCHAR(10))
                        + ' folios de Release de ''' + @ClaveVieja + ''' se reescribieron; ' + CAST(@NRelCandidatos - @NRelActualizados AS NVARCHAR(10))
                        + ' se saltaron por conflicto -- revisar a mano';
                ELSE
                    PRINT '  OK-RELEASE: ' + CAST(@NRelActualizados AS NVARCHAR(10)) + ' folios de Release reescritos de ''' + @ClaveVieja + ''' a ''' + @ClaveNueva + '''';

                SET @NRelOkTotal = @NRelOkTotal + @NRelActualizados;
                SET @NRelConflictoTotal = @NRelConflictoTotal + (@NRelCandidatos - @NRelActualizados);
            END
        END

        FETCH NEXT FROM curMapeo INTO @ClaveVieja, @ClaveNueva;
    END
    CLOSE curMapeo;
    DEALLOCATE curMapeo;

    PRINT '===== RESUMEN tblProyecto ====='
    PRINT 'Renombrados OK          : ' + CAST(@NOk AS NVARCHAR(10));
    PRINT 'Ya estaban correctos    : ' + CAST(@NNoop AS NVARCHAR(10));
    PRINT 'No encontrados          : ' + CAST(@NNoEncontrado AS NVARCHAR(10));
    PRINT 'Conflictos (sin aplicar): ' + CAST(@NConflicto AS NVARCHAR(10));
    PRINT '===== RESUMEN tblFolio (consecutivo propio del proyecto) ====='
    PRINT 'Consecutivo migrado OK  : ' + CAST(@NFolioOk AS NVARCHAR(10));
    PRINT 'Conflictos (sin migrar) : ' + CAST(@NFolioConflicto AS NVARCHAR(10));
    PRINT 'Sin folios que migrar   : ' + CAST(@NFolioSinDato AS NVARCHAR(10));
    PRINT '===== RESUMEN tblWorkItem (folios reescritos, tareas raiz + subtareas) ====='
    PRINT 'Folios reescritos OK    : ' + CAST(@NWiOkTotal AS NVARCHAR(10));
    PRINT 'Folios en conflicto     : ' + CAST(@NWiConflictoTotal AS NVARCHAR(10));
    PRINT '===== RESUMEN tblFolio (consecutivo de Release, REL-Clave-Anio) ====='
    PRINT 'Filas migradas OK       : ' + CAST(@NFolioRelOkTotal AS NVARCHAR(10));
    PRINT 'Conflictos (sin migrar) : ' + CAST(@NFolioRelConflictoTotal AS NVARCHAR(10));
    PRINT '===== RESUMEN tblRelease (folios reescritos) ====='
    PRINT 'Folios reescritos OK    : ' + CAST(@NRelOkTotal AS NVARCHAR(10));
    PRINT 'Folios en conflicto     : ' + CAST(@NRelConflictoTotal AS NVARCHAR(10));

    IF @NConflicto > 0 OR @NNoEncontrado > 0 OR @NWiConflictoTotal > 0 OR @NFolioRelConflictoTotal > 0 OR @NRelConflictoTotal > 0
        PRINT '===== ATENCION: revisar los SKIP/CONFLICTO/ATENCION impresos arriba antes de considerar esto cerrado ====='

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'curMapeo') >= -1
    BEGIN
        CLOSE curMapeo;
        DEALLOCATE curMapeo;
    END
    PRINT '===== ERROR - Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO

-- =====================================================================
-- Verificacion posterior (solo lectura): confirmar el estado final de
-- los 34 proyectos por su Clave NUEVA.
-- =====================================================================
DECLARE @ClavesNuevas TABLE (Clave NVARCHAR(20));
INSERT INTO @ClavesNuevas VALUES
    (N'SERVICE DESK'), (N'AD'), (N'BT'), (N'MCI'), (N'AB'), (N'MAA'), (N'MS'), (N'MU'),
    (N'MF'), (N'RC'), (N'CC'), (N'AP'), (N'RCO'), (N'INV'), (N'FIN'), (N'INFRA'), (N'IAPI'),
    (N'IND'), (N'REDES'), (N'ID'), (N'2CAD'), (N'RTI'), (N'PV'), (N'EXTRALABORAL'),
    (N'MANTENIMIENTO'), (N'INTRANET'), (N'PA'), (N'HSIS'), (N'STS'), (N'PW'),
    (N'CAPACITACIÓN'), (N'HELPDESK'), (N'AC'), (N'COM');

SELECT IdProyecto, Clave, Nombre, UsuarioMovto, FechaMovto
FROM dbo.tblProyecto
WHERE Clave IN (SELECT Clave FROM @ClavesNuevas)
ORDER BY Clave;

-- Folios de WorkItem (tareas raiz + subtareas) ya bajo la Clave nueva
SELECT p.IdProyecto, p.Clave, COUNT(wi.IdWorkItem) AS TotalWorkItemsConFolioNuevo
FROM dbo.tblProyecto p
JOIN @ClavesNuevas cn ON cn.Clave = p.Clave
LEFT JOIN dbo.tblWorkItem wi ON wi.IdProyecto = p.IdProyecto AND wi.Folio LIKE p.Clave + N'-%'
GROUP BY p.IdProyecto, p.Clave
ORDER BY p.Clave;

-- Releases ya bajo la Clave nueva
SELECT p.IdProyecto, p.Clave, COUNT(r.IdRelease) AS TotalReleasesConFolioNuevo
FROM dbo.tblProyecto p
JOIN @ClavesNuevas cn ON cn.Clave = p.Clave
LEFT JOIN dbo.tblRelease r ON r.IdProyecto = p.IdProyecto AND r.Folio LIKE N'REL-' + p.Clave + N'-%'
GROUP BY p.IdProyecto, p.Clave
ORDER BY p.Clave;
GO
