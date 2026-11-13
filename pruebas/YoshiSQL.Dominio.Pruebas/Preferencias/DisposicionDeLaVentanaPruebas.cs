using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Dominio.Pruebas.Preferencias;

public class DisposicionDeLaVentanaPruebas
{
    [Fact]
    public void Normalizar_ValoresRazonables_NoLosCambia()
    {
        var disposicion = new DisposicionDeLaVentana { Ancho = 1500, Alto = 900, AnchoDelExplorador = 400, AltoDeResultados = 350 };

        Assert.Equal(disposicion, disposicion.Normalizar());
    }

    // El archivo se puede editar a mano o venir de un monitor distinto: nunca debe dejar la ventana inutilizable
    [Fact]
    public void Normalizar_ValoresImposibles_LosLlevaAUnRangoUsable()
    {
        var disposicion = new DisposicionDeLaVentana { Ancho = 10, Alto = double.NaN, AnchoDelExplorador = 5000, AltoDeResultados = -3 };

        var normalizada = disposicion.Normalizar();

        Assert.Equal(DisposicionDeLaVentana.AnchoMinimoDeLaVentana, normalizada.Ancho);
        Assert.Equal(DisposicionDeLaVentana.Predeterminada.Alto, normalizada.Alto);
        Assert.Equal(DisposicionDeLaVentana.AnchoMaximoDelExplorador, normalizada.AnchoDelExplorador);
        Assert.Equal(DisposicionDeLaVentana.AltoMinimoDeResultados, normalizada.AltoDeResultados);
    }

    [Fact]
    public void Normalizar_ConservaLosTamanosDeLosDialogos()
    {
        var disposicion = new DisposicionDeLaVentana
        {
            Dialogos = new Dictionary<string, TamanoDeVentana> { ["DialogoDeTexto"] = new(900, 600) }
        };

        Assert.Equal(new TamanoDeVentana(900, 600), disposicion.Normalizar().Dialogos["DialogoDeTexto"]);
    }

    [Fact]
    public void PerfilDeConexion_ConNombre_LoUsaComoTituloYDejaElServidorComoDetalle()
    {
        var perfil = new PerfilDeConexion { Servidor = "10.0.0.5", Puerto = 1500, Usuario = "sa", Nombre = "Producción" };

        Assert.Equal("Producción", perfil.Titulo);
        Assert.Equal("10.0.0.5,1500 sa", perfil.Detalle);
    }

    [Fact]
    public void PerfilDeConexion_ConGrupo_LoAnteponeAlTitulo()
    {
        var conNombre = new PerfilDeConexion { Servidor = "10.0.0.5", Nombre = "Ventas", Grupo = "Producción" };
        var sinNombre = new PerfilDeConexion { Servidor = "10.0.0.5", Grupo = "Producción" };

        Assert.Equal("Producción / Ventas", conNombre.Titulo);
        Assert.Equal("Producción / 10.0.0.5", sinNombre.Titulo);
    }

    [Fact]
    public void PerfilDeConexion_SinNombre_SeIdentificaPorElServidor()
    {
        var perfil = new PerfilDeConexion { Servidor = "localhost", Usuario = "sa", Nombre = "  " };

        Assert.Equal("localhost", perfil.Titulo);
        Assert.Equal("sa", perfil.Detalle);
    }
}
