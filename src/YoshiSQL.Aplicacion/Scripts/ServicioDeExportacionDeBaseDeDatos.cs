using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Arma un único script .sql con la estructura y (opcionalmente) los datos de toda una base de datos,
/// para recrearla en cualquier servidor sin depender de un respaldo .bak.
/// </summary>
/// <remarks>
/// El orden de las secciones no es estético, cada una necesita a las anteriores:
/// esquemas y tipos → secuencias → tablas → datos → índices y UNIQUE (una llave foránea puede apuntar a una UNIQUE)
/// → llaves foráneas → funciones y vistas → valores predeterminados y CHECK (pueden usar funciones)
/// → procedimientos → disparadores (al final, para que no se disparen mientras se cargan los datos).
/// Excepción: las funciones que aparecen en la fórmula de una columna calculada van antes que las tablas.
/// El script se escribe directamente en el destino (un archivo): una base grande no cabe en memoria.
/// </remarks>
public sealed class ServicioDeExportacionDeBaseDeDatos
{
    // Un solo lote con decenas de miles de INSERT obliga al servidor a compilarlos todos juntos:
    // tarda mucho y con tablas grandes falla por falta de memoria. SSMS también los corta por tramos.
    private const int FilasPorLote = 1000;

    // Cada cuántas filas se informa el avance de una tabla grande
    private const int FilasEntreAvisosDeAvance = 20_000;

    private readonly IExploradorDeEsquema _explorador;
    private readonly IEjecutorDeConsultas _ejecutor;
    private readonly IGeneradorDeScripts _generador;
    private readonly TimeProvider _reloj;

    public ServicioDeExportacionDeBaseDeDatos(IProveedorDeBaseDeDatos proveedor, TimeProvider reloj)
    {
        _explorador = proveedor.Explorador;
        _ejecutor = proveedor.Ejecutor;
        _generador = proveedor.GeneradorDeScripts;
        _reloj = reloj;
    }

    /// <summary>
    /// Versión en memoria, para bases pequeñas y pruebas. Para guardar a un archivo se usa <see cref="ExportarAsync"/>.
    /// </summary>
    public async Task<ResultadoDeExportacion> GenerarScriptCompletoAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        OpcionesDeExportacion opciones,
        CancellationToken tokenDeCancelacion)
    {
        await using var destino = new StringWriter();
        var advertencias = await ExportarAsync(servidor, baseDeDatos, opciones, destino, avance: null, tokenDeCancelacion);
        return new ResultadoDeExportacion(destino.ToString(), advertencias);
    }

    /// <param name="destino">Donde se escribe el script a medida que se genera.</param>
    /// <param name="avance">Recibe un texto corto con lo que se está exportando en cada momento.</param>
    /// <returns>Lo que no se pudo exportar completo (también queda como comentario en el script).</returns>
    public async Task<IReadOnlyList<string>> ExportarAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        OpcionesDeExportacion opciones,
        TextWriter destino,
        IProgress<string>? avance,
        CancellationToken tokenDeCancelacion)
    {
        var acceso = servidor.DatosDeAcceso;
        var advertencias = new List<string>();

        destino.WriteLine($"-- Script de la base de datos {baseDeDatos}");
        destino.WriteLine($"-- Generado por YoshiSQL el {_reloj.GetLocalNow():yyyy-MM-dd HH:mm}");
        destino.WriteLine("-- No incluye: usuarios y permisos, índices columnstore/XML/espaciales/de texto completo,");
        destino.WriteLine("-- colecciones de esquemas XML, objetos de ensamblados CLR ni propiedades extendidas.");
        destino.WriteLine();
        AgregarLote(destino, _generador.GenerarUso(baseDeDatos));

        avance?.Report("Leyendo la estructura de la base...");
        var detalles = await _explorador.ObtenerDetallesDelEsquemaAsync(acceso, baseDeDatos, tokenDeCancelacion);

        // Esquemas, tipos y secuencias van antes que todo: sin ellos fallan las tablas, vistas y procedimientos
        // que los usan (ej. un procedimiento con un parámetro de tipo tabla, o un DEFAULT con NEXT VALUE FOR)
        await AgregarEsquemasYTiposAsync(destino, acceso, baseDeDatos, detalles, tokenDeCancelacion);

        var tablas = await _explorador.ObtenerTablasAsync(acceso, baseDeDatos, tokenDeCancelacion);

        var columnasPorTabla = new Dictionary<Tabla, IReadOnlyList<Columna>>();
        if (opciones.EstructuraDeTablas || opciones.DatosDeTablas)
        {
            foreach (var tabla in tablas)
            {
                columnasPorTabla[tabla] = await _explorador.ObtenerColumnasAsync(acceso, baseDeDatos, tabla, tokenDeCancelacion);
            }
        }

        IReadOnlyList<ObjetoDeEsquema> funcionesDeLasTablas = [];

        if (opciones.EstructuraDeTablas)
        {
            var funciones = await _explorador.ObtenerFuncionesAsync(acceso, baseDeDatos, tokenDeCancelacion);
            funcionesDeLasTablas = OrdenadorPorDependencias.Ordenar(
                ElegirFuncionesQueNecesitanLasTablas(tablas, funciones, detalles.DependenciasEntreModulos),
                detalles.DependenciasEntreModulos);

            await AgregarDefinicionesAsync(
                destino, advertencias, acceso, baseDeDatos,
                "Funciones que usan las columnas calculadas de las tablas", funcionesDeLasTablas, tokenDeCancelacion);

            avance?.Report($"Estructura de {tablas.Count} tablas...");
            await AgregarTablasAsync(destino, acceso, baseDeDatos, tablas, columnasPorTabla, detalles, tokenDeCancelacion);
        }

        if (opciones.DatosDeTablas)
        {
            await AgregarDatosAsync(destino, advertencias, servidor, baseDeDatos, tablas, columnasPorTabla, avance, tokenDeCancelacion);
        }

        avance?.Report("Índices, restricciones, vistas y procedimientos...");

        if (opciones.EstructuraDeTablas)
        {
            AgregarSeccion(destino, "Índices y restricciones UNIQUE", detalles.Indices.Select(_generador.GenerarCreacionDeIndice));
            await AgregarLlavesForaneasAsync(destino, acceso, baseDeDatos, tokenDeCancelacion);
        }

        await AgregarFuncionesYVistasAsync(
            destino, advertencias, acceso, baseDeDatos, opciones, detalles, funcionesDeLasTablas, tokenDeCancelacion);

        if (opciones.EstructuraDeTablas)
        {
            AgregarSeccion(destino, "Valores predeterminados", detalles.ValoresPredeterminados.Select(_generador.GenerarValorPredeterminado));
            AgregarSeccion(destino, "Restricciones CHECK", detalles.RestriccionesDeVerificacion.Select(_generador.GenerarRestriccionDeVerificacion));
        }

        if (opciones.Procedimientos)
        {
            var procedimientos = await _explorador.ObtenerProcedimientosAsync(acceso, baseDeDatos, tokenDeCancelacion);
            await AgregarDefinicionesAsync(destino, advertencias, acceso, baseDeDatos, "Procedimientos almacenados", procedimientos, tokenDeCancelacion);
        }

        if (opciones.EstructuraDeTablas)
        {
            AgregarDisparadores(destino, advertencias, tablas, opciones, detalles.Disparadores);
        }

        await destino.FlushAsync(tokenDeCancelacion);
        return advertencias;
    }

    private async Task AgregarEsquemasYTiposAsync(
        TextWriter destino,
        DatosDeAcceso acceso,
        string baseDeDatos,
        DetallesDelEsquema detalles,
        CancellationToken tokenDeCancelacion)
    {
        var esquemas = await _explorador.ObtenerEsquemasDeUsuarioAsync(acceso, baseDeDatos, tokenDeCancelacion);
        var tipos = await _explorador.ObtenerTiposDeUsuarioAsync(acceso, baseDeDatos, tokenDeCancelacion);

        AgregarSeccion(destino, "Esquemas", esquemas.Select(_generador.GenerarCreacionDeEsquema));
        AgregarSeccion(destino, "Tipos de datos del usuario", tipos.Select(_generador.GenerarCreacionDeTipo));

        // Los tipos tabla pueden usar los tipos alias de arriba
        AgregarSeccion(destino, "Tipos tabla", detalles.TiposDeTabla.Select(_generador.GenerarCreacionDeTipoDeTabla));
        AgregarSeccion(destino, "Secuencias", detalles.Secuencias.Select(_generador.GenerarCreacionDeSecuencia));

        // Un sinónimo se puede crear aunque su objeto todavía no exista
        AgregarSeccion(destino, "Sinónimos", detalles.Sinonimos.Select(_generador.GenerarCreacionDeSinonimo));
    }

    private async Task AgregarTablasAsync(
        TextWriter destino,
        DatosDeAcceso acceso,
        string baseDeDatos,
        IReadOnlyList<Tabla> tablas,
        IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>> columnasPorTabla,
        DetallesDelEsquema detalles,
        CancellationToken tokenDeCancelacion)
    {
        var calculadasPorTabla = detalles.ColumnasCalculadas.ToLookup(calculada => calculada.Tabla);
        var identidadesPorTabla = detalles.Identidades.ToLookup(identidad => identidad.Tabla);

        destino.WriteLine("-- Estructura de las tablas");
        destino.WriteLine();

        foreach (var tabla in tablas)
        {
            var indices = await _explorador.ObtenerIndicesAsync(acceso, baseDeDatos, tabla, tokenDeCancelacion);
            var llavePrimaria = indices.FirstOrDefault(indice => indice.EsLlavePrimaria);

            AgregarLote(destino, _generador.GenerarCreacionDeTablaSinEnvolver(
                tabla, columnasPorTabla[tabla], llavePrimaria, calculadasPorTabla[tabla].ToList(), identidadesPorTabla[tabla].ToList()));
        }
    }

    /// <summary>
    /// Funciones y vistas van juntas y en orden de dependencia: una vista puede usar una función y al revés.
    /// </summary>
    private async Task AgregarFuncionesYVistasAsync(
        TextWriter destino,
        List<string> advertencias,
        DatosDeAcceso acceso,
        string baseDeDatos,
        OpcionesDeExportacion opciones,
        DetallesDelEsquema detalles,
        IReadOnlyList<ObjetoDeEsquema> funcionesYaAgregadas,
        CancellationToken tokenDeCancelacion)
    {
        var objetos = new List<ObjetoDeEsquema>();

        if (opciones.Funciones)
        {
            var funciones = await _explorador.ObtenerFuncionesAsync(acceso, baseDeDatos, tokenDeCancelacion);
            objetos.AddRange(funciones.Except(funcionesYaAgregadas));
        }

        if (opciones.Vistas)
        {
            objetos.AddRange(await _explorador.ObtenerVistasAsync(acceso, baseDeDatos, tokenDeCancelacion));
        }

        var titulo = (opciones.Funciones, opciones.Vistas) switch
        {
            (true, true) => "Funciones y vistas (en el orden en que se necesitan entre sí)",
            (true, false) => "Funciones",
            _ => "Vistas"
        };

        var ordenados = OrdenadorPorDependencias.Ordenar(objetos, detalles.DependenciasEntreModulos);
        await AgregarDefinicionesAsync(destino, advertencias, acceso, baseDeDatos, titulo, ordenados, tokenDeCancelacion);
    }

    /// <summary>
    /// Una tabla con una columna calculada que llama a una función no se puede crear si la función no existe todavía.
    /// Se eligen esas funciones y las que ellas usen a su vez.
    /// </summary>
    private static List<ObjetoDeEsquema> ElegirFuncionesQueNecesitanLasTablas(
        IReadOnlyList<Tabla> tablas,
        IReadOnlyList<Funcion> funciones,
        IReadOnlyList<DependenciaEntreModulos> dependencias)
    {
        var requeridosPorObjeto = dependencias.ToLookup(
            dependencia => $"{dependencia.EsquemaDelDependiente}.{dependencia.NombreDelDependiente}",
            dependencia => $"{dependencia.EsquemaDelRequerido}.{dependencia.NombreDelRequerido}",
            StringComparer.OrdinalIgnoreCase);

        var necesarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var porRevisar = new Queue<string>(tablas.Select(tabla => tabla.NombreCompleto));

        while (porRevisar.TryDequeue(out var nombre))
        {
            foreach (var requerido in requeridosPorObjeto[nombre])
            {
                if (necesarios.Add(requerido))
                {
                    porRevisar.Enqueue(requerido);
                }
            }
        }

        return funciones.Where(funcion => necesarios.Contains(funcion.NombreCompleto)).ToList<ObjetoDeEsquema>();
    }

    private async Task AgregarDefinicionesAsync(
        TextWriter destino,
        List<string> advertencias,
        DatosDeAcceso acceso,
        string baseDeDatos,
        string titulo,
        IReadOnlyList<ObjetoDeEsquema> objetos,
        CancellationToken tokenDeCancelacion)
    {
        if (objetos.Count == 0)
        {
            return;
        }

        destino.WriteLine($"-- {titulo}");
        destino.WriteLine();

        foreach (var objeto in objetos)
        {
            var definicion = await _explorador.ObtenerDefinicionAsync(acceso, baseDeDatos, objeto, tokenDeCancelacion);
            AgregarDefinicion(destino, advertencias, objeto.NombreCompleto, definicion);
        }
    }

    private static void AgregarDefinicion(TextWriter destino, List<string> advertencias, string nombreCompleto, string? definicion)
    {
        if (string.IsNullOrWhiteSpace(definicion))
        {
            var aviso = $"{nombreCompleto}: cifrado o sin permiso para ver su definición; se omitió.";
            advertencias.Add(aviso);
            destino.WriteLine($"-- {aviso}");
            destino.WriteLine();
            return;
        }

        AgregarLote(destino, definicion.Trim());
    }

    private void AgregarDisparadores(
        TextWriter destino,
        List<string> advertencias,
        IReadOnlyList<Tabla> tablas,
        OpcionesDeExportacion opciones,
        IReadOnlyList<DisparadorDeTabla> disparadores)
    {
        // Un disparador sobre una vista solo tiene sentido si la vista también va en el script
        var nombresDeTablas = tablas.Select(tabla => tabla.NombreCompleto).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var incluidos = disparadores
            .Where(disparador => opciones.Vistas || nombresDeTablas.Contains($"{disparador.EsquemaDelObjeto}.{disparador.NombreDelObjeto}"))
            .ToList();

        if (incluidos.Count == 0)
        {
            return;
        }

        destino.WriteLine("-- Disparadores");
        destino.WriteLine();

        foreach (var disparador in incluidos)
        {
            AgregarDefinicion(destino, advertencias, disparador.NombreCompleto, disparador.Definicion);

            if (disparador.EstaDeshabilitado && !string.IsNullOrWhiteSpace(disparador.Definicion))
            {
                AgregarLote(destino, _generador.GenerarDeshabilitacionDeDisparador(disparador));
            }
        }
    }

    private async Task AgregarDatosAsync(
        TextWriter destino,
        List<string> advertencias,
        ServidorConectado servidor,
        string baseDeDatos,
        IReadOnlyList<Tabla> tablas,
        IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>> columnasPorTabla,
        IProgress<string>? avance,
        CancellationToken tokenDeCancelacion)
    {
        destino.WriteLine("-- Datos");
        destino.WriteLine();

        // Sin esto, cada INSERT devuelve su "(1 fila afectada)": con cientos de miles de filas
        // la pestaña de mensajes se llena y la ejecución se vuelve mucho más lenta
        AgregarLote(destino, "SET NOCOUNT ON;");

        for (var posicion = 0; posicion < tablas.Count; posicion++)
        {
            var tabla = tablas[posicion];
            var prefijoDeAvance = $"Datos de {tabla.NombreCompleto} (tabla {posicion + 1} de {tablas.Count})";
            avance?.Report(prefijoDeAvance);

            await AgregarDatosDeLaTablaAsync(
                destino, advertencias, servidor, baseDeDatos, tabla, columnasPorTabla[tabla], prefijoDeAvance, avance, tokenDeCancelacion);
        }

        // Se devuelve la sesión a su estado normal: quien ejecute el script seguirá usando esa pestaña
        AgregarLote(destino, "SET NOCOUNT OFF;");
    }

    /// <summary>
    /// Las filas se leen y se escriben por tramos: nunca está toda la tabla en memoria.
    /// </summary>
    private async Task AgregarDatosDeLaTablaAsync(
        TextWriter destino,
        List<string> advertencias,
        ServidorConectado servidor,
        string baseDeDatos,
        Tabla tabla,
        IReadOnlyList<Columna> columnas,
        string prefijoDeAvance,
        IProgress<string>? avance,
        CancellationToken tokenDeCancelacion)
    {
        var tramo = new List<object?[]>(FilasPorLote);
        var filasEscritas = 0L;

        try
        {
            var filas = _ejecutor.LeerFilasAsync(
                servidor.DatosDeAcceso, baseDeDatos, _generador.GenerarSeleccionCompleta(tabla), tokenDeCancelacion);

            await foreach (var fila in filas)
            {
                if (filasEscritas == 0 && tramo.Count == 0)
                {
                    destino.WriteLine($"-- {tabla.NombreCompleto}");
                }

                tramo.Add(fila);

                if (tramo.Count < FilasPorLote)
                {
                    continue;
                }

                filasEscritas += EscribirTramo(destino, tabla, columnas, tramo);

                if (filasEscritas % FilasEntreAvisosDeAvance == 0)
                {
                    avance?.Report($"{prefijoDeAvance}: {filasEscritas:N0} filas");
                }
            }

            filasEscritas += EscribirTramo(destino, tabla, columnas, tramo);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Antes un SELECT fallido se ignoraba y la tabla quedaba vacía en el script sin que nadie lo notara
            var aviso = $"{tabla.NombreCompleto}: no se pudieron leer todos sus datos; se exportaron {filasEscritas} filas ({error.Message.ReplaceLineEndings(" ")}).";
            advertencias.Add(aviso);
            destino.WriteLine($"-- ERROR: {aviso}");
            destino.WriteLine();
        }
    }

    /// <returns>Cantidad de filas escritas; el tramo queda vacío para volver a llenarlo.</returns>
    private int EscribirTramo(TextWriter destino, Tabla tabla, IReadOnlyList<Columna> columnas, List<object?[]> tramo)
    {
        var cantidad = tramo.Count;

        if (cantidad > 0)
        {
            AgregarLote(destino, _generador.GenerarInsertDeFilas(tabla, columnas, tramo));
            tramo.Clear();
        }

        return cantidad;
    }

    private async Task AgregarLlavesForaneasAsync(
        TextWriter destino,
        DatosDeAcceso acceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var llaves = await _explorador.ObtenerLlavesForaneasAsync(acceso, baseDeDatos, tokenDeCancelacion);
        AgregarSeccion(destino, "Llaves foráneas", llaves.Select(_generador.GenerarLlaveForanea));
    }

    /// <summary>
    /// Cada instrucción va en su propio lote: si una falla (por ejemplo, un CHECK que los datos no cumplen),
    /// las demás se aplican igual y el error señala solo a la que falló.
    /// </summary>
    private static void AgregarSeccion(TextWriter destino, string titulo, IEnumerable<string> instrucciones)
    {
        var lista = instrucciones.ToList();

        if (lista.Count == 0)
        {
            return;
        }

        destino.WriteLine($"-- {titulo}");
        destino.WriteLine();

        foreach (var instruccion in lista)
        {
            AgregarLote(destino, instruccion);
        }
    }

    private static void AgregarLote(TextWriter destino, string instruccion)
    {
        destino.WriteLine(instruccion);
        destino.WriteLine("GO");
        destino.WriteLine();
    }
}
