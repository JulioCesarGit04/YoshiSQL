namespace YoshiSQL.Dominio.Respaldos;

/// <summary>
/// Archivo de datos o de registro que contiene un respaldo y que hay que ubicar al restaurar.
/// </summary>
public sealed record ArchivoDelRespaldo(string NombreLogico, string RutaOriginal, bool EsDeRegistro);
