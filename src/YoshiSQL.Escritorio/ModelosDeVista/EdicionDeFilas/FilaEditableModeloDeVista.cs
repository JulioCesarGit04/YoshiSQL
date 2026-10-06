using CommunityToolkit.Mvvm.ComponentModel;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Escritorio.Convertidores;

namespace YoshiSQL.Escritorio.ModelosDeVista.EdicionDeFilas;

/// <summary>
/// Una fila de la grilla de edición. Recuerda los valores originales para ubicar la fila al guardar
/// y guarda como texto lo que el usuario escribe; el texto "NULL" significa valor nulo.
/// </summary>
public sealed partial class FilaEditableModeloDeVista : ModeloDeVistaBase
{
    public const string TextoDeNulo = "NULL";

    // Nombre que usa la grilla para refrescar todas las celdas enlazadas con [posición]
    private const string NombreDelIndexador = "Item[]";

    private readonly object?[] _valores;

    public FilaEditableModeloDeVista(object?[] valoresOriginales, bool esNueva = false)
    {
        ValoresOriginales = valoresOriginales;
        _valores = (object?[])valoresOriginales.Clone();
        Estado = esNueva ? EstadoDeFila.Nueva : EstadoDeFila.SinCambios;
    }

    public object?[] ValoresOriginales { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneCambios))]
    public partial EstadoDeFila Estado { get; private set; }

    public bool TieneCambios => Estado != EstadoDeFila.SinCambios;

    public string? this[int posicion]
    {
        get => (string?)ValorDeCeldaATexto.Instancia.Convert(_valores[posicion], typeof(string), null, System.Globalization.CultureInfo.InvariantCulture);
        set
        {
            // La grilla devuelve el texto al terminar de editar aunque no haya cambiado: eso no cuenta como cambio
            if (value == this[posicion])
            {
                return;
            }

            _valores[posicion] = string.Equals(value, TextoDeNulo, StringComparison.OrdinalIgnoreCase) ? null : value;

            if (Estado == EstadoDeFila.SinCambios)
            {
                Estado = EstadoDeFila.Modificada;
            }

            OnPropertyChanged(NombreDelIndexador);
        }
    }

    public void MarcarParaEliminar() => Estado = EstadoDeFila.Eliminada;

    public CambioDeFila CrearCambio() =>
        new(Estado, Estado == EstadoDeFila.Nueva ? [] : ValoresOriginales, _valores);

    /// <summary>
    /// Tras guardar la fila, los valores actuales pasan a ser los originales y la fila queda sin cambios,
    /// para que una edición posterior ubique bien la fila.
    /// </summary>
    public void ConfirmarGuardado()
    {
        Array.Copy(_valores, ValoresOriginales, _valores.Length);
        Estado = EstadoDeFila.SinCambios;
    }
}
