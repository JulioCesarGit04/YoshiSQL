namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Qué incluir al exportar una base de datos a un script .sql.
/// </summary>
public sealed record OpcionesDeExportacion(
    bool EstructuraDeTablas,
    bool DatosDeTablas,
    bool Vistas,
    bool Procedimientos,
    bool Funciones)
{
    public static readonly OpcionesDeExportacion Todo = new(true, true, true, true, true);
}
