using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Resultados;

/// <summary>
/// Contenido del panel inferior: la pestaña "Resultados" (grillas) y la pestaña "Mensajes".
/// </summary>
public sealed partial class ResultadosModeloDeVista : ModeloDeVistaBase
{
    public const int IndiceDePestanaDeResultados = 0;
    public const int IndiceDePestanaDeMensajes = 1;

    public ObservableCollection<ConjuntoDeResultados> Conjuntos { get; } = [];

    public ObservableCollection<MensajeDeEjecucion> Mensajes { get; } = [];

    [ObservableProperty]
    public partial int IndiceDePestanaSeleccionada { get; set; }

    public ConjuntoDeResultados? ConjuntoUnico => Conjuntos.Count == 1 ? Conjuntos[0] : null;

    public bool TieneUnSoloConjunto => Conjuntos.Count == 1;

    public bool TieneVariosConjuntos => Conjuntos.Count > 1;

    public bool NoTieneConjuntos => Conjuntos.Count == 0;

    public event EventHandler<int>? IrALineaSolicitado;

    public void Mostrar(ResultadoDeEjecucion resultado)
    {
        Limpiar();

        foreach (var conjunto in resultado.ConjuntosDeResultados)
        {
            Conjuntos.Add(conjunto);
        }

        foreach (var mensaje in resultado.Mensajes)
        {
            Mensajes.Add(mensaje);
        }

        Mensajes.Add(MensajeDeEjecucion.Informacion($"Hora de finalización: {DateTimeOffset.Now:yyyy-MM-ddTHH:mm:ss.fffzzz}"));

        var debeMostrarMensajes = Conjuntos.Count == 0 || resultado.TieneErrores;
        IndiceDePestanaSeleccionada = debeMostrarMensajes ? IndiceDePestanaDeMensajes : IndiceDePestanaDeResultados;

        NotificarCambioDeConjuntos();
    }

    public void MostrarError(string mensaje)
    {
        Limpiar();
        Mensajes.Add(MensajeDeEjecucion.Error(mensaje));
        IndiceDePestanaSeleccionada = IndiceDePestanaDeMensajes;
        NotificarCambioDeConjuntos();
    }

    public void Limpiar()
    {
        Conjuntos.Clear();
        Mensajes.Clear();
        NotificarCambioDeConjuntos();
    }

    public void SolicitarIrALinea(MensajeDeEjecucion mensaje)
    {
        if (mensaje.NumeroDeLinea is int numeroDeLinea)
        {
            IrALineaSolicitado?.Invoke(this, numeroDeLinea);
        }
    }

    private void NotificarCambioDeConjuntos()
    {
        OnPropertyChanged(nameof(ConjuntoUnico));
        OnPropertyChanged(nameof(TieneUnSoloConjunto));
        OnPropertyChanged(nameof(TieneVariosConjuntos));
        OnPropertyChanged(nameof(NoTieneConjuntos));
    }
}
