using System.ComponentModel;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

/// <summary>
/// Línea entre la columna foránea (origen) y la llave primaria (destino).
/// Se vuelve a calcular cada vez que se mueve cualquiera de las dos tablas.
/// </summary>
public sealed partial class RelacionDelDiagramaModeloDeVista : ModeloDeVistaBase
{
    private const double LongitudDelTramoLateral = 28;
    private const double RadioDelMarcador = 4;

    private readonly RelacionEntreTablas _relacion;
    private readonly TablaDelDiagramaModeloDeVista _tablaOrigen;
    private readonly TablaDelDiagramaModeloDeVista _tablaDestino;

    public RelacionDelDiagramaModeloDeVista(
        RelacionEntreTablas relacion,
        TablaDelDiagramaModeloDeVista tablaOrigen,
        TablaDelDiagramaModeloDeVista tablaDestino)
    {
        _relacion = relacion;
        _tablaOrigen = tablaOrigen;
        _tablaDestino = tablaDestino;

        _tablaOrigen.PropertyChanged += AlMoverseUnaTabla;
        _tablaDestino.PropertyChanged += AlMoverseUnaTabla;
        RecalcularRecorrido();
    }

    public string Descripcion
    {
        get
        {
            var llave = _relacion.LlaveForanea;
            var columnasOrigen = string.Join(", ", llave.ColumnasOrigen);
            var columnasDestino = string.Join(", ", llave.ColumnasDestino);
            return $"{llave.Nombre}\n{llave.TablaOrigen.NombreCompleto} ({columnasOrigen}) → {llave.TablaDestino.NombreCompleto} ({columnasDestino})";
        }
    }

    [ObservableProperty]
    public partial IList<Point> Puntos { get; private set; } = [];

    [ObservableProperty]
    public partial double MarcadorDeOrigenIzquierda { get; private set; }

    [ObservableProperty]
    public partial double MarcadorDeOrigenArriba { get; private set; }

    [ObservableProperty]
    public partial double MarcadorDeDestinoIzquierda { get; private set; }

    [ObservableProperty]
    public partial double MarcadorDeDestinoArriba { get; private set; }

    private void AlMoverseUnaTabla(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName is nameof(TablaDelDiagramaModeloDeVista.X) or nameof(TablaDelDiagramaModeloDeVista.Y))
        {
            RecalcularRecorrido();
        }
    }

    private void RecalcularRecorrido()
    {
        var alturaOrigen = _tablaOrigen.CalcularCentroVerticalDeColumna(_relacion.LlaveForanea.ColumnasOrigen[0]);
        var alturaDestino = _tablaDestino.CalcularCentroVerticalDeColumna(_relacion.LlaveForanea.ColumnasDestino[0]);

        var puntos = _relacion.EsAutorreferencia
            ? CalcularRecorridoDeAutorreferencia(alturaOrigen, alturaDestino)
            : CalcularRecorridoEntreTablas(alturaOrigen, alturaDestino);

        Puntos = puntos;
        MarcadorDeOrigenIzquierda = puntos[0].X - RadioDelMarcador;
        MarcadorDeOrigenArriba = puntos[0].Y - RadioDelMarcador;
        MarcadorDeDestinoIzquierda = puntos[^1].X - RadioDelMarcador;
        MarcadorDeDestinoArriba = puntos[^1].Y - RadioDelMarcador;
    }

    /// <summary>
    /// Línea en forma de escalón: sale por el lado de la tabla origen que mira hacia la tabla destino.
    /// </summary>
    private List<Point> CalcularRecorridoEntreTablas(double alturaOrigen, double alturaDestino)
    {
        var bordeDerechoOrigen = _tablaOrigen.X + TablaDelDiagramaModeloDeVista.Ancho;
        var bordeDerechoDestino = _tablaDestino.X + TablaDelDiagramaModeloDeVista.Ancho;

        if (_tablaDestino.X >= bordeDerechoOrigen + LongitudDelTramoLateral)
        {
            return CrearEscalon(bordeDerechoOrigen, alturaOrigen, _tablaDestino.X, alturaDestino);
        }

        if (_tablaOrigen.X >= bordeDerechoDestino + LongitudDelTramoLateral)
        {
            return CrearEscalon(_tablaOrigen.X, alturaOrigen, bordeDerechoDestino, alturaDestino);
        }

        // Las tablas están una encima de la otra: la línea rodea ambas por la derecha
        var rodeoDerecho = Math.Max(bordeDerechoOrigen, bordeDerechoDestino) + LongitudDelTramoLateral;
        return
        [
            new(bordeDerechoOrigen, alturaOrigen),
            new(rodeoDerecho, alturaOrigen),
            new(rodeoDerecho, alturaDestino),
            new(bordeDerechoDestino, alturaDestino)
        ];
    }

    private List<Point> CalcularRecorridoDeAutorreferencia(double alturaOrigen, double alturaDestino)
    {
        var bordeDerecho = _tablaOrigen.X + TablaDelDiagramaModeloDeVista.Ancho;
        var rodeoDerecho = bordeDerecho + LongitudDelTramoLateral;

        return
        [
            new(bordeDerecho, alturaOrigen),
            new(rodeoDerecho, alturaOrigen),
            new(rodeoDerecho, alturaDestino),
            new(bordeDerecho, alturaDestino)
        ];
    }

    private static List<Point> CrearEscalon(double inicioX, double inicioY, double finX, double finY)
    {
        var mitadX = (inicioX + finX) / 2;
        return [new(inicioX, inicioY), new(mitadX, inicioY), new(mitadX, finY), new(finX, finY)];
    }
}
