using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Favoritos;

/// <summary>
/// Panel con las consultas favoritas del usuario, con búsqueda por nombre o texto.
/// </summary>
public sealed partial class FavoritosModeloDeVista : ModeloDeVistaBase
{
    private readonly ServicioDeFavoritos _servicioDeFavoritos;
    private readonly IAccionesDeFavoritos _acciones;
    private readonly IServicioDeErrores _servicioDeErrores;

    public FavoritosModeloDeVista(
        ServicioDeFavoritos servicioDeFavoritos,
        IAccionesDeFavoritos acciones,
        IServicioDeErrores servicioDeErrores)
    {
        _servicioDeFavoritos = servicioDeFavoritos;
        _acciones = acciones;
        _servicioDeErrores = servicioDeErrores;

        _servicioDeFavoritos.FavoritosModificados += (_, _) => ActualizarEntradas();
    }

    public ObservableCollection<FavoritoModeloDeVista> Favoritos { get; } = [];

    public bool EstaVacio => Favoritos.Count == 0;

    [ObservableProperty]
    public partial string Filtro { get; set; } = string.Empty;

    [ObservableProperty]
    public partial FavoritoModeloDeVista? Seleccionado { get; set; }

    partial void OnFiltroChanged(string value) => ActualizarEntradas();

    public async Task AbrirAsync(FavoritoModeloDeVista entrada)
    {
        try
        {
            await _acciones.AbrirFavoritoAsync(entrada.Favorito);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir favorito"));
        }
    }

    public Task EliminarAsync(FavoritoModeloDeVista entrada) => _servicioDeFavoritos.EliminarAsync(entrada.Favorito);

    private void ActualizarEntradas()
    {
        var filtro = Filtro.Trim();

        var visibles = _servicioDeFavoritos.ObtenerTodos()
            .Where(favorito => filtro.Length == 0
                || favorito.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || favorito.Sql.Contains(filtro, StringComparison.OrdinalIgnoreCase));

        Favoritos.Clear();

        foreach (var favorito in visibles)
        {
            Favoritos.Add(new FavoritoModeloDeVista(favorito, this));
        }

        OnPropertyChanged(nameof(EstaVacio));
    }
}
