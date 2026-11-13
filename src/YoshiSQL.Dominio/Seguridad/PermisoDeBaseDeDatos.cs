namespace YoshiSQL.Dominio.Seguridad;

/// <summary>
/// Un permiso concedido o denegado dentro de la base (GRANT / DENY).
/// </summary>
/// <param name="EsDenegacion">Verdadero para DENY; falso para GRANT.</param>
/// <param name="Permiso">Nombre del permiso, ej. SELECT, EXECUTE, VIEW DEFINITION.</param>
/// <param name="Esquema">Esquema sobre el que aplica (o del objeto); nulo si el permiso es de toda la base.</param>
/// <param name="Objeto">Tabla, vista, procedimiento o función; nulo si el permiso es de un esquema o de toda la base.</param>
/// <param name="Columna">Columna, cuando el permiso es solo sobre ella.</param>
/// <param name="Beneficiario">Usuario o rol que recibe el permiso.</param>
/// <param name="PuedeOtorgarlo">Verdadero si se concedió WITH GRANT OPTION.</param>
public sealed record PermisoDeBaseDeDatos(
    bool EsDenegacion,
    string Permiso,
    string? Esquema,
    string? Objeto,
    string? Columna,
    string Beneficiario,
    bool PuedeOtorgarlo);
