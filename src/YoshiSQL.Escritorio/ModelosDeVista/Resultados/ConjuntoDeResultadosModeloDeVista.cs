using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Resultados;

/// <summary>
/// Una grilla de resultados con sus acciones del menú contextual (copiar y exportar).
/// </summary>
public sealed partial class ConjuntoDeResultadosModeloDeVista : ModeloDeVistaBase
{
    private readonly IServicioDeExportacionDeResultados _servicioDeExportacion;

    public ConjuntoDeResultadosModeloDeVista(ConjuntoDeResultados conjunto, IServicioDeExportacionDeResultados servicioDeExportacion)
    {
        Conjunto = conjunto;
        _servicioDeExportacion = servicioDeExportacion;
    }

    public ConjuntoDeResultados Conjunto { get; }

    private const double AltoDeCadaFila = 26;
    private const double AltoDelEncabezadoYLaBarra = 58;
    private const double AltoMinimoDeLaGrilla = 110;
    private const double AltoMaximoDeLaGrilla = 360;

    /// <summary>
    /// Alto de esta grilla cuando la consulta devuelve varias: una de 2 filas no necesita el mismo espacio que una de 500.
    /// </summary>
    public double AltoSugerido =>
        Math.Clamp((Conjunto.CantidadDeFilas * AltoDeCadaFila) + AltoDelEncabezadoYLaBarra, AltoMinimoDeLaGrilla, AltoMaximoDeLaGrilla);

    [RelayCommand]
    private Task CopiarTodoAsync() => _servicioDeExportacion.CopiarAlPortapapelesAsync(Conjunto);

    [RelayCommand]
    private Task CopiarComoInsertAsync() => _servicioDeExportacion.CopiarComoInsertAsync(Conjunto);

    [RelayCommand]
    private Task ExportarACsvAsync() => _servicioDeExportacion.ExportarAsync(Conjunto, FormatoDeExportacion.Csv);

    [RelayCommand]
    private Task ExportarAJsonAsync() => _servicioDeExportacion.ExportarAsync(Conjunto, FormatoDeExportacion.Json);

    [RelayCommand]
    private Task ExportarAExcelAsync() => _servicioDeExportacion.ExportarAsync(Conjunto, FormatoDeExportacion.Excel);
}
