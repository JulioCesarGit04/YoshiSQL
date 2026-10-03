using YoshiSQL.Dominio.Diagramas;

namespace YoshiSQL.Aplicacion.Diagramas;

/// <summary>
/// Acomoda las tablas en una cuadrícula sin que se encimen, colocando juntas
/// las tablas que están relacionadas entre sí.
/// </summary>
public static class OrganizadorDeDiagrama
{
    public const double Margen = 40;
    public const double SeparacionHorizontal = 80;
    public const double SeparacionVertical = 60;
    private const int ColumnasMaximasDeLaCuadricula = 6;

    /// <summary>
    /// Ubica todas las tablas desde cero, ignorando las posiciones anteriores.
    /// </summary>
    public static void OrganizarTodo(Diagrama diagrama, Func<NodoDeTabla, TamanoDeNodo> medir)
    {
        var nodosOrdenados = OrdenarPorCercania(diagrama.Nodos, diagrama.Relaciones);
        UbicarEnCuadricula(nodosOrdenados, medir, posicionYInicial: Margen);
    }

    /// <summary>
    /// Ubica solo las tablas que aún no tienen posición (ej. tablas nuevas), debajo de las existentes.
    /// </summary>
    public static void OrganizarNodosSinPosicion(Diagrama diagrama, Func<NodoDeTabla, TamanoDeNodo> medir)
    {
        var nodosSinPosicion = diagrama.Nodos.Where(nodo => !nodo.TienePosicion).ToList();

        if (nodosSinPosicion.Count == 0)
        {
            return;
        }

        var nodosUbicados = diagrama.Nodos.Where(nodo => nodo.TienePosicion).ToList();
        var posicionYInicial = nodosUbicados.Count == 0
            ? Margen
            : nodosUbicados.Max(nodo => nodo.Posicion!.Value.Y + medir(nodo).Alto) + SeparacionVertical;

        var nodosOrdenados = OrdenarPorCercania(nodosSinPosicion, diagrama.Relaciones);
        UbicarEnCuadricula(nodosOrdenados, medir, posicionYInicial);
    }

    private static void UbicarEnCuadricula(
        IReadOnlyList<NodoDeTabla> nodos,
        Func<NodoDeTabla, TamanoDeNodo> medir,
        double posicionYInicial)
    {
        var columnasPorFila = CalcularColumnasPorFila(nodos.Count);
        var posicionY = posicionYInicial;

        foreach (var fila in nodos.Chunk(columnasPorFila))
        {
            var posicionX = Margen;
            var altoDeLaFila = 0.0;

            foreach (var nodo in fila)
            {
                var tamano = medir(nodo);
                nodo.Posicion = new PosicionEnElDiagrama(posicionX, posicionY);
                posicionX += tamano.Ancho + SeparacionHorizontal;
                altoDeLaFila = Math.Max(altoDeLaFila, tamano.Alto);
            }

            posicionY += altoDeLaFila + SeparacionVertical;
        }
    }

    private static int CalcularColumnasPorFila(int cantidadDeNodos) =>
        Math.Clamp((int)Math.Ceiling(Math.Sqrt(cantidadDeNodos)), 1, ColumnasMaximasDeLaCuadricula);

    /// <summary>
    /// Recorre el grafo de relaciones empezando por la tabla más conectada, para que
    /// las tablas relacionadas queden una al lado de la otra.
    /// </summary>
    private static List<NodoDeTabla> OrdenarPorCercania(
        IReadOnlyList<NodoDeTabla> nodos,
        IReadOnlyList<RelacionEntreTablas> relaciones)
    {
        var vecinosPorTabla = nodos.ToDictionary(
            nodo => nodo.Tabla,
            nodo => relaciones
                .Where(relacion => !relacion.EsAutorreferencia)
                .Where(relacion => relacion.TablaOrigen == nodo.Tabla || relacion.TablaDestino == nodo.Tabla)
                .Select(relacion => relacion.TablaOrigen == nodo.Tabla ? relacion.TablaDestino : relacion.TablaOrigen)
                .Distinct()
                .ToList());

        var nodosPorTabla = nodos.ToDictionary(nodo => nodo.Tabla);
        var nodosPendientes = nodos
            .OrderByDescending(nodo => vecinosPorTabla[nodo.Tabla].Count)
            .ThenBy(nodo => nodo.Tabla.NombreCompleto, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nodosOrdenados = new List<NodoDeTabla>(nodos.Count);
        var nodosVisitados = new HashSet<NodoDeTabla>();

        foreach (var nodoInicial in nodosPendientes)
        {
            var porVisitar = new Queue<NodoDeTabla>([nodoInicial]);

            while (porVisitar.TryDequeue(out var nodoActual))
            {
                if (!nodosVisitados.Add(nodoActual))
                {
                    continue;
                }

                nodosOrdenados.Add(nodoActual);

                foreach (var tablaVecina in vecinosPorTabla[nodoActual.Tabla])
                {
                    if (nodosPorTabla.TryGetValue(tablaVecina, out var nodoVecino) && !nodosVisitados.Contains(nodoVecino))
                    {
                        porVisitar.Enqueue(nodoVecino);
                    }
                }
            }
        }

        return nodosOrdenados;
    }
}
