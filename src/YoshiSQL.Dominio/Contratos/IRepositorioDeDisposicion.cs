using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeDisposicion
{
    Task<DisposicionDeLaVentana> CargarAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(DisposicionDeLaVentana disposicion, CancellationToken tokenDeCancelacion);
}
