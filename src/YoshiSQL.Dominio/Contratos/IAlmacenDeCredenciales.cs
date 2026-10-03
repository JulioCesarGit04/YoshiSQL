namespace YoshiSQL.Dominio.Contratos;

public interface IAlmacenDeCredenciales
{
    Task GuardarContrasenaAsync(Guid idDelPerfil, string contrasena, CancellationToken tokenDeCancelacion);

    Task<string?> ObtenerContrasenaAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion);

    Task EliminarContrasenaAsync(Guid idDelPerfil, CancellationToken tokenDeCancelacion);
}
