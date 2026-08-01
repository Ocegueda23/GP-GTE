using GTE.Application.Common;
using GTE.Domain.Autenticacion;
using GTE.Domain.Interfaces;
using GTE.Infrastructure.Modelos.bdsGTE;
using GTE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GTE.Infrastructure.Repositories;

public class AutenticacionRepository(FabricaContexto fabrica, AuditContext auditoria)
    : RepositoryBase(fabrica, auditoria), IAutenticacionRepository
{
    public async Task<CredencialesUsuario?> ObtenerCredencialesAsync(
        string dominio, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblUsuario.AsNoTracking()
            .Where(u => u.Dominio == dominio)
            .Select(u => new CredencialesUsuario(
                u.IdUsuario, u.Dominio, u.Nombre, u.PasswordHash,
                u.RequiereCambioPassword ?? true, u.IntentosFallidos ?? 0, u.BloqueadoHasta, u.Activo))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RegistrarIntentoFallidoAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {idUsuario} no existe.");

        entidad.IntentosFallidos = (entidad.IntentosFallidos ?? 0) + 1;
        if (entidad.IntentosFallidos >= ConstantesAutenticacion.IntentosMaximos)
        {
            entidad.BloqueadoHasta = DateTime.Now.AddMinutes(ConstantesAutenticacion.MinutosBloqueo);
        }
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", idUsuario, "LOGIN_FALLIDO",
            $"Intento {entidad.IntentosFallidos}/{ConstantesAutenticacion.IntentosMaximos}", cancellationToken);
    }

    public async Task ResetearIntentosAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {idUsuario} no existe.");

        entidad.IntentosFallidos = 0;
        entidad.BloqueadoHasta = null;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task EstablecerPasswordAsync(
        int idUsuario, string passwordHash, bool requiereCambio, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblUsuario
            .FirstOrDefaultAsync(u => u.IdUsuario == idUsuario, cancellationToken)
            ?? throw new InvalidOperationException($"Usuario {idUsuario} no existe.");

        entidad.PasswordHash = passwordHash;
        entidad.RequiereCambioPassword = requiereCambio;
        entidad.FechaUltimoCambioPassword = DateTime.Now;
        entidad.IntentosFallidos = 0;
        entidad.BloqueadoHasta = null;
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", idUsuario, "ESTABLECER_PASSWORD", null, cancellationToken);
    }

    public async Task<int> GuardarRefreshTokenAsync(
        RefreshTokenNuevo datos, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = new TblRefreshToken
        {
            IdUsuario = datos.IdUsuario,
            TokenHash = datos.TokenHash,
            FechaExpiracion = datos.FechaExpiracion,
            IpOrigen = datos.IpOrigen,
            UsuarioRegistro = Auditoria.TieneIdentidad ? Auditoria.Usuario : "auth"
        };
        contexto.TblRefreshToken.Add(entidad);
        await contexto.SaveChangesAsync(cancellationToken);
        return entidad.IdRefreshToken;
    }

    public async Task<RefreshTokenValido?> ObtenerRefreshTokenAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        return await contexto.TblRefreshToken.AsNoTracking()
            .Where(t => t.TokenHash == tokenHash)
            .Select(t => new RefreshTokenValido(
                t.IdRefreshToken, t.IdUsuario, t.FechaExpiracion, t.FechaRevocado != null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task RevocarRefreshTokenAsync(int idRefreshToken, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var entidad = await contexto.TblRefreshToken
            .FirstOrDefaultAsync(t => t.IdRefreshToken == idRefreshToken, cancellationToken)
            ?? throw new InvalidOperationException($"Refresh token {idRefreshToken} no existe.");

        entidad.FechaRevocado = DateTime.Now;
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task RevocarTodosLosRefreshTokensAsync(int idUsuario, CancellationToken cancellationToken = default)
    {
        await using var contexto = Fabrica.ConectarContexto<DbContextGTE>();
        var vigentes = await contexto.TblRefreshToken
            .Where(t => t.IdUsuario == idUsuario && t.FechaRevocado == null)
            .ToListAsync(cancellationToken);

        foreach (var token in vigentes)
        {
            token.FechaRevocado = DateTime.Now;
        }
        await contexto.SaveChangesAsync(cancellationToken);

        await RegistrarBitacoraAsync("Usuario", idUsuario, "REVOCAR_TODAS_SESIONES",
            $"{vigentes.Count} refresh token(s)", cancellationToken);
    }
}
