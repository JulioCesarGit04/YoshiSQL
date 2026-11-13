namespace YoshiSQL.Dominio.Conexiones;

/// <summary>
/// Datos guardados de una conexión. La contraseña nunca forma parte del perfil:
/// se guarda aparte en el almacén de credenciales.
/// </summary>
public sealed record PerfilDeConexion
{
    public const int PuertoPredeterminado = 1433;
    public const string BaseDeDatosDelSistema = "master";

    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Servidor { get; init; }
    public int Puerto { get; init; } = PuertoPredeterminado;
    public TipoDeAutenticacion TipoDeAutenticacion { get; init; } = TipoDeAutenticacion.SqlServer;
    public string Usuario { get; init; } = string.Empty;
    public string BaseDeDatosPredeterminada { get; init; } = BaseDeDatosDelSistema;

    // Una instalación local de SQL Server en Linux usa un certificado autofirmado
    public bool ConfiarEnCertificadoDelServidor { get; init; } = true;

    public bool RecordarContrasena { get; init; } = true;

    /// <summary>
    /// Color de la conexión en formato "#RRGGBB" para distinguirla en la barra de estado
    /// (por ejemplo, rojo para producción). Nulo si el usuario no eligió ninguno.
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Nombre que el usuario le da a la conexión (ej. "Producción - ventas"), para reconocerla en la lista
    /// sin tener que recordar la dirección del servidor. Opcional.
    /// </summary>
    public string? Nombre { get; init; }

    /// <summary>
    /// Carpeta en la que el usuario agrupa la conexión (ej. "Producción", "Desarrollo"). Opcional.
    /// </summary>
    public string? Grupo { get; init; }

    public string NombreVisible => Puerto == PuertoPredeterminado ? Servidor : $"{Servidor},{Puerto}";

    /// <summary>
    /// Lo que identifica a la conexión en la lista de guardadas: su nombre (o el servidor si no tiene),
    /// precedido por su grupo, ej. "Producción / Ventas".
    /// </summary>
    public string Titulo
    {
        get
        {
            var nombre = string.IsNullOrWhiteSpace(Nombre) ? NombreVisible : Nombre;
            return string.IsNullOrWhiteSpace(Grupo) ? nombre : $"{Grupo} / {nombre}";
        }
    }

    /// <summary>Texto secundario junto al título: el servidor (si el título es un nombre) y el usuario.</summary>
    public string Detalle => string.IsNullOrWhiteSpace(Nombre) ? Usuario : $"{NombreVisible} {Usuario}".TrimEnd();
}
