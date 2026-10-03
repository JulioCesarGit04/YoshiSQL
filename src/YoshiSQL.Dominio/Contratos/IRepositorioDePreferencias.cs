using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDePreferencias
{
    Task<PreferenciasDelUsuario> CargarAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(PreferenciasDelUsuario preferencias, CancellationToken tokenDeCancelacion);
}
