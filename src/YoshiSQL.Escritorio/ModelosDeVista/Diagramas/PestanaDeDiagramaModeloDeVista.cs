using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Diagramas;
using YoshiSQL.Dominio.Sesion;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Diagramas;

/// <summary>
/// Diagrama de una base de datos: tablas como tarjetas y llaves foráneas como líneas.
/// La posición de cada tabla se guarda automáticamente al soltarla.
/// </summary>
public sealed partial class PestanaDeDiagramaModeloDeVista : DocumentoModeloDeVista
{
    public const double ZoomMinimo = 0.3;
    public const double ZoomMaximo = 2.0;
    private const double PasoDeZoom = 0.1;
    private const double ZoomNormal = 1.0;
    private const double EspacioExtraDelLienzo = 300;

    private readonly ServicioDeDiagramas _servicioDeDiagramas;
    private readonly IServicioDeErrores _servicioDeErrores;
    private Diagrama? _diagrama;
    private int _ultimoOrdenDeDibujo;

    public PestanaDeDiagramaModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        ServicioDeDiagramas servicioDeDiagramas,
        IServicioDeErrores servicioDeErrores)
        : base(servidor)
    {
        BaseDeDatos = baseDeDatos;
        _servicioDeDiagramas = servicioDeDiagramas;
        _servicioDeErrores = servicioDeErrores;
    }

    public string BaseDeDatos { get; }

    public override string Titulo => $"Diagrama: {BaseDeDatos}";

    public ObservableCollection<TablaDelDiagramaModeloDeVista> Tablas { get; } = [];

    public ObservableCollection<RelacionDelDiagramaModeloDeVista> Relaciones { get; } = [];

    public bool NoTieneTablas => !EstaCargando && MensajeDeError is null && Tablas.Count == 0;

    public string ZoomVisible => $"{Zoom * 100:0}%";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoTieneTablas))]
    public partial bool EstaCargando { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoTieneTablas))]
    public partial string? MensajeDeError { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ZoomVisible))]
    public partial double Zoom { get; private set; } = ZoomNormal;

    [ObservableProperty]
    public partial double AnchoDelLienzo { get; private set; }

    [ObservableProperty]
    public partial double AltoDelLienzo { get; private set; }

    public override PestanaGuardada CrearPestanaGuardada() =>
        new(TipoDePestana.Diagrama, Servidor.Perfil.Id, BaseDeDatos, Titulo, RutaDelArchivo: null, Texto: null, TieneCambiosSinGuardar: false);

    public static TamanoDeNodo MedirNodo(NodoDeTabla nodo) =>
        new(TablaDelDiagramaModeloDeVista.Ancho, TablaDelDiagramaModeloDeVista.CalcularAlto(nodo.Columnas.Count));

    [RelayCommand]
    private async Task CargarAsync()
    {
        EstaCargando = true;
        MensajeDeError = null;
        TextoDeEstado = "Cargando diagrama...";

        try
        {
            var diagrama = await _servicioDeDiagramas.CargarDiagramaAsync(Servidor, BaseDeDatos, CancellationToken.None);
            var habiaTablasSinPosicion = diagrama.Nodos.Any(nodo => !nodo.TienePosicion);

            MostrarDiagrama(diagrama);

            if (habiaTablasSinPosicion)
            {
                await GuardarDisposicionAsync();
            }
        }
        // El error se registra y se muestra dentro de la pestaña
        catch (Exception error)
        {
            MensajeDeError = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Abrir diagrama"));
            TextoDeEstado = "No se pudo cargar el diagrama.";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    /// <summary>
    /// Dibuja un diagrama ya cargado; las tablas sin posición se organizan automáticamente.
    /// </summary>
    public void MostrarDiagrama(Diagrama diagrama)
    {
        _diagrama = diagrama;
        OrganizadorDeDiagrama.OrganizarNodosSinPosicion(diagrama, MedirNodo);

        Tablas.Clear();
        Relaciones.Clear();

        var tablasPorNombre = new Dictionary<string, TablaDelDiagramaModeloDeVista>(StringComparer.OrdinalIgnoreCase);

        foreach (var nodo in diagrama.Nodos)
        {
            var tabla = new TablaDelDiagramaModeloDeVista(nodo, CrearColumnas(diagrama, nodo));
            tablasPorNombre[nodo.Tabla.NombreCompleto] = tabla;
            Tablas.Add(tabla);
        }

        foreach (var relacion in diagrama.Relaciones)
        {
            Relaciones.Add(new RelacionDelDiagramaModeloDeVista(
                relacion,
                tablasPorNombre[relacion.TablaOrigen.NombreCompleto],
                tablasPorNombre[relacion.TablaDestino.NombreCompleto]));
        }

        RecalcularTamanoDelLienzo();
        OnPropertyChanged(nameof(NoTieneTablas));
        TextoDeEstado = $"{Tablas.Count} tablas · {Relaciones.Count} relaciones";
    }

    public void MoverTabla(TablaDelDiagramaModeloDeVista tabla, double posicionX, double posicionY)
    {
        tabla.MoverA(Math.Max(0, posicionX), Math.Max(0, posicionY));
        RecalcularTamanoDelLienzo();
    }

    public void TraerAlFrente(TablaDelDiagramaModeloDeVista tabla) => tabla.OrdenDeDibujo = ++_ultimoOrdenDeDibujo;

    public Task TerminarArrastreAsync() => GuardarDisposicionAsync();

    public void CambiarZoomConRueda(double desplazamientoDeLaRueda) =>
        AplicarZoom(Zoom + (Math.Sign(desplazamientoDeLaRueda) * PasoDeZoom));

    [RelayCommand]
    private async Task ReorganizarAsync()
    {
        if (_diagrama is null)
        {
            return;
        }

        OrganizadorDeDiagrama.OrganizarTodo(_diagrama, MedirNodo);

        foreach (var tabla in Tablas)
        {
            tabla.SincronizarDesdeElNodo();
        }

        RecalcularTamanoDelLienzo();
        await GuardarDisposicionAsync();
    }

    [RelayCommand]
    private void Acercar() => AplicarZoom(Zoom + PasoDeZoom);

    [RelayCommand]
    private void Alejar() => AplicarZoom(Zoom - PasoDeZoom);

    [RelayCommand]
    private void RestablecerZoom() => AplicarZoom(ZoomNormal);

    private void AplicarZoom(double zoomDeseado) =>
        Zoom = Math.Round(Math.Clamp(zoomDeseado, ZoomMinimo, ZoomMaximo), 2);

    private async Task GuardarDisposicionAsync()
    {
        if (_diagrama is null)
        {
            return;
        }

        try
        {
            await _servicioDeDiagramas.GuardarDisposicionAsync(Servidor, _diagrama, CancellationToken.None);
        }
        catch (Exception error)
        {
            TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Guardar la posición de las tablas"));
        }
    }

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, Servidor.Perfil.NombreVisible, BaseDeDatos);

    private void RecalcularTamanoDelLienzo()
    {
        AnchoDelLienzo = Tablas.Count == 0 ? 0 : Tablas.Max(tabla => tabla.X + TablaDelDiagramaModeloDeVista.Ancho) + EspacioExtraDelLienzo;
        AltoDelLienzo = Tablas.Count == 0 ? 0 : Tablas.Max(tabla => tabla.Y + tabla.Alto) + EspacioExtraDelLienzo;
    }

    private static List<ColumnaDelDiagramaModeloDeVista> CrearColumnas(Diagrama diagrama, NodoDeTabla nodo) =>
        nodo.Columnas
            .OrderBy(columna => columna.Posicion)
            .Select(columna => new ColumnaDelDiagramaModeloDeVista(
                columna.Nombre,
                columna.TipoDeDato.Describir(),
                columna.EsLlavePrimaria,
                diagrama.EsColumnaForanea(nodo.Tabla, columna.Nombre),
                columna.AdmiteNulos))
            .ToList();
}
