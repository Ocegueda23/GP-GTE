using GTE.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace GTE.Infrastructure.Services;

/// <summary>
/// Wrapper delgado sobre IDataProtectionProvider: el purpose = nombre de columna deriva
/// una clave distinta por columna a partir del mismo key ring maestro (persistido en
/// bdsGTE, ver RepositorioClavesProteccionSql), sin gestionar claves AES a mano.
/// </summary>
public class ServicioCifradoColumna(IDataProtectionProvider proveedor) : IServicioCifradoColumna
{
    private const string PrefijoProposito = "GTE.CatalogoGenerico.Columna.";

    public string Proteger(string nombreColumna, string valorPlano) =>
        proveedor.CreateProtector(PrefijoProposito + nombreColumna).Protect(valorPlano);

    public string Desproteger(string nombreColumna, string valorProtegido) =>
        proveedor.CreateProtector(PrefijoProposito + nombreColumna).Unprotect(valorProtegido);
}
