namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeScripts
{
    Task<string> LeerAsync(string rutaDelArchivo, CancellationToken tokenDeCancelacion);

    Task GuardarAsync(string rutaDelArchivo, string contenido, CancellationToken tokenDeCancelacion);
}
