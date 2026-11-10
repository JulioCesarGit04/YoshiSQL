using YoshiSQL.Dominio.Sesion;

namespace YoshiSQL.Dominio.Contratos;

public interface IRepositorioDeSesion
{
    Task<EstadoDeLaSesion> CargarAsync(CancellationToken tokenDeCancelacion);

    Task GuardarAsync(EstadoDeLaSesion estado, CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Deja una marca de que YoshiSQL está abierto.
    /// </summary>
    /// <returns>Verdadero si la ejecución anterior terminó sin cerrarse correctamente.</returns>
    Task<bool> RegistrarInicioAsync(CancellationToken tokenDeCancelacion);

    Task RegistrarCierreCorrectoAsync(CancellationToken tokenDeCancelacion);

    /// <summary>
    /// Verdadero si al iniciar ya había otro YoshiSQL abierto. Esta ventana no restaura ni guarda la sesión,
    /// para no pisar la de la primera.
    /// </summary>
    bool EsInstanciaSecundaria { get; }

    /// <summary>
    /// Guarda como archivos .sql los scripts que no se pudieron restaurar en una pestaña.
    /// </summary>
    /// <returns>Carpeta donde quedaron los archivos.</returns>
    Task<string> GuardarScriptsRecuperadosAsync(IReadOnlyList<PestanaGuardada> pestanas, CancellationToken tokenDeCancelacion);
}
