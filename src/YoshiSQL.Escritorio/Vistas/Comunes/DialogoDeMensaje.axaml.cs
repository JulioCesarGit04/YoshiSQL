using Avalonia.Controls;

namespace YoshiSQL.Escritorio.Vistas.Comunes;

/// <summary>
/// Ventana sencilla con un mensaje y botones configurables (Aceptar, Guardar / No guardar...).
/// </summary>
public partial class DialogoDeMensaje : Window
{
    public DialogoDeMensaje()
    {
        InitializeComponent();
    }

    public DialogoDeMensaje(string titulo, string mensaje, IReadOnlyList<OpcionDeDialogo> opciones)
        : this()
    {
        Title = titulo;
        TextoDelMensaje.Text = mensaje;

        foreach (var opcion in opciones)
        {
            PanelDeBotones.Children.Add(CrearBoton(opcion));
        }
    }

    private Button CrearBoton(OpcionDeDialogo opcion)
    {
        var boton = new Button
        {
            Content = opcion.Texto,
            MinWidth = 96,
            IsDefault = opcion.EsPrincipal,
            IsCancel = opcion.EsCancelar
        };

        if (opcion.EsPrincipal)
        {
            boton.Classes.Add("Primario");
        }

        boton.Click += (_, _) => Close(opcion.Valor);
        return boton;
    }
}
