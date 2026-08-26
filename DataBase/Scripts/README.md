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

## Tanda de despliegue (2026-08-01)

| Script | Contenido |
|---|---|
| 01_2026-08-01_SCRIPT_bdsGTE_UsuarioServicio.sql | Login de Windows (cuenta de servicio) + usuario en bdsGTE con permisos minimos (db_datareader/db_datawriter + EXECUTE sobre spCambiarEstatus/spGenerarFolio/spRegistrarBitacora/spSnapshotKpi). **Excepcion deliberada**: el Bloque 1 corre contra `[master]` (el login es un principal de servidor), no contra bdsGTE -- unico script de esta carpeta que lo hace. Ajustar la variable `@NombreLogin` en los dos bloques antes de correrlo. Ver `Doctos/MANUAL_INSTALACION_GTE.md` |

## Catalogo de reglas de negocio (2026-08-24)

| Script | Contenido |
|---|---|
| 43_2026-08-24_SCRIPT_bdsGTE_CatalogoReglasNegocio.sql | Esquema del modulo: 3 enumerados de ID fijo (`tblTipoAmbitoRegla`, `tblEstadoReglaNegocio`, `tblTipoRelacionRegla` -- IDs son CONTRATO, los referencia `GTE.Domain/ReglasNegocio`), `tblAmbitoRegla` (flujos y caracteristicas, catalogo propio de cada proyecto), `tblReglaNegocio` (una regla, un proyecto DUENO), `tblReglaNegocioVersion` (historial del enunciado), `tblReglaNegocioImpacto` (proyectos secundarios afectados, lista explicita) y `tblReglaNegocioRelacion`. Permisos `RGN.Ver`/`RGN.Administrar` sembrados solo al rol Administrador |
| 44_2026-08-24_INSERT_bdsGTE_ReglasNegocioGTE.sql | Alta del proyecto `GTE` en `tblProyecto` (idempotente por Clave) + sus 9 flujos de operacion + las 40 reglas del Documento Maestro con su estado real + version 1 de cada una + 5 relaciones entre reglas. Siembra con las claves HISTORICAS (`RN-REQ-01`, `RN-QA-06`...); el script 45 las renumera despues. Lee la cabecera del script: documenta dos discrepancias reales entre el Documento Maestro y el codigo (una regla que existe en codigo sin documentar y otra clave con dos significados distintos) |
| 45_2026-08-24_UPDATE_bdsGTE_RenumeraReglasGTE.sql | Renumera esas 40 reglas al formato uniforme `RN-GTE-001..040` (orden de las secciones 3.x del Documento Maestro), reescribe las referencias cruzadas dentro de los enunciados y deja la serie de folio `RN-GTE` en 40. Idempotente: solo renombra si la clave vieja todavia existe |

**Contrato particular de este modulo**: una regla no puede impactar a su propio proyecto
dueno, y eso se garantiza de forma declarativa (sin trigger) con `IdProyectoDueno`
desnormalizado en la fila de impacto + FK compuesta contra `UQ_tblReglaNegocio_IdProyecto`
+ `CHECK IdProyectoAfectado <> IdProyectoDueno`. No quitar ninguna de las tres piezas por
separado: solo funcionan juntas.

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
