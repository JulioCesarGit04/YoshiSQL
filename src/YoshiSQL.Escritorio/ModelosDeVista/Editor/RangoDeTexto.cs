namespace YoshiSQL.Escritorio.ModelosDeVista.Editor;

/// <summary>
/// Posición y longitud del texto subrayado en el editor.
/// </summary>
public readonly record struct RangoDeTexto(int Inicio, int Longitud)
{
    public static readonly RangoDeTexto Vacio = new(0, 0);

    public bool EstaVacio => Longitud == 0;
}
