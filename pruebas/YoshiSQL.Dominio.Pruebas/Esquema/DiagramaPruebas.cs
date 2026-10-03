using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Pruebas.Esquema;

public class DiagramaPruebas
{
    [Fact]
    public void EsColumnaForanea_ColumnaQueApuntaAOtraTabla_DevuelveVerdadero()
    {
        var clientes = new Tabla("dbo", "Clientes");
        var pedidos = new Tabla("dbo", "Pedidos");
        var relacion = new RelacionEntreTablas(new LlaveForanea("FK_Pedidos_Clientes", pedidos, ["ClienteId"], clientes, ["Id"]));
        var diagrama = new Diagrama("Ventas", [], [relacion]);

        Assert.True(diagrama.EsColumnaForanea(pedidos, "ClienteId"));
        Assert.False(diagrama.EsColumnaForanea(pedidos, "Fecha"));
        Assert.False(diagrama.EsColumnaForanea(clientes, "Id"));
    }
}
