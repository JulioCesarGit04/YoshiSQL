namespace YoshiSQL.Escritorio.Controles;

/// <param name="Texto">Código del fragmento, con saltos "\n" y la marca "$0" donde debe quedar el cursor.</param>
public sealed record FragmentoDeCodigoSql(string Abreviatura, string Descripcion, string Texto);
