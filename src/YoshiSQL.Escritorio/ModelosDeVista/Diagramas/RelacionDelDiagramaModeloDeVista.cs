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
    private const double LargoDeLaFlecha = 10;
    private const double MitadDelAnchoDeLaFlecha = 4.5;
    private const double OpacidadAtenuada = 0.15;
    private const double OpacidadNormal = 1;

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

    public TablaDelDiagramaModeloDeVista TablaOrigen => _tablaOrigen;

    public TablaDelDiagramaModeloDeVista TablaDestino => _tablaDestino;

    public string Nombre => _relacion.LlaveForanea.Nombre;

    public IReadOnlyList<string> ColumnasOrigen => _relacion.LlaveForanea.ColumnasOrigen;

    public IReadOnlyList<string> ColumnasDestino => _relacion.LlaveForanea.ColumnasDestino;

    /// <summary>Verdadero si es la relación que el usuario tocó (o una de las de la tabla que tocó).</summary>
    [ObservableProperty]
    public partial bool EstaSeleccionada { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Opacidad))]
    public partial bool EstaAtenuada { get; set; }

    public double Opacidad => EstaAtenuada ? OpacidadAtenuada : OpacidadNormal;

    /// <summary>
    /// Explica la relación con palabras: de qué tabla y columna sale el dato y en cuál se guarda.
    /// </summary>
    public string Explicacion
    {
        get
        {
            var llave = _relacion.LlaveForanea;
            var origen = llave.TablaOrigen.NombreCompleto;
            var destino = llave.TablaDestino.NombreCompleto;
            var columnasOrigen = string.Join(", ", llave.ColumnasOrigen);
            var columnasDestino = string.Join(", ", llave.ColumnasDestino);

            return $"El dato sale de {destino} ({columnasDestino}) y se guarda en {origen} ({columnasOrigen}).\n"
                + $"Cada fila de {origen} apunta a una fila de {destino}.\n"
                + $"Si se elimina la fila de {destino}: {DescribirAccion(llave.AlEliminar)}.";
        }
    }

    private static string DescribirAccion(string accion) => accion switch
    {
        "CASCADE" => "se eliminan también las filas que la usan",
        "SET_NULL" => "la columna de las filas que la usan queda en NULL",
        "SET_DEFAULT" => "la columna de las filas que la usan vuelve a su valor predeterminado",
        _ => "SQL Server no lo permite mientras haya filas que la usen"
    };

    /// <summary>Triángulo en el extremo de destino: indica hacia qué tabla apunta la relación.</summary>
    [ObservableProperty]
    public partial IList<Point> PuntasDeLaFlecha { get; private set; } = [];

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
        PuntasDeLaFlecha = CalcularFlecha(puntos[^2], puntos[^1]);
    }

    /// <summary>
    /// El último tramo siempre es horizontal: la flecha apunta en el sentido en que ese tramo llega a la tabla.
    /// </summary>
    private static List<Point> CalcularFlecha(Point penultimo, Point punta)
    {
        var sentido = punta.X >= penultimo.X ? 1 : -1;
        var baseDeLaFlecha = punta.X - (sentido * LargoDeLaFlecha);

        return
        [
            punta,
            new(baseDeLaFlecha, punta.Y - MitadDelAnchoDeLaFlecha),
            new(baseDeLaFlecha, punta.Y + MitadDelAnchoDeLaFlecha)
        ];
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
