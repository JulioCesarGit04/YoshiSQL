using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Lo que el editor necesita para pedir sugerencias, sin conocer servidores ni bases de datos.
/// </summary>
public interface IProveedorDeSugerencias
{
    Task<IReadOnlyList<Sugerencia>> ObtenerSugerenciasAsync(string textoCompleto, int posicionDelCursor);

    /// <summary>Errores de sintaxis del texto, para subrayarlos. Se llama fuera del hilo de la interfaz.</summary>
    IReadOnlyList<ErrorDeSintaxis> BuscarErroresDeSintaxis(string textoCompleto);
}
