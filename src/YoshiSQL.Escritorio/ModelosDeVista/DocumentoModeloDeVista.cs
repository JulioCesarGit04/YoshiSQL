using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Sesion;

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

    /// <summary>Color "#RRGGBB" de la conexión para la barra de estado; nulo si no se eligió uno.</summary>
    public string? ColorDeLaConexion => Servidor.Perfil.Color;

    public abstract string Titulo { get; }

    /// <summary>
    /// Texto que aparece al pasar el mouse sobre la pestaña (ej. la ruta del archivo).
    /// </summary>
    public virtual string? InformacionAdicional => null;

    [ObservableProperty]
    public partial string TextoDeEstado { get; protected set; } = "Listo";

    /// <summary>
    /// Verdadero si cerrar la pestaña haría perder cambios que el usuario no aplicó (ej. en el diseñador).
    /// </summary>
    public virtual bool TieneCambiosSinAplicar => false;

    /// <summary>
    /// Datos para volver a abrir esta pestaña la próxima vez que se inicie YoshiSQL;
    /// nulo si este tipo de pestaña no se restaura.
    /// </summary>
    public virtual PestanaGuardada? CrearPestanaGuardada() => null;

    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
