using YoshiSQL.Dominio.Sesion;

namespace YoshiSQL.Aplicacion.Sesion;

public sealed record SesionAnterior(EstadoDeLaSesion Estado, bool SeCerroIncorrectamente)
{
    public int CantidadDeScriptsSinGuardar => Estado.Pestanas.Count(pestana => pestana.TieneCambiosSinGuardar);
}
