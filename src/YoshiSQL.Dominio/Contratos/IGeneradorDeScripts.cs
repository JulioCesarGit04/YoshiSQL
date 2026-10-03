using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Genera código SQL listo para abrir en el editor (opciones del menú contextual).
/// </summary>
public interface IGeneradorDeScripts
{
    string GenerarSeleccionDeFilas(string baseDeDatos, ObjetoDeEsquema objeto, int cantidadDeFilas);

    string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos);

    string GenerarCreacionDeTabla(string baseDeDatos, Tabla tabla, IReadOnlyList<Columna> columnas);

    string GenerarEliminacion(string baseDeDatos, ObjetoDeEsquema objeto);

    /// <summary>
    /// Script para crear el objeto tal como existe hoy, a partir de su definición.
    /// </summary>
    string GenerarCreacionDesdeDefinicion(string baseDeDatos, string definicion);

    /// <summary>
    /// Script para modificar el objeto: su definición actual con CREATE cambiado por ALTER.
    /// </summary>
    string GenerarModificacionDesdeDefinicion(string baseDeDatos, string definicion);

    string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos);
}
