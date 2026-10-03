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

    string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos);
}
