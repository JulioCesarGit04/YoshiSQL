using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Historial;

/// <summary>
/// Una fila del panel de historial: la primera línea de la consulta y cuándo y dónde se ejecutó.
/// </summary>
public sealed partial class EntradaDelHistorialModeloDeVista : ModeloDeVistaBase
{
    private const int LargoMaximoDelResumen = 110;
    private const int LargoMaximoDeLaVistaPrevia = 1500;

    private readonly HistorialModeloDeVista _historial;

    public EntradaDelHistorialModeloDeVista(ConsultaEjecutada consulta, HistorialModeloDeVista historial)
    {
        Consulta = consulta;
        _historial = historial;
    }

    public ConsultaEjecutada Consulta { get; }

    public string Resumen => CrearResumen(Consulta.Texto);

    public string Detalle =>
        $"{Consulta.Momento.ToLocalTime():dd/MM HH:mm} · {Consulta.BaseDeDatos} · {Consulta.Duracion.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture)} s";

    public string VistaPrevia => Consulta.Texto.Length > LargoMaximoDeLaVistaPrevia
        ? Consulta.Texto[..LargoMaximoDeLaVistaPrevia] + "..."
        : Consulta.Texto;

    public bool TerminoConErrores => Consulta.TerminoConErrores;

    [RelayCommand]
    private Task AbrirAsync() => _historial.AbrirAsync(this);

    [RelayCommand]
    private Task CopiarAsync() => _historial.CopiarAsync(this);

    [RelayCommand]
    private Task EliminarAsync() => _historial.EliminarAsync(this);

    /// <summary>
    /// Primera línea con contenido, sin espacios repetidos y recortada si es muy larga.
    /// </summary>
    private static string CrearResumen(string texto)
    {
        var primeraLinea = texto
            .Split('\n')
            .Select(linea => linea.Trim())
            .FirstOrDefault(linea => linea.Length > 0) ?? string.Empty;

        var lineaCompacta = string.Join(' ', primeraLinea.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return lineaCompacta.Length > LargoMaximoDelResumen ? lineaCompacta[..LargoMaximoDelResumen] + "..." : lineaCompacta;
    }
}
