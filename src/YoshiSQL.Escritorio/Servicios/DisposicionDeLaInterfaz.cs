using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Tamaño de la ventana y de sus paneles mientras la aplicación está abierta. Las vistas lo leen al mostrarse
/// y lo actualizan cuando el usuario arrastra un separador; se guarda al cerrar para abrir igual la próxima vez.
/// </summary>
public sealed partial class DisposicionDeLaInterfaz : ObservableObject
{
    private readonly IRepositorioDeDisposicion _repositorio;

    public DisposicionDeLaInterfaz(IRepositorioDeDisposicion repositorio)
    {
        _repositorio = repositorio;
    }

    public double Ancho { get; set; } = DisposicionDeLaVentana.Predeterminada.Ancho;

    public double Alto { get; set; } = DisposicionDeLaVentana.Predeterminada.Alto;

    public bool EstaMaximizada { get; set; }

    public double AnchoDelExplorador { get; set; } = DisposicionDeLaVentana.Predeterminada.AnchoDelExplorador;

    public double AltoDeResultados { get; set; } = DisposicionDeLaVentana.Predeterminada.AltoDeResultados;

    /// <summary>Tamaño de cada diálogo redimensionable, por su nombre.</summary>
    public Dictionary<string, TamanoDeVentana> Dialogos { get; } = [];

    [ObservableProperty]
    public partial bool ExploradorVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool ResultadosVisibles { get; set; } = true;

    public async Task CargarAsync()
    {
        var guardada = await _repositorio.CargarAsync(CancellationToken.None);

        Ancho = guardada.Ancho;
        Alto = guardada.Alto;
        EstaMaximizada = guardada.EstaMaximizada;
        AnchoDelExplorador = guardada.AnchoDelExplorador;
        AltoDeResultados = guardada.AltoDeResultados;
        ExploradorVisible = guardada.ExploradorVisible;
        ResultadosVisibles = guardada.ResultadosVisibles;

        Dialogos.Clear();

        foreach (var (nombre, tamano) in guardada.Dialogos)
        {
            Dialogos[nombre] = tamano;
        }
    }

    public Task GuardarAsync() =>
        _repositorio.GuardarAsync(
            new DisposicionDeLaVentana
            {
                Ancho = Ancho,
                Alto = Alto,
                EstaMaximizada = EstaMaximizada,
                AnchoDelExplorador = AnchoDelExplorador,
                AltoDeResultados = AltoDeResultados,
                ExploradorVisible = ExploradorVisible,
                ResultadosVisibles = ResultadosVisibles,
                Dialogos = new Dictionary<string, TamanoDeVentana>(Dialogos)
            },
            CancellationToken.None);
}
