using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.Convertidores;

public sealed class TipoDeMensajeAColor : IValueConverter
{
    public static readonly TipoDeMensajeAColor Instancia = new();

    private static readonly IBrush ColorDeInformacion = new SolidColorBrush(Color.Parse("#D4D7DC"));
    private static readonly IBrush ColorDeAdvertencia = new SolidColorBrush(Color.Parse("#E5C07B"));
    private static readonly IBrush ColorDeError = new SolidColorBrush(Color.Parse("#F07178"));

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor switch
        {
            TipoDeMensaje.Error => ColorDeError,
            TipoDeMensaje.Advertencia => ColorDeAdvertencia,
            _ => ColorDeInformacion
        };

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
