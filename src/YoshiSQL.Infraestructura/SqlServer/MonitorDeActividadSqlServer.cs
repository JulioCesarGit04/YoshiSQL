using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Actividad;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class MonitorDeActividadSqlServer : IMonitorDeActividad
{
    public Task<IReadOnlyList<ProcesoActivo>> ObtenerProcesosAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion) =>
        ExploradorDeEsquemaSqlServer.LeerFilasAsync(
            datosDeAcceso,
            PerfilDeConexion.BaseDeDatosDelSistema,
            "ListarProcesosActivos",
            parametros: [],
            lector => new ProcesoActivo(
                IdDeSesion: lector.GetInt16(0),
                InicioDeSesion: lector.GetString(1),
                Equipo: lector.GetString(2),
                Programa: lector.GetString(3),
                BaseDeDatos: lector.GetString(4),
                Estado: lector.GetString(5),
                Comando: lector.IsDBNull(6) ? null : lector.GetString(6),
                TipoDeEspera: lector.IsDBNull(7) ? null : lector.GetString(7),
                BloqueadoPorSesion: lector.IsDBNull(8) ? null : lector.GetInt16(8),
                TiempoTranscurridoEnMilisegundos: lector.GetInt64(9),
                TiempoDeCpuEnMilisegundos: lector.GetInt64(10),
                Lecturas: lector.GetInt64(11),
                TextoSql: lector.IsDBNull(12) ? null : lector.GetString(12)),
            tokenDeCancelacion);

    public async Task TerminarProcesoAsync(DatosDeAcceso datosDeAcceso, int idDeSesion, CancellationToken tokenDeCancelacion)
    {
        try
        {
            await using var conexion = new SqlConnection(
                ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, PerfilDeConexion.BaseDeDatosDelSistema, usarPoolDeConexiones: true));
            await conexion.OpenAsync(tokenDeCancelacion);

            // KILL no acepta parámetros; el id es un número entero, así que no hay riesgo de inyección
            await using var comando = new SqlCommand($"KILL {idDeSesion};", conexion);
            await comando.ExecuteNonQueryAsync(tokenDeCancelacion);
        }
        catch (SqlException excepcion)
        {
            throw new ErrorDeEjecucion($"No se pudo terminar la sesión {idDeSesion}: {excepcion.Message}", causa: excepcion);
        }
    }
}
