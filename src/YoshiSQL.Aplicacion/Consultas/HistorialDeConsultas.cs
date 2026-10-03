using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Últimas consultas ejecutadas, de la más reciente a la más antigua. Se conservan entre sesiones.
/// </summary>
public sealed class HistorialDeConsultas
{
    public const int CantidadMaximaDeConsultas = 500;
    public const int LargoMaximoDelTexto = 50_000;

    private readonly IRepositorioDeHistorial _repositorio;
    private readonly ILogger<HistorialDeConsultas> _registro;
    private readonly List<ConsultaEjecutada> _consultas = [];
    private readonly Lock _bloqueo = new();
    private readonly SemaphoreSlim _guardadoExclusivo = new(1, 1);

    public HistorialDeConsultas(IRepositorioDeHistorial repositorio, ILogger<HistorialDeConsultas> registro)
    {
        _repositorio = repositorio;
        _registro = registro;
    }

    public event EventHandler? HistorialModificado;

    public async Task CargarAsync(CancellationToken tokenDeCancelacion)
    {
        var consultasGuardadas = await _repositorio.CargarAsync(tokenDeCancelacion);

        lock (_bloqueo)
        {
            _consultas.Clear();
            _consultas.AddRange(consultasGuardadas.Take(CantidadMaximaDeConsultas));
        }

        HistorialModificado?.Invoke(this, EventArgs.Empty);
    }

    public async Task RegistrarAsync(ConsultaEjecutada consulta)
    {
        var consultaRecortada = consulta.Texto.Length > LargoMaximoDelTexto
            ? consulta with { Texto = consulta.Texto[..LargoMaximoDelTexto] }
            : consulta;

        lock (_bloqueo)
        {
            _consultas.Insert(0, consultaRecortada);

            if (_consultas.Count > CantidadMaximaDeConsultas)
            {
                _consultas.RemoveAt(_consultas.Count - 1);
            }
        }

        await GuardarYNotificarAsync();
    }

    public async Task EliminarAsync(ConsultaEjecutada consulta)
    {
        lock (_bloqueo)
        {
            _consultas.Remove(consulta);
        }

        await GuardarYNotificarAsync();
    }

    public async Task BorrarTodoAsync()
    {
        lock (_bloqueo)
        {
            _consultas.Clear();
        }

        await GuardarYNotificarAsync();
    }

    public IReadOnlyList<ConsultaEjecutada> ObtenerRecientes()
    {
        lock (_bloqueo)
        {
            return _consultas.ToList();
        }
    }

    /// <summary>
    /// Un fallo al guardar no debe interrumpir la ejecución de la consulta: se registra y se sigue.
    /// </summary>
    private async Task GuardarYNotificarAsync()
    {
        HistorialModificado?.Invoke(this, EventArgs.Empty);
        await _guardadoExclusivo.WaitAsync();

        try
        {
            await _repositorio.GuardarAsync(ObtenerRecientes(), CancellationToken.None);
        }
        catch (Exception error)
        {
            _registro.LogWarning(error, "No se pudo guardar el historial de consultas");
        }
        finally
        {
            _guardadoExclusivo.Release();
        }
    }
}
