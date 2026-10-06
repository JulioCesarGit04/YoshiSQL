using System.Globalization;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Arma el texto legible de las ventanas de propiedades (tabla y base de datos).
/// </summary>
internal static class DescriptorDePropiedades
{
    private static readonly CultureInfo Cultura = CultureInfo.CurrentCulture;

    public static string Describir(Tabla tabla, PropiedadesDeTabla propiedades)
    {
        string[] lineas =
        [
            $"Tabla:              {tabla.NombreCompleto}",
            $"Filas:              {propiedades.Filas.ToString("N0", Cultura)}",
            $"Columnas:           {propiedades.Columnas}",
            $"Índices:            {propiedades.Indices}",
            $"Espacio reservado:  {DescribirTamano(propiedades.EspacioTotalKb)}",
            $"Espacio usado:      {DescribirTamano(propiedades.EspacioUsadoKb)}",
            $"Creada:             {DescribirFecha(propiedades.Creacion)}",
            $"Última modificación:{" "}{DescribirFecha(propiedades.Modificacion)}"
        ];

        return string.Join(Environment.NewLine, lineas);
    }

    public static string Describir(PropiedadesDeBaseDeDatos propiedades)
    {
        string[] lineas =
        [
            $"Base de datos:       {propiedades.Nombre}",
            $"Estado:              {propiedades.Estado}",
            $"Modelo de recuperación: {propiedades.ModeloDeRecuperacion}",
            $"Nivel de compatibilidad: {propiedades.NivelDeCompatibilidad}",
            $"Intercalación:       {propiedades.Intercalacion ?? "(predeterminada)"}",
            $"Propietario:         {propiedades.Propietario ?? "(desconocido)"}",
            $"Tamaño en disco:     {DescribirTamano(propiedades.TamanoKb)}",
            $"Creada:              {DescribirFecha(propiedades.Creacion)}"
        ];

        return string.Join(Environment.NewLine, lineas);
    }

    public static string Describir(ObjetoDeEsquema objeto, IReadOnlyList<DependenciaDeObjeto> dependencias)
    {
        if (dependencias.Count == 0)
        {
            return $"{objeto.NombreCompleto} no tiene dependencias registradas.";
        }

        var texto = new System.Text.StringBuilder();

        foreach (var grupo in dependencias.GroupBy(dependencia => dependencia.Relacion))
        {
            if (texto.Length > 0)
            {
                texto.AppendLine();
            }

            texto.AppendLine($"{grupo.Key}:");

            foreach (var dependencia in grupo)
            {
                texto.AppendLine($"    {dependencia.NombreCompleto}");
            }
        }

        return texto.ToString().TrimEnd();
    }

    private static string DescribirFecha(DateTime fecha) => fecha.ToString("yyyy-MM-dd HH:mm", Cultura);

    private static string DescribirTamano(long kilobytes) => kilobytes switch
    {
        >= 1024 * 1024 => $"{(kilobytes / (1024.0 * 1024.0)).ToString("N2", Cultura)} GB",
        >= 1024 => $"{(kilobytes / 1024.0).ToString("N2", Cultura)} MB",
        _ => $"{kilobytes.ToString("N0", Cultura)} KB"
    };
}
