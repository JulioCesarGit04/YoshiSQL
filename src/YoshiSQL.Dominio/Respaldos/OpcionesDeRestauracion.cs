namespace YoshiSQL.Dominio.Respaldos;

/// <param name="Reemplazar">Sobrescribe la base de datos si ya existe (REPLACE).</param>
/// <param name="CerrarConexionesExistentes">Desconecta a los demás usuarios antes de restaurar.</param>
/// <param name="CarpetaDeDatos">Carpeta del servidor donde quedarán los archivos restaurados.</param>
/// <param name="PosicionEnElArchivo">Respaldo dentro del .bak que se restaura (WITH FILE = n).</param>
public sealed record OpcionesDeRestauracion(
    string RutaDelArchivo,
    string BaseDeDatosDestino,
    bool Reemplazar,
    bool CerrarConexionesExistentes,
    IReadOnlyList<ArchivoDelRespaldo> Archivos,
    string CarpetaDeDatos,
    string CarpetaDeRegistros,
    int PosicionEnElArchivo = 1);
