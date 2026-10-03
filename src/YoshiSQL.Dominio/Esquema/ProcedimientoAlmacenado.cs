namespace YoshiSQL.Dominio.Esquema;

public sealed record ProcedimientoAlmacenado(string Esquema, string Nombre) : ObjetoDeEsquema(Esquema, Nombre)
{
    public override TipoDeObjeto Tipo => TipoDeObjeto.ProcedimientoAlmacenado;
}
