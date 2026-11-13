using System.Collections;

namespace YoshiSQL.Escritorio.Vistas.Resultados;

/// <summary>
/// Ordena las filas de una grilla de resultados por una de sus columnas. Cada fila es un arreglo de valores,
/// así que la grilla no sabe compararlas sola. Los nulos van primero, como en SQL Server.
/// </summary>
internal sealed class ComparadorDeCeldas : IComparer
{
    private readonly int _posicion;

    public ComparadorDeCeldas(int posicion)
    {
        _posicion = posicion;
    }

    public int Compare(object? primera, object? segunda)
    {
        var valorDeLaPrimera = LeerValor(primera);
        var valorDeLaSegunda = LeerValor(segunda);

        if (valorDeLaPrimera is null || valorDeLaSegunda is null)
        {
            return (valorDeLaPrimera is null ? 0 : 1) - (valorDeLaSegunda is null ? 0 : 1);
        }

        // Mismo tipo y comparable (números, fechas, textos): orden natural
        if (valorDeLaPrimera.GetType() == valorDeLaSegunda.GetType() && valorDeLaPrimera is IComparable comparable)
        {
            return comparable.CompareTo(valorDeLaSegunda);
        }

        // Tipos mezclados o no comparables (binarios, sql_variant): por su texto
        return string.Compare(valorDeLaPrimera.ToString(), valorDeLaSegunda.ToString(), StringComparison.CurrentCultureIgnoreCase);
    }

    private object? LeerValor(object? fila) =>
        fila is object?[] valores && _posicion < valores.Length ? valores[_posicion] : null;
}
