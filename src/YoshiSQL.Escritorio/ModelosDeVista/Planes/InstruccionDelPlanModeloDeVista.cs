using System.Globalization;
using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Escritorio.ModelosDeVista.Planes;

public sealed class InstruccionDelPlanModeloDeVista
{
    private const int LargoMaximoDelTexto = 300;

    public InstruccionDelPlanModeloDeVista(InstruccionDelPlan instruccion, int numero)
    {
        Encabezado = $"Instrucción {numero} · costo estimado {instruccion.CostoTotal.ToString("0.####", CultureInfo.InvariantCulture)}";
        Texto = instruccion.Texto.Length > LargoMaximoDelTexto ? instruccion.Texto[..LargoMaximoDelTexto] + "..." : instruccion.Texto;
        Advertencias = string.Join(" · ", instruccion.Advertencias);
        Raices = instruccion.Raiz is null ? [] : [new NodoDelPlanModeloDeVista(instruccion.Raiz)];
    }

    public string Encabezado { get; }

    public string Texto { get; }

    public string Advertencias { get; }

    public bool TieneAdvertencias => Advertencias.Length > 0;

    /// <summary>
    /// Lista de un solo elemento porque el árbol de la vista necesita una colección como raíz.
    /// </summary>
    public IReadOnlyList<NodoDelPlanModeloDeVista> Raices { get; }
}
