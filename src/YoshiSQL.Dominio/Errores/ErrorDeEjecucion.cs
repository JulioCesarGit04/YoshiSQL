namespace YoshiSQL.Dominio.Errores;

public sealed class ErrorDeEjecucion : ErrorDeYoshiSql
{
    public ErrorDeEjecucion(string mensaje, int? numeroDeLinea = null, Exception? causa = null)
        : base(mensaje, causa)
    {
        NumeroDeLinea = numeroDeLinea;
    }

    public int? NumeroDeLinea { get; }
}
