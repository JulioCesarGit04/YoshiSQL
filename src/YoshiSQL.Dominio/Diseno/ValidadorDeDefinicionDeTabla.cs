namespace YoshiSQL.Dominio.Diseno;

/// <summary>
/// Reglas que debe cumplir una tabla antes de generar su script.
/// </summary>
public static class ValidadorDeDefinicionDeTabla
{
    private static readonly HashSet<string> TiposEnterosParaIdentidad =
        new(["int", "bigint", "smallint", "tinyint", "decimal", "numeric"], StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> Validar(DefinicionDeTabla definicion, DefinicionDeTabla? original)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(definicion.Esquema) || string.IsNullOrWhiteSpace(definicion.Nombre))
        {
            errores.Add("La tabla necesita un esquema y un nombre.");
        }

        if (definicion.Columnas.Count == 0)
        {
            errores.Add("La tabla necesita al menos una columna.");
        }

        if (definicion.Columnas.Any(columna => string.IsNullOrWhiteSpace(columna.Nombre)))
        {
            errores.Add("Todas las columnas necesitan un nombre.");
        }

        errores.AddRange(definicion.Columnas
            .Where(columna => !string.IsNullOrWhiteSpace(columna.Nombre))
            .GroupBy(columna => columna.Nombre.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => $"La columna \"{grupo.Key}\" está repetida."));

        if (definicion.Columnas.Count(columna => columna.EsIdentidad) > 1)
        {
            errores.Add("Solo una columna puede ser de identidad (IDENTITY).");
        }

        errores.AddRange(definicion.Columnas
            .Where(columna => columna.EsIdentidad && !TiposEnterosParaIdentidad.Contains(columna.TipoDeDato.Nombre))
            .Select(columna => $"La columna \"{columna.Nombre}\" no puede ser de identidad con el tipo {columna.TipoDeDato.Describir()}."));

        errores.AddRange(definicion.Columnas
            .Where(columna => columna.EsLlavePrimaria && columna.AdmiteNulos)
            .Select(columna => $"La columna \"{columna.Nombre}\" es llave primaria y no puede admitir nulos."));

        if (original is not null)
        {
            errores.AddRange(ValidarCambiosDeIdentidad(definicion, original));
        }

        return errores;
    }

    /// <summary>
    /// SQL Server no permite agregar ni quitar IDENTITY a una columna que ya existe.
    /// </summary>
    private static IEnumerable<string> ValidarCambiosDeIdentidad(DefinicionDeTabla definicion, DefinicionDeTabla original) =>
        from columna in definicion.Columnas
        where !columna.EsNueva
        let columnaOriginal = original.Columnas.FirstOrDefault(existente => existente.Nombre == columna.NombreOriginal)
        where columnaOriginal is not null && columnaOriginal.EsIdentidad != columna.EsIdentidad
        select $"No se puede cambiar IDENTITY en la columna existente \"{columna.Nombre}\". Crea una columna nueva en su lugar.";
}
