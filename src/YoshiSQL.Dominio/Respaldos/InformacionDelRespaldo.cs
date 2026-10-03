namespace YoshiSQL.Dominio.Respaldos;

public sealed record InformacionDelRespaldo(
    string BaseDeDatosOriginal,
    DateTime FechaDelRespaldo,
    IReadOnlyList<ArchivoDelRespaldo> Archivos);
