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

    [FactFueraDeWindows]
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

    [Fact]
    public async Task GuardarContrasena_LaClaveSeGuardaProtegidaPorElProtectorDelSistema()
    {
        var almacenConProtector = new AlmacenDeCredencialesCifrado(_rutas, new ProtectorQueInvierteLosBytes());
        var idDelPerfil = Guid.NewGuid();

        await almacenConProtector.GuardarContrasenaAsync(idDelPerfil, "Clave$Segura123", CancellationToken.None);

        // Se lee con el mismo protector: la clave en disco no es la clave real, pero se recupera bien
        Assert.Equal("Clave$Segura123", await almacenConProtector.ObtenerContrasenaAsync(idDelPerfil, CancellationToken.None));

        // Con otro protector (como otra cuenta de Windows) la contraseña ya no se puede leer
        Assert.Null(await _almacen.ObtenerContrasenaAsync(idDelPerfil, CancellationToken.None));
    }

    /// <summary>
    /// Imita a DPAPI: transforma la clave de forma reversible para comprobar que el almacén usa el protector.
    /// </summary>
    private sealed class ProtectorQueInvierteLosBytes : IProtectorDeLaClave
    {
        public byte[] Proteger(byte[] clave) => clave.Select(valor => (byte)~valor).ToArray();

        public byte[] Desproteger(byte[] claveProtegida) => Proteger(claveProtegida);
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }
}
