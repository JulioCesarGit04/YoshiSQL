using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class GeneracionDeComandosDeEdicionPruebas
{
    private static readonly Tabla Clientes = new("dbo", "Clientes");

    private static readonly Columna[] Columnas =
    [
        new("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
        new("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2),
        new("Saldo", new TipoDeDato("decimal", Precision: 18, Escala: 2), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 3)
    ];

    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void Modificacion_SoloActualizaLasCeldasCambiadasYUbicaLaFilaPorSuLlave()
    {
        object?[] originales = [7, "Mario", 10.5m];
        object?[] nuevos = [7, "Mario Bros", 10.5m];

        var comando = Assert.Single(_generador.GenerarComandosDeEdicion(Clientes, Columnas, [new CambioDeFila(EstadoDeFila.Modificada, originales, nuevos)]));

        Assert.Equal("UPDATE [dbo].[Clientes] SET [Nombre] = @v0 WHERE [Id] = @k0;", comando.Texto);
        Assert.Equal([new ParametroSql("@v0", "Mario Bros"), new ParametroSql("@k0", 7)], comando.Parametros);
        Assert.Equal(1, comando.FilasEsperadas);
    }

    [Fact]
    public void Insercion_OmiteLaIdentidadYConvierteElTexto()
    {
        object?[] nuevos = [null, "Luigi", "99.90"];

        var comando = Assert.Single(_generador.GenerarComandosDeEdicion(Clientes, Columnas, [new CambioDeFila(EstadoDeFila.Nueva, [], nuevos)]));

        Assert.Equal("INSERT INTO [dbo].[Clientes] ([Nombre], [Saldo]) VALUES (@v0, @v1);", comando.Texto);
        Assert.Equal(99.90m, comando.Parametros[1].Valor);
    }

    [Fact]
    public void Eliminacion_UsaSoloLaLlavePrimaria()
    {
        object?[] originales = [3, "Peach", null];

        var comando = Assert.Single(_generador.GenerarComandosDeEdicion(Clientes, Columnas, [new CambioDeFila(EstadoDeFila.Eliminada, originales, originales)]));

        Assert.Equal("DELETE FROM [dbo].[Clientes] WHERE [Id] = @k0;", comando.Texto);
    }

    [Fact]
    public void TextoInvalidoParaElTipo_ExplicaQueColumnaFallo()
    {
        object?[] nuevos = [null, "Toad", "mucho dinero"];

        var error = Assert.Throws<ErrorDeYoshiSql>(() =>
            _generador.GenerarComandosDeEdicion(Clientes, Columnas, [new CambioDeFila(EstadoDeFila.Nueva, [], nuevos)]));

        Assert.Contains("Saldo", error.Message);
    }

    [Fact]
    public void TablaSinLlavePrimaria_NoSePermiteEditar()
    {
        Columna[] sinLlave = [Columnas[1]];

        Assert.Throws<ErrorDeYoshiSql>(() =>
            _generador.GenerarComandosDeEdicion(Clientes, sinLlave, [new CambioDeFila(EstadoDeFila.Nueva, [], ["x"])]));
    }
}
