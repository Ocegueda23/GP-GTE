namespace GTE.Application.Interfaces;

/// <summary>
/// Cifrado/descifrado de valores de columnas del motor de catalogos genericos. Cada
/// columna usa su propio purpose (derivado del nombre de columna), asi que dos columnas
/// nunca comparten la misma clave derivada aunque compartan la clave maestra del key ring.
/// </summary>
public interface IServicioCifradoColumna
{
    string Proteger(string nombreColumna, string valorPlano);

    string Desproteger(string nombreColumna, string valorProtegido);
}
