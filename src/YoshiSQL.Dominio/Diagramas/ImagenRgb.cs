namespace YoshiSQL.Dominio.Diagramas;

/// <summary>
/// Imagen sin comprimir: tres bytes por punto (rojo, verde, azul), fila por fila desde arriba.
/// </summary>
/// <param name="Pixeles">Debe medir exactamente Ancho × Alto × 3 bytes.</param>
public sealed record ImagenRgb(int Ancho, int Alto, byte[] Pixeles)
{
    public const int BytesPorPixel = 3;
}
