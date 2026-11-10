using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

/// <summary>
/// Lo que acompaña a las tablas al exportar una base completa. Sin estas piezas la copia se creaba "sin errores"
/// pero no funcionaba: faltaban valores predeterminados, tipos tabla, secuencias, UNIQUE, índices y disparadores.
/// </summary>
public class GeneracionDeExportacionPruebas
{
    private static readonly Tabla Ventas = new("dbo", "Ventas");

    private readonly GeneradorDeScriptsSqlServer _generador = new();

    [Fact]
    public void GenerarCreacionDeTablaSinEnvolver_ColumnaCalculadaPersistida_UsaLaFormulaEnVezDelTipo()
    {
        Columna[] columnas =
        [
            new("Total", new TipoDeDato("decimal", Precision: 14, Escala: 4), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 1),
            new("Ganancia", new TipoDeDato("decimal", Precision: 14, Escala: 4), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2, EsCalculada: true),
            new("Doble", new TipoDeDato("decimal", Precision: 14, Escala: 4), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 3, EsCalculada: true)
        ];
        ColumnaCalculada[] formulas =
        [
            new(Ventas, "Ganancia", "([Total]-[Costo])", EsPersistida: true),
            new(Ventas, "Doble", "([Total]*(2))", EsPersistida: false)
        ];

        var script = _generador.GenerarCreacionDeTablaSinEnvolver(Ventas, columnas, columnasCalculadas: formulas);

        Assert.Contains("[Total] decimal(14,4) NOT NULL", script);
        Assert.Contains("[Ganancia] AS ([Total]-[Costo]) PERSISTED,", script);
        Assert.Contains("[Doble] AS ([Total]*(2))" + Environment.NewLine, script);
        Assert.DoesNotContain("[Ganancia] decimal", script);
    }

    [Fact]
    public void GenerarCreacionDeTablaSinEnvolver_ConservaLaSemillaYElIncrementoDeLaIdentidad()
    {
        Columna[] columnas =
        [
            new("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: true, Posicion: 1),
            new("Total", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        ];

        var script = _generador.GenerarCreacionDeTablaSinEnvolver(
            Ventas, columnas, identidades: [new IdentidadDeColumna(Ventas, "Id", "1000", "5")]);

        Assert.Contains("[Id] int IDENTITY(1000,5) NOT NULL", script);
    }

    [Fact]
    public void GenerarCreacionDeSinonimo_EsCondicional()
    {
        var script = _generador.GenerarCreacionDeSinonimo(new Sinonimo("dbo", "Clientes", "[OtraBase].[dbo].[Clientes]"));

        Assert.StartsWith("IF OBJECT_ID(N'[dbo].[Clientes]', N'SN') IS NULL", script);
        Assert.EndsWith("CREATE SYNONYM [dbo].[Clientes] FOR [OtraBase].[dbo].[Clientes];", script);
    }

    [Fact]
    public void GenerarCreacionDeTipoDeTabla_EsCondicionalEIncluyeLaLlave()
    {
        var tipo = new TipoDeTabla("dbo", "tt_Lineas",
        [
            new("ProductoId", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: false, Posicion: 1),
            new("Cantidad", new TipoDeDato("decimal", Precision: 12, Escala: 3), AdmiteNulos: true, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 2)
        ]);

        var script = _generador.GenerarCreacionDeTipoDeTabla(tipo);

        Assert.StartsWith("IF TYPE_ID(N'[dbo].[tt_Lineas]') IS NULL", script);
        Assert.Contains("CREATE TYPE [dbo].[tt_Lineas] AS TABLE", script);
        Assert.Contains("[ProductoId] int NOT NULL,", script);
        Assert.Contains("[Cantidad] decimal(12,3) NULL,", script);
        Assert.Contains("PRIMARY KEY ([ProductoId])", script);
    }

    [Fact]
    public void GenerarCreacionDeSecuencia_ContinuaDesdeElSiguienteValor()
    {
        var secuencia = new Secuencia("dbo", "seq_Codigo", "int", SiguienteValor: "606", Incremento: "1",
            ValorMinimo: "-2147483648", ValorMaximo: "2147483647", EsCiclica: false, UsaCache: true, TamanoDeCache: null);

        var script = _generador.GenerarCreacionDeSecuencia(secuencia);

        Assert.StartsWith("IF OBJECT_ID(N'[dbo].[seq_Codigo]', N'SO') IS NULL", script);
        Assert.Contains("CREATE SEQUENCE [dbo].[seq_Codigo] AS int START WITH 606 INCREMENT BY 1 MINVALUE -2147483648 MAXVALUE 2147483647 NO CYCLE CACHE;", script);
    }

    [Theory]
    [InlineData(false, 50, "NO CACHE;")]
    [InlineData(true, 50, "CACHE 50;")]
    public void GenerarCreacionDeSecuencia_SegunElCache(bool usaCache, int tamano, string esperado)
    {
        var secuencia = new Secuencia("dbo", "s", "bigint", "1", "1", "1", "100", EsCiclica: true, usaCache, tamano);

        var script = _generador.GenerarCreacionDeSecuencia(secuencia);

        Assert.EndsWith($"CYCLE {esperado}", script);
        Assert.DoesNotContain("NO CYCLE", script);
    }

    [Fact]
    public void GenerarCreacionDeIndice_RestriccionUnica_EsUnaConstraintParaQueLasLlavesForaneasPuedanApuntarle()
    {
        var indice = new IndiceDeTabla(Ventas, "UQ_Ventas_Numero", EsUnico: true, EsRestriccionUnica: true, EsAgrupado: false,
            [new ColumnaDeIndice("Serie", false), new ColumnaDeIndice("Numero", false)], [], Filtro: null);

        Assert.Equal(
            "ALTER TABLE [dbo].[Ventas] ADD CONSTRAINT [UQ_Ventas_Numero] UNIQUE NONCLUSTERED ([Serie], [Numero]);",
            _generador.GenerarCreacionDeIndice(indice));
    }

    [Fact]
    public void GenerarCreacionDeIndice_ConDescendenteIncludeYFiltro_ConservaTodo()
    {
        var indice = new IndiceDeTabla(Ventas, "IX_Ventas_Fecha", EsUnico: true, EsRestriccionUnica: false, EsAgrupado: false,
            [new ColumnaDeIndice("Fecha", true), new ColumnaDeIndice("ClienteId", false)], ["Total", "Estado"], Filtro: "([Anulada]=(0))");

        Assert.Equal(
            "CREATE UNIQUE NONCLUSTERED INDEX [IX_Ventas_Fecha] ON [dbo].[Ventas] ([Fecha] DESC, [ClienteId]) INCLUDE ([Total], [Estado]) WHERE ([Anulada]=(0));",
            _generador.GenerarCreacionDeIndice(indice));
    }

    [Fact]
    public void GenerarCreacionDeIndice_AgrupadoSimple()
    {
        var indice = new IndiceDeTabla(Ventas, "CIX_Ventas", EsUnico: false, EsRestriccionUnica: false, EsAgrupado: true,
            [new ColumnaDeIndice("Fecha", false)], [], Filtro: null);

        Assert.Equal("CREATE CLUSTERED INDEX [CIX_Ventas] ON [dbo].[Ventas] ([Fecha]);", _generador.GenerarCreacionDeIndice(indice));
    }

    [Fact]
    public void GenerarValorPredeterminado_ConNombre_LoConserva()
    {
        var valor = new ValorPredeterminado(Ventas, "Fecha", "DF_Ventas_Fecha", "(sysdatetime())");

        Assert.Equal(
            "ALTER TABLE [dbo].[Ventas] ADD CONSTRAINT [DF_Ventas_Fecha] DEFAULT (sysdatetime()) FOR [Fecha];",
            _generador.GenerarValorPredeterminado(valor));
    }

    [Fact]
    public void GenerarValorPredeterminado_ConNombreDelSistema_DejaQueSqlServerLoNombre()
    {
        var valor = new ValorPredeterminado(Ventas, "Activo", Nombre: null, "((1))");

        Assert.Equal("ALTER TABLE [dbo].[Ventas] ADD DEFAULT ((1)) FOR [Activo];", _generador.GenerarValorPredeterminado(valor));
    }

    [Fact]
    public void GenerarRestriccionDeVerificacion_Activa_ValidaLasFilas()
    {
        var restriccion = new RestriccionDeVerificacion(Ventas, "CK_Ventas_Total", "([Total]>=(0))", EstaDeshabilitada: false);

        Assert.Equal(
            "ALTER TABLE [dbo].[Ventas] WITH CHECK ADD CONSTRAINT [CK_Ventas_Total] CHECK ([Total]>=(0));",
            _generador.GenerarRestriccionDeVerificacion(restriccion));
    }

    [Fact]
    public void GenerarRestriccionDeVerificacion_Deshabilitada_NoValidaYLaDejaApagada()
    {
        var restriccion = new RestriccionDeVerificacion(Ventas, "CK_Vieja", "([Total]>(0))", EstaDeshabilitada: true);

        var script = _generador.GenerarRestriccionDeVerificacion(restriccion);

        Assert.Contains("WITH NOCHECK ADD CONSTRAINT [CK_Vieja] CHECK ([Total]>(0));", script);
        Assert.EndsWith("ALTER TABLE [dbo].[Ventas] NOCHECK CONSTRAINT [CK_Vieja];", script);
    }

    [Theory]
    [InlineData("CASCADE", "NO_ACTION", " ON DELETE CASCADE;")]
    [InlineData("SET_NULL", "CASCADE", " ON DELETE SET NULL ON UPDATE CASCADE;")]
    [InlineData("NO_ACTION", "SET_DEFAULT", " ON UPDATE SET DEFAULT;")]
    public void GenerarLlaveForanea_ConAccionesEnCascada_LasConserva(string alEliminar, string alActualizar, string esperado)
    {
        var llave = new LlaveForanea("FK_Detalle_Venta", new Tabla("dbo", "Detalle"), ["VentaId"], Ventas, ["Id"], alEliminar, alActualizar);

        var script = _generador.GenerarLlaveForanea(llave);

        Assert.EndsWith($"REFERENCES [dbo].[Ventas] ([Id]){esperado}", script);
    }

    [Fact]
    public void GenerarLlaveForanea_SinAcciones_NoAgregaNada()
    {
        var llave = new LlaveForanea("FK_Detalle_Venta", new Tabla("dbo", "Detalle"), ["VentaId"], Ventas, ["Id"]);

        Assert.EndsWith("REFERENCES [dbo].[Ventas] ([Id]);", _generador.GenerarLlaveForanea(llave));
    }

    [Fact]
    public void GenerarDeshabilitacionDeDisparador_NombraAlDisparadorYASuTabla()
    {
        var disparador = new DisparadorDeTabla("dbo", "trg_Auditar", "dbo", "Ventas", "CREATE TRIGGER ...", EstaDeshabilitado: true);

        Assert.Equal("DISABLE TRIGGER [dbo].[trg_Auditar] ON [dbo].[Ventas];", _generador.GenerarDeshabilitacionDeDisparador(disparador));
    }
}
