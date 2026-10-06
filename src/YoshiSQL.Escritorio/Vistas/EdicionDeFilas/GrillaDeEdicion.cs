using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Escritorio.ModelosDeVista.EdicionDeFilas;

namespace YoshiSQL.Escritorio.Vistas.EdicionDeFilas;

/// <summary>
/// Grilla editable cuyas columnas dependen de la tabla; por eso se construye en código.
/// La primera columna muestra el estado de cada fila (modificada, nueva o por eliminar).
/// </summary>
public sealed class GrillaDeEdicion : UserControl
{
    private const double AnchoMaximoDeColumna = 360;
    private const double AnchoDeLaColumnaDeEstado = 28;

    private static readonly Dictionary<EstadoDeFila, IBrush> ColoresDeEstado = new()
    {
        [EstadoDeFila.SinCambios] = Brushes.Transparent,
        [EstadoDeFila.Modificada] = new SolidColorBrush(Color.Parse("#E5C07B")),
        [EstadoDeFila.Nueva] = new SolidColorBrush(Color.Parse("#6CC24A")),
        [EstadoDeFila.Eliminada] = new SolidColorBrush(Color.Parse("#F07178"))
    };

    private readonly DataGrid _grilla = new()
    {
        CanUserSortColumns = false,
        CanUserReorderColumns = false,
        HeadersVisibility = DataGridHeadersVisibility.Column,
        SelectionMode = DataGridSelectionMode.Single
    };

    private PestanaDeEdicionDeFilasModeloDeVista? _modelo;
    private DataGridCollectionView? _vista;

    public GrillaDeEdicion()
    {
        Content = _grilla;
        _grilla.SelectionChanged += AlCambiarLaSeleccion;
        _grilla.KeyDown += AlPresionarTecla;
    }

    private void AlCambiarLaSeleccion(object? remitente, SelectionChangedEventArgs argumentos)
    {
        if (_modelo is null)
        {
            return;
        }

        // Al salir de una fila modificada, guardarla si el usuario activó esa opción
        if (argumentos.RemovedItems.OfType<FilaEditableModeloDeVista>().FirstOrDefault() is { } filaAnterior)
        {
            _ = _modelo.GuardarFilaModificadaAsync(filaAnterior);
        }

        _modelo.FilaSeleccionada = _grilla.SelectedItem as FilaEditableModeloDeVista;
    }

    // Ctrl+0 pone NULL en la celda activa, como en SSMS
    private void AlPresionarTecla(object? remitente, KeyEventArgs argumentos)
    {
        var esCeroConControl = argumentos.KeyModifiers.HasFlag(KeyModifiers.Control)
            && argumentos.Key is Key.D0 or Key.NumPad0;

        if (!esCeroConControl
            || _grilla.CurrentColumn is not { } columna
            || columna.IsReadOnly
            || columna.Tag is not int posicion
            || _grilla.SelectedItem is not FilaEditableModeloDeVista fila)
        {
            return;
        }

        _grilla.CancelEdit();
        fila[posicion] = FilaEditableModeloDeVista.TextoDeNulo;
        argumentos.Handled = true;
    }

    protected override void OnDataContextChanged(EventArgs argumentos)
    {
        base.OnDataContextChanged(argumentos);

        if (_modelo is not null)
        {
            _modelo.PropertyChanged -= AlCambiarElModelo;
        }

        _modelo = DataContext as PestanaDeEdicionDeFilasModeloDeVista;

        if (_modelo is not null)
        {
            _modelo.PropertyChanged += AlCambiarElModelo;
            ConstruirColumnas();
        }
    }

    private void AlCambiarElModelo(object? remitente, PropertyChangedEventArgs argumentos)
    {
        if (argumentos.PropertyName is nameof(PestanaDeEdicionDeFilasModeloDeVista.Columnas) or nameof(PestanaDeEdicionDeFilasModeloDeVista.PuedeEditarse))
        {
            ConstruirColumnas();
        }
        else if (argumentos.PropertyName == nameof(PestanaDeEdicionDeFilasModeloDeVista.FiltroDeFilas))
        {
            _vista?.Refresh();
        }
    }

    // Filtro rápido del lado del cliente: oculta las filas que no contienen el texto en ninguna celda.
    // Las filas nuevas siempre se ven para poder completarlas.
    private bool FilaCoincideConElFiltro(object? elemento)
    {
        if (_modelo is null || string.IsNullOrWhiteSpace(_modelo.FiltroDeFilas) || elemento is not FilaEditableModeloDeVista fila)
        {
            return true;
        }

        if (fila.Estado == EstadoDeFila.Nueva)
        {
            return true;
        }

        var termino = _modelo.FiltroDeFilas;

        for (var posicion = 0; posicion < _modelo.Columnas.Count; posicion++)
        {
            if (fila[posicion] is { } texto && texto.Contains(termino, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void ConstruirColumnas()
    {
        if (_modelo is null)
        {
            return;
        }

        _grilla.ItemsSource = null;
        _grilla.Columns.Clear();
        _grilla.IsReadOnly = !_modelo.PuedeEditarse;
        _grilla.Columns.Add(CrearColumnaDeEstado());

        for (var posicion = 0; posicion < _modelo.Columnas.Count; posicion++)
        {
            _grilla.Columns.Add(CrearColumnaDeDatos(_modelo.Columnas[posicion], posicion));
        }

        _vista = new DataGridCollectionView(_modelo.Filas) { Filter = FilaCoincideConElFiltro };
        _grilla.ItemsSource = _vista;
    }

    private static DataGridTemplateColumn CrearColumnaDeEstado() => new()
    {
        Width = new DataGridLength(AnchoDeLaColumnaDeEstado),
        IsReadOnly = true,
        CellTemplate = new FuncDataTemplate<FilaEditableModeloDeVista>((_, _) =>
        {
            var marca = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            marca.Bind(Border.BackgroundProperty, new Binding(nameof(FilaEditableModeloDeVista.Estado))
            {
                Converter = new Avalonia.Data.Converters.FuncValueConverter<EstadoDeFila, IBrush>(estado => ColoresDeEstado[estado])
            });

            return marca;
        })
    };

    private static DataGridTextColumn CrearColumnaDeDatos(Columna columna, int posicion) => new()
    {
        Header = columna.EsLlavePrimaria ? $"{columna.Nombre} (PK)" : columna.Nombre,
        // El Tag guarda la posición del valor en la fila; lo usa Ctrl+0 para poner NULL
        Tag = posicion,
        Binding = new Binding($"[{posicion}]") { Mode = BindingMode.TwoWay },
        // Identidad, rowversion y binarios los maneja SQL Server o no se pueden escribir como texto
        IsReadOnly = columna.EsIdentidad || columna.TipoDeDato.Nombre is "timestamp" or "rowversion" or "varbinary" or "binary" or "image",
        MaxWidth = AnchoMaximoDeColumna
    };
}
