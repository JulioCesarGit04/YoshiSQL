using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Dominio.Contratos;

public interface IAnalizadorDePlanes
{
    PlanDeEjecucion Interpretar(IReadOnlyList<string> documentosXml, bool esReal);
}
