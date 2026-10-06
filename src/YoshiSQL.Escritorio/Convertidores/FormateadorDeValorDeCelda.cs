using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace YoshiSQL.Escritorio.Convertidores;

/// <summary>
/// Prepara el valor de una celda para verlo en una ventana: formatea XML y JSON con sangría
/// y deja el resto como texto legible (NULL, fechas, binarios en hexadecimal).
/// </summary>
public static class FormateadorDeValorDeCelda
{
    public static string Formatear(object? valor)
    {
        if (valor is not string texto)
        {
            return ValorDeCeldaATexto.Instancia.Convert(valor, typeof(string), null, CultureInfo.InvariantCulture) as string ?? string.Empty;
        }

        var recortado = texto.TrimStart();

        if (recortado.StartsWith('<') && IntentarFormatearXml(texto, out var xml))
        {
            return xml;
        }

        if ((recortado.StartsWith('{') || recortado.StartsWith('[')) && IntentarFormatearJson(texto, out var json))
        {
            return json;
        }

        return texto;
    }

    private static bool IntentarFormatearXml(string texto, out string formateado)
    {
        try
        {
            formateado = XDocument.Parse(texto).ToString();
            return true;
        }
        catch (System.Xml.XmlException)
        {
            formateado = string.Empty;
            return false;
        }
    }

    private static bool IntentarFormatearJson(string texto, out string formateado)
    {
        try
        {
            using var documento = JsonDocument.Parse(texto);
            formateado = JsonSerializer.Serialize(documento.RootElement, new JsonSerializerOptions { WriteIndented = true });
            return true;
        }
        catch (JsonException)
        {
            formateado = string.Empty;
            return false;
        }
    }
}
