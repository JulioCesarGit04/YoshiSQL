using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Consultas favoritas en favoritos.json, dentro de la carpeta de configuración del usuario.
/// </summary>
public sealed class RepositorioDeFavoritosJson : IRepositorioDeFavoritos
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDeFavoritosJson> _registro;

    public RepositorioDeFavoritosJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDeFavoritosJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<IReadOnlyList<ConsultaFavorita>> CargarAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeFavoritos))
        {
            return [];
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDeFavoritos);
            return await JsonSerializer.DeserializeAsync<List<ConsultaFavorita>>(archivo, OpcionesDeJson, tokenDeCancelacion) ?? [];
        }
        catch (JsonException error)
        {
            _registro.LogWarning(error, "Los favoritos están dañados y se empezará de cero: {Archivo}", _rutas.ArchivoDeFavoritos);
            return [];
        }
    }

    public async Task GuardarAsync(IReadOnlyList<ConsultaFavorita> favoritos, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(favoritos, OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeFavoritos, contenido, tokenDeCancelacion);
    }
}
