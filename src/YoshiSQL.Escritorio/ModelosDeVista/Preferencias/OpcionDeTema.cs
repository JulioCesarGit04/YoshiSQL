using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Escritorio.ModelosDeVista.Preferencias;

public sealed record OpcionDeTema(TemaVisual Tema, string Descripcion)
{
    public static readonly IReadOnlyList<OpcionDeTema> Todas =
    [
        new(TemaVisual.Oscuro, "Oscuro"),
        new(TemaVisual.Claro, "Claro"),
        new(TemaVisual.Sistema, "Igual que el sistema")
    ];
}
