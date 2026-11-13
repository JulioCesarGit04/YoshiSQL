using YoshiSQL.Infraestructura.SqlServer;

namespace YoshiSQL.Infraestructura.Pruebas.SqlServer;

public class ReparadorDeCreateOrAlterPruebas
{
    [Theory]
    [InlineData(208, 6, "sp_Usuarios_Login", true)]
    [InlineData(208, 6, "SP_Usuarios_Login", true)]
    [InlineData(208, 6, "usp_Listar", false)]
    [InlineData(208, 1, "sp_Usuarios_Login", false)]
    [InlineData(102, 6, "sp_Usuarios_Login", false)]
    [InlineData(208, 6, null, false)]
    public void EsAlterDeUnSpQueNoEstaEnLaBase_SegunElError_SoloAceptaElCasoDeMaster(
        int numero, byte estado, string? procedimiento, bool esperado)
    {
        Assert.Equal(esperado, ReparadorDeCreateOrAlter.EsAlterDeUnSpQueNoEstaEnLaBase(numero, estado, procedimiento));
    }

    // Caso real: así empiezan los lotes del script del usuario (comentario + create or alter en minúsculas)
    [Fact]
    public void QuitarOrAlter_LoteConComentarioAntes_DejaSoloCreate()
    {
        var lote = "-- Login\ncreate or alter procedure sp_Usuarios_Login\n    @Nombre nvarchar(30)\nas\nselect 1";

        var reparado = ReparadorDeCreateOrAlter.QuitarOrAlter(lote, "sp_Usuarios_Login");

        Assert.Equal("-- Login\ncreate procedure sp_Usuarios_Login\n    @Nombre nvarchar(30)\nas\nselect 1", reparado);
    }

    [Theory]
    [InlineData("CREATE OR ALTER PROC dbo.sp_X AS SELECT 1")]
    [InlineData("CREATE OR ALTER PROCEDURE [dbo].[sp_X] AS SELECT 1")]
    [InlineData("/* nota */ CREATE OR ALTER PROCEDURE sp_x(@a int) AS SELECT 1")]
    public void QuitarOrAlter_NombreConEsquemaOCorchetes_LoReconoce(string lote)
    {
        var reparado = ReparadorDeCreateOrAlter.QuitarOrAlter(lote, "sp_X");

        Assert.NotNull(reparado);
        Assert.DoesNotContain("OR ALTER", reparado);
    }

    [Fact]
    public void QuitarOrAlter_OrAlterEnVariasLineas_ConservaLaCantidadDeLineas()
    {
        var lote = "create\r\nor\r\nalter procedure sp_X as select 1";

        var reparado = ReparadorDeCreateOrAlter.QuitarOrAlter(lote, "sp_X");

        Assert.Equal("create\r\n\r\n procedure sp_X as select 1", reparado);
    }

    [Theory]
    // El error viene de otro procedimiento, no del que define el lote
    [InlineData("create or alter procedure sp_Otro as select 1")]
    // No es CREATE OR ALTER: un ALTER a secas debe seguir fallando
    [InlineData("alter procedure sp_X as select 1")]
    // El lote no empieza creando el procedimiento: no se toca SQL dinámico ni textos
    [InlineData("exec('create or alter procedure sp_X as select 1')")]
    [InlineData("select 1; create or alter procedure sp_X as select 1")]
    public void QuitarOrAlter_LoteQueNoCreaEseProcedimiento_DevuelveNulo(string lote)
    {
        Assert.Null(ReparadorDeCreateOrAlter.QuitarOrAlter(lote, "sp_X"));
    }
}
