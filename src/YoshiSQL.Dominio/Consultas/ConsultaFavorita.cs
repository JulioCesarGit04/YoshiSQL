namespace YoshiSQL.Dominio.Consultas;

/// <summary>
/// Una consulta guardada por el usuario con un nombre, para reutilizarla con un clic.
/// </summary>
public sealed record ConsultaFavorita(string Nombre, string Sql);
