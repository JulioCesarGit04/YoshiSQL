using System.Text.RegularExpressions;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Salva los lotes "CREATE OR ALTER PROCEDURE sp_..." que SQL Server rechaza cuando hay un procedimiento
/// con el mismo nombre en master.
/// </summary>
/// <remarks>
/// SQL Server busca los procedimientos "sp_" también en master. Si ahí quedó una copia (de una vez que el script
/// se ejecutó estando en master), CREATE OR ALTER cree que el procedimiento ya existe e intenta modificarlo en la
/// base actual, donde no está: mensaje 208, estado 6. Pasó en un Ubuntu real con 79 procedimientos a la vez.
/// El propio error confirma que en la base actual no existe, así que un CREATE simple hace lo que el usuario pidió.
/// Comprobado contra SQL Server 2022: el CREATE funciona y, una vez creado, CREATE OR ALTER vuelve a funcionar.
/// </remarks>
internal static partial class ReparadorDeCreateOrAlter
{
    // "El nombre de objeto no es válido" con estado 6: se intentó modificar (ALTER) un objeto que no está en esta base
    private const int NumeroDeObjetoNoValido = 208;
    private const int EstadoDeAlterSinObjeto = 6;

    private const string PrefijoDeProcedimientosDelSistema = "sp_";

    public static bool EsAlterDeUnSpQueNoEstaEnLaBase(int numero, byte estado, string? procedimiento) =>
        numero == NumeroDeObjetoNoValido
        && estado == EstadoDeAlterSinObjeto
        && procedimiento is not null
        && procedimiento.StartsWith(PrefijoDeProcedimientosDelSistema, StringComparison.OrdinalIgnoreCase);

    /// <returns>
    /// El mismo lote con CREATE en lugar de CREATE OR ALTER, o nulo si el lote no empieza creando ese procedimiento.
    /// Conserva los saltos de línea para que los números de línea de los errores sigan coincidiendo con el editor.
    /// </returns>
    public static string? QuitarOrAlter(string textoDelLote, string procedimiento)
    {
        var coincidencia = ExpresionDeCreateOrAlterAlInicio().Match(textoDelLote);

        if (!coincidencia.Success || !NombraA(coincidencia.Groups["nombre"].Value, procedimiento))
        {
            return null;
        }

        var orAlter = coincidencia.Groups["oralter"];
        var soloSaltosDeLinea = string.Concat(orAlter.Value.Where(caracter => caracter is '\r' or '\n'));

        return string.Concat(
            textoDelLote.AsSpan(0, orAlter.Index),
            soloSaltosDeLinea,
            textoDelLote.AsSpan(orAlter.Index + orAlter.Length));
    }

    // El lote puede escribirlo como sp_X, dbo.sp_X, [dbo].[sp_X] o "dbo"."sp_X"; el error trae solo sp_X
    private static bool NombraA(string nombreEnElLote, string procedimiento)
    {
        var ultimaParte = nombreEnElLote[(nombreEnElLote.LastIndexOf('.') + 1)..].Trim('[', ']', '"');

        return string.Equals(ultimaParte, procedimiento, StringComparison.OrdinalIgnoreCase);
    }

    // Solo al inicio del lote (tras espacios y comentarios): un "create or alter" dentro de un texto o de SQL dinámico no se toca
    [GeneratedRegex(
        @"\A(?:\s+|--[^\n]*|/\*[\s\S]*?\*/)*CREATE(?<oralter>\s+OR\s+ALTER)\s+PROC(?:EDURE)?\s+(?<nombre>[^\s(@;]+)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpresionDeCreateOrAlterAlInicio();
}
