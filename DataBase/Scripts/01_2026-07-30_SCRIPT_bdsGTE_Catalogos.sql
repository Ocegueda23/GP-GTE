/* =====================================================================
   Script:      01_2026-07-30_SCRIPT_bdsGTE_Catalogos.sql
   Autor:       Equipo GTE
   Descripcion: Crea la base bdsGTE (si no existe) y los catalogos:
                - 12 catalogos de estatus (estructura estandar del motor:
                  Id, Descripcion, Orden, Activo - seccion 9.3 del estandar)
                - 10 enumerados de ID fijo (sin IDENTITY)
                - Gestionados: tblNivel, tblComplejidad, tblMatrizPresupuesto,
                  tblEtiqueta, tblCategoriaTicket
                + Seeds de estatus, enumerados y niveles.
   Nota:        Los IDs de estatus y enumerados son CONTRATO del sistema
                (los usa tblTransicion en bdsCentral y el backend). No cambiarlos.
   ===================================================================== */
IF DB_ID(N'bdsGTE') IS NULL
BEGIN
    CREATE DATABASE bdsGTE COLLATE Modern_Spanish_CI_AS
    PRINT 'OK: base de datos bdsGTE creada'
END
ELSE
    PRINT 'SKIP: base de datos bdsGTE ya existe'
GO
USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
SET XACT_ABORT ON
BEGIN TRANSACTION
BEGIN TRY

    /* ---------- 1. Catalogos de estatus (estructura estandar del motor) ---------- */
    DECLARE @catalogosEstatus TABLE (Nombre SYSNAME PRIMARY KEY)
    INSERT INTO @catalogosEstatus (Nombre) VALUES
        (N'tblEstatusSolicitud'), (N'tblEstatusWorkItem'), (N'tblEstatusSprint'),
        (N'tblEstatusRelease'), (N'tblEstatusDespliegue'), (N'tblEstatusIncidente'),
        (N'tblEstatusTicket'), (N'tblEstatusRiesgo'), (N'tblEstatusRevision'),
        (N'tblEstatusAprobacion'), (N'tblEstatusProyecto'), (N'tblEstatusAusencia')

    DECLARE @tabla SYSNAME, @sql NVARCHAR(MAX)
    DECLARE curEstatus CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @catalogosEstatus
    OPEN curEstatus
    FETCH NEXT FROM curEstatus INTO @tabla
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                       WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tabla)
        BEGIN
            SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@tabla) + N'
            (
                Id          INT           NOT NULL,
                Descripcion NVARCHAR(100) NOT NULL,
                Orden       INT           NOT NULL,
                Activo      BIT           NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + @tabla + '_Activo') + N' DEFAULT (1),
                CONSTRAINT ' + QUOTENAME('PK_' + @tabla) + N' PRIMARY KEY (Id),
                CONSTRAINT ' + QUOTENAME('UQ_' + @tabla + '_Descripcion') + N' UNIQUE (Descripcion)
            )'
            EXEC (@sql)
            PRINT 'OK: ' + @tabla + ' creada'
        END
        ELSE
            PRINT 'SKIP: ' + @tabla + ' ya existe'
        FETCH NEXT FROM curEstatus INTO @tabla
    END
    CLOSE curEstatus
    DEALLOCATE curEstatus

    /* ---------- 2. Enumerados de ID fijo (sin IDENTITY, con auditoria) ---------- */
    DECLARE @enumerados TABLE (Nombre SYSNAME PRIMARY KEY)
    INSERT INTO @enumerados (Nombre) VALUES
        (N'tblTipoWorkItem'), (N'tblTipoVinculo'), (N'tblPrioridad'), (N'tblSeveridad'),
        (N'tblResultadoPrueba'), (N'tblTipoPrueba'), (N'tblTipoArtefacto'),
        (N'tblTipoAusencia'), (N'tblTipoSolicitud'), (N'tblCategoriaProyecto')

    DECLARE curEnum CURSOR LOCAL FAST_FORWARD FOR SELECT Nombre FROM @enumerados
    OPEN curEnum
    FETCH NEXT FROM curEnum INTO @tabla
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                       WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tabla)
        BEGIN
            SET @sql = N'CREATE TABLE dbo.' + QUOTENAME(@tabla) + N'
            (
                Id              INT           NOT NULL,
                Nombre          NVARCHAR(100) NOT NULL,
                FechaRegistro   DATETIME2     NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + @tabla + '_FechaRegistro') + N' DEFAULT (SYSDATETIME()),
                UsuarioRegistro NVARCHAR(200) NOT NULL,
                UsuarioMovto    NVARCHAR(50)  NULL,
                FechaMovto      DATETIME      NULL,
                Activo          BIT           NOT NULL CONSTRAINT ' + QUOTENAME('DF_' + @tabla + '_Activo') + N' DEFAULT (1),
                CONSTRAINT ' + QUOTENAME('PK_' + @tabla) + N' PRIMARY KEY (Id),
                CONSTRAINT ' + QUOTENAME('UQ_' + @tabla + '_Nombre') + N' UNIQUE (Nombre)
            )'
            EXEC (@sql)
            PRINT 'OK: ' + @tabla + ' creada'
        END
        ELSE
            PRINT 'SKIP: ' + @tabla + ' ya existe'
        FETCH NEXT FROM curEnum INTO @tabla
    END
    CLOSE curEnum
    DEALLOCATE curEnum

    /* ---------- 3. Catalogos gestionados ---------- */
    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblNivel')
    BEGIN
        CREATE TABLE dbo.tblNivel
        (
            IdNivel         INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            Orden           INT                         NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblNivel_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblNivel_Activo DEFAULT (1),
            CONSTRAINT PK_tblNivel PRIMARY KEY (IdNivel),
            CONSTRAINT UQ_tblNivel_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblNivel creada'
    END
    ELSE
        PRINT 'SKIP: tblNivel ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblComplejidad')
    BEGIN
        CREATE TABLE dbo.tblComplejidad
        (
            IdComplejidad        INT           IDENTITY(1,1) NOT NULL,
            Nombre               NVARCHAR(100)               NOT NULL,
            IdCategoriaProyecto  INT                         NULL,
            Orden                INT                         NOT NULL,
            FechaRegistro        DATETIME2                   NOT NULL CONSTRAINT DF_tblComplejidad_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro      NVARCHAR(200)               NOT NULL,
            UsuarioMovto         NVARCHAR(50)                NULL,
            FechaMovto           DATETIME                    NULL,
            Activo               BIT                         NOT NULL CONSTRAINT DF_tblComplejidad_Activo DEFAULT (1),
            CONSTRAINT PK_tblComplejidad PRIMARY KEY (IdComplejidad),
            CONSTRAINT UQ_tblComplejidad_Nombre UNIQUE (Nombre),
            CONSTRAINT FK_tblComplejidad_tblCategoriaProyecto FOREIGN KEY (IdCategoriaProyecto) REFERENCES dbo.tblCategoriaProyecto (Id)
        )
        PRINT 'OK: tblComplejidad creada'
    END
    ELSE
        PRINT 'SKIP: tblComplejidad ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblMatrizPresupuesto')
    BEGIN
        CREATE TABLE dbo.tblMatrizPresupuesto
        (
            IdMatrizPresupuesto INT           IDENTITY(1,1) NOT NULL,
            IdComplejidad       INT                         NOT NULL,
            IdNivel             INT                         NOT NULL,
            Minutos             INT                         NOT NULL,
            Puntos              DECIMAL(6,2)                NULL,
            FechaRegistro       DATETIME2                   NOT NULL CONSTRAINT DF_tblMatrizPresupuesto_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)               NOT NULL,
            UsuarioMovto        NVARCHAR(50)                NULL,
            FechaMovto          DATETIME                    NULL,
            Activo              BIT                         NOT NULL CONSTRAINT DF_tblMatrizPresupuesto_Activo DEFAULT (1),
            CONSTRAINT PK_tblMatrizPresupuesto PRIMARY KEY (IdMatrizPresupuesto),
            CONSTRAINT UQ_tblMatrizPresupuesto_ComplejidadNivel UNIQUE (IdComplejidad, IdNivel),
            CONSTRAINT FK_tblMatrizPresupuesto_tblComplejidad FOREIGN KEY (IdComplejidad) REFERENCES dbo.tblComplejidad (IdComplejidad),
            CONSTRAINT FK_tblMatrizPresupuesto_tblNivel FOREIGN KEY (IdNivel) REFERENCES dbo.tblNivel (IdNivel),
            CONSTRAINT CK_tblMatrizPresupuesto_Minutos CHECK (Minutos > 0)
        )
        PRINT 'OK: tblMatrizPresupuesto creada'
    END
    ELSE
        PRINT 'SKIP: tblMatrizPresupuesto ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblEtiqueta')
    BEGIN
        CREATE TABLE dbo.tblEtiqueta
        (
            IdEtiqueta      INT           IDENTITY(1,1) NOT NULL,
            Nombre          NVARCHAR(100)               NOT NULL,
            FechaRegistro   DATETIME2                   NOT NULL CONSTRAINT DF_tblEtiqueta_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro NVARCHAR(200)               NOT NULL,
            UsuarioMovto    NVARCHAR(50)                NULL,
            FechaMovto      DATETIME                    NULL,
            Activo          BIT                         NOT NULL CONSTRAINT DF_tblEtiqueta_Activo DEFAULT (1),
            CONSTRAINT PK_tblEtiqueta PRIMARY KEY (IdEtiqueta),
            CONSTRAINT UQ_tblEtiqueta_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblEtiqueta creada'
    END
    ELSE
        PRINT 'SKIP: tblEtiqueta ya existe'

    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'tblCategoriaTicket')
    BEGIN
        CREATE TABLE dbo.tblCategoriaTicket
        (
            IdCategoriaTicket INT           IDENTITY(1,1) NOT NULL,
            Nombre            NVARCHAR(100)               NOT NULL,
            FechaRegistro     DATETIME2                   NOT NULL CONSTRAINT DF_tblCategoriaTicket_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro   NVARCHAR(200)               NOT NULL,
            UsuarioMovto      NVARCHAR(50)                NULL,
            FechaMovto        DATETIME                    NULL,
            Activo            BIT                         NOT NULL CONSTRAINT DF_tblCategoriaTicket_Activo DEFAULT (1),
            CONSTRAINT PK_tblCategoriaTicket PRIMARY KEY (IdCategoriaTicket),
            CONSTRAINT UQ_tblCategoriaTicket_Nombre UNIQUE (Nombre)
        )
        PRINT 'OK: tblCategoriaTicket creada'
    END
    ELSE
        PRINT 'SKIP: tblCategoriaTicket ya existe'

    COMMIT TRANSACTION
    PRINT '===== Parte 1 (DDL) ejecutada correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK (Parte 1) ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO

/* ---------- Parte 2: Seeds (batch separado: las tablas ya existen) ---------- */
SET XACT_ABORT ON
BEGIN TRANSACTION
BEGIN TRY

    /* Estatus: los IDs son contrato del motor de workflow */
    INSERT INTO dbo.tblEstatusSolicitud (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Borrador',1),(2,N'Enviada',2),(3,N'En Analisis',3),(4,N'Aprobada',4),
                 (5,N'Rechazada',5),(6,N'Convertida',6),(7,N'Cancelada',7)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusSolicitud e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusWorkItem (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Pendiente',1),(2,N'En Proceso',2),(3,N'En Pruebas',3),(4,N'Correccion',4),
                 (5,N'Suspendido',5),(6,N'Terminado',6),(7,N'Cancelado',7)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusWorkItem e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusSprint (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Planeado',1),(2,N'Activo',2),(3,N'Cerrado',3)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusSprint e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusRelease (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'En Preparacion',1),(2,N'En Aprobacion',2),(3,N'Aprobado',3),
                 (4,N'Liberado',4),(5,N'Revertido',5),(6,N'Cancelado',6)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusRelease e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusDespliegue (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'En Ejecucion',1),(2,N'Exitoso',2),(3,N'Fallido',3)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusDespliegue e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusIncidente (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Detectado',1),(2,N'En Atencion',2),(3,N'Mitigado',3),
                 (4,N'Resuelto',4),(5,N'Cerrado',5)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusIncidente e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusTicket (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Nuevo',1),(2,N'Asignado',2),(3,N'En Atencion',3),(4,N'Esperando Usuario',4),
                 (5,N'Resuelto',5),(6,N'Cerrado',6)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusTicket e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusRiesgo (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Identificado',1),(2,N'En Mitigacion',2),(3,N'Materializado',3),(4,N'Cerrado',4)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusRiesgo e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusRevision (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Pendiente',1),(2,N'En Proceso',2),(3,N'Terminada',3)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusRevision e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusAprobacion (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Pendiente',1),(2,N'Aprobada',2),(3,N'Rechazada',3)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusAprobacion e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusProyecto (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Propuesto',1),(2,N'Autorizado',2),(3,N'En Ejecucion',3),(4,N'En Pausa',4),
                 (5,N'Cerrado',5),(6,N'Cancelado',6)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusProyecto e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblEstatusAusencia (Id, Descripcion, Orden)
    SELECT v.Id, v.Descripcion, v.Orden
    FROM (VALUES (1,N'Solicitada',1),(2,N'Aprobada',2),(3,N'Rechazada',3),(4,N'Cancelada',4)) v(Id,Descripcion,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblEstatusAusencia e WHERE e.Id = v.Id)

    PRINT 'OK: seeds de estatus aplicados'

    /* Enumerados de ID fijo */
    INSERT INTO dbo.tblTipoWorkItem (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Epica'),(2,N'Feature'),(3,N'Historia'),(4,N'Tarea'),(5,N'Bug'),
                 (6,N'Cambio'),(7,N'Mejora'),(8,N'Soporte'),(9,N'Correccion')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoWorkItem e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblTipoVinculo (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Bloquea'),(2,N'Duplica'),(3,N'Relacionado'),(4,N'Deriva De')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoVinculo e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblPrioridad (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Critica'),(2,N'Alta'),(3,N'Media'),(4,N'Baja')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPrioridad e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblSeveridad (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'S1 - Critica'),(2,N'S2 - Alta'),(3,N'S3 - Media'),(4,N'S4 - Baja')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblSeveridad e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblResultadoPrueba (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Pasa'),(2,N'Falla'),(3,N'Bloqueado'),(4,N'No Aplica')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblResultadoPrueba e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblTipoPrueba (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Manual'),(2,N'Automatizada'),(3,N'Regresion')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoPrueba e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblTipoArtefacto (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Paquete'),(2,N'Script SQL'),(3,N'Archivo de Configuracion'),(4,N'Otro')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoArtefacto e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblTipoAusencia (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Vacaciones'),(2,N'Incapacidad'),(3,N'Permiso'),(4,N'Home Office')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoAusencia e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblTipoSolicitud (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Nuevo Desarrollo'),(2,N'Mejora'),(3,N'Correccion'),(4,N'Soporte')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblTipoSolicitud e WHERE e.Id = v.Id)

    INSERT INTO dbo.tblCategoriaProyecto (Id, Nombre, UsuarioRegistro)
    SELECT v.Id, v.Nombre, N'script-despliegue'
    FROM (VALUES (1,N'Desarrollo'),(2,N'TI'),(3,N'Mantenimiento')) v(Id,Nombre)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblCategoriaProyecto e WHERE e.Id = v.Id)

    PRINT 'OK: seeds de enumerados aplicados'

    /* Niveles de ingeniero (gestionado; nombres heredados del GT) */
    INSERT INTO dbo.tblNivel (Nombre, Orden, UsuarioRegistro)
    SELECT v.Nombre, v.Orden, N'script-despliegue'
    FROM (VALUES (N'Junior',1),(N'Senior',2),(N'Master',3)) v(Nombre,Orden)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblNivel e WHERE e.Nombre = v.Nombre)

    PRINT 'OK: seeds de niveles aplicados'

    COMMIT TRANSACTION
    PRINT '===== Script ejecutado correctamente ====='
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    PRINT '===== ERROR - Se hizo ROLLBACK (Parte 2 seeds) ====='
    PRINT 'Mensaje : ' + ERROR_MESSAGE()
    PRINT 'Linea   : ' + CAST(ERROR_LINE()   AS NVARCHAR(10))
    PRINT 'Numero  : ' + CAST(ERROR_NUMBER() AS NVARCHAR(10))
END CATCH
GO
