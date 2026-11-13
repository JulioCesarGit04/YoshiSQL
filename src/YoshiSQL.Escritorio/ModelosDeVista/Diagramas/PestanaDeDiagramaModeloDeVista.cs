using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Contratos;
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
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IEscritorDePdf _escritorDePdf;
    private Diagrama? _diagrama;
    private int _ultimoOrdenDeDibujo;
    private TablaDelDiagramaModeloDeVista? _ultimaTablaEncontrada;

    public PestanaDeDiagramaModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        ServicioDeDiagramas servicioDeDiagramas,
        IServicioDeErrores servicioDeErrores,
        IServicioDeDialogos servicioDeDialogos,
        IEscritorDePdf escritorDePdf)
        : base(servidor)
    {
        BaseDeDatos = baseDeDatos;
        _servicioDeDiagramas = servicioDeDiagramas;
        _servicioDeErrores = servicioDeErrores;
        _servicioDeDialogos = servicioDeDialogos;
        _escritorDePdf = escritorDePdf;
    }

    /// <summary>Pide a la vista desplazarse hasta una tabla (después de buscarla).</summary>
    public event EventHandler<TablaDelDiagramaModeloDeVista>? TablaEnfocada;

    /// <summary>Nombre de lo seleccionado (una relación o una tabla); nulo si no hay nada seleccionado.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HaySeleccion))]
    public partial string? TituloDeLaSeleccion { get; private set; }

    /// <summary>Explicación en palabras de lo seleccionado: de dónde sale el dato y adónde va.</summary>
    [ObservableProperty]
    public partial string? DetalleDeLaSeleccion { get; private set; }

    public bool HaySeleccion => TituloDeLaSeleccion is not null;

    /// <summary>Texto para buscar una tabla por su nombre; mientras se escribe se va seleccionando la primera que coincide.</summary>
    [ObservableProperty]
    public partial string TextoDeBusqueda { get; set; } = string.Empty;

    partial void OnTextoDeBusquedaChanged(string value)
    {
        _ultimaTablaEncontrada = null;
        BuscarSiguienteTabla();
    }

    /// <summary>Tamaño de lo dibujado, sin el espacio libre que se deja alrededor para poder mover tablas.</summary>
    public double AnchoDelContenido => Math.Max(0, AnchoDelLienzo - EspacioExtraDelLienzo);

    public double AltoDelContenido => Math.Max(0, AltoDelLienzo - EspacioExtraDelLienzo);

    /// <summary>
    /// Al tocar una línea: se destaca esa relación, sus dos tablas y las columnas que une, y se apaga todo lo demás.
    /// </summary>
    public void SeleccionarRelacion(RelacionDelDiagramaModeloDeVista relacion)
    {
        ResaltarRelaciones([relacion], tablaPrincipal: null);
        TituloDeLaSeleccion = $"{relacion.Nombre}:  {relacion.TablaOrigen.Titulo} ({string.Join(", ", relacion.ColumnasOrigen)})  →  "
            + $"{relacion.TablaDestino.Titulo} ({string.Join(", ", relacion.ColumnasDestino)})";
        DetalleDeLaSeleccion = relacion.Explicacion;
    }

    /// <summary>
    /// Al tocar una tabla: se destacan todas sus relaciones, las que salen de ella y las que llegan.
    /// </summary>
    public void SeleccionarTabla(TablaDelDiagramaModeloDeVista tabla)
    {
        var suyas = Relaciones.Where(relacion => relacion.TablaOrigen == tabla || relacion.TablaDestino == tabla).ToList();
        ResaltarRelaciones(suyas, tabla);

        var apuntaA = suyas.Where(relacion => relacion.TablaOrigen == tabla).Select(relacion => relacion.TablaDestino.Titulo).Distinct().ToList();
        var laUsan = suyas.Where(relacion => relacion.TablaDestino == tabla && relacion.TablaOrigen != tabla).Select(relacion => relacion.TablaOrigen.Titulo).Distinct().ToList();

        TituloDeLaSeleccion = tabla.Titulo;
        DetalleDeLaSeleccion =
            $"Toma datos de {DescribirLista(apuntaA)}.\n"
            + $"La usan {DescribirLista(laUsan)}.";
    }

    private static string DescribirLista(IReadOnlyList<string> tablas) =>
        tablas.Count == 0 ? "ninguna tabla" : $"{tablas.Count} tabla(s): {string.Join(", ", tablas)}";

    private void ResaltarRelaciones(IReadOnlyList<RelacionDelDiagramaModeloDeVista> destacadas, TablaDelDiagramaModeloDeVista? tablaPrincipal)
    {
        foreach (var tabla in Tablas)
        {
            tabla.QuitarResaltado();
            tabla.EstaAtenuada = true;
        }

        foreach (var relacion in Relaciones)
        {
            var estaDestacada = destacadas.Contains(relacion);
            relacion.EstaSeleccionada = estaDestacada;
            relacion.EstaAtenuada = !estaDestacada;

            if (estaDestacada)
            {
                relacion.TablaOrigen.Resaltar(relacion.ColumnasOrigen);
                relacion.TablaDestino.Resaltar(relacion.ColumnasDestino);
            }
        }

        // Una tabla sin relaciones también debe quedar destacada al tocarla
        tablaPrincipal?.Resaltar([]);
    }

    [RelayCommand]
    private void QuitarSeleccion()
    {
        foreach (var tabla in Tablas)
        {
            tabla.QuitarResaltado();
        }

        foreach (var relacion in Relaciones)
        {
            relacion.EstaSeleccionada = false;
            relacion.EstaAtenuada = false;
        }

        TituloDeLaSeleccion = null;
        DetalleDeLaSeleccion = null;
    }

    /// <summary>
    /// Selecciona la siguiente tabla cuyo nombre contiene el texto buscado y pide a la vista que la muestre.
    /// Llamarlo otra vez con el mismo texto pasa a la siguiente coincidencia.
    /// </summary>
    [RelayCommand]
    private void BuscarSiguienteTabla()
    {
        var texto = TextoDeBusqueda.Trim();

        if (texto.Length == 0)
        {
            return;
        }

        var coincidencias = Tablas.Where(tabla => tabla.Titulo.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();

        if (coincidencias.Count == 0)
        {
            TextoDeEstado = $"Ninguna tabla contiene \"{texto}\".";
            return;
        }

        var posicionAnterior = _ultimaTablaEncontrada is null ? -1 : coincidencias.IndexOf(_ultimaTablaEncontrada);
        var encontrada = coincidencias[(posicionAnterior + 1) % coincidencias.Count];

        _ultimaTablaEncontrada = encontrada;
        TraerAlFrente(encontrada);
        SeleccionarTabla(encontrada);
        TextoDeEstado = $"{coincidencias.IndexOf(encontrada) + 1} de {coincidencias.Count} tablas con \"{texto}\".";
        TablaEnfocada?.Invoke(this, encontrada);
    }

    public void EstablecerZoom(double zoom) => AplicarZoom(zoom);

    /// <summary>
    /// Guarda el diagrama tal como se ve (con las tablas donde el usuario las acomodó) en un archivo PNG.
    /// </summary>
    public async Task GuardarComoImagenAsync(byte[] png)
    {
        var ruta = await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync($"Diagrama de {BaseDeDatos}", TipoDeArchivo.ImagenPng);

        if (ruta is null)
        {
            return;
        }

        await File.WriteAllBytesAsync(ruta, png);
        TextoDeEstado = $"Diagrama guardado en {ruta}";
    }

    /// <summary>
    /// Guarda el diagrama tal como se ve en un PDF de una sola página, del tamaño del diagrama.
    /// </summary>
    public async Task GuardarComoPdfAsync(ImagenRgb imagen, double anchoEnPuntos, double altoEnPuntos)
    {
        var ruta = await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync($"Diagrama de {BaseDeDatos}", TipoDeArchivo.DocumentoPdf);

        if (ruta is null)
        {
            return;
        }

        await using (var archivo = File.Create(ruta))
        {
            await _escritorDePdf.EscribirImagenAsync(archivo, imagen, anchoEnPuntos, altoEnPuntos, CancellationToken.None);
        }

        TextoDeEstado = $"Diagrama guardado en {ruta}";
    }

    /// <summary>Un fallo al guardar el diagrama se registra y se muestra en la barra de estado.</summary>
    public void InformarErrorAlGuardar(Exception error) =>
        TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Guardar el diagrama en un archivo"));

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
    [NotifyPropertyChangedFor(nameof(AnchoDelContenido))]
    public partial double AnchoDelLienzo { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AltoDelContenido))]
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
        _ultimaTablaEncontrada = null;
        TituloDeLaSeleccion = null;
        DetalleDeLaSeleccion = null;
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
