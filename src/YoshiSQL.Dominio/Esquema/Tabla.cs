namespace YoshiSQL.Dominio.Esquema;

public sealed record Tabla(string Esquema, string Nombre) : ObjetoDeEsquema(Esquema, Nombre)
{
    public override TipoDeObjeto Tipo => TipoDeObjeto.Tabla;
}
