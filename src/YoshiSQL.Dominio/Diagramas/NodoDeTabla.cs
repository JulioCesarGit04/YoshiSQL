using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Dominio.Diagramas;

/// <summary>
/// Tabla dibujada en el lienzo. La posición es nula hasta que el usuario o el
/// organizador automático la ubican.
/// </summary>
public sealed class NodoDeTabla
{
    public NodoDeTabla(Tabla tabla, IReadOnlyList<Columna> columnas)
    {
        Tabla = tabla;
        Columnas = columnas;
    }

    public Tabla Tabla { get; }

    public IReadOnlyList<Columna> Columnas { get; }

    public PosicionEnElDiagrama? Posicion { get; set; }

    public bool TienePosicion => Posicion is not null;
}
