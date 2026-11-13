namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Dónde apareció el texto buscado. Los valores coinciden con los que devuelve la consulta de catálogo.
/// </summary>
public enum TipoDeCoincidencia
{
    EnElNombre = 1,
    EnUnaColumna = 2,
    EnElCodigo = 3
}
