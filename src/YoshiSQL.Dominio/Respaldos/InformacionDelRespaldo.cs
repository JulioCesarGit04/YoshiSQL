namespace YoshiSQL.Dominio.Respaldos;

/// <param name="PosicionEnElArchivo">
/// Respaldo elegido dentro del .bak (RESTORE ... WITH FILE = n). Un archivo puede guardar varios respaldos
/// si se hicieron sin INIT; se elige el respaldo completo más reciente.
/// </param>
/// <param name="CantidadDeRespaldosEnElArchivo">Cuántos respaldos contiene el archivo, para avisarlo al usuario.</param>
public sealed record InformacionDelRespaldo(
    string BaseDeDatosOriginal,
    DateTime FechaDelRespaldo,
    IReadOnlyList<ArchivoDelRespaldo> Archivos,
    int PosicionEnElArchivo = 1,
    int CantidadDeRespaldosEnElArchivo = 1);
