using System.Reflection;
using System.Runtime.InteropServices;

namespace YoshiSQL.Aplicacion.Errores;

/// <summary>
/// Versión de YoshiSQL, de .NET y del sistema operativo, para incluirlas en el registro.
/// </summary>
public static class InformacionDelEntorno
{
    public static string VersionDeYoshiSql { get; } = ObtenerVersionDeYoshiSql();

    public static string VersionDeDotNet => RuntimeInformation.FrameworkDescription;

    public static string SistemaOperativo => RuntimeInformation.OSDescription;

    public static string Describir() => $"YoshiSQL {VersionDeYoshiSql} · {VersionDeDotNet} · {SistemaOperativo}";

    private static string ObtenerVersionDeYoshiSql()
    {
        var ensamblado = Assembly.GetEntryAssembly() ?? typeof(InformacionDelEntorno).Assembly;
        var version = ensamblado.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? ensamblado.GetName().Version?.ToString()
            ?? "desconocida";

        // Se quita el identificador del commit que .NET agrega al final (ej. 0.5.0+dc16ad2)
        var finDeLaVersion = version.IndexOf('+');
        return finDeLaVersion < 0 ? version : version[..finDeLaVersion];
    }
}
