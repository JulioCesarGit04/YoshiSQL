using YoshiSQL.Aplicacion.Conexiones;

namespace YoshiSQL.Escritorio.ModelosDeVista.DisenoDeTablas;

/// <summary>
/// Lo que el diseñador y el editor de filas le piden a la ventana principal.
/// </summary>
public interface IAccionesDelDiseno
{
    Task AbrirScriptEnConsultaAsync(ServidorConectado servidor, string baseDeDatos, string script);

    /// <summary>
    /// Avisa que cambió la estructura de una tabla, para refrescar el explorador y el autocompletado.
    /// </summary>
    Task NotificarTablasModificadasAsync(ServidorConectado servidor, string baseDeDatos);
}
