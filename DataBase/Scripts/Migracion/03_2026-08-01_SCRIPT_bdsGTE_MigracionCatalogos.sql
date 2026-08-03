USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      03_2026-08-01_SCRIPT_bdsGTE_MigracionCatalogos.sql
   Autor:       Equipo GTE
   Descripcion: Migracion B3, bloque Catalogos: Equipos (con lider real,
                requiere que el script 02 de Usuarios ya haya corrido),
                Complejidad + Matriz de Presupuesto, Proyectos, Festivos.

                ASUNCION DE ENTORNO: la base origen restaurada en este
                entorno se llama 'bdsApollo' (ver script de
                perfilado de la Fase A). Para el corte real, cambiar el
                literal 'bdsApollo' por el nombre real de la
                base origen restaurada en ese momento (buscar y
                reemplazar en todo el script -- son referencias de 3
                partes, no hay sinonimo intermedio).
   ===================================================================== */
BEGIN TRY

    -- =================================================================
    -- Equipos (5, uno por division real del GT). Analisis de Datos sin
    -- lider (David Altamirano no se migro). Requiere que los usuarios
    -- del script 02 ya existan.
    -- =================================================================
    DECLARE @Equipos TABLE (Nombre NVARCHAR(100), DominioLider NVARCHAR(100) NULL);
    INSERT INTO @Equipos (Nombre, DominioLider) VALUES
        (N'Desarrollo',                          N'Antonio.Ochoa'),
        (N'Infraestructura',                     N'Roberto.Lopez'),
        (N'Servicios Tecnologicos y Soporte',     N'Roberto.Gonzalez'),
        (N'Gerencia',                             N'aviramontes'),
        (N'Analisis de Datos',                    NULL);

    DECLARE @NombreEquipo NVARCHAR(100), @DominioLider NVARCHAR(100), @IdLider INT;
    DECLARE curEq CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre, DominioLider FROM @Equipos;
    OPEN curEq;
    FETCH NEXT FROM curEq INTO @NombreEquipo, @DominioLider;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @IdLider = NULL;
        IF @DominioLider IS NOT NULL
            SELECT @IdLider = IdUsuario FROM dbo.tblUsuario WHERE Dominio = @DominioLider;

        IF NOT EXISTS (SELECT 1 FROM dbo.tblEquipo WHERE Nombre = @NombreEquipo)
        BEGIN
            INSERT INTO dbo.tblEquipo (Nombre, IdLider, FechaRegistro, UsuarioRegistro, Activo)
            VALUES (@NombreEquipo, @IdLider, SYSDATETIME(), N'migracion-gt', 1)
            PRINT 'OK: equipo ' + @NombreEquipo + ' creado'
        END
        ELSE
            PRINT 'SKIP: equipo ' + @NombreEquipo + ' ya existe'

        FETCH NEXT FROM curEq INTO @NombreEquipo, @DominioLider;
    END
    CLOSE curEq;
    DEALLOCATE curEq;

    -- =================================================================
    -- Complejidad + Matriz de Presupuesto (lectura cross-DB del origen;
    -- origen ya trae una fila por Complejidad x Nivel, estructura casi
    -- identica a la matriz destino)
    -- =================================================================
    INSERT INTO dbo.tblComplejidad (Nombre, IdCategoriaProyecto, Orden, FechaRegistro, UsuarioRegistro, Activo)
    SELECT
        o.Complejidad,
        CASE WHEN MAX(o.Categoria) = 'TI' THEN 2 ELSE 1 END,
        ROW_NUMBER() OVER (ORDER BY MIN(o.idComplejidad)),
        SYSDATETIME(), N'migracion-gt', 1
    FROM bdsApollo.dbo.tblComplejidad o
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblComplejidad c WHERE c.Nombre = o.Complejidad)
    GROUP BY o.Complejidad
    PRINT 'OK/SKIP: tblComplejidad procesado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

    INSERT INTO dbo.tblMatrizPresupuesto (IdComplejidad, IdNivel, Minutos, Puntos, FechaRegistro, UsuarioRegistro)
    SELECT
        c.IdComplejidad,
        CASE o.IdNivel WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 1 END, -- SENIOR->2,MASTER->3,JUNIOR->1
        o.Minutos,
        o.Puntos,
        SYSDATETIME(), N'migracion-gt'
    FROM bdsApollo.dbo.tblComplejidad o
    JOIN dbo.tblComplejidad c ON c.Nombre = o.Complejidad
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.tblMatrizPresupuesto m
        WHERE m.IdComplejidad = c.IdComplejidad
          AND m.IdNivel = CASE o.IdNivel WHEN 1 THEN 2 WHEN 2 THEN 3 WHEN 3 THEN 1 END
    )
    PRINT 'OK/SKIP: tblMatrizPresupuesto procesado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

    -- =================================================================
    -- Proyectos (excluye los 3 valores basura de tblTareas.Proyecto que
    -- nunca tuvieron fila real en tblProyecto -- esos quedan en el
    -- reporte de excepciones del script de Nucleo)
    -- =================================================================
    INSERT INTO dbo.tblProyecto
        (Clave, Nombre, IdCategoriaProyecto, IdEstatusProyecto, FechaInicioPlan,
         FechaRegistro, UsuarioRegistro, Activo)
    SELECT
        LEFT(o.Proyecto, 20),
        o.Proyecto,
        CASE WHEN o.Categoria = N'TI' THEN 2 ELSE 1 END,
        CASE WHEN o.Estatus = 1 THEN 3 ELSE 5 END, -- 3=En Ejecucion, 5=Cerrado
        o.FechaInicio,
        SYSDATETIME(), N'migracion-gt', 1
    FROM bdsApollo.dbo.tblProyecto o
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblProyecto p WHERE p.Clave = LEFT(o.Proyecto, 20))
    PRINT 'OK/SKIP: tblProyecto procesado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

    -- =================================================================
    -- Festivos (curado a mano: el origen tblCalendario mezcla dias de
    -- fin de semana ["Saturday"/"Sunday", no son festivos reales, ya
    -- cubiertos por los tramos L-V de los horarios) y 7 filas
    -- "VACACIONES Ana" (son ausencias de una persona, no festivos de la
    -- empresa -- pertenecen a tblAusencia, fuera de alcance de esta
    -- sesion). Tambien deduplica fechas con dos etiquetas distintas
    -- para el mismo feriado. Solo cubre 2025 -- el origen no tiene
    -- festivos cargados para 2026 tampoco, se replica fielmente.
    -- =================================================================
    DECLARE @Festivos TABLE (Fecha DATE, Descripcion NVARCHAR(200));
    INSERT INTO @Festivos (Fecha, Descripcion) VALUES
        ('2025-01-01', N'Año Nuevo'),
        ('2025-02-03', N'Día de la Constitución (se traslada al lunes)'),
        ('2025-03-17', N'Natalicio de Benito Juárez (se traslada al lunes)'),
        ('2025-04-17', N'Jueves Santo'),
        ('2025-04-18', N'Viernes Santo'),
        ('2025-05-01', N'Día del Trabajo'),
        ('2025-05-10', N'Día de las Madres'),
        ('2025-09-16', N'Día de la Independencia'),
        ('2025-10-01', N'Cambio de presidente (solo si hay transición ese año)'),
        ('2025-11-02', N'Día de Muertos'),
        ('2025-11-17', N'Revolución Mexicana (se traslada al lunes)'),
        ('2025-12-12', N'Día de la Virgen de Guadalupe'),
        ('2025-12-25', N'Navidad');

    INSERT INTO dbo.tblDiaFestivo (Fecha, Descripcion, IdHorario, FechaRegistro, UsuarioRegistro, Activo)
    SELECT f.Fecha, f.Descripcion, NULL, SYSDATETIME(), N'migracion-gt', 1
    FROM @Festivos f
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblDiaFestivo d WHERE d.Fecha = f.Fecha AND d.IdHorario IS NULL)
    PRINT 'OK/SKIP: tblDiaFestivo procesado (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas nuevas)'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'curEq') >= -1
    BEGIN
        CLOSE curEq;
        DEALLOCATE curEq;
    END
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
