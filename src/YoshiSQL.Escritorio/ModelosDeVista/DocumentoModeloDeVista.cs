using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Aplicacion.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista;

/// <summary>
/// Cualquier documento que se abre en una pestaña: una consulta, un diagrama...
/// Para agregar un nuevo tipo de pestaña se hereda de esta clase y se registra su vista
/// en VentanaPrincipal.axaml (sección DataTemplates).
/// </summary>
public abstract partial class DocumentoModeloDeVista : ModeloDeVistaBase, IAsyncDisposable
{
    protected DocumentoModeloDeVista(ServidorConectado servidor)
    {
        Servidor = servidor;
    }

    public ServidorConectado Servidor { get; }

    public string DescripcionDeLaConexion => $"{Servidor.Perfil.NombreVisible} ({Servidor.Perfil.Usuario})";

    public abstract string Titulo { get; }

    /// <summary>
    /// Texto que aparece al pasar el mouse sobre la pestaña (ej. la ruta del archivo).
    /// </summary>
    public virtual string? InformacionAdicional => null;

    [ObservableProperty]
    public partial string TextoDeEstado { get; protected set; } = "Listo";

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
