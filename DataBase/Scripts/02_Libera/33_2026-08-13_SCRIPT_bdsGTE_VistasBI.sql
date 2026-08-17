USE [bdsGTE]
GO
SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO
/* =====================================================================
   Script:      33_2026-08-13_SCRIPT_bdsGTE_VistasBI.sql
   Autor:       Equipo GTE
   Descripcion: Vistas vwBI* (Doctos/GTE-DocumentoMaestro.md seccion 13:
                "vistas vwBI* con contrato estable" para Power BI u otra
                herramienta de BI, consumidas por el usuario de solo
                lectura del script 32). Son tablas de hechos planas
                (una fila por WorkItem/registro de costo/release/riesgo/
                ticket), con nombres ya resueltos -- para que el analista
                de BI arme sus propios pivotes sin tener que unir
                catalogos el mismo.

                No todos los reportes tienen una vista BI equivalente:
                R13 (Flujo/CFD) necesita reconstruir el estado de cada
                item dia por dia (no es una fila-por-hecho simple) y R14
                (Auditoria) es sensible/paginada -- ambos se calculan
                solo en la API, no se exponen aqui. R02/R07/R11 ya son
                planas de origen (tblRegistroTiempo/tblSolicitud/
                tblKpiValor) y no necesitan una vista adicional.

                Objetos programables: CREATE OR ALTER como primera
                instruccion de su batch (igual que el resto de vistas de
                GTE, ver script 08), sin la transaccion unica de los
                scripts de datos.
   Requiere:    01-30 (todas las tablas transaccionales referenciadas).
   ===================================================================== */
PRINT 'Inicia creacion de vistas BI...'
GO

/* ---------- vwBIWorkItems ---------- */
CREATE OR ALTER VIEW dbo.vwBIWorkItems
AS
SELECT
    w.IdWorkItem, w.Folio, w.Titulo,
    w.IdProyecto, p.Nombre AS Proyecto, p.Clave AS ClaveProyecto,
    w.IdEquipo, eq.Nombre AS Equipo,
    w.IdTipoWorkItem, tw.Nombre AS Tipo,
    w.IdEstatusWorkItem, ew.Descripcion AS Estatus,
    w.IdAsignado, u.Nombre AS Asignado,
    w.IdPrioridad, pr.Nombre AS Prioridad,
    w.PuntosHistoria, w.MinutosPresupuesto, ti.MinutosInvertidos,
    w.FechaRegistro, w.FechaInicio, w.FechaCompromiso, w.FechaFin,
    CASE WHEN w.FechaCompromiso IS NULL OR w.FechaFin IS NULL THEN NULL
         WHEN w.FechaFin <= w.FechaCompromiso THEN 1 ELSE 0 END AS EntregaATiempo
FROM dbo.tblWorkItem w
INNER JOIN dbo.tblProyecto p ON p.IdProyecto = w.IdProyecto
LEFT JOIN dbo.tblEquipo eq ON eq.IdEquipo = w.IdEquipo
INNER JOIN dbo.tblTipoWorkItem tw ON tw.Id = w.IdTipoWorkItem
INNER JOIN dbo.tblEstatusWorkItem ew ON ew.Id = w.IdEstatusWorkItem
LEFT JOIN dbo.tblUsuario u ON u.IdUsuario = w.IdAsignado
INNER JOIN dbo.tblPrioridad pr ON pr.Id = w.IdPrioridad
LEFT JOIN dbo.vwTiempoInvertido ti ON ti.IdWorkItem = w.IdWorkItem
WHERE w.Activo = 1
GO
PRINT 'OK: vwBIWorkItems'
GO

/* ---------- vwBICostos ---------- */
CREATE OR ALTER VIEW dbo.vwBICostos
AS
SELECT
    c.IdRegistroTiempo, c.IdProyecto, p.Nombre AS Proyecto, p.Clave AS ClaveProyecto,
    c.IdUsuario, u.Nombre AS Usuario, c.Fecha, c.Minutos, c.CostoHora, c.Costo
FROM dbo.vwCostoRegistroTiempo c
INNER JOIN dbo.tblProyecto p ON p.IdProyecto = c.IdProyecto
INNER JOIN dbo.tblUsuario u ON u.IdUsuario = c.IdUsuario
GO
PRINT 'OK: vwBICostos'
GO

/* ---------- vwBIReleases ---------- */
CREATE OR ALTER VIEW dbo.vwBIReleases
AS
SELECT
    r.IdRelease, r.Folio, r.Version, r.IdProyecto, p.Nombre AS Proyecto,
    r.IdEstatusRelease, es.Descripcion AS Estatus,
    r.FechaRegistro, r.FechaPlan, r.FechaLiberacion,
    (SELECT COUNT(*) FROM dbo.tblWorkItem w WHERE w.IdRelease = r.IdRelease) AS ItemsIncluidos
FROM dbo.tblRelease r
INNER JOIN dbo.tblProyecto p ON p.IdProyecto = r.IdProyecto
INNER JOIN dbo.tblEstatusRelease es ON es.Id = r.IdEstatusRelease
WHERE r.Activo = 1
GO
PRINT 'OK: vwBIReleases'
GO

/* ---------- vwBIRiesgos ---------- */
CREATE OR ALTER VIEW dbo.vwBIRiesgos
AS
SELECT
    r.IdRiesgo, r.IdProyecto, p.Nombre AS Proyecto, r.Descripcion,
    r.Probabilidad, r.Impacto, ISNULL(r.Exposicion, r.Probabilidad * r.Impacto) AS Exposicion,
    r.IdEstatusRiesgo, es.Descripcion AS Estatus, r.FechaRegistro
FROM dbo.tblRiesgo r
INNER JOIN dbo.tblProyecto p ON p.IdProyecto = r.IdProyecto
INNER JOIN dbo.tblEstatusRiesgo es ON es.Id = r.IdEstatusRiesgo
WHERE r.Activo = 1
GO
PRINT 'OK: vwBIRiesgos'
GO

/* ---------- vwBISla ---------- */
CREATE OR ALTER VIEW dbo.vwBISla
AS
SELECT
    t.IdTicket, t.Folio, t.IdPrioridad, pr.Nombre AS Prioridad,
    t.IdAsignado, u.Nombre AS Agente,
    t.FechaRegistro, t.FechaLimiteRespuesta, t.FechaPrimeraRespuesta,
    t.FechaLimiteResolucion, t.FechaResolucion,
    CASE WHEN t.FechaLimiteResolucion IS NULL OR t.FechaResolucion IS NULL THEN NULL
         WHEN t.FechaResolucion <= t.FechaLimiteResolucion THEN 1 ELSE 0 END AS DentroSla,
    enc.Calificacion AS Csat
FROM dbo.tblTicket t
INNER JOIN dbo.tblPrioridad pr ON pr.Id = t.IdPrioridad
LEFT JOIN dbo.tblUsuario u ON u.IdUsuario = t.IdAsignado
LEFT JOIN dbo.tblEncuestaSatisfaccion enc ON enc.IdTicket = t.IdTicket
WHERE t.Activo = 1
GO
PRINT 'OK: vwBISla'
GO

PRINT 'Vistas BI creadas correctamente'
GO
