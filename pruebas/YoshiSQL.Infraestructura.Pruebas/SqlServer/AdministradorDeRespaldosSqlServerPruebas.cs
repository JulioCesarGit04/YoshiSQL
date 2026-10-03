using YoshiSQL.Dominio.Respaldos;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class AdministradorDeRespaldosSqlServerPruebas
{
    private readonly AdministradorDeRespaldosSqlServer _administrador = new();

    [Fact]
    public void GenerarScriptDeRespaldo_ConTodasLasOpciones()
    {
        var script = _administrador.GenerarScriptDeRespaldo(
            new OpcionesDeRespaldo("Ventas", "/var/opt/mssql/data/Ventas.bak", SoloCopia: true, Comprimir: true, Verificar: true));

        Assert.Contains("BACKUP DATABASE [Ventas]", script);
        Assert.Contains("TO DISK = N'/var/opt/mssql/data/Ventas.bak'", script);
        Assert.Contains("WITH COPY_ONLY, INIT", script);
        Assert.Contains("COMPRESSION", script);
        Assert.Contains("RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/data/Ventas.bak';", script);
    }

    [Fact]
    public void GenerarScriptDeRespaldo_RutaConComilla_LaEscapa()
    {
        var script = _administrador.GenerarScriptDeRespaldo(
            new OpcionesDeRespaldo("Ventas", "/respaldos/O'Brien.bak", SoloCopia: false, Comprimir: false, Verificar: false));

        Assert.Contains("N'/respaldos/O''Brien.bak'", script);
        Assert.DoesNotContain("VERIFYONLY", script);
    }

    [Fact]
    public void GenerarScriptDeRestauracion_UbicaCadaArchivoConElNombreDelDestino()
    {
        var opciones = new OpcionesDeRestauracion(
            "/var/opt/mssql/data/Ventas.bak",
            "VentasCopia",
            Reemplazar: true,
            CerrarConexionesExistentes: true,
            [new ArchivoDelRespaldo("Ventas", "/datos/Ventas.mdf", EsDeRegistro: false), new ArchivoDelRespaldo("Ventas_log", "/datos/Ventas_log.ldf", EsDeRegistro: true)],
            CarpetaDeDatos: "/var/opt/mssql/data/",
            CarpetaDeRegistros: "/var/opt/mssql/data");

        var script = _administrador.GenerarScriptDeRestauracion(opciones);

        Assert.Contains("ALTER DATABASE [VentasCopia] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", script);
        Assert.Contains("RESTORE DATABASE [VentasCopia]", script);
        Assert.Contains("MOVE N'Ventas' TO N'/var/opt/mssql/data/VentasCopia.mdf'", script);
        Assert.Contains("MOVE N'Ventas_log' TO N'/var/opt/mssql/data/VentasCopia_log.ldf'", script);
        Assert.Contains("REPLACE", script);
        Assert.Contains("ALTER DATABASE [VentasCopia] SET MULTI_USER;", script);
    }
}
