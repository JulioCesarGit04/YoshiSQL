using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Aplicacion.Pruebas.Consultas;

public class HistorialDeConsultasPruebas
{
    [Fact]
    public void ObtenerRecientes_DevuelveLaUltimaPrimero()
    {
        var historial = new HistorialDeConsultas();

        historial.Registrar(CrearConsulta("SELECT 1"));
        historial.Registrar(CrearConsulta("SELECT 2"));

        Assert.Equal(["SELECT 2", "SELECT 1"], historial.ObtenerRecientes().Select(consulta => consulta.Texto));
    }

    [Fact]
    public void Registrar_MasDelLimite_DescartaLasMasAntiguas()
    {
        var historial = new HistorialDeConsultas();

        for (var numero = 1; numero <= 250; numero++)
        {
            historial.Registrar(CrearConsulta($"SELECT {numero}"));
        }

        var recientes = historial.ObtenerRecientes();
        Assert.Equal(200, recientes.Count);
        Assert.Equal("SELECT 250", recientes[0].Texto);
    }

    private static ConsultaEjecutada CrearConsulta(string texto) =>
        new(texto, "master", DateTimeOffset.Now, EstadoDeEjecucion.Completada, TimeSpan.Zero);
}
