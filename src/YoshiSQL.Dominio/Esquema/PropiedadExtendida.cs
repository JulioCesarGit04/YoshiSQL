namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Propiedad extendida de un objeto o de una de sus columnas; la más usada es MS_Description,
/// donde se guarda la descripción que documenta tablas y columnas.
/// </summary>
/// <param name="TipoDelObjeto">Como lo pide sp_addextendedproperty: TABLE, VIEW, PROCEDURE o FUNCTION.</param>
/// <param name="Columna">Nulo si la propiedad es del objeto y no de una columna.</param>
public sealed record PropiedadExtendida(
    string Nombre,
    string Valor,
    string Esquema,
    string Objeto,
    string TipoDelObjeto,
    string? Columna)
{
    public const string TipoTabla = "TABLE";
    public const string TipoVista = "VIEW";
    public const string TipoProcedimiento = "PROCEDURE";
    public const string TipoFuncion = "FUNCTION";
}
