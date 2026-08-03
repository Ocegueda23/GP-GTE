USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      05_2026-08-01_SCRIPT_bdsGTE_MigracionNucleo.sql
   Autor:       Equipo GTE
   Descripcion: Migracion B3, bloque Nucleo (el bloque grande): tblTareas
                + tblSubtareas -> tblWorkItem (padre/hijo) + tblRegistroTiempo,
                tblHistorialEstatus -> tblHistorialEstatus (Proceso='WorkItem').
                tblRevisiones/tblHistorialRevision estan VACIAS en el
                origen (0 filas) -- no hay nada que migrar ahi en esta
                corrida, se documenta como hallazgo, no como pendiente.

                Requiere que ya hayan corrido: 01 (ALTER Locacion/IdEquipo),
                02 (Usuarios), 03 (Catalogos: Proyectos/Complejidad/
                Equipos/Festivos), 04 (Solicitudes desde EDM).

                Reglas de mapeo (decididas y verificadas contra datos
                reales en la Fase A de esta migracion):
                - Excluye la fila basura tblTareas.id=1 (Registro IS NULL,
                  plantilla de prueba con valores literales = nombres de columna).
                - Folio: spGenerarFolio con Serie = Clave del proyecto
                  resuelto (mismo patron que CrearWorkItemCommand.cs) --
                  NO un folio fijo 'GTE'.
                - ClaveJira: Id2 del origen si existe (casi nunca, 1/4054),
                  si no 'GT-<id>' / 'GT-SUB-<Id>' -- esto es lo que hace
                  el script repetible.
                - IdTipoWorkItem: Soporte(8) si el proyecto es categoria
                  TI, Tarea(4) en cualquier otro caso (no hay una senal
                  de tipo mas fina y confiable en el origen).
                - IdProyecto sin match (3 valores basura en tblTareas.Proyecto,
                  ~7 tareas) cae en un proyecto de rescate 'SIN-PROY-GT'
                  (NOT NULL exige algun proyecto valido).
                - IdEquipo: por Division via el catalogo de Equipos migrado.
                - Locacion: solo si el proyecto es categoria TI, tomando
                  el valor de Release del origen.
                - IdAsignado/IdSolicitante: match por Dominio, con
                  redireccion especial 'Ana.Viramontes'->'aviramontes'
                  (ella ya existia en bdsGTE con otro Dominio). David
                  Altamirano no se migro -> sus asignaciones quedan NULL.
                - IdSolicitud: enlaza al EDM migrado como Solicitud
                  (script 04) cuando tblTareas.EDM no es NULL.
                - Comentarios (RTF o texto plano) -> Descripcion HTML:
                  el texto plano se envuelve en <p> con escape basico;
                  el RTF crudo NO se parsea en esta pasada (fuera de
                  alcance de tiempo razonable para este corte) -- se
                  preserva integro escapado dentro de <pre> con una nota
                  explicita de que la conversion visual queda pendiente.
                  Cero perdida de informacion, solo pendiente de formato.
                - tblRegistroTiempo: se omite si el asignado no resuelve
                  a un usuario migrado (afecta solo a tareas de David
                  Altamirano) o si la duracion es 0.
                - tblHistorialEstatus: MinutosLaborales se deja NULL para
                  las filas migradas (no se reproduce fnMinutosLaborales
                  historico en esta pasada -- ver reporte de excepciones
                  al final del script).

                CONFIGURACION DE ENTORNO: igual que los scripts 03/04, el
                nombre de la base origen se ajusta en la variable
                @BaseOrigen de abajo (default 'bdsApollo'), via sinonimos
                temporales (se eliminan antes del COMMIT).
   ===================================================================== */
BEGIN TRY

    -- =================================================================
    -- Sinonimos hacia la base origen (unico lugar donde se ajusta el
    -- nombre de la base restaurada; se eliminan antes del COMMIT)
    -- =================================================================
    DECLARE @BaseOrigen SYSNAME = N'bdsApollo';  -- <-- AJUSTAR AQUI si el entorno usa otro nombre
    DECLARE @SqlSinonimo NVARCHAR(MAX);
    DECLARE @NombresSinonimos TABLE (Sinonimo SYSNAME, TablaOrigen SYSNAME);
    INSERT INTO @NombresSinonimos VALUES
        (N'OrigenTareas', N'tblTareas'), (N'OrigenSubtareas', N'tblSubtareas'),
        (N'OrigenHistorialEstatus', N'tblHistorialEstatus'), (N'OrigenComplejidadNucleo', N'tblComplejidad');
    DECLARE @Sinonimo SYSNAME, @TablaOrigen SYSNAME;
    DECLARE curSin CURSOR LOCAL FAST_FORWARD FOR SELECT Sinonimo, TablaOrigen FROM @NombresSinonimos;
    OPEN curSin;
    FETCH NEXT FROM curSin INTO @Sinonimo, @TablaOrigen;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.synonyms WHERE name = @Sinonimo)
        BEGIN
            SET @SqlSinonimo = N'DROP SYNONYM dbo.' + QUOTENAME(@Sinonimo);
            EXEC sp_executesql @SqlSinonimo;
        END
        SET @SqlSinonimo = N'CREATE SYNONYM dbo.' + QUOTENAME(@Sinonimo) + N' FOR ' + QUOTENAME(@BaseOrigen) + N'.dbo.' + QUOTENAME(@TablaOrigen);
        EXEC sp_executesql @SqlSinonimo;
        FETCH NEXT FROM curSin INTO @Sinonimo, @TablaOrigen;
    END
    CLOSE curSin;
    DEALLOCATE curSin;

    -- =================================================================
    -- Proyecto de rescate para tareas cuyo Proyecto de origen no
    -- matcheo ningun proyecto real (basura de captura, ~7 tareas)
    -- =================================================================
    DECLARE @IdProyectoFallback INT;
    IF NOT EXISTS (SELECT 1 FROM dbo.tblProyecto WHERE Clave = N'SIN-PROY-GT')
    BEGIN
        INSERT INTO dbo.tblProyecto (Clave, Nombre, IdCategoriaProyecto, IdEstatusProyecto, FechaRegistro, UsuarioRegistro, Activo)
        VALUES (N'SIN-PROY-GT', N'Sin proyecto identificado (migracion GT)', 1, 3, SYSDATETIME(), N'migracion-gt', 1)
        PRINT 'OK: proyecto de rescate SIN-PROY-GT creado'
    END
    ELSE
        PRINT 'SKIP: proyecto de rescate SIN-PROY-GT ya existe'
    SELECT @IdProyectoFallback = IdProyecto FROM dbo.tblProyecto WHERE Clave = N'SIN-PROY-GT';

    -- =================================================================
    -- Mapeos de catalogo (chicos, se resuelven una sola vez)
    -- =================================================================
    DECLARE @MapeoEstatus TABLE (EstatusOrigen NVARCHAR(50), IdEstatusDestino INT);
    INSERT INTO @MapeoEstatus VALUES
        (N'Pendiente', 1), (N'En Proceso', 2), (N'Suspendido', 5), (N'Terminado', 6);

    DECLARE @MapeoEquipo TABLE (Division NVARCHAR(25), IdEquipo INT);
    INSERT INTO @MapeoEquipo (Division, IdEquipo)
    SELECT v.Division, e.IdEquipo
    FROM (VALUES (N'DES', N'Desarrollo'), (N'INFRA', N'Infraestructura'),
                 (N'STS', N'Servicios Tecnologicos y Soporte'),
                 (N'AD', N'Analisis de Datos'), (N'GER', N'Gerencia')) v(Division, NombreEquipo)
    JOIN dbo.tblEquipo e ON e.Nombre = v.NombreEquipo;

    -- =================================================================
    -- Tabla puente: tarea origen -> WorkItem nuevo (alimenta subtareas
    -- e historial mas abajo, evita recalcular la resolucion dos veces)
    -- =================================================================
    CREATE TABLE #MapaTareaWorkItem (
        IdTareaOrigen INT PRIMARY KEY,
        IdWorkItem INT NOT NULL,
        IdAsignado INT NULL,
        YaExistia BIT NOT NULL
    );

    -- =================================================================
    -- WorkItems raiz (desde tblTareas, excluye la fila basura id=1)
    -- =================================================================
    DECLARE
        @IdTarea INT, @Titulo NVARCHAR(200), @Comentarios NVARCHAR(MAX), @ClaveJira NVARCHAR(50),
        @IdEstatusWorkItem INT, @IdProyecto INT, @ClaveProyecto NVARCHAR(20), @IdTipoWorkItem INT,
        @IdEquipo INT, @IdAsignado INT, @IdSolicitanteWi INT, @IdComplejidad INT,
        @FechaCompromiso DATE, @Inicio DATETIME, @Fin DATETIME, @Revisado BIT,
        @Locacion NVARCHAR(100), @Registro DATE, @UltimaModificacion DATETIME, @IdSolicitud INT,
        @Descripcion NVARCHAR(MAX), @Folio NVARCHAR(50), @Mensaje NVARCHAR(4000), @IdWorkItemNuevo INT;

    DECLARE curTareas CURSOR LOCAL FAST_FORWARD FOR
        SELECT
            t.id,
            LEFT(t.Tarea, 200),
            t.Comentarios,
            ISNULL(NULLIF(LTRIM(RTRIM(t.Id2)), N''), N'GT-' + CAST(t.id AS NVARCHAR(20))),
            ISNULL(me.IdEstatusDestino, 1),
            ISNULL(p.IdProyecto, @IdProyectoFallback),
            ISNULL(p.Clave, N'SIN-PROY-GT'),
            CASE WHEN ISNULL(p.IdCategoriaProyecto, 1) = 2 THEN 8 ELSE 4 END,
            eq.IdEquipo,
            ua.IdUsuario,
            us.IdUsuario,
            cx.IdComplejidad,
            t.FechaCompromiso, t.Inicio, t.Fin,
            ISNULL(t.Revisado, 0),
            CASE WHEN ISNULL(p.IdCategoriaProyecto, 1) = 2 THEN NULLIF(LTRIM(RTRIM(t.Release)), N'') ELSE NULL END,
            t.Registro, t.UltimaModificacion,
            sol.IdSolicitud
        FROM dbo.OrigenTareas t
        LEFT JOIN @MapeoEstatus me ON me.EstatusOrigen = t.Estatus
        LEFT JOIN dbo.tblProyecto p ON p.Clave = LEFT(t.Proyecto, 20)
        LEFT JOIN @MapeoEquipo eq ON eq.Division = t.Division
        LEFT JOIN dbo.tblUsuario ua
            ON UPPER(LTRIM(RTRIM(ua.Dominio))) = UPPER(LTRIM(RTRIM(CASE t.Asignado WHEN N'Ana.Viramontes' THEN N'aviramontes' ELSE t.Asignado END)))
        LEFT JOIN dbo.tblUsuario us
            ON UPPER(LTRIM(RTRIM(us.Dominio))) = UPPER(LTRIM(RTRIM(CASE t.Solicitante WHEN N'Ana.Viramontes' THEN N'aviramontes' ELSE t.Solicitante END)))
        LEFT JOIN (SELECT DISTINCT idComplejidad, Complejidad FROM dbo.OrigenComplejidadNucleo) co ON co.idComplejidad = t.Complejidad
        LEFT JOIN dbo.tblComplejidad cx ON cx.Nombre = co.Complejidad
        LEFT JOIN dbo.tblSolicitud sol ON t.EDM IS NOT NULL AND sol.Descripcion LIKE N'[Migracion GT - EDM ' + CAST(t.EDM AS NVARCHAR(10)) + N']%'
        WHERE t.Registro IS NOT NULL
        ORDER BY t.id;

    OPEN curTareas;
    FETCH NEXT FROM curTareas INTO
        @IdTarea, @Titulo, @Comentarios, @ClaveJira, @IdEstatusWorkItem, @IdProyecto, @ClaveProyecto,
        @IdTipoWorkItem, @IdEquipo, @IdAsignado, @IdSolicitanteWi, @IdComplejidad,
        @FechaCompromiso, @Inicio, @Fin, @Revisado, @Locacion, @Registro, @UltimaModificacion, @IdSolicitud;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM dbo.tblWorkItem WHERE ClaveJira = @ClaveJira)
        BEGIN
            SELECT @IdWorkItemNuevo = IdWorkItem FROM dbo.tblWorkItem WHERE ClaveJira = @ClaveJira;
            INSERT INTO #MapaTareaWorkItem (IdTareaOrigen, IdWorkItem, IdAsignado, YaExistia)
            VALUES (@IdTarea, @IdWorkItemNuevo, @IdAsignado, 1);
        END
        ELSE
        BEGIN
            SET @Descripcion = NULL;
            IF @Comentarios IS NOT NULL AND LTRIM(RTRIM(@Comentarios)) <> N''
            BEGIN
                IF @Comentarios LIKE N'{\rtf%'
                    SET @Descripcion = N'<p><em>[Comentario original en formato RTF - conversion visual pendiente, contenido preservado integro]</em></p><pre>'
                        + REPLACE(REPLACE(REPLACE(@Comentarios, N'&', N'&amp;'), N'<', N'&lt;'), N'>', N'&gt;') + N'</pre>';
                ELSE
                    SET @Descripcion = N'<p>' + REPLACE(REPLACE(REPLACE(@Comentarios, N'&', N'&amp;'), N'<', N'&lt;'), N'>', N'&gt;') + N'</p>';
            END

            EXEC dbo.spGenerarFolio @Serie = @ClaveProyecto, @Usuario = N'migracion-gt', @Folio = @Folio OUTPUT, @Mensaje = @Mensaje OUTPUT;

            INSERT INTO dbo.tblWorkItem
                (Folio, IdTipoWorkItem, IdPadre, IdProyecto, IdSolicitud, Titulo, Descripcion,
                 IdEstatusWorkItem, IdPrioridad, IdComplejidad, IdAsignado, IdSolicitante,
                 FechaCompromiso, FechaInicio, FechaFin, Revisado, Locacion, IdEquipo, ClaveJira,
                 FechaRegistro, UsuarioRegistro, UsuarioMovto, FechaMovto, Activo)
            VALUES
                (@Folio, @IdTipoWorkItem, NULL, @IdProyecto, @IdSolicitud, @Titulo, @Descripcion,
                 @IdEstatusWorkItem, 3, @IdComplejidad, @IdAsignado, @IdSolicitanteWi,
                 @FechaCompromiso, @Inicio, @Fin, @Revisado, @Locacion, @IdEquipo, @ClaveJira,
                 ISNULL(CAST(@Registro AS DATETIME2), SYSDATETIME()), N'migracion-gt', NULL, @UltimaModificacion, 1);

            SET @IdWorkItemNuevo = SCOPE_IDENTITY();
            INSERT INTO #MapaTareaWorkItem (IdTareaOrigen, IdWorkItem, IdAsignado, YaExistia)
            VALUES (@IdTarea, @IdWorkItemNuevo, @IdAsignado, 0);
        END

        FETCH NEXT FROM curTareas INTO
            @IdTarea, @Titulo, @Comentarios, @ClaveJira, @IdEstatusWorkItem, @IdProyecto, @ClaveProyecto,
            @IdTipoWorkItem, @IdEquipo, @IdAsignado, @IdSolicitanteWi, @IdComplejidad,
            @FechaCompromiso, @Inicio, @Fin, @Revisado, @Locacion, @Registro, @UltimaModificacion, @IdSolicitud;
    END
    CLOSE curTareas;
    DEALLOCATE curTareas;

    DECLARE @NEnMapa INT, @NNuevosRaiz INT;
    SELECT @NEnMapa = COUNT(*), @NNuevosRaiz = SUM(CASE WHEN YaExistia = 0 THEN 1 ELSE 0 END) FROM #MapaTareaWorkItem;
    PRINT 'OK: WorkItems raiz procesados (' + CAST(@NEnMapa AS NVARCHAR(10)) + ' filas en el mapa, '
        + CAST(@NNuevosRaiz AS NVARCHAR(10)) + ' nuevas)'

    -- =================================================================
    -- WorkItems hijos (desde tblSubtareas) + tblRegistroTiempo
    -- =================================================================
    DECLARE
        @IdSub INT, @IdTareaPadre INT, @DescSub NVARCHAR(200), @TiempoEmpleado TIME,
        @FechaRegistroSub DATETIME, @Id2Sub NVARCHAR(50), @ClaveJiraSub NVARCHAR(50),
        @IdWorkItemPadre INT, @IdAsignadoPadre INT, @IdWorkItemHijo INT, @Minutos INT;

    DECLARE curSub CURSOR LOCAL FAST_FORWARD FOR
        SELECT s.Id, s.idTarea, LEFT(s.Descripción, 200), s.TiempoEmpleado, s.FechaRegistro,
               ISNULL(NULLIF(LTRIM(RTRIM(s.Id2)), N''), N'GT-SUB-' + CAST(s.Id AS NVARCHAR(20)))
        FROM dbo.OrigenSubtareas s
        ORDER BY s.Id;

    OPEN curSub;
    FETCH NEXT FROM curSub INTO @IdSub, @IdTareaPadre, @DescSub, @TiempoEmpleado, @FechaRegistroSub, @ClaveJiraSub;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT @IdWorkItemPadre = IdWorkItem, @IdAsignadoPadre = IdAsignado
        FROM #MapaTareaWorkItem WHERE IdTareaOrigen = @IdTareaPadre;

        IF @IdWorkItemPadre IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.tblWorkItem WHERE ClaveJira = @ClaveJiraSub)
            BEGIN
                DECLARE @ClaveProyectoPadre NVARCHAR(20), @IdProyectoPadre INT, @IdTipoPadre INT, @IdEquipoPadre INT;
                SELECT @IdProyectoPadre = w.IdProyecto, @ClaveProyectoPadre = p.Clave, @IdTipoPadre = w.IdTipoWorkItem, @IdEquipoPadre = w.IdEquipo
                FROM dbo.tblWorkItem w JOIN dbo.tblProyecto p ON p.IdProyecto = w.IdProyecto
                WHERE w.IdWorkItem = @IdWorkItemPadre;

                EXEC dbo.spGenerarFolio @Serie = @ClaveProyectoPadre, @Usuario = N'migracion-gt', @Folio = @Folio OUTPUT, @Mensaje = @Mensaje OUTPUT;

                INSERT INTO dbo.tblWorkItem
                    (Folio, IdTipoWorkItem, IdPadre, IdProyecto, Titulo, IdEstatusWorkItem, IdPrioridad,
                     IdAsignado, IdEquipo, ClaveJira, FechaRegistro, UsuarioRegistro, Activo)
                VALUES
                    (@Folio, @IdTipoPadre, @IdWorkItemPadre, @IdProyectoPadre, @DescSub, 6, 3,
                     @IdAsignadoPadre, @IdEquipoPadre, @ClaveJiraSub,
                     ISNULL(CAST(@FechaRegistroSub AS DATETIME2), SYSDATETIME()), N'migracion-gt', 1);

                SET @IdWorkItemHijo = SCOPE_IDENTITY();
            END
            ELSE
                SELECT @IdWorkItemHijo = IdWorkItem FROM dbo.tblWorkItem WHERE ClaveJira = @ClaveJiraSub;

            SET @Minutos = DATEDIFF(MINUTE, '00:00:00', @TiempoEmpleado);
            IF @Minutos BETWEEN 1 AND 1440 AND @IdAsignadoPadre IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.tblRegistroTiempo WHERE IdWorkItem = @IdWorkItemHijo)
                BEGIN
                    INSERT INTO dbo.tblRegistroTiempo
                        (IdWorkItem, IdUsuario, Fecha, Minutos, FechaRegistro, UsuarioRegistro, Activo)
                    VALUES
                        (@IdWorkItemHijo, @IdAsignadoPadre, CAST(@FechaRegistroSub AS DATE), @Minutos,
                         SYSDATETIME(), N'migracion-gt', 1)
                END
            END
        END

        FETCH NEXT FROM curSub INTO @IdSub, @IdTareaPadre, @DescSub, @TiempoEmpleado, @FechaRegistroSub, @ClaveJiraSub;
    END
    CLOSE curSub;
    DEALLOCATE curSub;

    PRINT 'OK: Subtareas procesadas'

    -- =================================================================
    -- Historial de estatus (Proceso='WorkItem'), set-based (no cursor,
    -- no requiere folios). Solo aplica a los WorkItem RAIZ (el origen
    -- no llevaba historial de estatus por subtarea).
    -- =================================================================
    INSERT INTO dbo.tblHistorialEstatus (Proceso, IdRegistro, IdEstatus, Accion, FechaInicio, FechaFin, MinutosLaborales, Usuario, Motivo)
    SELECT
        N'WorkItem',
        m.IdWorkItem,
        ISNULL(me.IdEstatusDestino, 1),
        NULL,
        h.FechaInicio,
        h.FechaFin,
        NULL,
        N'',
        NULL
    FROM dbo.OrigenHistorialEstatus h
    JOIN #MapaTareaWorkItem m ON m.IdTareaOrigen = h.IdTarea
    LEFT JOIN @MapeoEstatus me ON me.EstatusOrigen = h.Estatus
    WHERE m.YaExistia = 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.tblHistorialEstatus he
          WHERE he.Proceso = N'WorkItem' AND he.IdRegistro = m.IdWorkItem
            AND he.FechaInicio = h.FechaInicio
      )
    PRINT 'OK: tblHistorialEstatus procesado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

    -- =================================================================
    -- Reporte de excepciones (informativo, no bloquea el commit)
    -- =================================================================
    PRINT '===== REPORTE DE EXCEPCIONES ====='
    SELECT 'Tareas con proyecto no identificado (bucket SIN-PROY-GT)' Detalle, COUNT(*) n
    FROM dbo.OrigenTareas t
    WHERE t.Registro IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.tblProyecto p WHERE p.Clave = LEFT(t.Proyecto, 20) AND p.Clave <> N'SIN-PROY-GT')
    UNION ALL
    SELECT 'Tareas con Asignado sin resolver (ej. David Altamirano, excluido)', COUNT(*)
    FROM dbo.OrigenTareas t
    WHERE t.Registro IS NOT NULL AND t.Asignado IS NOT NULL AND LTRIM(RTRIM(t.Asignado)) <> N''
      AND NOT EXISTS (
          SELECT 1 FROM dbo.tblUsuario u
          WHERE UPPER(LTRIM(RTRIM(u.Dominio))) = UPPER(LTRIM(RTRIM(CASE t.Asignado WHEN N'Ana.Viramontes' THEN N'aviramontes' ELSE t.Asignado END)))
      )
    UNION ALL
    SELECT 'Subtareas con duracion 0 (sin fila de tblRegistroTiempo)', COUNT(*)
    FROM dbo.OrigenSubtareas
    WHERE DATEDIFF(MINUTE, '00:00:00', TiempoEmpleado) = 0
    UNION ALL
    SELECT 'Comentarios RTF preservados sin convertir (formato pendiente)', COUNT(*)
    FROM dbo.OrigenTareas WHERE Registro IS NOT NULL AND Comentarios LIKE '{\rtf%'
    UNION ALL
    SELECT 'Filas de tblHistorialEstatus con MinutosLaborales NULL (no recalculado en esta pasada)', COUNT(*)
    FROM dbo.tblHistorialEstatus WHERE Proceso = N'WorkItem' AND MinutosLaborales IS NULL AND FechaFin IS NOT NULL

    DROP TABLE #MapaTareaWorkItem;

    DROP SYNONYM dbo.OrigenTareas;
    DROP SYNONYM dbo.OrigenSubtareas;
    DROP SYNONYM dbo.OrigenHistorialEstatus;
    DROP SYNONYM dbo.OrigenComplejidadNucleo;

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'curSin') >= -1 BEGIN CLOSE curSin; DEALLOCATE curSin; END
    IF CURSOR_STATUS('local', 'curTareas') >= -1 BEGIN CLOSE curTareas; DEALLOCATE curTareas; END
    IF CURSOR_STATUS('local', 'curSub') >= -1 BEGIN CLOSE curSub; DEALLOCATE curSub; END
    IF OBJECT_ID('tempdb..#MapaTareaWorkItem') IS NOT NULL DROP TABLE #MapaTareaWorkItem;
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
