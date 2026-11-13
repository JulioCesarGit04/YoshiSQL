using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Diagramas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Aplicacion.Sesion;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Escritorio.ModelosDeVista.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.Diagramas;
using YoshiSQL.Aplicacion.Administracion;
using YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;
using YoshiSQL.Escritorio.ModelosDeVista.EdicionDeFilas;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;
using YoshiSQL.Escritorio.ModelosDeVista.Historial;
using YoshiSQL.Escritorio.ModelosDeVista.Favoritos;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista;

public sealed partial class VentanaPrincipalModeloDeVista : ModeloDeVistaBase, IAccionesDelExplorador, IAccionesDelHistorial, IAccionesDelDiseno, IAccionesDeFavoritos
{
    private const string PrefijoDeConsultaNueva = "SQLQuery";

    private readonly ServiciosDeConsulta _serviciosDeConsulta;
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly ServicioDeFavoritos _servicioDeFavoritos;
    private readonly ServicioDeGeneracionDeScripts _servicioDeGeneracionDeScripts;
    private readonly ServicioDeExportacionDeBaseDeDatos _servicioDeExportacionDeBaseDeDatos;
    private readonly ServicioDeArchivosSql _servicioDeArchivosSql;
    private readonly ServicioDeDiagramas _servicioDeDiagramas;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;
    private readonly ServicioDeSesion _servicioDeSesion;
    private readonly HistorialDeConsultas _historialDeConsultas;
    private readonly ServiciosDeDiseno _serviciosDeDiseno;
    private readonly ServicioDeMonitor _servicioDeMonitor;
    private readonly ServicioDeEliminacionDeBaseDeDatos _servicioDeEliminacionDeBaseDeDatos;
    private readonly Dominio.Contratos.IEscritorDePdf _escritorDePdf;
    private int _contadorDeConsultasNuevas;

    public VentanaPrincipalModeloDeVista(
        ServiciosDeConsulta serviciosDeConsulta,
        ServicioDelExplorador servicioDelExplorador,
        ServicioDeArchivosSql servicioDeArchivosSql,
        ServicioDeExportacionDeBaseDeDatos servicioDeExportacionDeBaseDeDatos,
        ServicioDeGeneracionDeScripts servicioDeGeneracionDeScripts,
        ServicioDeDiagramas servicioDeDiagramas,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDeErrores servicioDeErrores,
        IServicioDelSistemaOperativo sistemaOperativo,
        ServicioDeSesion servicioDeSesion,
        HistorialDeConsultas historialDeConsultas,
        ServicioDeFavoritos servicioDeFavoritos,
        ServiciosDeDiseno serviciosDeDiseno,
        ServicioDeMonitor servicioDeMonitor,
        ServicioDeEliminacionDeBaseDeDatos servicioDeEliminacionDeBaseDeDatos,
        ServicioDePreferencias servicioDePreferencias,
        DisposicionDeLaInterfaz disposicion,
        Dominio.Contratos.IEscritorDePdf escritorDePdf)
    {
        _escritorDePdf = escritorDePdf;
        Disposicion = disposicion;
        _servicioDeEliminacionDeBaseDeDatos = servicioDeEliminacionDeBaseDeDatos;
        _servicioDeMonitor = servicioDeMonitor;
        _serviciosDeDiseno = serviciosDeDiseno;
        _historialDeConsultas = historialDeConsultas;
        _servicioDeSesion = servicioDeSesion;
        _servicioDeErrores = servicioDeErrores;
        _sistemaOperativo = sistemaOperativo;
        _servicioDeDiagramas = servicioDeDiagramas;
        _serviciosDeConsulta = serviciosDeConsulta;
        _servicioDelExplorador = servicioDelExplorador;
        _servicioDeArchivosSql = servicioDeArchivosSql;
        _servicioDeExportacionDeBaseDeDatos = servicioDeExportacionDeBaseDeDatos;
        _servicioDeDialogos = servicioDeDialogos;

        _servicioDeGeneracionDeScripts = servicioDeGeneracionDeScripts;
        _servicioDeFavoritos = servicioDeFavoritos;

        var fabricaDeNodos = new FabricaDeNodos(servicioDelExplorador, servicioDeGeneracionDeScripts, this, servicioDeErrores, servicioDePreferencias);
        Explorador = new ExploradorModeloDeVista(fabricaDeNodos);
        Historial = new HistorialModeloDeVista(historialDeConsultas, this, servicioDeDialogos, sistemaOperativo, servicioDeErrores);
        Favoritos = new FavoritosModeloDeVista(servicioDeFavoritos, this, servicioDeErrores);
    }

    public ExploradorModeloDeVista Explorador { get; }

    /// <summary>Tamaño y visibilidad de los paneles; lo usan la ventana y las vistas de consulta.</summary>
    public DisposicionDeLaInterfaz Disposicion { get; }

    public FavoritosModeloDeVista Favoritos { get; }

    public HistorialModeloDeVista Historial { get; }

    public ObservableCollection<DocumentoModeloDeVista> Documentos { get; } = [];

    public bool HayDocumentosAbiertos => Documentos.Count > 0;

    public bool NoHayDocumentosAbiertos => Documentos.Count == 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConsultaSeleccionada), nameof(HayConsultaSeleccionada))]
    public partial DocumentoModeloDeVista? DocumentoSeleccionado { get; set; }

    /// <summary>
    /// La pestaña seleccionada si es una consulta; los botones Ejecutar, Guardar y la lista
    /// de bases de datos solo aplican a consultas.
    /// </summary>
    public PestanaDeConsultaModeloDeVista? ConsultaSeleccionada => DocumentoSeleccionado as PestanaDeConsultaModeloDeVista;

    public bool HayConsultaSeleccionada => ConsultaSeleccionada is not null;

    [RelayCommand]
    private async Task AbrirPaletaDeComandosAsync()
    {
        var elegido = await _servicioDeDialogos.MostrarPaletaDeComandosAsync(ConstruirComandosDePaleta());

        if (elegido is not null && elegido.Comando.CanExecute(null))
        {
            elegido.Comando.Execute(null);
        }
    }

    private IReadOnlyList<Paleta.ComandoDePaleta> ConstruirComandosDePaleta()
    {
        var comandos = new List<Paleta.ComandoDePaleta>
        {
            new("Nueva consulta", "Ctrl+N", NuevaConsultaCommand),
            new("Abrir archivo...", "Ctrl+O", AbrirArchivoCommand),
            new("Guardar", "Ctrl+S", GuardarCommand),
            new("Guardar como...", "Ctrl+Shift+S", GuardarComoCommand),
            new("Conectar al servidor...", string.Empty, ConectarCommand),
            new("Cerrar pestaña", "Ctrl+W", CerrarPestanaCommand),
            new("Cerrar todas las pestañas", string.Empty, CerrarTodasLasPestanasCommand),
            new("Mostrar u ocultar el explorador", "Ctrl+B", ConmutarExploradorCommand),
            new("Mostrar u ocultar el panel de resultados", "Ctrl+R", ConmutarPanelDeResultadosCommand),
            new("Fragmentos de código (Tab)...", string.Empty, MostrarFragmentosCommand),
            new("Buscar en la base de datos...", "Ctrl+Shift+O", BuscarEnLaBaseDeDatosCommand),
            new("Ver diagrama de la base actual", string.Empty, VerDiagramaDeLaBaseActualCommand),
            new("Guardar consulta como favorito...", string.Empty, GuardarComoFavoritoCommand),
            new("Preferencias...", "Ctrl+,", MostrarPreferenciasCommand),
            new("Abrir carpeta de registros", string.Empty, AbrirCarpetaDeRegistrosCommand),
            new("Acerca de YoshiSQL", string.Empty, MostrarAcercaDeCommand)
        };

        if (ConsultaSeleccionada is { } consulta)
        {
            comandos.InsertRange(0,
            [
                new Paleta.ComandoDePaleta("Ejecutar", "F5", consulta.EjecutarCommand),
                new Paleta.ComandoDePaleta("Cancelar ejecución", "Alt+Pause", consulta.CancelarCommand),
                new Paleta.ComandoDePaleta("Formatear SQL", "Ctrl+Shift+F", consulta.FormatearCommand),
                new Paleta.ComandoDePaleta("Comentar selección", string.Empty, consulta.ComentarCommand),
                new Paleta.ComandoDePaleta("Descomentar selección", string.Empty, consulta.DescomentarCommand),
                new Paleta.ComandoDePaleta("Comentar o descomentar", "Ctrl+/", consulta.AlternarComentarioCommand),
                new Paleta.ComandoDePaleta("Pasar a MAYÚSCULAS", "Ctrl+Shift+U", consulta.ConvertirAMayusculasCommand),
                new Paleta.ComandoDePaleta("Pasar a minúsculas", "Ctrl+Shift+L", consulta.ConvertirAMinusculasCommand),
                new Paleta.ComandoDePaleta("Buscar...", "Ctrl+F", consulta.BuscarCommand),
                new Paleta.ComandoDePaleta("Reemplazar...", "Ctrl+H", consulta.ReemplazarCommand),
                new Paleta.ComandoDePaleta("Mostrar plan estimado", "Ctrl+L", consulta.MostrarPlanEstimadoCommand),
                new Paleta.ComandoDePaleta("Incluir plan real", "Ctrl+M", consulta.ConmutarPlanRealCommand),
                new Paleta.ComandoDePaleta("Resultados en texto", "Ctrl+T", consulta.ConmutarResultadosEnTextoCommand)
            ]);
        }

        return comandos;
    }

    [RelayCommand]
    private async Task ConectarAsync()
    {
        var servidor = await _servicioDeDialogos.MostrarDialogoDeConexionAsync();

        if (servidor is null)
        {
            return;
        }

        Explorador.AgregarServidor(servidor);

        if (NoHayDocumentosAbiertos)
        {
            await AbrirNuevaConsultaAsync(new ContextoDelNodo(servidor), string.Empty, ejecutarAlAbrir: false);
        }
    }

    [RelayCommand]
    private async Task NuevaConsultaAsync()
    {
        var contexto = await ObtenerOSolicitarContextoAsync();

        if (contexto is not null)
        {
            await AbrirNuevaConsultaAsync(contexto, string.Empty, ejecutarAlAbrir: false);
        }
    }

    [RelayCommand]
    private async Task AbrirArchivoAsync()
    {
        var rutaDelArchivo = await _servicioDeDialogos.SeleccionarArchivoParaAbrirAsync();

        if (rutaDelArchivo is null)
        {
            return;
        }

        var contexto = await ObtenerOSolicitarContextoAsync();

        if (contexto is null)
        {
            return;
        }

        try
        {
            var contenido = await _servicioDeArchivosSql.AbrirAsync(rutaDelArchivo, CancellationToken.None);
            var pestana = await AgregarPestanaAsync(contexto, contenido);
            pestana.MarcarComoGuardado(rutaDelArchivo);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir archivo"));
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (ConsultaSeleccionada is { } pestana)
        {
            await GuardarPestanaAsync(pestana, pedirUbicacion: pestana.RutaDelArchivo is null);
        }
    }

    [RelayCommand]
    private async Task GuardarComoAsync()
    {
        if (ConsultaSeleccionada is { } pestana)
        {
            await GuardarPestanaAsync(pestana, pedirUbicacion: true);
        }
    }

    [RelayCommand]
    private async Task AbrirCarpetaDeRegistrosAsync()
    {
        try
        {
            _sistemaOperativo.AbrirCarpetaDeRegistros();
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir carpeta de registros"));
        }
    }

    [RelayCommand]
    private Task MostrarAcercaDeAsync() => _servicioDeDialogos.MostrarAcercaDeAsync();

    [RelayCommand]
    private Task MostrarFragmentosAsync() =>
        _servicioDeDialogos.MostrarTextoAsync("Fragmentos de código", Controles.FragmentosDeTSql.DescribirTodos());

    /// <summary>
    /// Busca un texto en toda la base (nombres de objetos, columnas y código) y abre lo que el usuario elija.
    /// El filtro del explorador solo ve lo que ya se desplegó en el árbol; esto consulta el catálogo completo.
    /// </summary>
    [RelayCommand]
    private async Task BuscarEnLaBaseDeDatosAsync()
    {
        const string Titulo = "Buscar en la base de datos";

        var contexto = ConsultaSeleccionada is { } consulta
            ? new ContextoDelNodo(consulta.Servidor, consulta.BaseDeDatosActual)
            : await ObtenerOSolicitarContextoAsync();

        if (contexto is null)
        {
            return;
        }

        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;
        var texto = await _servicioDeDialogos.PedirTextoAsync(
            Titulo,
            $"Texto a buscar en \"{baseDeDatos}\": nombre de tabla, columna, procedimiento, o algo escrito dentro del código.",
            string.Empty);

        if (string.IsNullOrWhiteSpace(texto))
        {
            return;
        }

        try
        {
            var encontrados = await _servicioDelExplorador.BuscarObjetosAsync(contexto.Servidor, baseDeDatos, texto, CancellationToken.None);

            if (encontrados.Count == 0)
            {
                await _servicioDeDialogos.MostrarInformacionAsync(Titulo, $"No se encontró \"{texto}\" en la base \"{baseDeDatos}\".");
                return;
            }

            var contextoDeLaBase = contexto.EnBaseDeDatos(baseDeDatos);
            var opciones = encontrados
                .Select(encontrado => new Paleta.ComandoDePaleta(
                    DescribirEncontrado(encontrado),
                    DescribirTipo(encontrado),
                    new AsyncRelayCommand(() => AbrirEncontradoAsync(contextoDeLaBase, encontrado.Objeto))))
                .ToList();

            var elegido = await _servicioDeDialogos.MostrarPaletaDeComandosAsync(opciones);
            elegido?.Comando.Execute(null);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError(Titulo, contexto.Servidor.Perfil.NombreVisible, baseDeDatos));
        }
    }

    private static string DescribirEncontrado(ObjetoEncontrado encontrado) => encontrado.Coincidencia switch
    {
        TipoDeCoincidencia.EnUnaColumna => $"{encontrado.Objeto.NombreCompleto}.{encontrado.Columna}",
        _ => encontrado.Objeto.NombreCompleto
    };

    private static string DescribirTipo(ObjetoEncontrado encontrado)
    {
        var tipo = encontrado.Objeto.Tipo switch
        {
            TipoDeObjeto.Tabla => "tabla",
            TipoDeObjeto.Vista => "vista",
            TipoDeObjeto.ProcedimientoAlmacenado => "procedimiento",
            _ => "función"
        };

        return encontrado.Coincidencia switch
        {
            TipoDeCoincidencia.EnUnaColumna => $"columna de {tipo}",
            TipoDeCoincidencia.EnElCodigo => $"dentro del código · {tipo}",
            _ => tipo
        };
    }

    /// <summary>
    /// Una tabla se abre mostrando sus primeras filas; lo demás, con su código listo para modificar.
    /// </summary>
    private async Task AbrirEncontradoAsync(ContextoDelNodo contexto, ObjetoDeEsquema objeto)
    {
        try
        {
            if (objeto is Tabla)
            {
                var seleccion = _servicioDeGeneracionDeScripts.GenerarSeleccionDeFilas(contexto.BaseDeDatosOPredeterminada, objeto);
                await AbrirNuevaConsultaAsync(contexto, seleccion, ejecutarAlAbrir: true);
                return;
            }

            var codigo = await _servicioDeGeneracionDeScripts.GenerarModificacionAsync(
                contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, CancellationToken.None);
            await AbrirNuevaConsultaAsync(contexto, codigo, ejecutarAlAbrir: false);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir el objeto encontrado", contexto.Servidor.Perfil.NombreVisible, contexto.BaseDeDatos));
        }
    }

    /// <summary>
    /// Abre el diagrama de la base de la pestaña actual, sin tener que buscarla en el árbol del explorador.
    /// </summary>
    [RelayCommand]
    private async Task VerDiagramaDeLaBaseActualAsync()
    {
        var contexto = DocumentoSeleccionado switch
        {
            PestanaDeConsultaModeloDeVista consulta => new ContextoDelNodo(consulta.Servidor, consulta.BaseDeDatosActual),
            _ => await ObtenerOSolicitarContextoAsync()
        };

        if (contexto is null)
        {
            return;
        }

        try
        {
            await AbrirDiagramaAsync(contexto);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Abrir diagrama", contexto.Servidor.Perfil.NombreVisible, contexto.BaseDeDatos));
        }
    }

    [RelayCommand]
    private void ConmutarExplorador() => Disposicion.ExploradorVisible = !Disposicion.ExploradorVisible;

    [RelayCommand]
    private void ConmutarPanelDeResultados() => Disposicion.ResultadosVisibles = !Disposicion.ResultadosVisibles;

    [RelayCommand]
    private Task CerrarOtrasPestanasAsync(DocumentoModeloDeVista? conservada) => CerrarVariasPestanasAsync(conservada);

    [RelayCommand]
    private Task CerrarTodasLasPestanasAsync() => CerrarVariasPestanasAsync(conservada: null);

    /// <summary>
    /// Cierra las pestañas una por una preguntando por los cambios sin guardar; si el usuario cancela alguna, se detiene.
    /// </summary>
    private async Task CerrarVariasPestanasAsync(DocumentoModeloDeVista? conservada)
    {
        foreach (var documento in Documentos.Where(documento => documento != conservada).ToList())
        {
            DocumentoSeleccionado = documento;

            if (!await ConfirmarCierreAsync(documento))
            {
                return;
            }

            await QuitarDocumentoAsync(documento);
        }

        if (conservada is not null)
        {
            DocumentoSeleccionado = conservada;
        }
    }

    [RelayCommand]
    private Task MostrarPreferenciasAsync() => _servicioDeDialogos.MostrarPreferenciasAsync();

    [RelayCommand]
    private async Task CerrarPestanaAsync(DocumentoModeloDeVista? documento)
    {
        documento ??= DocumentoSeleccionado;

        if (documento is not null && await ConfirmarCierreAsync(documento))
        {
            await QuitarDocumentoAsync(documento);
        }
    }

    /// <summary>
    /// Pregunta por los cambios sin guardar de cada pestaña antes de cerrar la aplicación.
    /// </summary>
    /// <returns>true si la aplicación puede cerrarse.</returns>
    public async Task<bool> PrepararCierreAsync()
    {
        foreach (var documento in Documentos.ToList())
        {
            DocumentoSeleccionado = documento;

            if (!await ConfirmarCierreAsync(documento))
            {
                return false;
            }
        }

        await CerrarSesionCorrectamenteAsync();

        // Un fallo al liberar una conexión no debe impedir cerrar la aplicación
        foreach (var documento in Documentos)
        {
            try
            {
                await documento.DisposeAsync();
            }
            catch (Exception error)
            {
                _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Cerrar pestaña al salir"));
            }
        }

        return true;
    }

    public async Task AbrirNuevaConsultaAsync(ContextoDelNodo contexto, string textoInicial, bool ejecutarAlAbrir)
    {
        var pestana = await AgregarPestanaAsync(contexto, textoInicial);

        if (ejecutarAlAbrir)
        {
            await pestana.EjecutarCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Abre el diagrama de la base de datos; si ya está abierto, solo lo selecciona.
    /// </summary>
    public async Task AbrirDiagramaAsync(ContextoDelNodo contexto)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;
        var diagramaAbierto = Documentos
            .OfType<PestanaDeDiagramaModeloDeVista>()
            .FirstOrDefault(diagrama => diagrama.Servidor == contexto.Servidor && diagrama.BaseDeDatos == baseDeDatos);

        if (diagramaAbierto is not null)
        {
            DocumentoSeleccionado = diagramaAbierto;
            return;
        }

        var diagrama = new PestanaDeDiagramaModeloDeVista(
            contexto.Servidor, baseDeDatos, _servicioDeDiagramas, _servicioDeErrores, _servicioDeDialogos, _escritorDePdf);
        AgregarDocumento(diagrama);
        await diagrama.CargarCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Usa el servidor donde se ejecutó la consulta si sigue conectado; si no, el servidor actual.
    /// </summary>
    public async Task AbrirFavoritoAsync(Dominio.Consultas.ConsultaFavorita favorito)
    {
        var contexto = Explorador.ContextoActual ?? await ObtenerOSolicitarContextoAsync();

        if (contexto is not null)
        {
            await AbrirNuevaConsultaAsync(contexto, favorito.Sql, ejecutarAlAbrir: false);
        }
    }

    [RelayCommand]
    private async Task GuardarComoFavoritoAsync()
    {
        if (ConsultaSeleccionada?.Documento.Text.Trim() is not { Length: > 0 } sql)
        {
            return;
        }

        var nombre = await _servicioDeDialogos.PedirTextoAsync("Guardar como favorito", "Nombre del favorito:", string.Empty);

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            await _servicioDeFavoritos.AgregarAsync(nombre.Trim(), sql);
        }
    }

    public async Task AbrirConsultaDelHistorialAsync(ConsultaEjecutada consulta)
    {
        if (Explorador.BuscarContextoDelServidor(consulta.Servidor) is { } contextoDelMismoServidor)
        {
            await AbrirNuevaConsultaAsync(contextoDelMismoServidor with { BaseDeDatos = consulta.BaseDeDatos }, consulta.Texto, ejecutarAlAbrir: false);
            return;
        }

        // El servidor de esa consulta no está conectado: se abre en el actual, pero SIN forzar la base del historial.
        // Antes se usaba el nombre de esa base en otro servidor: o no existía, o (peor) existía y era otra distinta.
        if (await ObtenerOSolicitarContextoAsync() is { } contexto)
        {
            await AbrirNuevaConsultaAsync(contexto, consulta.Texto, ejecutarAlAbrir: false);
        }
    }

    public async Task AbrirDisenadorDeTablaAsync(ContextoDelNodo contexto, Tabla? tabla)
    {
        var disenador = new PestanaDeDisenoDeTablaModeloDeVista(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, _serviciosDeDiseno, this);

        AgregarDocumento(disenador);
        await disenador.CargarCommand.ExecuteAsync(null);
    }

    public async Task AbrirEdicionDeFilasAsync(ContextoDelNodo contexto, Tabla tabla)
    {
        var edicion = new PestanaDeEdicionDeFilasModeloDeVista(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, _serviciosDeDiseno);

        AgregarDocumento(edicion);
        await edicion.CargarCommand.ExecuteAsync(null);
    }

    public async Task MostrarPropiedadesDeTablaAsync(ContextoDelNodo contexto, Tabla tabla)
    {
        var propiedades = await _servicioDelExplorador.ObtenerPropiedadesDeTablaAsync(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, CancellationToken.None);

        await _servicioDeDialogos.MostrarTextoAsync(
            $"Propiedades de {tabla.NombreCompleto}",
            DescriptorDePropiedades.Describir(tabla, propiedades));
    }

    public async Task MostrarPropiedadesDeBaseDeDatosAsync(ContextoDelNodo contexto)
    {
        var propiedades = await _servicioDelExplorador.ObtenerPropiedadesDeBaseDeDatosAsync(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, CancellationToken.None);

        await _servicioDeDialogos.MostrarTextoAsync(
            $"Propiedades de {propiedades.Nombre}",
            DescriptorDePropiedades.Describir(propiedades));
    }

    public async Task MostrarDependenciasAsync(ContextoDelNodo contexto, Dominio.Esquema.ObjetoDeEsquema objeto)
    {
        var dependencias = await _servicioDelExplorador.ObtenerDependenciasAsync(
            contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, CancellationToken.None);

        await _servicioDeDialogos.MostrarTextoAsync(
            $"Dependencias de {objeto.NombreCompleto}",
            DescriptorDePropiedades.Describir(objeto, dependencias));
    }

    public async Task RenombrarObjetoAsync(ContextoDelNodo contexto, Dominio.Esquema.ObjetoDeEsquema objeto)
    {
        var nuevoNombre = await _servicioDeDialogos.PedirTextoAsync(
            "Renombrar", $"Nuevo nombre para {objeto.NombreCompleto}:", objeto.Nombre);

        if (string.IsNullOrWhiteSpace(nuevoNombre) || nuevoNombre == objeto.Nombre)
        {
            return;
        }

        var script = _servicioDeGeneracionDeScripts.GenerarRenombrado(contexto.BaseDeDatosOPredeterminada, objeto, nuevoNombre);
        await AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: true);
    }

    public async Task AbrirSaludAsync(ContextoDelNodo contexto)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;

        var saludAbierta = Documentos
            .OfType<Salud.PestanaDeSaludModeloDeVista>()
            .FirstOrDefault(salud => salud.Servidor == contexto.Servidor && salud.BaseDeDatos == baseDeDatos);

        if (saludAbierta is not null)
        {
            DocumentoSeleccionado = saludAbierta;
            return;
        }

        var salud = new Salud.PestanaDeSaludModeloDeVista(
            contexto.Servidor, baseDeDatos, _servicioDelExplorador, _servicioDeErrores);

        AgregarDocumento(salud);
        await salud.CargarCommand.ExecuteAsync(null);
    }

    public async Task AbrirMonitorDeActividadAsync(ContextoDelNodo contexto)
    {
        var monitorAbierto = Documentos
            .OfType<PestanaDeMonitorModeloDeVista>()
            .FirstOrDefault(monitor => monitor.Servidor == contexto.Servidor);

        if (monitorAbierto is not null)
        {
            DocumentoSeleccionado = monitorAbierto;
            return;
        }

        var monitor = new PestanaDeMonitorModeloDeVista(contexto.Servidor, _servicioDeMonitor, _servicioDeDialogos, _servicioDeErrores);
        AgregarDocumento(monitor);
        await monitor.IniciarAsync();
    }

    public Task MostrarRespaldoAsync(ContextoDelNodo contexto) =>
        _servicioDeDialogos.MostrarRespaldoAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada);

    public async Task ExportarBaseDeDatosAsync(ContextoDelNodo contexto)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;

        var opciones = await _servicioDeDialogos.PedirOpcionesDeExportacionAsync(baseDeDatos);

        if (opciones is null)
        {
            return;
        }

        var ruta = await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync(baseDeDatos, TipoDeArchivo.ScriptSql);

        if (ruta is null)
        {
            return;
        }

        IReadOnlyList<string> advertencias = [];

        // Se escribe directo al archivo, con avance y posibilidad de cancelar: una base grande no cabe en memoria
        // y antes la aplicación parecía colgada hasta terminar
        var llegoAlFinal = await _servicioDeDialogos.EjecutarConProgresoAsync(
            $"Exportando {baseDeDatos}",
            async (avance, tokenDeCancelacion) =>
            {
                // Con BOM: sin él, sqlcmd y SSMS (según su configuración) leen el archivo como ANSI y las tildes y eñes
                // de los datos llegan corruptas a la base de destino (N'año' -> N'aÃ±o')
                await using var archivo = new StreamWriter(ruta, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                advertencias = await _servicioDeExportacionDeBaseDeDatos.ExportarAsync(
                    contexto.Servidor, baseDeDatos, opciones, archivo, avance, tokenDeCancelacion);
            });

        if (!llegoAlFinal)
        {
            // Un script a medias parecería una exportación válida: se borra
            File.Delete(ruta);
            await _servicioDeDialogos.MostrarInformacionAsync("Exportación cancelada", "Se canceló la exportación y no se guardó ningún archivo.");
            return;
        }

        var mensaje = $"La base de datos \"{baseDeDatos}\" se exportó a:\n{ruta}";

        if (advertencias.Count > 0)
        {
            mensaje += $"\n\nAtención: {advertencias.Count} elemento(s) no se exportaron completos "
                + "(también quedó anotado en el script):\n- " + string.Join("\n- ", advertencias);
        }

        await _servicioDeDialogos.MostrarInformacionAsync(
            advertencias.Count > 0 ? "Exportación con advertencias" : "Exportación completa",
            mensaje);
    }

    public async Task EliminarBaseDeDatosAsync(ContextoDelNodo contexto)
    {
        const string Titulo = "Eliminar base de datos";
        var servidor = contexto.Servidor;
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;

        if (ServicioDeEliminacionDeBaseDeDatos.EsBaseDelSistema(baseDeDatos))
        {
            await _servicioDeDialogos.MostrarInformacionAsync(Titulo, $"\"{baseDeDatos}\" es una base de datos del sistema y no se puede eliminar.");
            return;
        }

        if (await _servicioDeDialogos.PedirOpcionesDeEliminacionAsync(baseDeDatos) is not { } opciones)
        {
            return;
        }

        // Las pestañas de consulta conservan su propia conexión en la base: se cierran antes del DROP para que
        // no lo bloqueen y, después, abren una conexión nueva en master en lugar de fallar contra una base inexistente
        var consultasEnLaBase = Documentos
            .OfType<PestanaDeConsultaModeloDeVista>()
            .Where(pestana => pestana.Servidor == servidor
                && string.Equals(pestana.BaseDeDatosActual, baseDeDatos, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var pestana in consultasEnLaBase)
        {
            await pestana.SalirDeLaBaseDeDatosAsync();
        }

        await _servicioDeEliminacionDeBaseDeDatos.EliminarAsync(servidor, baseDeDatos, opciones, CancellationToken.None);

        _serviciosDeConsulta.Autocompletado.InvalidarCatalogo(servidor, baseDeDatos);
        await Explorador.ActualizarBasesDeDatosAsync(servidor);

        foreach (var pestana in Documentos.OfType<PestanaDeConsultaModeloDeVista>().Where(pestana => pestana.Servidor == servidor))
        {
            await pestana.CargarBasesDeDatosAsync();
        }

        await _servicioDeDialogos.MostrarInformacionAsync(Titulo, $"La base de datos \"{baseDeDatos}\" se eliminó correctamente.");
    }

    public async Task MostrarRestauracionAsync(ContextoDelNodo contexto)
    {
        if (await _servicioDeDialogos.MostrarRestauracionAsync(contexto.Servidor))
        {
            await Explorador.ActualizarBasesDeDatosAsync(contexto.Servidor);
        }
    }

    public Task AbrirScriptEnConsultaAsync(ServidorConectado servidor, string baseDeDatos, string script) =>
        AbrirNuevaConsultaAsync(new ContextoDelNodo(servidor, baseDeDatos), script, ejecutarAlAbrir: false);

    public async Task NotificarTablasModificadasAsync(ServidorConectado servidor, string baseDeDatos)
    {
        _serviciosDeConsulta.Autocompletado.InvalidarCatalogo(servidor, baseDeDatos);
        await Explorador.ActualizarTablasAsync(servidor, baseDeDatos);
    }

    public async Task DesconectarServidorAsync(ContextoDelNodo contexto)
    {
        var servidor = contexto.Servidor;
        var documentosDelServidor = Documentos.Where(documento => documento.Servidor == servidor).ToList();

        // Primero se confirma cada pestaña; si el usuario cancela alguna, no se desconecta
        foreach (var documento in documentosDelServidor)
        {
            DocumentoSeleccionado = documento;

            if (!await ConfirmarCierreAsync(documento))
            {
                return;
            }
        }

        // Cerrar las pestañas libera su conexión (cada consulta tiene su propia sesión abierta)
        foreach (var documento in documentosDelServidor)
        {
            await QuitarDocumentoAsync(documento);
        }

        Explorador.QuitarServidor(servidor);
    }

    /// <param name="nombreDelArchivo">Nombre a usar; si es nulo se genera uno nuevo (SQLQuery1.sql, SQLQuery2.sql...).</param>
    private async Task<PestanaDeConsultaModeloDeVista> AgregarPestanaAsync(
        ContextoDelNodo contexto,
        string textoInicial,
        string? nombreDelArchivo = null)
    {
        var pestana = new PestanaDeConsultaModeloDeVista(
            contexto.Servidor,
            contexto.BaseDeDatosOPredeterminada,
            nombreDelArchivo ?? GenerarNombreDeConsultaNueva(),
            _serviciosDeConsulta);

        pestana.CargarTexto(textoInicial);
        pestana.BasesDeDatosModificadas += ActualizarBasesDeDatosDelExplorador;
        AgregarDocumento(pestana);

        await pestana.CargarBasesDeDatosAsync();
        return pestana;
    }

    private string GenerarNombreDeConsultaNueva() => $"{PrefijoDeConsultaNueva}{++_contadorDeConsultasNuevas}.sql";

    private void AgregarDocumento(DocumentoModeloDeVista documento)
    {
        Documentos.Add(documento);
        DocumentoSeleccionado = documento;
        NotificarCambioDeDocumentos();
    }

    private async Task QuitarDocumentoAsync(DocumentoModeloDeVista documento)
    {
        var indice = Documentos.IndexOf(documento);

        if (documento is PestanaDeConsultaModeloDeVista pestana)
        {
            pestana.BasesDeDatosModificadas -= ActualizarBasesDeDatosDelExplorador;
        }

        Documentos.Remove(documento);

        if (DocumentoSeleccionado is null || DocumentoSeleccionado == documento)
        {
            DocumentoSeleccionado = Documentos.Count == 0 ? null : Documentos[Math.Min(indice, Documentos.Count - 1)];
        }

        NotificarCambioDeDocumentos();
        await documento.DisposeAsync();
    }

    private async void ActualizarBasesDeDatosDelExplorador(object? remitente, EventArgs argumentos)
    {
        if (remitente is not PestanaDeConsultaModeloDeVista pestana)
        {
            return;
        }

        // Manejador de evento asíncrono: los errores se registran y se avisan en lugar de perderse
        try
        {
            await Explorador.ActualizarBasesDeDatosAsync(pestana.Servidor);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(
                error,
                new ContextoDeError("Actualizar el explorador", pestana.Servidor.Perfil.NombreVisible));
        }
    }

    /// <summary>
    /// Solo las consultas con cambios sin guardar piden confirmación; los diagramas se guardan solos.
    /// </summary>
    private async Task<bool> ConfirmarCierreAsync(DocumentoModeloDeVista documento)
    {
        if (documento is PestanaDeConsultaModeloDeVista consulta
            && await consulta.ObtenerAdvertenciaDeCierreAsync() is { } advertencia
            && !await _servicioDeDialogos.ConfirmarAsync("Cerrar pestaña", advertencia, "Cerrar de todos modos", "Cancelar"))
        {
            return false;
        }

        if (documento.TieneCambiosSinAplicar)
        {
            return await _servicioDeDialogos.ConfirmarAsync(
                "Cambios sin aplicar",
                $"\"{documento.Titulo.TrimEnd(' ', '*')}\" tiene cambios que no se aplicaron. ¿Deseas descartarlos?",
                "Descartar",
                "Cancelar");
        }

        if (documento is not PestanaDeConsultaModeloDeVista { TieneCambiosSinGuardar: true } pestana)
        {
            return true;
        }

        var respuesta = await _servicioDeDialogos.PreguntarSiGuardarCambiosAsync(pestana.NombreDelArchivo);

        return respuesta switch
        {
            RespuestaAlCerrar.Guardar => await GuardarPestanaAsync(pestana, pedirUbicacion: pestana.RutaDelArchivo is null),
            RespuestaAlCerrar.NoGuardar => true,
            _ => false
        };
    }

    /// <returns>true si el archivo quedó guardado.</returns>
    private async Task<bool> GuardarPestanaAsync(PestanaDeConsultaModeloDeVista pestana, bool pedirUbicacion)
    {
        var rutaDelArchivo = pedirUbicacion
            ? await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync(pestana.NombreDelArchivo, TipoDeArchivo.ScriptSql)
            : pestana.RutaDelArchivo;

        if (rutaDelArchivo is null)
        {
            return false;
        }

        try
        {
            var rutaFinal = await _servicioDeArchivosSql.GuardarAsync(rutaDelArchivo, pestana.Documento.Text, CancellationToken.None);
            pestana.MarcarComoGuardado(rutaFinal);
            return true;
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Guardar archivo"));
            return false;
        }
    }

    /// <summary>
    /// Si no hay ningún servidor conectado, abre la ventana de conexión primero.
    /// </summary>
    private async Task<ContextoDelNodo?> ObtenerOSolicitarContextoAsync()
    {
        if (Explorador.ContextoActual is { } contexto)
        {
            return contexto;
        }

        var servidor = await _servicioDeDialogos.MostrarDialogoDeConexionAsync();

        if (servidor is null)
        {
            return null;
        }

        Explorador.AgregarServidor(servidor);
        return new ContextoDelNodo(servidor);
    }

    private void NotificarCambioDeDocumentos()
    {
        OnPropertyChanged(nameof(HayDocumentosAbiertos));
        OnPropertyChanged(nameof(NoHayDocumentosAbiertos));
    }
}
