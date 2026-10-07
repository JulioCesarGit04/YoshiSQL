using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Lo que el explorador puede pedirle a la ventana principal desde un menú contextual.
/// </summary>
public interface IAccionesDelExplorador
{
    Task AbrirNuevaConsultaAsync(ContextoDelNodo contexto, string textoInicial, bool ejecutarAlAbrir);

    Task AbrirDiagramaAsync(ContextoDelNodo contexto);

    /// <param name="tabla">Tabla a modificar; nula para diseñar una tabla nueva.</param>
    Task AbrirDisenadorDeTablaAsync(ContextoDelNodo contexto, Tabla? tabla);

    Task AbrirEdicionDeFilasAsync(ContextoDelNodo contexto, Tabla tabla);

    Task MostrarPropiedadesDeTablaAsync(ContextoDelNodo contexto, Tabla tabla);

    Task MostrarPropiedadesDeBaseDeDatosAsync(ContextoDelNodo contexto);

    Task MostrarDependenciasAsync(ContextoDelNodo contexto, ObjetoDeEsquema objeto);

    Task RenombrarObjetoAsync(ContextoDelNodo contexto, ObjetoDeEsquema objeto);

    Task AbrirMonitorDeActividadAsync(ContextoDelNodo contexto);

    Task MostrarRespaldoAsync(ContextoDelNodo contexto);

    Task MostrarRestauracionAsync(ContextoDelNodo contexto);

    Task DesconectarServidorAsync(ContextoDelNodo contexto);
}
