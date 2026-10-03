using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Dominio.Preferencias;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public sealed class RepositorioDePreferenciasJsonPruebas : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}");
    private readonly RutasDeLaAplicacion _rutas;
    private readonly RepositorioDePreferenciasJson _repositorio;

    public RepositorioDePreferenciasJsonPruebas()
    {
        _rutas = new RutasDeLaAplicacion(_carpetaTemporal);
        _repositorio = new RepositorioDePreferenciasJson(_rutas, NullLogger<RepositorioDePreferenciasJson>.Instance);
    }

    [Fact]
    public async Task Cargar_SinArchivo_DevuelveLasPredeterminadas()
    {
        Assert.Equal(PreferenciasDelUsuario.Predeterminadas, await _repositorio.CargarAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Cargar_DespuesDeGuardar_DevuelveLoMismoYGuardaElTemaComoTexto()
    {
        var preferencias = new PreferenciasDelUsuario { Tema = TemaVisual.Claro, TamanoDeLetraDelEditor = 16, AjustarLineasLargas = true };

        await _repositorio.GuardarAsync(preferencias, CancellationToken.None);

        Assert.Equal(preferencias, await _repositorio.CargarAsync(CancellationToken.None));
        Assert.Contains("\"Claro\"", await File.ReadAllTextAsync(_rutas.ArchivoDePreferencias));
    }

    [Fact]
    public async Task Cargar_ValoresFueraDeRango_LosCorrige()
    {
        _rutas.AsegurarQueExistaLaCarpeta();
        await File.WriteAllTextAsync(_rutas.ArchivoDePreferencias, """{ "TamanoDeLetraDelEditor": 300, "FuenteDelEditor": "  " }""");

        var preferencias = await _repositorio.CargarAsync(CancellationToken.None);

        Assert.Equal(PreferenciasDelUsuario.TamanoDeLetraMaximo, preferencias.TamanoDeLetraDelEditor);
        Assert.Equal(PreferenciasDelUsuario.FuentePredeterminada, preferencias.FuenteDelEditor);
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
