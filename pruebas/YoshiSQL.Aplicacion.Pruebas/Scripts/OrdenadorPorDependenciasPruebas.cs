using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Pruebas.Scripts;

public class OrdenadorPorDependenciasPruebas
{
    private static readonly Vista VistaA = new("dbo", "A_Resumen");
    private static readonly Vista VistaB = new("dbo", "B_Base");
    private static readonly Funcion FuncionZ = new("dbo", "Z_Calcular");

    [Fact]
    public void Ordenar_VistaQueUsaOtraDeNombrePosterior_PoneAntesLaRequerida()
    {
        ObjetoDeEsquema[] objetos = [VistaA, VistaB];
        DependenciaEntreModulos[] dependencias = [new("dbo", "A_Resumen", "dbo", "B_Base")];

        var ordenados = OrdenadorPorDependencias.Ordenar(objetos, dependencias);

        Assert.Equal([VistaB, VistaA], ordenados);
    }

    [Fact]
    public void Ordenar_CadenaDeTresConFuncion_RespetaTodaLaCadena()
    {
        ObjetoDeEsquema[] objetos = [VistaA, VistaB, FuncionZ];
        DependenciaEntreModulos[] dependencias =
        [
            new("dbo", "A_Resumen", "dbo", "B_Base"),
            new("dbo", "B_Base", "dbo", "Z_Calcular")
        ];

        var ordenados = OrdenadorPorDependencias.Ordenar(objetos, dependencias);

        Assert.Equal([FuncionZ, VistaB, VistaA], ordenados);
    }

    [Fact]
    public void Ordenar_SinDependencias_ConservaElOrdenOriginal()
    {
        ObjetoDeEsquema[] objetos = [FuncionZ, VistaA, VistaB];

        Assert.Equal(objetos, OrdenadorPorDependencias.Ordenar(objetos, []));
    }

    [Fact]
    public void Ordenar_DependenciaHaciaUnObjetoQueNoVaEnElScript_SeIgnora()
    {
        ObjetoDeEsquema[] objetos = [VistaA];
        DependenciaEntreModulos[] dependencias = [new("dbo", "A_Resumen", "dbo", "NoExportada")];

        Assert.Equal([VistaA], OrdenadorPorDependencias.Ordenar(objetos, dependencias));
    }

    [Fact]
    public void Ordenar_ConCiclo_NoSePierdeNingunObjetoNiSeQuedaEsperando()
    {
        ObjetoDeEsquema[] objetos = [VistaA, VistaB, FuncionZ];
        DependenciaEntreModulos[] dependencias =
        [
            new("dbo", "A_Resumen", "dbo", "B_Base"),
            new("dbo", "B_Base", "dbo", "A_Resumen")
        ];

        var ordenados = OrdenadorPorDependencias.Ordenar(objetos, dependencias);

        Assert.Equal([FuncionZ, VistaA, VistaB], ordenados);
    }

    [Fact]
    public void Ordenar_NombresConOtrasMayusculas_LosReconoce()
    {
        ObjetoDeEsquema[] objetos = [VistaA, VistaB];
        DependenciaEntreModulos[] dependencias = [new("DBO", "a_resumen", "dbo", "B_BASE")];

        Assert.Equal([VistaB, VistaA], OrdenadorPorDependencias.Ordenar(objetos, dependencias));
    }
}
