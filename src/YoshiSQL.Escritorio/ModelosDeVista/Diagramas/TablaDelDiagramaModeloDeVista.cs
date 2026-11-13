using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

/// <summary>
/// Tarjeta de una tabla en el lienzo. Las medidas son fijas para poder calcular
/// exactamente dónde está cada columna al dibujar las relaciones.
/// </summary>
public sealed partial class TablaDelDiagramaModeloDeVista : ModeloDeVistaBase
{
    public const double Ancho = 260;
    public const double AlturaDelEncabezado = 34;
    public const double AlturaDeFila = 22;
    public const double GrosorDelBorde = 1;
    public const double RellenoInferior = 6;

    private readonly NodoDeTabla _nodo;

    public TablaDelDiagramaModeloDeVista(NodoDeTabla nodo, IReadOnlyList<ColumnaDelDiagramaModeloDeVista> columnas)
    {
        _nodo = nodo;
        Columnas = columnas;
        SincronizarDesdeElNodo();
    }

    public Tabla Tabla => _nodo.Tabla;

    public string Titulo => Tabla.NombreCompleto;

    public IReadOnlyList<ColumnaDelDiagramaModeloDeVista> Columnas { get; }

    public double Alto => CalcularAlto(Columnas.Count);

    [ObservableProperty]
    public partial double X { get; private set; }

    [ObservableProperty]
    public partial double Y { get; private set; }

    /// <summary>
    /// Las tablas con un número mayor se dibujan encima; la última tocada queda al frente.
    /// </summary>
    [ObservableProperty]
    public partial int OrdenDeDibujo { get; set; }

    // Lo que no participa de lo seleccionado se ve apagado para que destaque lo que sí
    private const double OpacidadAtenuada = 0.35;
    private const double OpacidadNormal = 1;

    /// <summary>Verdadero si es un extremo de la relación seleccionada (o la tabla seleccionada).</summary>
    [ObservableProperty]
    public partial bool EstaResaltada { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Opacidad))]
    public partial bool EstaAtenuada { get; set; }

    public double Opacidad => EstaAtenuada ? OpacidadAtenuada : OpacidadNormal;

    /// <summary>
    /// Marca la tabla y, dentro de ella, las columnas que participan en la relación. Se puede llamar varias veces
    /// (una tabla puede ser extremo de varias relaciones a la vez): las columnas se van sumando.
    /// </summary>
    public void Resaltar(IEnumerable<string> columnasDeLaRelacion)
    {
        EstaResaltada = true;
        EstaAtenuada = false;
        var nombres = columnasDeLaRelacion.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var columna in Columnas.Where(columna => nombres.Contains(columna.Nombre)))
        {
            columna.EstaResaltada = true;
        }
    }

    public void QuitarResaltado()
    {
        EstaResaltada = false;
        EstaAtenuada = false;

        foreach (var columna in Columnas)
        {
            columna.EstaResaltada = false;
        }
    }

    public static double CalcularAlto(int cantidadDeColumnas) =>
        (GrosorDelBorde * 2) + AlturaDelEncabezado + (cantidadDeColumnas * AlturaDeFila) + RellenoInferior;

    public void MoverA(double posicionX, double posicionY)
    {
        _nodo.Posicion = new PosicionEnElDiagrama(posicionX, posicionY);
        SincronizarDesdeElNodo();
    }

    public void SincronizarDesdeElNodo()
    {
        var posicion = _nodo.Posicion ?? default;
        X = posicion.X;
        Y = posicion.Y;
    }

    /// <summary>
    /// Altura del centro de la fila de una columna, para que la línea de la relación llegue justo ahí.
    /// </summary>
    public double CalcularCentroVerticalDeColumna(string nombreDeColumna)
    {
        for (var indice = 0; indice < Columnas.Count; indice++)
        {
            if (string.Equals(Columnas[indice].Nombre, nombreDeColumna, StringComparison.OrdinalIgnoreCase))
            {
                return Y + GrosorDelBorde + AlturaDelEncabezado + (indice * AlturaDeFila) + (AlturaDeFila / 2);
            }
        }

        return Y + (Alto / 2);
    }
}
