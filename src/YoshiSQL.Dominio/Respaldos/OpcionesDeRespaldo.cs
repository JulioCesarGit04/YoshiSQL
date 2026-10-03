namespace YoshiSQL.Dominio.Respaldos;

/// <param name="RutaDelArchivo">Ruta en el servidor donde SQL Server escribirá el archivo .bak.</param>
/// <param name="SoloCopia">No altera la cadena de respaldos (COPY_ONLY); ideal para copias manuales.</param>
/// <param name="Verificar">Comprueba que el archivo se pueda restaurar (RESTORE VERIFYONLY).</param>
public sealed record OpcionesDeRespaldo(
    string BaseDeDatos,
    string RutaDelArchivo,
    bool SoloCopia,
    bool Comprimir,
    bool Verificar);
