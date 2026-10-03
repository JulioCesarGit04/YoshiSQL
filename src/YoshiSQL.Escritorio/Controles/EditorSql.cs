using System.Xml;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;
using AvaloniaEdit.Search;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Editor de código T-SQL: resaltado de sintaxis, números de línea y
/// publicación del texto subrayado para "ejecutar solo la selección".
/// </summary>
public sealed class EditorSql : TextEditor
{
    private const string RecursoDeResaltadoOscuro = "ResaltadoTSql.xshd";
    private const string RecursoDeResaltadoClaro = "ResaltadoTSqlClaro.xshd";

    public static readonly StyledProperty<IProveedorDeSugerencias?> ProveedorDeSugerenciasProperty =
        AvaloniaProperty.Register<EditorSql, IProveedorDeSugerencias?>(nameof(ProveedorDeSugerencias));

    // Con dos letras escritas ya vale la pena sugerir; con menos, la lista sería demasiado larga
    private const int LetrasParaSugerirAutomaticamente = 2;

    public static readonly StyledProperty<RangoDeTexto> SeleccionProperty =
        AvaloniaProperty.Register<EditorSql, RangoDeTexto>(
            nameof(Seleccion),
            defaultBindingMode: BindingMode.OneWayToSource);

    private static readonly Lazy<IHighlightingDefinition> ResaltadoOscuro = new(() => CargarDefinicionDeResaltado(RecursoDeResaltadoOscuro));
    private static readonly Lazy<IHighlightingDefinition> ResaltadoClaro = new(() => CargarDefinicionDeResaltado(RecursoDeResaltadoClaro));

    private readonly SearchPanel _panelDeBusqueda;
    private CompletionWindow? _ventanaDeSugerencias;

    public EditorSql()
    {
        AplicarResaltadoDelTema();
        ActualThemeVariantChanged += (_, _) => AplicarResaltadoDelTema();
        ShowLineNumbers = true;
        Options.ConvertTabsToSpaces = true;
        Options.IndentationSize = 4;
        Options.HighlightCurrentLine = true;
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;

        TextArea.SelectionChanged += (_, _) => PublicarSeleccion();

        // Panel de buscar y reemplazar incluido en AvaloniaEdit (Ctrl+F)
        _panelDeBusqueda = SearchPanel.Install(this);

        TextArea.TextEntered += AlEscribirTexto;
        TextArea.AddHandler(KeyDownEvent, AlPresionarTecla, RoutingStrategies.Tunnel);
    }

    public IProveedorDeSugerencias? ProveedorDeSugerencias
    {
        get => GetValue(ProveedorDeSugerenciasProperty);
        set => SetValue(ProveedorDeSugerenciasProperty, value);
    }

    public RangoDeTexto Seleccion
    {
        get => GetValue(SeleccionProperty);
        set => SetValue(SeleccionProperty, value);
    }

    // Reutiliza la plantilla visual del TextEditor original
    protected override Type StyleKeyOverride => typeof(TextEditor);

    /// <summary>
    /// Abre el panel de búsqueda con el texto subrayado como término a buscar.
    /// </summary>
    public void AbrirBusqueda(bool conReemplazo)
    {
        if (!TextArea.Selection.IsEmpty && !TextArea.Selection.IsMultiline)
        {
            _panelDeBusqueda.SearchPattern = TextArea.Selection.GetText();
        }

        _panelDeBusqueda.IsReplaceMode = conReemplazo;
        _panelDeBusqueda.Open();
    }

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

    private void AlEscribirTexto(object? remitente, TextInputEventArgs argumentos)
    {
        if (_ventanaDeSugerencias is not null || string.IsNullOrEmpty(argumentos.Text))
        {
            return;
        }

        var esPunto = argumentos.Text == ".";
        var completoLasLetrasNecesarias = char.IsLetter(argumentos.Text[^1])
            && LeerPalabraAntesDelCursor().Length == LetrasParaSugerirAutomaticamente;

        if (esPunto || completoLasLetrasNecesarias)
        {
            _ = MostrarSugerenciasAsync();
        }
    }

    // Ctrl+Espacio abre las sugerencias en cualquier momento, como en SSMS y VS Code
    private void AlPresionarTecla(object? remitente, KeyEventArgs argumentos)
    {
        if (argumentos.Key == Key.Space && argumentos.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            argumentos.Handled = true;
            _ = MostrarSugerenciasAsync();
        }
    }

    private async Task MostrarSugerenciasAsync()
    {
        if (ProveedorDeSugerencias is not { } proveedor || Document is null)
        {
            return;
        }

        var posicionAlPedir = CaretOffset;
        var sugerencias = await proveedor.ObtenerSugerenciasAsync(Document.Text, posicionAlPedir);

        // Si mientras llegaban las sugerencias el usuario se movió hacia atrás o ya abrió otra lista, se descartan
        if (sugerencias.Count == 0 || _ventanaDeSugerencias is not null || CaretOffset < posicionAlPedir)
        {
            return;
        }

        var palabraActual = LeerPalabraAntesDelCursor();
        _ventanaDeSugerencias = new CompletionWindow(TextArea)
        {
            StartOffset = CaretOffset - palabraActual.Length,
            CloseWhenCaretAtBeginning = true
        };

        foreach (var sugerencia in sugerencias)
        {
            _ventanaDeSugerencias.CompletionList.CompletionData.Add(new DatoDeSugerencia(sugerencia));
        }

        _ventanaDeSugerencias.Closed += (_, _) => _ventanaDeSugerencias = null;
        _ventanaDeSugerencias.Show();
        _ventanaDeSugerencias.CompletionList.SelectItem(palabraActual);
    }

    private string LeerPalabraAntesDelCursor()
    {
        var inicio = CaretOffset;

        while (inicio > 0 && EsCaracterDeNombre(Document.GetCharAt(inicio - 1)))
        {
            inicio--;
        }

        return Document.GetText(inicio, CaretOffset - inicio);
    }

    private static bool EsCaracterDeNombre(char caracter) => char.IsLetterOrDigit(caracter) || caracter is '_' or '@' or '#';

    private void PublicarSeleccion()
    {
        var segmento = TextArea.Selection.SurroundingSegment;
        Seleccion = segmento is null ? RangoDeTexto.Vacio : new RangoDeTexto(segmento.Offset, segmento.Length);
    }

    // Los colores de sintaxis dependen del fondo: se cambian junto con el tema
    private void AplicarResaltadoDelTema() =>
        SyntaxHighlighting = ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light ? ResaltadoClaro.Value : ResaltadoOscuro.Value;

    private static IHighlightingDefinition CargarDefinicionDeResaltado(string nombreDelRecurso)
    {
        var ensamblado = typeof(EditorSql).Assembly;
        using var flujo = ensamblado.GetManifestResourceStream(nombreDelRecurso)
            ?? throw new InvalidOperationException($"No se encontró el recurso '{nombreDelRecurso}'.");
        using var lector = XmlReader.Create(flujo);

        return HighlightingLoader.Load(lector, HighlightingManager.Instance);
    }
}
