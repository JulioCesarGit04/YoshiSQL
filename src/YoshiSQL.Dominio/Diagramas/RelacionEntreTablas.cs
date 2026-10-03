using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diagramas;

public sealed record RelacionEntreTablas(LlaveForanea LlaveForanea)
{
    public Tabla TablaOrigen => LlaveForanea.TablaOrigen;

    public Tabla TablaDestino => LlaveForanea.TablaDestino;

    public bool EsAutorreferencia => TablaOrigen == TablaDestino;
}
