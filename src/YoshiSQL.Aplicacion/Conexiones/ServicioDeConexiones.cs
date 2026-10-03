using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Conexiones;

public sealed class ServicioDeConexiones
{
    private readonly IProveedorDeBaseDeDatos _proveedor;
    private readonly IRepositorioDeConexiones _repositorioDeConexiones;
    private readonly IAlmacenDeCredenciales _almacenDeCredenciales;

    public ServicioDeConexiones(
        IProveedorDeBaseDeDatos proveedor,
        IRepositorioDeConexiones repositorioDeConexiones,
        IAlmacenDeCredenciales almacenDeCredenciales)
    {
        _proveedor = proveedor;
        _repositorioDeConexiones = repositorioDeConexiones;
        _almacenDeCredenciales = almacenDeCredenciales;
    }

    public Task<IReadOnlyList<PerfilDeConexion>> ObtenerPerfilesGuardadosAsync(CancellationToken tokenDeCancelacion) =>
        _repositorioDeConexiones.ObtenerTodosAsync(tokenDeCancelacion);

    public Task<string?> ObtenerContrasenaGuardadaAsync(PerfilDeConexion perfil, CancellationToken tokenDeCancelacion) =>
        _almacenDeCredenciales.ObtenerContrasenaAsync(perfil.Id, tokenDeCancelacion);

    /// <summary>
    /// Prueba la conexión y, si funciona, guarda el perfil para la próxima vez.
    /// </summary>
    public async Task<ServidorConectado> ConectarAsync(
        PerfilDeConexion perfil,
        string contrasena,
        CancellationToken tokenDeCancelacion)
    {
        var datosDeAcceso = new DatosDeAcceso(perfil, contrasena);

        await _proveedor.ProbarConexionAsync(datosDeAcceso, tokenDeCancelacion);
        var servidor = await _proveedor.Explorador.ObtenerServidorAsync(datosDeAcceso, tokenDeCancelacion);

        await _repositorioDeConexiones.GuardarAsync(perfil, tokenDeCancelacion);
        await GuardarOEliminarContrasenaAsync(perfil, contrasena, tokenDeCancelacion);

        return new ServidorConectado(datosDeAcceso, servidor);
    }

    public async Task EliminarPerfilAsync(PerfilDeConexion perfil, CancellationToken tokenDeCancelacion)
    {
        await _repositorioDeConexiones.EliminarAsync(perfil.Id, tokenDeCancelacion);
        await _almacenDeCredenciales.EliminarContrasenaAsync(perfil.Id, tokenDeCancelacion);
    }

    private Task GuardarOEliminarContrasenaAsync(
        PerfilDeConexion perfil,
        string contrasena,
        CancellationToken tokenDeCancelacion) =>
        perfil.RecordarContrasena
            ? _almacenDeCredenciales.GuardarContrasenaAsync(perfil.Id, contrasena, tokenDeCancelacion)
            : _almacenDeCredenciales.EliminarContrasenaAsync(perfil.Id, tokenDeCancelacion);
}
