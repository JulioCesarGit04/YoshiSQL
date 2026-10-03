using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diseno;

/// <param name="NombreDeLaLlavePrimaria">Nombre actual de la restricción PRIMARY KEY, si la tabla ya existe y la tiene.</param>
public sealed record DefinicionDeTabla(
    string Esquema,
    string Nombre,
    IReadOnlyList<DefinicionDeColumna> Columnas,
    string? NombreDeLaLlavePrimaria = null)
{
    public Tabla Tabla => new(Esquema, Nombre);

    public IEnumerable<DefinicionDeColumna> ColumnasDeLaLlavePrimaria => Columnas.Where(columna => columna.EsLlavePrimaria);
}
