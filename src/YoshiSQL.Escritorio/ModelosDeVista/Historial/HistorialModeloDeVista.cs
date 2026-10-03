using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Historial;

/// <summary>
/// Panel con las consultas ejecutadas anteriormente, con búsqueda por texto, servidor o base de datos.
/// </summary>
public sealed partial class HistorialModeloDeVista : ModeloDeVistaBase
{
    private readonly HistorialDeConsultas _historial;
    private readonly IAccionesDelHistorial _acciones;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;
    private readonly IServicioDeErrores _servicioDeErrores;

    public HistorialModeloDeVista(
        HistorialDeConsultas historial,
        IAccionesDelHistorial acciones,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDelSistemaOperativo sistemaOperativo,
        IServicioDeErrores servicioDeErrores)
    {
        _historial = historial;
        _acciones = acciones;
        _servicioDeDialogos = servicioDeDialogos;
        _sistemaOperativo = sistemaOperativo;
        _servicioDeErrores = servicioDeErrores;

        _historial.HistorialModificado += (_, _) => ActualizarEntradas();
    }

    public ObservableCollection<EntradaDelHistorialModeloDeVista> Entradas { get; } = [];

    public bool EstaVacio => Entradas.Count == 0;

    [ObservableProperty]
    public partial string Filtro { get; set; } = string.Empty;

    [ObservableProperty]
    public partial EntradaDelHistorialModeloDeVista? EntradaSeleccionada { get; set; }

    partial void OnFiltroChanged(string value) => ActualizarEntradas();

    public async Task AbrirAsync(EntradaDelHistorialModeloDeVista entrada)
    {
        try
        {
            await _acciones.AbrirConsultaDelHistorialAsync(entrada.Consulta);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir consulta del historial", entrada.Consulta.Servidor));
        }
    }

    public async Task CopiarAsync(EntradaDelHistorialModeloDeVista entrada)
    {
        try
        {
            await _sistemaOperativo.CopiarAlPortapapelesAsync(entrada.Consulta.Texto);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Copiar consulta del historial"));
        }
    }

    public Task EliminarAsync(EntradaDelHistorialModeloDeVista entrada) => _historial.EliminarAsync(entrada.Consulta);

    [RelayCommand]
    private async Task BorrarTodoAsync()
    {
        var confirmado = await _servicioDeDialogos.ConfirmarAsync(
            "Borrar historial",
            "¿Deseas borrar todas las consultas del historial? Esta acción no se puede deshacer.",
            "Borrar todo",
            "Cancelar");

        if (confirmado)
        {
            await _historial.BorrarTodoAsync();
        }
    }

    private void ActualizarEntradas()
    {
        var filtro = Filtro.Trim();

        var consultasVisibles = _historial.ObtenerRecientes()
            .Where(consulta => filtro.Length == 0
                || consulta.Texto.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || consulta.BaseDeDatos.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                || consulta.Servidor.Contains(filtro, StringComparison.OrdinalIgnoreCase));

        Entradas.Clear();

        foreach (var consulta in consultasVisibles)
        {
            Entradas.Add(new EntradaDelHistorialModeloDeVista(consulta, this));
        }

        OnPropertyChanged(nameof(EstaVacio));
    }
}
