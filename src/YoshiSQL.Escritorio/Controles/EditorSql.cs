using System.Xml;
using Avalonia;
using Avalonia.Data;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Editor de código T-SQL: resaltado de sintaxis, números de línea y
/// publicación del texto subrayado para "ejecutar solo la selección".
/// </summary>
public sealed class EditorSql : TextEditor
{
    private const string NombreDelRecursoDeResaltado = "ResaltadoTSql.xshd";

    public static readonly StyledProperty<RangoDeTexto> SeleccionProperty =
        AvaloniaProperty.Register<EditorSql, RangoDeTexto>(
            nameof(Seleccion),
            defaultBindingMode: BindingMode.OneWayToSource);

    private static readonly Lazy<IHighlightingDefinition> DefinicionDeResaltado = new(CargarDefinicionDeResaltado);

    public EditorSql()
    {
        SyntaxHighlighting = DefinicionDeResaltado.Value;
        ShowLineNumbers = true;
        Options.ConvertTabsToSpaces = true;
        Options.IndentationSize = 4;
        Options.HighlightCurrentLine = true;
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;

        TextArea.SelectionChanged += (_, _) => PublicarSeleccion();
    }

    public RangoDeTexto Seleccion
    {
        get => GetValue(SeleccionProperty);
        set => SetValue(SeleccionProperty, value);
    }

    // Reutiliza la plantilla visual del TextEditor original
    protected override Type StyleKeyOverride => typeof(TextEditor);

    /// <summary>
    /// Subraya la línea indicada y la desplaza a la vista; se usa al hacer doble clic en un error.
    /// </summary>
    public void IrALinea(int numeroDeLinea)
    {
        if (Document is null || numeroDeLinea < 1 || numeroDeLinea > Document.LineCount)
        {
            return;
        }

        var linea = Document.GetLineByNumber(numeroDeLinea);
        Select(linea.Offset, linea.Length);
        ScrollToLine(numeroDeLinea);
        TextArea.Focus();
    }

    private void PublicarSeleccion()
    {
        var segmento = TextArea.Selection.SurroundingSegment;
        Seleccion = segmento is null ? RangoDeTexto.Vacio : new RangoDeTexto(segmento.Offset, segmento.Length);
    }

    private static IHighlightingDefinition CargarDefinicionDeResaltado()
    {
        var ensamblado = typeof(EditorSql).Assembly;
        using var flujo = ensamblado.GetManifestResourceStream(NombreDelRecursoDeResaltado)
            ?? throw new InvalidOperationException($"No se encontró el recurso '{NombreDelRecursoDeResaltado}'.");
        using var lector = XmlReader.Create(flujo);

        return HighlightingLoader.Load(lector, HighlightingManager.Instance);
    }
}
