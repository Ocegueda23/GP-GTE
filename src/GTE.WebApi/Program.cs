using FluentValidation;
using GTE.Application.Common;
using GTE.Application.Common.Behaviors;
using GTE.Infrastructure.Persistence;
using GTE.WebApi;
using GTE.WebApi.Middleware;
using GTE.WebApi.Seguridad;
using MediatR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

// Modulo Administracion
builder.Services.AddScoped<GTE.Domain.Interfaces.IAdministracionRepository, GTE.Infrastructure.Repositories.AdministracionRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IAdministracionQueryService, GTE.Infrastructure.Services.AdministracionQueryService>();

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
builder.Services.AddScoped<GTE.Application.Interfaces.IServicioNotificaciones, GTE.Infrastructure.Services.ServicioNotificaciones>();
builder.Services.AddScoped<GTE.Application.Interfaces.INotificadorTiempoReal, GTE.WebApi.Hubs.NotificadorSignalR>();

// Modulo Autenticacion (propia de GTE, sin proveedor externo)
builder.Services.AddScoped<GTE.Domain.Interfaces.IAutenticacionRepository, GTE.Infrastructure.Repositories.AutenticacionRepository>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IHashPassword, GTE.Infrastructure.Services.HashPasswordBCrypt>();
builder.Services.AddSingleton<GTE.Application.Interfaces.IEmisorTokenSesion, GTE.Infrastructure.Services.EmisorTokenSesion>();

// Autenticacion: JWT propio de GTE (sin proveedor externo).
// Arranca con FallbackPolicy que exige identidad en toda la API.
builder.Services.AgregarAutenticacionGte(builder.Configuration, builder.Environment);

// CORS para el SPA (AllowCredentials: el refresh token viaja en una cookie HttpOnly)
var origenesSpa = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(opciones => opciones.AddPolicy("Spa", politica => politica
    .WithOrigins(origenesSpa)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

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
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("Spa");
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<GTE.WebApi.Hubs.NotificacionesHub>("/hubs/notificaciones");
app.MapGet("/health", () => Results.Ok(new { estado = "ok", fecha = DateTime.UtcNow }))
    .AllowAnonymous();

// Rutas de cliente de la SPA sin archivo fisico (ej. "/proyectos/123"): no coinciden con
// ningun controlador ni con un archivo real, asi que si matchean el fallback (":nonfile").
// AllowAnonymous porque el shell tiene que poder cargar sin sesion -- es lo que muestra la
// pantalla de login. Los datos reales siguen exigiendo token en /api/v1/...
app.MapFallbackToFile("index.html").AllowAnonymous();

app.Run();

// Expone la clase Program para las pruebas de integracion (WebApplicationFactory).
public partial class Program;
