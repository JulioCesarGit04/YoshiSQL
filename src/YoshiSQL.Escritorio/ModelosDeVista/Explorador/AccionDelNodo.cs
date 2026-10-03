using System.Windows.Input;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Opción del menú contextual de un nodo, ej. "Seleccionar las primeras 1000 filas".
/// </summary>
public sealed record AccionDelNodo(string Texto, ICommand Comando);
