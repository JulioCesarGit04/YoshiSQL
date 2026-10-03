using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

public sealed partial class ExploradorModeloDeVista : ModeloDeVistaBase
{
    private readonly FabricaDeNodos _fabricaDeNodos;

    public ExploradorModeloDeVista(FabricaDeNodos fabricaDeNodos)
    {
        _fabricaDeNodos = fabricaDeNodos;
    }

    public ObservableCollection<NodoDelArbolModeloDeVista> Servidores { get; } = [];

    [ObservableProperty]
    public partial NodoDelArbolModeloDeVista? NodoSeleccionado { get; set; }

    /// <summary>
    /// Servidor y base de datos donde se abrirá una nueva consulta: los del nodo seleccionado
    /// o, si no hay ninguno, los del primer servidor conectado.
    /// </summary>
    public ContextoDelNodo? ContextoActual =>
        NodoSeleccionado?.Contexto ?? Servidores.FirstOrDefault()?.Contexto;

    public bool TieneServidores => Servidores.Count > 0;

    public void AgregarServidor(ServidorConectado servidor)
    {
        var nodo = _fabricaDeNodos.CrearNodoDeServidor(servidor);
        Servidores.Add(nodo);
        nodo.EstaExpandido = true;
        NodoSeleccionado = nodo;
        OnPropertyChanged(nameof(TieneServidores));
    }

    /// <summary>
    /// Recarga el nodo seleccionado; si no hay ninguno, recarga todos los servidores.
    /// </summary>
    [RelayCommand]
    private async Task ActualizarAsync()
    {
        if (NodoSeleccionado?.BuscarNodoActualizable() is { } nodo)
        {
            await nodo.RecargarAsync();
            return;
        }

        foreach (var servidor in Servidores)
        {
            await servidor.RecargarAsync();
        }
    }

    /// <summary>
    /// Vuelve a leer la lista de bases de datos de un servidor, solo si el usuario ya la había desplegado.
    /// </summary>
    public async Task ActualizarBasesDeDatosAsync(ServidorConectado servidor)
    {
        var carpetaDeBasesDeDatos = BuscarNodoDeServidor(servidor)?.BuscarHijo(TipoDeNodo.CarpetaDeBasesDeDatos);

        if (carpetaDeBasesDeDatos is { HijosCargados: true })
        {
            await carpetaDeBasesDeDatos.RecargarAsync();
        }
    }

    /// <summary>
    /// Vuelve a leer las tablas de una base de datos, solo si el usuario ya había desplegado esa carpeta.
    /// </summary>
    public async Task ActualizarTablasAsync(ServidorConectado servidor, string baseDeDatos)
    {
        var carpetaDeTablas = BuscarNodoDeServidor(servidor)?
            .RecorrerDescendientesCargados()
            .FirstOrDefault(nodo => nodo.Tipo == TipoDeNodo.CarpetaDeTablas && nodo.Contexto?.BaseDeDatos == baseDeDatos);

        if (carpetaDeTablas is { HijosCargados: true })
        {
            await carpetaDeTablas.RecargarAsync();
        }
    }

    public void QuitarServidor(ServidorConectado servidor)
    {
        var nodo = BuscarNodoDeServidor(servidor);

        if (nodo is null)
        {
            return;
        }

        Servidores.Remove(nodo);
        NodoSeleccionado = null;
        OnPropertyChanged(nameof(TieneServidores));
    }

    public ContextoDelNodo? BuscarContextoDelServidor(string nombreVisibleDelServidor) =>
        Servidores
            .Select(nodo => nodo.Contexto)
            .FirstOrDefault(contexto => string.Equals(contexto?.Servidor.Perfil.NombreVisible, nombreVisibleDelServidor, StringComparison.OrdinalIgnoreCase));

    private NodoDelArbolModeloDeVista? BuscarNodoDeServidor(ServidorConectado servidor) =>
        Servidores.FirstOrDefault(nodoDeServidor => nodoDeServidor.Contexto?.Servidor == servidor);
}
