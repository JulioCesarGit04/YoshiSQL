using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeHistorial
{
    Task<IReadOnlyList<ConsultaEjecutada>> CargarAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(IReadOnlyList<ConsultaEjecutada> consultas, CancellationToken tokenDeCancelacion);
}
