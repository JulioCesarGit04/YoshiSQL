using YoshiSQL.Aplicacion.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Servidor y base de datos a los que pertenece un nodo del árbol.
/// </summary>
public sealed record ContextoDelNodo(ServidorConectado Servidor, string? BaseDeDatos = null)
{
    public string BaseDeDatosOPredeterminada => BaseDeDatos ?? Servidor.Perfil.BaseDeDatosPredeterminada;

    public ContextoDelNodo EnBaseDeDatos(string baseDeDatos) => this with { BaseDeDatos = baseDeDatos };
}
