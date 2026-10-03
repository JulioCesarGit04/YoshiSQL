using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Dominio.Planes;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Planes;

public sealed partial class PlanDeEjecucionModeloDeVista : ModeloDeVistaBase
{
    private readonly PlanDeEjecucion _plan;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;

    public PlanDeEjecucionModeloDeVista(PlanDeEjecucion plan, IServicioDelSistemaOperativo sistemaOperativo)
    {
        _plan = plan;
        _sistemaOperativo = sistemaOperativo;
        Instrucciones = plan.Instrucciones
            .Where(instruccion => instruccion.Raiz is not null)
            .Select((instruccion, indice) => new InstruccionDelPlanModeloDeVista(instruccion, indice + 1))
            .ToList();
    }

    public string Tipo => _plan.EsReal
        ? "Plan real: incluye las filas que devolvió cada operación"
        : "Plan estimado: la consulta no se ejecutó";

    public IReadOnlyList<InstruccionDelPlanModeloDeVista> Instrucciones { get; }

    public bool NoTieneInstrucciones => Instrucciones.Count == 0;

    // El XML se puede pegar en otras herramientas que dibujan planes
    [RelayCommand]
    private Task CopiarXmlAsync() => _sistemaOperativo.CopiarAlPortapapelesAsync(string.Join(Environment.NewLine, _plan.DocumentosXml));
}
