namespace YoshiSQL.Escritorio.ModelosDeVista.Conexiones;

/// <summary>
/// Opción de color para marcar una conexión en la barra de estado.
/// </summary>
public sealed record OpcionDeColor(string Nombre, string? Hex)
{
    public static readonly IReadOnlyList<OpcionDeColor> Todas =
    [
        new("Sin color", null),
        new("Rojo (producción)", "#E5484D"),
        new("Ámbar", "#F5A623"),
        new("Verde", "#30A46C"),
        new("Azul", "#3B82F6"),
        new("Violeta", "#8B5CF6")
    ];
}
