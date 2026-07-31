using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GTE.Api.Tests;

/// <summary>
/// E2E del modulo Administracion por HTTP contra una bdsGTE real (LocalDB): folio de
/// proyecto al autorizar, RN-PRY-01 (cierre bloqueado con WorkItems abiertos), RN-ADM-01
/// (ciclo de jerarquia rechazado), alta/baja de miembros de equipo y guardado en lote de
/// la matriz rol-permiso. Se omite si no hay LocalDB.
/// </summary>
public class AdministracionApiTests(WebApplicationFactory<Program> fabricaApp)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string CadenaLocal =
        @"Server=(localdb)\MSSQLLocalDB;Database=bdsGTE;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private sealed record Envelope<T>(string Code, bool Success, string UserMessage, T? Response);

    private static bool BaseDisponible() => FabricaApiAutenticada.BaseDisponible();

    private static FabricaContexto CrearFabricaDatos()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:bdsGTE"] = CadenaLocal
            })
            .Build();
        return new FabricaContexto(configuracion);
    }

    [Fact]
    public async Task Proyecto_AutorizarAsignaFolio_CerrarConWorkItemsAbiertosSeBloquea()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var clave = $"E2EADM{sufijo}";

        int idProyecto;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var proyecto = new TblProyecto
            {
                Clave = clave,
                Nombre = $"Proyecto Admin E2E {sufijo}",
                IdCategoriaProyecto = 1,
                IdEstatusProyecto = 1,   // Propuesto
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblProyecto.Add(proyecto);
            await contexto.SaveChangesAsync();
            idProyecto = proyecto.IdProyecto;

            contexto.TblHistorialEstatus.Add(new TblHistorialEstatus
            {
                Proceso = "Proyecto",
                IdRegistro = idProyecto,
                IdEstatus = 1,
                Accion = "ALTA",
                Usuario = "e2e"
            });
            await contexto.SaveChangesAsync();
        }

        var idWorkItem = 0;
        try
        {
            // AUTORIZAR asigna el folio de la serie PRY-anio
            var autorizado = await CambiarEstatusProyectoAsync(cliente, idProyecto, "AUTORIZAR");
            var folio = autorizado.GetProperty("folio").GetString();
            Assert.NotNull(folio);
            Assert.StartsWith($"PRY-{DateTime.Today.Year}-", folio);
            Assert.Equal("Autorizado", autorizado.GetProperty("estatus").GetString());

            // INICIAR: pasa a En Ejecucion
            await CambiarEstatusProyectoAsync(cliente, idProyecto, "INICIAR");

            // Un WorkItem abierto del proyecto bloquea el CERRAR (RN-PRY-01, 409)
            await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
            {
                var item = new TblWorkItem
                {
                    Folio = $"{clave}-0001",
                    IdTipoWorkItem = 3,
                    IdProyecto = idProyecto,
                    Titulo = "Item abierto E2E",
                    IdEstatusWorkItem = 1,   // Pendiente
                    IdPrioridad = 3,
                    UsuarioRegistro = "e2e",
                    Activo = true
                };
                contexto.TblWorkItem.Add(item);
                await contexto.SaveChangesAsync();
                idWorkItem = item.IdWorkItem;
            }

            var respuestaCierre = await cliente.PutAsJsonAsync(
                $"/api/v1/proyectos/{idProyecto}/estatus", new { accion = "CERRAR" });
            Assert.Equal(HttpStatusCode.Conflict, respuestaCierre.StatusCode);

            // Al terminar el item, CERRAR ya procede
            await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
            {
                var item = await contexto.TblWorkItem.FirstAsync(w => w.IdWorkItem == idWorkItem);
                item.IdEstatusWorkItem = 6;   // Terminado
                await contexto.SaveChangesAsync();
            }

            var cerrado = await CambiarEstatusProyectoAsync(cliente, idProyecto, "CERRAR");
            Assert.Equal("Cerrado", cerrado.GetProperty("estatus").GetString());
        }
        finally
        {
            await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
            if (idWorkItem > 0)
            {
                contexto.TblWorkItem.RemoveRange(contexto.TblWorkItem.Where(w => w.IdWorkItem == idWorkItem));
            }
            contexto.TblHistorialEstatus.RemoveRange(
                contexto.TblHistorialEstatus.Where(h => h.Proceso == "Proyecto" && h.IdRegistro == idProyecto));
            contexto.TblBitacora.RemoveRange(
                contexto.TblBitacora.Where(b => b.Entidad == "Proyecto" && b.IdEntidad == idProyecto));
            await contexto.SaveChangesAsync();
            contexto.TblProyecto.RemoveRange(contexto.TblProyecto.Where(p => p.IdProyecto == idProyecto));
            await contexto.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Usuario_CicloDeJerarquiaSeRechaza()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        int idJefe;
        int idSubordinado;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var jefe = new TblUsuario
            {
                Dominio = $"e2ejefe{sufijo}",
                Nombre = $"Jefe E2E {sufijo}",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(jefe);
            await contexto.SaveChangesAsync();
            idJefe = jefe.IdUsuario;

            var subordinado = new TblUsuario
            {
                Dominio = $"e2esub{sufijo}",
                Nombre = $"Subordinado E2E {sufijo}",
                IdJefe = idJefe,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(subordinado);
            await contexto.SaveChangesAsync();
            idSubordinado = subordinado.IdUsuario;
        }

        try
        {
            // Un usuario no puede ser su propio jefe
            var respuestaPropio = await cliente.PutAsJsonAsync($"/api/v1/usuarios/{idJefe}", new
            {
                nombre = "Jefe E2E",
                idJefe
            });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaPropio.StatusCode);

            // Asignar al subordinado como jefe de su propio jefe formaria un ciclo
            var respuestaCiclo = await cliente.PutAsJsonAsync($"/api/v1/usuarios/{idJefe}", new
            {
                nombre = "Jefe E2E",
                idJefe = idSubordinado
            });
            Assert.Equal(HttpStatusCode.BadRequest, respuestaCiclo.StatusCode);

            // Editar sin tocar la jerarquia si procede
            var respuestaValida = await cliente.PutAsJsonAsync($"/api/v1/usuarios/{idSubordinado}", new
            {
                nombre = "Subordinado E2E editado"
            });
            respuestaValida.EnsureSuccessStatusCode();
        }
        finally
        {
            await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
            contexto.TblBitacora.RemoveRange(contexto.TblBitacora.Where(b =>
                b.Entidad == "Usuario" && (b.IdEntidad == idJefe || b.IdEntidad == idSubordinado)));
            await contexto.SaveChangesAsync();
            contexto.TblUsuario.RemoveRange(
                contexto.TblUsuario.Where(u => u.IdUsuario == idSubordinado || u.IdUsuario == idJefe));
            await contexto.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Equipo_AgregarYRetirarMiembro()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        int idUsuario;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var usuario = new TblUsuario
            {
                Dominio = $"e2emiembro{sufijo}",
                Nombre = $"Miembro E2E {sufijo}",
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblUsuario.Add(usuario);
            await contexto.SaveChangesAsync();
            idUsuario = usuario.IdUsuario;
        }

        var idEquipo = 0;
        try
        {
            var respuestaEquipo = await cliente.PostAsJsonAsync("/api/v1/equipos", new
            {
                nombre = $"Equipo E2E {sufijo}"
            });
            respuestaEquipo.EnsureSuccessStatusCode();
            var equipoCreado = await respuestaEquipo.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            idEquipo = equipoCreado!.Response.GetProperty("idEquipo").GetInt32();

            var respuestaAgregar = await cliente.PostAsJsonAsync($"/api/v1/equipos/{idEquipo}/miembros", new
            {
                idUsuario,
                porcentajeDedicacion = 50
            });
            respuestaAgregar.EnsureSuccessStatusCode();
            var envelopeAgregar = await respuestaAgregar.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            var miembros = envelopeAgregar!.Response.GetProperty("miembros").EnumerateArray().ToList();
            Assert.Single(miembros);
            var idEquipoMiembro = miembros[0].GetProperty("idEquipoMiembro").GetInt32();

            var respuestaRetirar = await cliente.PutAsJsonAsync(
                $"/api/v1/equipos/{idEquipo}/miembros/{idEquipoMiembro}/retirar", new { });
            respuestaRetirar.EnsureSuccessStatusCode();
            var envelopeRetirar = await respuestaRetirar.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
            Assert.Empty(envelopeRetirar!.Response.GetProperty("miembros").EnumerateArray());
        }
        finally
        {
            await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
            if (idEquipo > 0)
            {
                contexto.TblEquipoMiembro.RemoveRange(
                    contexto.TblEquipoMiembro.Where(m => m.IdEquipo == idEquipo));
                contexto.TblBitacora.RemoveRange(
                    contexto.TblBitacora.Where(b => b.Entidad == "Equipo" && b.IdEntidad == idEquipo));
                await contexto.SaveChangesAsync();
                contexto.TblEquipo.RemoveRange(contexto.TblEquipo.Where(e => e.IdEquipo == idEquipo));
            }
            contexto.TblUsuario.RemoveRange(contexto.TblUsuario.Where(u => u.IdUsuario == idUsuario));
            await contexto.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Roles_GuardarMatrizPermisosEnLote_AgregaYQuitaEnUnaSolaLlamada()
    {
        if (!BaseDisponible())
        {
            return;
        }

        var cliente = await FabricaApiAutenticada.CrearClienteAsync(fabricaApp, "aviramontes");
        var fabricaDatos = CrearFabricaDatos();
        var sufijo = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        int idRol;
        int[] idsPermiso;
        await using (var contexto = fabricaDatos.ConectarContexto<DbContextGTE>())
        {
            var rol = new TblRol
            {
                Nombre = $"RolE2E{sufijo}",
                EsSistema = false,
                UsuarioRegistro = "e2e",
                Activo = true
            };
            contexto.TblRol.Add(rol);
            await contexto.SaveChangesAsync();
            idRol = rol.IdRol;

            idsPermiso = await contexto.TblPermiso.AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.IdPermiso)
                .Select(p => p.IdPermiso)
                .Take(3)
                .ToArrayAsync();
        }

        try
        {
            // Primera llamada: asigna los dos primeros permisos
            var primera = await GuardarMatrizAsync(cliente, idRol, [idsPermiso[0], idsPermiso[1]]);
            var asignadosPrimera = primera.GetProperty("permisos").EnumerateArray()
                .Where(p => p.GetProperty("asignado").GetBoolean())
                .Select(p => p.GetProperty("idPermiso").GetInt32())
                .OrderBy(x => x)
                .ToArray();
            Assert.Equal(new[] { idsPermiso[0], idsPermiso[1] }.OrderBy(x => x), asignadosPrimera);

            // Segunda llamada: quita el primero, mantiene el segundo, agrega el tercero
            var segunda = await GuardarMatrizAsync(cliente, idRol, [idsPermiso[1], idsPermiso[2]]);
            var asignadosSegunda = segunda.GetProperty("permisos").EnumerateArray()
                .Where(p => p.GetProperty("asignado").GetBoolean())
                .Select(p => p.GetProperty("idPermiso").GetInt32())
                .OrderBy(x => x)
                .ToArray();
            Assert.Equal(new[] { idsPermiso[1], idsPermiso[2] }.OrderBy(x => x), asignadosSegunda);
        }
        finally
        {
            await using var contexto = fabricaDatos.ConectarContexto<DbContextGTE>();
            contexto.TblRolPermiso.RemoveRange(contexto.TblRolPermiso.Where(rp => rp.IdRol == idRol));
            contexto.TblBitacora.RemoveRange(contexto.TblBitacora.Where(b => b.Entidad == "Rol" && b.IdEntidad == idRol));
            await contexto.SaveChangesAsync();
            contexto.TblRol.RemoveRange(contexto.TblRol.Where(r => r.IdRol == idRol));
            await contexto.SaveChangesAsync();
        }
    }

    private static async Task<JsonElement> CambiarEstatusProyectoAsync(HttpClient cliente, int idProyecto, string accion)
    {
        var respuesta = await cliente.PutAsJsonAsync($"/api/v1/proyectos/{idProyecto}/estatus", new { accion });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response;
    }

    private static async Task<JsonElement> GuardarMatrizAsync(HttpClient cliente, int idRol, int[] idsPermiso)
    {
        var respuesta = await cliente.PutAsJsonAsync($"/api/v1/roles/{idRol}/permisos", new { idsPermiso });
        respuesta.EnsureSuccessStatusCode();
        var envelope = await respuesta.Content.ReadFromJsonAsync<Envelope<JsonElement>>(OpcionesJson);
        Assert.NotNull(envelope);
        return envelope.Response;
    }
}
