using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;

/// <summary>
/// Una fila de la grilla del diseñador de tablas.
/// </summary>
public sealed partial class ColumnaEnDisenoModeloDeVista : ModeloDeVistaBase
{
    public ColumnaEnDisenoModeloDeVista(IReadOnlyList<string> tiposDeDatoSugeridos, DefinicionDeColumna? columnaExistente = null)
    {
        TiposDeDatoSugeridos = tiposDeDatoSugeridos;
        NombreOriginal = columnaExistente?.NombreOriginal;
        Nombre = columnaExistente?.Nombre ?? string.Empty;
        TipoDeDato = columnaExistente?.TipoDeDato.Describir() ?? "nvarchar(50)";
        AdmiteNulos = columnaExistente?.AdmiteNulos ?? true;
        EsLlavePrimaria = columnaExistente?.EsLlavePrimaria ?? false;
        EsIdentidad = columnaExistente?.EsIdentidad ?? false;
    }

    public IReadOnlyList<string> TiposDeDatoSugeridos { get; }

    /// <summary>Nombre en la base de datos; nulo si la columna todavía no existe.</summary>
    public string? NombreOriginal { get; }

    public bool EsNueva => NombreOriginal is null;

    [ObservableProperty]
    public partial string Nombre { get; set; }

    [ObservableProperty]
    public partial string TipoDeDato { get; set; }

    [ObservableProperty]
    public partial bool AdmiteNulos { get; set; }

    [ObservableProperty]
    public partial bool EsLlavePrimaria { get; set; }

    [ObservableProperty]
    public partial bool EsIdentidad { get; set; }

    // Una llave primaria nunca admite nulos; se ajusta automáticamente para no obligar al usuario
    partial void OnEsLlavePrimariaChanged(bool value)
    {
        if (value)
        {
            AdmiteNulos = false;
        }
    }

    /// <returns>La definición, o nulo si el tipo de dato escrito no es válido.</returns>
    public DefinicionDeColumna? CrearDefinicion()
    {
        var tipoDeDato = Dominio.Esquema.TipoDeDato.Interpretar(TipoDeDato);

        return tipoDeDato is null
            ? null
            : new DefinicionDeColumna(NombreOriginal, Nombre.Trim(), tipoDeDato, AdmiteNulos, EsLlavePrimaria, EsIdentidad);
    }
}
