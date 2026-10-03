namespace YoshiSQL.Infraestructura.Persistencia;

/// <summary>
/// Ubicación de los archivos de configuración: ~/.config/yoshisql en Linux.
/// </summary>
public sealed class RutasDeLaAplicacion
{
    private const string NombreDeLaCarpeta = "yoshisql";

    public RutasDeLaAplicacion()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            NombreDeLaCarpeta))
    {
    }

    public RutasDeLaAplicacion(string carpetaDeConfiguracion)
    {
        CarpetaDeConfiguracion = carpetaDeConfiguracion;
    }

    public string CarpetaDeConfiguracion { get; }

    public string ArchivoDeConexiones => Path.Combine(CarpetaDeConfiguracion, "conexiones.json");

    public string ArchivoDeCredenciales => Path.Combine(CarpetaDeConfiguracion, "credenciales.json");

    public string ArchivoDeClave => Path.Combine(CarpetaDeConfiguracion, "clave.bin");

    public string CarpetaDeDiagramas => Path.Combine(CarpetaDeConfiguracion, "diagramas");

    public void AsegurarQueExistaLaCarpeta()
    {
        if (Directory.Exists(CarpetaDeConfiguracion))
        {
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(CarpetaDeConfiguracion);
        }
        else
        {
            Directory.CreateDirectory(CarpetaDeConfiguracion, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
