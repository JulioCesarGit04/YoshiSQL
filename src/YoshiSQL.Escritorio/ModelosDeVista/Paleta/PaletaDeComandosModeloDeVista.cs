using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YoshiSQL.Escritorio.ModelosDeVista.Paleta;

/// <summary>
/// Buscador de acciones: filtra la lista de comandos por lo que el usuario escribe.
/// </summary>
public sealed partial class PaletaDeComandosModeloDeVista : ModeloDeVistaBase
{
    private readonly IReadOnlyList<ComandoDePaleta> _todos;

    public PaletaDeComandosModeloDeVista(IReadOnlyList<ComandoDePaleta> comandos)
    {
        _todos = comandos;
        Actualizar();
    }

    public ObservableCollection<ComandoDePaleta> Resultados { get; } = [];

    [ObservableProperty]
    public partial string Filtro { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ComandoDePaleta? Seleccionado { get; set; }

    partial void OnFiltroChanged(string value) => Actualizar();

    public void MoverSeleccion(int direccion)
    {
        if (Resultados.Count == 0)
        {
            return;
        }

        var indice = Seleccionado is null ? -1 : Resultados.IndexOf(Seleccionado);
        indice = Math.Clamp(indice + direccion, 0, Resultados.Count - 1);
        Seleccionado = Resultados[indice];
    }

    private void Actualizar()
    {
        var filtro = Filtro.Trim();

        Resultados.Clear();

        foreach (var comando in _todos.Where(comando => filtro.Length == 0 || comando.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase)))
        {
            Resultados.Add(comando);
        }

        Seleccionado = Resultados.FirstOrDefault();
    }
}
