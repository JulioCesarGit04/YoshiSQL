namespace YoshiSQL.Dominio.Respaldos;

/// <param name="Reemplazar">Sobrescribe la base de datos si ya existe (REPLACE).</param>
/// <param name="CerrarConexionesExistentes">Desconecta a los demás usuarios antes de restaurar.</param>
/// <param name="CarpetaDeDatos">Carpeta del servidor donde quedarán los archivos restaurados.</param>
public sealed record OpcionesDeRestauracion(
    string RutaDelArchivo,
    string BaseDeDatosDestino,
    bool Reemplazar,
    bool CerrarConexionesExistentes,
    IReadOnlyList<ArchivoDelRespaldo> Archivos,
    string CarpetaDeDatos,
    string CarpetaDeRegistros);
