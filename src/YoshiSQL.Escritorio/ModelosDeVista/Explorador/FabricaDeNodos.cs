using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Explorador;
using YoshiSQL.Aplicacion.Preferencias;
using YoshiSQL.Aplicacion.Scripts;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Escritorio.Servicios;

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
    private readonly IServicioDeErrores _servicioDeErrores;
    private readonly ServicioDePreferencias _servicioDePreferencias;

    public FabricaDeNodos(
        ServicioDelExplorador servicioDelExplorador,
        ServicioDeGeneracionDeScripts generadorDeScripts,
        IAccionesDelExplorador acciones,
        IServicioDeErrores servicioDeErrores,
        ServicioDePreferencias servicioDePreferencias)
    {
        _servicioDelExplorador = servicioDelExplorador;
        _generadorDeScripts = generadorDeScripts;
        _acciones = acciones;
        _servicioDeErrores = servicioDeErrores;
        _servicioDePreferencias = servicioDePreferencias;
    }

    public NodoDelArbolModeloDeVista CrearNodoDeServidor(ServidorConectado servidor)
    {
        var contexto = new ContextoDelNodo(servidor);
        var nodo = new NodoDelArbolModeloDeVista(
            DescribirServidor(servidor),
            TipoDeNodo.Servidor,
            contexto,
            _ => Task.FromResult<IReadOnlyList<NodoDelArbolModeloDeVista>>(
                [CrearCarpetaDeBasesDeDatos(contexto), CrearCarpetaDeSeguridadDelServidor(contexto)]));

        nodo.EstablecerAcciones(
            AccionDeNuevaConsulta(contexto),
            new AccionDelNodo("Monitor de actividad", ComandoSeguro("Abrir monitor de actividad", contexto, () => _acciones.AbrirMonitorDeActividadAsync(contexto))),
            new AccionDelNodo("Ver log de errores del servidor", ComandoSeguro("Ver log del servidor", contexto,
                () => _acciones.AbrirNuevaConsultaAsync(contexto, "EXEC sys.sp_readerrorlog;", ejecutarAlAbrir: true))),
            new AccionDelNodo("Desconectar", ComandoSeguro("Desconectar", contexto, () => _acciones.DesconectarServidorAsync(contexto))),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeBasesDeDatos(ContextoDelNodo contexto)
    {
        var carpeta = new NodoDelArbolModeloDeVista(
            "Bases de datos",
            TipoDeNodo.CarpetaDeBasesDeDatos,
            contexto,
            ProtegerCarga("Cargar bases de datos", contexto, token => CrearNodosDeBasesDeDatosAsync(contexto, token)));

        carpeta.EstablecerAcciones(
            AccionQueAbreScript("Nueva base de datos...", contexto,
                () => _generadorDeScripts.GenerarCreacionDeBaseDeDatos("NuevaBaseDeDatos")),
            AccionDeRestaurar(contexto),
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
            AccionDeNuevaTabla(contexto),
            new AccionDelNodo("Respaldar...", ComandoSeguro("Respaldar base de datos", contexto, () => _acciones.MostrarRespaldoAsync(contexto))),
            AccionDeRestaurar(contexto),
            new AccionDelNodo("Exportar todo a script...", ComandoSeguro("Exportar base de datos", contexto, () => _acciones.ExportarBaseDeDatosAsync(contexto))),
            new AccionDelNodo("Salud de la base de datos", ComandoSeguro("Abrir salud de la base de datos", contexto, () => _acciones.AbrirSaludAsync(contexto))),
            AccionQueAbreScript("Generar script DROP DATABASE", contexto with { BaseDeDatos = null },
                () => _generadorDeScripts.GenerarEliminacionDeBaseDeDatos(baseDeDatos.Nombre)),
            new AccionDelNodo("Propiedades", ComandoSeguro("Ver propiedades de la base de datos", contexto, () => _acciones.MostrarPropiedadesDeBaseDeDatosAsync(contexto))),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private IReadOnlyList<NodoDelArbolModeloDeVista> CrearCarpetasDeBaseDeDatos(ContextoDelNodo contexto)
    {
        var carpetaDeTablas = CrearCarpeta("Tablas", contexto, TipoDeNodo.CarpetaDeTablas, async token =>
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
            AccionDeNuevaTabla(contexto),
            AccionDeActualizar(carpetaDeTablas));

        return
        [
            CrearCarpetaDeDiagramas(contexto),
            carpetaDeTablas,
            carpetaDeVistas,
            carpetaDeProcedimientos,
            carpetaDeFunciones,
            CrearCarpetaDeSeguridadDeLaBaseDeDatos(contexto)
        ];
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeSeguridadDelServidor(ContextoDelNodo contexto)
    {
        var carpetaDeIniciosDeSesion = CrearCarpeta("Inicios de sesión", contexto, async token =>
            (await _servicioDelExplorador.ObtenerIniciosDeSesionAsync(contexto.Servidor, token))
                .Select(inicioDeSesion =>
                {
                    var texto = inicioDeSesion.EstaDeshabilitado ? $"{inicioDeSesion.Nombre} (deshabilitado)" : inicioDeSesion.Nombre;
                    var nodo = new NodoDelArbolModeloDeVista(texto, TipoDeNodo.InicioDeSesion, contexto);
                    nodo.EstablecerAcciones(AccionQueAbreScript("Generar script DROP LOGIN", contexto,
                        () => _generadorDeScripts.GenerarEliminacionDeInicioDeSesion(inicioDeSesion.Nombre)));
                    return nodo;
                }));

        carpetaDeIniciosDeSesion.EstablecerAcciones(
            AccionQueAbreScript("Nuevo inicio de sesión...", contexto, _generadorDeScripts.GenerarCreacionDeInicioDeSesion),
            AccionDeActualizar(carpetaDeIniciosDeSesion));

        return CrearCarpetaConHijosFijos("Seguridad", contexto, [carpetaDeIniciosDeSesion]);
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeSeguridadDeLaBaseDeDatos(ContextoDelNodo contexto)
    {
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;

        var carpetaDeUsuarios = CrearCarpeta("Usuarios", contexto, async token =>
            (await _servicioDelExplorador.ObtenerUsuariosAsync(contexto.Servidor, baseDeDatos, token))
                .Select(usuario =>
                {
                    var nodo = new NodoDelArbolModeloDeVista(usuario.Nombre, TipoDeNodo.Usuario, contexto);
                    nodo.EstablecerAcciones(AccionQueAbreScript("Generar script DROP USER", contexto,
                        () => _generadorDeScripts.GenerarEliminacionDeUsuario(baseDeDatos, usuario.Nombre)));
                    return nodo;
                }));

        carpetaDeUsuarios.EstablecerAcciones(
            AccionQueAbreScript("Nuevo usuario...", contexto, () => _generadorDeScripts.GenerarCreacionDeUsuario(baseDeDatos)),
            AccionDeActualizar(carpetaDeUsuarios));

        var carpetaDeRoles = CrearCarpeta("Roles", contexto, async token =>
            (await _servicioDelExplorador.ObtenerRolesAsync(contexto.Servidor, baseDeDatos, token))
                .Select(rol => new NodoDelArbolModeloDeVista(rol.EsFijo ? $"{rol.Nombre} (predefinido)" : rol.Nombre, TipoDeNodo.Rol, contexto)));

        return CrearCarpetaConHijosFijos("Seguridad", contexto, [carpetaDeUsuarios, carpetaDeRoles]);
    }

    private static NodoDelArbolModeloDeVista CrearCarpetaConHijosFijos(
        string texto,
        ContextoDelNodo contexto,
        IReadOnlyList<NodoDelArbolModeloDeVista> hijos) =>
        new(texto, TipoDeNodo.Carpeta, contexto, _ => Task.FromResult(hijos));

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
                CrearCarpetaDeIndices(contexto, tabla),
                CrearCarpetaDeDisparadores(contexto, tabla)
            ]));

        nodo.EstablecerAcciones(
            AccionDeSeleccionarFilas(contexto, tabla),
            new AccionDelNodo($"Editar las primeras {_servicioDePreferencias.Actuales.FilasAlEditar} filas",
                ComandoSeguro("Editar filas", contexto, () => _acciones.AbrirEdicionDeFilasAsync(contexto, tabla))),
            new AccionDelNodo("Diseñar", ComandoSeguro("Diseñar tabla", contexto, () => _acciones.AbrirDisenadorDeTablaAsync(contexto, tabla))),
            new AccionDelNodo("Generar script CREATE TABLE", ComandoSeguro("Generar script CREATE TABLE", contexto, async () =>
            {
                var script = await _generadorDeScripts.GenerarCreacionDeTablaAsync(contexto.Servidor, baseDeDatos, tabla, CancellationToken.None);
                await _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: false);
            })),
            AccionDeScriptDml("Generar script INSERT", contexto, tabla, TipoDeScriptDml.Insercion),
            AccionDeScriptDml("Generar script UPDATE", contexto, tabla, TipoDeScriptDml.Actualizacion),
            AccionDeScriptDml("Generar script DELETE", contexto, tabla, TipoDeScriptDml.Eliminacion),
            AccionQueAbreScript("Generar script DROP TABLE", contexto,
                () => _generadorDeScripts.GenerarEliminacion(baseDeDatos, tabla)),
            AccionDeVerDependencias(contexto, tabla),
            AccionDeRenombrar(contexto, tabla),
            new AccionDelNodo("Ver fragmentación de índices", ComandoSeguro("Ver fragmentación de índices", contexto,
                () => _acciones.AbrirNuevaConsultaAsync(contexto, _generadorDeScripts.GenerarConsultaDeFragmentacion(baseDeDatos, tabla), ejecutarAlAbrir: true))),
            new AccionDelNodo("Propiedades", ComandoSeguro("Ver propiedades de la tabla", contexto, () => _acciones.MostrarPropiedadesDeTablaAsync(contexto, tabla))),
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
            AccionDeScriptDml("Generar script SELECT", contexto, vista, TipoDeScriptDml.Seleccion),
            AccionDeModificar(contexto, vista),
            AccionDeGenerarCreacion(contexto, vista),
            AccionQueAbreScript("Generar script DROP VIEW", contexto,
                () => _generadorDeScripts.GenerarEliminacion(contexto.BaseDeDatosOPredeterminada, vista)),
            AccionDeVerDependencias(contexto, vista),
            AccionDeRenombrar(contexto, vista),
            AccionDeActualizar(nodo));

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearNodoDeProgramacion(ContextoDelNodo contexto, ObjetoDeEsquema objeto, TipoDeNodo tipo)
    {
        var nodo = new NodoDelArbolModeloDeVista(objeto.NombreCompleto, tipo, contexto);

        var accionDeModificar = AccionDeModificar(contexto, objeto);

        nodo.EstablecerAcciones(
            accionDeModificar,
            AccionDeGenerarCreacion(contexto, objeto),
            AccionQueAbreScript("Generar script DROP", contexto,
                () => _generadorDeScripts.GenerarEliminacion(contexto.BaseDeDatosOPredeterminada, objeto)),
            AccionDeVerDependencias(contexto, objeto),
            AccionDeRenombrar(contexto, objeto));
        nodo.EstablecerComandoAlHacerDobleClic(accionDeModificar.Comando);

        return nodo;
    }

    private NodoDelArbolModeloDeVista CrearCarpetaDeColumnas(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        CrearCarpeta("Columnas", contexto, async token =>
            (await _servicioDelExplorador.ObtenerColumnasAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, token))
                .Select(columna => new NodoDelArbolModeloDeVista(
                    columna.DescribirParaExplorador(),
                    columna.EsLlavePrimaria ? TipoDeNodo.LlavePrimaria : TipoDeNodo.Columna,
                    contexto)));

    private NodoDelArbolModeloDeVista CrearCarpetaDeDisparadores(ContextoDelNodo contexto, Tabla tabla) =>
        CrearCarpeta("Disparadores", contexto, async token =>
            (await _servicioDelExplorador.ObtenerDisparadoresAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, token))
                .Select(disparador => new NodoDelArbolModeloDeVista(
                    disparador.EstaDeshabilitado ? $"{disparador.Nombre} (deshabilitado)" : disparador.Nombre,
                    TipoDeNodo.Disparador,
                    contexto)));

    private NodoDelArbolModeloDeVista CrearCarpetaDeIndices(ContextoDelNodo contexto, Tabla tabla) =>
        CrearCarpeta("Índices", contexto, async token =>
            (await _servicioDelExplorador.ObtenerIndicesAsync(contexto.Servidor, contexto.BaseDeDatosOPredeterminada, tabla, token))
                .Select(indice => CrearNodoDeIndice(contexto, tabla, indice)));

    private NodoDelArbolModeloDeVista CrearNodoDeIndice(ContextoDelNodo contexto, Tabla tabla, Indice indice)
    {
        var nodo = new NodoDelArbolModeloDeVista(DescribirIndice(indice), TipoDeNodo.Indice, contexto);
        var baseDeDatos = contexto.BaseDeDatosOPredeterminada;

        nodo.EstablecerAcciones(
            AccionQueAbreScript("Reconstruir (REBUILD)", contexto,
                () => _generadorDeScripts.GenerarMantenimientoDeIndice(baseDeDatos, tabla, indice.Nombre, reconstruir: true)),
            AccionQueAbreScript("Reorganizar (REORGANIZE)", contexto,
                () => _generadorDeScripts.GenerarMantenimientoDeIndice(baseDeDatos, tabla, indice.Nombre, reconstruir: false)));

        return nodo;
    }

    /// <summary>
    /// Carpeta cuyos hijos se piden al servidor al desplegarla, con la opción "Actualizar".
    /// </summary>
    private NodoDelArbolModeloDeVista CrearCarpeta(
        string texto,
        ContextoDelNodo contexto,
        Func<CancellationToken, Task<IEnumerable<NodoDelArbolModeloDeVista>>> obtenerHijos) =>
        CrearCarpeta(texto, contexto, TipoDeNodo.Carpeta, obtenerHijos);

    private NodoDelArbolModeloDeVista CrearCarpeta(
        string texto,
        ContextoDelNodo contexto,
        TipoDeNodo tipo,
        Func<CancellationToken, Task<IEnumerable<NodoDelArbolModeloDeVista>>> obtenerHijos)
    {
        var carpeta = new NodoDelArbolModeloDeVista(
            texto,
            tipo,
            contexto,
            ProtegerCarga($"Cargar {texto.ToLowerInvariant()}", contexto, async token => (await obtenerHijos(token)).ToList()));

        carpeta.EstablecerAcciones(AccionDeActualizar(carpeta));
        return carpeta;
    }

    private AccionDelNodo AccionDeNuevaConsulta(ContextoDelNodo contexto) =>
        AccionQueAbreScript("Nueva consulta", contexto, () => string.Empty);

    private AccionDelNodo AccionDeModificar(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new("Modificar", ComandoSeguro("Modificar objeto", contexto, async () =>
        {
            var script = await _generadorDeScripts.GenerarModificacionAsync(
                contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, CancellationToken.None);
            await _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: false);
        }));

    private AccionDelNodo AccionDeGenerarCreacion(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new("Generar script CREATE", ComandoSeguro("Generar script CREATE", contexto, async () =>
        {
            var script = await _generadorDeScripts.GenerarCreacionDeObjetoAsync(
                contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, CancellationToken.None);
            await _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: false);
        }));

    private AccionDelNodo AccionDeRestaurar(ContextoDelNodo contexto) =>
        new("Restaurar base de datos...", ComandoSeguro("Restaurar base de datos", contexto, () => _acciones.MostrarRestauracionAsync(contexto)));

    private AccionDelNodo AccionDeNuevaTabla(ContextoDelNodo contexto) =>
        new("Nueva tabla...", ComandoSeguro("Nueva tabla", contexto, () => _acciones.AbrirDisenadorDeTablaAsync(contexto, tabla: null)));

    private AccionDelNodo AccionDeVerDiagrama(ContextoDelNodo contexto) =>
        new("Ver diagrama", ComandoSeguro("Abrir diagrama", contexto, () => _acciones.AbrirDiagramaAsync(contexto)));

    private AccionDelNodo AccionDeSeleccionarFilas(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new($"Seleccionar las primeras {_generadorDeScripts.FilasPorDefectoAlSeleccionar} filas", ComandoSeguro("Seleccionar filas", contexto, () =>
        {
            var script = _generadorDeScripts.GenerarSeleccionDeFilas(contexto.BaseDeDatosOPredeterminada, objeto);
            return _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: true);
        }));

    private AccionDelNodo AccionDeVerDependencias(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new("Ver dependencias", ComandoSeguro("Ver dependencias", contexto, () => _acciones.MostrarDependenciasAsync(contexto, objeto)));

    private AccionDelNodo AccionDeRenombrar(ContextoDelNodo contexto, ObjetoDeEsquema objeto) =>
        new("Renombrar...", ComandoSeguro("Renombrar", contexto, () => _acciones.RenombrarObjetoAsync(contexto, objeto)));

    private AccionDelNodo AccionDeScriptDml(string texto, ContextoDelNodo contexto, ObjetoDeEsquema objeto, TipoDeScriptDml tipo) =>
        new(texto, ComandoSeguro(texto, contexto, async () =>
        {
            var script = await _generadorDeScripts.GenerarInstruccionDmlAsync(
                contexto.Servidor, contexto.BaseDeDatosOPredeterminada, objeto, tipo, CancellationToken.None);
            await _acciones.AbrirNuevaConsultaAsync(contexto, script, ejecutarAlAbrir: false);
        }));

    private AccionDelNodo AccionQueAbreScript(string texto, ContextoDelNodo contexto, Func<string> generarScript) =>
        new(texto, ComandoSeguro(texto, contexto, () => _acciones.AbrirNuevaConsultaAsync(contexto, generarScript(), ejecutarAlAbrir: false)));

    private AccionDelNodo AccionDeActualizar(NodoDelArbolModeloDeVista nodo) =>
        new("Actualizar", ComandoSeguro("Actualizar el explorador", nodo.Contexto, nodo.RecargarAsync));

    /// <summary>
    /// Comando de menú que registra cualquier error y muestra un aviso en lugar de cerrar la aplicación.
    /// </summary>
    private AsyncRelayCommand ComandoSeguro(string accion, ContextoDelNodo? contexto, Func<Task> ejecutar) =>
        new(async () =>
        {
            try
            {
                await ejecutar();
            }
            catch (Exception error)
            {
                await _servicioDeErrores.RegistrarYMostrarAsync(error, CrearContextoDeError(accion, contexto));
            }
        });

    /// <summary>
    /// Envuelve la carga de los hijos de un nodo: cualquier error queda registrado y el árbol
    /// muestra un mensaje claro en lugar del detalle técnico.
    /// </summary>
    private Func<CancellationToken, Task<IReadOnlyList<NodoDelArbolModeloDeVista>>> ProtegerCarga(
        string accion,
        ContextoDelNodo contexto,
        Func<CancellationToken, Task<IReadOnlyList<NodoDelArbolModeloDeVista>>> cargar) =>
        async token =>
        {
            try
            {
                return await cargar(token);
            }
            catch (Exception error)
            {
                throw new ErrorDeYoshiSql(_servicioDeErrores.RegistrarYDescribir(error, CrearContextoDeError(accion, contexto)), error);
            }
        };

    private static ContextoDeError CrearContextoDeError(string accion, ContextoDelNodo? contexto) =>
        new(accion, contexto?.Servidor.Perfil.NombreVisible, contexto?.BaseDeDatos);

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
