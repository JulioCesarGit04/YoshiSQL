using Microsoft.Extensions.Logging;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Aplicacion.Errores;

/// <summary>
/// Punto único para registrar errores. Distingue entre errores esperados (un servidor apagado,
/// una contraseña incorrecta), que se registran como advertencia, y fallas de YoshiSQL,
/// que reciben un código y se registran con todo su detalle técnico.
/// </summary>
public sealed class RegistroDeErrores
{
    private const string ValorDesconocido = "-";

    private readonly ILogger<RegistroDeErrores> _registro;
    private readonly TimeProvider _reloj;

    public RegistroDeErrores(ILogger<RegistroDeErrores> registro, TimeProvider reloj)
    {
        _registro = registro;
        _reloj = reloj;
    }

    public ErrorRegistrado Registrar(Exception error, ContextoDeError contexto) => error switch
    {
        OperationCanceledException => new ErrorRegistrado(null, "La operación fue cancelada.", EsInesperado: false, string.Empty),
        ErrorDeYoshiSql errorEsperado => RegistrarErrorEsperado(errorEsperado, contexto),
        _ => RegistrarErrorInesperado(error, contexto)
    };

    private ErrorRegistrado RegistrarErrorEsperado(ErrorDeYoshiSql error, ContextoDeError contexto)
    {
        _registro.LogWarning(
            "{Accion}: {MensajeDelError}\n  Servidor: {Servidor}    Base de datos: {BaseDeDatos}",
            contexto.Accion,
            error.Message,
            contexto.Servidor ?? ValorDesconocido,
            contexto.BaseDeDatos ?? ValorDesconocido);

        return new ErrorRegistrado(null, error.Message, EsInesperado: false, error.ToString());
    }

    private ErrorRegistrado RegistrarErrorInesperado(Exception error, ContextoDeError contexto)
    {
        var codigo = GeneradorDeCodigosDeError.Generar(_reloj.GetLocalNow());

        _registro.LogError(
            error,
            "{CodigoDeError}\n  Acción:        {Accion}\n  Servidor:      {Servidor}    Base de datos: {BaseDeDatos}\n  Mensaje:       {MensajeDelError}\n  Entorno:       {Entorno}",
            codigo,
            contexto.Accion,
            contexto.Servidor ?? ValorDesconocido,
            contexto.BaseDeDatos ?? ValorDesconocido,
            error.Message,
            InformacionDelEntorno.Describir());

        var mensaje = $"Algo falló al realizar la acción \"{contexto.Accion}\".\n"
            + $"YoshiSQL sigue funcionando. El detalle quedó guardado en el registro con el código {codigo}.";

        return new ErrorRegistrado(codigo, mensaje, EsInesperado: true, error.ToString());
    }
}
