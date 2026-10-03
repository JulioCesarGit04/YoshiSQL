namespace YoshiSQL.Dominio.Errores;

public sealed class ErrorDeConexion : ErrorDeYoshiSql
{
    public ErrorDeConexion(string mensaje, Exception? causa = null)
        : base(mensaje, causa)
    {
    }
}
