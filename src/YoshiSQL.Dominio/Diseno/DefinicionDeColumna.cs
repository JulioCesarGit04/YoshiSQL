using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diseno;

/// <summary>
/// Una columna tal como la deja el usuario en el diseñador de tablas.
/// </summary>
/// <param name="NombreOriginal">Nombre que tenía en la base de datos; nulo si es una columna nueva.</param>
public sealed record DefinicionDeColumna(
    string? NombreOriginal,
    string Nombre,
    TipoDeDato TipoDeDato,
    bool AdmiteNulos,
    bool EsLlavePrimaria,
    bool EsIdentidad)
{
    public bool EsNueva => NombreOriginal is null;

    public bool FueRenombrada => NombreOriginal is not null && !string.Equals(NombreOriginal, Nombre, StringComparison.Ordinal);

    public static DefinicionDeColumna DesdeColumnaExistente(Columna columna) =>
        new(columna.Nombre, columna.Nombre, columna.TipoDeDato, columna.AdmiteNulos, columna.EsLlavePrimaria, columna.EsIdentidad);
}
