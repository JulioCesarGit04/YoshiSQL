using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Pruebas.Diseno;

public class ValidadorDeDefinicionDeTablaPruebas
{
    private static readonly TipoDeDato Entero = new("int");

    [Fact]
    public void Validar_TablaCorrecta_NoTieneErrores()
    {
        var tabla = new DefinicionDeTabla("dbo", "Clientes",
        [
            new(null, "Id", Entero, AdmiteNulos: false, EsLlavePrimaria: true, EsIdentidad: true),
            new(null, "Nombre", new TipoDeDato("nvarchar", 100), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false)
        ]);

        Assert.Empty(ValidadorDeDefinicionDeTabla.Validar(tabla, original: null));
    }

    [Fact]
    public void Validar_ColumnasRepetidasYLlaveConNulos_ReportaAmbosErrores()
    {
        var tabla = new DefinicionDeTabla("dbo", "T",
        [
            new(null, "Id", Entero, AdmiteNulos: true, EsLlavePrimaria: true, EsIdentidad: false),
            new(null, "id", Entero, AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false)
        ]);

        var errores = ValidadorDeDefinicionDeTabla.Validar(tabla, original: null);

        Assert.Contains(errores, error => error.Contains("repetida"));
        Assert.Contains(errores, error => error.Contains("no puede admitir nulos"));
    }

    [Fact]
    public void Validar_CambiarIdentidadDeColumnaExistente_NoSePermite()
    {
        var original = new DefinicionDeTabla("dbo", "T", [new("Id", "Id", Entero, false, true, EsIdentidad: false)]);
        var nueva = original with { Columnas = [new("Id", "Id", Entero, false, true, EsIdentidad: true)] };

        Assert.Contains(ValidadorDeDefinicionDeTabla.Validar(nueva, original), error => error.Contains("IDENTITY"));
    }
}
