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

    public Task<ResultadoDeEjecucion> EjecutarLotesAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion) =>
        EjecutarDeFormaExclusivaAsync(() => EjecutarYMedirAsync(lotes, tokenDeCancelacion));

    public Task<ResultadoDeEjecucion> EjecutarConPlanRealAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion) =>
        EjecutarDeFormaExclusivaAsync(async () =>
        {
            await EjecutarInstruccionDeConfiguracionAsync("SET STATISTICS XML ON;", tokenDeCancelacion);

            try
            {
                return SeparadorDePlanesReales.Separar(await EjecutarYMedirAsync(lotes, tokenDeCancelacion));
            }
            finally
            {
                await EjecutarInstruccionDeConfiguracionAsync("SET STATISTICS XML OFF;", CancellationToken.None);
            }
        });

    public Task<ResultadoDeEjecucion> EjecutarConEstadisticasAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion) =>
        EjecutarDeFormaExclusivaAsync(async () =>
        {
            await EjecutarInstruccionDeConfiguracionAsync("SET STATISTICS IO, TIME ON;", tokenDeCancelacion);

            try
            {
                return await EjecutarYMedirAsync(lotes, tokenDeCancelacion);
            }
            finally
            {
                await EjecutarInstruccionDeConfiguracionAsync("SET STATISTICS IO, TIME OFF;", CancellationToken.None);
            }
        });

    public Task<IReadOnlyList<string>> ObtenerPlanesEstimadosAsync(
        IReadOnlyList<LoteSql> lotes,
        CancellationToken tokenDeCancelacion) =>
        EjecutarDeFormaExclusivaAsync(async () =>
        {
            // Con SHOWPLAN_XML activo el servidor no ejecuta nada: solo devuelve el plan de cada instrucción
            await EjecutarInstruccionDeConfiguracionAsync("SET SHOWPLAN_XML ON;", tokenDeCancelacion);

            try
            {
                return await LeerPlanesEstimadosAsync(lotes, tokenDeCancelacion);
            }
            catch (SqlException excepcion)
            {
                throw new ErrorDeEjecucion($"No se pudo obtener el plan: {excepcion.Message}", causa: excepcion);
            }
            finally
            {
                await EjecutarInstruccionDeConfiguracionAsync("SET SHOWPLAN_XML OFF;", CancellationToken.None);
            }
        });

    /// <summary>
    /// Una pestaña ejecuta una cosa a la vez: una conexión de SQL Server no admite dos consultas simultáneas.
    /// </summary>
    private async Task<T> EjecutarDeFormaExclusivaAsync<T>(Func<Task<T>> ejecutar)
    {
        if (Interlocked.Exchange(ref _ejecucionEnCurso, 1) == 1)
        {
            throw new ErrorDeEjecucion("Ya hay una consulta en ejecución en esta pestaña.");
        }

        try
        {
            return await ejecutar();
        }
        finally
        {
            Volatile.Write(ref _ejecucionEnCurso, 0);
        }
    }

    private async Task<ResultadoDeEjecucion> EjecutarYMedirAsync(IReadOnlyList<LoteSql> lotes, CancellationToken tokenDeCancelacion)
    {
        _mensajesDeLaEjecucion.Clear();
        var conjuntosDeResultados = new List<ConjuntoDeResultados>();
        var cronometro = Stopwatch.StartNew();

        await ReabrirSiSePerdioLaConexionAsync(tokenDeCancelacion);
        var estado = await EjecutarCadaLoteAsync(lotes, conjuntosDeResultados, tokenDeCancelacion);
        return CrearResultado(estado, conjuntosDeResultados, cronometro.Elapsed);
    }

    private async Task<IReadOnlyList<string>> LeerPlanesEstimadosAsync(IReadOnlyList<LoteSql> lotes, CancellationToken tokenDeCancelacion)
    {
        var planes = new List<string>();

        foreach (var lote in lotes)
        {
            await using var comando = new SqlCommand(lote.Texto, _conexion) { CommandTimeout = SinTiempoLimite };
            await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);

            do
            {
                while (await lector.ReadAsync(tokenDeCancelacion))
                {
                    planes.Add(lector.GetString(0));
                }
            }
            while (await lector.NextResultAsync(tokenDeCancelacion));
        }

        return planes;
    }

    private async Task EjecutarInstruccionDeConfiguracionAsync(string instruccion, CancellationToken tokenDeCancelacion)
    {
        await ReabrirSiSePerdioLaConexionAsync(tokenDeCancelacion);
        await using var comando = new SqlCommand(instruccion, _conexion);
        await comando.ExecuteNonQueryAsync(tokenDeCancelacion);
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
            filas.Add(LeerValoresDeLaFila(lector));
        }

        return new ConjuntoDeResultados(columnas, filas);
    }

    private static string ObtenerNombreVisibleDeColumna(string nombre) =>
        string.IsNullOrEmpty(nombre) ? "(Sin nombre de columna)" : nombre;

    private static object?[] LeerValoresDeLaFila(SqlDataReader lector)
    {
        var valores = new object?[lector.FieldCount];

        for (var posicion = 0; posicion < valores.Length; posicion++)
        {
            valores[posicion] = LeerValorDeCelda(lector, posicion);
        }

        return valores;
    }

    private static object? LeerValorDeCelda(SqlDataReader lector, int posicion)
    {
        if (lector.IsDBNull(posicion))
        {
            return null;
        }

        try
        {
            return lector.GetValue(posicion);
        }
        // Los tipos CLR (geography, geometry, hierarchyid, UDT) necesitan el ensamblado
        // Microsoft.SqlServer.Types; si no está, se leen como sus bytes serializados en vez de romper.
        catch (Exception error) when (error is FileNotFoundException or FileLoadException or TypeLoadException)
        {
            try
            {
                return lector.GetSqlBytes(posicion).Value;
            }
            catch
            {
                return $"(valor {lector.GetDataTypeName(posicion)} no legible sin Microsoft.SqlServer.Types)";
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
