using CommunityToolkit.Mvvm.ComponentModel;

namespace YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

/// <summary>
/// Una fila dentro de la tarjeta de una tabla del diagrama.
/// </summary>
public sealed partial class ColumnaDelDiagramaModeloDeVista : ModeloDeVistaBase
{
    public ColumnaDelDiagramaModeloDeVista(string nombre, string tipoVisible, bool esLlavePrimaria, bool esLlaveForanea, bool admiteNulos)
    {
        Nombre = nombre;
        TipoVisible = tipoVisible;
        EsLlavePrimaria = esLlavePrimaria;
        EsLlaveForanea = esLlaveForanea;
        AdmiteNulos = admiteNulos;
    }

    public string Nombre { get; }

    public string TipoVisible { get; }

    public bool EsLlavePrimaria { get; }

    public bool EsLlaveForanea { get; }

    public bool AdmiteNulos { get; }

    public bool EsColumnaComun => !EsLlavePrimaria && !EsLlaveForanea;

    /// <summary>Verdadero cuando es uno de los extremos de la relación que el usuario está mirando.</summary>
    [ObservableProperty]
    public partial bool EstaResaltada { get; set; }
}
