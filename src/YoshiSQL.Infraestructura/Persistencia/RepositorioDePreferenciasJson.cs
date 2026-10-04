using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Preferencias en preferencias.json, dentro de la carpeta de configuración, legible y editable a mano.
/// </summary>
public sealed class RepositorioDePreferenciasJson : IRepositorioDePreferencias
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDePreferenciasJson> _registro;

    public RepositorioDePreferenciasJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDePreferenciasJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<PreferenciasDelUsuario> CargarAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDePreferencias))
        {
            return PreferenciasDelUsuario.Predeterminadas;
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDePreferencias);
            var preferencias = await JsonSerializer.DeserializeAsync<PreferenciasDelUsuario>(archivo, OpcionesDeJson, tokenDeCancelacion);
            return (preferencias ?? PreferenciasDelUsuario.Predeterminadas).Normalizar();
        }
        catch (JsonException error)
        {
            _registro.LogWarning(error, "Las preferencias están dañadas; se usarán las predeterminadas: {Archivo}", _rutas.ArchivoDePreferencias);
            return PreferenciasDelUsuario.Predeterminadas;
        }
    }

    public async Task GuardarAsync(PreferenciasDelUsuario preferencias, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(preferencias.Normalizar(), OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDePreferencias, contenido, tokenDeCancelacion);
    }
}
