using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class PistasDeErroresPruebas
{
    // Caso real (Ubuntu): copias de los procedimientos "sp_" en master hacían fallar CREATE OR ALTER en la base del usuario
    [Fact]
    public void Obtener_AlterDeProcedimientoSpQueNoExiste_ExplicaQueEstaEnMaster()
    {
        var pista = PistasDeErrores.Obtener(numero: 208, estado: 6, procedimiento: "sp_Usuarios_Login");

        Assert.NotNull(pista);
        Assert.Contains("master", pista);
        Assert.Contains("sp_Usuarios_Login", pista);
    }

    [Fact]
    public void Obtener_AlterDeOtroProcedimientoQueNoExiste_SugiereCrearlo()
    {
        var pista = PistasDeErrores.Obtener(numero: 208, estado: 6, procedimiento: "usp_Listar");

        Assert.NotNull(pista);
        Assert.Contains("no existe en esta base", pista);
    }

    [Theory]
    [InlineData(208, 1, "sp_Usuarios_Login")]
    [InlineData(102, 6, "sp_Usuarios_Login")]
    [InlineData(208, 6, null)]
    public void Obtener_OtrosErrores_NoAgregaNada(int numero, byte estado, string? procedimiento)
    {
        Assert.Null(PistasDeErrores.Obtener(numero, estado, procedimiento));
    }
}
