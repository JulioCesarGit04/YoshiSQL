namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Bloque de código que se envía al servidor de una sola vez (lo que hay entre dos GO).
/// </summary>
/// <param name="Texto">Código SQL del lote, sin la instrucción GO.</param>
/// <param name="LineaInicial">Línea del editor donde comienza el lote, para ubicar errores.</param>
/// <param name="Repeticiones">Veces que se ejecuta el lote, como en "GO 5".</param>
public sealed record LoteSql(string Texto, int LineaInicial, int Repeticiones = 1)
{
    public bool EstaVacio => string.IsNullOrWhiteSpace(Texto);
}
