using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Planes;
using YoshiSQL.Escritorio.Convertidores;
using YoshiSQL.Escritorio.ModelosDeVista.Planes;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Resultados;

/// <summary>
/// Contenido del panel inferior: la pestaña "Resultados" (grillas) y la pestaña "Mensajes".
/// </summary>
public sealed partial class ResultadosModeloDeVista : ModeloDeVistaBase
{
    public const int IndiceDePestanaDeResultados = 0;
    public const int IndiceDePestanaDeMensajes = 1;
    public const int IndiceDePestanaDelPlan = 2;

    private readonly IServicioDeExportacionDeResultados _servicioDeExportacion;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;

    public ResultadosModeloDeVista(IServicioDeExportacionDeResultados servicioDeExportacion, IServicioDelSistemaOperativo sistemaOperativo)
    {
        _servicioDeExportacion = servicioDeExportacion;
        _sistemaOperativo = sistemaOperativo;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePlan))]
    public partial PlanDeEjecucionModeloDeVista? Plan { get; private set; }

    public bool TienePlan => Plan is not null;

    /// <summary>
    /// Muestra el plan; si se pide, cambia a su pestaña (así ocurre con el plan estimado).
    /// </summary>
    public void MostrarPlan(PlanDeEjecucion plan, bool seleccionarPestana)
    {
        Plan = new PlanDeEjecucionModeloDeVista(plan, _sistemaOperativo);

        if (seleccionarPestana)
        {
            IndiceDePestanaSeleccionada = IndiceDePestanaDelPlan;
        }
    }

    public ObservableCollection<ConjuntoDeResultadosModeloDeVista> Conjuntos { get; } = [];

    public ObservableCollection<MensajeDeEjecucion> Mensajes { get; } = [];

    [ObservableProperty]
    public partial int IndiceDePestanaSeleccionada { get; set; }

    /// <summary>Muestra los resultados como texto monoespaciado en vez de grillas (Ctrl+T, como en SSMS).</summary>
    [ObservableProperty]
    public partial bool MostrarComoTexto { get; set; }

    public ConjuntoDeResultadosModeloDeVista? ConjuntoUnico => Conjuntos.Count == 1 ? Conjuntos[0] : null;

    public bool TieneUnSoloConjunto => Conjuntos.Count == 1;

    public bool TieneVariosConjuntos => Conjuntos.Count > 1;

    public bool NoTieneConjuntos => Conjuntos.Count == 0;

    public bool MostrarGrillaUnica => !MostrarComoTexto && TieneUnSoloConjunto;

    public bool MostrarVariasGrillas => !MostrarComoTexto && TieneVariosConjuntos;

    public bool MostrarTexto => MostrarComoTexto && !NoTieneConjuntos;

    public string TextoDeResultados => ConstruirTexto();

    partial void OnMostrarComoTextoChanged(bool value) => NotificarCambioDeConjuntos();

    public event EventHandler<int>? IrALineaSolicitado;

    public void Mostrar(ResultadoDeEjecucion resultado)
    {
        Limpiar();

        foreach (var conjunto in resultado.ConjuntosDeResultados)
        {
            Conjuntos.Add(new ConjuntoDeResultadosModeloDeVista(conjunto, _servicioDeExportacion));
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
        Plan = null;
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
        OnPropertyChanged(nameof(MostrarGrillaUnica));
        OnPropertyChanged(nameof(MostrarVariasGrillas));
        OnPropertyChanged(nameof(MostrarTexto));
        OnPropertyChanged(nameof(TextoDeResultados));
    }

    /// <summary>Arma todos los conjuntos como tablas de texto con columnas alineadas, estilo SSMS.</summary>
    private string ConstruirTexto()
    {
        if (Conjuntos.Count == 0)
        {
            return string.Empty;
        }

        var texto = new StringBuilder();

        foreach (var modelo in Conjuntos)
        {
            if (texto.Length > 0)
            {
                texto.AppendLine().AppendLine();
            }

            AgregarConjunto(texto, modelo.Conjunto);
        }

        return texto.ToString();
    }

    private static void AgregarConjunto(StringBuilder texto, ConjuntoDeResultados conjunto)
    {
        var columnas = conjunto.Columnas;
        var celdas = conjunto.Filas
            .Select(fila => fila.Select(TextoDeCelda).ToArray())
            .ToList();

        var anchos = new int[columnas.Count];
        for (var j = 0; j < columnas.Count; j++)
        {
            anchos[j] = columnas[j].Nombre.Length;
            foreach (var fila in celdas)
            {
                anchos[j] = Math.Max(anchos[j], fila[j].Length);
            }
        }

        texto.AppendLine(string.Join(" ", columnas.Select((columna, j) => columna.Nombre.PadRight(anchos[j]))));
        texto.AppendLine(string.Join(" ", anchos.Select(ancho => new string('-', ancho))));

        foreach (var fila in celdas)
        {
            texto.AppendLine(string.Join(" ", fila.Select((valor, j) => valor.PadRight(anchos[j]))));
        }

        texto.Append(CultureInfo.InvariantCulture, $"({conjunto.CantidadDeFilas} filas afectadas)");
    }

    private static string TextoDeCelda(object? valor) =>
        ValorDeCeldaATexto.Instancia.Convert(valor, typeof(string), null, CultureInfo.InvariantCulture) as string ?? string.Empty;
}
