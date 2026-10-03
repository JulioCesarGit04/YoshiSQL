namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Ubicación de los archivos de YoshiSQL según el estándar de Linux (XDG):
/// configuración en ~/.config/yoshisql y estado (registros, sesión, historial) en ~/.local/state/yoshisql.
/// </summary>
public sealed class RutasDeLaAplicacion
{
    private const string NombreDeLaCarpeta = "yoshisql";
    private const string VariableDeCarpetaDeEstado = "XDG_STATE_HOME";

    public RutasDeLaAplicacion()
        : this(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), NombreDeLaCarpeta),
            Path.Combine(ObtenerCarpetaBaseDeEstado(), NombreDeLaCarpeta))
    {
    }

    /// <param name="carpetaDeEstado">Si es nula, el estado se guarda dentro de la carpeta de configuración.</param>
    public RutasDeLaAplicacion(string carpetaDeConfiguracion, string? carpetaDeEstado = null)
    {
        CarpetaDeConfiguracion = carpetaDeConfiguracion;
        CarpetaDeEstado = carpetaDeEstado ?? Path.Combine(carpetaDeConfiguracion, "estado");
    }

    public string CarpetaDeConfiguracion { get; }

    public string CarpetaDeEstado { get; }

    public string CarpetaDeRegistros => Path.Combine(CarpetaDeEstado, "registros");

    public string ArchivoDeSesion => Path.Combine(CarpetaDeEstado, "sesion.json");

    public string ArchivoDeEjecucionEnCurso => Path.Combine(CarpetaDeEstado, "en-ejecucion");

    public string ArchivoDeHistorial => Path.Combine(CarpetaDeEstado, "historial.json");

    public string ArchivoDePreferencias => Path.Combine(CarpetaDeConfiguracion, "preferencias.json");

    public string ArchivoDeConexiones => Path.Combine(CarpetaDeConfiguracion, "conexiones.json");

    public string ArchivoDeCredenciales => Path.Combine(CarpetaDeConfiguracion, "credenciales.json");

    public string ArchivoDeClave => Path.Combine(CarpetaDeConfiguracion, "clave.bin");

    public string CarpetaDeDiagramas => Path.Combine(CarpetaDeConfiguracion, "diagramas");

    public void AsegurarQueExistaLaCarpeta() => CrearCarpetaPrivada(CarpetaDeConfiguracion);

    public void AsegurarQueExistaLaCarpetaDeEstado() => CrearCarpetaPrivada(CarpetaDeEstado);

    public void AsegurarQueExistaLaCarpetaDeRegistros()
    {
        CrearCarpetaPrivada(CarpetaDeEstado);
        CrearCarpetaPrivada(CarpetaDeRegistros);
    }

    /// <summary>
    /// Crea la carpeta con permisos 700: solo el usuario puede entrar, porque guarda datos personales.
    /// </summary>
    private static void CrearCarpetaPrivada(string carpeta)
    {
        if (Directory.Exists(carpeta))
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(carpeta);
        }
        else
        {
            Directory.CreateDirectory(carpeta, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static string ObtenerCarpetaBaseDeEstado()
    {
        var carpetaDefinidaPorElSistema = Environment.GetEnvironmentVariable(VariableDeCarpetaDeEstado);

        return string.IsNullOrWhiteSpace(carpetaDefinidaPorElSistema)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "state")
            : carpetaDefinidaPorElSistema;
    }
}
