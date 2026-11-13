using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using YoshiSQL.Escritorio.Convertidores;

namespace YoshiSQL.Escritorio.Vistas.Resultados;

/// <summary>
/// Columna de la grilla de resultados: escribe los NULL atenuados y en cursiva (para no confundirlos con el texto
/// "NULL" ni con datos reales) y alinea los números a la derecha.
/// </summary>
internal sealed class ColumnaDeResultados : DataGridTextColumn
{
    private const string ClaseDeCeldaNumerica = "Numerica";

    private static readonly HashSet<string> TiposNumericos = new(StringComparer.OrdinalIgnoreCase)
    {
        "tinyint", "smallint", "int", "bigint", "decimal", "numeric", "money", "smallmoney", "float", "real"
    };

    private readonly int _posicion;

    public ColumnaDeResultados(int posicion, string nombreDelTipo)
    {
        _posicion = posicion;

        if (TiposNumericos.Contains(nombreDelTipo))
        {
            CellStyleClasses.Add(ClaseDeCeldaNumerica);
        }
    }

    protected override Control GenerateElement(DataGridCell celda, object fila)
    {
        var elemento = base.GenerateElement(celda, fila);

        // Las celdas se reutilizan al desplazarse: el aspecto se enlaza al valor, no se fija una sola vez
        if (elemento is TextBlock texto)
        {
            var ruta = $"[{_posicion}]";
            texto.Bind(TextBlock.OpacityProperty, new Binding(ruta) { Converter = NuloAOpacidad.Instancia, Mode = BindingMode.OneTime });
            texto.Bind(TextBlock.FontStyleProperty, new Binding(ruta) { Converter = NuloACursiva.Instancia, Mode = BindingMode.OneTime });
        }

        return elemento;
    }
}
