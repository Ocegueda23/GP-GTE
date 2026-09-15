USE [bdsGTE]
GO
SET XACT_ABORT ON
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
BEGIN TRANSACTION
/* =====================================================================
   Script:      02_2026-08-27_SCRIPT_bdsGTE_CentroMandoEsquema.sql
   Autor:       Equipo GTE
   Version:     1.0.0
   Descripcion: Esquema del Centro de Mando TI: evaluacion mensual de los
                responsables de area (modelo descrito en el documento de
                Ayuda "Centro de Mando TI").

                POR QUE TABLAS NUEVAS Y NO tblKpiDefinicion/tblKpiValor:
                esas dos ya tienen dueno y semantica propia -- una serie
                de tiempo de un numero global, alimentada por
                dbo.spSnapshotKpi y consumida por el widget "KPIs
                personalizados" del Dashboard Ejecutivo P18. Lo que
                necesita este modelo es distinto: un catalogo con
                categoria, ambito por area, meta, umbral de alerta, peso
                y accion gerencial, evaluado POR EQUIPO Y POR MES para
                producir un score 0-100. Meter ambas cosas en la misma
                tabla ensuciaria el widget del P18 (apareceria una serie
                por cada indicador nuevo) y obligaria a columnas que la
                mitad de las filas no usaria.

                UNIDAD DE EVALUACION = EQUIPO (dbo.tblEquipo), y el
                responsable evaluado es su lider (tblEquipo.IdLider) --
                decision del equipo el 2026-08-27. Se eligio sobre
                tblArea porque tblArea no tiene responsable y porque
                tblEquipo.IdLider ya existe, ya se usa para resolver
                alcance en el Dashboard Ejecutivo y ya tiene miembros
                (tblEquipoMiembro) de donde sale la capacidad del area.

   Tablas:      tblIndicadorGestion        catalogo del modelo
                tblEvaluacionEquipo        score mensual por equipo
                tblEvaluacionEquipoDetalle valor por indicador
                tblDiagnosticoCausa        Persona/Proceso/Recursos/...
                tblAlertaGestion           alertas gerenciales

   Permisos:    GES.Ver         consultar el Centro de Mando
                GES.Administrar editar catalogo, metas, umbrales y pesos

   Requiere:    01 y 02 (tblPermiso, tblRol, tblRolPermiso, tblEquipo,
                tblUsuario) aplicados.
   ===================================================================== */
BEGIN TRY

    /* ---------------------------------------------------------------
       1. Catalogo de indicadores
       --------------------------------------------------------------- */
    IF OBJECT_ID('dbo.tblIndicadorGestion', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblIndicadorGestion
        (
            IdIndicadorGestion  INT            IDENTITY(1,1) NOT NULL,
            Clave               NVARCHAR(100)                NOT NULL,
            Nombre              NVARCHAR(200)                NOT NULL,
            Descripcion         NVARCHAR(1000)               NULL,
            -- Categoria del modelo (Cumplimiento, Productividad, Calidad, SLA, ...)
            Categoria           NVARCHAR(60)                 NOT NULL,
            -- Comun = aplica a los tres responsables; el resto es el bloque tecnico del area.
            Ambito              NVARCHAR(30)                 NOT NULL,
            -- Automatico = lo calcula el motor desde datos de GTE.
            -- Manual = requiere captura (backups, disponibilidad, seguridad...): sin fuente
            -- en GTE hoy, se reporta "sin datos" en vez de inventar un numero.
            Origen              NVARCHAR(20)                 NOT NULL,
            Formula             NVARCHAR(500)                NULL,
            Unidad              NVARCHAR(20)                 NOT NULL,
            Meta                DECIMAL(18,4)                NULL,
            UmbralAlerta        DECIMAL(18,4)                NULL,
            -- Subir = mas alto es mejor; Bajar = mas bajo es mejor.
            Direccion           NVARCHAR(10)                 NOT NULL,
            Peso                DECIMAL(9,4)                 NOT NULL CONSTRAINT DF_tblIndicadorGestion_Peso DEFAULT (0),
            -- 0 para indicadores que son senal diagnostica y no meta a perseguir
            -- (ej. "% de tiempo en soporte reactivo"): se muestran pero no puntuan.
            PonderaEnScore      BIT                          NOT NULL CONSTRAINT DF_tblIndicadorGestion_Pondera DEFAULT (1),
            Periodicidad        NVARCHAR(20)                 NOT NULL CONSTRAINT DF_tblIndicadorGestion_Periodicidad DEFAULT (N'Mensual'),
            InterpretacionBuena NVARCHAR(500)                NULL,
            InterpretacionMala  NVARCHAR(500)                NULL,
            AccionSugerida      NVARCHAR(1000)               NULL,
            FechaRegistro       DATETIME2                    NOT NULL CONSTRAINT DF_tblIndicadorGestion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)                NOT NULL,
            UsuarioMovto        NVARCHAR(50)                 NULL,
            FechaMovto          DATETIME                     NULL,
            Activo              BIT                          NOT NULL CONSTRAINT DF_tblIndicadorGestion_Activo DEFAULT (1),
            CONSTRAINT PK_tblIndicadorGestion PRIMARY KEY (IdIndicadorGestion),
            CONSTRAINT UQ_tblIndicadorGestion_Clave UNIQUE (Clave),
            CONSTRAINT CK_tblIndicadorGestion_Direccion CHECK (Direccion IN (N'Subir', N'Bajar')),
            CONSTRAINT CK_tblIndicadorGestion_Origen CHECK (Origen IN (N'Automatico', N'Manual')),
            CONSTRAINT CK_tblIndicadorGestion_Ambito CHECK (Ambito IN (N'Comun', N'Desarrollo', N'Infraestructura', N'Soporte'))
        )
        PRINT 'OK: tblIndicadorGestion creada'
    END
    ELSE
        PRINT 'SKIP: tblIndicadorGestion ya existe'

    /* ---------------------------------------------------------------
       2. Evaluacion mensual por equipo
       --------------------------------------------------------------- */
    IF OBJECT_ID('dbo.tblEvaluacionEquipo', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblEvaluacionEquipo
        (
            IdEvaluacionEquipo  INT            IDENTITY(1,1) NOT NULL,
            IdEquipo            INT                          NOT NULL,
            -- Lider al momento del calculo: se congela para que la evaluacion historica
            -- siga siendo legible aunque despues cambie el lider del equipo.
            IdResponsable       INT                          NULL,
            Anio                SMALLINT                     NOT NULL,
            Mes                 TINYINT                      NOT NULL,
            ScoreGeneral        DECIMAL(9,2)                 NULL,
            Nivel               NVARCHAR(30)                 NULL,
            Semaforo            NVARCHAR(10)                 NULL,
            IndiceCarga         DECIMAL(9,2)                 NULL,
            IndicadoresConDato  SMALLINT                     NOT NULL CONSTRAINT DF_tblEvaluacionEquipo_ConDato DEFAULT (0),
            IndicadoresTotales  SMALLINT                     NOT NULL CONSTRAINT DF_tblEvaluacionEquipo_Totales DEFAULT (0),
            FechaCalculo        DATETIME2                    NOT NULL CONSTRAINT DF_tblEvaluacionEquipo_FechaCalculo DEFAULT (SYSDATETIME()),
            FechaRegistro       DATETIME2                    NOT NULL CONSTRAINT DF_tblEvaluacionEquipo_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro     NVARCHAR(200)                NOT NULL,
            UsuarioMovto        NVARCHAR(50)                 NULL,
            FechaMovto          DATETIME                     NULL,
            Activo              BIT                          NOT NULL CONSTRAINT DF_tblEvaluacionEquipo_Activo DEFAULT (1),
            CONSTRAINT PK_tblEvaluacionEquipo PRIMARY KEY (IdEvaluacionEquipo),
            CONSTRAINT UQ_tblEvaluacionEquipo_Periodo UNIQUE (IdEquipo, Anio, Mes),
            CONSTRAINT FK_tblEvaluacionEquipo_Equipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo),
            CONSTRAINT FK_tblEvaluacionEquipo_Responsable FOREIGN KEY (IdResponsable) REFERENCES dbo.tblUsuario (IdUsuario),
            CONSTRAINT CK_tblEvaluacionEquipo_Mes CHECK (Mes BETWEEN 1 AND 12)
        )
        PRINT 'OK: tblEvaluacionEquipo creada'
    END
    ELSE
        PRINT 'SKIP: tblEvaluacionEquipo ya existe'

    /* ---------------------------------------------------------------
       3. Detalle: valor de cada indicador en esa evaluacion
       --------------------------------------------------------------- */
    IF OBJECT_ID('dbo.tblEvaluacionEquipoDetalle', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblEvaluacionEquipoDetalle
        (
            IdEvaluacionEquipoDetalle BIGINT      IDENTITY(1,1) NOT NULL,
            IdEvaluacionEquipo        INT                       NOT NULL,
            IdIndicadorGestion        INT                       NOT NULL,
            -- Valor crudo en la unidad del indicador (%, horas, conteo...).
            Valor                     DECIMAL(18,4)             NULL,
            -- Mismo valor llevado a escala 0-100 segun Meta/Direccion, para poder promediar
            -- indicadores de unidades distintas dentro del score.
            ValorNormalizado          DECIMAL(9,2)              NULL,
            Semaforo                  NVARCHAR(10)              NULL,
            -- Sin datos suficientes (denominador 0 u origen Manual sin captura): no participa
            -- en el score, mismo criterio que CalculadoraPuntaje del dashboard de colaborador.
            SinDatos                  BIT                       NOT NULL CONSTRAINT DF_tblEvalEquipoDet_SinDatos DEFAULT (0),
            ValorPeriodoAnterior      DECIMAL(18,4)             NULL,
            FechaRegistro             DATETIME2                 NOT NULL CONSTRAINT DF_tblEvalEquipoDet_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro           NVARCHAR(200)             NOT NULL,
            CONSTRAINT PK_tblEvaluacionEquipoDetalle PRIMARY KEY (IdEvaluacionEquipoDetalle),
            CONSTRAINT UQ_tblEvalEquipoDet_Indicador UNIQUE (IdEvaluacionEquipo, IdIndicadorGestion),
            CONSTRAINT FK_tblEvalEquipoDet_Evaluacion FOREIGN KEY (IdEvaluacionEquipo)
                REFERENCES dbo.tblEvaluacionEquipo (IdEvaluacionEquipo),
            CONSTRAINT FK_tblEvalEquipoDet_Indicador FOREIGN KEY (IdIndicadorGestion)
                REFERENCES dbo.tblIndicadorGestion (IdIndicadorGestion)
        )
        PRINT 'OK: tblEvaluacionEquipoDetalle creada'
    END
    ELSE
        PRINT 'SKIP: tblEvaluacionEquipoDetalle ya existe'

    /* ---------------------------------------------------------------
       4. Diagnostico de causa (Persona / Proceso / Recursos / Dependencia / Prioridad)
       --------------------------------------------------------------- */
    IF OBJECT_ID('dbo.tblDiagnosticoCausa', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblDiagnosticoCausa
        (
            IdDiagnosticoCausa BIGINT        IDENTITY(1,1) NOT NULL,
            IdEvaluacionEquipo INT                         NOT NULL,
            Causa              NVARCHAR(20)                NOT NULL,
            -- Indice que la disparo (espera, cambio de prioridad, carga, manual repetitivo...).
            IndiceClave        NVARCHAR(100)               NOT NULL,
            Valor              DECIMAL(18,4)               NULL,
            Umbral             DECIMAL(18,4)               NULL,
            Evidencia          NVARCHAR(1000)              NULL,
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblDiagnosticoCausa_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            CONSTRAINT PK_tblDiagnosticoCausa PRIMARY KEY (IdDiagnosticoCausa),
            CONSTRAINT UQ_tblDiagnosticoCausa_Indice UNIQUE (IdEvaluacionEquipo, IndiceClave),
            CONSTRAINT FK_tblDiagnosticoCausa_Evaluacion FOREIGN KEY (IdEvaluacionEquipo)
                REFERENCES dbo.tblEvaluacionEquipo (IdEvaluacionEquipo),
            CONSTRAINT CK_tblDiagnosticoCausa_Causa CHECK (Causa IN
                (N'Persona', N'Proceso', N'Recursos', N'Dependencia', N'Prioridad'))
        )
        PRINT 'OK: tblDiagnosticoCausa creada'
    END
    ELSE
        PRINT 'SKIP: tblDiagnosticoCausa ya existe'

    /* ---------------------------------------------------------------
       5. Alertas gerenciales
       --------------------------------------------------------------- */
    IF OBJECT_ID('dbo.tblAlertaGestion', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.tblAlertaGestion
        (
            IdAlertaGestion    BIGINT        IDENTITY(1,1) NOT NULL,
            -- Clave de deduplicacion: mismo hallazgo, mismo equipo, mismo periodo = una alerta.
            -- Evita que el job la reemita en cada corrida.
            Clave              NVARCHAR(200)               NOT NULL,
            Severidad          NVARCHAR(10)                NOT NULL,
            IdEquipo           INT                         NULL,
            IdIndicadorGestion INT                         NULL,
            Anio               SMALLINT                    NOT NULL,
            Mes                TINYINT                     NOT NULL,
            Titulo             NVARCHAR(200)               NOT NULL,
            Mensaje            NVARCHAR(1000)              NULL,
            -- True cuando el origen no es el responsable del equipo sino otra area/proceso:
            -- estas son las que el documento marca como "requieren intervencion de gerencia".
            RequiereGerencia   BIT                         NOT NULL CONSTRAINT DF_tblAlertaGestion_Gerencia DEFAULT (0),
            Atendida           BIT                         NOT NULL CONSTRAINT DF_tblAlertaGestion_Atendida DEFAULT (0),
            AtendidaPor        NVARCHAR(200)               NULL,
            FechaAtendida      DATETIME2                   NULL,
            FechaRegistro      DATETIME2                   NOT NULL CONSTRAINT DF_tblAlertaGestion_FechaRegistro DEFAULT (SYSDATETIME()),
            UsuarioRegistro    NVARCHAR(200)               NOT NULL,
            UsuarioMovto       NVARCHAR(50)                NULL,
            FechaMovto         DATETIME                    NULL,
            Activo             BIT                         NOT NULL CONSTRAINT DF_tblAlertaGestion_Activo DEFAULT (1),
            CONSTRAINT PK_tblAlertaGestion PRIMARY KEY (IdAlertaGestion),
            CONSTRAINT UQ_tblAlertaGestion_Clave UNIQUE (Clave),
            CONSTRAINT FK_tblAlertaGestion_Equipo FOREIGN KEY (IdEquipo) REFERENCES dbo.tblEquipo (IdEquipo),
            CONSTRAINT FK_tblAlertaGestion_Indicador FOREIGN KEY (IdIndicadorGestion)
                REFERENCES dbo.tblIndicadorGestion (IdIndicadorGestion),
            CONSTRAINT CK_tblAlertaGestion_Severidad CHECK (Severidad IN (N'Critica', N'Atencion', N'Positiva')),
            CONSTRAINT CK_tblAlertaGestion_Mes CHECK (Mes BETWEEN 1 AND 12)
        )
        PRINT 'OK: tblAlertaGestion creada'
    END
    ELSE
        PRINT 'SKIP: tblAlertaGestion ya existe'

    /* ---------------------------------------------------------------
       6. Indices de apoyo
       --------------------------------------------------------------- */
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblEvaluacionEquipo_Periodo'
                   AND object_id = OBJECT_ID('dbo.tblEvaluacionEquipo'))
    BEGIN
        CREATE INDEX IX_tblEvaluacionEquipo_Periodo
            ON dbo.tblEvaluacionEquipo (Anio, Mes, Activo)
            INCLUDE (IdEquipo, ScoreGeneral, Semaforo)
        PRINT 'OK: IX_tblEvaluacionEquipo_Periodo creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblEvaluacionEquipo_Periodo ya existe'

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_tblAlertaGestion_Vigentes'
                   AND object_id = OBJECT_ID('dbo.tblAlertaGestion'))
    BEGIN
        CREATE INDEX IX_tblAlertaGestion_Vigentes
            ON dbo.tblAlertaGestion (Activo, Atendida, Anio, Mes)
            INCLUDE (Severidad, IdEquipo, Titulo)
        PRINT 'OK: IX_tblAlertaGestion_Vigentes creado'
    END
    ELSE
        PRINT 'SKIP: IX_tblAlertaGestion_Vigentes ya existe'

    /* ---------------------------------------------------------------
       7. Permisos
       --------------------------------------------------------------- */
    INSERT INTO dbo.tblPermiso (Clave, Modulo, Descripcion, UsuarioRegistro)
    SELECT v.Clave, v.Modulo, v.Descripcion, N'script-despliegue'
    FROM (VALUES
        (N'GES.Ver', N'CentroMando',
         N'Consultar el Centro de Mando TI: scores, evaluaciones, diagnostico y alertas'),
        (N'GES.Administrar', N'CentroMando',
         N'Editar el catalogo de indicadores: metas, umbrales, pesos y acciones sugeridas')
        ) v(Clave, Modulo, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.tblPermiso p WHERE p.Clave = v.Clave)
    PRINT 'OK: permisos GES.* sembrados (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

    INSERT INTO dbo.tblRolPermiso (IdRol, IdPermiso, UsuarioRegistro)
    SELECT r.IdRol, p.IdPermiso, N'script-despliegue'
    FROM dbo.tblRol r
    CROSS JOIN dbo.tblPermiso p
    WHERE r.Nombre = N'Administrador'
      AND p.Clave IN (N'GES.Ver', N'GES.Administrar')
      AND NOT EXISTS (SELECT 1 FROM dbo.tblRolPermiso rp
                      WHERE rp.IdRol = r.IdRol AND rp.IdPermiso = p.IdPermiso)
    PRINT 'OK: permisos GES.* asignados al rol Administrador (' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' filas)'

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
