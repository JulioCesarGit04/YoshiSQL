using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Escritorio.ModelosDeVista.Explorador;

/// <summary>
/// Construye cada tipo de nodo del árbol con sus hijos y su menú contextual.
/// Para agregar un nuevo tipo de objeto al explorador, se agrega un método aquí.
/// </summary>
public sealed class FabricaDeNodos
{
    private readonly ServicioDelExplorador _servicioDelExplorador;
    private readonly ServicioDeGeneracionDeScripts _generadorDeScripts;
    private readonly IAccionesDelExplorador _acciones;

    public FabricaDeNodos(
        ServicioDelExplorador servicioDelExplorador,
        ServicioDeGeneracionDeScripts generadorDeScripts,
        IAccionesDelExplorador acciones)
    {
        _servicioDelExplorador = servicioDelExplorador;
        _generadorDeScripts = generadorDeScripts;
        _acciones = acciones;
    }

    public NodoDelArbolModeloDeVista CrearNodoDeServidor(ServidorConectado servidor)
    {
        var contexto = new ContextoDelNodo(servidor);
        var nodo = new NodoDelArbolModeloDeVista(
            DescribirServidor(servidor),
            TipoDeNodo.Servidor,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>([CrearCarpetaDeBasesDeDatos(contexto)]));

        nodo.EstablecerAcciones(
            AccionDeNuevaConsulta(contexto),
            new AccionDelNodo("Desconectar", new RelayCommand(() => _acciones.DesconectarServidor(contexto))),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeBasesDeDatos(ContextoDelNodo contexto)
    {
        var carpeta = new NodoDelArbolModeloDeVista(
            "Bases de datos",
            TipoDeNodo.CarpetaDeBasesDeDatos,
            contexto,
            token => CrearNodosDeBasesDeDatosAsync(contexto, token));

        carpeta.EstablecerAcciones(
            AccionQueAbreScript("Nueva base de datos...", contexto,
                () => _generadorDeScripts.GenerarCreacionDeBaseDeDatos("NuevaBaseDeDatos")),
            AccionDeActualizar(carpeta));

        return carpeta;
    }

    private async Task<IReadOnlyList<NodoDelArbolModeloDeVista>> CrearNodosDeBasesDeDatosAsync(
        ContextoDelNodo contexto,
        CancellationToken tokenDeCancelacion)
    {
        var basesDeDatos = await _servicioDelExplorador.ObtenerBasesDeDatosAsync(contexto.Servidor, tokenDeCancelacion);

        var nodosDelSistema = basesDeDatos
            .Where(baseDeDatos => baseDeDatos.EsDelSistema)
            .Select(baseDeDatos => CrearNodoDeBaseDeDatos(contexto, baseDeDatos))
            .ToList();

        var carpetaDelSistema = new NodoDelArbolModeloDeVista(
            "Bases de datos del sistema",
            TipoDeNodo.Carpeta,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>(nodosDelSistema));

        var nodosDeUsuario = basesDeDatos
            .Where(baseDeDatos => !baseDeDatos.EsDelSistema)
            .Select(baseDeDatos => CrearNodoDeBaseDeDatos(contexto, baseDeDatos));

        return [carpetaDelSistema, .. nodosDeUsuario];
    }

    private NodoDelArbolModeloDeVista CrearNodoDeBaseDeDatos(ContextoDelNodo contextoDelServidor, BaseDeDatos baseDeDatos)
    {
        var contexto = contextoDelServidor.EnBaseDeDatos(baseDeDatos.Nombre);

        if (!baseDeDatos.EstaEnLinea)
        {
            return new NodoDelArbolModeloDeVista($"{baseDeDatos.Nombre} (sin conexión)", TipoDeNodo.BaseDeDatosSinConexion, contexto);
        }

        var nodo = new NodoDelArbolModeloDeVista(
            baseDeDatos.Nombre,
            TipoDeNodo.BaseDeDatos,
            contexto,
            _ => Task.FromResult(CrearCarpetasDeBaseDeDatos(contexto)));

        nodo.EstablecerAcciones(
            AccionDeNuevaConsulta(contexto),
            AccionDeVerDiagrama(contexto),
            AccionQueAbreScript("Nueva tabla...", contexto,
                () => _generadorDeScripts.GenerarPlantillaDeTablaNueva(baseDeDatos.Nombre)),
            AccionQueAbreScript("Generar script DROP DATABASE", contexto with { BaseDeDatos = null },
                () => _generadorDeScripts.GenerarEliminacionDeBaseDeDatos(baseDeDatos.Nombre)),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private IReadOnlyList<NodoDelArbolModeloDeVista> CrearCarpetasDeBaseDeDatos(ContextoDelNodo contexto)
    {
        var carpetaDeTablas = CrearCarpeta("Tablas", contexto, async token =>
            (await _servicioDelExplorador.ObtenerTablasAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, token))
                .Select(tabla => CrearNodoDeTabla(contexto, tabla)));

        var carpetaDeVistas = CrearCarpeta("Vistas", contexto, async token =>
            (await _servicioDelExplorador.ObtenerVistasAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, token))
                .Select(vista => CrearNodoDeVista(contexto, vista)));

        var carpetaDeProcedimientos = CrearCarpeta("Procedimientos almacenados", contexto, async token =>
            (await _servicioDelExplorador.ObtenerProcedimientosAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, token))
                .Select(procedimiento => CrearNodoDeProgramacion(contexto, procedimiento, TipoDeNodo.ProcedimientoAlmacenado)));

        var carpetaDeFunciones = CrearCarpeta("Funciones", contexto, async token =>
            (await _servicioDelExplorador.ObtenerFuncionesAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, token))
                .Select(funcion => CrearNodoDeProgramacion(contexto, funcion, TipoDeNodo.Funcion)));

        carpetaDeTablas.EstablecerAcciones(
            AccionQueAbreScript("Nueva tabla...", contexto,
                () => _generadorDeScripts.GenerarPlantillaDeTablaNueva(contexto.BaseDeDatosOPredeterminada)),
            AccionDeActualizar(carpetaDeTablas));

        return [CrearCarpetaDeDiagramas(contexto), carpetaDeTablas, carpetaDeVistas, carpetaDeProcedimientos, carpetaDeFunciones];
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeDiagramas(ContextoDelNodo contexto)
    {
        var accionDeVerDiagrama = AccionDeVerDiagrama(contexto);
        var nodoDelDiagrama = new NodoDelArbolModeloDeVista(
            $"Diagrama de {contexto.BaseDeDatosOPredeterminada}",
            TipoDeNodo.Diagrama,
            contexto);

        nodoDelDiagrama.EstablecerAcciones(accionDeVerDiagrama);
        nodoDelDiagrama.EstablecerComandoAlHacerDobleClic(accionDeVerDiagrama.Comando);

        return new NodoDelArbolModeloDeVista(
            "Diagramas de base de datos",
            TipoDeNodo.Carpeta,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>([nodoDelDiagrama]));
    }

    private NodoDelArbolModeloDeVista CrearNodoDeTabla(ContextoDelNodo contexto, Tabla tabla)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;
        var nodo = new NodoDelArbolModeloDeVista(
            tabla.NombreCompleto,
            TipoDeNodo.Tabla,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>(
            [
                CrearCarpetaDeColumnas(contexto, tabla),
                CrearCarpetaDeIndices(contexto, tabla)
            ]));

        nodo.EstablecerAcciones(
            AccionDeSeleccionarFilas(contexto, tabla),
            new AccionDelNodo("Generar script CREATE TABLE", ComandoSeguro(async () =>
            {
                var script = await _generadorDeScripts.GenerarCreacionDeTablaAsync(contexto.Servidor, baseDeDatos, tabla, CancellationToken.None);
                await _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: false);
            })),
            AccionQueAbreScript("Generar script DROP TABLE", contexto,
                () => _generadorDeScripts.GenerarEliminacion(baseDeDatos, tabla)),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearNodoDeVista(ContextoDelNodo contexto, Vista vista)
    {
        var nodo = new NodoDelArbolModeloDeVista(
            vista.NombreCompleto,
            TipoDeNodo.Vista,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>([CrearCarpetaDeColumnas(contexto, vista)]));

        nodo.EstablecerAcciones(
            AccionDeSeleccionarFilas(contexto, vista),
            AccionQueAbreScript("Generar script DROP VIEW", contexto,
                () => _generadorDeScripts.GenerarEliminacion(contexto.BaseDeDatosOPredeterminada, vista)),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearNodoDeProgramacion(ContextoDelNodo contexto, ObjetoDeEsquema objeto, TipoDeNodo tipo)
    {
        var nodo = new NodoDelArbolModeloDeVista(objeto.NombreCompleto, tipo, contexto);

        nodo.EstablecerAcciones(
            AccionQueAbreScript("Generar script DROP", contexto,
                () => _generadorDeScripts.GenerarEliminacion(contexto.BaseDeDatosOPredeterminada, objeto)));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeColumnas(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        CrearCarpeta("Columnas", contexto, async token =>
            (await _servicioDelExplorador.ObtenerColumnasAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, token))
                .Select(columna => new NodoDelArbolModeloDeVista(
                    columna.DescribirParaExplorador(),
                    columna.EsLlavePrimaria ? TipoDeNodo.LlavePrimaria : TipoDeNodo.Columna,
                    contexto)));

    private NodoDelArbolModeloDeVista CrearCarpetaDeIndices(ContextoDelNodo contexto, Tabla tabla) =>
        CrearCarpeta("Índices", contexto, async token =>
            (await _servicioDelExplorador.ObtenerIndicesAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, token))
                .Select(indice => new NodoDelArbolModeloDeVista(DescribirIndice(indice), TipoDeNodo.Indice, contexto)));

    /// <summary>
    /// Carpeta cuyos hijos se piden al servidor al desplegarla, con la opción "Actualizar".
    /// </summary>
    private NodoDelArbolModeloDeVista CrearCarpeta(
        string texto,
        ContextoDelNodo contexto,
        Func<CancellationToken, Task<IEnumerable<NodoDelArbolModeloDeVista>>> obtenerHijos)
    {
        var carpeta = new NodoDelArbolModeloDeVista(
            texto,
            TipoDeNodo.Carpeta,
            contexto,
            async token => (await obtenerHijos(token)).ToList());

        carpeta.EstablecerAcciones(AccionDeActualizar(carpeta));
        return carpeta;
    }

    private AccionDelNodo AccionDeNuevaConsulta(ContextoDelNodo contexto) =>
        AccionQueAbreScript("Nueva consulta", contexto, () => string.Empty);

    private AccionDelNodo AccionDeVerDiagrama(ContextoDelNodo contexto) =>
        new("Ver diagrama", ComandoSeguro(() => _acciones.AbrirDiagramaAsync(contexto)));

    private AccionDelNodo AccionDeSeleccionarFilas(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new($"Seleccionar las primeras {ServicioDeGeneracionDeScripts.FilasPorDefectoAlSeleccionar} filas", ComandoSeguro(() =>
        {
            var script = _generadorDeScripts.GenerarSeleccionDeFilas(contexto.BaseDeDatosOPredeterminada, objeto);
            return _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: true);
        }));

    private AccionDelNodo AccionQueAbreScript(string texto, ContextoDelNodo contexto, Func<string> generarScript) =>
        new(texto, ComandoSeguro(() => _acciones.AbrirNuevaConsultaAsync(contexto, generarScript(), ejecutarAlAbrir: false)));

    private AccionDelNodo AccionDeActualizar(NodoDelArbolModeloDeVista nodo) =>
        new("Actualizar", ComandoSeguro(nodo.RecargarAsync));

    /// <summary>
    /// Comando que muestra los errores al usuario en lugar de cerrar la aplicación.
    /// </summary>
    private AsyncRelayCommand ComandoSeguro(Func<Task> accion) =>
        new(async () =>
        {
            try
            {
                await accion();
            }
            catch (Exception error)
            {
                await _acciones.MostrarErrorAsync(error.Message);
            }
        });

    private static string DescribirServidor(ServidorConectado servidor)
    {
        var usuario = string.IsNullOrEmpty(servidor.Perfil.Usuario) ? string.Empty : $" - {servidor.Perfil.Usuario}";
        return $"{servidor.Perfil.NombreVisible} (SQL Server {servidor.Servidor.Version}{usuario})";
    }

    private static string DescribirIndice(Indice indice)
    {
        var caracteristicas = new List<string>
        {
            indice.EsAgrupado ? "Agrupado" : "No agrupado"
        };

        if (indice.EsLlavePrimaria)
        {
            caracteristicas.Insert(0, "PK");
        }
        else if (indice.EsUnico)
        {
            caracteristicas.Add("Único");
        }

        return $"{indice.Nombre} ({string.Join(", ", caracteristicas)})";
    }
}
