using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Dominio.Seguridad;

namespace YoshiSQL.Aplicacion.Explorador;

/// <summary>
/// Entrega al explorador de objetos el contenido de cada nodo cuando el usuario lo despliega.
/// </summary>
public sealed class ServicioDelExplorador
{
    private readonly IExploradorDeEsquema _exploradorDeEsquema;

    public ServicioDelExplorador(IProveedorDeBaseDeDatos proveedor)
    {
        _exploradorDeEsquema = proveedor.Explorador;
    }

    public Task<IReadOnlyList<BaseDeDatos>> ObtenerBasesDeDatosAsync(
        ServidorConectado servidor,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerBasesDeDatosAsync(servidor.DatosDeAcceso, tokenDeCancelacion);

    public Task<IReadOnlyList<Tabla>> ObtenerTablasAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerTablasAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<Vista>> ObtenerVistasAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerVistasAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<ProcedimientoAlmacenado>> ObtenerProcedimientosAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerProcedimientosAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<Funcion>> ObtenerFuncionesAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerFuncionesAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<Columna>> ObtenerColumnasAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerColumnasAsync(servidor.DatosDeAcceso, baseDeDatos, objeto, tokenDeCancelacion);

    public Task<PropiedadesDeTabla> ObtenerPropiedadesDeTablaAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerPropiedadesDeTablaAsync(servidor.DatosDeAcceso, baseDeDatos, tabla, tokenDeCancelacion);

    public Task<PropiedadesDeBaseDeDatos> ObtenerPropiedadesDeBaseDeDatosAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerPropiedadesDeBaseDeDatosAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<Indice>> ObtenerIndicesAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerIndicesAsync(servidor.DatosDeAcceso, baseDeDatos, tabla, tokenDeCancelacion);

    public Task<IReadOnlyList<InicioDeSesion>> ObtenerIniciosDeSesionAsync(
        ServidorConectado servidor,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerIniciosDeSesionAsync(servidor.DatosDeAcceso, tokenDeCancelacion);

    public Task<IReadOnlyList<UsuarioDeBaseDeDatos>> ObtenerUsuariosAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerUsuariosAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public Task<IReadOnlyList<RolDeBaseDeDatos>> ObtenerRolesAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _exploradorDeEsquema.ObtenerRolesAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);
}
