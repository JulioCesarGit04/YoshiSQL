using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Guarda dónde dejó el usuario cada tabla del diagrama de una base de datos.
/// </summary>
public interface IRepositorioDeDiagramas
{
    Task<DisposicionDeDiagrama?> CargarDisposicionAsync(
        string servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion);

    Task GuardarDisposicionAsync(
        string servidor,
        string baseDeDatos,
        DisposicionDeDiagrama disposicion,
        CancellationToken tokenDeCancelacion);
}
