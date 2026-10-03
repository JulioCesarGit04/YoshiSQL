using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.EdicionDeFilas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.EdicionDeFilas;

/// <summary>
/// "Editar las primeras 200 filas": grilla editable que guarda todos los cambios juntos en una transacción.
/// </summary>
public sealed partial class PestanaDeEdicionDeFilasModeloDeVista : DocumentoModeloDeVista
{
    private readonly ServicioDeEdicionDeFilas _servicioDeEdicion;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDeErrores _servicioDeErrores;

    public PestanaDeEdicionDeFilasModeloDeVista(
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        ServiciosDeDiseno servicios)
        : base(servidor)
    {
        BaseDeDatos = baseDeDatos;
        Tabla = tabla;
        _servicioDeEdicion = servicios.EdicionDeFilas;
        _servicioDeDialogos = servicios.Dialogos;
        _servicioDeErrores = servicios.Errores;
    }

    public string BaseDeDatos { get; }

    public Tabla Tabla { get; }

    public override string Titulo => TieneCambiosSinAplicar ? $"Editar: {Tabla.NombreCompleto} *" : $"Editar: {Tabla.NombreCompleto}";

    public override bool TieneCambiosSinAplicar => Filas.Any(fila => fila.TieneCambios);

    public ObservableCollection<FilaEditableModeloDeVista> Filas { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<Columna> Columnas { get; private set; } = [];

    [ObservableProperty]
    public partial FilaEditableModeloDeVista? FilaSeleccionada { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AgregarFilaCommand), nameof(EliminarFilaCommand), nameof(GuardarCambiosCommand))]
    public partial bool PuedeEditarse { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GuardarCambiosCommand))]
    public partial bool EstaTrabajando { get; private set; }

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (TieneCambiosSinAplicar && !await ConfirmarDescartarCambiosAsync())
        {
            return;
        }

        EstaTrabajando = true;
        TextoDeEstado = "Cargando filas...";

        try
        {
            var datos = await _servicioDeEdicion.CargarAsync(Servidor, BaseDeDatos, Tabla, CancellationToken.None);
            MostrarDatos(datos);
        }
        catch (Exception error)
        {
            TextoDeEstado = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Cargar filas para editar"));
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeEditarse))]
    private void AgregarFila()
    {
        var filaNueva = new FilaEditableModeloDeVista(new object?[Columnas.Count], esNueva: true);
        AgregarFilaALaLista(filaNueva);
        FilaSeleccionada = filaNueva;
        ActualizarTitulo();
    }

    [RelayCommand(CanExecute = nameof(PuedeEditarse))]
    private void EliminarFila()
    {
        if (FilaSeleccionada is not { } fila)
        {
            return;
        }

        // Una fila que todavía no se guardó simplemente se quita de la lista
        if (fila.Estado == EstadoDeFila.Nueva)
        {
            Filas.Remove(fila);
        }
        else
        {
            fila.MarcarParaEliminar();
        }

        ActualizarTitulo();
    }

    [RelayCommand(CanExecute = nameof(PuedeGuardar))]
    private async Task GuardarCambiosAsync()
    {
        var cambios = Filas.Where(fila => fila.TieneCambios).Select(fila => fila.CrearCambio()).ToList();

        if (cambios.Count == 0)
        {
            TextoDeEstado = "No hay cambios que guardar.";
            return;
        }

        EstaTrabajando = true;

        try
        {
            var filasAfectadas = await _servicioDeEdicion.GuardarCambiosAsync(Servidor, BaseDeDatos, Tabla, Columnas, cambios, CancellationToken.None);
            var datos = await _servicioDeEdicion.CargarAsync(Servidor, BaseDeDatos, Tabla, CancellationToken.None);
            MostrarDatos(datos);
            TextoDeEstado = $"Cambios guardados ({filasAfectadas} filas afectadas).";
        }
        catch (Exception error)
        {
            var mensaje = _servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError("Guardar cambios de filas"));
            await _servicioDeDialogos.MostrarInformacionAsync("No se guardaron los cambios", mensaje);
            TextoDeEstado = "No se guardaron los cambios.";
        }
        finally
        {
            EstaTrabajando = false;
        }
    }

    private bool PuedeGuardar() => PuedeEditarse && !EstaTrabajando;

    private void MostrarDatos(DatosParaEditar datos)
    {
        Filas.Clear();
        Columnas = datos.Columnas;

        foreach (var valores in datos.Filas)
        {
            AgregarFilaALaLista(new FilaEditableModeloDeVista(valores));
        }

        PuedeEditarse = datos.PuedeEditarse;
        ActualizarTitulo();
        TextoDeEstado = datos.PuedeEditarse
            ? $"{Filas.Count} filas. Escribe NULL para dejar una celda vacía."
            : "La tabla no tiene llave primaria: se muestra en modo de solo lectura.";
    }

    private void AgregarFilaALaLista(FilaEditableModeloDeVista fila)
    {
        fila.PropertyChanged += (_, _) => ActualizarTitulo();
        Filas.Add(fila);
    }

    private void ActualizarTitulo() => OnPropertyChanged(nameof(Titulo));

    private Task<bool> ConfirmarDescartarCambiosAsync() =>
        _servicioDeDialogos.ConfirmarAsync(
            "Descartar cambios",
            "Hay cambios sin guardar en la grilla. ¿Deseas descartarlos y volver a cargar las filas?",
            "Descartar",
            "Cancelar");

    private ContextoDeError CrearContextoDeError(string accion) =>
        new(accion, Servidor.Perfil.NombreVisible, BaseDeDatos);
}
