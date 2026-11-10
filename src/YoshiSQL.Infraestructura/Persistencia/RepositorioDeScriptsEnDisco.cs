using System.Text;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

public sealed class RepositorioDeScriptsEnDisco : IRepositorioDeScripts
{
    private const char UltimoCaracterAscii = '\u007F';

    private static readonly Encoding CodificacionUtf8SinBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Encoding CodificacionUtf8ConBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>
    /// Lee el archivo en la codificación que realmente tiene. Leerlo siempre como UTF-8 convertía en "�" las tildes
    /// y eñes de los scripts guardados en ANSI (lo habitual en SSMS y en editores de Windows): un procedimiento con
    /// una columna [Descripción] o un texto 'Año' dejaba de funcionar o guardaba datos corruptos.
    /// </summary>
    public async Task<string> LeerAsync(string rutaDelArchivo, CancellationToken tokenDeCancelacion)
    {
        var bytes = await File.ReadAllBytesAsync(rutaDelArchivo, tokenDeCancelacion);
        return DetectorDeCodificacion.Decodificar(bytes);
    }

    /// <summary>
    /// Un script solo con caracteres ASCII se guarda sin BOM (idéntico en cualquier herramienta). Si tiene tildes o eñes
    /// se guarda con BOM: sin él, sqlcmd y SSMS lo leen como ANSI y "año" se convierte en "aÃ±o".
    /// </summary>
    public Task GuardarAsync(string rutaDelArchivo, string contenido, CancellationToken tokenDeCancelacion)
    {
        var codificacion = contenido.Any(caracter => caracter > UltimoCaracterAscii) ? CodificacionUtf8ConBom : CodificacionUtf8SinBom;
        return File.WriteAllTextAsync(rutaDelArchivo, contenido, codificacion, tokenDeCancelacion);
    }
}
