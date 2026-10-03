using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Autocompletado;

/// <summary>
/// Tablas y vistas de una base de datos con sus columnas, guardadas en memoria para sugerir rápido.
/// </summary>
public sealed class CatalogoDeLaBaseDeDatos
{
    public static readonly CatalogoDeLaBaseDeDatos Vacio = new(new Dictionary<ObjetoDeEsquema, IReadOnlyList<Columna>>());

    public CatalogoDeLaBaseDeDatos(IReadOnlyDictionary<ObjetoDeEsquema, IReadOnlyList<Columna>> columnasPorObjeto)
    {
        ColumnasPorObjeto = columnasPorObjeto;
    }

    public IReadOnlyDictionary<ObjetoDeEsquema, IReadOnlyList<Columna>> ColumnasPorObjeto { get; }

    public IEnumerable<ObjetoDeEsquema> Objetos => ColumnasPorObjeto.Keys;

    public IEnumerable<string> Esquemas => Objetos.Select(objeto => objeto.Esquema).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Busca una tabla o vista por nombre; si no se indica esquema, se prefiere dbo.
    /// </summary>
    public ObjetoDeEsquema? Buscar(string? esquema, string nombre) =>
        Objetos
            .Where(objeto => string.Equals(objeto.Nombre, nombre, StringComparison.OrdinalIgnoreCase))
            .Where(objeto => esquema is null || string.Equals(objeto.Esquema, esquema, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(objeto => string.Equals(objeto.Esquema, EsquemaPredeterminado, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

    public IReadOnlyList<Columna> ObtenerColumnas(ObjetoDeEsquema objeto) =>
        ColumnasPorObjeto.TryGetValue(objeto, out var columnas) ? columnas : [];

    public const string EsquemaPredeterminado = "dbo";
}
