using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Aplicacion.Consultas;

public sealed class ServicioDeEjecucion
{
    private readonly IEjecutorDeConsultas _ejecutorDeConsultas;
    private readonly IDivisorDeLotes _divisorDeLotes;
    private readonly HistorialDeConsultas _historial;
    private readonly IAnalizadorDePlanes _analizadorDePlanes;

    public ServicioDeEjecucion(IProveedorDeBaseDeDatos proveedor, HistorialDeConsultas historial)
    {
        _ejecutorDeConsultas = proveedor.Ejecutor;
        _divisorDeLotes = proveedor.DivisorDeLotes;
        _analizadorDePlanes = proveedor.AnalizadorDePlanes;
        _historial = historial;
    }

    public Task<ISesionDeConsulta> AbrirSesionAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _ejecutorDeConsultas.AbrirSesionAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    /// <param name="incluirPlanReal">Además de ejecutar, devuelve el plan real de cada instrucción.</param>
    /// <param name="incluirEstadisticas">Ejecuta con SET STATISTICS IO y TIME; las estadísticas llegan como mensajes.</param>
    public async Task<ResultadoDeEjecucion> EjecutarAsync(
        ServidorConectado servidor,
        ISesionDeConsulta sesion,
        FragmentoDeCodigo fragmento,
        bool incluirPlanReal,
        bool incluirEstadisticas,
        CancellationToken tokenDeCancelacion)
    {
        var lotes = _divisorDeLotes.DividirEnLotes(fragmento.Texto, fragmento.LineaInicial);

        if (lotes.Count == 0)
        {
            return CrearResultadoSinCodigo(sesion);
        }

        var baseDeDatosAlIniciar = sesion.BaseDeDatosActual;
        var resultado = incluirPlanReal
            ? await sesion.EjecutarConPlanRealAsync(lotes, tokenDeCancelacion)
            : incluirEstadisticas
                ? await sesion.EjecutarConEstadisticasAsync(lotes, tokenDeCancelacion)
                : await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);

        await _historial.RegistrarAsync(new ConsultaEjecutada(
            fragmento.Texto,
            servidor.Perfil.NombreVisible,
            baseDeDatosAlIniciar,
            DateTimeOffset.Now,
            resultado.Estado,
            resultado.Duracion));

        return resultado;
    }

    public async Task<PlanDeEjecucion> ObtenerPlanEstimadoAsync(
        ISesionDeConsulta sesion,
        FragmentoDeCodigo fragmento,
        CancellationToken tokenDeCancelacion)
    {
        var lotes = _divisorDeLotes.DividirEnLotes(fragmento.Texto, fragmento.LineaInicial);
        var documentosXml = await sesion.ObtenerPlanesEstimadosAsync(lotes, tokenDeCancelacion);
        return _analizadorDePlanes.Interpretar(documentosXml, esReal: false);
    }

    public PlanDeEjecucion? InterpretarPlanReal(ResultadoDeEjecucion resultado) =>
        resultado.PlanesRealesXml.Count == 0 ? null : _analizadorDePlanes.Interpretar(resultado.PlanesRealesXml, esReal: true);

    private static ResultadoDeEjecucion CrearResultadoSinCodigo(ISesionDeConsulta sesion) =>
        new(
            EstadoDeEjecucion.Completada,
            ConjuntosDeResultados: [],
            Mensajes: [MensajeDeEjecucion.Informacion("No hay código para ejecutar.")],
            Duracion: TimeSpan.Zero,
            BaseDeDatosAlTerminar: sesion.BaseDeDatosActual);
}
