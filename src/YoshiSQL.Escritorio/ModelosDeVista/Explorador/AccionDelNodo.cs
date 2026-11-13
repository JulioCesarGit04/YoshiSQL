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

    // Avalonia dibuja como línea separadora cualquier opción de menú cuyo texto sea un guion
    private const string TextoDeSeparador = "-";

    /// <summary>Línea que separa grupos de opciones en el menú.</summary>
    public static AccionDelNodo Separador() => new(TextoDeSeparador, new CommunityToolkit.Mvvm.Input.RelayCommand(() => { }));

    public string Texto => _obtenerTexto();

    /// <summary>Icono que acompaña a la opción en el menú; nulo en los separadores y en las opciones sin uno asignado.</summary>
    public Avalonia.Media.Geometry? Icono => Texto == TextoDeSeparador ? null : Controles.IconosDeAcciones.Buscar(Texto);

    public ICommand Comando { get; }
}
