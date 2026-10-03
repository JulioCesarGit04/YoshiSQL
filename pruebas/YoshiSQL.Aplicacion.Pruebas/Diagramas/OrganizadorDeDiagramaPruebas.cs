using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Pruebas.Diagramas;

public class OrganizadorDeDiagramaPruebas
{
    private static readonly TamanoDeNodo TamanoFijo = new(200, 120);

    [Fact]
    public void OrganizarTodo_VariasTablas_NingunaSeEncima()
    {
        var diagrama = CrearDiagrama(cantidadDeTablas: 10);

        OrganizadorDeDiagrama.OrganizarTodo(diagrama, _ => TamanoFijo);

        Assert.All(diagrama.Nodos, nodo => Assert.True(nodo.TienePosicion));
        foreach (var nodo in diagrama.Nodos)
        {
            foreach (var otroNodo in diagrama.Nodos.Where(otro => otro != nodo))
            {
                Assert.False(SeEnciman(nodo.Posicion!.Value, otroNodo.Posicion!.Value), $"{nodo.Tabla.Nombre} se encima con {otroNodo.Tabla.Nombre}");
            }
        }
    }

    [Fact]
    public void OrganizarTodo_TablasRelacionadas_QuedanContiguas()
    {
        var clientes = new Tabla("dbo", "Clientes");
        var pedidos = new Tabla("dbo", "Pedidos");
        var aislada = new Tabla("dbo", "Aislada");
        var relacion = new RelacionEntreTablas(new LlaveForanea("FK_Pedidos_Clientes", pedidos, ["ClienteId"], clientes, ["Id"]));
        var diagrama = new Diagrama("Ventas", [CrearNodo(aislada), CrearNodo(clientes), CrearNodo(pedidos)], [relacion]);

        OrganizadorDeDiagrama.OrganizarTodo(diagrama, _ => TamanoFijo);

        var ordenEnPantalla = diagrama.Nodos
            .OrderBy(nodo => nodo.Posicion!.Value.Y)
            .ThenBy(nodo => nodo.Posicion!.Value.X)
            .Select(nodo => nodo.Tabla.Nombre)
            .ToList();
        Assert.Equal(1, Math.Abs(ordenEnPantalla.IndexOf("Clientes") - ordenEnPantalla.IndexOf("Pedidos")));
    }

    [Fact]
    public void OrganizarNodosSinPosicion_RespetaLasPosicionesGuardadas()
    {
        var diagrama = CrearDiagrama(cantidadDeTablas: 3);
        var posicionGuardada = new PosicionEnElDiagrama(500, 300);
        diagrama.Nodos[0].Posicion = posicionGuardada;

        OrganizadorDeDiagrama.OrganizarNodosSinPosicion(diagrama, _ => TamanoFijo);

        Assert.Equal(posicionGuardada, diagrama.Nodos[0].Posicion);
        Assert.All(diagrama.Nodos.Skip(1), nodo => Assert.True(nodo.Posicion!.Value.Y > posicionGuardada.Y + TamanoFijo.Alto));
    }

    private static bool SeEnciman(PosicionEnElDiagrama primera, PosicionEnElDiagrama segunda) =>
        primera.X < segunda.X + TamanoFijo.Ancho && segunda.X < primera.X + TamanoFijo.Ancho
        && primera.Y < segunda.Y + TamanoFijo.Alto && segunda.Y < primera.Y + TamanoFijo.Alto;

    private static Diagrama CrearDiagrama(int cantidadDeTablas) =>
        new("Prueba", Enumerable.Range(1, cantidadDeTablas).Select(numero => CrearNodo(new Tabla("dbo", $"Tabla{numero}"))).ToList(), []);

    private static NodoDeTabla CrearNodo(Tabla tabla) =>
        new(tabla, [new Columna("Id", new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true, Posicion: 1)]);
}
