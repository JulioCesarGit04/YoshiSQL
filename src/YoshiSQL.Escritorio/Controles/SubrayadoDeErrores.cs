using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Dibuja una línea ondulada roja debajo de los errores de sintaxis, como los correctores ortográficos.
/// </summary>
internal sealed class SubrayadoDeErrores : IBackgroundRenderer
{
    private const double AnchoDeLaOnda = 3;
    private const double AltoDeLaOnda = 2;
    private const double GrosorDeLaLinea = 1;

    private static readonly IPen Lapiz = new Pen(new SolidColorBrush(Color.Parse("#F0565F")), GrosorDeLaLinea);

    /// <summary>Tramos del documento a subrayar (posición inicial y longitud).</summary>
    public IReadOnlyList<(int Inicio, int Longitud)> Tramos { get; set; } = [];

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView vistaDeTexto, DrawingContext contexto)
    {
        if (Tramos.Count == 0 || !vistaDeTexto.VisualLinesValid || vistaDeTexto.Document is not { } documento)
        {
            return;
        }

        foreach (var (inicio, longitud) in Tramos)
        {
            // El texto pudo cambiar después del análisis: un tramo fuera del documento se ignora
            if (inicio < 0 || longitud <= 0 || inicio + longitud > documento.TextLength)
            {
                continue;
            }

            var segmento = new TextSegment { StartOffset = inicio, Length = longitud };

            foreach (var rectangulo in BackgroundGeometryBuilder.GetRectsForSegment(vistaDeTexto, segmento))
            {
                DibujarOnda(contexto, rectangulo);
            }
        }
    }

    private static void DibujarOnda(DrawingContext contexto, Rect rectangulo)
    {
        var base_ = rectangulo.Bottom - GrosorDeLaLinea;
        var geometria = new StreamGeometry();

        using (var trazo = geometria.Open())
        {
            trazo.BeginFigure(new Point(rectangulo.Left, base_), isFilled: false);
            var sube = true;

            for (var x = rectangulo.Left + AnchoDeLaOnda; x <= rectangulo.Right + AnchoDeLaOnda; x += AnchoDeLaOnda)
            {
                trazo.LineTo(new Point(Math.Min(x, rectangulo.Right), sube ? base_ - AltoDeLaOnda : base_));
                sube = !sube;
            }

            trazo.EndFigure(isClosed: false);
        }

        contexto.DrawGeometry(null, Lapiz, geometria);
    }
}
