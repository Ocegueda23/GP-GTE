# Prompt para la siguiente sesión

Copiar el bloque de abajo tal cual en el chat nuevo. Sustituir la sección
**Objetivo** si se decide atacar otro frente (ver alternativas al final).

---

## Prompt (listo para copiar)

Trabajo en **GTE (Gestor Tecnológico Empresarial)**, la plataforma que sustituye al
Gestor de Proyectos WinForms de Interflo. El repositorio es `C:\CODE\GTE`
(GitHub: `Ocegueda23/GP-GTE`, rama `main`).

**Antes de escribir código, lee en este orden:**

1. `Doctos/PENDIENTES.md` — estado actual, pendientes por prioridad, decisiones firmes
   y trampas técnicas ya resueltas. Es la fuente al día.
2. `CLAUDE.md` (raíz del repo) — reglas duras de arquitectura y estilo.
3. `Doctos/GTE-DocumentoMaestro.md` — solo las secciones del módulo que vayamos a tocar
   (es largo; no lo leas completo).

**Objetivo de esta sesión: el módulo de Administración**, que es el bloqueante B1 del
documento de pendientes. Hoy no existe forma de crear proyectos, equipos, usuarios,
asignar roles, horarios ni ambientes desde la aplicación —todo se hace por SQL—, y por
eso GTE todavía no se le puede entregar a nadie más.

Alcance concreto:

- **Proyectos**: alta y edición (clave, nombre, categoría, equipo, responsable, fechas
  plan, bandera de mantenimiento) y cambio de estatus por el motor de workflow.
- **Equipos y miembros**: alta de equipo con líder, agregar y quitar miembros con su
  porcentaje de dedicación.
- **Usuarios**: alta manual, baja lógica, nivel, horario, jefe y correo.
- **Roles**: asignar y retirar roles a un usuario con su alcance (global o por proyecto);
  matriz rol-permiso guardada en lote, no un round-trip por fila.
- **Horarios**: tramos por día (soportar turnos partidos) y días festivos.
- **Ambientes**: alta por proyecto o globales.
- Pantallas bajo `/admin/*`, visibles solo con los permisos correspondientes.

Reglas que ya están definidas y hay que respetar:

- Permisos `ADM.Usuarios` y `ADM.Roles` según la operación; se validan en el caso de uso,
  nunca solo en el controlador.
- RN-ADM-01: un usuario no puede ser su propio jefe ni formar ciclos en la jerarquía
  (validar con CTE recursivo antes de guardar).
- RN-ADM-02: el rol Administrador otorga todos los permisos, pero **no** cortocircuita las
  reglas de negocio.
- Los proyectos con `EsMantenimiento = 1` activan reglas especiales ya implementadas en
  WorkItems; no las alteres.

**Matices críticos (no negociables):**

- Toda la API exige token (401 sin él). Si agregas un endpoint público necesita
  `[AllowAnonymous]` y una razón escrita.
- El frontend **nunca** decide transiciones de estatus: pide las acciones válidas al motor
  y envía acciones, jamás un estatus destino.
- El esquema lo gobiernan los scripts idempotentes de `DataBase/Scripts` con la
  nomenclatura y plantilla del estándar; **no uses migraciones de EF**. Si cambias el
  esquema, corre el script y vuelve a hacer scaffold.
- Toda alta debe fijar `Activo = true` explícitamente: el `DEFAULT` de la base no aplica
  en los INSERT de EF.
- No filtres ni ordenes sobre proyecciones intermedias complejas en EF (da error 500);
  une entidades, filtra por columnas reales y proyecta al final.
- Métodos en español; sin emojis ni caracteres decorativos en código, comentarios ni
  mensajes de commit.
- **Verifica de verdad antes de afirmar que algo funciona**: compila, corre
  `dotnet test` (hoy 34 pruebas en verde) y prueba el flujo por API o en el navegador.
  Si algo no se puede verificar en este entorno, dilo explícitamente.

Cómo levantar la base, la API y el SPA está en la sección 1 de `Doctos/PENDIENTES.md`.

**Al terminar**: actualiza `Doctos/PENDIENTES.md` con lo que quedó hecho y lo que falta,
y haz commit y push a `main`.

---

## Alternativas de objetivo

Si se prefiere otro frente, sustituir la sección **Objetivo** por uno de estos
(están descritos con detalle en `PENDIENTES.md`):

| Frente | Por qué elegirlo |
|---|---|
| **Comentarios y adjuntos** (A1) | Es lo que más se usa a diario en un gestor de tareas; las tablas y el contrato `IAlmacenArchivos` ya existen |
| **Edición de WorkItem en la UI** (A2) | Rápido: el endpoint con todas sus reglas ya existe, solo falta la pantalla |
| **Notificaciones + Hangfire** (A3, A4) | Cierra el ciclo con el solicitante y activa la vigilancia de SLA y los KPIs |
| **Flujo de Entra ID en el SPA** (B2) | Requiere tener a mano el tenant, client id y redirect URI reales |
| **Migración de datos del GT** (B3) | Necesario para el corte real; el mapeo está en el Documento Maestro §15.4 |
| **Integración Git** (resto de Fase 3) | Traza commits y PRs contra los WorkItems, tras la abstracción `IProveedorGit` |
