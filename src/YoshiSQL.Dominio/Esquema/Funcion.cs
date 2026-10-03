namespace YoshiSQL.Dominio.Esquema;

public sealed record Funcion(string Esquema, string Nombre) : ObjetoDeEsquema(Esquema, Nombre)
{
    public override TipoDeObjeto Tipo => TipoDeObjeto.Funcion;
}
