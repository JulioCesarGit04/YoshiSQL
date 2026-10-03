using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.EdicionDeFilas;

public sealed record DatosParaEditar(IReadOnlyList<Columna> Columnas, IReadOnlyList<object?[]> Filas)
{
    /// <summary>
    /// Sin llave primaria no hay forma segura de saber qué fila actualizar o borrar.
    /// </summary>
    public bool PuedeEditarse => Columnas.Any(columna => columna.EsLlavePrimaria);
}
