namespace YoshiSQL.Dominio.Esquema;

public sealed record Vista(string Esquema, string Nombre) : ObjetoDeEsquema(Esquema, Nombre)
{
    public override TipoDeObjeto Tipo => TipoDeObjeto.Vista;
}
