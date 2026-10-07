using Avalonia.Controls;
using Avalonia.Interactivity;
using YoshiSQL.Aplicacion.Scripts;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Ventana para elegir qué partes de una base de datos exportar a un script.
/// Devuelve las opciones elegidas o null si se cancela.
/// </summary>
public partial class DialogoDeExportacion : Window
{
    public DialogoDeExportacion()
    {
        InitializeComponent();
    }

    public DialogoDeExportacion(string baseDeDatos)
        : this()
    {
        Encabezado.Text = $"Elige qué exportar de \"{baseDeDatos}\":";
    }

    private void AlAceptar(object? remitente, RoutedEventArgs argumentos) =>
        Close(new OpcionesDeExportacion(
            EstructuraDeTablas: EstructuraDeTablas.IsChecked == true,
            DatosDeTablas: DatosDeTablas.IsChecked == true,
            Vistas: Vistas.IsChecked == true,
            Procedimientos: Procedimientos.IsChecked == true,
            Funciones: Funciones.IsChecked == true));

    private void AlCancelar(object? remitente, RoutedEventArgs argumentos) => Close(null);
}
