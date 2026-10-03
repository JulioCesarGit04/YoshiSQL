using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public sealed class RepositorioDeDiagramasJsonPruebas : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}");
    private readonly RepositorioDeDiagramasJson _repositorio;

    public RepositorioDeDiagramasJsonPruebas()
    {
        _repositorio = new RepositorioDeDiagramasJson(new RutasDeLaAplicacion(_carpetaTemporal));
    }

    [Fact]
    public async Task CargarDisposicion_DespuesDeGuardar_DevuelveLasMismasPosiciones()
    {
        var clientes = new Tabla("dbo", "Clientes");
        var disposicion = new DisposicionDeDiagrama(new Dictionary<string, PosicionEnElDiagrama>
        {
            [clientes.NombreCompleto] = new(120.5, 80)
        });

        await _repositorio.GuardarDisposicionAsync("localhost,1433", "Ventas/2026", disposicion, CancellationToken.None);
        var disposicionLeida = await _repositorio.CargarDisposicionAsync("localhost,1433", "Ventas/2026", CancellationToken.None);

        Assert.Equal(new PosicionEnElDiagrama(120.5, 80), disposicionLeida?.ObtenerPosicion(clientes));
    }

    [Fact]
    public async Task CargarDisposicion_SinArchivo_DevuelveNulo()
    {
        Assert.Null(await _repositorio.CargarDisposicionAsync("localhost", "NoExiste", CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
