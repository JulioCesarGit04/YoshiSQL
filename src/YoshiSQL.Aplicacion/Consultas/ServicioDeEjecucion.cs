using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

public sealed class ServicioDeEjecucion
{
    private readonly IEjecutorDeConsultas _ejecutorDeConsultas;
    private readonly IDivisorDeLotes _divisorDeLotes;
    private readonly HistorialDeConsultas _historial;

    public ServicioDeEjecucion(IProveedorDeBaseDeDatos proveedor, HistorialDeConsultas historial)
    {
        _ejecutorDeConsultas = proveedor.Ejecutor;
        _divisorDeLotes = proveedor.DivisorDeLotes;
        _historial = historial;
    }

    public Task<ISesionDeConsulta> AbrirSesionAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        _ejecutorDeConsultas.AbrirSesionAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

    public async Task<ResultadoDeEjecucion> EjecutarAsync(
        ServidorConectado servidor,
        ISesionDeConsulta sesion,
        FragmentoDeCodigo fragmento,
        CancellationToken tokenDeCancelacion)
    {
        var lotes = _divisorDeLotes.DividirEnLotes(fragmento.Texto, fragmento.LineaInicial);

        if (lotes.Count == 0)
        {
            return CrearResultadoSinCodigo(sesion);
        }

        var baseDeDatosAlIniciar = sesion.BaseDeDatosActual;
        var resultado = await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);

        await _historial.RegistrarAsync(new ConsultaEjecutada(
            fragmento.Texto,
            servidor.Perfil.NombreVisible,
            baseDeDatosAlIniciar,
            DateTimeOffset.Now,
            resultado.Estado,
            resultado.Duracion));

        return resultado;
    }

    private static ResultadoDeEjecucion CrearResultadoSinCodigo(ISesionDeConsulta sesion) =>
        new(
            EstadoDeEjecucion.Completada,
            ConjuntosDeResultados: [],
            Mensajes: [MensajeDeEjecucion.Informacion("No hay código para ejecutar.")],
            Duracion: TimeSpan.Zero,
            BaseDeDatosAlTerminar: sesion.BaseDeDatosActual);
}
