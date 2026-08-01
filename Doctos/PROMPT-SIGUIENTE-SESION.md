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

**Estado importante al abrir este chat: hay trabajo terminado y verificado pero SIN
commitear.** En la sesión anterior se construyeron y verificaron de punta a punta dos
bloques completos:

- **Módulo de Administración (B1)**: proyectos, equipos+miembros, usuarios, roles
  (asignación + matriz en lote), horarios (tramos+festivos), ambientes. API completa +
  pantallas bajo `/admin` (6 pestañas).
- **Autenticación propia de GTE (B2, reemplaza el plan de Entra ID)**: el equipo decidió
  que GTE no depende de ningún proveedor externo. Login con usuario+contraseña (BCrypt),
  bloqueo temporal tras intentos fallidos, JWT propio + refresh token rotativo en cookie
  HttpOnly (con detección de reuso), cambio de contraseña propio y reset por
  administrador.

Ambos bloques están verificados (`dotnet test` en 45/45, prueba manual completa en
navegador incluyendo el flujo de login real, refresh silencioso y logout) y ya están
documentados en `Doctos/PENDIENTES.md`. `git status` en el repo va a mostrar todos los
archivos nuevos/modificados de estos dos bloques, todavía sin `git add`/`commit`/`push`.

**Primer paso de esta sesión, antes de cualquier otra cosa: revisar el diff (`git status`
y `git diff`) y, si todo se ve bien, hacer `git add` + commit + push a `main`.** No asumas
autorización de push sin confirmar primero con el usuario — pregunta explícitamente antes
de empujar a `main`. Excluir del commit `run-api-dev.cmd` (script local de conveniencia
para levantar la API con la cadena de conexión de LocalDB, no es parte del repo).

**Objetivo de esta sesión (una vez cerrado el commit): a decidir con el usuario** entre
las alternativas de abajo, o lo que él prefiera.

**Matices críticos (no negociables, aplican a cualquier frente que se elija):**

- Toda la API exige token (401 sin él). Si agregas un endpoint público necesita
  `[AllowAnonymous]` y una razón escrita.
- El frontend **nunca** decide transiciones de estatus: pide las acciones válidas al motor
  y envía acciones, jamás un estatus destino.
- El esquema lo gobiernan los scripts idempotentes de `DataBase/Scripts` con la
  nomenclatura y plantilla del estándar; **no uses migraciones de EF**. Si cambias el
  esquema, corre el script y aplica el delta a mano en el scaffold (correr el scaffolder
  completo sin filtro reescribe `DbContextGTE.cs` entero y lo deja solo con las tablas que
  filtres — ver la lección en `PENDIENTES.md` sección 5).
- Toda alta debe fijar `Activo = true` explícitamente: el `DEFAULT` de la base no aplica
  en los INSERT de EF.
- No filtres ni ordenes sobre proyecciones intermedias complejas en EF (da error 500);
  une entidades, filtra por columnas reales y proyecta al final.
- Métodos en español; sin emojis ni caracteres decorativos en código, comentarios ni
  mensajes de commit.
- GTE no usa Entra ID ni ningún proveedor de identidad externo (decisión firme,
  2026-08-01): la autenticación, los accesos y los roles se manejan enteramente dentro de
  `bdsGTE`.
- **Verifica de verdad antes de afirmar que algo funciona**: compila, corre
  `dotnet test` (hoy 45 pruebas en verde) y prueba el flujo por API o en el navegador.
  Si algo no se puede verificar en este entorno, dilo explícitamente.

Cómo levantar la base, la API y el SPA está en la sección 1 de `Doctos/PENDIENTES.md`
(incluye la nota nueva sobre `Jwt:ClaveFirma` y cómo probar el login real).

**Al terminar**: actualiza `Doctos/PENDIENTES.md` con lo que quedó hecho y lo que falta,
y haz commit y push a `main` (confirmando antes con el usuario).

---

## Alternativas de objetivo

Si se prefiere otro frente, sustituir la sección **Objetivo** por uno de estos
(están descritos con detalle en `PENDIENTES.md`):

| Frente | Por qué elegirlo |
|---|---|
| **Comentarios y adjuntos** (A1) | Es lo que más se usa a diario en un gestor de tareas; las tablas y el contrato `IAlmacenArchivos` ya existen |
| **Edición de WorkItem en la UI** (A2) | Rápido: el endpoint con todas sus reglas ya existe, solo falta la pantalla |
| **Notificaciones + Hangfire** (A3, A4) | Cierra el ciclo con el solicitante y activa la vigilancia de SLA y los KPIs |
| **Migración de datos del GT** (B3) | Necesario para el corte real; el mapeo está en el Documento Maestro §15.4 |
| **Despliegue** (B4) | Ya no bloquea en la parte de identidad (B2 resuelto); falta pipeline CI/CD, `appsettings.Production.json`, usuario de BD de mínimo privilegio, hosting (IIS/Kestrel/Docker) — ver el desglose completo que se armó para este frente en el historial de esta sesión |
| **Integración Git** (resto de Fase 3) | Traza commits y PRs contra los WorkItems, tras la abstracción `IProveedorGit` |
