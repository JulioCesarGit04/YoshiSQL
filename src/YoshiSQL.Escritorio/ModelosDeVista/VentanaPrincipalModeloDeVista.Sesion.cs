using System.Text.RegularExpressions;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Aplicacion.Sesion;
using YoshiSQL.Dominio.Sesion;
using YoshiSQL.Escritorio.ModelosDeVista.Editor;
using YoshiSQL.Escritorio.ModelosDeVista.Explorador;

namespace YoshiSQL.Escritorio.ModelosDeVista;

/// <summary>
/// Restauración de las pestañas de la última vez y copia de seguridad periódica
/// para no perder scripts si YoshiSQL se cierra de forma inesperada.
/// </summary>
public sealed partial class VentanaPrincipalModeloDeVista
{
    private static readonly TimeSpan IntervaloDeAutoguardado = TimeSpan.FromSeconds(10);

    private readonly CancellationTokenSource _detencionDelAutoguardado = new();
    private bool _elUltimoAutoguardadoFallo;

    /// <summary>
    /// Se ejecuta una vez, cuando la ventana principal termina de abrirse.
    /// </summary>
    public async Task IniciarAsync()
    {
        await CargarHistorialAsync();

        try
        {
            var sesionAnterior = await _servicioDeSesion.IniciarAsync(CancellationToken.None);
            await RestaurarSesionAsync(sesionAnterior);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Restaurar la sesión anterior"));
        }

        _ = MantenerAutoguardadoAsync(_detencionDelAutoguardado.Token);
    }

    private async Task CargarHistorialAsync()
    {
        try
        {
            await _historialDeConsultas.CargarAsync(CancellationToken.None);
        }
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Cargar el historial de consultas"));
        }
    }

    private async Task RestaurarSesionAsync(SesionAnterior sesionAnterior)
    {
        var pestanasARestaurar = await DecidirQuePestanasRestaurarAsync(sesionAnterior);

        if (pestanasARestaurar.Count == 0)
        {
            return;
        }

        var pestanaSeleccionada = sesionAnterior.Estado.Pestanas.ElementAtOrDefault(sesionAnterior.Estado.IndiceDeLaPestanaSeleccionada);
        var pestanasNoRestauradas = new List<PestanaGuardada>();
        DocumentoModeloDeVista? documentoASeleccionar = null;

        foreach (var pestanasDelServidor in pestanasARestaurar.GroupBy(pestana => pestana.IdDelPerfil))
        {
            var servidor = await ReconectarParaRestaurarAsync(pestanasDelServidor.Key);

            if (servidor is null)
            {
                pestanasNoRestauradas.AddRange(pestanasDelServidor);
                continue;
            }

            foreach (var pestana in pestanasDelServidor)
            {
                var documento = await RestaurarPestanaAsync(servidor, pestana);

                if (pestana == pestanaSeleccionada)
                {
                    documentoASeleccionar = documento;
                }
            }
        }

        if (documentoASeleccionar is not null)
        {
            DocumentoSeleccionado = documentoASeleccionar;
        }

        await ResguardarPestanasNoRestauradasAsync(pestanasNoRestauradas);
    }

    /// <summary>
    /// Si YoshiSQL se cerró de forma inesperada y había scripts sin guardar, se pregunta si recuperarlos.
    /// </summary>
    private async Task<IReadOnlyList<PestanaGuardada>> DecidirQuePestanasRestaurarAsync(SesionAnterior sesionAnterior)
    {
        var pestanas = sesionAnterior.Estado.Pestanas;

        if (!sesionAnterior.SeCerroIncorrectamente || sesionAnterior.CantidadDeScriptsSinGuardar == 0)
        {
            return pestanas;
        }

        var deseaRecuperar = await _servicioDeDialogos.ConfirmarAsync(
            "Recuperar scripts",
            $"YoshiSQL no se cerró correctamente la última vez.\n¿Deseas recuperar {sesionAnterior.CantidadDeScriptsSinGuardar} script(s) con cambios sin guardar?",
            "Recuperar",
            "Descartar");

        if (deseaRecuperar)
        {
            return pestanas;
        }

        // Se descartan los cambios: los archivos guardados se vuelven a abrir tal como están en disco
        return pestanas
            .Where(pestana => !pestana.TieneCambiosSinGuardar || pestana.RutaDelArchivo is not null)
            .Select(pestana => pestana.TieneCambiosSinGuardar
                ? pestana with { Texto = null, TieneCambiosSinGuardar = false }
                : pestana)
            .ToList();
    }

    /// <summary>
    /// Reconecta con la contraseña guardada; si no es posible, abre la ventana de conexión con ese servidor.
    /// </summary>
    private async Task<ServidorConectado?> ReconectarParaRestaurarAsync(Guid idDelPerfil)
    {
        var perfil = await _servicioDeSesion.BuscarPerfilAsync(idDelPerfil, CancellationToken.None);

        if (perfil is null)
        {
            return null;
        }

        ServidorConectado? servidor = null;

        try
        {
            servidor = await _servicioDeSesion.ReconectarSinPreguntarAsync(perfil, CancellationToken.None);
        }
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Reconectar al restaurar la sesión", perfil.NombreVisible));
        }

        servidor ??= await _servicioDeDialogos.MostrarDialogoDeConexionAsync(perfil);

        if (servidor is not null)
        {
            Explorador.AgregarServidor(servidor);
        }

        return servidor;
    }

    private async Task<DocumentoModeloDeVista?> RestaurarPestanaAsync(ServidorConectado servidor, PestanaGuardada pestana)
    {
        var contexto = new ContextoDelNodo(servidor, pestana.BaseDeDatos);

        if (pestana.Tipo == TipoDePestana.Diagrama)
        {
            await AbrirDiagramaAsync(contexto);
            return DocumentoSeleccionado;
        }

        var texto = pestana.Texto ?? await LeerArchivoParaRestaurarAsync(pestana.RutaDelArchivo);

        if (texto is null)
        {
            return null;
        }

        ActualizarContadorDeConsultasNuevas(pestana.NombreDelArchivo);
        var consulta = await AgregarPestanaAsync(contexto, texto, pestana.NombreDelArchivo);
        consulta.RestaurarEstadoDelArchivo(pestana.RutaDelArchivo, pestana.TieneCambiosSinGuardar);

        return consulta;
    }

    /// <returns>El contenido del archivo, o nulo si ya no existe o no se puede leer.</returns>
    private async Task<string?> LeerArchivoParaRestaurarAsync(string? rutaDelArchivo)
    {
        if (rutaDelArchivo is null || !File.Exists(rutaDelArchivo))
        {
            return null;
        }

        try
        {
            return await _servicioDeArchivosSql.AbrirAsync(rutaDelArchivo, CancellationToken.None);
        }
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError($"Reabrir el archivo {Path.GetFileName(rutaDelArchivo)}"));
            return null;
        }
    }

    /// <summary>
    /// Los scripts de servidores a los que no se pudo reconectar se guardan como archivos para no perderlos.
    /// </summary>
    private async Task ResguardarPestanasNoRestauradasAsync(IReadOnlyList<PestanaGuardada> pestanasNoRestauradas)
    {
        var pestanasConTexto = pestanasNoRestauradas.Where(pestana => pestana.TieneTexto).ToList();

        if (pestanasConTexto.Count == 0)
        {
            return;
        }

        var carpeta = await _servicioDeSesion.GuardarScriptsRecuperadosAsync(pestanasConTexto, CancellationToken.None);

        await _servicioDeDialogos.MostrarInformacionAsync(
            "Scripts guardados",
            $"No se pudo reconectar con el servidor de {pestanasConTexto.Count} pestaña(s).\nSus scripts se guardaron en:\n{carpeta}");
    }

    private void ActualizarContadorDeConsultasNuevas(string nombreDelArchivo)
    {
        var coincidencia = Regex.Match(nombreDelArchivo, $@"^{PrefijoDeConsultaNueva}(\d+)\.sql$", RegexOptions.IgnoreCase);

        if (coincidencia.Success && int.TryParse(coincidencia.Groups[1].Value, out var numero))
        {
            _contadorDeConsultasNuevas = Math.Max(_contadorDeConsultasNuevas, numero);
        }
    }

    private async Task MantenerAutoguardadoAsync(CancellationToken tokenDeCancelacion)
    {
        using var temporizador = new PeriodicTimer(IntervaloDeAutoguardado);

        try
        {
            while (await temporizador.WaitForNextTickAsync(tokenDeCancelacion))
            {
                await GuardarCopiaDeLaSesionAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // El autoguardado se detiene al cerrar la aplicación
        }
    }

    private async Task GuardarCopiaDeLaSesionAsync()
    {
        try
        {
            await _servicioDeSesion.GuardarAsync(CrearEstadoDeLaSesion(esCierreFinal: false), CancellationToken.None);
            _elUltimoAutoguardadoFallo = false;
        }
        catch (Exception error)
        {
            // Solo se registra el primer fallo seguido, para no llenar el registro cada 10 segundos
            if (!_elUltimoAutoguardadoFallo)
            {
                _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Guardar copia de seguridad de las pestañas"));
                _elUltimoAutoguardadoFallo = true;
            }
        }
    }

    private async Task CerrarSesionCorrectamenteAsync()
    {
        await _detencionDelAutoguardado.CancelAsync();

        try
        {
            await _servicioDeSesion.CerrarCorrectamenteAsync(CrearEstadoDeLaSesion(esCierreFinal: true), CancellationToken.None);
        }
        catch (Exception error)
        {
            _servicioDeErrores.RegistrarYDescribir(error, new ContextoDeError("Guardar la sesión al cerrar"));
        }
    }

    /// <param name="esCierreFinal">
    /// Al cerrar, las consultas que siguen con cambios son las que el usuario eligió no guardar:
    /// las nuevas se descartan y las de archivo se reabrirán tal como están en disco.
    /// </param>
    private EstadoDeLaSesion CrearEstadoDeLaSesion(bool esCierreFinal)
    {
        var documentosAGuardar = Documentos
            .Where(documento => documento is not PestanaDeConsultaModeloDeVista { EstaVacia: true })
            .Where(documento => !esCierreFinal || documento is not PestanaDeConsultaModeloDeVista { TieneCambiosSinGuardar: true, RutaDelArchivo: null })
            .ToList();

        var pestanas = documentosAGuardar
            .Select(documento => documento.CrearPestanaGuardada())
            .Select(pestana => esCierreFinal && pestana.TieneCambiosSinGuardar
                ? pestana with { Texto = null, TieneCambiosSinGuardar = false }
                : pestana)
            .ToList();

        var indiceSeleccionado = DocumentoSeleccionado is null ? -1 : documentosAGuardar.IndexOf(DocumentoSeleccionado);
        return new EstadoDeLaSesion(pestanas, indiceSeleccionado);
    }
}
