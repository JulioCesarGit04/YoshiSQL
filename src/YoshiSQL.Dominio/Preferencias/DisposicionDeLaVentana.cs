namespace YoshiSQL.Dominio.Preferencias;

/// <summary>
/// Cómo dejó el usuario la ventana principal (tamaño y paneles), para abrirla igual la próxima vez.
/// </summary>
public sealed record DisposicionDeLaVentana
{
    public const double AnchoMinimoDeLaVentana = 900;
    public const double AltoMinimoDeLaVentana = 560;
    public const double AnchoMinimoDelExplorador = 180;
    public const double AnchoMaximoDelExplorador = 900;
    public const double AltoMinimoDeResultados = 80;
    public const double AltoMaximoDeResultados = 1600;

    public static readonly DisposicionDeLaVentana Predeterminada = new();

    public double Ancho { get; init; } = 1360;

    public double Alto { get; init; } = 820;

    public bool EstaMaximizada { get; init; }

    public double AnchoDelExplorador { get; init; } = 320;

    public double AltoDeResultados { get; init; } = 280;

    public bool ExploradorVisible { get; init; } = true;

    public bool ResultadosVisibles { get; init; } = true;

    /// <summary>
    /// Tamaño con que quedó cada diálogo que se puede agrandar (texto, respaldo, restauración), por su nombre.
    /// </summary>
    public IReadOnlyDictionary<string, TamanoDeVentana> Dialogos { get; init; } = new Dictionary<string, TamanoDeVentana>();

    /// <summary>
    /// Corrige valores imposibles (archivo editado a mano, o guardado en un monitor más grande que el actual).
    /// </summary>
    public DisposicionDeLaVentana Normalizar() => this with
    {
        Ancho = double.IsFinite(Ancho) ? Math.Max(Ancho, AnchoMinimoDeLaVentana) : Predeterminada.Ancho,
        Alto = double.IsFinite(Alto) ? Math.Max(Alto, AltoMinimoDeLaVentana) : Predeterminada.Alto,
        AnchoDelExplorador = double.IsFinite(AnchoDelExplorador)
            ? Math.Clamp(AnchoDelExplorador, AnchoMinimoDelExplorador, AnchoMaximoDelExplorador)
            : Predeterminada.AnchoDelExplorador,
        AltoDeResultados = double.IsFinite(AltoDeResultados)
            ? Math.Clamp(AltoDeResultados, AltoMinimoDeResultados, AltoMaximoDeResultados)
            : Predeterminada.AltoDeResultados
    };
}
