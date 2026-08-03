USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      04_2026-08-01_SCRIPT_bdsGTE_MigracionSolicitudes.sql
   Autor:       Equipo GTE
   Descripcion: Migracion B3, bloque Solicitudes. Origen: bdsApollo.tblEDM
                (funcionalidad nueva del GT, sin commitear, ni siquiera
                en el Documento Maestro de GTE -- decision del negocio:
                sus 19 filas se migran como tblSolicitud, ya que
                estructuralmente son solicitudes que ya generaron
                WorkItems reales, igual que el patron Solicitud->
                conversion que GTE ya implementa). Debe correr ANTES del
                script de Nucleo (05), porque tblWorkItem.IdSolicitud
                depende de las Solicitudes creadas aqui.

                IdEstatusSolicitud = 6 (Convertida) para las 19: ya
                generaron WorkItems reales en el origen.
                IdProyecto: las 19 filas de tblEDM tienen Proyecto='EDM'
                literal, que coincide con el proyecto migrado 'EDM'
                (script 03) -- se resuelve directo, sin excepciones.
                IdSolicitante (NOT NULL): 5 de las 19 tienen un
                solicitante externo que no es ninguno de los usuarios
                migrados -- se usa el placeholder 'solicitante-externo-gt'
                (creado en el script 02).

                Idempotencia: tblSolicitud no tiene una columna de
                referencia al origen (a diferencia de
                tblWorkItem.ClaveJira) -- se usa un marcador al inicio
                de Descripcion ('[Migracion GT - EDM N]') para detectar
                si una fila ya fue migrada en una corrida anterior.

                ASUNCION DE ENTORNO: igual que el script 03, la base
                origen se asume restaurada como 'bdsApollo'.
   ===================================================================== */
BEGIN TRY

    DECLARE @IdProyectoEDM INT;
    SELECT @IdProyectoEDM = IdProyecto FROM dbo.tblProyecto WHERE Clave = N'EDM';

    -- Mapeo de Solicitante origen (tblEDM.Solicitante) -> Dominio destino.
    -- Blanco/vacio -> aviramontes (los 6 casos en blanco de origen tienen
    -- a Ana Viramontes tambien como Asignado, es autosolicitud).
    -- Los 4 solicitantes externos (no son ninguno de los 5 migrados)
    -- resuelven a NULL aqui -> se usa el placeholder mas abajo.
    -- Se compara por PREFIJO ASCII (LEFT 6 caracteres) para no depender
    -- de la codificacion exacta del caracter acentuado en "PAMELA.MUÑOZ".
    DECLARE @MapeoSolicitante TABLE (PrefijoOrigen NVARCHAR(10), DominioDestino NVARCHAR(100) NULL);
    INSERT INTO @MapeoSolicitante (PrefijoOrigen, DominioDestino) VALUES
        (N'',       N'aviramontes'),
        (N'ANA.VI', N'aviramontes'),
        (N'ANTONI', N'Antonio.Ochoa'),
        (N'KARLA.', NULL),
        (N'LORENA', NULL),
        (N'GERARD', NULL),
        (N'PAMELA', NULL);

    -- Clasificacion manual de IdTipoSolicitud por fila (19 filas, sin
    -- heuristica generica -- ver comentario en el plan de migracion).
    -- 1=Nuevo Desarrollo, 2=Mejora, 3=Correccion, 4=Soporte
    DECLARE @TipoPorEDM TABLE (IdEDM INT, IdTipoSolicitud INT);
    INSERT INTO @TipoPorEDM (IdEDM, IdTipoSolicitud) VALUES
        (1, 2), (2, 4), (3, 4), (4, 4), (5, 4), (6, 4), (7, 1), (8, 2), (9, 4),
        (10, 2), (11, 1), (12, 1), (13, 2), (14, 1), (15, 2), (16, 2), (17, 2),
        (18, 2), (19, 3);

    DECLARE @IdEDM INT, @Titulo NVARCHAR(200), @SolicitanteOrigen NVARCHAR(50),
            @Id2 NVARCHAR(50), @Registro DATE, @Marcador NVARCHAR(60),
            @DominioSolicitante NVARCHAR(100), @IdSolicitante INT, @IdTipoSolicitud INT,
            @Folio NVARCHAR(50), @Mensaje NVARCHAR(4000), @Descripcion NVARCHAR(500);

    DECLARE curEdm CURSOR LOCAL FAST_FORWARD FOR
        SELECT IdEDM, LEFT(Tarea, 200), ISNULL(Solicitante, N''), Id2, Registro
        FROM bdsApollo.dbo.tblEDM
        ORDER BY IdEDM;

    OPEN curEdm;
    FETCH NEXT FROM curEdm INTO @IdEDM, @Titulo, @SolicitanteOrigen, @Id2, @Registro;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @Marcador = N'[Migracion GT - EDM ' + CAST(@IdEDM AS NVARCHAR(10)) + N']';

        IF NOT EXISTS (SELECT 1 FROM dbo.tblSolicitud WHERE LEFT(Descripcion, LEN(@Marcador)) = @Marcador)
        BEGIN
            -- SET (no SELECT) para evitar el problema clasico de T-SQL en el
            -- que "SELECT @var = col FROM ... WHERE sin_match" deja el valor
            -- de la iteracion anterior en vez de limpiarlo a NULL.
            SET @DominioSolicitante = NULL;
            SET @IdSolicitante = NULL;
            SELECT TOP (1) @DominioSolicitante = DominioDestino
            FROM @MapeoSolicitante
            WHERE PrefijoOrigen = LEFT(@SolicitanteOrigen, 6)
            ORDER BY LEN(PrefijoOrigen) DESC;

            IF @DominioSolicitante IS NOT NULL
                SELECT @IdSolicitante = IdUsuario FROM dbo.tblUsuario WHERE Dominio = @DominioSolicitante;

            IF @IdSolicitante IS NULL
                SELECT @IdSolicitante = IdUsuario FROM dbo.tblUsuario WHERE Dominio = N'solicitante-externo-gt';

            SELECT @IdTipoSolicitud = IdTipoSolicitud FROM @TipoPorEDM WHERE IdEDM = @IdEDM;

            SET @Descripcion = @Marcador + N' Origen tblEDM.Id2: ' + ISNULL(@Id2, N'(sin dato)');

            EXEC dbo.spGenerarFolio
                @Serie = N'SOL-2026', @Usuario = N'migracion-gt',
                @Folio = @Folio OUTPUT, @Mensaje = @Mensaje OUTPUT;

            INSERT INTO dbo.tblSolicitud
                (Folio, IdSolicitante, IdProyecto, Titulo, Descripcion, IdTipoSolicitud,
                 IdPrioridad, IdEstatusSolicitud, FechaDeseada,
                 FechaRegistro, UsuarioRegistro, Activo)
            VALUES
                (@Folio, @IdSolicitante, @IdProyectoEDM, @Titulo, @Descripcion, @IdTipoSolicitud,
                 3, 6, NULL,
                 ISNULL(CAST(@Registro AS DATETIME2), SYSDATETIME()), N'migracion-gt', 1)

            PRINT 'OK: Solicitud ' + @Folio + ' creada (origen EDM ' + CAST(@IdEDM AS NVARCHAR(10)) + ')'
        END
        ELSE
            PRINT 'SKIP: EDM ' + CAST(@IdEDM AS NVARCHAR(10)) + ' ya migrado como Solicitud'

        FETCH NEXT FROM curEdm INTO @IdEDM, @Titulo, @SolicitanteOrigen, @Id2, @Registro;
    END
    CLOSE curEdm;
    DEALLOCATE curEdm;

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'curEdm') >= -1
    BEGIN
        CLOSE curEdm;
        DEALLOCATE curEdm;
    END
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
