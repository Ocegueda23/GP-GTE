# GTE - Gestor Tecnologico Empresarial

Plataforma integral de gestion del departamento de desarrollo de software de Interflo.
Sucesora del Gestor de Proyectos (GT, WinForms). El diseno completo vive en
`Doctos/GTE-DocumentoMaestro.md` (fuente unica de decisiones de arquitectura).

**Repositorio definitivo: https://github.com/Ocegueda23/GP-GTE** (ADR-09). Es una
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

## Reglas del repositorio

Ver `CLAUDE.md`. Resumen: flujo Controller -> AppService -> Repository/QueryService ->
DbContext; metodos en espanol; todo cambio de estatus pasa por el motor de workflow;
el esquema de BD lo gobiernan los scripts de `DataBase/Scripts`, no migraciones EF.
