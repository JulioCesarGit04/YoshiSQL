using YoshiSQL.Infraestructura.Persistencia;
using YoshiSQL.Infraestructura.Seguridad;

namespace YoshiSQL.Infraestructura.Pruebas.Seguridad;

public sealed class AlmacenDeCredencialesCifradoPruebas : IDisposable
{
    private readonly string _carpetaTemporal = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}");
    private readonly RutasDeLaAplicacion _rutas;
    private readonly AlmacenDeCredencialesCifrado _almacen;

    public AlmacenDeCredencialesCifradoPruebas()
    {
        _rutas = new RutasDeLaAplicacion(_carpetaTemporal);
        _almacen = new AlmacenDeCredencialesCifrado(_rutas);
    }

    [Fact]
    public async Task ObtenerContrasena_DespuesDeGuardar_DevuelveLaMisma()
    {
        var idDelPerfil = Guid.NewGuid();

        await _almacen.GuardarContrasenaAsync(idDelPerfil, "Clave$Segura123", CancellationToken.None);
        var contrasena = await _almacen.ObtenerContrasenaAsync(idDelPerfil, CancellationToken.None);

        Assert.Equal("Clave$Segura123", contrasena);
    }

    [Fact]
    public async Task GuardarContrasena_ElArchivoNoContieneElTextoPlano()
    {
        await _almacen.GuardarContrasenaAsync(Guid.NewGuid(), "Clave$Segura123", CancellationToken.None);

        var contenidoDelArchivo = await File.ReadAllTextAsync(_rutas.ArchivoDeCredenciales);

        Assert.DoesNotContain("Clave$Segura123", contenidoDelArchivo);
    }

    [Fact]
    [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
    public async Task GuardarContrasena_ArchivosSoloLegiblesPorElUsuario()
    {
        await _almacen.GuardarContrasenaAsync(Guid.NewGuid(), "x", CancellationToken.None);

        var permisosEsperados = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        Assert.Equal(permisosEsperados, File.GetUnixFileMode(_rutas.ArchivoDeCredenciales));
        Assert.Equal(permisosEsperados, File.GetUnixFileMode(_rutas.ArchivoDeClave));
    }

    [Fact]
    public async Task ObtenerContrasena_DespuesDeEliminar_DevuelveNulo()
    {
        var idDelPerfil = Guid.NewGuid();
        await _almacen.GuardarContrasenaAsync(idDelPerfil, "x", CancellationToken.None);

        await _almacen.EliminarContrasenaAsync(idDelPerfil, CancellationToken.None);

        Assert.Null(await _almacen.ObtenerContrasenaAsync(idDelPerfil, CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
