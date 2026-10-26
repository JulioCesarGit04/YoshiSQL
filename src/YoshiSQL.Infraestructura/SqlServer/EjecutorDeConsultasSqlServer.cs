using System.Data;
using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class EjecutorDeConsultasSqlServer : IEjecutorDeConsultas
{
    // Tipos de fecha y hora que se envían con su tipo exacto; para el resto basta con inferirlo del valor
    private static readonly Dictionary<string, SqlDbType> TiposDeFechaYHora = new(StringComparer.OrdinalIgnoreCase)
    {
        ["date"] = SqlDbType.Date,
        ["datetime"] = SqlDbType.DateTime,
        ["datetime2"] = SqlDbType.DateTime2,
        ["smalldatetime"] = SqlDbType.SmallDateTime,
        ["time"] = SqlDbType.Time,
        ["datetimeoffset"] = SqlDbType.DateTimeOffset
    };

    public void LiberarConexionesInactivas() => SqlConnection.ClearAllPools();

    public async Task<ISesionDeConsulta> AbrirSesionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var cadenaDeConexion = ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, baseDeDatos, usarPoolDeConexiones: false);
        return await SesionDeConsultaSqlServer.AbrirAsync(cadenaDeConexion, tokenDeCancelacion);
    }

    public async Task<int> EjecutarEnTransaccionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        IReadOnlyList<ComandoSql> comandos,
        CancellationToken tokenDeCancelacion)
    {
        await using var conexion = new SqlConnection(
            ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, baseDeDatos, usarPoolDeConexiones: false));

        try
        {
            await conexion.OpenAsync(tokenDeCancelacion);
        }
        catch (SqlException excepcion)
        {
            throw TraductorDeErroresSqlServer.TraducirErrorDeConexion(excepcion);
        }

        await using var transaccion = (SqlTransaction)await conexion.BeginTransactionAsync(tokenDeCancelacion);

        try
        {
            var totalDeFilasAfectadas = 0;

            foreach (var comando in comandos)
            {
                totalDeFilasAfectadas += await EjecutarComandoAsync(conexion, transaccion, comando, tokenDeCancelacion);
            }

            await transaccion.CommitAsync(tokenDeCancelacion);
            return totalDeFilasAfectadas;
        }
        catch (SqlException excepcion)
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw new ErrorDeEjecucion($"No se guardó ningún cambio. SQL Server informó:\n{excepcion.Message}", causa: excepcion);
        }
        catch
        {
            await transaccion.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<int> EjecutarComandoAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        ComandoSql comando,
        CancellationToken tokenDeCancelacion)
    {
        // ExecuteNonQuery suma también las filas que tocan los disparadores (si no usan SET NOCOUNT ON), y entonces
        // "1 fila esperada" fallaba siempre en tablas con auditoría. Con NOCOUNT (que los disparadores heredan)
        // y @@ROWCOUNT justo después se cuenta solo lo que hizo esta instrucción.
        await using var comandoSql = new SqlCommand($"SET NOCOUNT ON;\n{comando.Texto}\nSELECT @@ROWCOUNT;", conexion, transaccion);

        foreach (var parametro in comando.Parametros)
        {
            comandoSql.Parameters.Add(CrearParametro(parametro));
        }

        var filasAfectadas = await LeerFilasAfectadasAsync(comandoSql, tokenDeCancelacion);

        if (comando.FilasEsperadas is int filasEsperadas && filasAfectadas != filasEsperadas)
        {
            throw new ErrorDeYoshiSql(
                "No se guardó ningún cambio: una de las filas ya no existe o fue modificada por otra persona. Recarga los datos e inténtalo de nuevo.");
        }

        return filasAfectadas;
    }

    /// <summary>
    /// El conteo es el último valor que devuelve el lote; si un disparador devuelve sus propias filas, se saltan.
    /// </summary>
    private static async Task<int> LeerFilasAfectadasAsync(SqlCommand comando, CancellationToken tokenDeCancelacion)
    {
        await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);
        object? ultimoValor = null;

        do
        {
            while (await lector.ReadAsync(tokenDeCancelacion))
            {
                ultimoValor = lector.GetValue(0);
            }
        }
        while (await lector.NextResultAsync(tokenDeCancelacion));

        return Convert.ToInt32(ultimoValor, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static SqlParameter CrearParametro(ParametroSql parametro)
    {
        var parametroSql = new SqlParameter(parametro.Nombre, parametro.Valor ?? DBNull.Value);

        if (parametro.TipoEnElServidor is { } tipo && TiposDeFechaYHora.TryGetValue(tipo, out var tipoDeFecha))
        {
            parametroSql.SqlDbType = tipoDeFecha;
        }

        return parametroSql;
    }
}
