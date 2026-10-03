using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.ModelosDeVista.Historial;

public interface IAccionesDelHistorial
{
    /// <summary>
    /// Abre la consulta en una pestaña nueva, en el mismo servidor y base de datos donde se ejecutó.
    /// </summary>
    Task AbrirConsultaDelHistorialAsync(ConsultaEjecutada consulta);
}
