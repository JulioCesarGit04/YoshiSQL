using Avalonia.Controls;
using Avalonia.Input;
using YoshiSQL.Escritorio.ModelosDeVista.Favoritos;

namespace YoshiSQL.Escritorio.Vistas.Favoritos;

public partial class PanelDeFavoritos : UserControl
{
    public PanelDeFavoritos()
    {
        InitializeComponent();
    }

    private void AlHacerDobleClicEnFavorito(object? remitente, TappedEventArgs argumentos)
    {
        var favorito = (DataContext as FavoritosModeloDeVista)?.Seleccionado;
        favorito?.AbrirCommand.Execute(null);
    }
}
