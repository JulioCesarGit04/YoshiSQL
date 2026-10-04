using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Seguridad;

/// <summary>
/// Guarda las contraseñas cifradas con AES-GCM. La clave se genera al azar la primera vez y se
/// protege según el sistema: con DPAPI en Windows y con permisos 600 en Linux.
/// Protege contra quien vea el archivo de credenciales, pero no contra alguien con acceso
/// total a la cuenta del usuario.
/// </summary>
public sealed class AlmacenDeCredencialesCifrado : IAlmacenDeCredenciales
{
    private const int TamanoDeLaClaveEnBytes = 32;
    private const int TamanoDelNonceEnBytes = 12;
    private const int TamanoDeLaEtiquetaEnBytes = 16;

    private readonly RutasDeLaAplicacion _rutas;
    private readonly IProtectorDeLaClave _protectorDeLaClave;
    private readonly SemaphoreSlim _accesoExclusivo = new(1, 1);

    public AlmacenDeCredencialesCifrado(RutasDeLaAplicacion rutas)
        : this(rutas, CrearProtectorParaEsteSistema())
    {
    }

    internal AlmacenDeCredencialesCifrado(RutasDeLaAplicacion rutas, IProtectorDeLaClave protectorDeLaClave)
    {
        _rutas = rutas;
        _protectorDeLaClave = protectorDeLaClave;
    }

    public async Task GuardarContrasenaAsync(Guid idDelPerfil, string contrasena, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            var clave = await ObtenerOCrearClaveAsync(tokenDeCancelacion);
            var credenciales = await LeerCredencialesAsync(tokenDeCancelacion);
            credenciales[idDelPerfil] = Cifrar(contrasena, clave);
            await EscribirCredencialesAsync(credenciales, tokenDeCancelacion);
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    public async Task<string?> ObtenerContrasenaAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            var credenciales = await LeerCredencialesAsync(tokenDeCancelacion);

            if (!credenciales.TryGetValue(idDelPerfil, out var contrasenaCifrada) || !File.Exists(_rutas.ArchivoDeClave))
            {
                return null;
            }

            var clave = await LeerClaveAsync(tokenDeCancelacion);
            return Descifrar(contrasenaCifrada, clave);
        }
        catch (CryptographicException)
        {
            // La clave cambió o el archivo está dañado: se trata como si no hubiera contraseña guardada
            return null;
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    public async Task EliminarContrasenaAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            var credenciales = await LeerCredencialesAsync(tokenDeCancelacion);

            if (credenciales.Remove(idDelPerfil))
            {
                await EscribirCredencialesAsync(credenciales, tokenDeCancelacion);
            }
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    private static string Cifrar(string textoPlano, byte[] clave)
    {
        var bytesDelTexto = Encoding.UTF8.GetBytes(textoPlano);
        var nonce = RandomNumberGenerator.GetBytes(TamanoDelNonceEnBytes);
        var textoCifrado = new byte[bytesDelTexto.Length];
        var etiqueta = new byte[TamanoDeLaEtiquetaEnBytes];

        using var aes = new AesGcm(clave, TamanoDeLaEtiquetaEnBytes);
        aes.Encrypt(nonce, bytesDelTexto, textoCifrado, etiqueta);

        // Formato guardado: nonce + etiqueta + texto cifrado
        return Convert.ToBase64String([.. nonce, .. etiqueta, .. textoCifrado]);
    }

    private static string Descifrar(string contenidoEnBase64, byte[] clave)
    {
        var contenido = Convert.FromBase64String(contenidoEnBase64);
        var nonce = contenido.AsSpan(0, TamanoDelNonceEnBytes);
        var etiqueta = contenido.AsSpan(TamanoDelNonceEnBytes, TamanoDeLaEtiquetaEnBytes);
        var textoCifrado = contenido.AsSpan(TamanoDelNonceEnBytes + TamanoDeLaEtiquetaEnBytes);
        var bytesDelTexto = new byte[textoCifrado.Length];

        using var aes = new AesGcm(clave, TamanoDeLaEtiquetaEnBytes);
        aes.Decrypt(nonce, textoCifrado, etiqueta, bytesDelTexto);

        return Encoding.UTF8.GetString(bytesDelTexto);
    }

    private async Task<byte[]> ObtenerOCrearClaveAsync(CancellationToken tokenDeCancelacion)
    {
        if (File.Exists(_rutas.ArchivoDeClave))
        {
            return await LeerClaveAsync(tokenDeCancelacion);
        }

        _rutas.AsegurarQueExistaLaCarpeta();
        var claveNueva = RandomNumberGenerator.GetBytes(TamanoDeLaClaveEnBytes);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeClave, _protectorDeLaClave.Proteger(claveNueva), tokenDeCancelacion);

        return claveNueva;
    }

    private async Task<byte[]> LeerClaveAsync(CancellationToken tokenDeCancelacion) =>
        _protectorDeLaClave.Desproteger(await File.ReadAllBytesAsync(_rutas.ArchivoDeClave, tokenDeCancelacion));

    private static IProtectorDeLaClave CrearProtectorParaEsteSistema() =>
        OperatingSystem.IsWindows() ? new ProtectorDeLaClaveConDpapi() : new ProtectorDeLaClaveConPermisosDeArchivo();

    private async Task<Dictionary<Guid, string>> LeerCredencialesAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeCredenciales))
        {
            return [];
        }

        await using var archivo = File.OpenRead(_rutas.ArchivoDeCredenciales);
        var credenciales = await JsonSerializer.DeserializeAsync<Dictionary<Guid, string>>(
            archivo,
            cancellationToken: tokenDeCancelacion);

        return credenciales ?? [];
    }

    private async Task EscribirCredencialesAsync(Dictionary<Guid, string> credenciales, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(credenciales);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeCredenciales, contenido, tokenDeCancelacion);
    }
}
