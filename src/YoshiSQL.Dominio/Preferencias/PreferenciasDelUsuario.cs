namespace YoshiSQL.Dominio.Preferencias;

public sealed record PreferenciasDelUsuario
{
    public const double TamanoDeLetraMinimo = 9;
    public const double TamanoDeLetraMaximo = 28;
    public const int FilasMinimas = 1;
    public const int FilasMaximas = 100000;
    public const string FuentePredeterminada = "Cascadia Code, Consolas, JetBrains Mono, Ubuntu Mono, DejaVu Sans Mono, monospace";

    public static readonly PreferenciasDelUsuario Predeterminadas = new();

    public TemaVisual Tema { get; init; } = TemaVisual.Oscuro;

    public string FuenteDelEditor { get; init; } = FuentePredeterminada;

    public double TamanoDeLetraDelEditor { get; init; } = 14;

    public bool MostrarNumerosDeLinea { get; init; } = true;

    public bool AjustarLineasLargas { get; init; }

    /// <summary>
    /// Filas que trae "Seleccionar las primeras N filas" (TOP del SELECT generado).
    /// </summary>
    public int FilasAlSeleccionar { get; init; } = 1000;

    /// <summary>
    /// Filas que trae "Editar las primeras N filas" en la grilla editable.
    /// </summary>
    public int FilasAlEditar { get; init; } = 200;

    /// <summary>
    /// Corrige valores fuera de rango (por ejemplo si se editó el archivo a mano).
    /// </summary>
    public PreferenciasDelUsuario Normalizar() => this with
    {
        TamanoDeLetraDelEditor = Math.Clamp(TamanoDeLetraDelEditor, TamanoDeLetraMinimo, TamanoDeLetraMaximo),
        FuenteDelEditor = string.IsNullOrWhiteSpace(FuenteDelEditor) ? FuentePredeterminada : FuenteDelEditor.Trim(),
        Tema = Enum.IsDefined(Tema) ? Tema : TemaVisual.Oscuro,
        FilasAlSeleccionar = Math.Clamp(FilasAlSeleccionar, FilasMinimas, FilasMaximas),
        FilasAlEditar = Math.Clamp(FilasAlEditar, FilasMinimas, FilasMaximas)
    };
}
