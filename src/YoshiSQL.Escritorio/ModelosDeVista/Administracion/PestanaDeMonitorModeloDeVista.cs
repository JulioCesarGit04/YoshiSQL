using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Actividad;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Administracion;

/// <summary>
/// Monitor de actividad: quién está conectado, qué ejecuta y quién bloquea a quién.
/// Se actualiza solo cada pocos segundos mientras la pestaña está abierta.
/// </summary>
public sealed partial class PestanaDeMonitorModeloDeVista : DocumentoModeloDeVista
{
    private static readonly TimeSpan IntervaloDeActualizacion = TimeSpan.FromSeconds(5);

    private readonly ServicioDeMonitor _servicioDeMonitor;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly CancellationTokenSource _detencion = new();

    public PestanaDeMonitorModeloDeVista(
        ServidorConectado servidor,
        ServicioDeMonitor servicioDeMonitor,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDeErrores servicioDeErrores)
        : base(servidor)
    {
        _servicioDeMonitor = servicioDeMonitor;
        _servicioDeDialogos = servicioDeDialogos;
        _servicioDeErrores = servicioDeErrores;
    }

    public override string Titulo => $"Monitor: {Servidor.Perfil.NombreVisible}";

    public ObservableCollection<ProcesoActivo> Procesos { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TerminarProcesoCommand))]
    public partial ProcesoActivo? ProcesoSeleccionado { get; set; }

    [ObservableProperty]
    public partial bool ActualizacionAutomatica { get; set; } = true;

    /// <summary>
    /// Carga los procesos y deja corriendo la actualización automática.
    /// </summary>
    public async Task IniciarAsync()
    {
        await ActualizarAsync();
        _ = MantenerActualizadoAsync(_detencion.Token);
    }

    [RelayCommand]
    private async Task ActualizarAsync()
    {
        try
        {
            var procesos = await _servicioDeMonitor.ObtenerProcesosAsync(Servidor, CancellationToken.None);
            var idSeleccionado = ProcesoSeleccionado?.IdDeSesion;

            Procesos.Clear();

            foreach (var proceso in procesos)
            {
                Procesos.Add(proceso);
            }

            ProcesoSeleccionado = Procesos.FirstOrDefault(proceso => proceso.IdDeSesion == idSeleccionado);
            var bloqueados = Procesos.Count(proceso => proceso.EstaBloqueado);
            TextoDeEstado = $"{Procesos.Count} sesiones · {bloqueados} bloqueadas · actualizado a las {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception error)
        {
            TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Actualizar monitor de actividad", Servidor.Perfil.NombreVisible));
        }
    }

    [RelayCommand(CanExecute = nameof(HayProcesoSeleccionado))]
    private async Task TerminarProcesoAsync()
    {
        if (ProcesoSeleccionado is not { } proceso)
        {
            return;
        }

        var confirmado = await _servicioDeDialogos.ConfirmarAsync(
            "Terminar proceso",
            $"¿Deseas terminar la sesión {proceso.IdDeSesion} de {proceso.InicioDeSesion}?\nSi tiene una transacción en curso, se deshará.",
            "Terminar",
            "Cancelar");

        if (!confirmado)
        {
            return;
        }

        try
        {
            await _servicioDeMonitor.TerminarProcesoAsync(Servidor, proceso.IdDeSesion, CancellationToken.None);
            await ActualizarAsync();
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Terminar proceso", Servidor.Perfil.NombreVisible));
        }
    }

    private bool HayProcesoSeleccionado() => ProcesoSeleccionado is not null;

    public override async ValueTask DisposeAsync()
    {
        await _detencion.CancelAsync();
        _detencion.Dispose();
    }

    private async Task MantenerActualizadoAsync(CancellationToken tokenDeCancelacion)
    {
        using var temporizador = new PeriodicTimer(IntervaloDeActualizacion);

        try
        {
            while (await temporizador.WaitForNextTickAsync(tokenDeCancelacion))
            {
                if (ActualizacionAutomatica)
                {
                    await ActualizarAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // La actualización se detiene al cerrar la pestaña
        }
    }
}
