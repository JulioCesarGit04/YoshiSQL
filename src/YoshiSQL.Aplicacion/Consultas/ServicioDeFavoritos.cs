using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Consultas favoritas del usuario, guardadas con un nombre y conservadas entre sesiones.
/// </summary>
public sealed class ServicioDeFavoritos
{
    private readonly IRepositorioDeFavoritos _repositorio;
    private readonly ILogger<ServicioDeFavoritos> _registro;
    private readonly List<ConsultaFavorita> _favoritos = [];
    private readonly Lock _bloqueo = new();

    public ServicioDeFavoritos(IRepositorioDeFavoritos repositorio, ILogger<ServicioDeFavoritos> registro)
    {
        _repositorio = repositorio;
        _registro = registro;
    }

    public event EventHandler? FavoritosModificados;

    public async Task CargarAsync(CancellationToken tokenDeCancelacion)
    {
        var guardados = await _repositorio.CargarAsync(tokenDeCancelacion);

        lock (_bloqueo)
        {
            _favoritos.Clear();
            _favoritos.AddRange(guardados);
        }

        FavoritosModificados?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Agrega un favorito; si ya existe uno con el mismo nombre, lo reemplaza.</summary>
    public async Task AgregarAsync(string nombre, string sql)
    {
        lock (_bloqueo)
        {
            _favoritos.RemoveAll(favorito => string.Equals(favorito.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
            _favoritos.Insert(0, new ConsultaFavorita(nombre, sql));
        }

        await GuardarYNotificarAsync();
    }

    public async Task EliminarAsync(ConsultaFavorita favorito)
    {
        lock (_bloqueo)
        {
            _favoritos.Remove(favorito);
        }

        await GuardarYNotificarAsync();
    }

    public IReadOnlyList<ConsultaFavorita> ObtenerTodos()
    {
        lock (_bloqueo)
        {
            return _favoritos.ToList();
        }
    }

    private async Task GuardarYNotificarAsync()
    {
        FavoritosModificados?.Invoke(this, EventArgs.Empty);

        try
        {
            await _repositorio.GuardarAsync(ObtenerTodos(), CancellationToken.None);
        }
        catch (Exception error)
        {
            _registro.LogWarning(error, "No se pudieron guardar los favoritos");
        }
    }
}
