# GTE - Gestor Tecnologico Empresarial

Plataforma integral de gestion del departamento de desarrollo de software de Interflo.
Sucesora del Gestor de Proyectos (GT, WinForms). El diseno completo vive en
`Doctos/GTE-DocumentoMaestro.md` (fuente unica de decisiones de arquitectura).

**Repositorio definitivo: https://github.com/Ocegueda23/GP-GTE** (ADR-09).
Estado y pendientes al dia: `Doctos/PENDIENTES.md`. Es una
excepcion deliberada al estandar del ecosistema Interflo, que usa Gitea self-hosted:
aplica solo a este proyecto.

## Estructura

```
GTE.sln
src/
  GTE.Domain/            entidades, excepciones y logica de negocio pura (sin EF)
  GTE.Application/       casos de uso (MediatR), DTOs, validadores, contratos
  GTE.Infrastructure/    EF Core (un DbContext por base), repositorios, integraciones
  GTE.WebApi/            controllers, middleware, ApiResponse, Program
tests/
  GTE.Domain.Tests/      reglas de negocio
  GTE.Application.Tests/ handlers
  GTE.Api.Tests/         integracion
frontend/gte-web/        SPA React + TypeScript (Vite)
DataBase/Scripts/        scripts SQL de despliegue (idempotentes, versionados)
Doctos/                  documento maestro de diseno
```

## Requisitos

- .NET SDK 9.x
- Node 20+
- SQL Server (una sola base: bdsGTE - el sistema es totalmente independiente)

## Ejecutar en desarrollo

```
dotnet run --project src/GTE.WebApi     # API en https://localhost:puerto/swagger
cd frontend/gte-web && npm run dev      # SPA en http://localhost:5173
```

La cadena de conexion local se configura en `appsettings.Development.json`
(o `appsettings.Local.json`, ignorado por git). Nunca versionar credenciales.

## Re-scaffold del modelo EF (tras cada cambio de esquema)

El esquema lo gobiernan los scripts de `DataBase/Scripts` (no migraciones EF). Despues de
correr una tanda nueva contra la BD de desarrollo, regenerar el modelo:

```
dotnet tool run dotnet-ef -- dbcontext scaffold "Server=<servidor>;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True" Microsoft.EntityFrameworkCore.SqlServer --project src/GTE.Infrastructure --output-dir Modelos/bdsGTE --context-dir Persistence --context DbContextGTE --namespace GTE.Infrastructure.Modelos.bdsGTE --context-namespace GTE.Infrastructure.Persistence --no-onconfiguring --no-pluralize --force
```

Revisar despues columnas computadas y tipos (leccion 7.8 del estandar). Para un entorno
local rapido: crear bdsGTE en `(localdb)\MSSQLLocalDB` y correr los scripts 01-09.
Las pruebas de integracion (GTE.Api.Tests) usan esa LocalDB si existe; si no, se omiten.

## Autenticacion

Toda la API exige identidad: sin token responde 401. Solo quedan abiertos `/health`,
`/api/v1/version` y `/api/v1/auth/configuracion`. Los permisos finos se evaluan por caso
de uso contra el RBAC de la base (403 si falta el permiso).

### Produccion: Entra ID

Llenar en `appsettings.json` (o variables de entorno):

```
Jwt:Authority = https://login.microsoftonline.com/<TENANT_ID>/v2.0
Jwt:Audience  = <APPLICATION_ID_URI o client id de la app registrada>
```

**La API no arranca si en produccion no hay `Jwt:Authority` configurado**: es deliberado,
para que nunca quede abierta por un despliegue incompleto. Al primer inicio de sesion de
una identidad valida, su usuario se crea solo (aprovisionamiento JIT) y nace SIN roles:
no puede operar hasta que administracion se los asigne.

Pendiente de este frente: el flujo de redireccion del SPA hacia Entra (Authorization Code
con PKCE) y la suplantacion auditada (permiso `ADM.Suplantar`).

### Desarrollo sin tenant

Con `Jwt:Desarrollo:Habilitado = true` (ya activo en `appsettings.Development.json`) la API
emite tokens firmados localmente en `POST /api/v1/auth/desarrollo/token` con el cuerpo
`{ "dominio": "aviramontes" }`. Ese endpoint responde 404 fuera del ambiente Development,
y la clave de firma se genera aleatoria en cada arranque si no se configura una.

## Reglas del repositorio

Ver `CLAUDE.md`. Resumen: flujo Controller -> AppService -> Repository/QueryService ->
DbContext; metodos en espanol; todo cambio de estatus pasa por el motor de workflow;
el esquema de BD lo gobiernan los scripts de `DataBase/Scripts`, no migraciones EF.
