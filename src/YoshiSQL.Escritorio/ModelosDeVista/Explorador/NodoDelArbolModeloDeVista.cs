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
        EsVisible = true;

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

    /// <summary>El filtro del explorador oculta los nodos que no coinciden ni tienen descendientes que coincidan.</summary>
    [ObservableProperty]
    public partial bool EsVisible { get; set; }

    /// <summary>
    /// Oculta los nodos ya cargados que no contengan el texto buscado (ni sus descendientes).
    /// Con texto vacío vuelve a mostrar todo. Solo afecta lo ya cargado: no consulta al servidor.
    /// </summary>
    /// <returns>true si este nodo queda visible.</returns>
    public bool AplicarFiltro(string termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            MostrarTodo();
            return true;
        }

        var coincideEste = Texto.Contains(termino, StringComparison.OrdinalIgnoreCase);
        var algunHijoCoincide = false;

        foreach (var hijo in Hijos)
        {
            if (hijo.Tipo is TipoDeNodo.Cargando)
            {
                hijo.EsVisible = true;
                continue;
            }

            if (hijo.AplicarFiltro(termino))
            {
                algunHijoCoincide = true;
            }
        }

        // Si coincide la carpeta por su nombre, se muestra su contenido completo
        if (coincideEste && !algunHijoCoincide)
        {
            foreach (var hijo in Hijos)
            {
                hijo.MostrarTodo();
            }
        }

        if (algunHijoCoincide)
        {
            EstaExpandido = true;
        }

        EsVisible = coincideEste || algunHijoCoincide;
        return EsVisible;
    }

    private void MostrarTodo()
    {
        EsVisible = true;

        foreach (var hijo in Hijos)
        {
            hijo.MostrarTodo();
        }
    }

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

    /// <summary>
    /// Todos los nodos que ya se cargaron debajo de este, sin consultar al servidor.
    /// </summary>
    public IEnumerable<NodoDelArbolModeloDeVista> RecorrerDescendientesCargados() =>
        Hijos.SelectMany(hijo => hijo.RecorrerDescendientesCargados().Prepend(hijo));

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
