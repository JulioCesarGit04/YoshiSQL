using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

/// <summary>
/// Últimas consultas ejecutadas, de la más reciente a la más antigua. Se conservan entre sesiones.
/// </summary>
public sealed partial class HistorialDeConsultas
{
    public const int CantidadMaximaDeConsultas = 500;
    public const int LargoMaximoDelTexto = 50_000;

    private const string TextoOculto = "'***'";

    /// <summary>
    /// Se agrega al final de un script recortado. Sin esto, al abrirlo desde el historial parecía completo
    /// y se ejecutaba hasta donde estuviera cortado, a veces en mitad de una instrucción.
    /// </summary>
    public const string AvisoDeTextoRecortado =
        "\n-- [YoshiSQL] SCRIPT INCOMPLETO: el historial solo guarda el comienzo de los scripts muy largos. Abre el archivo original.\n";

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
        var textoRecortado = consulta.Texto.Length > LargoMaximoDelTexto
            ? consulta.Texto[..(LargoMaximoDelTexto - AvisoDeTextoRecortado.Length)] + AvisoDeTextoRecortado
            : consulta.Texto;
        var consultaRecortada = consulta with { Texto = OcultarContrasenas(textoRecortado) };

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

    /// <summary>
    /// El historial se guarda en un archivo de texto: un CREATE LOGIN ... WITH PASSWORD = '...' dejaba ahí la contraseña.
    /// </summary>
    public static string OcultarContrasenas(string textoSql) =>
        ExpresionDeContrasena().Replace(textoSql, coincidencia => coincidencia.Groups["inicio"].Value + TextoOculto);

    // PASSWORD = '...', OLD_PASSWORD = N'...' y SECRET = '...' (credenciales); '' es una comilla dentro del texto
    [GeneratedRegex(@"(?<inicio>(PASSWORD|SECRET)\s*=\s*)N?'(?:[^']|'')*'", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ExpresionDeContrasena();

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
