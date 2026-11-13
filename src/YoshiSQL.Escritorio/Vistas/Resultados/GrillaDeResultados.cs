using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.Convertidores;
using YoshiSQL.Escritorio.ModelosDeVista.Resultados;
using YoshiSQL.Escritorio.Vistas.Comunes;

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
        CanUserSortColumns = true,
        HeadersVisibility = DataGridHeadersVisibility.All,
        SelectionMode = DataGridSelectionMode.Extended,
        ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader
    };

    // Oculta hasta que se pide "Filtrar filas...": la mayoría de las veces no hace falta y quitaría espacio
    private readonly TextBox _cajaDeFiltro = new()
    {
        PlaceholderText = "Filtrar las filas que contengan... (Esc para quitar el filtro)",
        Margin = new Thickness(6, 4),
        IsVisible = false
    };

    private DataGridCollectionView? _vistaDeFilas;

    public GrillaDeResultados()
    {
        // Número de fila en el encabezado, como en SSMS
        _grilla.LoadingRow += (_, argumentos) => argumentos.Row.Header = argumentos.Row.Index + 1;

        _cajaDeFiltro.TextChanged += (_, _) => AplicarFiltro();
        _cajaDeFiltro.KeyDown += (_, argumentos) =>
        {
            if (argumentos.Key == Key.Escape)
            {
                QuitarFiltro();
                argumentos.Handled = true;
            }
        };

        var panel = new DockPanel();
        DockPanel.SetDock(_cajaDeFiltro, Dock.Top);
        panel.Children.Add(_cajaDeFiltro);
        panel.Children.Add(_grilla);
        Content = panel;
    }

    private void MostrarFiltro()
    {
        _cajaDeFiltro.IsVisible = true;
        Dispatcher.UIThread.Post(() => _cajaDeFiltro.Focus(), DispatcherPriority.Background);
    }

    private void QuitarFiltro()
    {
        _cajaDeFiltro.Text = string.Empty;
        _cajaDeFiltro.IsVisible = false;
        _grilla.Focus();
    }

    /// <summary>
    /// Deja solo las filas en las que alguna celda contiene el texto. Trabaja sobre lo ya recibido: no vuelve a consultar.
    /// </summary>
    private void AplicarFiltro()
    {
        if (_vistaDeFilas is null)
        {
            return;
        }

        var texto = _cajaDeFiltro.Text?.Trim();

        _vistaDeFilas.Filter = string.IsNullOrEmpty(texto)
            ? null
            : fila => fila is object?[] valores && valores.Any(valor => TextoDeCelda(valor).Contains(texto, StringComparison.OrdinalIgnoreCase));
    }

    private static string TextoDeCelda(object? valor) =>
        ValorDeCeldaATexto.Instancia.Convert(valor, typeof(string), null, CultureInfo.InvariantCulture) as string ?? string.Empty;

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
        _vistaDeFilas = null;
        _cajaDeFiltro.Text = string.Empty;
        _cajaDeFiltro.IsVisible = false;

        if (modelo is null)
        {
            return;
        }

        var columnas = modelo.Conjunto.Columnas;

        for (var posicion = 0; posicion < columnas.Count; posicion++)
        {
            _grilla.Columns.Add(CrearColumna(columnas[posicion], posicion));
        }

        _vistaDeFilas = new DataGridCollectionView(modelo.Conjunto.Filas);
        _grilla.ItemsSource = _vistaDeFilas;
        _grilla.ContextMenu = CrearMenuContextual(modelo);
    }

    private ContextMenu CrearMenuContextual(ConjuntoDeResultadosModeloDeVista modelo)
    {
        var verValor = new MenuItem { Header = "Ver valor de la celda..." };
        verValor.Click += (_, _) => MostrarValorDeLaCeldaActual();

        var filtrar = new MenuItem { Header = "Filtrar filas..." };
        filtrar.Click += (_, _) => MostrarFiltro();

        return new ContextMenu
        {
            ItemsSource = new object[]
            {
                verValor,
                filtrar,
                new Separator(),
                CrearOpcion("Copiar todo con encabezados", modelo.CopiarTodoCommand),
                CrearOpcion("Copiar como INSERT", modelo.CopiarComoInsertCommand),
                new Separator(),
                CrearOpcion("Exportar a CSV...", modelo.ExportarACsvCommand),
                CrearOpcion("Exportar a Excel...", modelo.ExportarAExcelCommand),
                CrearOpcion("Exportar a JSON...", modelo.ExportarAJsonCommand)
            }
        };
    }

    // Abre el valor de la celda activa en una ventana, con XML y JSON formateados
    private void MostrarValorDeLaCeldaActual()
    {
        if (_grilla.CurrentColumn?.Tag is not int posicion
            || _grilla.SelectedItem is not object?[] fila
            || posicion >= fila.Length)
        {
            return;
        }

        var columna = Conjunto?.Conjunto.Columnas.ElementAtOrDefault(posicion)?.Nombre ?? "Valor";
        var texto = FormateadorDeValorDeCelda.Formatear(fila[posicion]);

        if (TopLevel.GetTopLevel(this) is Window ventana)
        {
            _ = new DialogoDeTexto(columna, texto).ShowDialog(ventana);
        }
    }

    private static MenuItem CrearOpcion(string texto, ICommand comando) => new() { Header = texto, Command = comando };

    private static DataGridTextColumn CrearColumna(ColumnaDeResultado columna, int posicion) => new ColumnaDeResultados(posicion, columna.NombreDelTipo)
    {
        Header = columna.Nombre,
        // El Tag guarda la posición del valor en la fila; lo usa "Ver valor de la celda"
        Tag = posicion,
        // Cada fila es un arreglo de valores; la columna lee la posición que le corresponde
        Binding = new Binding($"[{posicion}]") { Converter = ValorDeCeldaATexto.Instancia, Mode = BindingMode.OneTime },
        IsReadOnly = true,
        MaxWidth = AnchoMaximoDeColumna,
        // Clic en el encabezado ordena por esa columna (solo en pantalla; no vuelve a consultar)
        CanUserSort = true,
        CustomSortComparer = new ComparadorDeCeldas(posicion)
    };
}
