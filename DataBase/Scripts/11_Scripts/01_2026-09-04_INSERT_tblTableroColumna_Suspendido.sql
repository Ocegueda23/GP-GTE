USE [bdsGTE]
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
/* =====================================================================
   Script:      01_2026-09-04_INSERT_tblTableroColumna_Suspendido.sql
   Autor:       Ana Viramontes
   Descripcion: Agrega la columna "Suspendido" a los tableros ya creados.
                El mapeo estandar de columnas (ColumnasTableroEstandar en
                GTE.Domain) solo tenia Pendiente / En proceso / En pruebas /
                Correccion / Terminado, asi que un WorkItem que pasaba a
                estatus Suspendido (5) desaparecia del tablero: no habia
                columna que lo recibiera. Los tableros nuevos ya nacen con
                ella desde el codigo; este script la da de alta en los
                tableros que se aprovisionaron antes del cambio.
                La columna se inserta en la posicion que ocupaba Terminado
                (por default 5) y se desplaza +1 el orden de las columnas
                que quedan a su derecha, para que el tablero se siga
                leyendo de izquierda a derecha y Terminado lo cierre.
   Idempotente: solo toca los tableros activos que NO tienen columna para
                el estatus 5; correrlo de nuevo no reordena ni duplica.
                El NOT EXISTS no filtra por Activo a proposito: la columna
                inactiva tambien ocupa la llave UQ_tblTableroColumna_
                TableroEstatus, y si alguien la dio de baja a mano no se
                reactiva por script.
   ===================================================================== */
BEGIN TRY

    DECLARE @EstatusSuspendido INT = 5,
            @EstatusTerminado  INT = 6,
            @Usuario NVARCHAR(50) = N'script-despliegue'

    DECLARE @Pendientes TABLE (IdTablero INT PRIMARY KEY, OrdenInsercion INT NOT NULL)

    INSERT INTO @Pendientes (IdTablero, OrdenInsercion)
    SELECT t.IdTablero,
           /* Donde entra Suspendido: la posicion de Terminado si el tablero la
              tiene; si no (columnas personalizadas), al final del tablero. */
           COALESCE(
               (SELECT MIN(c.Orden) FROM dbo.tblTableroColumna c
                 WHERE c.IdTablero = t.IdTablero AND c.Activo = 1
                   AND c.IdEstatusWorkItem = @EstatusTerminado),
               (SELECT ISNULL(MAX(c.Orden), 0) + 1 FROM dbo.tblTableroColumna c
                 WHERE c.IdTablero = t.IdTablero AND c.Activo = 1))
    FROM dbo.tblTablero t
    WHERE t.Activo = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.tblTableroColumna c
                       WHERE c.IdTablero = t.IdTablero
                         AND c.IdEstatusWorkItem = @EstatusSuspendido)

    IF NOT EXISTS (SELECT 1 FROM @Pendientes)
    BEGIN
        PRINT 'SKIP: todos los tableros activos ya tienen columna de Suspendido'
    END
    ELSE
    BEGIN
        /* Sin filtro de Activo en el WHERE: las columnas dadas de baja tambien
           corren su orden, para no dejar huecos si alguna se reactiva. */
        UPDATE c
           SET c.Orden        = c.Orden + 1,
               c.UsuarioMovto = @Usuario,
               c.FechaMovto   = GETDATE()
          FROM dbo.tblTableroColumna c
          JOIN @Pendientes p ON p.IdTablero = c.IdTablero
         WHERE c.Orden >= p.OrdenInsercion
        PRINT 'OK: columnas desplazadas a la derecha -> ' + CAST(@@ROWCOUNT AS NVARCHAR(10))

        INSERT INTO dbo.tblTableroColumna
            (IdTablero, Nombre, IdEstatusWorkItem, Orden, LimiteWip, UsuarioRegistro, Activo)
        SELECT p.IdTablero, N'Suspendido', @EstatusSuspendido, p.OrdenInsercion,
               NULL,   /* sin limite WIP: el trabajo detenido no se topa */
               @Usuario, 1
        FROM @Pendientes p
        PRINT 'OK: columna Suspendido agregada en tableros -> ' + CAST(@@ROWCOUNT AS NVARCHAR(10))
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
