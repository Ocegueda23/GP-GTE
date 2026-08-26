using GTE.Domain.CatalogoGenerico;

namespace GTE.Domain.Interfaces;

/// <summary>
/// Contrato de ESCRITURA de las tablas de sistema del motor de catalogos genericos
/// (tblCatalogoGenerico/tblCatalogoGenericoColumna, tipadas por EF). Los datos del
/// catalogo administrado en si (tabla de negocio arbitraria) NO pasan por aqui: los
/// maneja IMotorCrudGenerico con SQL dinamico blindado.
/// </summary>
public interface ICatalogoGenericoRepository
{
    Task<int> CrearCatalogoAsync(CatalogoNuevo datos, CancellationToken cancellationToken = default);

    Task ActualizarConfigColumnasAsync(
        int idCatalogo, IReadOnlyList<ColumnaConfigEdicion> columnas, CancellationToken cancellationToken = default);

    /// <summary>Siembra dbo.tblPermiso/tblRolPermiso con las claves CAT.<![CDATA[<]]>CLAVE<![CDATA[>]]>.* del catalogo, asignadas al rol Administrador.</summary>
    Task SembrarPermisosAsync(string clave, string titulo, CancellationToken cancellationToken = default);

    /// <summary>Bitacora de un descifrado puntual (accion sensible, siempre auditada).</summary>
    Task RegistrarDescifradoAsync(
        string clave, string nombreColumna, CancellationToken cancellationToken = default);
}
