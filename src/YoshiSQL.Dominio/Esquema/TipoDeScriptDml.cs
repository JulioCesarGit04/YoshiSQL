namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Tipo de plantilla de datos a generar para una tabla o vista (menú "Generar script como").
/// </summary>
public enum TipoDeScriptDml
{
    Seleccion,
    Insercion,
    Actualizacion,
    Eliminacion
}
