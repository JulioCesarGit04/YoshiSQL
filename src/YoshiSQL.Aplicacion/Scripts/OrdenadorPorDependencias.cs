using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Ordena vistas y funciones para que cada una se cree después de las que usa.
/// En orden alfabético, una vista que usa a otra de nombre posterior falla con "nombre de objeto no válido".
/// </summary>
public static class OrdenadorPorDependencias
{
    /// <returns>
    /// Los mismos objetos, con los requeridos primero. Se respeta el orden original en todo lo demás;
    /// si hay un ciclo (no debería), esos objetos quedan al final en su orden original.
    /// </returns>
    public static IReadOnlyList<ObjetoDeEsquema> Ordenar(
        IReadOnlyList<ObjetoDeEsquema> objetos,
        IReadOnlyList<DependenciaEntreModulos> dependencias)
    {
        var presentes = objetos.Select(objeto => CrearClave(objeto.Esquema, objeto.Nombre)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Solo cuentan las dependencias entre objetos que van en el script
        var requeridosPorObjeto = dependencias
            .Select(dependencia => (
                Dependiente: CrearClave(dependencia.EsquemaDelDependiente, dependencia.NombreDelDependiente),
                Requerido: CrearClave(dependencia.EsquemaDelRequerido, dependencia.NombreDelRequerido)))
            .Where(par => presentes.Contains(par.Dependiente) && presentes.Contains(par.Requerido))
            .GroupBy(par => par.Dependiente, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Select(par => par.Requerido).ToHashSet(StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);

        var ordenados = new List<ObjetoDeEsquema>(objetos.Count);
        var yaAgregados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pendientes = objetos.ToList();

        while (pendientes.Count > 0)
        {
            var listos = pendientes
                .Where(objeto => !requeridosPorObjeto.TryGetValue(CrearClave(objeto.Esquema, objeto.Nombre), out var requeridos)
                    || requeridos.IsSubsetOf(yaAgregados))
                .ToList();

            if (listos.Count == 0)
            {
                ordenados.AddRange(pendientes);
                break;
            }

            foreach (var objeto in listos)
            {
                ordenados.Add(objeto);
                yaAgregados.Add(CrearClave(objeto.Esquema, objeto.Nombre));
            }

            pendientes = pendientes.Except(listos).ToList();
        }

        return ordenados;
    }

    // Los corchetes evitan que "a.b" + "c" se confunda con "a" + "b.c"
    private static string CrearClave(string esquema, string nombre) => $"[{esquema}].[{nombre}]";
}
