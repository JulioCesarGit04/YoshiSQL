namespace YoshiSQL.Aplicacion.Errores;

/// <summary>
/// Resultado de registrar un error: lo que se le muestra al usuario y cómo encontrarlo en el registro.
/// </summary>
/// <param name="Codigo">Código para buscar el error en el registro; nulo en errores esperados.</param>
/// <param name="EsInesperado">Verdadero si es una falla de YoshiSQL y no, por ejemplo, un servidor apagado.</param>
public sealed record ErrorRegistrado(
    string? Codigo,
    string MensajeParaElUsuario,
    bool EsInesperado,
    string DetalleTecnico);
