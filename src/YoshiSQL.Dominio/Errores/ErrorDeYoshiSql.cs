namespace YoshiSQL.Dominio.Errores;

/// <summary>
/// Excepción base de YoshiSQL. Su mensaje siempre es apto para mostrarse al usuario.
/// </summary>
public class ErrorDeYoshiSql : Exception
{
    public ErrorDeYoshiSql(string mensaje, Exception? causa = null)
        : base(mensaje, causa)
    {
    }
}
