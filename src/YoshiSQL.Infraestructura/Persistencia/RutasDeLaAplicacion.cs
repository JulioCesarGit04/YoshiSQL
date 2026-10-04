namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Ubicación de los archivos de YoshiSQL según las convenciones de cada sistema:
/// <list type="bullet">
/// <item>Linux: configuración en ~/.config/yoshisql y estado (registros, sesión, historial) en ~/.local/state/yoshisql.</item>
/// <item>Windows: configuración en %APPDATA%\YoshiSQL y estado en %LOCALAPPDATA%\YoshiSQL.</item>
/// </list>
/// </summary>
public sealed class RutasDeLaAplicacion
{
    private const string NombreDeLaCarpetaEnLinux = "yoshisql";
    private const string NombreDeLaCarpetaEnWindows = "YoshiSQL";
    private const string VariableDeCarpetaDeEstado = "XDG_STATE_HOME";

    public RutasDeLaAplicacion()
        : this(ObtenerCarpetaDeConfiguracionDelSistema(), ObtenerCarpetaDeEstadoDelSistema())
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

    private static string ObtenerCarpetaDeConfiguracionDelSistema()
    {
        var nombreDeLaCarpeta = OperatingSystem.IsWindows() ? NombreDeLaCarpetaEnWindows : NombreDeLaCarpetaEnLinux;
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nombreDeLaCarpeta);
    }

    /// <summary>
    /// En Windows los datos que no se sincronizan entre equipos (registros, historial) van en la carpeta local.
    /// </summary>
    private static string ObtenerCarpetaDeEstadoDelSistema() =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), NombreDeLaCarpetaEnWindows)
            : Path.Combine(ObtenerCarpetaBaseDeEstado(), NombreDeLaCarpetaEnLinux);

    private static string ObtenerCarpetaBaseDeEstado()
    {
        var carpetaDefinidaPorElSistema = Environment.GetEnvironmentVariable(VariableDeCarpetaDeEstado);

        return string.IsNullOrWhiteSpace(carpetaDefinidaPorElSistema)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "state")
            : carpetaDefinidaPorElSistema;
    }
}
