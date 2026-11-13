using System.Runtime.CompilerServices;
using System.Xml;
using Avalonia;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
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

    public static readonly StyledProperty<string?> PosicionDelCursorProperty =
        AvaloniaProperty.Register<EditorSql, string?>(
            nameof(PosicionDelCursor),
            defaultBindingMode: BindingMode.OneWayToSource);

    public static readonly StyledProperty<bool> SubrayarErroresDeSintaxisProperty =
        AvaloniaProperty.Register<EditorSql, bool>(nameof(SubrayarErroresDeSintaxis), defaultValue: true);

    // Se espera a que el usuario deje de escribir: analizar en cada tecla sería un desperdicio
    private static readonly TimeSpan EsperaAntesDeAnalizar = TimeSpan.FromMilliseconds(700);

    // Un script enorme tarda en analizarse y casi nunca se está escribiendo a mano
    private const int LargoMaximoParaAnalizar = 300_000;

    private static readonly Lazy<IHighlightingDefinition> ResaltadoOscuro = new(() => CargarDefinicionDeResaltado(RecursoDeResaltadoOscuro));
    private static readonly Lazy<IHighlightingDefinition> ResaltadoClaro = new(() => CargarDefinicionDeResaltado(RecursoDeResaltadoClaro));

    private readonly SearchPanel _panelDeBusqueda;

    // La misma vista se reutiliza para todas las pestañas de consulta: sin recordar el estado de cada documento,
    // al volver a una pestaña el cursor y el desplazamiento quedaban al inicio y se perdía la selección
    private readonly ConditionalWeakTable<TextDocument, EstadoDelDocumento> _estadosPorDocumento = new();
    private TextDocument? _documentoMostrado;
    private bool _estaRestaurando;
    private CompletionWindow? _ventanaDeSugerencias;
    private readonly SubrayadoDeErrores _subrayadoDeErrores = new();
    private readonly DispatcherTimer _temporizadorDeAnalisis;

    public EditorSql()
    {
        _temporizadorDeAnalisis = new DispatcherTimer { Interval = EsperaAntesDeAnalizar };
        _temporizadorDeAnalisis.Tick += async (_, _) => await AnalizarSintaxisAsync();
        TextArea.TextView.BackgroundRenderers.Add(_subrayadoDeErrores);
        TextChanged += (_, _) => ProgramarAnalisisDeSintaxis();

        AplicarResaltadoDelTema();
        ActualThemeVariantChanged += (_, _) => AplicarResaltadoDelTema();
        ShowLineNumbers = true;
        Options.ConvertTabsToSpaces = true;
        Options.IndentationSize = 4;
        Options.HighlightCurrentLine = true;
        Options.EnableHyperlinks = false;
        Options.EnableEmailHyperlinks = false;

        TextArea.SelectionChanged += (_, _) =>
        {
            PublicarSeleccion();
            RecordarEstadoDelDocumento();
        };
        TextArea.Caret.PositionChanged += (_, _) =>
        {
            RecordarEstadoDelDocumento();
            PublicarPosicionDelCursor();
        };
        TextArea.TextView.ScrollOffsetChanged += (_, _) => RecordarEstadoDelDocumento();
        DocumentChanged += (_, _) => AlCambiarDeDocumento();

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

    public bool SubrayarErroresDeSintaxis
    {
        get => GetValue(SubrayarErroresDeSintaxisProperty);
        set => SetValue(SubrayarErroresDeSintaxisProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs cambio)
    {
        base.OnPropertyChanged(cambio);

        if (cambio.Property == SubrayarErroresDeSintaxisProperty)
        {
            ProgramarAnalisisDeSintaxis();
        }
    }

    private void ProgramarAnalisisDeSintaxis()
    {
        // Lo subrayado corresponde al texto anterior: se quita ya y se vuelve a calcular cuando el usuario pare
        MostrarErroresDeSintaxis([]);
        _temporizadorDeAnalisis.Stop();
        _temporizadorDeAnalisis.Start();
    }

    private async Task AnalizarSintaxisAsync()
    {
        _temporizadorDeAnalisis.Stop();

        if (!SubrayarErroresDeSintaxis || ProveedorDeSugerencias is not { } proveedor || Document is not { } documento
            || documento.TextLength == 0 || documento.TextLength > LargoMaximoParaAnalizar)
        {
            return;
        }

        var texto = documento.Text;
        var errores = await Task.Run(() => proveedor.BuscarErroresDeSintaxis(texto));

        // Si mientras tanto cambió el texto o la pestaña, este resultado ya no corresponde a lo que se ve
        if (!ReferenceEquals(Document, documento) || documento.TextLength != texto.Length || documento.Text != texto)
        {
            return;
        }

        MostrarErroresDeSintaxis(errores.Select(error => CalcularTramoDelError(texto, error.Posicion)).ToList());
    }

    private void MostrarErroresDeSintaxis(IReadOnlyList<(int Inicio, int Longitud)> tramos)
    {
        if (tramos.Count == 0 && _subrayadoDeErrores.Tramos.Count == 0)
        {
            return;
        }

        _subrayadoDeErrores.Tramos = tramos;
        TextArea.TextView.InvalidateLayer(_subrayadoDeErrores.Layer);
    }

    /// <summary>
    /// El analizador solo dice dónde empieza el error: se subraya la palabra que está ahí
    /// (o un carácter, si no es una palabra), sin pasar del final del texto.
    /// </summary>
    private static (int Inicio, int Longitud) CalcularTramoDelError(string texto, int posicion)
    {
        if (texto.Length == 0)
        {
            return (0, 0);
        }

        var inicio = Math.Clamp(posicion, 0, texto.Length - 1);
        var fin = inicio;

        while (fin < texto.Length && EsCaracterDeNombre(texto[fin]))
        {
            fin++;
        }

        return (inicio, Math.Max(fin - inicio, 1));
    }

    /// <summary>Línea y columna del cursor ("Lín 12, Col 5"), para mostrarlas en la barra de estado.</summary>
    public string? PosicionDelCursor
    {
        get => GetValue(PosicionDelCursorProperty);
        set => SetValue(PosicionDelCursorProperty, value);
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

    /// <summary>Comenta con "-- " las líneas de la selección.</summary>
    public void Comentar() => AplicarComentario(comentar: true);

    /// <summary>Quita el "--" del comienzo de las líneas comentadas de la selección.</summary>
    public void Descomentar() => AplicarComentario(comentar: false);

    /// <summary>
    /// Comenta las líneas de la selección, o las descomenta si ya lo estaban todas (Ctrl+/).
    /// </summary>
    public void AlternarComentario()
    {
        var (lineaInicial, lineaFinal) = LineasDeLaSeleccion();
        AplicarComentario(comentar: !TodasLasLineasEstanComentadas(lineaInicial, lineaFinal));
    }

    // Un solo paso de deshacer (Ctrl+Z); se recorre de la última línea a la primera para no correr los offsets
    private void AplicarComentario(bool comentar)
    {
        if (Document is null)
        {
            return;
        }

        var (lineaInicial, lineaFinal) = LineasDeLaSeleccion();

        Document.BeginUpdate();
        try
        {
            for (var numero = lineaFinal; numero >= lineaInicial; numero--)
            {
                if (comentar)
                {
                    AgregarComentarioALinea(numero);
                }
                else
                {
                    QuitarComentarioDeLinea(numero);
                }
            }
        }
        finally
        {
            Document.EndUpdate();
        }
    }

    /// <summary>
    /// Convierte el texto subrayado a mayúsculas o minúsculas, conservando la selección.
    /// </summary>
    public void ConvertirSeleccion(bool aMayusculas)
    {
        var segmento = TextArea.Selection.SurroundingSegment;

        if (Document is null || segmento is null || segmento.Length == 0)
        {
            return;
        }

        var texto = Document.GetText(segmento.Offset, segmento.Length);
        var convertido = aMayusculas ? texto.ToUpperInvariant() : texto.ToLowerInvariant();

        if (convertido == texto)
        {
            return;
        }

        Document.Replace(segmento.Offset, segmento.Length, convertido);
        Select(segmento.Offset, convertido.Length);
    }

    private (int Inicial, int Final) LineasDeLaSeleccion()
    {
        var segmento = TextArea.Selection.SurroundingSegment;

        if (segmento is null)
        {
            var linea = Document.GetLineByOffset(CaretOffset).LineNumber;
            return (linea, linea);
        }

        var lineaInicial = Document.GetLineByOffset(segmento.Offset).LineNumber;
        var lineaFinal = Document.GetLineByOffset(segmento.Offset + segmento.Length);

        // Si la selección termina justo al inicio de una línea, esa línea no se incluye
        if (lineaFinal.Offset == segmento.Offset + segmento.Length && lineaFinal.LineNumber > lineaInicial)
        {
            return (lineaInicial, lineaFinal.LineNumber - 1);
        }

        return (lineaInicial, lineaFinal.LineNumber);
    }

    private bool TodasLasLineasEstanComentadas(int lineaInicial, int lineaFinal)
    {
        for (var numero = lineaInicial; numero <= lineaFinal; numero++)
        {
            var linea = Document.GetLineByNumber(numero);
            var texto = Document.GetText(linea.Offset, linea.Length).TrimStart();

            if (texto.Length > 0 && !texto.StartsWith("--", StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private void AgregarComentarioALinea(int numero)
    {
        var linea = Document.GetLineByNumber(numero);
        var texto = Document.GetText(linea.Offset, linea.Length);

        if (texto.Trim().Length == 0)
        {
            return;
        }

        var columna = texto.Length - texto.TrimStart().Length;
        Document.Insert(linea.Offset + columna, "-- ");
    }

    private void QuitarComentarioDeLinea(int numero)
    {
        var linea = Document.GetLineByNumber(numero);
        var texto = Document.GetText(linea.Offset, linea.Length);
        var posicion = texto.IndexOf("--", StringComparison.Ordinal);

        if (posicion < 0 || texto[..posicion].Trim().Length != 0)
        {
            return;
        }

        var cantidad = posicion + 2 < texto.Length && texto[posicion + 2] == ' ' ? 3 : 2;
        Document.Remove(linea.Offset + posicion, cantidad);
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
            return;
        }

        // Tab después de una abreviatura (ej. "ssf") la reemplaza por su fragmento de código.
        // Con la lista de sugerencias abierta, Tab sigue sirviendo para aceptar la sugerencia.
        var esTabSolo = argumentos.Key == Key.Tab && argumentos.KeyModifiers == KeyModifiers.None;

        if (esTabSolo && _ventanaDeSugerencias is null && TextArea.Selection.IsEmpty && InsertarFragmento())
        {
            argumentos.Handled = true;
        }
    }

    /// <returns>Verdadero si la palabra antes del cursor era la abreviatura de un fragmento y se insertó.</returns>
    private bool InsertarFragmento()
    {
        if (Document is null)
        {
            return false;
        }

        var abreviatura = LeerPalabraAntesDelCursor();

        if (abreviatura.Length == 0 || FragmentosDeTSql.Buscar(abreviatura) is not { } fragmento)
        {
            return false;
        }

        var inicio = CaretOffset - abreviatura.Length;
        var texto = fragmento.Texto.ReplaceLineEndings(Environment.NewLine);
        var posicionDeLaMarca = texto.IndexOf(FragmentosDeTSql.MarcaDelCursor, StringComparison.Ordinal);
        var textoFinal = texto.Replace(FragmentosDeTSql.MarcaDelCursor, string.Empty, StringComparison.Ordinal);

        Document.Replace(inicio, abreviatura.Length, textoFinal);
        CaretOffset = inicio + (posicionDeLaMarca >= 0 ? posicionDeLaMarca : textoFinal.Length);
        return true;
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

    private void PublicarPosicionDelCursor() =>
        PosicionDelCursor = $"Lín {TextArea.Caret.Line}, Col {TextArea.Caret.Column}";

    private void PublicarSeleccion()
    {
        var segmento = TextArea.Selection.SurroundingSegment;
        Seleccion = segmento is null ? RangoDeTexto.Vacio : new RangoDeTexto(segmento.Offset, segmento.Length, Document);
    }

    /// <summary>
    /// Guarda cursor, selección y desplazamiento del documento visible. Mientras se cambia de documento
    /// no se guarda nada: los eventos de ese momento traen el estado reiniciado, no el del usuario.
    /// </summary>
    private void RecordarEstadoDelDocumento()
    {
        if (_documentoMostrado is null || !ReferenceEquals(Document, _documentoMostrado) || _estaRestaurando)
        {
            return;
        }

        _estadosPorDocumento.AddOrUpdate(
            _documentoMostrado,
            new EstadoDelDocumento(CaretOffset, SelectionStart, SelectionLength, VerticalOffset, HorizontalOffset));
    }

    private void AlCambiarDeDocumento()
    {
        _documentoMostrado = Document;
        PublicarSeleccion();
        PublicarPosicionDelCursor();
        ProgramarAnalisisDeSintaxis();

        if (Document is not { } documento || !_estadosPorDocumento.TryGetValue(documento, out var estado))
        {
            return;
        }

        // Se restaura cuando la vista ya midió el nuevo documento; antes, el desplazamiento no tendría efecto
        _estaRestaurando = true;
        Dispatcher.UIThread.Post(() => RestaurarEstado(documento, estado), DispatcherPriority.Background);
    }

    private void RestaurarEstado(TextDocument documento, EstadoDelDocumento estado)
    {
        try
        {
            // El usuario pudo cambiar otra vez de pestaña antes de que llegara este momento
            if (!ReferenceEquals(Document, documento))
            {
                return;
            }

            var largo = documento.TextLength;
            var inicioDeSeleccion = Math.Min(estado.InicioDeSeleccion, largo);
            Select(inicioDeSeleccion, Math.Min(estado.LargoDeSeleccion, largo - inicioDeSeleccion));
            CaretOffset = Math.Min(estado.PosicionDelCursor, largo);
            ScrollToVerticalOffset(estado.DesplazamientoVertical);
            ScrollToHorizontalOffset(estado.DesplazamientoHorizontal);
        }
        finally
        {
            _estaRestaurando = false;
            PublicarSeleccion();
        }
    }

    private sealed record EstadoDelDocumento(
        int PosicionDelCursor,
        int InicioDeSeleccion,
        int LargoDeSeleccion,
        double DesplazamientoVertical,
        double DesplazamientoHorizontal);

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
