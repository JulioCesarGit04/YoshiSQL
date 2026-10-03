using YoshiSQL.Aplicacion.Errores;

namespace YoshiSQL.Escritorio.Servicios;

/// <summary>
/// Todos los errores de la interfaz pasan por aquí: se registran y se informan al usuario
/// sin mostrarle código ni cerrar la aplicación.
/// </summary>
public interface IServicioDeErrores
{
    /// <summary>
    /// Registra el error y devuelve un mensaje para mostrarlo dentro de la propia pantalla
    /// (ej. en la pestaña Mensajes o en el árbol del explorador).
    /// </summary>
    string RegistrarYDescribir(Exception error, ContextoDeError contexto);

    /// <summary>
    /// Registra el error y muestra una ventana de aviso.
    /// </summary>
    Task RegistrarYMostrarAsync(Exception error, ContextoDeError contexto);
}
