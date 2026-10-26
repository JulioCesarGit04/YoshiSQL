using YoshiSQL.Aplicacion.Administracion;

namespace YoshiSQL.Aplicacion.Pruebas.Consultas;

public class ServicioDeRespaldosPruebas
{
    [Theory]
    [InlineData("Express Edition (64-bit)", false)]
    [InlineData("Express Edition with Advanced Services (64-bit)", false)]
    [InlineData("Web Edition (64-bit)", false)]
    [InlineData("Developer Edition (64-bit)", true)]
    [InlineData("Standard Edition (64-bit)", true)]
    [InlineData("Enterprise Edition: Core-based Licensing (64-bit)", true)]
    public void AdmiteCompresion_SegunLaEdicion(string edicion, bool esperado)
    {
        Assert.Equal(esperado, ServicioDeRespaldos.AdmiteCompresion(edicion));
    }
}
