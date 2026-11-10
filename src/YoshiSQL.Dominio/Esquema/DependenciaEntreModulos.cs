namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Una vista o función que usa a otra: la requerida debe crearse antes que la dependiente.
/// </summary>
public sealed record DependenciaEntreModulos(
    string EsquemaDelDependiente,
    string NombreDelDependiente,
    string EsquemaDelRequerido,
    string NombreDelRequerido);
