using System.Globalization;
using Avalonia.Data.Converters;
using YoshiSQL.Escritorio.Controles;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;

namespace YoshiSQL.Escritorio.Convertidores;

public sealed class TipoDeNodoAIcono : IValueConverter
{
    public static readonly TipoDeNodoAIcono Instancia = new();

    public object? Convert(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        valor is TipoDeNodo tipo ? CatalogoDeIconos.ObtenerForma(tipo) : null;

    public object? ConvertBack(object? valor, Type tipoDestino, object? parametro, CultureInfo cultura) =>
        throw new NotSupportedException();
}
