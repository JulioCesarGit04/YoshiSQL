using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Dominio.Sesion;
using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public sealed class RepositorioDeSesionJsonPruebas : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}");
    private readonly RutasDeLaAplicacion _rutas;
    private readonly RepositorioDeSesionJson _repositorio;

    public RepositorioDeSesionJsonPruebas()
    {
        _rutas = new RutasDeLaAplicacion(_carpetaTemporal);
        _repositorio = new RepositorioDeSesionJson(_rutas, NullLogger<RepositorioDeSesionJson>.Instance);
    }

    [Fact]
    public async Task Cargar_DespuesDeGuardar_DevuelveLasMismasPestanas()
    {
        var pestana = new PestanaGuardada(TipoDePestana.Consulta, Guid.NewGuid(), "Ventas", "SQLQuery1.sql", null, "SELECT 1;", TieneCambiosSinGuardar: true);

        await _repositorio.GuardarAsync(new EstadoDeLaSesion([pestana], 0), CancellationToken.None);
        var estado = await _repositorio.CargarAsync(CancellationToken.None);

        Assert.Equal(pestana, Assert.Single(estado.Pestanas));
    }

    [Fact]
    public async Task RegistrarInicio_SinCierreCorrectoAnterior_DetectaElCierreInesperado()
    {
        // Simula una ejecución anterior que dejó la marca y cuyo proceso ya no existe
        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        await File.WriteAllTextAsync(_rutas.ArchivoDeEjecucionEnCurso, int.MaxValue.ToString());

        Assert.True(await _repositorio.RegistrarInicioAsync(CancellationToken.None));
    }

    // Se usa el proceso de las pruebas como si fuera "otro" YoshiSQL abierto (por eso el segundo argumento es 0)
    [Fact]
    public void OtraInstanciaSigueAbierta_MismoNumeroYMismoArranque_EsLaMismaVentana()
    {
        using var proceso = System.Diagnostics.Process.GetCurrentProcess();
        var marca = RepositorioDeSesionJson.CrearMarca(proceso.Id, proceso.StartTime);

        Assert.True(RepositorioDeSesionJson.OtraInstanciaSigueAbierta(marca, idDeEsteProceso: 0));
    }

    // Linux reutiliza los números de proceso: tras un cierre inesperado el número puede ser de otro programa
    [Fact]
    public void OtraInstanciaSigueAbierta_NumeroReutilizadoPorOtroProceso_NoLoConfunde()
    {
        using var proceso = System.Diagnostics.Process.GetCurrentProcess();
        var marca = RepositorioDeSesionJson.CrearMarca(proceso.Id, proceso.StartTime.AddHours(-3));

        Assert.False(RepositorioDeSesionJson.OtraInstanciaSigueAbierta(marca, idDeEsteProceso: 0));
    }

    [Fact]
    public async Task RegistrarInicio_EscribeUnaMarcaConNumeroYArranque()
    {
        await _repositorio.RegistrarInicioAsync(CancellationToken.None);

        var marca = await File.ReadAllTextAsync(_rutas.ArchivoDeEjecucionEnCurso);

        Assert.Matches(@"^\d+;\d+$", marca);
    }

    [Fact]
    public async Task RegistrarInicio_DespuesDeUnCierreCorrecto_NoDetectaProblemas()
    {
        await _repositorio.RegistrarInicioAsync(CancellationToken.None);
        await _repositorio.RegistrarCierreCorrectoAsync(CancellationToken.None);

        Assert.False(await _repositorio.RegistrarInicioAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Cargar_ArchivoDanado_DevuelveSesionVacia()
    {
        _rutas.AsegurarQueExistaLaCarpetaDeEstado();
        await File.WriteAllTextAsync(_rutas.ArchivoDeSesion, "{ esto no es json");

        Assert.True((await _repositorio.CargarAsync(CancellationToken.None)).EstaVacio);
    }

    [Fact]
    public async Task GuardarScriptsRecuperados_EscribeUnArchivoPorScript()
    {
        PestanaGuardada[] pestanas =
        [
            new(TipoDePestana.Consulta, Guid.NewGuid(), "master", "SQLQuery1.sql", null, "SELECT 1;", TieneCambiosSinGuardar: true),
            new(TipoDePestana.Diagrama, Guid.NewGuid(), "master", string.Empty, null, null, TieneCambiosSinGuardar: false)
        ];

        var carpeta = await _repositorio.GuardarScriptsRecuperadosAsync(pestanas, CancellationToken.None);

        var archivo = Assert.Single(Directory.GetFiles(carpeta));
        Assert.Equal("SELECT 1;", await File.ReadAllTextAsync(archivo));
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
