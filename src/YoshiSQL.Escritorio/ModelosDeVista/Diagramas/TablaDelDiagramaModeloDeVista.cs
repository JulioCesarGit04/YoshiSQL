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
