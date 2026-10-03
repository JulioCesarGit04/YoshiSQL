namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Últimas consultas ejecutadas durante la sesión, de la más reciente a la más antigua.
/// </summary>
public sealed class HistorialDeConsultas
{
    private const int CantidadMaximaDeConsultas = 200;

    private readonly LinkedList<ConsultaEjecutada> _consultas = new();
    private readonly Lock _bloqueo = new();

    public void Registrar(ConsultaEjecutada consulta)
    {
        lock (_bloqueo)
        {
            _consultas.AddFirst(consulta);

            if (_consultas.Count > CantidadMaximaDeConsultas)
            {
                _consultas.RemoveLast();
            }
        }
    }

    public IReadOnlyList<ConsultaEjecutada> ObtenerRecientes()
    {
        lock (_bloqueo)
        {
            return _consultas.ToList();
        }
    }
}
