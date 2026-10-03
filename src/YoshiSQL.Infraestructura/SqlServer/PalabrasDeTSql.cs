namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Palabras clave y funciones de T-SQL que ofrece el autocompletado.
/// </summary>
internal static class PalabrasDeTSql
{
    public static readonly IReadOnlyList<string> PalabrasClave =
    [
        "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC", "BACKUP", "BEGIN", "BETWEEN", "BREAK", "BY",
        "CASCADE", "CASE", "CATCH", "CHECK", "CLUSTERED", "COLUMN", "COMMIT", "CONSTRAINT", "CONTINUE",
        "CREATE", "CROSS", "CURSOR", "DATABASE", "DEALLOCATE", "DECLARE", "DEFAULT", "DELETE", "DESC",
        "DISTINCT", "DROP", "ELSE", "END", "EXCEPT", "EXEC", "EXECUTE", "EXISTS", "FETCH", "FOREIGN",
        "FROM", "FULL", "FUNCTION", "GO", "GRANT", "GROUP BY", "HAVING", "IDENTITY", "IF", "IN", "INDEX",
        "INNER JOIN", "INSERT", "INTERSECT", "INTO", "IS", "JOIN", "KEY", "LEFT JOIN", "LIKE", "MERGE",
        "NOCOUNT", "NONCLUSTERED", "NOT", "NULL", "OFFSET", "ON", "OR", "ORDER BY", "OUTER APPLY",
        "OUTPUT", "OVER", "PARTITION BY", "PRIMARY KEY", "PRINT", "PROCEDURE", "RAISERROR", "REFERENCES",
        "RETURN", "RETURNS", "REVOKE", "RIGHT JOIN", "ROLLBACK", "ROWS", "SCHEMA", "SELECT", "SET",
        "TABLE", "THEN", "THROW", "TOP", "TRANSACTION", "TRIGGER", "TRUNCATE", "TRY", "UNION", "UNION ALL",
        "UNIQUE", "UPDATE", "USE", "VALUES", "VIEW", "WHEN", "WHERE", "WHILE", "WITH",
        "BIGINT", "BIT", "CHAR", "DATE", "DATETIME", "DATETIME2", "DATETIMEOFFSET", "DECIMAL", "FLOAT",
        "INT", "MONEY", "NCHAR", "NUMERIC", "NVARCHAR", "REAL", "SMALLINT", "TIME", "TINYINT",
        "UNIQUEIDENTIFIER", "VARBINARY", "VARCHAR", "XML"
    ];

    public static readonly IReadOnlyList<string> Funciones =
    [
        "ABS", "AVG", "CAST", "CEILING", "CHARINDEX", "CHOOSE", "COALESCE", "CONCAT", "CONCAT_WS", "CONVERT",
        "COUNT", "COUNT_BIG", "CURRENT_TIMESTAMP", "DATEADD", "DATEDIFF", "DATEFROMPARTS", "DATENAME",
        "DATEPART", "DAY", "DB_ID", "DB_NAME", "DENSE_RANK", "EOMONTH", "FIRST_VALUE", "FLOOR", "FORMAT",
        "GETDATE", "GETUTCDATE", "IIF", "ISNULL", "ISNUMERIC", "JSON_QUERY", "JSON_VALUE", "LAG",
        "LAST_VALUE", "LEAD", "LEFT", "LEN", "LOWER", "LTRIM", "MAX", "MIN", "MONTH", "NEWID", "NULLIF",
        "OBJECT_ID", "OBJECT_NAME", "PATINDEX", "QUOTENAME", "RANK", "REPLACE", "REPLICATE", "REVERSE",
        "RIGHT", "ROUND", "ROW_NUMBER", "RTRIM", "SCOPE_IDENTITY", "SERVERPROPERTY", "SPACE", "STRING_AGG",
        "STRING_SPLIT", "STUFF", "SUBSTRING", "SUM", "SYSDATETIME", "TRANSLATE", "TRIM", "TRY_CAST",
        "TRY_CONVERT", "UPPER", "YEAR"
    ];
}
