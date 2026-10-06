namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Datos de una base de datos para la ventana de propiedades.
/// </summary>
public sealed record PropiedadesDeBaseDeDatos(
    string Nombre,
    string Estado,
    string ModeloDeRecuperacion,
    string? Intercalacion,
    int NivelDeCompatibilidad,
    string? Propietario,
    DateTime Creacion,
    long TamanoKb);
