namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Fragmentos de código: se escribe la abreviatura y se pulsa Tab (ej. "ssf" + Tab → SELECT * FROM).
/// </summary>
public static class FragmentosDeTSql
{
    /// <summary>Marca dentro del fragmento el lugar donde queda el cursor después de insertarlo.</summary>
    public const string MarcaDelCursor = "$0";

    private static readonly Dictionary<string, FragmentoDeCodigoSql> PorAbreviatura = new FragmentoDeCodigoSql[]
    {
        new("ssf", "SELECT * FROM", "SELECT *\nFROM $0;"),
        new("st", "SELECT TOP (100)", "SELECT TOP (100) *\nFROM $0;"),
        new("ins", "INSERT", "INSERT INTO $0 ()\nVALUES ();"),
        new("upd", "UPDATE", "UPDATE $0\nSET \nWHERE ;"),
        new("del", "DELETE", "DELETE FROM $0\nWHERE ;"),
        new("ct", "CREATE TABLE", "CREATE TABLE dbo.$0\n(\n    Id int IDENTITY(1,1) NOT NULL PRIMARY KEY\n);"),
        new("cte", "WITH ... AS (expresión de tabla)", "WITH $0 AS\n(\n    SELECT \n)\nSELECT *\nFROM ;"),
        new("decl", "DECLARE", "DECLARE @$0 int = 0;"),
        new("ifex", "IF EXISTS", "IF EXISTS (SELECT 1 FROM $0)\nBEGIN\n    \nEND;"),
        new("try", "TRY / CATCH", "BEGIN TRY\n    $0\nEND TRY\nBEGIN CATCH\n    THROW;\nEND CATCH;"),
        new("tran", "Transacción con TRY / CATCH",
            "BEGIN TRY\n    BEGIN TRANSACTION;\n\n    $0\n\n    COMMIT TRANSACTION;\nEND TRY\nBEGIN CATCH\n    IF @@TRANCOUNT > 0\n        ROLLBACK TRANSACTION;\n\n    THROW;\nEND CATCH;"),
        new("proc", "CREATE PROCEDURE",
            "CREATE OR ALTER PROCEDURE dbo.$0\n    @parametro int\nAS\nBEGIN\n    SET NOCOUNT ON;\n\n    \nEND\nGO"),
        new("func", "CREATE FUNCTION (escalar)",
            "CREATE OR ALTER FUNCTION dbo.$0 (@parametro int)\nRETURNS int\nAS\nBEGIN\n    RETURN @parametro;\nEND\nGO"),
        new("cur", "Cursor",
            "DECLARE @valor int;\n\nDECLARE recorrido CURSOR LOCAL FAST_FORWARD FOR\n    SELECT $0;\n\nOPEN recorrido;\nFETCH NEXT FROM recorrido INTO @valor;\n\nWHILE @@FETCH_STATUS = 0\nBEGIN\n    \n    FETCH NEXT FROM recorrido INTO @valor;\nEND\n\nCLOSE recorrido;\nDEALLOCATE recorrido;")
    }.ToDictionary(fragmento => fragmento.Abreviatura, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<FragmentoDeCodigoSql> Todos => PorAbreviatura.Values;

    public static FragmentoDeCodigoSql? Buscar(string abreviatura) => PorAbreviatura.GetValueOrDefault(abreviatura);

    /// <summary>Lista para mostrar en la ayuda: una línea por fragmento.</summary>
    public static string DescribirTodos() =>
        "Escribe la abreviatura en el editor y pulsa Tab:" + Environment.NewLine + Environment.NewLine
        + string.Join(Environment.NewLine, Todos.Select(fragmento => $"  {fragmento.Abreviatura,-6} {fragmento.Descripcion}"));
}
