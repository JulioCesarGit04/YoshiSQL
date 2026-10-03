using System.Globalization;
using System.Reflection;
using System.Resources;
using AvaloniaEdit;

namespace YoshiSQL.Escritorio.Controles;

/// <summary>
/// AvaloniaEdit trae sus textos (panel de búsqueda) solo en inglés y chino, y no ofrece una forma
/// pública de traducirlos. Se reemplaza su administrador de recursos por uno que responde en español.
/// Si una versión futura cambia su estructura interna, simplemente se conservan los textos en inglés.
/// </summary>
internal static class TraduccionDeAvaloniaEdit
{
    private const string NombreDelCampoDeRecursos = "resourceMan";

    private static readonly Dictionary<string, string> TextosEnEspanol = new()
    {
        ["SearchLabel"] = "Buscar...",
        ["ReplaceLabel"] = "Reemplazar por...",
        ["SearchMatchCaseText"] = "Distinguir mayúsculas y minúsculas",
        ["SearchMatchWholeWordsText"] = "Solo palabras completas",
        ["SearchUseRegexText"] = "Usar expresiones regulares",
        ["SearchFindNextText"] = "Buscar siguiente (F3)",
        ["SearchFindPreviousText"] = "Buscar anterior (Mayús+F3)",
        ["SearchReplaceNext"] = "Reemplazar siguiente (Alt+R)",
        ["SearchReplaceAll"] = "Reemplazar todo (Alt+A)",
        ["SearchToggleReplace"] = "Mostrar u ocultar reemplazar",
        ["SearchErrorText"] = "Error: ",
        ["SearchNoMatchesFoundText"] = "Sin coincidencias",
        ["Search1Match"] = "1 coincidencia",
        ["SearchXMatches"] = "{0} coincidencias",
        ["SearchXOfY"] = "{0} de {1}"
    };

    public static void Aplicar()
    {
        var campoDeRecursos = typeof(SR).GetField(NombreDelCampoDeRecursos, BindingFlags.NonPublic | BindingFlags.Static);

        if (campoDeRecursos?.FieldType != typeof(ResourceManager))
        {
            return;
        }

        campoDeRecursos.SetValue(null, new RecursosEnEspanol(SR.ResourceManager));
    }

    private sealed class RecursosEnEspanol : ResourceManager
    {
        private readonly ResourceManager _recursosOriginales;

        public RecursosEnEspanol(ResourceManager recursosOriginales)
        {
            _recursosOriginales = recursosOriginales;
        }

        public override string? GetString(string nombre) => GetString(nombre, culture: null);

        public override string? GetString(string nombre, CultureInfo? culture) =>
            TextosEnEspanol.TryGetValue(nombre, out var texto) ? texto : _recursosOriginales.GetString(nombre, culture);
    }
}
