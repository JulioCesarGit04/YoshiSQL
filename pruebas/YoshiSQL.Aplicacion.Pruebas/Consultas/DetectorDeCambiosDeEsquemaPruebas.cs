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

public class DetectorDeCambiosDeTablasPruebas
{
    [Theory]
    [InlineData("CREATE TABLE dbo.T (Id int)", true)]
    [InlineData("drop view dbo.V", true)]
    [InlineData("EXEC sp_rename 'dbo.T', 'T2'", true)]
    [InlineData("SELECT * INTO dbo.Copia FROM dbo.T", true)]
    [InlineData("SELECT @total = COUNT(*) FROM dbo.T", false)]
    [InlineData("INSERT INTO dbo.T VALUES (1)", false)]
    [InlineData("SELECT * FROM dbo.Tabla", false)]
    public void ModificaTablasOVistas_DetectaCambiosDeEstructura(string script, bool esperado)
    {
        Assert.Equal(esperado, DetectorDeCambiosDeEsquema.ModificaTablasOVistas(script));
    }
}
