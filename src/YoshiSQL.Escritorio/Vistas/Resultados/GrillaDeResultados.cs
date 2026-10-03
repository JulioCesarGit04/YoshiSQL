using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.Convertidores;

namespace YoshiSQL.Escritorio.Vistas.Resultados;

/// <summary>
/// Grilla de solo lectura para un conjunto de resultados. Las columnas se crean
/// en código porque cada consulta devuelve columnas distintas.
/// </summary>
public sealed class GrillaDeResultados : UserControl
{
    public static readonly StyledProperty<ConjuntoDeResultados?> ConjuntoProperty =
        AvaloniaProperty.Register<GrillaDeResultados, ConjuntoDeResultados?>(nameof(Conjunto));

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

    public ConjuntoDeResultados? Conjunto
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

    private void MostrarConjunto(ConjuntoDeResultados? conjunto)
    {
        _grilla.ItemsSource = null;
        _grilla.Columns.Clear();

        if (conjunto is null)
        {
            return;
        }

        for (var posicion = 0; posicion < conjunto.Columnas.Count; posicion++)
        {
            _grilla.Columns.Add(CrearColumna(conjunto.Columnas[posicion], posicion));
        }

        _grilla.ItemsSource = conjunto.Filas;
    }

    private static DataGridTextColumn CrearColumna(ColumnaDeResultado columna, int posicion) => new()
    {
        Header = columna.Nombre,
        // Cada fila es un arreglo de valores; la columna lee la posición que le corresponde
        Binding = new Binding($"[{posicion}]") { Converter = ValorDeCeldaATexto.Instancia, Mode = BindingMode.OneTime },
        IsReadOnly = true,
        MaxWidth = 480
    };
}
