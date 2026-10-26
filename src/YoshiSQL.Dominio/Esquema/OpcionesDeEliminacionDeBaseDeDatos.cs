namespace YoshiSQL.Dominio.Esquema;

/// <param name="CerrarConexionesExistentes">
/// Desconecta a todos (SET SINGLE_USER WITH ROLLBACK IMMEDIATE) antes de eliminar; sin esto,
/// cualquier sesión abierta en la base hace fallar el DROP con "la base de datos está en uso".
/// </param>
/// <param name="EliminarHistorialDeRespaldos">Borra de msdb el historial de respaldos y restauraciones de la base.</param>
public sealed record OpcionesDeEliminacionDeBaseDeDatos(
    bool CerrarConexionesExistentes,
    bool EliminarHistorialDeRespaldos)
{
    /// <summary>Lo que usa el script "Generar script DROP DATABASE" del explorador.</summary>
    public static OpcionesDeEliminacionDeBaseDeDatos Predeterminadas { get; } =
        new(CerrarConexionesExistentes: true, EliminarHistorialDeRespaldos: false);
}
