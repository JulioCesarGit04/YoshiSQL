namespace YoshiSQL.Dominio.Sesion;

public sealed record EstadoDeLaSesion(IReadOnlyList<PestanaGuardada> Pestanas, int IndiceDeLaPestanaSeleccionada)
{
    public static readonly EstadoDeLaSesion Vacio = new([], IndiceDeLaPestanaSeleccionada: -1);

    public bool EstaVacio => Pestanas.Count == 0;
}
