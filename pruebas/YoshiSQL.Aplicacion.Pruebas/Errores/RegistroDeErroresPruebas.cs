using Microsoft.Extensions.Logging;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Errores;

namespace YoshiSQL.Aplicacion.Pruebas.Errores;

public class RegistroDeErroresPruebas
{
    private readonly RegistradorEnMemoria _registrador = new();
    private readonly RegistroDeErrores _registroDeErrores;

    public RegistroDeErroresPruebas()
    {
        _registroDeErrores = new RegistroDeErrores(_registrador, TimeProvider.System);
    }

    [Fact]
    public void Registrar_ErrorInesperado_GeneraCodigoYLoRegistraComoError()
    {
        var resultado = _registroDeErrores.Registrar(
            new InvalidOperationException("falla interna"),
            new ContextoDeError("Abrir diagrama", "localhost", "Ventas"));

        Assert.True(resultado.EsInesperado);
        Assert.Matches(@"^E-\d{4}-[0-9A-F]{4}$", resultado.Codigo);
        Assert.Contains(resultado.Codigo!, resultado.MensajeParaElUsuario);
        var entrada = Assert.Single(_registrador.Entradas);
        Assert.Equal(LogLevel.Error, entrada.Nivel);
        Assert.Contains("Abrir diagrama", entrada.Mensaje);
        Assert.Contains("Ventas", entrada.Mensaje);
        Assert.IsType<InvalidOperationException>(entrada.Excepcion);
    }

    [Fact]
    public void Registrar_ErrorEsperado_MuestraSuMensajeYSoloAdvierte()
    {
        var resultado = _registroDeErrores.Registrar(
            new ErrorDeConexion("No se encontró el servidor."),
            new ContextoDeError("Conectar"));

        Assert.False(resultado.EsInesperado);
        Assert.Null(resultado.Codigo);
        Assert.Equal("No se encontró el servidor.", resultado.MensajeParaElUsuario);
        Assert.Equal(LogLevel.Warning, Assert.Single(_registrador.Entradas).Nivel);
    }

    [Fact]
    public void Registrar_Cancelacion_NoEscribeEnElRegistro()
    {
        var resultado = _registroDeErrores.Registrar(new OperationCanceledException(), new ContextoDeError("Ejecutar consulta"));

        Assert.False(resultado.EsInesperado);
        Assert.Empty(_registrador.Entradas);
    }

    private sealed class RegistradorEnMemoria : ILogger<RegistroDeErrores>
    {
        public List<(LogLevel Nivel, string Mensaje, Exception? Excepcion)> Entradas { get; } = [];

        public IDisposable? BeginScope<TEstado>(TEstado estado) where TEstado : notnull => null;

        public bool IsEnabled(LogLevel nivel) => true;

        public void Log<TEstado>(LogLevel nivel, EventId evento, TEstado estado, Exception? excepcion, Func<TEstado, Exception?, string> formatear) =>
            Entradas.Add((nivel, formatear(estado, excepcion), excepcion));
    }
}
