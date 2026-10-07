using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Favoritos;

/// <summary>
/// Una fila del panel de favoritos: el nombre y una vista previa del SQL.
/// </summary>
public sealed partial class FavoritoModeloDeVista : ModeloDeVistaBase
{
    private const int LargoMaximoDeLaVistaPrevia = 1500;

    private readonly FavoritosModeloDeVista _panel;

    public FavoritoModeloDeVista(ConsultaFavorita favorito, FavoritosModeloDeVista panel)
    {
        Favorito = favorito;
        _panel = panel;
    }

    public ConsultaFavorita Favorito { get; }

    public string Nombre => Favorito.Nombre;

    public string VistaPrevia => Favorito.Sql.Length > LargoMaximoDeLaVistaPrevia
        ? Favorito.Sql[..LargoMaximoDeLaVistaPrevia] + "..."
        : Favorito.Sql;

    [RelayCommand]
    private Task AbrirAsync() => _panel.AbrirAsync(this);

    [RelayCommand]
    private Task EliminarAsync() => _panel.EliminarAsync(this);
}
