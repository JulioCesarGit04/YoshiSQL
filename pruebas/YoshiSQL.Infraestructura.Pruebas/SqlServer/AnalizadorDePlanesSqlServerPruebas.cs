using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class AnalizadorDePlanesSqlServerPruebas
{
    // Plan simplificado con la misma estructura que devuelve SQL Server: un Nested Loops con dos hijos
    private const string PlanReal = """
        <ShowPlanXML xmlns="http://schemas.microsoft.com/sqlserver/2004/07/showplan">
          <BatchSequence><Batch><Statements>
            <StmtSimple StatementText="SELECT * FROM dbo.Pedidos p JOIN dbo.Clientes c ON c.Id = p.ClienteId" StatementSubTreeCost="1.0">
              <QueryPlan>
                <MissingIndexes />
                <RelOp PhysicalOp="Nested Loops" LogicalOp="Inner Join" EstimateRows="100" EstimatedTotalSubtreeCost="1.0">
                  <RunTimeInformation><RunTimeCountersPerThread Thread="0" ActualRows="5000" ActualExecutions="1" /></RunTimeInformation>
                  <NestedLoops>
                    <RelOp PhysicalOp="Clustered Index Scan" LogicalOp="Clustered Index Scan" EstimateRows="100" EstimatedTotalSubtreeCost="0.7">
                      <Warnings><PlanAffectingConvert /></Warnings>
                      <IndexScan><Object Database="[Ventas]" Schema="[dbo]" Table="[Pedidos]" Index="[PK_Pedidos]" /></IndexScan>
                    </RelOp>
                    <RelOp PhysicalOp="Clustered Index Seek" LogicalOp="Clustered Index Seek" EstimateRows="1" EstimatedTotalSubtreeCost="0.2">
                      <IndexScan><Object Database="[Ventas]" Schema="[dbo]" Table="[Clientes]" Index="[PK_Clientes]" /></IndexScan>
                    </RelOp>
                  </NestedLoops>
                </RelOp>
              </QueryPlan>
            </StmtSimple>
          </Statements></Batch></BatchSequence>
        </ShowPlanXML>
        """;

    private readonly AnalizadorDePlanesSqlServer _analizador = new();

    [Fact]
    public void Interpretar_ArmaElArbolConCostoPropioDeCadaOperacion()
    {
        var instruccion = Assert.Single(_analizador.Interpretar([PlanReal], esReal: true).Instrucciones);
        var raiz = instruccion.Raiz!;

        Assert.Equal("Nested Loops", raiz.OperacionFisica);
        Assert.Equal(10.0, raiz.PorcentajeDelCosto);
        Assert.Equal(2, raiz.Hijos.Count);
        Assert.Equal(70.0, raiz.Hijos[0].PorcentajeDelCosto);
        Assert.Equal("[dbo].[Pedidos] [PK_Pedidos]", raiz.Hijos[0].Objeto);
    }

    [Fact]
    public void Interpretar_PlanReal_IncluyeFilasRealesYDetectaMalasEstimaciones()
    {
        var raiz = _analizador.Interpretar([PlanReal], esReal: true).Instrucciones[0].Raiz!;

        Assert.Equal(5000, raiz.FilasReales);
        Assert.True(raiz.EstimacionMuyDiferente);
        Assert.Null(raiz.Hijos[1].FilasReales);
    }

    [Fact]
    public void Interpretar_TraduceLasAdvertencias()
    {
        var instruccion = _analizador.Interpretar([PlanReal], esReal: true).Instrucciones[0];

        Assert.Contains("Falta un índice que mejoraría esta consulta", instruccion.Advertencias);
        Assert.Contains("Conversión implícita que empeora el plan", instruccion.Raiz!.Hijos[0].Advertencias);
    }
}
