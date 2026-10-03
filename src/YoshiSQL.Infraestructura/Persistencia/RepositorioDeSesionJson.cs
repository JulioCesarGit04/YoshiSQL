using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Sesion;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Guarda las pestañas abiertas en ~/.local/state/yoshisql/sesion.json (solo legible por el usuario)
/// y usa un archivo de marca para saber si YoshiSQL se cerró de forma inesperada.
/// </summary>
public sealed class RepositorioDeSesionJson : IRepositorioDeSesion
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDeSesionJson> _registro;
    private readonly SemaphoreSlim _accesoExclusivo = new(1, 1);

    public RepositorioDeSesionJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDeSesionJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<EstadoDeLaSesion> CargarAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeSesion))
        {
            return EstadoDeLaSesion.Vacio;
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDeSesion);
            return await JsonSerializer.DeserializeAsync<EstadoDeLaSesion>(archivo, OpcionesDeJson, tokenDeCancelacion)
                ?? EstadoDeLaSesion.Vacio;
        }
        catch (JsonException error)
        {
            // Un archivo dañado no debe impedir que YoshiSQL inicie; se empieza con una sesión vacía
            _registro.LogWarning(error, "El archivo de sesión está dañado y se ignorará: {Archivo}", _rutas.ArchivoDeSesion);
            return EstadoDeLaSesion.Vacio;
        }
    }

    public async Task GuardarAsync(EstadoDeLaSesion estado, CancellationToken tokenDeCancelacion)
    {
        await _accesoExclusivo.WaitAsync(tokenDeCancelacion);

        try
        {
            _rutas.AsegurarQueExistaLaCarpetaDeEstado();
            var contenido = JsonSerializer.SerializeToUtf8Bytes(estado, OpcionesDeJson);
            await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeSesion, contenido, tokenDeCancelacion);
        }
        finally
        {
            _accesoExclusivo.Release();
        }
    }

    public async Task<bool> RegistrarInicioAsync(CancellationToken tokenDeCancelacion)
    {
        var seCerroIncorrectamente = File.Exists(_rutas.ArchivoDeEjecucionEnCurso)
            && !OtraInstanciaSigueAbierta(await File.ReadAllTextAsync(_rutas.ArchivoDeEjecucionEnCurso, tokenDeCancelacion));

        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        var marca = Encoding.UTF8.GetBytes(Environment.ProcessId.ToString());
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeEjecucionEnCurso, marca, tokenDeCancelacion);

        return seCerroIncorrectamente;
    }

    public Task RegistrarCierreCorrectoAsync(CancellationToken tokenDeCancelacion)
    {
        File.Delete(_rutas.ArchivoDeEjecucionEnCurso);
        return Task.CompletedTask;
    }

    public async Task<string> GuardarScriptsRecuperadosAsync(
        IReadOnlyList<PestanaGuardada> pestanas,
        CancellationToken tokenDeCancelacion)
    {
        var carpeta = Path.Combine(_rutas.CarpetaDeEstado, "recuperados", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        Directory.CreateDirectory(carpeta);

        foreach (var pestana in pestanas.Where(pestana => pestana.TieneTexto))
        {
            var rutaDelArchivo = Path.Combine(carpeta, Path.GetFileName(pestana.NombreDelArchivo));
            var contenido = Encoding.UTF8.GetBytes(pestana.Texto!);
            await EscritorDeArchivosSeguro.EscribirAsync(rutaDelArchivo, contenido, tokenDeCancelacion);
        }

        return carpeta;
    }

    /// <summary>
    /// Si la marca pertenece a un YoshiSQL que todavía está abierto, no hubo un cierre inesperado.
    /// </summary>
    private static bool OtraInstanciaSigueAbierta(string contenidoDeLaMarca)
    {
        if (!int.TryParse(contenidoDeLaMarca, out var idDelProceso) || idDelProceso == Environment.ProcessId)
        {
            return false;
        }

        try
        {
            using var proceso = Process.GetProcessById(idDelProceso);
            return !proceso.HasExited;
        }
        catch (ArgumentException)
        {
            // El proceso ya no existe
            return false;
        }
    }
}
