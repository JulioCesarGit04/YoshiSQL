using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Dominio.Seguridad;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Lee los objetos del servidor para mostrarlos en el árbol del explorador.
/// </summary>
public interface IExploradorDeEsquema
{
    Task<Servidor> ObtenerServidorAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<BaseDeDatos>> ObtenerBasesDeDatosAsync(
        DatosDeAcceso datosDeAcceso,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Tabla>> ObtenerTablasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Vista>> ObtenerVistasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<ProcedimientoAlmacenado>> ObtenerProcedimientosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Funcion>> ObtenerFuncionesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Columna>> ObtenerColumnasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Columnas de todas las tablas de la base en una sola consulta (para el diagrama).
    /// </summary>
    Task<IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasTablasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Columnas de todas las vistas de la base en una sola consulta (para el autocompletado).
    /// </summary>
    Task<IReadOnlyDictionary<Vista, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasVistasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Código con el que se creó una vista, procedimiento o función.
    /// </summary>
    /// <returns>El código, o nulo si el objeto está cifrado o no hay permiso para verlo.</returns>
    Task<string?> ObtenerDefinicionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Indice>> ObtenerIndicesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion);

    Task<PropiedadesDeTabla> ObtenerPropiedadesDeTablaAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion);

    Task<PropiedadesDeBaseDeDatos> ObtenerPropiedadesDeBaseDeDatosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<DependenciaDeObjeto>> ObtenerDependenciasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<Disparador>> ObtenerDisparadoresAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<InicioDeSesion>> ObtenerIniciosDeSesionAsync(
        DatosDeAcceso datosDeAcceso,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<UsuarioDeBaseDeDatos>> ObtenerUsuariosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<RolDeBaseDeDatos>> ObtenerRolesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task<IReadOnlyList<LlaveForanea>> ObtenerLlavesForaneasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);
}
