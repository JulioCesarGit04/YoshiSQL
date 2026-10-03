using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Pruebas.Consultas;

public class HistorialDeConsultasPruebas
{
    private readonly RepositorioEnMemoria _repositorio = new();
    private readonly HistorialDeConsultas _historial;

    public HistorialDeConsultasPruebas()
    {
        _historial = new HistorialDeConsultas(_repositorio, NullLogger<HistorialDeConsultas>.Instance);
    }

    [Fact]
    public async Task ObtenerRecientes_DevuelveLaUltimaPrimero()
    {
        await _historial.RegistrarAsync(CrearConsulta("SELECT 1"));
        await _historial.RegistrarAsync(CrearConsulta("SELECT 2"));

        Assert.Equal(["SELECT 2", "SELECT 1"], _historial.ObtenerRecientes().Select(consulta => consulta.Texto));
    }

    [Fact]
    public async Task Registrar_MasDelLimite_DescartaLasMasAntiguas()
    {
        for (var numero = 1; numero <= HistorialDeConsultas.CantidadMaximaDeConsultas + 50; numero++)
        {
            await _historial.RegistrarAsync(CrearConsulta($"SELECT {numero}"));
        }

        var recientes = _historial.ObtenerRecientes();
        Assert.Equal(HistorialDeConsultas.CantidadMaximaDeConsultas, recientes.Count);
        Assert.Equal($"SELECT {HistorialDeConsultas.CantidadMaximaDeConsultas + 50}", recientes[0].Texto);
    }

    [Fact]
    public async Task Registrar_GuardaEnElRepositorio()
    {
        await _historial.RegistrarAsync(CrearConsulta("SELECT 1"));

        Assert.Single(_repositorio.ConsultasGuardadas);
    }

    [Fact]
    public async Task Cargar_RecuperaLoGuardadoAnteriormente()
    {
        _repositorio.ConsultasGuardadas = [CrearConsulta("SELECT 'de ayer'")];

        await _historial.CargarAsync(CancellationToken.None);

        Assert.Equal("SELECT 'de ayer'", Assert.Single(_historial.ObtenerRecientes()).Texto);
    }

    [Fact]
    public async Task Registrar_TextoMuyLargo_LoRecorta()
    {
        await _historial.RegistrarAsync(CrearConsulta(new string('x', HistorialDeConsultas.LargoMaximoDelTexto + 10)));

        Assert.Equal(HistorialDeConsultas.LargoMaximoDelTexto, _historial.ObtenerRecientes()[0].Texto.Length);
    }

    private static ConsultaEjecutada CrearConsulta(string texto) =>
        new(texto, "localhost", "master", DateTimeOffset.Now, EstadoDeEjecucion.Completada, TimeSpan.Zero);

    private sealed class RepositorioEnMemoria : IRepositorioDeHistorial
    {
        public IReadOnlyList<ConsultaEjecutada> ConsultasGuardadas { get; set; } = [];

        public Task<IReadOnlyList<ConsultaEjecutada>> CargarAsync(CancellationToken tokenDeCancelacion) =>
            Task.FromResult(ConsultasGuardadas);

        public Task GuardarAsync(IReadOnlyList<ConsultaEjecutada> consultas, CancellationToken tokenDeCancelacion)
        {
            ConsultasGuardadas = consultas;
            return Task.CompletedTask;
        }
    }
}
