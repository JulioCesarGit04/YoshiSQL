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
