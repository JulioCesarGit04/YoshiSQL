using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

/// <summary>
/// Conexión abierta de una pestaña. Ejecuta los lotes uno tras otro y recoge
/// resultados y mensajes en el mismo orden en que los envía el servidor.
/// </summary>
internal sealed class SesionDeConsultaSqlServer : ISesionDeConsulta
{
    // Los errores con severidad mayor a 10 son errores reales; los demás son avisos o PRINT
    private const int SeveridadMaximaDeUnAviso = 10;

    // Igual que SSMS: las consultas no tienen tiempo límite, el usuario las cancela
    private const int SinTiempoLimite = 0;

    private readonly SqlConnection _conexion;
    private readonly List<MensajeDeEjecucion> _mensajesDeLaEjecucion = [];
    private int _lineaInicialDelLoteActual = 1;
    private int _ejecucionEnCurso;

    private SesionDeConsultaSqlServer(SqlConnection conexion)
    {
        _conexion = conexion;
        // Así los errores leves llegan como mensajes y el lote sigue, como en SSMS
        _conexion.FireInfoMessageEventOnUserErrors = true;
        _conexion.InfoMessage += RegistrarMensajeDelServidor;
    }

    public string BaseDeDatosActual => _conexion.Database;

    public bool EstaEjecutando => Volatile.Read(ref _ejecucionEnCurso) == 1;

    public static async Task<SesionDeConsultaSqlServer> AbrirAsync(
        string cadenaDeConexion,
        CancellationToken tokenDeCancelacion)
    {
        var conexion = new SqlConnection(cadenaDeConexion);

        try
        {
            await conexion.OpenAsync(tokenDeCancelacion);
            return new SesionDeConsultaSqlServer(conexion);
        }
        catch (SqlException excepcion)
        {
            await conexion.DisposeAsync();
            throw TraductorDeErroresSqlServer.TraducirErrorDeConexion(excepcion);
        }
    }

    public async Task<ResultadoDeEjecucion> EjecutarLotesAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion)
    {
        if (Interlocked.Exchange(ref _ejecucionEnCurso, 1) == 1)
        {
            throw new ErrorDeEjecucion("Ya hay una consulta en ejecución en esta pestaña.");
        }

        _mensajesDeLaEjecucion.Clear();
        var conjuntosDeResultados = new List<ConjuntoDeResultados>();
        var cronometro = Stopwatch.StartNew();

        try
        {
            await ReabrirSiSePerdioLaConexionAsync(tokenDeCancelacion);
            var estado = await EjecutarCadaLoteAsync(lotes, conjuntosDeResultados, tokenDeCancelacion);
            return CrearResultado(estado, conjuntosDeResultados, cronometro.Elapsed);
        }
        finally
        {
            Volatile.Write(ref _ejecucionEnCurso, 0);
        }
    }

    public async Task CambiarBaseDeDatosAsync(string baseDeDatos, CancellationToken tokenDeCancelacion)
    {
        try
        {
            await ReabrirSiSePerdioLaConexionAsync(tokenDeCancelacion);
            await _conexion.ChangeDatabaseAsync(baseDeDatos, tokenDeCancelacion);
        }
        catch (SqlException excepcion)
        {
            throw TraductorDeErroresSqlServer.TraducirErrorDeConexion(excepcion);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _conexion.InfoMessage -= RegistrarMensajeDelServidor;
        await _conexion.DisposeAsync();
    }

    private async Task<EstadoDeEjecucion> EjecutarCadaLoteAsync(
        IReadOnlyList<LoteSql> lotes,
        List<ConjuntoDeResultados> conjuntosDeResultados,
        CancellationToken tokenDeCancelacion)
    {
        foreach (var lote in lotes)
        {
            for (var repeticion = 0; repeticion < lote.Repeticiones; repeticion++)
            {
                try
                {
                    await EjecutarLoteAsync(lote, conjuntosDeResultados, tokenDeCancelacion);
                }
                catch (Exception excepcion) when (tokenDeCancelacion.IsCancellationRequested
                    && excepcion is OperationCanceledException or SqlException)
                {
                    _mensajesDeLaEjecucion.Add(MensajeDeEjecucion.Error("La consulta fue cancelada por el usuario."));
                    return EstadoDeEjecucion.Cancelada;
                }
                catch (SqlException excepcion)
                {
                    RegistrarErroresDelLote(excepcion);

                    // Si se cayó la conexión no tiene sentido seguir con los demás lotes
                    if (_conexion.State != ConnectionState.Open)
                    {
                        return EstadoDeEjecucion.Fallida;
                    }
                }
            }
        }

        return EstadoDeEjecucion.Completada;
    }

    private async Task EjecutarLoteAsync(
        LoteSql lote,
        List<ConjuntoDeResultados> conjuntosDeResultados,
        CancellationToken tokenDeCancelacion)
    {
        _lineaInicialDelLoteActual = lote.LineaInicial;

        await using var comando = new SqlCommand(lote.Texto, _conexion)
        {
            CommandTimeout = SinTiempoLimite
        };
        comando.StatementCompleted += RegistrarFilasAfectadas;

        await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);

        do
        {
            if (lector.FieldCount > 0)
            {
                conjuntosDeResultados.Add(await LeerConjuntoDeResultadosAsync(lector, tokenDeCancelacion));
            }
        }
        while (await lector.NextResultAsync(tokenDeCancelacion));
    }

    private static async Task<ConjuntoDeResultados> LeerConjuntoDeResultadosAsync(
        SqlDataReader lector,
        CancellationToken tokenDeCancelacion)
    {
        var columnas = Enumerable.Range(0, lector.FieldCount)
            .Select(posicion => new ColumnaDeResultado(
                ObtenerNombreVisibleDeColumna(lector.GetName(posicion)),
                lector.GetDataTypeName(posicion)))
            .ToList();

        var filas = new List<object?[]>();

        while (await lector.ReadAsync(tokenDeCancelacion))
        {
            var valores = new object?[lector.FieldCount];
            lector.GetValues(valores!);
            ConvertirNulosDeBaseDeDatos(valores);
            filas.Add(valores);
        }

        return new ConjuntoDeResultados(columnas, filas);
    }

    private static string ObtenerNombreVisibleDeColumna(string nombre) =>
        string.IsNullOrEmpty(nombre) ? "(Sin nombre de columna)" : nombre;

    private static void ConvertirNulosDeBaseDeDatos(object?[] valores)
    {
        for (var posicion = 0; posicion < valores.Length; posicion++)
        {
            if (valores[posicion] is DBNull)
            {
                valores[posicion] = null;
            }
        }
    }

    private void RegistrarFilasAfectadas(object? remitente, StatementCompletedEventArgs argumentos)
    {
        var textoDeFilas = argumentos.RecordCount == 1 ? "fila afectada" : "filas afectadas";
        _mensajesDeLaEjecucion.Add(MensajeDeEjecucion.Informacion($"({argumentos.RecordCount} {textoDeFilas})"));
    }

    private void RegistrarMensajeDelServidor(object remitente, SqlInfoMessageEventArgs argumentos)
    {
        foreach (SqlError error in argumentos.Errors)
        {
            _mensajesDeLaEjecucion.Add(ConvertirEnMensaje(error));
        }
    }

    private void RegistrarErroresDelLote(SqlException excepcion)
    {
        foreach (SqlError error in excepcion.Errors)
        {
            _mensajesDeLaEjecucion.Add(ConvertirEnMensaje(error));
        }
    }

    private MensajeDeEjecucion ConvertirEnMensaje(SqlError error)
    {
        if (error.Class <= SeveridadMaximaDeUnAviso)
        {
            return MensajeDeEjecucion.Informacion(error.Message);
        }

        var lineaEnElEditor = _lineaInicialDelLoteActual + error.LineNumber - 1;
        var texto = $"Mensaje {error.Number}, Nivel {error.Class}, Estado {error.State}, Línea {lineaEnElEditor}\n{error.Message}";

        return MensajeDeEjecucion.Error(texto, lineaEnElEditor);
    }

    private ResultadoDeEjecucion CrearResultado(
        EstadoDeEjecucion estado,
        List<ConjuntoDeResultados> conjuntosDeResultados,
        TimeSpan duracion)
    {
        var mensajes = _mensajesDeLaEjecucion.ToList();
        var hayErrores = mensajes.Any(mensaje => mensaje.Tipo == TipoDeMensaje.Error);
        var estadoFinal = estado == EstadoDeEjecucion.Completada && hayErrores
            ? EstadoDeEjecucion.CompletadaConErrores
            : estado;

        var baseDeDatosAlTerminar = _conexion.State == ConnectionState.Open ? _conexion.Database : string.Empty;

        return new ResultadoDeEjecucion(estadoFinal, conjuntosDeResultados, mensajes, duracion, baseDeDatosAlTerminar);
    }

    private async Task ReabrirSiSePerdioLaConexionAsync(CancellationToken tokenDeCancelacion)
    {
        if (_conexion.State == ConnectionState.Open)
        {
            return;
        }

        try
        {
            await _conexion.CloseAsync();
            await _conexion.OpenAsync(tokenDeCancelacion);
        }
        catch (SqlException excepcion)
        {
            throw TraductorDeErroresSqlServer.TraducirErrorDeConexion(excepcion);
        }
    }
}
