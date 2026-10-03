using System.Text.Json;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

public sealed class RepositorioDeConexionesJson : IRepositorioDeConexiones
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly SemaphoreSlim _accesoExclusivo = new(1, 1);

    public RepositorioDeConexionesJson(RutasDeLaAplicacion rutas)
    {
        _rutas = rutas;
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

        await using var archivo = File.OpenRead(_rutas.ArchivoDeConexiones);
        var perfiles = await JsonSerializer.DeserializeAsync<List<PerfilDeConexion>>(archivo, OpcionesDeJson, tokenDeCancelacion);

        return perfiles ?? [];
    }

    private async Task EscribirPerfilesAsync(List<PerfilDeConexion> perfiles, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(perfiles, OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeConexiones, contenido, tokenDeCancelacion);
    }
}
