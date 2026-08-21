USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      35_2026-08-21_SCRIPT_bdsGTE_CalidadWorkItemCentrica.sql
   Autor:       Equipo GTE
   Descripcion: Rediseno de Calidad (QA) a partir de la revision con el
                usuario: las pruebas dejan de organizarse en Plan/Ciclo y
                pasan a ejecutarse directamente desde el WorkItem que se
                esta probando. Una falla ya no crea un WorkItem tipo Bug
                (eso se reserva para defectos reales de produccion via
                Incidente -> Correccion, que ya existia); crea un
                HALLAZGO (tblRevision) sobre el mismo item, igual que un
                hallazgo de code review.

                Revierte la parte de 34_2026-08-21_ALTER_tblWorkItem_Severidad.sql
                que ya no aplica (la severidad se mueve al hallazgo, no al
                WorkItem) sin tocar ese script -- ya pudo haberse corrido.

                Cambios:
                1. tblWorkItem: quita IdSeveridad e IdEjecucionPruebaOrigen
                   (eran del flujo Bug-desde-QA, ahora retirado).
                2. tblCicloPrueba: eliminada.
                3. tblCasoPrueba: pasa de pertenecer a un Plan a ser un
                   catalogo reutilizable por PROYECTO (Reutilizable BIT
                   decide si aparece en el selector de "usar caso
                   existente" o si fue creado libre para una sola prueba).
                4. tblPlanPrueba: eliminada (tras migrar IdProyecto a los
                   casos que tenia).
                5. tblWorkItemCasoPrueba: tabla nueva, relacion N a N
                   entre WorkItem y Caso de prueba (un caso reutilizable
                   se puede asignar a varios items a lo largo del tiempo).
                6. tblEjecucionPrueba: quita IdCicloPrueba, agrega
                   IdWorkItem (contra que item se corrio esa ejecucion).
                7. tblRevision: agrega IdSeveridad (decide si el hallazgo
                   bloquea el cierre: solo S1/S2 bloquean) e
                   IdEjecucionPrueba (trazabilidad opcional hacia la
                   prueba que origino el hallazgo, NULL en hallazgos de
                   code review sin prueba asociada).

                Todas las columnas nuevas quedan NULL a nivel de BD (igual
                que el resto de los ALTER de este proyecto): lo
                obligatorio se valida en el backend, no con NOT NULL aqui,
                para no romper si ya hay filas.
   ===================================================================== */
BEGIN TRY

    /* ---------- 1. tblWorkItem: retira columnas del flujo Bug-desde-QA ---------- */
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblSeveridad')
    BEGIN
        ALTER TABLE dbo.tblWorkItem DROP CONSTRAINT FK_tblWorkItem_tblSeveridad
        PRINT 'OK: FK_tblWorkItem_tblSeveridad eliminada'
    END
    ELSE PRINT 'SKIP: FK_tblWorkItem_tblSeveridad no existe'

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'IdSeveridad'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem DROP COLUMN IdSeveridad
        PRINT 'OK: tblWorkItem.IdSeveridad eliminada'
    END
    ELSE PRINT 'SKIP: tblWorkItem.IdSeveridad no existe'

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblWorkItem_tblEjecucionPrueba')
    BEGIN
        ALTER TABLE dbo.tblWorkItem DROP CONSTRAINT FK_tblWorkItem_tblEjecucionPrueba
        PRINT 'OK: FK_tblWorkItem_tblEjecucionPrueba eliminada'
    END
    ELSE PRINT 'SKIP: FK_tblWorkItem_tblEjecucionPrueba no existe'

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItem' AND COLUMN_NAME = 'IdEjecucionPruebaOrigen'
    )
    BEGIN
        ALTER TABLE dbo.tblWorkItem DROP COLUMN IdEjecucionPruebaOrigen
        PRINT 'OK: tblWorkItem.IdEjecucionPruebaOrigen eliminada'
    END
    ELSE PRINT 'SKIP: tblWorkItem.IdEjecucionPruebaOrigen no existe'

    /* ---------- 2. tblCasoPrueba: de Plan a catalogo por Proyecto ---------- */
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPrueba' AND COLUMN_NAME = 'IdProyecto'
    )
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba ADD IdProyecto INT NULL
        PRINT 'OK: tblCasoPrueba.IdProyecto agregada -> INT NULL'

        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPlanPrueba')
        BEGIN
            UPDATE cp
                SET cp.IdProyecto = pp.IdProyecto
            FROM dbo.tblCasoPrueba cp
            JOIN dbo.tblPlanPrueba pp ON cp.IdPlanPrueba = pp.IdPlanPrueba
            PRINT 'OK: tblCasoPrueba.IdProyecto migrado desde tblPlanPrueba'
        END
    END
    ELSE PRINT 'SKIP: tblCasoPrueba.IdProyecto ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPrueba' AND COLUMN_NAME = 'Reutilizable'
    )
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba ADD Reutilizable BIT NOT NULL CONSTRAINT DF_tblCasoPrueba_Reutilizable DEFAULT (1)
        PRINT 'OK: tblCasoPrueba.Reutilizable agregada -> BIT NOT NULL DEFAULT 1'
    END
    ELSE PRINT 'SKIP: tblCasoPrueba.Reutilizable ya existe'

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblCasoPrueba_tblPlanPrueba')
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba DROP CONSTRAINT FK_tblCasoPrueba_tblPlanPrueba
        PRINT 'OK: FK_tblCasoPrueba_tblPlanPrueba eliminada'
    END
    ELSE PRINT 'SKIP: FK_tblCasoPrueba_tblPlanPrueba no existe'

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPrueba' AND COLUMN_NAME = 'IdPlanPrueba'
    )
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba DROP COLUMN IdPlanPrueba
        PRINT 'OK: tblCasoPrueba.IdPlanPrueba eliminada'
    END
    ELSE PRINT 'SKIP: tblCasoPrueba.IdPlanPrueba no existe'

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblCasoPrueba_tblWorkItem')
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba DROP CONSTRAINT FK_tblCasoPrueba_tblWorkItem
        PRINT 'OK: FK_tblCasoPrueba_tblWorkItem eliminada'
    END
    ELSE PRINT 'SKIP: FK_tblCasoPrueba_tblWorkItem no existe'

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblCasoPrueba_WorkItem' AND object_id = OBJECT_ID('dbo.tblCasoPrueba'))
    BEGIN
        DROP INDEX IX_tblCasoPrueba_WorkItem ON dbo.tblCasoPrueba
        PRINT 'OK: IX_tblCasoPrueba_WorkItem eliminado'
    END
    ELSE PRINT 'SKIP: IX_tblCasoPrueba_WorkItem no existe'

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCasoPrueba' AND COLUMN_NAME = 'IdWorkItem'
    )
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba DROP COLUMN IdWorkItem
        PRINT 'OK: tblCasoPrueba.IdWorkItem eliminada (vinculo unico viejo; ahora es N a N via tblWorkItemCasoPrueba)'
    END
    ELSE PRINT 'SKIP: tblCasoPrueba.IdWorkItem no existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblCasoPrueba_tblProyecto')
    BEGIN
        ALTER TABLE dbo.tblCasoPrueba
            ADD CONSTRAINT FK_tblCasoPrueba_tblProyecto FOREIGN KEY (IdProyecto) REFERENCES dbo.tblProyecto (IdProyecto)
        PRINT 'OK: FK_tblCasoPrueba_tblProyecto agregada'
    END
    ELSE PRINT 'SKIP: FK_tblCasoPrueba_tblProyecto ya existe'

    /* ---------- 3. tblEjecucionPrueba: de Ciclo a WorkItem ---------- */
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEjecucionPrueba' AND COLUMN_NAME = 'IdWorkItem'
    )
    BEGIN
        ALTER TABLE dbo.tblEjecucionPrueba ADD IdWorkItem INT NULL
        PRINT 'OK: tblEjecucionPrueba.IdWorkItem agregada -> INT NULL'
    END
    ELSE PRINT 'SKIP: tblEjecucionPrueba.IdWorkItem ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblEjecucionPrueba_tblWorkItem')
    BEGIN
        ALTER TABLE dbo.tblEjecucionPrueba
            ADD CONSTRAINT FK_tblEjecucionPrueba_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem)
        PRINT 'OK: FK_tblEjecucionPrueba_tblWorkItem agregada'
    END
    ELSE PRINT 'SKIP: FK_tblEjecucionPrueba_tblWorkItem ya existe'

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblEjecucionPrueba_tblCicloPrueba')
    BEGIN
        ALTER TABLE dbo.tblEjecucionPrueba DROP CONSTRAINT FK_tblEjecucionPrueba_tblCicloPrueba
        PRINT 'OK: FK_tblEjecucionPrueba_tblCicloPrueba eliminada'
    END
    ELSE PRINT 'SKIP: FK_tblEjecucionPrueba_tblCicloPrueba no existe'

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblEjecucionPrueba_Ciclo' AND object_id = OBJECT_ID('dbo.tblEjecucionPrueba'))
    BEGIN
        DROP INDEX IX_tblEjecucionPrueba_Ciclo ON dbo.tblEjecucionPrueba
        PRINT 'OK: IX_tblEjecucionPrueba_Ciclo eliminado'
    END
    ELSE PRINT 'SKIP: IX_tblEjecucionPrueba_Ciclo no existe'

    IF EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEjecucionPrueba' AND COLUMN_NAME = 'IdCicloPrueba'
    )
    BEGIN
        ALTER TABLE dbo.tblEjecucionPrueba DROP COLUMN IdCicloPrueba
        PRINT 'OK: tblEjecucionPrueba.IdCicloPrueba eliminada'
    END
    ELSE PRINT 'SKIP: tblEjecucionPrueba.IdCicloPrueba no existe'

    /* ---------- 4. Elimina tblCicloPrueba y tblPlanPrueba ---------- */
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCicloPrueba')
    BEGIN
        DROP TABLE dbo.tblCicloPrueba
        PRINT 'OK: tblCicloPrueba eliminada'
    END
    ELSE PRINT 'SKIP: tblCicloPrueba no existe'

    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblPlanPrueba')
    BEGIN
        DROP TABLE dbo.tblPlanPrueba
        PRINT 'OK: tblPlanPrueba eliminada'
    END
    ELSE PRINT 'SKIP: tblPlanPrueba no existe'

    /* ---------- 5. tblWorkItemCasoPrueba: asignacion N a N ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblWorkItemCasoPrueba')
    BEGIN
        CREATE TABLE dbo.tblWorkItemCasoPrueba
        (
            IdWorkItemCasoPrueba INT           IDENTITY(1,1) NOT NULL,
            IdWorkItem           INT                         NOT NULL,
            IdCasoPrueba         INT                         NOT NULL,
            FechaRegistro        DATETIME2                   NOT NULL CONSTRAINT DF_tblWorkItemCasoPrueba_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)               NOT NULL,
            UsuarioMovto         NVARCHAR(50)                NULL,
            FechaMovto           DATETIME                    NULL,
            Activo               BIT                         NOT NULL CONSTRAINT DF_tblWorkItemCasoPrueba_Activo DEFAULT (1),
            CONSTRAINT PK_tblWorkItemCasoPrueba PRIMARY KEY (IdWorkItemCasoPrueba),
            CONSTRAINT FK_tblWorkItemCasoPrueba_tblWorkItem FOREIGN KEY (IdWorkItem) REFERENCES dbo.tblWorkItem (IdWorkItem),
            CONSTRAINT FK_tblWorkItemCasoPrueba_tblCasoPrueba FOREIGN KEY (IdCasoPrueba) REFERENCES dbo.tblCasoPrueba (IdCasoPrueba)
        )
        CREATE UNIQUE INDEX UQ_tblWorkItemCasoPrueba_ItemCaso ON dbo.tblWorkItemCasoPrueba (IdWorkItem, IdCasoPrueba) WHERE Activo = 1
        PRINT 'OK: tblWorkItemCasoPrueba creada'
    END
    ELSE PRINT 'SKIP: tblWorkItemCasoPrueba ya existe'

    /* ---------- 6. tblRevision: severidad (bloquea o no) y trazabilidad a la prueba ---------- */
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRevision' AND COLUMN_NAME = 'IdSeveridad'
    )
    BEGIN
        ALTER TABLE dbo.tblRevision ADD IdSeveridad INT NULL
        PRINT 'OK: tblRevision.IdSeveridad agregada -> INT NULL'
    END
    ELSE PRINT 'SKIP: tblRevision.IdSeveridad ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblRevision_tblSeveridad')
    BEGIN
        ALTER TABLE dbo.tblRevision
            ADD CONSTRAINT FK_tblRevision_tblSeveridad FOREIGN KEY (IdSeveridad) REFERENCES dbo.tblSeveridad (Id)
        PRINT 'OK: FK_tblRevision_tblSeveridad agregada'
    END
    ELSE PRINT 'SKIP: FK_tblRevision_tblSeveridad ya existe'

    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblRevision' AND COLUMN_NAME = 'IdEjecucionPrueba'
    )
    BEGIN
        ALTER TABLE dbo.tblRevision ADD IdEjecucionPrueba INT NULL
        PRINT 'OK: tblRevision.IdEjecucionPrueba agregada -> INT NULL'
    END
    ELSE PRINT 'SKIP: tblRevision.IdEjecucionPrueba ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tblRevision_tblEjecucionPrueba')
    BEGIN
        ALTER TABLE dbo.tblRevision
            ADD CONSTRAINT FK_tblRevision_tblEjecucionPrueba FOREIGN KEY (IdEjecucionPrueba) REFERENCES dbo.tblEjecucionPrueba (IdEjecucionPrueba)
        PRINT 'OK: FK_tblRevision_tblEjecucionPrueba agregada'
    END
    ELSE PRINT 'SKIP: FK_tblRevision_tblEjecucionPrueba ya existe'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR — Se hizo ROLLBACK ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Línea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Número  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
