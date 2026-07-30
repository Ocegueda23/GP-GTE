# Scripts de despliegue de bdsGTE

Nomenclatura: `<Secuencia>_<AAAA-MM-DD>_<Categoria>_<Objeto>.sql` (la secuencia fija el
orden dentro de la tanda). Todo script es **idempotente**: se puede correr N veces
(segunda corrida = puro `SKIP`).

## Como ejecutar

- En orden 01 a 10, contra el servidor destino.
- SSMS: abrir y ejecutar (QUOTED_IDENTIFIER ya viene ON). Los scripts tambien lo fijan
  explicitamente por si se usa sqlcmd (`sqlcmd -I -i script.sql`); los indices filtrados
  lo requieren.
- TODOS los scripts corren contra `bdsGTE` (el 01 crea la base si no existe, con colacion
  `Modern_Spanish_CI_AS`). GTE es totalmente independiente: no toca ninguna otra base.

## Tanda inicial (validada 2026-07-30 en LocalDB: 2 corridas limpias + pruebas funcionales)

| Script | Contenido |
|---|---|
| 01 Catalogos | Base bdsGTE + 12 catalogos de estatus (estructura estandar del motor, IDs = CONTRATO) + 10 enumerados de ID fijo + gestionados (niveles, complejidad, matriz de presupuesto, etiquetas, categorias de ticket) + seeds |
| 02 Administracion | Areas, puestos, horarios con tramos (seeds heredados del GT: BANSI, EXALXKA, EXITSEEKER, BECARIO), festivos, usuarios (jerarquia IdJefe), RBAC (roles/permisos/asignaciones con alcance; Administrador recibe todo), equipos, ausencias |
| 03 Portafolio | Portafolios, programas, proyectos, hitos, riesgos (exposicion computada), OKRs, tarifas por nivel, presupuestos, ambientes (seed DEV/QA/PREPROD/PROD), repositorios + FK pendiente de tblUsuarioRol |
| 04 Nucleo | Sprints, capacidad, tableros kanban, solicitudes, tblWorkItem (entidad unificada, con indices de bandeja), registro de tiempo, revisiones, vinculos, comentarios, archivos por GUID, tblHistorialEstatus (hechos temporales con MinutosLaborales materializado), tblHistorialCampo |
| 05 DesarrolloQaReleases | Commits, PRs, pipelines, artefactos, releases (rollback pareado), despliegues, aprobaciones con firma, planes/casos/pasos/ciclos/ejecuciones de prueba + FKs pendientes de tblWorkItem |
| 06 OperacionSoporte | Incidentes, bitacora de cambios, SLA, tickets, encuestas, base de conocimiento con versionado |
| 07 Transversales | MOTOR DE ESTATUS PROPIO (tblProceso, tblTransicion) + tblFolio, tblBitacora (espejo de la entidad EF), notificaciones, plantillas, reglas de automatizacion (JSON validado con ISJSON), outbox de eventos, KPIs, versiones del sistema, tblTransicionConfig (metadatos de UI del workflow) |
| 08 Programables | fnMinutosLaborales (motor UNICO de tiempo laborable, inline sin cursor, con festivos), spCambiarEstatus (motor de estatus propio: UPDATE dinamico blindado + guard de concurrencia + materializacion de historial), spGenerarFolio (ROWLOCK/UPDLOCK/HOLDLOCK), spRegistrarBitacora, spSnapshotKpi, trWorkItemHistorialCampo, vwTiempoInvertido, vwBandejaTrabajo. Usa batches GO + CREATE OR ALTER (los programables deben abrir batch) |
| 09 INSERT Procesos | Alta de los 11 procesos GTE en dbo.tblProceso + ~55 transiciones en dbo.tblTransicion (todo en bdsGTE) |
| 10 Verificacion | Solo lectura: comprueba los 100 objetos esperados + seeds de contrato (incluye tblProceso/tblTransicion pobladas); imprime FALTA/EXITOSA. Usar para comparar dev/preprod/prod |

## Tanda 2 (2026-07-30)

| Script | Contenido |
|---|---|
| 01_2026-07-30_INSERT_bdsGTE_TransicionesYEtiquetas.sql | Transicion WorkItem Terminado a Correccion (RECHAZAR_QA) que necesita el modulo de Revisiones + siembra de tblTransicionConfig con etiquetas de boton, permisos y motivos obligatorios de las 21 transiciones de WorkItem, Solicitud y Revision |

## Contratos importantes

- **GTE es totalmente independiente**: una sola base (`bdsGTE`), sin referencias a ninguna
  otra (ADR-03 del Documento Maestro, decision del equipo 2026-07-30).
- **IDs de estatus y enumerados son contrato** (los referencian tblTransicion, las vistas
  y el backend). No cambiarlos ni reordenarlos.
- `tblHistorialEstatus.Proceso` usa el NOMBRE del proceso (`'WorkItem'`, `'Ticket'`...),
  igual que `spCambiarEstatus` y `tblTransicionConfig`.
- `DiaSemana` en tblHorarioTramo: 1 = lunes ... 7 = domingo (independiente de DATEFIRST).
- `spCambiarEstatus` es el SP generico del motor: los procesos nuevos se dan de alta con
  DATOS (tblProceso/tblTransicion), nunca modificando el SP.

## Pendientes (proximas tandas)

- `spImportarJira` / importador GT (fase de migracion, seccion 15.4 del Documento Maestro).
- Calculo inverso de SLA (`SumarMinutosLaborales`): decidido implementarlo en el backend
  (`ICalendarioLaboral`), no en SQL.
- Vistas `vwBI*` para Power BI (fase 5).
