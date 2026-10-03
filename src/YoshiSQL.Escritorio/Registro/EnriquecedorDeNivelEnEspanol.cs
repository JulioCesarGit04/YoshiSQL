using Serilog.Core;
using Serilog.Events;

namespace YoshiSQL.Escritorio.Registro;

/// <summary>
/// Agrega el nivel del mensaje en español (INFORMACIÓN, ADVERTENCIA, ERROR...) para que el registro se lea fácil.
/// </summary>
internal sealed class EnriquecedorDeNivelEnEspanol : ILogEventEnricher
{
    public const string NombreDeLaPropiedad = "Nivel";

    public void Enrich(LogEvent evento, ILogEventPropertyFactory fabricaDePropiedades)
    {
        var nivel = evento.Level switch
        {
            LogEventLevel.Verbose => "DETALLE",
            LogEventLevel.Debug => "DEPURACIÓN",
            LogEventLevel.Information => "INFORMACIÓN",
            LogEventLevel.Warning => "ADVERTENCIA",
            LogEventLevel.Error => "ERROR",
            _ => "CRÍTICO"
        };

        evento.AddPropertyIfAbsent(fabricaDePropiedades.CreateProperty(NombreDeLaPropiedad, nivel));
    }
}
