using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public sealed class RepositorioDeConexionesJsonPruebas : IDisposable
{
    private const string ContenidoDanado = "[{ \"Id\": ";

    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}");
    private readonly RutasDeLaAplicacion _rutas;
    private readonly RepositorioDeConexionesJson _repositorio;

    public RepositorioDeConexionesJsonPruebas()
    {
        _rutas = new RutasDeLaAplicacion(_carpetaTemporal);
        _repositorio = new RepositorioDeConexionesJson(_rutas, NullLogger<RepositorioDeConexionesJson>.Instance);
    }

    [Fact]
    public async Task ObtenerTodos_ArchivoDanado_DevuelveListaVaciaYGuardaUnaCopia()
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        await File.WriteAllTextAsync(_rutas.ArchivoDeConexiones, ContenidoDanado);

        var perfiles = await _repositorio.ObtenerTodosAsync(CancellationToken.None);

        Assert.Empty(perfiles);
        Assert.Equal(ContenidoDanado, await File.ReadAllTextAsync(_rutas.ArchivoDeConexiones + ".danado"));
    }

    [Fact]
    public async Task Guardar_ConArchivoDanado_PermiteGuardarUnaConexionNueva()
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        await File.WriteAllTextAsync(_rutas.ArchivoDeConexiones, ContenidoDanado);
        var perfil = new PerfilDeConexion { Servidor = "localhost", Usuario = "sa" };

        await _repositorio.GuardarAsync(perfil, CancellationToken.None);

        var guardado = Assert.Single(await _repositorio.ObtenerTodosAsync(CancellationToken.None));
        Assert.Equal(perfil.Id, guardado.Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
