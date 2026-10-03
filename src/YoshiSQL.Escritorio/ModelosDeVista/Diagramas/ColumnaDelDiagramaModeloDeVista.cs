namespace YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

public sealed record ColumnaDelDiagramaModeloDeVista(
    string Nombre,
    string TipoVisible,
    bool EsLlavePrimaria,
    bool EsLlaveForanea,
    bool AdmiteNulos)
{
    public bool EsColumnaComun => !EsLlavePrimaria && !EsLlaveForanea;
}
