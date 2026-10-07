using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class GeneradorDeScriptsSqlServerPruebas
{
    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void GenerarSeleccionDeFilas_NombreConCorchete_LoEscapa()
    {
        var script = _generador.GenerarSeleccionDeFilas("Ventas", new Tabla("dbo", "Mi]Tabla"), 1000);

        Assert.Contains("SELECT TOP (1000) *", script);
        Assert.Contains("FROM [dbo].[Mi]]Tabla];", script);
        Assert.Contains("USE [Ventas];", script);
    }

    [Fact]
    public void GenerarSeleccionDeFilas_ConFiltroYOrden_AgregaWhereYOrderBy()
    {
        var script = _generador.GenerarSeleccionDeFilas("Ventas", new Tabla("dbo", "Clientes"), 200, "Ciudad = 'Lima'", "Fecha DESC");

        Assert.Contains("SELECT TOP (200) *", script);
        Assert.Contains("WHERE Ciudad = 'Lima'", script);
        Assert.Contains("ORDER BY Fecha DESC", script);
    }

    [Fact]
    public void GenerarSeleccionDeFilas_SinFiltro_NoAgregaWhereNiOrderBy()
    {
        var script = _generador.GenerarSeleccionDeFilas("Ventas", new Tabla("dbo", "Clientes"), 200, filtroWhere: "   ", ordenarPor: null);

        Assert.DoesNotContain("WHERE", script);
        Assert.DoesNotContain("ORDER BY", script);
    }

    [Fact]
    public void GenerarInstruccionDml_Insert_ExcluyeLaIdentidadYUsaMarcadores()
    {
        var columnas = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        };

        var script = _generador.GenerarInstruccionDml("Ventas", new Tabla("dbo", "Clientes"), columnas, TipoDeScriptDml.Insercion);

        Assert.Contains("INSERT INTO [dbo].[Clientes] ([Nombre])", script);
        Assert.DoesNotContain("[Id]", script);
        Assert.Contains("<Nombre, nvarchar(100),>", script);
    }

    [Fact]
    public void GenerarInstruccionDml_Update_UsaLaLlavePrimariaEnElWhere()
    {
        var columnas = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        };

        var script = _generador.GenerarInstruccionDml("Ventas", new Tabla("dbo", "Clientes"), columnas, TipoDeScriptDml.Actualizacion);

        Assert.Contains("UPDATE [dbo].[Clientes]", script);
        Assert.Contains("[Nombre] = <Nombre, nvarchar(100),>", script);
        Assert.Contains("WHERE [Id] = <Id, int,>", script);
    }

    [Fact]
    public void GenerarInstruccionDml_DeleteSinLlavePrimaria_DejaMarcadorDeCondicion()
    {
        var columnas = new[]
        {
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 1)
        };

        var script = _generador.GenerarInstruccionDml("Ventas", new Tabla("dbo", "Clientes"), columnas, TipoDeScriptDml.Eliminacion);

        Assert.Contains("DELETE FROM [dbo].[Clientes]", script);
        Assert.Contains("WHERE <condición de búsqueda, ,>", script);
    }

    [Fact]
    public void GenerarRenombrado_UsaSpRenameConElNombreCalificadoYElNuevo()
    {
        var script = _generador.GenerarRenombrado("Ventas", new Tabla("dbo", "Clientes"), "ClientesNuevo");

        Assert.Contains("USE [Ventas];", script);
        Assert.Contains("EXEC sys.sp_rename N'[dbo].[Clientes]', N'ClientesNuevo';", script);
    }

    [Theory]
    [InlineData(true, "REBUILD")]
    [InlineData(false, "REORGANIZE")]
    public void GenerarMantenimientoDeIndice_SegunReconstruir_UsaRebuildOReorganize(bool reconstruir, string esperado)
    {
        var script = _generador.GenerarMantenimientoDeIndice("Ventas", new Tabla("dbo", "Clientes"), "IX_Clientes_Ciudad", reconstruir);

        Assert.Contains($"ALTER INDEX [IX_Clientes_Ciudad] ON [dbo].[Clientes] {esperado};", script);
    }

    [Fact]
    public void GenerarConsultaDeFragmentacion_UsaDmvConElObjectIdDeLaTabla()
    {
        var script = _generador.GenerarConsultaDeFragmentacion("Ventas", new Tabla("dbo", "Clientes"));

        Assert.Contains("sys.dm_db_index_physical_stats", script);
        Assert.Contains("OBJECT_ID(N'[dbo].[Clientes]')", script);
    }

    [Fact]
    public void GenerarInsertDeFilas_ConIdentidad_EnvuelveConIdentityInsert()
    {
        var columnas = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 50), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        };
        var filas = new object?[][] { [1, "Ana"], [2, null] };

        var script = _generador.GenerarInsertDeFilas(new Tabla("dbo", "Clientes"), columnas, filas);

        Assert.Contains("SET IDENTITY_INSERT [dbo].[Clientes] ON;", script);
        Assert.Contains("INSERT INTO [dbo].[Clientes] ([Id], [Nombre]) VALUES (1, N'Ana');", script);
        Assert.Contains("VALUES (2, NULL);", script);
        Assert.Contains("SET IDENTITY_INSERT [dbo].[Clientes] OFF;", script);
    }

    [Fact]
    public void GenerarInsertDeFilas_FormateaCadaTipoComoLiteralDeTSql()
    {
        var columnas = Enumerable.Range(1, 8)
            .Select(posicion => new Columna($"c{posicion}", new TipoDeDato("sql_variant"), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: posicion))
            .ToArray();

        var fila = new object?[]
        {
            new DateTime(2026, 1, 5, 10, 30, 0),
            Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"),
            new byte[] { 0xDE, 0xAD },
            12.5m,
            true,
            "O'Hara",
            1234567890L,
            null
        };

        var script = _generador.GenerarInsertDeFilas(new Tabla("dbo", "Variado"), columnas, [fila]);

        Assert.Contains("'2026-01-05 10:30:00.000'", script);
        Assert.Contains("'f47ac10b-58cc-4372-a567-0e02b2c3d479'", script);
        Assert.Contains("0xDEAD", script);
        Assert.Contains("12.5", script);
        Assert.Contains("N'O''Hara'", script);
        Assert.Contains("1234567890", script);
        Assert.Contains("NULL", script);
    }

    [Fact]
    public void GenerarInsertDeFilas_SinFilas_DevuelveVacio()
    {
        var columnas = new[] { new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: false, Posicion: 1) };

        Assert.Equal(string.Empty, _generador.GenerarInsertDeFilas(new Tabla("dbo", "Clientes"), columnas, []));
    }

    [Fact]
    public void GenerarLlaveForanea_GeneraAlterTableAddConstraint()
    {
        var llave = new LlaveForanea("FK_Pedido_Cliente", new Tabla("dbo", "Pedido"), ["ClienteId"], new Tabla("dbo", "Cliente"), ["Id"]);

        var script = _generador.GenerarLlaveForanea(llave);

        Assert.Equal(
            "ALTER TABLE [dbo].[Pedido] ADD CONSTRAINT [FK_Pedido_Cliente] FOREIGN KEY ([ClienteId]) REFERENCES [dbo].[Cliente] ([Id]);",
            script);
    }

    [Fact]
    public void GenerarCreacionDeTabla_ConIdentidadYLlavePrimaria_GeneraDefinicionCompleta()
    {
        var columnas = new[]
        {
            new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1),
            new Columna("Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        };

        var script = _generador.GenerarCreacionDeTabla("Ventas", new Tabla("dbo", "Clientes"), columnas);

        Assert.Contains("CREATE TABLE [dbo].[Clientes]", script);
        Assert.Contains("[Id] int IDENTITY(1,1) NOT NULL", script);
        Assert.Contains("[Nombre] nvarchar(100) NULL", script);
        Assert.Contains("CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", script);
    }

    [Fact]
    public void GenerarEliminacion_Vista_UsaDropView()
    {
        var script = _generador.GenerarEliminacion("Ventas", new Vista("dbo", "VentasDelMes"));

        Assert.Contains("DROP VIEW IF EXISTS [dbo].[VentasDelMes];", script);
    }
}

public class GeneracionDeModificacionPruebas
{
    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void GenerarModificacionDesdeDefinicion_CambiaCreatePorAlter()
    {
        var script = _generador.GenerarModificacionDesdeDefinicion("Ventas", "CREATE VIEW dbo.Activos AS SELECT 1 AS Uno");

        Assert.Contains("ALTER VIEW dbo.Activos AS SELECT 1 AS Uno", script);
        Assert.StartsWith("USE [Ventas];", script);
    }

    [Fact]
    public void GenerarModificacionDesdeDefinicion_ConComentarioInicial_RespetaElComentario()
    {
        var script = _generador.GenerarModificacionDesdeDefinicion("Ventas", "-- creado por Julio\ncreate procedure dbo.Listar as select 1");

        Assert.Contains("-- creado por Julio\nALTER procedure dbo.Listar", script);
    }

    [Fact]
    public void GenerarModificacionDesdeDefinicion_CreateOrAlter_NoLoCambia()
    {
        var script = _generador.GenerarModificacionDesdeDefinicion("Ventas", "CREATE OR ALTER VIEW dbo.V AS SELECT 1 AS Uno");

        Assert.Contains("CREATE OR ALTER VIEW", script);
    }
}
