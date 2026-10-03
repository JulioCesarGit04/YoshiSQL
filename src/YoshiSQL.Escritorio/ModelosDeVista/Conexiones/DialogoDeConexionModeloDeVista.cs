using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Escritorio.ModelosDeVista.Conexiones;

/// <summary>
/// Ventana "Conectar al servidor", equivalente a la de SSMS.
/// </summary>
public sealed partial class DialogoDeConexionModeloDeVista : ModeloDeVistaBase
{
    private const string ServidorPredeterminado = "localhost";
    private const string UsuarioPredeterminado = "sa";

    private readonly ServicioDeConexiones _servicioDeConexiones;
    private CancellationTokenSource? _cancelacionDeLaConexion;
    private Guid _idDelPerfilEnEdicion = Guid.NewGuid();

    public DialogoDeConexionModeloDeVista(ServicioDeConexiones servicioDeConexiones)
    {
        _servicioDeConexiones = servicioDeConexiones;
        AutenticacionSeleccionada = OpcionDeAutenticacion.Todas[0];
    }

    public ObservableCollection<PerfilDeConexion> PerfilesGuardados { get; } = [];

    public IReadOnlyList<OpcionDeAutenticacion> OpcionesDeAutenticacion => OpcionDeAutenticacion.Todas;

    public bool TienePerfilesGuardados => PerfilesGuardados.Count > 0;

    public bool UsaAutenticacionDeSqlServer => AutenticacionSeleccionada.Tipo == TipoDeAutenticacion.SqlServer;

    [ObservableProperty]
    public partial PerfilDeConexion? PerfilSeleccionado { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConectarCommand))]
    public partial string Servidor { get; set; } = ServidorPredeterminado;

    [ObservableProperty]
    public partial int Puerto { get; set; } = PerfilDeConexion.PuertoPredeterminado;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsaAutenticacionDeSqlServer))]
    public partial OpcionDeAutenticacion AutenticacionSeleccionada { get; set; }

    [ObservableProperty]
    public partial string Usuario { get; set; } = UsuarioPredeterminado;

    [ObservableProperty]
    public partial string Contrasena { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string BaseDeDatos { get; set; } = PerfilDeConexion.BaseDeDatosDelSistema;

    [ObservableProperty]
    public partial bool RecordarContrasena { get; set; } = true;

    [ObservableProperty]
    public partial bool ConfiarEnCertificadoDelServidor { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConectarCommand))]
    public partial bool EstaConectando { get; private set; }

    [ObservableProperty]
    public partial string? MensajeDeError { get; private set; }

    /// <summary>
    /// Se dispara cuando la ventana debe cerrarse; lleva el servidor conectado o null si se canceló.
    /// </summary>
    public event EventHandler<ServidorConectado?>? CierreSolicitado;

    public async Task CargarPerfilesGuardadosAsync()
    {
        var perfiles = await _servicioDeConexiones.ObtenerPerfilesGuardadosAsync(CancellationToken.None);

        foreach (var perfil in perfiles)
        {
            PerfilesGuardados.Add(perfil);
        }

        OnPropertyChanged(nameof(TienePerfilesGuardados));
        PerfilSeleccionado = PerfilesGuardados.FirstOrDefault();
    }

    partial void OnPerfilSeleccionadoChanged(PerfilDeConexion? value)
    {
        if (value is not null)
        {
            _ = LlenarConPerfilAsync(value);
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeConectar))]
    private async Task ConectarAsync()
    {
        _cancelacionDeLaConexion = new CancellationTokenSource();
        EstaConectando = true;
        MensajeDeError = null;

        try
        {
            var servidorConectado = await _servicioDeConexiones.ConectarAsync(
                CrearPerfil(),
                Contrasena,
                _cancelacionDeLaConexion.Token);

            CierreSolicitado?.Invoke(this, servidorConectado);
        }
        catch (OperationCanceledException)
        {
            MensajeDeError = null;
        }
        // Límite de la interfaz: cualquier fallo se muestra en la ventana
        catch (Exception error)
        {
            MensajeDeError = error.Message;
        }
        finally
        {
            EstaConectando = false;
            _cancelacionDeLaConexion.Dispose();
            _cancelacionDeLaConexion = null;
        }
    }

    private bool PuedeConectar() => !EstaConectando && !string.IsNullOrWhiteSpace(Servidor);

    [RelayCommand]
    private void Cancelar()
    {
        if (EstaConectando)
        {
            _cancelacionDeLaConexion?.Cancel();
            return;
        }

        CierreSolicitado?.Invoke(this, null);
    }

    [RelayCommand]
    private async Task OlvidarPerfilAsync()
    {
        if (PerfilSeleccionado is not { } perfil)
        {
            return;
        }

        await _servicioDeConexiones.EliminarPerfilAsync(perfil, CancellationToken.None);
        PerfilesGuardados.Remove(perfil);
        PerfilSeleccionado = null;
        _idDelPerfilEnEdicion = Guid.NewGuid();
        OnPropertyChanged(nameof(TienePerfilesGuardados));
    }

    private async Task LlenarConPerfilAsync(PerfilDeConexion perfil)
    {
        _idDelPerfilEnEdicion = perfil.Id;
        Servidor = perfil.Servidor;
        Puerto = perfil.Puerto;
        AutenticacionSeleccionada = OpcionesDeAutenticacion.First(opcion => opcion.Tipo == perfil.TipoDeAutenticacion);
        Usuario = perfil.Usuario;
        BaseDeDatos = perfil.BaseDeDatosPredeterminada;
        RecordarContrasena = perfil.RecordarContrasena;
        ConfiarEnCertificadoDelServidor = perfil.ConfiarEnCertificadoDelServidor;
        Contrasena = await _servicioDeConexiones.ObtenerContrasenaGuardadaAsync(perfil, CancellationToken.None) ?? string.Empty;
    }

    private PerfilDeConexion CrearPerfil() => new()
    {
        Id = ObtenerIdDelPerfil(),
        Servidor = Servidor.Trim(),
        Puerto = Puerto,
        TipoDeAutenticacion = AutenticacionSeleccionada.Tipo,
        Usuario = UsaAutenticacionDeSqlServer ? Usuario.Trim() : string.Empty,
        BaseDeDatosPredeterminada = string.IsNullOrWhiteSpace(BaseDeDatos) ? PerfilDeConexion.BaseDeDatosDelSistema : BaseDeDatos.Trim(),
        RecordarContrasena = RecordarContrasena,
        ConfiarEnCertificadoDelServidor = ConfiarEnCertificadoDelServidor
    };

    /// <summary>
    /// Si el usuario cambió el servidor o el usuario de un perfil guardado, se guarda como uno nuevo.
    /// </summary>
    private Guid ObtenerIdDelPerfil()
    {
        var esElMismoPerfil = PerfilSeleccionado is { } perfil
            && perfil.Id == _idDelPerfilEnEdicion
            && perfil.Servidor == Servidor.Trim()
            && perfil.Usuario == Usuario.Trim();

        return esElMismoPerfil ? _idDelPerfilEnEdicion : Guid.NewGuid();
    }
}
