using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diagramas;

/// <summary>
/// Posición guardada de cada tabla del diagrama, identificada por su nombre completo (esquema.tabla).
/// </summary>
public sealed record DisposicionDeDiagrama(IReadOnlyDictionary<string, PosicionEnElDiagrama> PosicionesPorTabla)
{
    public PosicionEnElDiagrama? ObtenerPosicion(Tabla tabla) =>
        PosicionesPorTabla.TryGetValue(tabla.NombreCompleto, out var posicion) ? posicion : null;

    public static DisposicionDeDiagrama CrearDesde(IEnumerable<NodoDeTabla> nodos) =>
        new(nodos
            .Where(nodo => nodo.Posicion is not null)
            .ToDictionary(nodo => nodo.Tabla.NombreCompleto, nodo => nodo.Posicion!.Value));
}
