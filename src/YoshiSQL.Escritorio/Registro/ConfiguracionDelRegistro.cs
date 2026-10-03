using Serilog;
using Serilog.Events;

namespace YoshiSQL.Escritorio.Registro;

/// <summary>
/// Configura el archivo de registro: uno por día, legible por personas y con limpieza automática.
/// </summary>
internal static class ConfiguracionDelRegistro
{
    private const int DiasQueSeConservan = 14;
    private const long TamanoMaximoPorArchivoEnBytes = 10 * 1024 * 1024;

    private const string FormatoDeCadaLinea =
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{" + EnriquecedorDeNivelEnEspanol.NombreDeLaPropiedad + ":l}] {Message:lj}{NewLine}"
        + "{" + EnriquecedorDeDetalleTecnico.NombreDeLaPropiedad + ":l}";

    public static ILogger CrearRegistrador(string carpetaDeRegistros) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.With<EnriquecedorDeNivelEnEspanol>()
            .Enrich.With<EnriquecedorDeDetalleTecnico>()
            .WriteTo.File(
                path: Path.Combine(carpetaDeRegistros, "yoshisql-.log"),
                outputTemplate: FormatoDeCadaLinea,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: DiasQueSeConservan,
                fileSizeLimitBytes: TamanoMaximoPorArchivoEnBytes,
                rollOnFileSizeLimit: true)
            .CreateLogger();
}
