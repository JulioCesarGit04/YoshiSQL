using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class GeneracionDeCambiosDeTablaPruebas
{
    private static readonly TipoDeDato Entero = new("int");
    private static readonly TipoDeDato Texto100 = new("nvarchar", 100);

    private static readonly DefinicionDeTabla TablaOriginal = new("dbo", "Clientes",
    [
        new("Id", "Id", Entero, AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true),
        new("Nombre", "Nombre", Texto100, AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false),
        new("Ciudad", "Ciudad", new TipoDeDato("nvarchar", 50), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false)
    ], NombreDeLaLlavePrimaria: "PK_Clientes");

    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void TablaNueva_GeneraCreateTableEnUnaTransaccion()
    {
        var script = _generador.GenerarCambiosDeTabla("Ventas", original: null, TablaOriginal with { NombreDeLaLlavePrimaria = null });

        Assert.Contains("SET XACT_ABORT ON;", script);
        Assert.Contains("BEGIN TRANSACTION;", script);
        Assert.Contains("CREATE TABLE [dbo].[Clientes]", script);
        Assert.Contains("[Id] int IDENTITY(1,1) NOT NULL", script);
        Assert.Contains("CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", script);
        Assert.Contains("COMMIT TRANSACTION;", script);
    }

    [Fact]
    public void AgregarRenombrarYEliminarColumnas_GeneraCadaInstruccionEnOrden()
    {
        var nueva = TablaOriginal with
        {
            Columnas =
            [
                TablaOriginal.Columnas[0],
                TablaOriginal.Columnas[1] with { Nombre = "NombreCompleto" },
                new(null, "Correo", Texto100, AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false)
            ]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        var posicionDelDrop = script.IndexOf("DROP COLUMN [Ciudad]", StringComparison.Ordinal);
        var posicionDelRename = script.IndexOf("EXEC sp_rename N'[dbo].[Clientes].[Nombre]', N'NombreCompleto', N'COLUMN';", StringComparison.Ordinal);
        var posicionDelAdd = script.IndexOf("ADD [Correo] nvarchar(100) NULL;", StringComparison.Ordinal);

        Assert.True(posicionDelDrop > 0 && posicionDelRename > posicionDelDrop && posicionDelAdd > posicionDelRename, script);
        Assert.DoesNotContain("PRIMARY KEY", script);
    }

    // SQL Server no deja eliminar una columna que todavía tiene su restricción DEFAULT
    [Fact]
    public void EliminarColumna_QuitaAntesSuValorPredeterminado()
    {
        var nueva = TablaOriginal with { Columnas = [TablaOriginal.Columnas[0], TablaOriginal.Columnas[1]] };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        var posicionDeLaBusqueda = script.IndexOf("FROM sys.default_constraints AS dc", StringComparison.Ordinal);
        var posicionDelDrop = script.IndexOf("DROP COLUMN [Ciudad]", StringComparison.Ordinal);

        Assert.True(posicionDeLaBusqueda > 0 && posicionDelDrop > posicionDeLaBusqueda, script);
        Assert.Contains("WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[Clientes]') AND c.name = N'Ciudad';", script);
        Assert.Contains("EXEC sys.sp_executesql @quitarPredeterminado1;", script);
    }

    // Sin un valor para las filas que ya existen, SQL Server rechaza la columna NOT NULL en una tabla con datos
    [Fact]
    public void AgregarColumnaObligatoria_UsaUnValorInicialTemporalParaLasFilasExistentes()
    {
        var nueva = TablaOriginal with
        {
            Columnas = [.. TablaOriginal.Columnas, new(null, "Puntos", Entero, AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false)]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        Assert.Contains("ADD [Puntos] int NOT NULL CONSTRAINT [DF_YoshiSQL_Temporal_Puntos] DEFAULT (0);", script);
        Assert.Contains("ALTER TABLE [dbo].[Clientes] DROP CONSTRAINT [DF_YoshiSQL_Temporal_Puntos];", script);
    }

    [Fact]
    public void AgregarColumnaOpcional_NoNecesitaValorInicial()
    {
        var nueva = TablaOriginal with
        {
            Columnas = [.. TablaOriginal.Columnas, new(null, "Puntos", Entero, AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false)]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        Assert.Contains("ADD [Puntos] int NULL;", script);
        Assert.DoesNotContain("DF_YoshiSQL_Temporal", script);
    }

    [Fact]
    public void TablaNueva_ConValorPredeterminado_LoEscribeEntreParentesis()
    {
        var tabla = new DefinicionDeTabla("dbo", "Productos",
        [
            new(null, "Id", Entero, AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true),
            new(null, "Activo", new TipoDeDato("bit"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, ValorPredeterminado: "1"),
            new(null, "Creado", new TipoDeDato("datetime"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, ValorPredeterminado: "(getdate())")
        ]);

        var script = _generador.GenerarCambiosDeTabla("Ventas", original: null, tabla);

        Assert.Contains("[Activo] bit NOT NULL DEFAULT (1)", script);
        Assert.Contains("[Creado] datetime NOT NULL DEFAULT (getdate())", script);
    }

    [Fact]
    public void CambiarElValorPredeterminado_QuitaElAnteriorYCreaElNuevo()
    {
        var original = TablaOriginal with
        {
            Columnas = [TablaOriginal.Columnas[0], TablaOriginal.Columnas[1], TablaOriginal.Columnas[2] with { ValorPredeterminado = "(N'Lima')" }]
        };
        var nueva = original with
        {
            Columnas = [original.Columnas[0], original.Columnas[1], original.Columnas[2] with { ValorPredeterminado = "N'Cusco'" }]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", original, nueva);

        var posicionDeLaBusqueda = script.IndexOf("AND c.name = N'Ciudad';", StringComparison.Ordinal);
        var posicionDelNuevo = script.IndexOf("ALTER TABLE [dbo].[Clientes] ADD DEFAULT (N'Cusco') FOR [Ciudad];", StringComparison.Ordinal);

        Assert.True(posicionDeLaBusqueda > 0 && posicionDelNuevo > posicionDeLaBusqueda, script);
        Assert.DoesNotContain("ALTER COLUMN", script);
    }

    [Fact]
    public void QuitarElValorPredeterminado_SoloEliminaLaRestriccion()
    {
        var original = TablaOriginal with
        {
            Columnas = [TablaOriginal.Columnas[0], TablaOriginal.Columnas[1], TablaOriginal.Columnas[2] with { ValorPredeterminado = "(N'Lima')" }]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", original, TablaOriginal);

        Assert.Contains("EXEC sys.sp_executesql @quitarPredeterminado1;", script);
        Assert.DoesNotContain("ADD DEFAULT", script);
    }

    [Fact]
    public void AgregarColumnaObligatoriaConValorPredeterminadoPropio_NoUsaElTemporal()
    {
        var nueva = TablaOriginal with
        {
            Columnas = [.. TablaOriginal.Columnas, new(null, "Puntos", Entero, AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, ValorPredeterminado: "10")]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        Assert.Contains("ADD [Puntos] int NOT NULL DEFAULT (10);", script);
        Assert.DoesNotContain("DF_YoshiSQL_Temporal", script);
    }

    // Si el script se abre en una pestaña, esa conexión no debe quedarse con XACT_ABORT activo
    [Fact]
    public void CualquierCambio_AlTerminarApagaXactAbortEnUnLoteAparte()
    {
        var nueva = TablaOriginal with { Columnas = [TablaOriginal.Columnas[0], TablaOriginal.Columnas[1]] };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        Assert.EndsWith($"COMMIT TRANSACTION;{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}SET XACT_ABORT OFF;{Environment.NewLine}GO", script);
    }

    [Fact]
    public void CambiarTipoDeColumnaDeLaLlave_QuitaYVuelveACrearLaLlave()
    {
        var nueva = TablaOriginal with
        {
            Columnas = [TablaOriginal.Columnas[0] with { TipoDeDato = new TipoDeDato("bigint") }, .. TablaOriginal.Columnas.Skip(1)]
        };

        var script = _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, nueva);

        var posicionDelDropDeLaLlave = script.IndexOf("DROP CONSTRAINT [PK_Clientes]", StringComparison.Ordinal);
        var posicionDelAlter = script.IndexOf("ALTER COLUMN [Id] bigint NOT NULL", StringComparison.Ordinal);
        var posicionDeLaNuevaLlave = script.IndexOf("ADD CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])", StringComparison.Ordinal);

        Assert.True(posicionDelDropDeLaLlave > 0 && posicionDelAlter > posicionDelDropDeLaLlave && posicionDeLaNuevaLlave > posicionDelAlter, script);
    }

    [Fact]
    public void SinCambios_NoGeneraInstrucciones()
    {
        Assert.False(CambiosDeTabla.Calcular(TablaOriginal, TablaOriginal).HayCambios);
        Assert.Contains("No hay cambios", _generador.GenerarCambiosDeTabla("Ventas", TablaOriginal, TablaOriginal));
    }
}
