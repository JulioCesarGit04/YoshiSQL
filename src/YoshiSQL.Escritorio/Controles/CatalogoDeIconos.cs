using Avalonia.Media;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Íconos lineales de 16x16 dibujados con geometría (sin imágenes ni emojis) y su color por tipo de nodo.
/// </summary>
public static class CatalogoDeIconos
{
    private static readonly Dictionary<TipoDeNodo, (Geometry Forma, IBrush Color)> Iconos = new()
    {
        [TipoDeNodo.Servidor] = Crear("M2.5,2.5 H13.5 V7 H2.5 Z M2.5,9 H13.5 V13.5 H2.5 Z M5,4.75 H5.5 M5,11.25 H5.5", "#AEB4BC"),
        [TipoDeNodo.Carpeta] = Crear("M1.5,4 V13 H14.5 V5.5 H7.5 L6,4 Z", "#D9B66A"),
        [TipoDeNodo.CarpetaDeTablas] = Crear("M1.5,4 V13 H14.5 V5.5 H7.5 L6,4 Z", "#D9B66A"),
        [TipoDeNodo.CarpetaDeBasesDeDatos] = Crear("M1.5,4 V13 H14.5 V5.5 H7.5 L6,4 Z", "#D9B66A"),
        [TipoDeNodo.BaseDeDatos] = Crear("M3,4 C3,2 13,2 13,4 V12 C13,14 3,14 3,12 Z M3,4 C3,6 13,6 13,4 M3,8 C3,10 13,10 13,8", "#6CC24A"),
        [TipoDeNodo.BaseDeDatosSinConexion] = Crear("M3,4 C3,2 13,2 13,4 V12 C13,14 3,14 3,12 Z M3,4 C3,6 13,6 13,4", "#6B7078"),
        [TipoDeNodo.Diagrama] = Crear("M1.5,2.5 H7 V6.5 H1.5 Z M9,9.5 H14.5 V13.5 H9 Z M4.25,6.5 V11.5 H9", "#7FB3E0"),
        [TipoDeNodo.Tabla] = Crear("M2,3 H14 V13 H2 Z M2,6.5 H14 M2,9.75 H14 M6.5,6.5 V13", "#61AFEF"),
        [TipoDeNodo.Vista] = Crear("M1.5,8 C4,3.5 12,3.5 14.5,8 C12,12.5 4,12.5 1.5,8 Z M6,8 A2,2 0 1 0 10,8 A2,2 0 1 0 6,8 Z", "#C678DD"),
        [TipoDeNodo.ProcedimientoAlmacenado] = Crear("M3.5,1.5 H10 L12.5,4 V14.5 H3.5 Z M5.5,7 H10.5 M5.5,9.5 H10.5 M5.5,12 H8.5", "#D19A66"),
        [TipoDeNodo.Funcion] = Crear("M10.5,2.5 C8.5,2.5 8,3.5 8,5 V13.5 M5.5,7 H10.5", "#56B6C2"),
        [TipoDeNodo.Columna] = Crear("M5,2.5 H11 V13.5 H5 Z M5,6 H11", "#9DA5B4"),
        [TipoDeNodo.LlavePrimaria] = Crear("M2,8 A3,3 0 1 0 8,8 A3,3 0 1 0 2,8 Z M8,8 H14.5 M12,8 V10.5 M14.5,8 V10", "#E5C07B"),
        [TipoDeNodo.Indice] = Crear("M3,4 H13 M3,8 H10 M3,12 H7", "#98C379"),
        [TipoDeNodo.InicioDeSesion] = Crear("M8,2.5 A2.75,2.75 0 1 0 8.01,2.5 Z M2.5,14 C2.5,10.5 13.5,10.5 13.5,14", "#C8CDD4"),
        [TipoDeNodo.Usuario] = Crear("M8,2.5 A2.75,2.75 0 1 0 8.01,2.5 Z M2.5,14 C2.5,10.5 13.5,10.5 13.5,14", "#6CC24A"),
        [TipoDeNodo.Rol] = Crear("M5.5,3 A2.25,2.25 0 1 0 5.51,3 Z M1,13 C1,10 10,10 10,13 M11,4 A2,2 0 1 0 11.01,4 Z M10.5,9.5 C13,9.3 15,10.5 15,12.5", "#D19A66"),
        [TipoDeNodo.Cargando] = Crear("M8,2 A6,6 0 1 1 2,8", "#6B7078"),
        [TipoDeNodo.Error] = Crear("M8,1.5 A6.5,6.5 0 1 0 8.01,1.5 Z M8,4.5 V9 M8,11.25 V11.5", "#F07178")
    };

    public static Geometry ObtenerForma(TipoDeNodo tipo) => Iconos[tipo].Forma;

    public static IBrush ObtenerColor(TipoDeNodo tipo) => Iconos[tipo].Color;

    private static (Geometry Forma, IBrush Color) Crear(string datosDeLaForma, string colorHexadecimal) =>
        (StreamGeometry.Parse(datosDeLaForma), new SolidColorBrush(Color.Parse(colorHexadecimal)));
}
