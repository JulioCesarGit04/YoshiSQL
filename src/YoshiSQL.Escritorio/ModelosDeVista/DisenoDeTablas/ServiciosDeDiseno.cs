using YoshiSQL.Aplicacion.DisenoDeTablas;
using YoshiSQL.Aplicacion.EdicionDeFilas;
using YoshiSQL.Escritorio.Servicios;

namespace YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;

/// <summary>
/// Servicios que usan las pestañas del diseñador de tablas y de edición de filas.
/// </summary>
public sealed record ServiciosDeDiseno(
    ServicioDeDisenoDeTablas DisenoDeTablas,
    ServicioDeEdicionDeFilas EdicionDeFilas,
    IServicioDeDialogos Dialogos,
    IServicioDeErrores Errores);
