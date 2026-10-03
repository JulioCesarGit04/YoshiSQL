using YoshiSQL.Dominio.Autocompletado;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Lo que el editor necesita para pedir sugerencias, sin conocer servidores ni bases de datos.
/// </summary>
public interface IProveedorDeSugerencias
{
    Task<IReadOnlyList<Sugerencia>> ObtenerSugerenciasAsync(string textoCompleto, int posicionDelCursor);
}
