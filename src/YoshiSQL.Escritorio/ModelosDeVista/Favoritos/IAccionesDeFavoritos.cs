using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Favoritos;

public interface IAccionesDeFavoritos
{
    /// <summary>Abre el favorito en una pestaña nueva, en el servidor seleccionado.</summary>
    Task AbrirFavoritoAsync(ConsultaFavorita favorito);
}
