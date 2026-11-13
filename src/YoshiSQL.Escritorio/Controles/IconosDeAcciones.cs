using Avalonia.Media;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// Icono de cada opción del menú contextual del explorador. Se elige por una palabra del texto de la opción
/// en vez de declararlo en cada una: así toda acción nueva recibe su icono sola y las de un mismo tipo
/// (todas las que generan un script, todas las que eliminan) se ven iguales.
/// </summary>
public static class IconosDeAcciones
{
    // El orden importa: gana la primera palabra que aparezca en el texto.
    // "DROP" y "Eliminar" van antes que "script" para que "Generar script DROP TABLE" lleve el icono de eliminar.
    private static readonly (string Palabra, Geometry Forma)[] PorPalabra =
    [
        ("DROP", Crear("M3,4.5 H13 M6.5,4.5 V3 H9.5 V4.5 M4.5,4.5 L5,13.5 H11 L11.5,4.5 M7,7 V11 M9,7 V11")),
        ("Eliminar", Crear("M3,4.5 H13 M6.5,4.5 V3 H9.5 V4.5 M4.5,4.5 L5,13.5 H11 L11.5,4.5 M7,7 V11 M9,7 V11")),
        ("Desconectar", Crear("M6,2 V4.5 M10,2 V4.5 M4.5,4.5 H11.5 V7.5 A3.5,3.5 0 0 1 4.5,7.5 Z M8,11 V14 M2.5,2.5 L13.5,13.5")),
        ("Seleccionar", Crear("M2,3 H14 V13 H2 Z M2,6.5 H14 M2,9.75 H14 M6.5,6.5 V13")),
        ("Editar", Crear("M3,13 L3.5,10.5 L10.5,3.5 L12.5,5.5 L5.5,12.5 Z M9.5,4.5 L11.5,6.5")),
        ("Diseñar", Crear("M3,13 L3.5,10.5 L10.5,3.5 L12.5,5.5 L5.5,12.5 Z M9.5,4.5 L11.5,6.5")),
        ("Modificar", Crear("M3,13 L3.5,10.5 L10.5,3.5 L12.5,5.5 L5.5,12.5 Z M9.5,4.5 L11.5,6.5")),
        ("Renombrar", Crear("M2.5,11.5 L5.5,4 L8.5,11.5 M3.7,8.5 H7.3 M11.5,3 V13 M10,3 H13 M10,13 H13")),
        ("Nueva", Crear("M8,3 V13 M3,8 H13")),
        ("Nuevo", Crear("M8,3 V13 M3,8 H13")),
        ("Propiedades", Crear("M8,1.5 A6.5,6.5 0 1 0 8.01,1.5 Z M8,7 V11.5 M8,4.5 V4.75")),
        ("Actualizar", Crear("M13,8 A5,5 0 1 1 11.5,4.5 M11.5,1.5 V4.5 H8.5")),
        ("dependencias", Crear("M2.5,3 H6.5 V6 H2.5 Z M9.5,10 H13.5 V13 H9.5 Z M4.5,6 V11.5 H9.5")),
        ("diagrama", Crear("M1.5,2.5 H7 V6.5 H1.5 Z M9,9.5 H14.5 V13.5 H9 Z M4.25,6.5 V11.5 H9")),
        ("Exportar", Crear("M8,2 V10 M5,7 L8,10 L11,7 M3,12.5 H13")),
        ("Respaldar", Crear("M3,2.5 H10.5 L13.5,5.5 V13.5 H3 Z M5,2.5 V6 H10 M5.5,9 H11 V13.5 H5.5 Z")),
        ("Restaurar", Crear("M3,8 A5,5 0 1 0 4.5,4.5 M4.5,1.5 V4.5 H7.5 M8,5.5 V8.2 L10,10")),
        ("Salud", Crear("M1.5,8 H4.5 L6,4 L8.5,12 L10.5,6.5 L11.5,8 H14.5")),
        ("monitor", Crear("M1.5,8 H4.5 L6,4 L8.5,12 L10.5,6.5 L11.5,8 H14.5")),
        ("fragmentación", Crear("M3,4 H13 M3,8 H10 M3,12 H7")),
        ("Reconstruir", Crear("M3,4 H13 M3,8 H10 M3,12 H7")),
        ("Reorganizar", Crear("M3,4 H13 M3,8 H10 M3,12 H7")),
        ("EXEC", Crear("M5,3.2 L13,8 L5,12.8 Z")),
        ("consulta", Crear("M4,2 H9.5 L12,4.5 V14 H4 Z M9.5,2 V4.5 H12 M8,7 V11 M6,9 H10")),
        ("script", Crear("M3.5,1.5 H10 L12.5,4 V14.5 H3.5 Z M5.5,7 H10.5 M5.5,9.5 H10.5 M5.5,12 H8.5")),
        ("log", Crear("M3.5,1.5 H10 L12.5,4 V14.5 H3.5 Z M5.5,7 H10.5 M5.5,9.5 H10.5 M5.5,12 H8.5"))
    ];

    /// <returns>El icono de la opción, o nulo si ninguna palabra coincide (la opción queda sin icono).</returns>
    public static Geometry? Buscar(string textoDeLaOpcion)
    {
        foreach (var (palabra, forma) in PorPalabra)
        {
            if (textoDeLaOpcion.Contains(palabra, StringComparison.OrdinalIgnoreCase))
            {
                return forma;
            }
        }

        return null;
    }

    private static Geometry Crear(string datosDeLaForma) => StreamGeometry.Parse(datosDeLaForma);
}
