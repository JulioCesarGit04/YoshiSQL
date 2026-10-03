using Serilog.Core;
using Serilog.Events;

namespace YoshiSQL.Escritorio.Registro;

/// <summary>
/// Escribe la excepción con sangría y bajo el título "Detalle técnico", separada del mensaje principal.
/// Los mensajes sin excepción no agregan nada.
/// </summary>
internal sealed class EnriquecedorDeDetalleTecnico : ILogEventEnricher
{
    public const string NombreDeLaPropiedad = "DetalleTecnico";
    private const string Sangria = "      ";

    public void Enrich(LogEvent evento, ILogEventPropertyFactory fabricaDePropiedades)
    {
        var detalle = evento.Exception is null
            ? string.Empty
            : $"  Detalle técnico:{Environment.NewLine}{AgregarSangria(evento.Exception.ToString())}{Environment.NewLine}";

        evento.AddPropertyIfAbsent(fabricaDePropiedades.CreateProperty(NombreDeLaPropiedad, detalle));
    }

    private static string AgregarSangria(string texto) =>
        string.Join(Environment.NewLine, texto.Split('\n').Select(linea => Sangria + linea.TrimEnd('\r')));
}
