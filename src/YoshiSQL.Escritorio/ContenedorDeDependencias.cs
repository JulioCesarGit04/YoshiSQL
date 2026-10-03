using Microsoft.Extensions.DependencyInjection;
using YoshiSQL.Aplicacion;
using YoshiSQL.Escritorio.ModelosDeVista;
using YoshiSQL.Escritorio.Servicios;
using YoshiSQL.Infraestructura;

namespace YoshiSQL.Escritorio;

/// <summary>
/// Une todas las capas: aquí se decide qué implementación recibe cada contrato.
/// </summary>
internal static class ContenedorDeDependencias
{
    public static ServiceProvider Construir()
    {
        var servicios = new ServiceCollection()
            .AgregarInfraestructura()
            .AgregarAplicacion();

        servicios.AddSingleton<IServicioDeDialogos, ServicioDeDialogos>();
        servicios.AddSingleton<VentanaPrincipalModeloDeVista>();

        return servicios.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }
}
