using System.Globalization;
using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Escritorio.ModelosDeVista.Planes;

/// <summary>
/// Una operación del plan tal como se muestra en el árbol: nombre, objeto, costo y filas.
/// </summary>
public sealed class NodoDelPlanModeloDeVista
{
    // Una operación que concentra al menos un cuarto del costo merece llamar la atención
    private const double PorcentajeConsideradoCostoso = 25;
    private const double AnchoMaximoDeLaBarra = 80;

    private readonly NodoDelPlan _nodo;

    public NodoDelPlanModeloDeVista(NodoDelPlan nodo)
    {
        _nodo = nodo;
        Hijos = nodo.Hijos.Select(hijo => new NodoDelPlanModeloDeVista(hijo)).ToList();
    }

    public IReadOnlyList<NodoDelPlanModeloDeVista> Hijos { get; }

    public string Operacion => _nodo.OperacionLogica.Length > 0 && _nodo.OperacionLogica != _nodo.OperacionFisica
        ? $"{_nodo.OperacionFisica} ({_nodo.OperacionLogica})"
        : _nodo.OperacionFisica;

    public string? Objeto => _nodo.Objeto;

    public bool TieneObjeto => _nodo.Objeto is not null;

    public string Costo => $"{_nodo.PorcentajeDelCosto.ToString("0.#", CultureInfo.InvariantCulture)} %";

    public double AnchoDeLaBarraDeCosto => Math.Max(2, _nodo.PorcentajeDelCosto / 100 * AnchoMaximoDeLaBarra);

    public bool EsCostosa => _nodo.PorcentajeDelCosto >= PorcentajeConsideradoCostoso;

    public string Filas => _nodo.FilasReales is long reales
        ? $"Filas estimadas: {_nodo.FilasEstimadas:0.##} · reales: {reales}"
        : $"Filas estimadas: {_nodo.FilasEstimadas:0.##}";

    public bool EstimacionMuyDiferente => _nodo.EstimacionMuyDiferente;

    public bool TieneAdvertencias => _nodo.TieneAdvertencias;

    public string Advertencias => string.Join(" · ", _nodo.Advertencias);
}
