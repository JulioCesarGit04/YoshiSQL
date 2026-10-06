namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Datos de una tabla para la ventana de propiedades: fechas, filas, espacio en disco y conteos.
/// </summary>
public sealed record PropiedadesDeTabla(
    DateTime Creacion,
    DateTime Modificacion,
    long Filas,
    long EspacioTotalKb,
    long EspacioUsadoKb,
    int Columnas,
    int Indices);
