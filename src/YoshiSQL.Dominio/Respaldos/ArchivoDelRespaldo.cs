namespace YoshiSQL.Dominio.Respaldos;

/// <summary>
/// Archivo de datos o de registro que contiene un respaldo y que hay que ubicar al restaurar.
/// </summary>
/// <param name="EsCarpeta">Verdadero para FILESTREAM y tablas en memoria, que se guardan en una carpeta y no en un archivo.</param>
public sealed record ArchivoDelRespaldo(string NombreLogico, string RutaOriginal, bool EsDeRegistro, bool EsCarpeta = false);
