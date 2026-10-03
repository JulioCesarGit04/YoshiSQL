namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Lo que el explorador puede pedirle a la ventana principal desde un menú contextual.
/// </summary>
public interface IAccionesDelExplorador
{
    Task AbrirNuevaConsultaAsync(ContextoDelNodo contexto, string textoInicial, bool ejecutarAlAbrir);

    Task AbrirDiagramaAsync(ContextoDelNodo contexto);

    void DesconectarServidor(ContextoDelNodo contexto);

    Task MostrarErrorAsync(string mensaje);
}
