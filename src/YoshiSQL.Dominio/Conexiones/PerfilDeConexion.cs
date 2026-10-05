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

    public string NombreVisible => Puerto == PuertoPredeterminado ? Servidor : $"{Servidor},{Puerto}";
}
