namespace YoshiSQL.Dominio.Preferencias;

public sealed record PreferenciasDelUsuario
{
    public const double TamanoDeLetraMinimo = 9;
    public const double TamanoDeLetraMaximo = 28;
    public const string FuentePredeterminada = "Cascadia Code, JetBrains Mono, Ubuntu Mono, DejaVu Sans Mono, monospace";

    public static readonly PreferenciasDelUsuario Predeterminadas = new();

    public TemaVisual Tema { get; init; } = TemaVisual.Oscuro;

    public string FuenteDelEditor { get; init; } = FuentePredeterminada;

    public double TamanoDeLetraDelEditor { get; init; } = 14;

    public bool MostrarNumerosDeLinea { get; init; } = true;

    public bool AjustarLineasLargas { get; init; }

    /// <summary>
    /// Corrige valores fuera de rango (por ejemplo si se editó el archivo a mano).
    /// </summary>
    public PreferenciasDelUsuario Normalizar() => this with
    {
        TamanoDeLetraDelEditor = Math.Clamp(TamanoDeLetraDelEditor, TamanoDeLetraMinimo, TamanoDeLetraMaximo),
        FuenteDelEditor = string.IsNullOrWhiteSpace(FuenteDelEditor) ? FuentePredeterminada : FuenteDelEditor.Trim(),
        Tema = Enum.IsDefined(Tema) ? Tema : TemaVisual.Oscuro
    };
}
