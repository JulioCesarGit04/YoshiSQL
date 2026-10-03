namespace YoshiSQL.Dominio.Sesion;

/// <summary>
/// Lo necesario para volver a abrir una pestaña la próxima vez que se inicie YoshiSQL.
/// </summary>
/// <param name="Texto">Contenido del editor; nulo si basta con volver a leer el archivo guardado.</param>
/// <param name="TieneCambiosSinGuardar">Verdadero si el texto tenía cambios que el usuario no había guardado.</param>
public sealed record PestanaGuardada(
    TipoDePestana Tipo,
    Guid IdDelPerfil,
    string BaseDeDatos,
    string NombreDelArchivo,
    string? RutaDelArchivo,
    string? Texto,
    bool TieneCambiosSinGuardar)
{
    public bool TieneTexto => Texto is not null;
}
