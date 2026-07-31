using FluentValidation;
using GTE.Application.Common;
using GTE.Application.Common.Behaviors;
using GTE.Infrastructure.Persistence;
using GTE.WebApi;
using GTE.WebApi.Middleware;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Modulo WorkItems
builder.Services.AddScoped<GTE.Domain.Interfaces.IWorkItemRepository, GTE.Infrastructure.Repositories.WorkItemRepository>();
builder.Services.AddScoped<GTE.Application.Interfaces.IWorkItemQueryService, GTE.Infrastructure.Services.WorkItemQueryService>();
builder.Services.AddScoped<GTE.Application.Catalogos.Queries.ICatalogosQueryService, GTE.Infrastructure.Services.CatalogosQueryService>();

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

// Autenticacion JWT (Entra ID via configuracion; los endpoints exigen [Authorize] por modulo)
var seccionJwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.Authority = seccionJwt["Authority"];
        opciones.Audience = seccionJwt["Audience"];
        opciones.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    });
builder.Services.AddAuthorization();

// CORS para el SPA
var origenesSpa = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(opciones => opciones.AddPolicy("Spa", politica => politica
    .WithOrigins(origenesSpa)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Spa");
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { estado = "ok", fecha = DateTime.UtcNow }));

app.Run();

// Expone la clase Program para las pruebas de integracion (WebApplicationFactory).
public partial class Program;
