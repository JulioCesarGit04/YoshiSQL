using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Historial de consultas en ~/.local/state/yoshisql/historial.json, legible solo por el usuario
/// porque contiene el texto de las consultas.
/// </summary>
public sealed class RepositorioDeHistorialJson : IRepositorioDeHistorial
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = false };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDeHistorialJson> _registro;

    public RepositorioDeHistorialJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDeHistorialJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<IReadOnlyList<ConsultaEjecutada>> CargarAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeHistorial))
        {
            return [];
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDeHistorial);
            return await JsonSerializer.DeserializeAsync<List<ConsultaEjecutada>>(archivo, OpcionesDeJson, tokenDeCancelacion) ?? [];
        }
        catch (JsonException error)
        {
            _registro.LogWarning(error, "El historial está dañado y se empezará uno nuevo: {Archivo}", _rutas.ArchivoDeHistorial);
            return [];
        }
    }

    public async Task GuardarAsync(IReadOnlyList<ConsultaEjecutada> consultas, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(consultas, OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeHistorial, contenido, tokenDeCancelacion);
    }
}
