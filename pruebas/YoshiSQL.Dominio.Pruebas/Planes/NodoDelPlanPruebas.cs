using YoshiSQL.Dominio.Planes;

namespace YoshiSQL.Dominio.Pruebas.Planes;

public class NodoDelPlanPruebas
{
    [Theory]
    [InlineData(1, 50, 50, false)]
    [InlineData(100, 5000, 1, true)]
    [InlineData(100, 120, 1, false)]
    public void EstimacionMuyDiferente_ComparaPorEjecucion(double estimadas, long reales, long ejecuciones, bool esperado)
    {
        var nodo = new NodoDelPlan("Key Lookup", "Key Lookup", null, 0, estimadas, reales, ejecuciones, [], []);

        Assert.Equal(esperado, nodo.EstimacionMuyDiferente);
    }
}
