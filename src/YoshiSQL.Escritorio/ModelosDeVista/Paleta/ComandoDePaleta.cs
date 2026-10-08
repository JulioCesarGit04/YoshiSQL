using System.Windows.Input;

namespace YoshiSQL.Escritorio.ModelosDeVista.Paleta;

/// <summary>
/// Una acción que aparece en la paleta de comandos (nombre, atajo y el comando a ejecutar).
/// </summary>
public sealed record ComandoDePaleta(string Nombre, string Atajo, ICommand Comando);
