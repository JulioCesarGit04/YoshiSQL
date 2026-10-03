using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using YoshiSQL.Dominio.Autocompletado;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Una fila de la lista de autocompletado: marca de color según el tipo, texto y detalle.
/// </summary>
internal sealed class DatoDeSugerencia : ICompletionData
{
    private const double TamanoDeLaMarca = 8;

    private static readonly IBrush ColorDelDetalle = new SolidColorBrush(Color.Parse("#8B93A0"));

    // A igual coincidencia se preselecciona la de mayor prioridad: columnas, tablas, palabras clave, funciones
    private static readonly Dictionary<TipoDeSugerencia, (IBrush Color, double Prioridad)> AparienciaPorTipo = new()
    {
        [TipoDeSugerencia.Columna] = (new SolidColorBrush(Color.Parse("#E5C07B")), 5),
        [TipoDeSugerencia.Tabla] = (new SolidColorBrush(Color.Parse("#61AFEF")), 4),
        [TipoDeSugerencia.Vista] = (new SolidColorBrush(Color.Parse("#C678DD")), 4),
        [TipoDeSugerencia.Esquema] = (new SolidColorBrush(Color.Parse("#98C379")), 3),
        [TipoDeSugerencia.PalabraClave] = (new SolidColorBrush(Color.Parse("#569CD6")), 2),
        [TipoDeSugerencia.Funcion] = (new SolidColorBrush(Color.Parse("#D486C9")), 1)
    };

    private readonly Sugerencia _sugerencia;

    public DatoDeSugerencia(Sugerencia sugerencia)
    {
        _sugerencia = sugerencia;
    }

    public IImage? Image => null;

    public string Text => _sugerencia.Texto;

    public object Content => CrearContenido();

    public object? Description => _sugerencia.Detalle;

    public double Priority => AparienciaPorTipo[_sugerencia.Tipo].Prioridad;

    public void Complete(TextArea areaDeTexto, ISegment segmentoAReemplazar, EventArgs argumentos) =>
        areaDeTexto.Document.Replace(segmentoAReemplazar, _sugerencia.Texto);

    private StackPanel CrearContenido()
    {
        var contenido = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        contenido.Children.Add(new Border
        {
            Width = TamanoDeLaMarca,
            Height = TamanoDeLaMarca,
            CornerRadius = new Avalonia.CornerRadius(2),
            Background = AparienciaPorTipo[_sugerencia.Tipo].Color,
            VerticalAlignment = VerticalAlignment.Center
        });

        contenido.Children.Add(new TextBlock { Text = _sugerencia.Texto, VerticalAlignment = VerticalAlignment.Center });

        if (_sugerencia.Detalle is not null)
        {
            contenido.Children.Add(new TextBlock
            {
                Text = _sugerencia.Detalle,
                FontSize = 11,
                Foreground = ColorDelDetalle,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return contenido;
    }
}
