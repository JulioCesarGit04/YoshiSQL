namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Botón de un diálogo de mensaje y el valor que devuelve al pulsarlo.
/// </summary>
public sealed record OpcionDeDialogo(string Texto, object Valor, bool EsPrincipal = false, bool EsCancelar = false);
