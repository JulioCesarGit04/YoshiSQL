using System.Text;
using System.Text.Json;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Guarda la disposición de cada diagrama en diagramas/servidor__base.json, dentro de la carpeta de configuración.
/// </summary>
public sealed class RepositorioDeDiagramasJson : IRepositorioDeDiagramas
{
    private const char CaracterDeReemplazo = '_';
    private const string SeparadorDeServidorYBase = "__";

    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private readonly RutasDeLaAplicacion _rutas;

    public RepositorioDeDiagramasJson(RutasDeLaAplicacion rutas)
    {
        _rutas = rutas;
    }

    public async Task<DisposicionDeDiagrama?> CargarDisposicionAsync(
        string servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var rutaDelArchivo = ObtenerRutaDelArchivo(servidor, baseDeDatos);

        if (!File.Exists(rutaDelArchivo))
        {
            return null;
        }

        try
        {
            await using var archivo = File.OpenRead(rutaDelArchivo);
            return await JsonSerializer.DeserializeAsync<DisposicionDeDiagrama>(archivo, OpcionesDeJson, tokenDeCancelacion);
        }
        catch (JsonException)
        {
            // Un archivo dañado no debe impedir abrir el diagrama: se reorganiza desde cero
            return null;
        }
    }

    public async Task GuardarDisposicionAsync(
        string servidor,
        string baseDeDatos,
        DisposicionDeDiagrama disposicion,
        CancellationToken tokenDeCancelacion)
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        Directory.CreateDirectory(_rutas.CarpetaDeDiagramas);

        var contenido = JsonSerializer.SerializeToUtf8Bytes(disposicion, OpcionesDeJson);
        await EscritorDeArchivosSeguro.EscribirAsync(ObtenerRutaDelArchivo(servidor, baseDeDatos), contenido, tokenDeCancelacion);
    }

    private string ObtenerRutaDelArchivo(string servidor, string baseDeDatos)
    {
        var nombreDelArchivo = $"{LimpiarNombre(servidor)}{SeparadorDeServidorYBase}{LimpiarNombre(baseDeDatos)}.json";
        return Path.Combine(_rutas.CarpetaDeDiagramas, nombreDelArchivo);
    }

    /// <summary>
    /// Deja solo letras, números, punto y guion para obtener un nombre de archivo válido.
    /// </summary>
    private static string LimpiarNombre(string nombre)
    {
        var nombreLimpio = new StringBuilder(nombre.Length);

        foreach (var caracter in nombre)
        {
            nombreLimpio.Append(char.IsLetterOrDigit(caracter) || caracter is '.' or '-' ? caracter : CaracterDeReemplazo);
        }

        return nombreLimpio.ToString();
    }
}
