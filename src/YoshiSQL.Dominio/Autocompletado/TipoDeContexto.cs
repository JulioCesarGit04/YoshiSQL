namespace YoshiSQL.Dominio.Autocompletado;

public enum TipoDeContexto
{
    /// <summary>Dentro de un texto o comentario: no se sugiere nada.</summary>
    SinSugerencias,

    /// <summary>Cualquier posición de una instrucción.</summary>
    General,

    /// <summary>Después de FROM, JOIN, UPDATE, INTO...: se espera una tabla.</summary>
    NombreDeTabla,

    /// <summary>Después de "alias." o "esquema.": se esperan columnas o tablas de ese elemento.</summary>
    DespuesDePunto
}
