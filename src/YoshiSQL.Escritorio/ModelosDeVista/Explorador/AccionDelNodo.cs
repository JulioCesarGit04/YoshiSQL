using System.Windows.Input;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Opción del menú contextual de un nodo, ej. "Seleccionar las primeras 1000 filas".
/// </summary>
public sealed class AccionDelNodo
{
    private readonly Func<string> _obtenerTexto;

    public AccionDelNodo(string texto, ICommand comando)
        : this(() => texto, comando)
    {
    }

    /// <param name="obtenerTexto">
    /// Para textos que dependen de algo que puede cambiar (la cantidad de filas de las preferencias):
    /// se vuelve a calcular cada vez que se abre el menú.
    /// </param>
    public AccionDelNodo(Func<string> obtenerTexto, ICommand comando)
    {
        _obtenerTexto = obtenerTexto;
        Comando = comando;
    }

    public string Texto => _obtenerTexto();

    public ICommand Comando { get; }
}
