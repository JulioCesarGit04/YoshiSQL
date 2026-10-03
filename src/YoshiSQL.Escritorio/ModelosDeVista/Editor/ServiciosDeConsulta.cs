using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.Editor;

/// <summary>
/// Servicios que usa cada pestaña de consulta, agrupados para no repetir una lista larga de parámetros.
/// </summary>
public sealed record ServiciosDeConsulta(
    ServicioDeEjecucion Ejecucion,
    ServicioDelExplorador Explorador,
    ServicioDeFormatoSql Formato,
    IServicioDeErrores Errores,
    IServicioDeExportacionDeResultados Exportacion,
    ServicioDeAutocompletado Autocompletado,
    IServicioDelSistemaOperativo SistemaOperativo);
