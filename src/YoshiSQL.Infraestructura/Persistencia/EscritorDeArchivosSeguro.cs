namespace YoshiSQL.Infraestructura.Persistencia;

internal static class EscritorDeArchivosSeguro
{
    private const UnixFileMode SoloElUsuario = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Escribe primero en un archivo temporal y luego lo reemplaza, para que un cierre
    /// inesperado nunca deje el archivo original a medio escribir.
    /// </summary>
    public static async Task EscribirAsync(string rutaDelArchivo, byte[] contenido, CancellationToken tokenDeCancelacion)
    {
        var rutaTemporal = rutaDelArchivo + ".tmp";

        await File.WriteAllBytesAsync(rutaTemporal, contenido, tokenDeCancelacion);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(rutaTemporal, SoloElUsuario);
        }

        File.Move(rutaTemporal, rutaDelArchivo, overwrite: true);
    }
}
