using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Dominio.Preferencias;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Preferencias;

public sealed partial class DialogoDePreferenciasModeloDeVista : ModeloDeVistaBase
{
    private readonly ServicioDePreferencias _servicioDePreferencias;
    private readonly IServicioDeErrores _servicioDeErrores;

    public DialogoDePreferenciasModeloDeVista(
        ServicioDePreferencias servicioDePreferencias,
        IServicioDeErrores servicioDeErrores,
        IReadOnlyList<string> fuentesInstaladas)
    {
        _servicioDePreferencias = servicioDePreferencias;
        _servicioDeErrores = servicioDeErrores;
        FuentesInstaladas = fuentesInstaladas;

        var actuales = servicioDePreferencias.Actuales;
        TemaSeleccionado = OpcionDeTema.Todas.First(opcion => opcion.Tema == actuales.Tema);
        FuenteDelEditor = actuales.FuenteDelEditor;
        TamanoDeLetraDelEditor = (decimal)actuales.TamanoDeLetraDelEditor;
        MostrarNumerosDeLinea = actuales.MostrarNumerosDeLinea;
        AjustarLineasLargas = actuales.AjustarLineasLargas;
    }

    public IReadOnlyList<OpcionDeTema> OpcionesDeTema => OpcionDeTema.Todas;

    public IReadOnlyList<string> FuentesInstaladas { get; }

    public decimal TamanoMinimo => (decimal)PreferenciasDelUsuario.TamanoDeLetraMinimo;

    public decimal TamanoMaximo => (decimal)PreferenciasDelUsuario.TamanoDeLetraMaximo;

    [ObservableProperty]
    public partial OpcionDeTema TemaSeleccionado { get; set; }

    [ObservableProperty]
    public partial string FuenteDelEditor { get; set; }

    [ObservableProperty]
    public partial decimal? TamanoDeLetraDelEditor { get; set; }

    [ObservableProperty]
    public partial bool MostrarNumerosDeLinea { get; set; }

    [ObservableProperty]
    public partial bool AjustarLineasLargas { get; set; }

    [ObservableProperty]
    public partial string? MensajeDeError { get; private set; }

    /// <summary>
    /// Se dispara cuando la ventana debe cerrarse (después de guardar o al cancelar).
    /// </summary>
    public event EventHandler? CierreSolicitado;

    [RelayCommand]
    private async Task GuardarAsync()
    {
        var preferencias = new PreferenciasDelUsuario
        {
            Tema = TemaSeleccionado.Tema,
            FuenteDelEditor = FuenteDelEditor,
            TamanoDeLetraDelEditor = (double)(TamanoDeLetraDelEditor ?? (decimal)PreferenciasDelUsuario.Predeterminadas.TamanoDeLetraDelEditor),
            MostrarNumerosDeLinea = MostrarNumerosDeLinea,
            AjustarLineasLargas = AjustarLineasLargas
        };

        try
        {
            await _servicioDePreferencias.GuardarAsync(preferencias, CancellationToken.None);
            CierreSolicitado?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception error)
        {
            MensajeDeError = _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Guardar preferencias"));
        }
    }

    [RelayCommand]
    private void RestablecerPredeterminadas()
    {
        var predeterminadas = PreferenciasDelUsuario.Predeterminadas;
        TemaSeleccionado = OpcionDeTema.Todas.First(opcion => opcion.Tema == predeterminadas.Tema);
        FuenteDelEditor = predeterminadas.FuenteDelEditor;
        TamanoDeLetraDelEditor = (decimal)predeterminadas.TamanoDeLetraDelEditor;
        MostrarNumerosDeLinea = predeterminadas.MostrarNumerosDeLinea;
        AjustarLineasLargas = predeterminadas.AjustarLineasLargas;
    }

    [RelayCommand]
    private void Cancelar() => CierreSolicitado?.Invoke(this, EventArgs.Empty);
}
