namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Explicaciones para errores de SQL Server cuyo mensaje confunde más de lo que ayuda.
/// </summary>
internal static class PistasDeErrores
{
    // "El nombre de objeto no es válido" con estado 6: se intentó modificar (ALTER) un objeto que no está en esta base
    private const int NumeroDeObjetoNoValido = 208;
    private const int EstadoDeAlterSinObjeto = 6;

    private const string PrefijoDeProcedimientosDelSistema = "sp_";

    /// <returns>Una explicación para agregar al mensaje, o nulo si el error no tiene una pista conocida.</returns>
    public static string? Obtener(int numero, byte estado, string? procedimiento)
    {
        if (numero != NumeroDeObjetoNoValido || estado != EstadoDeAlterSinObjeto || string.IsNullOrEmpty(procedimiento))
        {
            return null;
        }

        // SQL Server busca los procedimientos "sp_" primero en master. Si alguna vez el script se ejecutó estando
        // en master, quedó ahí una copia: CREATE OR ALTER la encuentra, cree que ya existe e intenta modificarla
        // en la base actual, donde no está. Pasó en un Ubuntu real con 79 procedimientos "sp_" a la vez.
        if (procedimiento.StartsWith(PrefijoDeProcedimientosDelSistema, StringComparison.OrdinalIgnoreCase))
        {
            return $"Pista de YoshiSQL: probablemente existe un procedimiento \"{procedimiento}\" en la base master "
                + "(de una vez que el script se ejecutó estando en master). Como el nombre empieza con \"sp_\", SQL Server "
                + "lo encuentra ahí primero y CREATE OR ALTER intenta modificarlo en esta base, donde no existe. "
                + "Revísalo con: SELECT name FROM master.sys.procedures WHERE name LIKE 'sp[_]%' AND is_ms_shipped = 0; "
                + "y elimina de master los que no deban estar ahí.";
        }

        return $"Pista de YoshiSQL: se intentó modificar (ALTER) \"{procedimiento}\", pero no existe en esta base de datos. "
            + "Usa CREATE (o CREATE OR ALTER) para crearlo, o revisa que estés en la base correcta.";
    }
}
