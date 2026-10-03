using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Scripts;

public sealed class ServicioDeArchivosSql
{
    public const string ExtensionDeScripts = ".sql";

    private readonly IRepositorioDeScripts _repositorioDeScripts;

    public ServicioDeArchivosSql(IRepositorioDeScripts repositorioDeScripts)
    {
        _repositorioDeScripts = repositorioDeScripts;
    }

    public Task<string> AbrirAsync(string rutaDelArchivo, CancellationToken tokenDeCancelacion) =>
        _repositorioDeScripts.LeerAsync(rutaDelArchivo, tokenDeCancelacion);

    /// <returns>La ruta final del archivo, con la extensión .sql asegurada.</returns>
    public async Task<string> GuardarAsync(string rutaDelArchivo, string contenido, CancellationToken tokenDeCancelacion)
    {
        var rutaConExtension = AsegurarExtensionSql(rutaDelArchivo);
        await _repositorioDeScripts.GuardarAsync(rutaConExtension, contenido, tokenDeCancelacion);
        return rutaConExtension;
    }

    private static string AsegurarExtensionSql(string rutaDelArchivo) =>
        Path.HasExtension(rutaDelArchivo) ? rutaDelArchivo : rutaDelArchivo + ExtensionDeScripts;
}
