using Avalonia.Controls;
using Avalonia.Interactivity;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Confirma la eliminación de una base de datos y pide las opciones (cerrar conexiones, historial).
/// Para evitar borrar la base equivocada, el botón se activa solo al escribir su nombre.
/// Devuelve las opciones elegidas o null si se cancela.
/// </summary>
public partial class DialogoDeEliminacionDeBaseDeDatos : Window
{
    private readonly string _baseDeDatos = string.Empty;

    public DialogoDeEliminacionDeBaseDeDatos()
    {
        InitializeComponent();
    }

    public DialogoDeEliminacionDeBaseDeDatos(string baseDeDatos)
        : this()
    {
        _baseDeDatos = baseDeDatos;
        Encabezado.Text = $"¿Eliminar la base de datos \"{baseDeDatos}\"?";
        EtiquetaDeConfirmacion.Text = $"Para confirmar, escribe el nombre de la base de datos ({baseDeDatos}):";
        Opened += (_, _) => NombreDeConfirmacion.Focus();
    }

    private void AlCambiarNombreDeConfirmacion(object? remitente, TextChangedEventArgs argumentos) =>
        BotonEliminar.IsEnabled = string.Equals(NombreDeConfirmacion.Text?.Trim(), _baseDeDatos, StringComparison.Ordinal);

    private void AlAceptar(object? remitente, RoutedEventArgs argumentos) =>
        Close(new OpcionesDeEliminacionDeBaseDeDatos(
            CerrarConexionesExistentes: CerrarConexiones.IsChecked == true,
            EliminarHistorialDeRespaldos: EliminarHistorial.IsChecked == true));

    private void AlCancelar(object? remitente, RoutedEventArgs argumentos) => Close(null);
}
