namespace YoshiSQL.Infraestructura.Persistencia;

internal static class EscritorDeArchivosSeguro
{
    private const UnixFileMode SoloElUsuario = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Escribe primero en un archivo temporal y luego lo reemplaza, para que un cierre
    /// inesperado nunca deje el archivo original a medio escribir.
    /// </summary>
    /// <remarks>
    /// En Linux el temporal nace ya con permisos 600. Antes se creaba con los permisos por defecto (normalmente 644,
    /// legible por todos) y se restringía después: durante ese instante otro usuario del equipo podía leer la clave
    /// de cifrado o las contraseñas guardadas.
    /// </remarks>
    public static async Task EscribirAsync(string rutaDelArchivo, byte[] contenido, CancellationToken tokenDeCancelacion)
    {
        var rutaTemporal = rutaDelArchivo + ".tmp";

        // Un temporal que quedó de un cierre inesperado conservaría sus permisos viejos al reutilizarlo
        File.Delete(rutaTemporal);

        var opciones = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Options = FileOptions.Asynchronous
        };

        if (!OperatingSystem.IsWindows())
        {
            opciones.UnixCreateMode = SoloElUsuario;
        }

        await using (var archivo = new FileStream(rutaTemporal, opciones))
        {
            await archivo.WriteAsync(contenido, tokenDeCancelacion);
        }

        File.Move(rutaTemporal, rutaDelArchivo, overwrite: true);
    }
}
