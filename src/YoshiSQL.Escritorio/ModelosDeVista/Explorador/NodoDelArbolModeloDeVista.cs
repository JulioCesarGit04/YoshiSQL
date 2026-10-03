using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Un elemento del árbol del explorador. Sus hijos se cargan desde el servidor
/// solo la primera vez que el usuario lo despliega (carga perezosa).
/// </summary>
public sealed partial class NodoDelArbolModeloDeVista : ModeloDeVistaBase
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<NodoDelArbolModeloDeVista>>>? _cargarHijos;
    private bool _hijosCargados;

    public NodoDelArbolModeloDeVista(
        string texto,
        TipoDeNodo tipo,
        ContextoDelNodo? contexto,
        Func<CancellationToken, Task<IReadOnlyList<NodoDelArbolModeloDeVista>>>? cargarHijos = null)
    {
        Texto = texto;
        Tipo = tipo;
        Contexto = contexto;
        _cargarHijos = cargarHijos;

        if (PuedeTenerHijos)
        {
            // Un hijo provisional hace que el árbol muestre la flecha para desplegar
            Hijos.Add(CrearNodoDeCarga());
        }
    }

    public string Texto { get; }

    public TipoDeNodo Tipo { get; }

    public ContextoDelNodo? Contexto { get; }

    public NodoDelArbolModeloDeVista? Padre { get; private set; }

    public bool HijosCargados => _hijosCargados;

    public ObservableCollection<NodoDelArbolModeloDeVista> Hijos { get; } = [];

    public IReadOnlyList<AccionDelNodo> Acciones { get; private set; } = [];

    /// <summary>
    /// Acción que se ejecuta al hacer doble clic sobre el nodo (ej. abrir un diagrama).
    /// </summary>
    public ICommand? ComandoAlHacerDobleClic { get; private set; }

    public bool PuedeTenerHijos => _cargarHijos is not null;

    [ObservableProperty]
    public partial bool EstaExpandido { get; set; }

    public static NodoDelArbolModeloDeVista CrearNodoDeError(string mensaje) =>
        new(mensaje, TipoDeNodo.Error, contexto: null);

    public void EstablecerAcciones(params AccionDelNodo[] acciones) => Acciones = acciones;

    public void EstablecerComandoAlHacerDobleClic(ICommand comando) => ComandoAlHacerDobleClic = comando;

    /// <summary>
    /// Nodo que se recarga al pulsar "Actualizar": este mismo o, si no tiene hijos
    /// (ej. una columna), el ancestro más cercano que sí los tenga.
    /// </summary>
    public NodoDelArbolModeloDeVista? BuscarNodoActualizable() =>
        PuedeTenerHijos ? this : Padre?.BuscarNodoActualizable();

    public NodoDelArbolModeloDeVista? BuscarHijo(TipoDeNodo tipo) =>
        Hijos.FirstOrDefault(hijo => hijo.Tipo == tipo);

    public async Task RecargarAsync()
    {
        _hijosCargados = false;
        Hijos.Clear();
        Hijos.Add(CrearNodoDeCarga());

        if (EstaExpandido)
        {
            await CargarHijosAsync();
        }
        else
        {
            EstaExpandido = true;
        }
    }

    partial void OnEstaExpandidoChanged(bool value)
    {
        if (value && !_hijosCargados)
        {
            _ = CargarHijosAsync();
        }
    }

    private async Task CargarHijosAsync()
    {
        if (_cargarHijos is null)
        {
            return;
        }

        _hijosCargados = true;

        try
        {
            var hijos = await _cargarHijos(CancellationToken.None);
            ReemplazarHijos(hijos);
        }
        // La fábrica de nodos ya registró el error; aquí solo se muestra su mensaje dentro del árbol
        catch (Exception error)
        {
            _hijosCargados = false;
            ReemplazarHijos([CrearNodoDeError(error.Message)]);
        }
    }

    private void ReemplazarHijos(IReadOnlyList<NodoDelArbolModeloDeVista> hijos)
    {
        Hijos.Clear();

        foreach (var hijo in hijos)
        {
            hijo.Padre = this;
            Hijos.Add(hijo);
        }
    }

    private static NodoDelArbolModeloDeVista CrearNodoDeCarga() =>
        new("Cargando...", TipoDeNodo.Cargando, contexto: null);
}
