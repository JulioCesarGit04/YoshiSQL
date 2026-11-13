using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Tamaño de la ventana y de sus paneles en ventana.json, dentro de la carpeta de estado.
/// </summary>
public sealed class RepositorioDeDisposicionJson : IRepositorioDeDisposicion
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private readonly RutasDeLaAplicacion _rutas;
    private readonly ILogger<RepositorioDeDisposicionJson> _registro;

    public RepositorioDeDisposicionJson(RutasDeLaAplicacion rutas, ILogger<RepositorioDeDisposicionJson> registro)
    {
        _rutas = rutas;
        _registro = registro;
    }

    public async Task<DisposicionDeLaVentana> CargarAsync(CancellationToken tokenDeCancelacion)
    {
        if (!File.Exists(_rutas.ArchivoDeDisposicion))
        {
            return DisposicionDeLaVentana.Predeterminada;
        }

        try
        {
            await using var archivo = File.OpenRead(_rutas.ArchivoDeDisposicion);
            var disposicion = await JsonSerializer.DeserializeAsync<DisposicionDeLaVentana>(archivo, OpcionesDeJson, tokenDeCancelacion);
            return (disposicion ?? DisposicionDeLaVentana.Predeterminada).Normalizar();
        }
        // Es solo una comodidad: si el archivo está dañado o no se puede leer, la ventana abre con su tamaño de siempre
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            _registro.LogWarning(error, "No se pudo leer la disposición de la ventana; se usará la predeterminada: {Archivo}", _rutas.ArchivoDeDisposicion);
            return DisposicionDeLaVentana.Predeterminada;
        }
    }

    public async Task GuardarAsync(DisposicionDeLaVentana disposicion, CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        var contenido = JsonSerializer.SerializeToUtf8Bytes(disposicion.Normalizar(), OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeDisposicion, contenido, tokenDeCancelacion);
    }
}
