using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

public sealed class RepositorioDeConexionesJson : IRepositorioDeConexiones
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private const string ExtensionDeArchivoDanado = ".danado";

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDeConexionesJson> _registro;
    private readonly SemaphoreSlim _accesoExclusivo = new(1, 1);

    public RepositorioDeConexionesJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDeConexionesJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<IReadOnlyList<PerfilDeConexion>> ObtenerTodosAsync(CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            return await LeerPerfilesAsync(tokenDeCancelacion);
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    public async Task GuardarAsync(PerfilDeConexion perfil, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            var perfiles = (await LeerPerfilesAsync(tokenDeCancelacion))
                .Where(perfilGuardado => perfilGuardado.Id != perfil.Id)
                .Prepend(perfil)
                .ToList();

            await EscribirPerfilesAsync(perfiles, tokenDeCancelacion);
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    public async Task EliminarAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            var perfiles = (await LeerPerfilesAsync(tokenDeCancelacion))
                .Where(perfil => perfil.Id != idDelPerfil)
                .ToList();

            await EscribirPerfilesAsync(perfiles, tokenDeCancelacion);
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    private async Task<List<PerfilDeConexion>> LeerPerfilesAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeConexiones))
        {
            return [];
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDeConexiones);
            var perfiles = await JsonSerializer.DeserializeAsync<List<PerfilDeConexion>>(archivo, OpcionesDeJson, tokenDeCancelacion);

            return perfiles ?? [];
        }
        // Un archivo dañado (ej. por un corte de luz) impedía listar y guardar conexiones: no se podía conectar a nada.
        // Se aparta una copia para no perder las conexiones y se empieza con la lista vacía.
        catch (JsonException error)
        {
            var copiaDelArchivoDanado = _rutas.ArchivoDeConexiones + ExtensionDeArchivoDanado;
            File.Copy(_rutas.ArchivoDeConexiones, copiaDelArchivoDanado, overwrite: true);
            _registro.LogWarning(error, "Las conexiones guardadas están dañadas; se guardó una copia en {Copia}", copiaDelArchivoDanado);
            return [];
        }
    }

    private async Task EscribirPerfilesAsync(List<PerfilDeConexion> perfiles, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(perfiles, OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeConexiones, contenido, tokenDeCancelacion);
    }
}
