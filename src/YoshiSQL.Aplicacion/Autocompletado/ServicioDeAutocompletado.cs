using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Autocompletado;

/// <summary>
/// Decide qué sugerir según lo que se está escribiendo: tablas después de FROM, columnas después
/// de un alias con punto, y en general columnas, funciones, tablas y palabras clave.
/// </summary>
public sealed partial class ServicioDeAutocompletado
{
    private static readonly TimeSpan VigenciaDelCatalogo = TimeSpan.FromMinutes(5);

    private readonly IAnalizadorDeContextoSql _analizador;
    private readonly IExploradorDeEsquema _explorador;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ServicioDeAutocompletado> _registro;
    private readonly ConcurrentDictionary<(Guid IdDelPerfil, string BaseDeDatos), CatalogoEnCache> _catalogos = new();

    public ServicioDeAutocompletado(
        IProveedorDeBaseDeDatos proveedor,
        TimeProvider reloj,
        ILogger<ServicioDeAutocompletado> registro)
    {
        _analizador = proveedor.AnalizadorDeContexto;
        _explorador = proveedor.Explorador;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<IReadOnlyList<Sugerencia>> ObtenerSugerenciasAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        string textoCompleto,
        int posicionDelCursor,
        CancellationToken tokenDeCancelacion)
    {
        var contexto = _analizador.Analizar(textoCompleto, posicionDelCursor);

        if (contexto.Tipo == TipoDeContexto.SinSugerencias)
        {
            return [];
        }

        var catalogo = await ObtenerCatalogoAsync(servidor, baseDeDatos, tokenDeCancelacion);

        IEnumerable<Sugerencia> sugerencias = contexto.Tipo switch
        {
            TipoDeContexto.NombreDeTabla => SugerirEsquemas(catalogo).Concat(SugerirObjetos(catalogo)),
            TipoDeContexto.DespuesDePunto => SugerirDespuesDePunto(contexto, catalogo),
            _ => SugerirColumnasDeLaInstruccion(contexto, catalogo)
                .Concat(SugerirObjetos(catalogo))
                .Concat(_analizador.Funciones.Select(funcion => new Sugerencia(funcion, TipoDeSugerencia.Funcion, "Función")))
                .Concat(_analizador.PalabrasClave.Select(palabra => new Sugerencia(palabra, TipoDeSugerencia.PalabraClave)))
        };

        return sugerencias.DistinctBy(sugerencia => (sugerencia.Texto, sugerencia.Tipo)).ToList();
    }

    /// <summary>
    /// Olvida el catálogo guardado para que la próxima sugerencia lea las tablas de nuevo
    /// (por ejemplo, después de crear o eliminar una tabla).
    /// </summary>
    public void InvalidarCatalogo(ServidorConectado servidor, string baseDeDatos) =>
        _catalogos.TryRemove((servidor.Perfil.Id, baseDeDatos), out _);

    private static IEnumerable<Sugerencia> SugerirDespuesDePunto(ContextoDeAutocompletado contexto, CatalogoDeLaBaseDeDatos catalogo)
    {
        var calificador = contexto.Calificador ?? string.Empty;
        var referencia = contexto.TablasDeLaInstruccion.FirstOrDefault(tabla => tabla.CoincideCon(calificador));
        var objeto = referencia is null
            ? catalogo.Buscar(esquema: null, calificador)
            : catalogo.Buscar(referencia.Esquema, referencia.Nombre);

        if (objeto is not null)
        {
            return SugerirColumnas(objeto, catalogo, mostrarTabla: false);
        }

        // "dbo." -> tablas y vistas de ese esquema, sin repetir el esquema al insertar
        return catalogo.Objetos
            .Where(objetoDelEsquema => string.Equals(objetoDelEsquema.Esquema, calificador, StringComparison.OrdinalIgnoreCase))
            .Select(objetoDelEsquema => CrearSugerenciaDeObjeto(objetoDelEsquema, Delimitar(objetoDelEsquema.Nombre)));
    }

    private static IEnumerable<Sugerencia> SugerirColumnasDeLaInstruccion(ContextoDeAutocompletado contexto, CatalogoDeLaBaseDeDatos catalogo) =>
        contexto.TablasDeLaInstruccion
            .Select(referencia => catalogo.Buscar(referencia.Esquema, referencia.Nombre))
            .OfType<ObjetoDeEsquema>()
            .SelectMany(objeto => SugerirColumnas(objeto, catalogo, mostrarTabla: true));

    private static IEnumerable<Sugerencia> SugerirColumnas(ObjetoDeEsquema objeto, CatalogoDeLaBaseDeDatos catalogo, bool mostrarTabla) =>
        catalogo.ObtenerColumnas(objeto).Select(columna => new Sugerencia(
            Delimitar(columna.Nombre),
            TipoDeSugerencia.Columna,
            mostrarTabla ? $"{columna.TipoDeDato.Describir()} · {objeto.NombreCompleto}" : columna.TipoDeDato.Describir()));

    private static IEnumerable<Sugerencia> SugerirObjetos(CatalogoDeLaBaseDeDatos catalogo) =>
        catalogo.Objetos
            .OrderBy(objeto => objeto.NombreCompleto, StringComparer.OrdinalIgnoreCase)
            .Select(objeto => CrearSugerenciaDeObjeto(objeto, TextoParaInsertar(objeto)));

    private static IEnumerable<Sugerencia> SugerirEsquemas(CatalogoDeLaBaseDeDatos catalogo) =>
        catalogo.Esquemas
            .Where(esquema => !string.Equals(esquema, CatalogoDeLaBaseDeDatos.EsquemaPredeterminado, StringComparison.OrdinalIgnoreCase))
            .Select(esquema => new Sugerencia(Delimitar(esquema), TipoDeSugerencia.Esquema, "Esquema"));

    private static Sugerencia CrearSugerenciaDeObjeto(ObjetoDeEsquema objeto, string textoParaInsertar) =>
        objeto.Tipo == TipoDeObjeto.Vista
            ? new Sugerencia(textoParaInsertar, TipoDeSugerencia.Vista, $"Vista · {objeto.Esquema}")
            : new Sugerencia(textoParaInsertar, TipoDeSugerencia.Tabla, $"Tabla · {objeto.Esquema}");

    /// <summary>
    /// Las tablas de dbo se insertan sin esquema, como suele escribirse; las demás con su esquema.
    /// </summary>
    private static string TextoParaInsertar(ObjetoDeEsquema objeto) =>
        string.Equals(objeto.Esquema, CatalogoDeLaBaseDeDatos.EsquemaPredeterminado, StringComparison.OrdinalIgnoreCase)
            ? Delimitar(objeto.Nombre)
            : $"{Delimitar(objeto.Esquema)}.{Delimitar(objeto.Nombre)}";

    /// <summary>
    /// Agrega corchetes solo cuando el nombre los necesita (espacios, guiones, empieza con número...).
    /// </summary>
    private static string Delimitar(string nombre) =>
        ExpresionDeNombreSimple().IsMatch(nombre) ? nombre : $"[{nombre.Replace("]", "]]", StringComparison.Ordinal)}]";

    private async Task<CatalogoDeLaBaseDeDatos> ObtenerCatalogoAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var clave = (servidor.Perfil.Id, baseDeDatos);
        var ahora = _reloj.GetUtcNow();

        if (!_catalogos.TryGetValue(clave, out var enCache) || ahora - enCache.Momento > VigenciaDelCatalogo)
        {
            enCache = new CatalogoEnCache(ahora, CargarCatalogoAsync(servidor, baseDeDatos));
            _catalogos[clave] = enCache;
        }

        try
        {
            return await enCache.Carga.WaitAsync(tokenDeCancelacion);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Sin catálogo igual se sugieren palabras clave y funciones; se reintenta en la próxima sugerencia
            _registro.LogWarning(error, "No se pudo leer el catálogo de {BaseDeDatos} para el autocompletado", baseDeDatos);
            _catalogos.TryRemove(clave, out _);
            return CatalogoDeLaBaseDeDatos.Vacio;
        }
    }

    private async Task<CatalogoDeLaBaseDeDatos> CargarCatalogoAsync(ServidorConectado servidor, string baseDeDatos)
    {
        var columnasDeTablas = await _explorador.ObtenerColumnasDeTodasLasTablasAsync(servidor.DatosDeAcceso, baseDeDatos, CancellationToken.None);
        var columnasDeVistas = await _explorador.ObtenerColumnasDeTodasLasVistasAsync(servidor.DatosDeAcceso, baseDeDatos, CancellationToken.None);

        var columnasPorObjeto = columnasDeTablas
            .Select(par => KeyValuePair.Create((ObjetoDeEsquema)par.Key, par.Value))
            .Concat(columnasDeVistas.Select(par => KeyValuePair.Create((ObjetoDeEsquema)par.Key, par.Value)))
            .ToDictionary();

        return new CatalogoDeLaBaseDeDatos(columnasPorObjeto);
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex ExpresionDeNombreSimple();

    private sealed record CatalogoEnCache(DateTimeOffset Momento, Task<CatalogoDeLaBaseDeDatos> Carga);
}
