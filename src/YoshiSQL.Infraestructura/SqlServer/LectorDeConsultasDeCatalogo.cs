using System.Collections.Concurrent;
using System.Reflection;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Lee las consultas .sql incrustadas en el ensamblado (carpeta ConsultasDeCatalogo).
/// </summary>
internal static class LectorDeConsultasDeCatalogo
{
    private const string PrefijoDelRecurso = "YoshiSQL.Infraestructura.SqlServer.ConsultasDeCatalogo.";

    private static readonly ConcurrentDictionary<string, string> ConsultasLeidas = new();

    public static string Leer(string nombreDeLaConsulta) =>
        ConsultasLeidas.GetOrAdd(nombreDeLaConsulta, LeerDelEnsamblado);

    private static string LeerDelEnsamblado(string nombreDeLaConsulta)
    {
        var nombreDelRecurso = $"{PrefijoDelRecurso}{nombreDeLaConsulta}.sql";
        var ensamblado = Assembly.GetExecutingAssembly();

        using var flujo = ensamblado.GetManifestResourceStream(nombreDelRecurso)
            ?? throw new InvalidOperationException($"No existe la consulta de catálogo '{nombreDelRecurso}'.");
        using var lector = new StreamReader(flujo);

        return lector.ReadToEnd();
    }
}
