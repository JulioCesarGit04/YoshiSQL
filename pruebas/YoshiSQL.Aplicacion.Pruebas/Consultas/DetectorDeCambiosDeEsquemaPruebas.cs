using YoshiSQL.Aplicacion.Consultas;

namespace YoshiSQL.Aplicacion.Pruebas.Consultas;

public class DetectorDeCambiosDeEsquemaPruebas
{
    [Theory]
    [InlineData("CREATE DATABASE Prueba01")]
    [InlineData("create   database [Mi Base];")]
    [InlineData("DROP DATABASE Prueba01")]
    [InlineData("ALTER DATABASE Prueba01 MODIFY NAME = Prueba02")]
    [InlineData("USE master;\nGO\nCREATE\nDATABASE Ventas;")]
    public void ModificaBasesDeDatos_ScriptQueCambiaBases_DevuelveVerdadero(string script)
    {
        Assert.True(DetectorDeCambiosDeEsquema.ModificaBasesDeDatos(script));
    }

    [Theory]
    [InlineData("SELECT * FROM sys.databases")]
    [InlineData("CREATE TABLE dbo.DatabaseLog (Id int)")]
    [InlineData("SELECT DB_NAME()")]
    public void ModificaBasesDeDatos_ScriptSinCambiosDeBases_DevuelveFalso(string script)
    {
        Assert.False(DetectorDeCambiosDeEsquema.ModificaBasesDeDatos(script));
    }
}
