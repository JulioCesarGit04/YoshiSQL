using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Contratos;

/// <summary>
/// Genera código SQL listo para abrir en el editor (opciones del menú contextual).
/// </summary>
public interface IGeneradorDeScripts
{
    /// <param name="filtroWhere">Condición del WHERE escrita por el usuario, sin la palabra WHERE; nula u opcional.</param>
    /// <param name="ordenarPor">Columnas del ORDER BY escritas por el usuario, sin las palabras ORDER BY; nula u opcional.</param>
    string GenerarSeleccionDeFilas(
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        int cantidadDeFilas,
        string? filtroWhere = null,
        string? ordenarPor = null);

    string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos);

    string GenerarCreacionDeTabla(string baseDeDatos, Tabla tabla, IReadOnlyList<Columna> columnas);

    /// <summary>CREATE TABLE sin el USE/GO, para armar el script completo de una base de datos.</summary>
    string GenerarCreacionDeTablaSinEnvolver(Tabla tabla, IReadOnlyList<Columna> columnas);

    /// <summary>ALTER TABLE ADD CONSTRAINT FOREIGN KEY para una llave foránea.</summary>
    string GenerarLlaveForanea(LlaveForanea llave);

    /// <summary>Un INSERT por fila con los valores reales; envuelve con SET IDENTITY_INSERT si hay identidad.</summary>
    string GenerarInsertDeFilas(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<object?[]> filas);

    /// <summary>Instrucción USE para fijar la base de datos.</summary>
    string GenerarUso(string baseDeDatos);

    /// <summary>SELECT de todas las filas de una tabla (sin TOP).</summary>
    string GenerarSeleccionCompleta(Tabla tabla);

    /// <summary>
    /// Plantilla SELECT, INSERT, UPDATE o DELETE para una tabla o vista, lista para editar en el editor.
    /// </summary>
    string GenerarInstruccionDml(string baseDeDatos, ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas, TipoDeScriptDml tipo);

    string GenerarEliminacion(string baseDeDatos, ObjetoDeEsquema objeto);

    /// <summary>Script EXEC sp_rename para cambiar el nombre de un objeto.</summary>
    string GenerarRenombrado(string baseDeDatos, ObjetoDeEsquema objeto, string nuevoNombre);

    /// <summary>Consulta que muestra la fragmentación de los índices de una tabla.</summary>
    string GenerarConsultaDeFragmentacion(string baseDeDatos, Tabla tabla);

    /// <summary>Script ALTER INDEX para reconstruir (REBUILD) o reorganizar (REORGANIZE) un índice.</summary>
    string GenerarMantenimientoDeIndice(string baseDeDatos, Tabla tabla, string nombreDelIndice, bool reconstruir);

    /// <summary>
    /// Script para crear el objeto tal como existe hoy, a partir de su definición.
    /// </summary>
    string GenerarCreacionDesdeDefinicion(string baseDeDatos, string definicion);

    /// <summary>
    /// Script para modificar el objeto: su definición actual con CREATE cambiado por ALTER.
    /// </summary>
    string GenerarModificacionDesdeDefinicion(string baseDeDatos, string definicion);

    string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos);

    string GenerarCreacionDeInicioDeSesion();

    string GenerarEliminacionDeInicioDeSesion(string nombre);

    string GenerarCreacionDeUsuario(string baseDeDatos);

    string GenerarEliminacionDeUsuario(string baseDeDatos, string nombre);

    /// <summary>
    /// Script para crear la tabla (si original es nulo) o para llevarla de su estado original al nuevo.
    /// Todo dentro de una transacción.
    /// </summary>
    string GenerarCambiosDeTabla(string baseDeDatos, DefinicionDeTabla? original, DefinicionDeTabla nueva);

    /// <summary>
    /// INSERT, UPDATE y DELETE parametrizados para guardar los cambios hechos en la grilla de edición.
    /// </summary>
    IReadOnlyList<ComandoSql> GenerarComandosDeEdicion(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<CambioDeFila> cambios);
}
