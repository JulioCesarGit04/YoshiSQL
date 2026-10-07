using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Dominio.Salud;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Salud;

/// <summary>
/// Pestaña con el chequeo de una base de datos: resumen, tablas más pesadas e índices fragmentados.
/// </summary>
public sealed partial class PestanaDeSaludModeloDeVista : DocumentoModeloDeVista
{
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly IServicioDeErrores _servicioDeErrores;

    public PestanaDeSaludModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        ServicioDelExplorador servicioDelExplorador,
        IServicioDeErrores servicioDeErrores)
        : base(servidor)
    {
        BaseDeDatos = baseDeDatos;
        _servicioDelExplorador = servicioDelExplorador;
        _servicioDeErrores = servicioDeErrores;
    }

    public string BaseDeDatos { get; }

    public override string Titulo => $"Salud: {BaseDeDatos}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneDatos))]
    public partial SaludDeLaBaseDeDatos? Salud { get; private set; }

    [ObservableProperty]
    public partial bool EstaCargando { get; private set; }

    public bool TieneDatos => Salud is not null;

    [RelayCommand]
    private async Task CargarAsync()
    {
        EstaCargando = true;
        TextoDeEstado = "Analizando la base de datos...";

        try
        {
            Salud = await _servicioDelExplorador.ObtenerSaludAsync(Servidor, BaseDeDatos, CancellationToken.None);
            TextoDeEstado = "Listo.";
        }
        catch (Exception error)
        {
            TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(
                error, new ContextoDeError("Analizar salud de la base de datos", Servidor.Perfil.NombreVisible, BaseDeDatos));
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
