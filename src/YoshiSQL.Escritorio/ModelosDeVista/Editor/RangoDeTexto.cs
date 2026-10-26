using AvaloniaEdit.Document;

namespace YoshiSQL.Escritorio.ModelosDeVista.Editor;

/// <summary>
/// Posición y longitud del texto subrayado en el editor.
/// </summary>
/// <param name="Documento">
/// Documento al que pertenece la selección. La vista del editor se reutiliza entre pestañas, así que una
/// pestaña solo debe usar una selección hecha sobre su propio documento (si no, ejecutaría un pedazo al azar).
/// </param>
public readonly record struct RangoDeTexto(int Inicio, int Longitud, TextDocument? Documento = null)
{
    public static readonly RangoDeTexto Vacio = new(0, 0);

    public bool EstaVacio => Longitud == 0;
}
