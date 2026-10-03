using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class EjecutorDeConsultasSqlServer : IEjecutorDeConsultas
{
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
        await using var comandoSql = new SqlCommand(comando.Texto, conexion, transaccion);

        foreach (var parametro in comando.Parametros)
        {
            comandoSql.Parameters.AddWithValue(parametro.Nombre, parametro.Valor ?? DBNull.Value);
        }

        var filasAfectadas = await comandoSql.ExecuteNonQueryAsync(tokenDeCancelacion);

        if (comando.FilasEsperadas is int filasEsperadas && filasAfectadas != filasEsperadas)
        {
            throw new ErrorDeYoshiSql(
                "No se guardó ningún cambio: una de las filas ya no existe o fue modificada por otra persona. Recarga los datos e inténtalo de nuevo.");
        }

        return filasAfectadas;
    }
}
