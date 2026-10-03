using YoshiSQL.Dominio.Conexiones;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeConexiones
{
    Task<IReadOnlyList<PerfilDeConexion>> ObtenerTodosAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(PerfilDeConexion perfil, CancellationToken tokenDeCancelacion);

    Task EliminarAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion);
}
