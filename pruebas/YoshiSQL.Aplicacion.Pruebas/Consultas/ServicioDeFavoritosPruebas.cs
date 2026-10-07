using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Pruebas.Consultas;

public class ServicioDeFavoritosPruebas
{
    private readonly RepositorioEnMemoria _repositorio = new();
    private readonly ServicioDeFavoritos _servicio;

    public ServicioDeFavoritosPruebas()
    {
        _servicio = new ServicioDeFavoritos(_repositorio, NullLogger<ServicioDeFavoritos>.Instance);
    }

    [Fact]
    public async Task Agregar_PoneElMasNuevoPrimero()
    {
        await _servicio.AgregarAsync("Ventas", "SELECT 1");
        await _servicio.AgregarAsync("Clientes", "SELECT 2");

        Assert.Equal(["Clientes", "Ventas"], _servicio.ObtenerTodos().Select(favorito => favorito.Nombre));
    }

    [Fact]
    public async Task Agregar_ConNombreRepetido_ReemplazaYNoDuplica()
    {
        await _servicio.AgregarAsync("Reporte", "SELECT 1");
        await _servicio.AgregarAsync("reporte", "SELECT 2");

        var favorito = Assert.Single(_servicio.ObtenerTodos());
        Assert.Equal("reporte", favorito.Nombre);
        Assert.Equal("SELECT 2", favorito.Sql);
    }

    [Fact]
    public async Task Eliminar_QuitaElFavorito()
    {
        await _servicio.AgregarAsync("Uno", "SELECT 1");
        var favorito = _servicio.ObtenerTodos()[0];

        await _servicio.EliminarAsync(favorito);

        Assert.Empty(_servicio.ObtenerTodos());
    }

    [Fact]
    public async Task Agregar_GuardaEnElRepositorio()
    {
        await _servicio.AgregarAsync("Uno", "SELECT 1");

        Assert.Single(_repositorio.Guardados);
    }

    [Fact]
    public async Task Cargar_RecuperaLoGuardadoAnteriormente()
    {
        _repositorio.Guardados = [new ConsultaFavorita("De ayer", "SELECT 'x'")];

        await _servicio.CargarAsync(CancellationToken.None);

        Assert.Equal("De ayer", Assert.Single(_servicio.ObtenerTodos()).Nombre);
    }

    private sealed class RepositorioEnMemoria : IRepositorioDeFavoritos
    {
        public IReadOnlyList<ConsultaFavorita> Guardados { get; set; } = [];

        public Task<IReadOnlyList<ConsultaFavorita>> CargarAsync(CancellationToken tokenDeCancelacion) =>
            Task.FromResult(Guardados);

        public Task GuardarAsync(IReadOnlyList<ConsultaFavorita> favoritos, CancellationToken tokenDeCancelacion)
        {
            Guardados = favoritos;
            return Task.CompletedTask;
        }
    }
}
