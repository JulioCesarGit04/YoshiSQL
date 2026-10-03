namespace YoshiSQL.Dominio.Seguridad;

/// <param name="EsFijo">Roles que trae SQL Server (db_owner, db_datareader...) y que no se pueden eliminar.</param>
public sealed record RolDeBaseDeDatos(string Nombre, bool EsFijo);
