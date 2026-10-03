using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.DisenoDeTablas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;

/// <summary>
/// Diseñador visual: crear una tabla nueva o cambiar las columnas de una existente.
/// </summary>
public sealed partial class PestanaDeDisenoDeTablaModeloDeVista : DocumentoModeloDeVista
{
    private const string EsquemaPredeterminado = "dbo";
    private const string NombrePredeterminadoDeTablaNueva = "NuevaTabla";

    private readonly ServicioDeDisenoDeTablas _servicioDeDiseno;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly IAccionesDelDiseno _acciones;
    private DefinicionDeTabla? _definicionOriginal;
    private bool _hayCambiosSinAplicar;

    /// <param name="tablaExistente">Tabla a modificar; nula para crear una tabla nueva.</param>
    public PestanaDeDisenoDeTablaModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla? tablaExistente,
        ServiciosDeDiseno servicios,
        IAccionesDelDiseno acciones)
        : base(servidor)
    {
        BaseDeDatos = baseDeDatos;
        TablaExistente = tablaExistente;
        _servicioDeDiseno = servicios.DisenoDeTablas;
        _servicioDeDialogos = servicios.Dialogos;
        _servicioDeErrores = servicios.Errores;
        _acciones = acciones;

        Esquema = tablaExistente?.Esquema ?? EsquemaPredeterminado;
        NombreDeLaTabla = tablaExistente?.Nombre ?? NombrePredeterminadoDeTablaNueva;
        Columnas.CollectionChanged += AlCambiarLasColumnas;
    }

    public string BaseDeDatos { get; }

    public Tabla? TablaExistente { get; }

    public bool EsTablaNueva => TablaExistente is null;

    public override string Titulo
    {
        get
        {
            var nombre = EsTablaNueva ? $"Nueva tabla ({BaseDeDatos})" : $"Diseño: {TablaExistente!.NombreCompleto}";
            return _hayCambiosSinAplicar ? $"{nombre} *" : nombre;
        }
    }

    public override bool TieneCambiosSinAplicar => _hayCambiosSinAplicar;

    public ObservableCollection<ColumnaEnDisenoModeloDeVista> Columnas { get; } = [];

    public ObservableCollection<string> ErroresDeValidacion { get; } = [];

    public bool TieneErroresDeValidacion => ErroresDeValidacion.Count > 0;

    [ObservableProperty]
    public partial string Esquema { get; set; }

    [ObservableProperty]
    public partial string NombreDeLaTabla { get; set; }

    [ObservableProperty]
    public partial ColumnaEnDisenoModeloDeVista? ColumnaSeleccionada { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AplicarCommand))]
    public partial bool EstaTrabajando { get; private set; }

    partial void OnEsquemaChanged(string value) => MarcarCambios();

    partial void OnNombreDeLaTablaChanged(string value) => MarcarCambios();

    [RelayCommand]
    private async Task CargarAsync()
    {
        Columnas.Clear();

        if (EsTablaNueva)
        {
            AgregarColumnaDeIdentificador();
            DesmarcarCambios("Define las columnas y pulsa \"Aplicar cambios\".");
            return;
        }

        EstaTrabajando = true;

        try
        {
            _definicionOriginal = await _servicioDeDiseno.CargarAsync(Servidor, BaseDeDatos, TablaExistente!, CancellationToken.None);

            foreach (var columna in _definicionOriginal.Columnas)
            {
                Columnas.Add(new ColumnaEnDisenoModeloDeVista(_servicioDeDiseno.TiposDeDatoSugeridos, columna));
            }

            DesmarcarCambios($"{Columnas.Count} columnas cargadas.");
        }
        catch (Exception error)
        {
            TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Abrir el diseñador de tablas"));
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    [RelayCommand]
    private void AgregarColumna()
    {
        var columnaNueva = new ColumnaEnDisenoModeloDeVista(_servicioDeDiseno.TiposDeDatoSugeridos) { Nombre = $"Columna{Columnas.Count + 1}" };
        Columnas.Add(columnaNueva);
        ColumnaSeleccionada = columnaNueva;
    }

    [RelayCommand]
    private void QuitarColumna()
    {
        if (ColumnaSeleccionada is { } columna)
        {
            Columnas.Remove(columna);
        }
    }

    [RelayCommand]
    private async Task VerScriptAsync()
    {
        if (CrearDefinicionValida() is not { } definicion)
        {
            return;
        }

        var script = _servicioDeDiseno.GenerarScript(BaseDeDatos, definicion, _definicionOriginal);
        await _acciones.AbrirScriptEnConsultaAsync(Servidor, BaseDeDatos, script);
    }

    [RelayCommand(CanExecute = nameof(PuedeAplicar))]
    private async Task AplicarAsync()
    {
        if (CrearDefinicionValida() is not { } definicion)
        {
            return;
        }

        if (!_servicioDeDiseno.HayCambios(definicion, _definicionOriginal))
        {
            TextoDeEstado = "No hay cambios que aplicar.";
            return;
        }

        if (!await ConfirmarColumnasEliminadasAsync(definicion))
        {
            return;
        }

        EstaTrabajando = true;

        try
        {
            await _servicioDeDiseno.AplicarAsync(Servidor, BaseDeDatos, definicion, _definicionOriginal, CancellationToken.None);
            await _acciones.NotificarTablasModificadasAsync(Servidor, BaseDeDatos);

            // Una tabla nueva ya existe: a partir de ahora se edita como tabla existente
            _definicionOriginal = await _servicioDeDiseno.CargarAsync(Servidor, BaseDeDatos, definicion.Tabla, CancellationToken.None);
            ReemplazarColumnas(_definicionOriginal);
            DesmarcarCambios("Cambios aplicados correctamente.");
        }
        catch (Exception error)
        {
            ErroresDeValidacion.Clear();
            ErroresDeValidacion.Add(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Aplicar cambios de la tabla")));
            OnPropertyChanged(nameof(TieneErroresDeValidacion));
            TextoDeEstado = "No se aplicaron los cambios.";
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    private bool PuedeAplicar() => !EstaTrabajando;

    /// <summary>
    /// Arma la definición y la valida; si hay errores los muestra y devuelve nulo.
    /// </summary>
    private DefinicionDeTabla? CrearDefinicionValida()
    {
        ErroresDeValidacion.Clear();

        var columnasConTipoInvalido = Columnas.Where(columna => columna.CrearDefinicion() is null).ToList();

        foreach (var columna in columnasConTipoInvalido)
        {
            ErroresDeValidacion.Add($"El tipo de dato \"{columna.TipoDeDato}\" de la columna \"{columna.Nombre}\" no es válido.");
        }

        DefinicionDeTabla? definicion = null;

        if (columnasConTipoInvalido.Count == 0)
        {
            var columnas = Columnas.Select(columna => columna.CrearDefinicion()!).ToList();
            definicion = new DefinicionDeTabla(Esquema.Trim(), NombreDeLaTabla.Trim(), columnas, _definicionOriginal?.NombreDeLaLlavePrimaria);

            foreach (var error in _servicioDeDiseno.Validar(definicion, _definicionOriginal))
            {
                ErroresDeValidacion.Add(error);
            }
        }

        OnPropertyChanged(nameof(TieneErroresDeValidacion));
        return ErroresDeValidacion.Count == 0 ? definicion : null;
    }

    /// <summary>
    /// Eliminar columnas borra sus datos para siempre: se pide confirmación explícita.
    /// </summary>
    private async Task<bool> ConfirmarColumnasEliminadasAsync(DefinicionDeTabla definicion)
    {
        if (_definicionOriginal is null)
        {
            return true;
        }

        var columnasEliminadas = CambiosDeTabla.Calcular(_definicionOriginal, definicion).ColumnasEliminadas;

        if (columnasEliminadas.Count == 0)
        {
            return true;
        }

        var nombres = string.Join(", ", columnasEliminadas.Select(columna => columna.Nombre));
        return await _servicioDeDialogos.ConfirmarAsync(
            "Eliminar columnas",
            $"Se eliminarán las columnas {nombres} y todos sus datos. Esta acción no se puede deshacer.\n¿Deseas continuar?",
            "Eliminar y aplicar",
            "Cancelar");
    }

    private void AgregarColumnaDeIdentificador()
    {
        Columnas.Add(new ColumnaEnDisenoModeloDeVista(_servicioDeDiseno.TiposDeDatoSugeridos)
        {
            Nombre = "Id",
            TipoDeDato = "int",
            EsLlavePrimaria = true,
            EsIdentidad = true
        });
    }

    private void ReemplazarColumnas(DefinicionDeTabla definicion)
    {
        Columnas.Clear();

        foreach (var columna in definicion.Columnas)
        {
            Columnas.Add(new ColumnaEnDisenoModeloDeVista(_servicioDeDiseno.TiposDeDatoSugeridos, columna));
        }
    }

    private void AlCambiarLasColumnas(object? remitente, NotifyCollectionChangedEventArgs argumentos)
    {
        foreach (var columna in argumentos.NewItems?.OfType<ColumnaEnDisenoModeloDeVista>() ?? [])
        {
            columna.PropertyChanged += AlCambiarUnaColumna;
        }

        foreach (var columna in argumentos.OldItems?.OfType<ColumnaEnDisenoModeloDeVista>() ?? [])
        {
            columna.PropertyChanged -= AlCambiarUnaColumna;
        }

        MarcarCambios();
    }

    private void AlCambiarUnaColumna(object? remitente, PropertyChangedEventArgs argumentos) => MarcarCambios();

    private void MarcarCambios()
    {
        _hayCambiosSinAplicar = true;
        OnPropertyChanged(nameof(Titulo));
    }

    private void DesmarcarCambios(string textoDeEstado)
    {
        _hayCambiosSinAplicar = false;
        ErroresDeValidacion.Clear();
        OnPropertyChanged(nameof(TieneErroresDeValidacion));
        OnPropertyChanged(nameof(Titulo));
        TextoDeEstado = textoDeEstado;
    }

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, Servidor.Perfil.NombreVisible, BaseDeDatos);
}
