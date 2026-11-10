namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Todo lo que acompaña a las tablas y que hace falta para recrear una base que funcione igual que la original:
/// sin esto la copia se crea "sin errores" pero sus procedimientos y la aplicación que la usa fallan
/// (faltan valores predeterminados, tipos tabla, secuencias, restricciones UNIQUE a las que apuntan llaves foráneas...).
/// </summary>
public sealed record DetallesDelEsquema(
    IReadOnlyList<ValorPredeterminado> ValoresPredeterminados,
    IReadOnlyList<RestriccionDeVerificacion> RestriccionesDeVerificacion,
    IReadOnlyList<IndiceDeTabla> Indices,
    IReadOnlyList<ColumnaCalculada> ColumnasCalculadas,
    IReadOnlyList<DisparadorDeTabla> Disparadores,
    IReadOnlyList<Secuencia> Secuencias,
    IReadOnlyList<TipoDeTabla> TiposDeTabla,
    IReadOnlyList<DependenciaEntreModulos> DependenciasEntreModulos,
    IReadOnlyList<IdentidadDeColumna> Identidades,
    IReadOnlyList<Sinonimo> Sinonimos)
{
    public static readonly DetallesDelEsquema Vacio = new([], [], [], [], [], [], [], [], [], []);
}
