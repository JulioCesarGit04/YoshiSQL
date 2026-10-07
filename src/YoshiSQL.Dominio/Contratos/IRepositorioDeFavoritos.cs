using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeFavoritos
{
    Task<IReadOnlyList<ConsultaFavorita>> CargarAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(IReadOnlyList<ConsultaFavorita> favoritos, CancellationToken tokenDeCancelacion);
}
