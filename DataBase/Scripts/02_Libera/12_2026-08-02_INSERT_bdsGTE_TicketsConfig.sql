USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      12_2026-08-02_INSERT_bdsGTE_TicketsConfig.sql
   Autor:       Equipo GTE
   Descripcion: Modulo Mesa de ayuda (Tickets y SLA), Fase 4.
                1. Etiquetas/permisos de UI para las 7 transiciones del
                   proceso Ticket (ya sembradas en
                   09_2026-07-30_INSERT_bdsGTE_Procesos.sql, aqui solo se
                   agrega su tblTransicionConfig, mismo patron que
                   01_2026-07-31_INSERT_bdsGTE_TransicionesYEtiquetas.sql).
                2. Categorias de ticket iniciales (tblCategoriaTicket
                   existe desde el script 01 pero sin filas).
                3. SLA por defecto por prioridad (tblSla existe desde el
                   script 06 pero sin filas), usando el horario INTERFLO.
                Valores de tiempo y nombres de categoria son un punto de
                partida razonable, ajustables por el negocio despues.
   Requiere:    01-11 aplicados (en particular 01, 02, 06, 07, 09).
   ===================================================================== */
BEGIN TRY

    /* ---------- 1. Etiquetas y permisos de las transiciones de Ticket ---------- */
    INSERT INTO dbo.tblTransicionConfig
        (Proceso, IdEstatusOrigen, Accion, EtiquetaBoton, RequierePermiso,
         RequiereMotivo, EsAccionPrincipal, Orden, UsuarioRegistro)
    SELECT v.Proceso, v.Origen, v.Accion, v.Etiqueta, v.Permiso,
           v.RequiereMotivo, v.EsPrincipal, v.Orden, N'script-despliegue'
    FROM (VALUES
        /* Ticket: 1 Nuevo, 2 Asignado, 3 En Atencion, 4 Esperando Usuario,
                   5 Resuelto, 6 Cerrado */
        (N'Ticket', 1, N'ASIGNAR',           N'Asignar',            N'TKT.Atender', 0, 1, 10),
        (N'Ticket', 2, N'INICIAR_ATENCION',  N'Iniciar atencion',   N'TKT.Atender', 0, 1, 10),
        (N'Ticket', 3, N'ESPERAR_USUARIO',   N'Esperar usuario',    N'TKT.Atender', 0, 0, 20),
        (N'Ticket', 4, N'REANUDAR',          N'Reanudar',           N'TKT.Atender', 0, 0, 20),
        (N'Ticket', 3, N'RESOLVER',          N'Resolver',           N'TKT.Atender', 0, 1, 10),
        (N'Ticket', 5, N'CERRAR',            N'Cerrar',             N'TKT.Atender', 0, 1, 10),
        (N'Ticket', 5, N'REABRIR',           N'Reabrir',            N'TKT.Atender', 0, 0, 20)
        ) v(Proceso, Origen, Accion, Etiqueta, Permiso, RequiereMotivo, EsPrincipal, Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTransicionConfig c
                      WHERE c.Proceso = v.Proceso
                        AND c.IdEstatusOrigen = v.Origen
                        AND c.Accion = v.Accion)
    PRINT 'OK: etiquetas y permisos de transiciones de Ticket sembrados'

    /* ---------- 2. Categorias de ticket ---------- */
    INSERT INTO dbo.tblCategoriaTicket (Nombre, UsuarioRegistro)
    SELECT v.Nombre, N'script-despliegue'
    FROM (VALUES (N'Incidencia'),(N'Duda'),(N'Acceso'),(N'Mejora'),(N'Otro')) v(Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblCategoriaTicket c WHERE c.Nombre = v.Nombre)
    PRINT 'OK: categorias de ticket sembradas'

    /* ---------- 3. SLA por defecto por prioridad ---------- */
    /* tblPrioridad es un enumerado de Id fijo (1 Critica, 2 Alta, 3 Media, 4 Baja,
       ver 01_2026-07-30_SCRIPT_bdsGTE_Catalogos.sql), asi que se referencia por Id
       directo. tblHorario es un catalogo gestionado (IDENTITY), se resuelve por
       Nombre para no depender del Id que le haya tocado en cada ambiente. */
    IF NOT EXISTS (SELECT 1 FROM dbo.tblHorario WHERE Nombre = N'INTERFLO')
    BEGIN
        RAISERROR('No existe el horario INTERFLO: revisar 02_2026-08-01_SCRIPT_bdsGTE_MigracionUsuarios.sql antes de sembrar tblSla.', 16, 1)
    END

    INSERT INTO dbo.tblSla (Nombre, IdPrioridad, MinutosRespuesta, MinutosResolucion, IdHorario, UsuarioRegistro)
    SELECT v.Nombre, v.IdPrioridad, v.MinutosRespuesta, v.MinutosResolucion, h.IdHorario, N'script-despliegue'
    FROM (VALUES
        (N'SLA Critica', 1, 30,  240),
        (N'SLA Alta',    2, 60,  480),
        (N'SLA Media',   3, 240, 1440),
        (N'SLA Baja',    4, 480, 2880)
        ) v(Nombre, IdPrioridad, MinutosRespuesta, MinutosResolucion)
    CROSS JOIN (SELECT IdHorario FROM dbo.tblHorario WHERE Nombre = N'INTERFLO') h
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblSla s WHERE s.Nombre = v.Nombre)
    PRINT 'OK: SLA por defecto sembrado (uno por prioridad, horario INTERFLO)'

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
