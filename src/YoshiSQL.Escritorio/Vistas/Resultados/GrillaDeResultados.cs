using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.Convertidores;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;

namespace YoshiSQL.Escritorio.Vistas.Resultados;

/// <summary>
/// Grilla de solo lectura para un conjunto de resultados. Las columnas se crean
/// en código porque cada consulta devuelve columnas distintas.
/// </summary>
public sealed class GrillaDeResultados : UserControl
{
    public static readonly StyledProperty<ConjuntoDeResultadosModeloDeVista?> ConjuntoProperty =
        AvaloniaProperty.Register<GrillaDeResultados, ConjuntoDeResultadosModeloDeVista?>(nameof(Conjunto));

    private const double AnchoMaximoDeColumna = 480;

    private readonly DataGrid _grilla = new()
    {
        IsReadOnly = true,
        CanUserReorderColumns = true,
        CanUserResizeColumns = true,
        CanUserSortColumns = false,
        HeadersVisibility = DataGridHeadersVisibility.All,
        SelectionMode = DataGridSelectionMode.Extended,
        ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader
    };

    public GrillaDeResultados()
    {
        // Número de fila en el encabezado, como en SSMS
        _grilla.LoadingRow += (_, argumentos) => argumentos.Row.Header = argumentos.Row.Index + 1;
        Content = _grilla;
    }

    public ConjuntoDeResultadosModeloDeVista? Conjunto
    {
        get => GetValue(ConjuntoProperty);
        set => SetValue(ConjuntoProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs cambio)
    {
        base.OnPropertyChanged(cambio);

        if (cambio.Property == ConjuntoProperty)
        {
            MostrarConjunto(Conjunto);
        }
    }

    private void MostrarConjunto(ConjuntoDeResultadosModeloDeVista? modelo)
    {
        _grilla.ItemsSource = null;
        _grilla.Columns.Clear();
        _grilla.ContextMenu = null;

        if (modelo is null)
        {
            return;
        }

        var columnas = modelo.Conjunto.Columnas;

        for (var posicion = 0; posicion < columnas.Count; posicion++)
        {
            _grilla.Columns.Add(CrearColumna(columnas[posicion], posicion));
        }

        _grilla.ItemsSource = modelo.Conjunto.Filas;
        _grilla.ContextMenu = CrearMenuContextual(modelo);
    }

    private static ContextMenu CrearMenuContextual(ConjuntoDeResultadosModeloDeVista modelo) => new()
    {
        ItemsSource = new object[]
        {
            CrearOpcion("Copiar todo con encabezados", modelo.CopiarTodoCommand),
            new Separator(),
            CrearOpcion("Exportar a CSV...", modelo.ExportarACsvCommand),
            CrearOpcion("Exportar a Excel...", modelo.ExportarAExcelCommand),
            CrearOpcion("Exportar a JSON...", modelo.ExportarAJsonCommand)
        }
    };

    private static MenuItem CrearOpcion(string texto, ICommand comando) => new() { Header = texto, Command = comando };

    private static DataGridTextColumn CrearColumna(ColumnaDeResultado columna, int posicion) => new()
    {
        Header = columna.Nombre,
        // Cada fila es un arreglo de valores; la columna lee la posición que le corresponde
        Binding = new Binding($"[{posicion}]") { Converter = ValorDeCeldaATexto.Instancia, Mode = BindingMode.OneTime },
        IsReadOnly = true,
        MaxWidth = AnchoMaximoDeColumna
    };
}
