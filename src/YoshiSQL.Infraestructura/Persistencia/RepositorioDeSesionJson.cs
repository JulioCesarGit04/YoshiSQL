using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Sesion;

namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Guarda las pestañas abiertas en sesion.json, dentro de la carpeta de estado (solo legible por el usuario)
/// y usa un archivo de marca para saber si YoshiSQL se cerró de forma inesperada.
/// </summary>
public sealed class RepositorioDeSesionJson : IRepositorioDeSesion
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new() { WriteIndented = true };

    private const char SeparadorDeLaMarca = ';';

    // En Linux la hora de arranque se calcula a partir de contadores del sistema y puede variar unos milisegundos
    // entre una lectura y otra: se toleran diferencias pequeñas
    private static readonly TimeSpan ToleranciaDelInicio = TimeSpan.FromSeconds(1);

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

    public bool EsInstanciaSecundaria { get; private set; }

    public async Task GuardarAsync(EstadoDeLaSesion estado, CancellationToken tokenDeCancelacion)
    {
        // La sesión guardada pertenece a la primera ventana de YoshiSQL: si una segunda también escribiera,
        // cada autoguardado borraría las pestañas de la otra
        if (EsInstanciaSecundaria)
        {
            return;
        }

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
        var hayMarca = File.Exists(_rutas.ArchivoDeEjecucionEnCurso);

        if (hayMarca && OtraInstanciaSigueAbierta(await File.ReadAllTextAsync(_rutas.ArchivoDeEjecucionEnCurso, tokenDeCancelacion), Environment.ProcessId))
        {
            // La marca es de otro YoshiSQL que sigue abierto: no se toca, y esta ventana no guarda sesión
            EsInstanciaSecundaria = true;
            return false;
        }

        var seCerroIncorrectamente = hayMarca;

        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        var marca = Encoding.UTF8.GetBytes(CrearMarcaDeEsteProceso());
        await EscritorDeArchivosSeguro.EscribirAsync(_rutas.ArchivoDeEjecucionEnCurso, marca, tokenDeCancelacion);

        return seCerroIncorrectamente;
    }

    public Task RegistrarCierreCorrectoAsync(CancellationToken tokenDeCancelacion)
    {
        if (!EsInstanciaSecundaria)
        {
            File.Delete(_rutas.ArchivoDeEjecucionEnCurso);
        }

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
            var rutaDelArchivo = ElegirRutaLibre(carpeta, Path.GetFileName(pestana.NombreDelArchivo));
            var contenido = Encoding.UTF8.GetBytes(pestana.Texto!);
            await EscritorDeArchivosSeguro.EscribirAsync(rutaDelArchivo, contenido, tokenDeCancelacion);
        }

        return carpeta;
    }

    /// <summary>
    /// Dos pestañas pueden llamarse igual (consulta.sql de carpetas distintas): la segunda se guarda como
    /// "consulta (2).sql" en vez de sobrescribir a la primera.
    /// </summary>
    private static string ElegirRutaLibre(string carpeta, string nombreDelArchivo)
    {
        var ruta = Path.Combine(carpeta, nombreDelArchivo);
        var nombreSinExtension = Path.GetFileNameWithoutExtension(nombreDelArchivo);
        var extension = Path.GetExtension(nombreDelArchivo);

        for (var numero = 2; File.Exists(ruta); numero++)
        {
            ruta = Path.Combine(carpeta, $"{nombreSinExtension} ({numero}){extension}");
        }

        return ruta;
    }

    /// <summary>
    /// La marca identifica al proceso por su número y por el momento en que arrancó: el número solo no basta,
    /// porque el sistema lo reutiliza (en Linux muy rápido) y otro programa podía heredarlo.
    /// </summary>
    internal static string CrearMarca(int idDelProceso, DateTime inicioDelProceso) =>
        string.Create(CultureInfo.InvariantCulture, $"{idDelProceso}{SeparadorDeLaMarca}{inicioDelProceso.ToUniversalTime().Ticks}");

    private static string CrearMarcaDeEsteProceso()
    {
        using var esteProceso = Process.GetCurrentProcess();
        return CrearMarca(esteProceso.Id, esteProceso.StartTime);
    }

    /// <summary>
    /// Si la marca pertenece a un YoshiSQL que todavía está abierto, no hubo un cierre inesperado.
    /// </summary>
    /// <param name="idDeEsteProceso">Proceso actual; una marca suya no cuenta como "otra" instancia.</param>
    internal static bool OtraInstanciaSigueAbierta(string contenidoDeLaMarca, int idDeEsteProceso)
    {
        var partes = contenidoDeLaMarca.Trim().Split(SeparadorDeLaMarca);

        if (!int.TryParse(partes[0], CultureInfo.InvariantCulture, out var idDelProceso) || idDelProceso == idDeEsteProceso)
        {
            return false;
        }

        try
        {
            using var proceso = Process.GetProcessById(idDelProceso);

            if (proceso.HasExited)
            {
                return false;
            }

            // Marca nueva: sigue abierto solo si es el mismo proceso que la escribió (mismo número y mismo arranque)
            if (partes.Length > 1 && long.TryParse(partes[1], CultureInfo.InvariantCulture, out var inicioRegistrado))
            {
                var diferencia = Math.Abs(proceso.StartTime.ToUniversalTime().Ticks - inicioRegistrado);
                return diferencia <= ToleranciaDelInicio.Ticks;
            }

            // Marca de una versión anterior (solo el número): se compara el nombre del programa
            using var esteProceso = Process.GetCurrentProcess();
            return string.Equals(proceso.ProcessName, esteProceso.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // El proceso ya no existe, o no se puede consultar (es de otro usuario): no es este YoshiSQL
            return false;
        }
    }
}
