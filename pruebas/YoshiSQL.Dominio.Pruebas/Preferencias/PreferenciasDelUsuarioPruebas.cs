using YoshiSQL.Dominio.Preferencias;

namespace YoshiSQL.Dominio.Pruebas.Preferencias;

public class PreferenciasDelUsuarioPruebas
{
    [Fact]
    public void Normalizar_FilasDebajoDelMinimo_LasSubeAlMinimo()
    {
        var preferencias = PreferenciasDelUsuario.Predeterminadas with { FilasAlSeleccionar = 0, FilasAlEditar = -5 };

        var normalizadas = preferencias.Normalizar();

        Assert.Equal(PreferenciasDelUsuario.FilasMinimas, normalizadas.FilasAlSeleccionar);
        Assert.Equal(PreferenciasDelUsuario.FilasMinimas, normalizadas.FilasAlEditar);
    }

    [Fact]
    public void Normalizar_FilasPorEncimaDelMaximo_LasBajaAlMaximo()
    {
        var preferencias = PreferenciasDelUsuario.Predeterminadas with { FilasAlSeleccionar = 999999, FilasAlEditar = 999999 };

        var normalizadas = preferencias.Normalizar();

        Assert.Equal(PreferenciasDelUsuario.FilasMaximas, normalizadas.FilasAlSeleccionar);
        Assert.Equal(PreferenciasDelUsuario.FilasMaximas, normalizadas.FilasAlEditar);
    }

    [Fact]
    public void Normalizar_FilasEnRango_LasConserva()
    {
        var preferencias = PreferenciasDelUsuario.Predeterminadas with { FilasAlSeleccionar = 500, FilasAlEditar = 100 };

        var normalizadas = preferencias.Normalizar();

        Assert.Equal(500, normalizadas.FilasAlSeleccionar);
        Assert.Equal(100, normalizadas.FilasAlEditar);
    }
}
