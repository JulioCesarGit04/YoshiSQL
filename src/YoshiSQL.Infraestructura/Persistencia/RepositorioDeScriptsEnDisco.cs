using System.Text;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

public sealed class RepositorioDeScriptsEnDisco : IRepositorioDeScripts
{
    private static readonly Encoding CodificacionUtf8SinBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public Task<string> LeerAsync(string rutaDelArchivo, CancellationToken tokenDeCancelacion) =>
        File.ReadAllTextAsync(rutaDelArchivo, tokenDeCancelacion);

    public Task GuardarAsync(string rutaDelArchivo, string contenido, CancellationToken tokenDeCancelacion) =>
        File.WriteAllTextAsync(rutaDelArchivo, contenido, CodificacionUtf8SinBom, tokenDeCancelacion);
}
