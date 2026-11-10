namespace YoshiSQL.Dominio.Esquema;

/// <summary>
/// Generador de números (CREATE SEQUENCE). Los valores van como texto porque pueden ser bigint o decimal(38,0).
/// </summary>
/// <param name="SiguienteValor">Valor con el que debe continuar la secuencia al recrearla, para no repetir números ya usados.</param>
/// <param name="TamanoDeCache">Nulo si usa el tamaño predeterminado del servidor.</param>
public sealed record Secuencia(
    string Esquema,
    string Nombre,
    string TipoDeDato,
    string SiguienteValor,
    string Incremento,
    string ValorMinimo,
    string ValorMaximo,
    bool EsCiclica,
    bool UsaCache,
    int? TamanoDeCache);
