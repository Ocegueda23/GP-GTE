using FluentValidation;
using GTE.Application.Common;
using GTE.Application.Common.Behaviors;
using GTE.Infrastructure.Persistence;
using GTE.Infrastructure.Services;
using GTE.WebApi;
using GTE.WebApi.Middleware;
using GTE.WebApi.Seguridad;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Overrides locales POR MAQUINA (la cadena de conexion del SQL Server de cada quien), en
// un archivo gitignored. Ya estaba documentado en CLAUDE.md y listado en .gitignore, pero
// nunca se cargaba: "Local" no es un ASPNETCORE_ENVIRONMENT, asi que el
// appsettings.{Environment}.json por convencion no lo tomaba. Va al final para ganarle a
// appsettings.json y appsettings.{Environment}.json, y sigue por debajo de las variables
// de entorno y los argumentos de linea de comandos.
//
// SOLO en Development, a proposito. El 2026-08-23 un appsettings.Local.json de desarrollo
// se colo en el paquete de publicacion y, al tener la precedencia mas alta, repunto el
// servicio de Windows a una instancia de SQL donde LocalSystem no tiene acceso: todas las
// llamadas a BD tronaron y el login empezo a responder 500. La primera defensa es
// CopyToPublishDirectory="Never" en el .csproj (no viaja en el publish); esta es la
// segunda, para que aunque alguien lo copie a mano a un servidor jamas pise la config de
// produccion.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

// Integracion con el Service Control Manager de Windows: sin esto, el .exe publicado
// corre como una consola normal y "sc start" truena con error 1053 (nunca le avisa a
// Windows que ya quedo en estado RUNNING). No-op si no se esta corriendo como servicio
// (dotnet run local, WebApplicationFactory de las pruebas), asi que es seguro dejarlo
// siempre activo.
builder.Host.UseWindowsService();

// Logging estructurado
builder.Host.UseSerilog((contexto, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/gte-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// MVC + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// MediatR + FluentValidation + AutoMapper
var ensambladoApplication = typeof(AuditContext).Assembly;
builder.Services.AddMediatR(configuracion => configuracion.RegisterServicesFromAssembly(ensambladoApplication));
builder.Services.AddValidatorsFromAssembly(ensambladoApplication);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ComportamientoValidacion<,>));
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

// Transversales
builder.Services.AddScoped<AuditContext>();
builder.Services.AddSingleton<FabricaContexto>();

// Data Protection: key ring persistido en bdsGTE (ADR-03) en vez del filesystem por
// defecto, usado por el cifrado de columnas del motor de catalogos genericos.
builder.Services.AddDataProtection().SetApplicationName("GTE");
builder.Services.AddOptions<KeyManagementOptions>()
    .Configure<FabricaContexto>((opciones, fabrica) =>
    {
        opciones.XmlRepository = new RepositorioClavesProteccionSql(fabrica);
    });

builder.Services.AddScoped<GTE.Application.Interfaces.IMotorWorkflow, GTE.Infrastructure.Services.MotorWorkflow>();
builder.Services.AddScoped<GTE.Application.Interfaces.ICalendarioLaboral, GTE.Infrastructure.Services.CalendarioLaboral>();
builder.Services.AddScoped<GTE.Application.Interfaces.IGeneradorFolios, GTE.Infrastructure.Services.GeneradorFolios>();
builder.Services.AddScoped<GTE.Application.Interfaces.IVerificadorPermisos, GTE.Infrastructure.Services.VerificadorPermisos>();
builder.Services.AddScoped<GTE.Application.Interfaces.IProveedorUsuarioActual, GTE.Infrastructure.Services.ProveedorUsuarioActual>();
builder.Services.AddScoped<GTE.Application.Interfaces.IAprovisionadorUsuarios, GTE.Infrastructure.Services.AprovisionadorUsuarios>();
builder.Services.AddScoped<GTE.Application.Interfaces.ISesionQueryService, GTE.Infrastructure.Services.SesionQueryService>();

// Modulo WorkItems
builder.Services.AddScoped<GTE.Domain.Interfaces.IWorkItemRepository, GTE.Infrastructure.Repositories.WorkItemRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IWorkItemQueryService, GTE.Infrastructure.Services.WorkItemQueryService>();
builder.Services.AddScoped<GTE.Application.Catalogos.Queries.ICatalogosQueryService, GTE.Infrastructure.Services.CatalogosQueryService>();

// Modulo Calidad (QA)
builder.Services.AddScoped<GTE.Domain.Interfaces.ICalidadRepository, GTE.Infrastructure.Repositories.CalidadRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.ICalidadQueryService, GTE.Infrastructure.Services.CalidadQueryService>();

// Modulo Entregas (releases)
builder.Services.AddScoped<GTE.Domain.Interfaces.IEntregaRepository, GTE.Infrastructure.Repositories.EntregaRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IEntregaQueryService, GTE.Infrastructure.Services.EntregaQueryService>();

// Modulo Planeacion
builder.Services.AddScoped<GTE.Domain.Interfaces.IPlaneacionRepository, GTE.Infrastructure.Repositories.PlaneacionRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IPlaneacionQueryService, GTE.Infrastructure.Services.PlaneacionQueryService>();

// Modulo Revisiones
builder.Services.AddScoped<GTE.Domain.Interfaces.IRevisionRepository, GTE.Infrastructure.Repositories.RevisionRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IRevisionQueryService, GTE.Infrastructure.Services.RevisionQueryService>();

// Modulo Mi Dia
builder.Services.AddScoped<GTE.Application.Interfaces.IMiDiaQueryService, GTE.Infrastructure.Services.MiDiaQueryService>();

// Modulo Solicitudes
builder.Services.AddScoped<GTE.Domain.Interfaces.ISolicitudRepository, GTE.Infrastructure.Repositories.SolicitudRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.ISolicitudQueryService, GTE.Infrastructure.Services.SolicitudQueryService>();

// Modulo Tickets (Mesa de ayuda y SLA)
builder.Services.AddScoped<GTE.Domain.Interfaces.ITicketRepository, GTE.Infrastructure.Repositories.TicketRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.ITicketQueryService, GTE.Infrastructure.Services.TicketQueryService>();

// Modulo Incidentes (Operacion)
builder.Services.AddScoped<GTE.Domain.Interfaces.IIncidenteRepository, GTE.Infrastructure.Repositories.IncidenteRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IIncidenteQueryService, GTE.Infrastructure.Services.IncidenteQueryService>();

// Modulo Base de conocimiento (P23; incluye el consumo anonimo /api/v1/publico/conocimiento)
builder.Services.AddScoped<GTE.Domain.Interfaces.IConocimientoRepository, GTE.Infrastructure.Repositories.ConocimientoRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IConocimientoQueryService, GTE.Infrastructure.Services.ConocimientoQueryService>();
builder.Services.AddScoped<GTE.Domain.Interfaces.IReglasNegocioRepository, GTE.Infrastructure.Repositories.ReglasNegocioRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IReglasNegocioQueryService, GTE.Infrastructure.Services.ReglasNegocioQueryService>();

// Modulo Portafolio (Costeo + OKR)
builder.Services.AddScoped<GTE.Domain.Interfaces.ICosteoRepository, GTE.Infrastructure.Repositories.CosteoRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.ICosteoQueryService, GTE.Infrastructure.Services.CosteoQueryService>();
builder.Services.AddScoped<GTE.Domain.Interfaces.IOkrRepository, GTE.Infrastructure.Repositories.OkrRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IOkrQueryService, GTE.Infrastructure.Services.OkrQueryService>();

// Modulo Administracion
builder.Services.AddScoped<GTE.Domain.Interfaces.IAdministracionRepository, GTE.Infrastructure.Repositories.AdministracionRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IAdministracionQueryService, GTE.Infrastructure.Services.AdministracionQueryService>();

// Motor de catalogos genericos (CRUD administrativo sobre tablas simples de bdsGTE)
builder.Services.AddScoped<GTE.Domain.Interfaces.ICatalogoGenericoRepository, GTE.Infrastructure.Repositories.CatalogoGenericoRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.ICatalogoGenericoQueryService, GTE.Infrastructure.Services.CatalogoGenericoQueryService>();
builder.Services.AddScoped<GTE.Application.Interfaces.IMotorMetadatosEsquema, GTE.Infrastructure.Services.MotorMetadatosEsquema>();
builder.Services.AddScoped<GTE.Application.Interfaces.IMotorCrudGenerico, GTE.Infrastructure.Services.MotorCrudGenerico>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IServicioCifradoColumna, GTE.Infrastructure.Services.ServicioCifradoColumna>();

// Modulo Dashboard Ejecutivo de colaborador individual (solo lectura, sin repositorio de escritura)
builder.Services.AddScoped<GTE.Application.Interfaces.IDashboardQueryService, GTE.Infrastructure.Services.DashboardQueryService>();

// Modulo Dashboard Ejecutivo P18 (equipo/proyecto: DORA, costo, OKR)
builder.Services.AddScoped<GTE.Domain.Interfaces.IIndicadoresEjecutivosRepository, GTE.Infrastructure.Repositories.IndicadoresEjecutivosRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IIndicadoresEjecutivosQueryService, GTE.Infrastructure.Services.IndicadoresEjecutivosQueryService>();
builder.Services.AddScoped<SnapshotKpiJob>();
builder.Services.AddScoped<PurgaArchivosBorradorJob>();

// Modulo Workflow (P21, editor de transiciones)
builder.Services.AddScoped<GTE.Domain.Interfaces.IWorkflowRepository, GTE.Infrastructure.Repositories.WorkflowRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IWorkflowQueryService, GTE.Infrastructure.Services.WorkflowQueryService>();

// Modulo Comentarios y Archivos (adjuntos)
builder.Services.AddScoped<GTE.Domain.Interfaces.IComentarioRepository, GTE.Infrastructure.Repositories.ComentarioRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IComentarioQueryService, GTE.Infrastructure.Services.ComentarioQueryService>();
builder.Services.AddScoped<GTE.Domain.Interfaces.IArchivoRepository, GTE.Infrastructure.Repositories.ArchivoRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IArchivoQueryService, GTE.Infrastructure.Services.ArchivoQueryService>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IAlmacenArchivos, GTE.Infrastructure.Services.AlmacenArchivosDisco>();
builder.Services.AddSingleton<GTE.Application.Interfaces.ISanitizadorHtml, GTE.Infrastructure.Services.SanitizadorHtmlGanss>();

// Modulo Notificaciones (InApp + SignalR)
builder.Services.AddScoped<GTE.Domain.Interfaces.INotificacionRepository, GTE.Infrastructure.Repositories.NotificacionRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.INotificacionQueryService, GTE.Infrastructure.Services.NotificacionQueryService>();

// Modulo Reportes (solo lectura, sin repositorio de escritura)
builder.Services.AddScoped<GTE.Application.Interfaces.IReportesQueryService, GTE.Infrastructure.Services.ReportesQueryService>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IExportadorExcel, GTE.Infrastructure.Services.ExportadorExcelClosedXml>();
builder.Services.AddScoped<GTE.Application.Interfaces.IServicioNotificaciones, GTE.Infrastructure.Services.ServicioNotificaciones>();
builder.Services.AddScoped<GTE.Application.Interfaces.INotificadorTiempoReal, GTE.WebApi.Hubs.NotificadorSignalR>();
// Canal Correo: sin credenciales SMTP en este entorno, Smtp:Habilitado queda en false por
// default (appsettings) y el canal no envia nada -- ver CanalCorreoSmtp.
builder.Services.Configure<GTE.Infrastructure.Services.OpcionesSmtp>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<GTE.Application.Interfaces.ICanalNotificacion, GTE.Infrastructure.Services.CanalCorreoSmtp>();

// Modulo Autenticacion (propia de GTE, sin proveedor externo)
builder.Services.AddScoped<GTE.Domain.Interfaces.IAutenticacionRepository, GTE.Infrastructure.Repositories.AutenticacionRepository>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IHashPassword, GTE.Infrastructure.Services.HashPasswordBCrypt>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IEmisorTokenSesion, GTE.Infrastructure.Services.EmisorTokenSesion>();

// Autenticacion: JWT propio de GTE (sin proveedor externo).
// Arranca con FallbackPolicy que exige identidad en toda la API.
builder.Services.AgregarAutenticacionGte(builder.Configuration, builder.Environment);

// Limitacion de tasa para las rutas ANONIMAS de la base de conocimiento
// (/api/v1/publico/conocimiento). El resto de la API no la necesita: exige token y no esta
// expuesta a internet. Particionado por IP de origen -- un visitante anonimo no tiene
// identidad con la que particionar. Ventana fija por simplicidad; si el trafico real lo
// pide se cambia a token bucket sin tocar los controladores (la politica es un nombre).
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opciones.AddPolicy(GTE.WebApi.Seguridad.LimitadoresTasa.Publico, contexto =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// CORS para el SPA (AllowCredentials: el refresh token viaja en una cookie HttpOnly)
var origenesSpa = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(opciones => opciones.AddPolicy("Spa", politica => politica
    .WithOrigins(origenesSpa)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Hangfire (A4 del roadmap): storage propio en bdsGTE (schema [HangFire], la libreria lo
// crea/migra sola). Sin dashboard web expuesto en esta primera pasada (el .UseHangfireDashboard
// de ASP.NET Core no entiende el JWT propio de GTE; exponerlo exigiria un filtro de
// autorizacion dedicado, fuera de alcance de este bloque).
// Deshabilitado explicitamente en pruebas de integracion (Hangfire:Deshabilitado=true en
// FabricaApiAutenticada): cada prueba levanta su propio WebApplicationFactory, y
// reinstalar/consultar el storage SQL de Hangfire en cada una satura LocalDB (mismo tipo de
// congestion ya documentado en Doctos/PENDIENTES.md) y ademas RecurringJob (API estatica)
// no reinicializa JobStorage.Current entre hosts sucesivos del mismo proceso de pruebas.
var hangfireDeshabilitado = builder.Configuration.GetValue<bool>("Hangfire:Deshabilitado");
if (!hangfireDeshabilitado)
{
    var cadenaHangfire = builder.Configuration.GetConnectionString("bdsGTE")
        ?? throw new InvalidOperationException("Falta ConnectionStrings:bdsGTE para Hangfire.");
    builder.Services.AddHangfire(configuracion => configuracion
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(cadenaHangfire));
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

if (!hangfireDeshabilitado)
{
    // Snapshot nocturno de KPIs personalizados (Dashboard Ejecutivo P18): 01:00 hora del servidor.
    // IRecurringJobManager (service-based API) en vez de RecurringJob (estatica): la estatica
    // depende de JobStorage.Current, fragil cuando el proceso hospeda mas de un host (pruebas).
    // Envuelto en try/catch: si bdsGTE no esta disponible en el arranque (ej. WebApplicationFactory
    // sin base real, ver VersionEndpointTests), el resto de la API debe poder arrancar igual --
    // el registro del job se reintenta solo en el siguiente arranque.
    try
    {
        app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<SnapshotKpiJob>(
            "snapshot-kpi-diario", job => job.EjecutarAsync(CancellationToken.None), Cron.Daily(1));

        // Recoge las imagenes que se pegaron en un alta que despues se cancelo: sin vinculo
        // ya no pueden adjuntarse a nada. 02:00, despues del snapshot.
        app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<PurgaArchivosBorradorJob>(
            "purga-archivos-borrador", job => job.EjecutarAsync(CancellationToken.None), Cron.Daily(2));
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "No se pudo registrar el job recurrente snapshot-kpi-diario en el arranque.");
    }
}

// Chequeo de almacen en el arranque: una ruta de AlmacenArchivos:Ruta que apunte a una
// unidad o share inexistente en el servidor rompe TODA subida de archivos, y antes solo se
// notaba al primer intento del usuario y como INTERNAL_ERROR sin pistas. No se aborta el
// arranque a proposito: un share caido puede volver, y el resto de la API sirve igual.
var sondaAlmacen = app.Services.GetRequiredService<GTE.Application.Interfaces.IAlmacenArchivos>().Verificar();
if (sondaAlmacen.SePuedeEscribir)
{
    Log.Information("Almacen de archivos listo en {Raiz}", sondaAlmacen.Raiz);
}
else
{
    Log.Error(
        "ALMACEN DE ARCHIVOS NO DISPONIBLE en {Raiz} (existe: {Existe}): {Error}. "
        + "Ninguna subida de archivos va a funcionar hasta que se corrija AlmacenArchivos:Ruta.",
        sondaAlmacen.Raiz, sondaAlmacen.Existe, sondaAlmacen.Error);
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// SPA: en produccion, Kestrel sirve el build de React (frontend/gte-web/dist copiado a
// wwwroot al publicar) en el mismo proceso que la API -- sin IIS ni reverse proxy separado
// (topologia minima fase 1, ver Documento Maestro seccion 1.1). Si wwwroot no existe (por
// ejemplo, en Development, donde el SPA corre aparte con "npm run dev") esto no falla: solo
// no hay archivos estaticos que servir y la peticion sigue de largo por el resto del pipeline.
// Va ANTES de CORS/auth a proposito: un archivo real (JS/CSS/index.html) tiene que servirse
// sin pasar por el FallbackPolicy. MapFallbackToFile usa la restriccion implicita ":nonfile",
// asi que una ruta como "/assets/app.js" NUNCA la matchea (queda sin endpoint) y el
// FallbackPolicy (RequireAuthenticatedUser) la bloquearia con 401 si UseStaticFiles no la
// hubiera servido ya aqui arriba -- confirmado en pruebas manuales.
// index.html NUNCA se cachea; los assets con hash en el nombre se cachean para siempre.
//
// Por que existe esto (incidente del 2026-08-24, no quitarlo): index.html se servia sin
// Cache-Control, solo con ETag/Last-Modified. Tras un despliegue, un navegador que tenia
// cacheado el index.html viejo seguia pidiendo el bundle viejo -- que sigue existiendo en
// wwwroot si el despliegue no lo borro -- y cargaba la version ANTERIOR de la SPA sin
// fallar en nada: modulo nuevo invisible, cero errores en consola, imposible de
// diagnosticar desde la UI. Los nombres de los assets llevan hash de contenido, asi que
// el unico archivo que debe revalidarse en cada carga es index.html.
static void ConfigurarCacheSpa(StaticFileResponseContext contexto)
{
    var esHtml = contexto.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    contexto.Context.Response.Headers.CacheControl = esHtml
        ? "no-cache, no-store, must-revalidate"
        : "public, max-age=31536000, immutable";
}

var opcionesArchivosSpa = new StaticFileOptions { OnPrepareResponse = ConfigurarCacheSpa };

app.UseDefaultFiles();
app.UseStaticFiles(opcionesArchivosSpa);

app.UseCors("Spa");
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

// Despues de UseAuthorization: solo las rutas que declaran [EnableRateLimiting] pasan por
// un limitador (hoy, las anonimas de la base de conocimiento publica).
app.UseRateLimiter();

app.MapControllers();
app.MapHub<GTE.WebApi.Hubs.NotificacionesHub>("/hubs/notificaciones");
app.MapGet("/health", () => Results.Ok(new { estado = "ok", fecha = DateTime.UtcNow }))
    .AllowAnonymous();

// Rutas de cliente de la SPA sin archivo fisico (ej. "/proyectos/123"): no coinciden con
// ningun controlador ni con un archivo real, asi que si matchean el fallback (":nonfile").
// AllowAnonymous porque el shell tiene que poder cargar sin sesion -- es lo que muestra la
// pantalla de login. Los datos reales siguen exigiendo token en /api/v1/...
// Con las mismas opciones de cache: este es el camino que sirve index.html en los deep
// links (/reglas-negocio, /wi/123...), y si no se le pasan, esas rutas volverian a
// entregar el index.html cacheable y reaparece el problema por la puerta de atras.
app.MapFallbackToFile("index.html", opcionesArchivosSpa).AllowAnonymous();

app.Run();

// Expone la clase Program para las pruebas de integracion (WebApplicationFactory).
public partial class Program;
