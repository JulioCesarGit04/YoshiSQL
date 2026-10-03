using Microsoft.Extensions.Logging.Abstractions;
using YoshiSQL.Aplicacion.Autocompletado;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Autocompletado;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Pruebas.Autocompletado;

public class ServicioDeAutocompletadoPruebas
{
    private static readonly Tabla Clientes = new("dbo", "Clientes");
    private static readonly Tabla Empleados = new("rrhh", "Empleados");
    private static readonly Tabla TablaConEspacios = new("dbo", "Detalle de Pedido");

    private readonly AnalizadorFalso _analizador = new();
    private readonly ExploradorFalso _explorador = new();
    private readonly ServicioDeAutocompletado _servicio;
    private readonly ServidorConectado _servidor = new(
        new DatosDeAcceso(new PerfilDeConexion { Servidor = "localhost" }, "x"),
        new Servidor("localhost", "17.0", "Developer"));

    public ServicioDeAutocompletadoPruebas()
    {
        var proveedor = new ProveedorFalso(_analizador, _explorador);
        _servicio = new ServicioDeAutocompletado(proveedor, TimeProvider.System, NullLogger<ServicioDeAutocompletado>.Instance);
    }

    [Fact]
    public async Task DespuesDeFrom_SugiereTablasYEsquemasSinRepetirDbo()
    {
        _analizador.Contexto = new ContextoDeAutocompletado(TipoDeContexto.NombreDeTabla, "", null, []);

        var textos = (await SugerirAsync()).Select(sugerencia => sugerencia.Texto).ToList();

        Assert.Contains("Clientes", textos);
        Assert.Contains("rrhh.Empleados", textos);
        Assert.Contains("[Detalle de Pedido]", textos);
        Assert.Contains("rrhh", textos);
        Assert.DoesNotContain("dbo", textos);
    }

    [Fact]
    public async Task DespuesDeAliasConPunto_SugiereSoloLasColumnasDeEsaTabla()
    {
        _analizador.Contexto = new ContextoDeAutocompletado(
            TipoDeContexto.DespuesDePunto, "", "c", [new ReferenciaDeTabla("dbo", "Clientes", "c")]);

        var sugerencias = await SugerirAsync();

        Assert.All(sugerencias, sugerencia => Assert.Equal(TipoDeSugerencia.Columna, sugerencia.Tipo));
        Assert.Equal(["Id", "Nombre"], sugerencias.Select(sugerencia => sugerencia.Texto));
    }

    [Fact]
    public async Task DespuesDeEsquemaConPunto_SugiereLasTablasDelEsquema()
    {
        _analizador.Contexto = new ContextoDeAutocompletado(TipoDeContexto.DespuesDePunto, "", "rrhh", []);

        var sugerencia = Assert.Single(await SugerirAsync());

        Assert.Equal("Empleados", sugerencia.Texto);
    }

    [Fact]
    public async Task ContextoGeneral_IncluyeColumnasDeLaInstruccionYPalabrasClave()
    {
        _analizador.Contexto = new ContextoDeAutocompletado(
            TipoDeContexto.General, "", null, [new ReferenciaDeTabla(null, "Clientes", null)]);

        var sugerencias = await SugerirAsync();

        Assert.Contains(sugerencias, sugerencia => sugerencia is { Texto: "Nombre", Tipo: TipoDeSugerencia.Columna });
        Assert.Contains(sugerencias, sugerencia => sugerencia is { Texto: "SELECT", Tipo: TipoDeSugerencia.PalabraClave });
    }

    [Fact]
    public async Task ElCatalogoSeLeeUnaSolaVezHastaQueSeInvalida()
    {
        _analizador.Contexto = new ContextoDeAutocompletado(TipoDeContexto.NombreDeTabla, "", null, []);

        await SugerirAsync();
        await SugerirAsync();
        Assert.Equal(1, _explorador.LecturasDelCatalogo);

        _servicio.InvalidarCatalogo(_servidor, "Ventas");
        await SugerirAsync();
        Assert.Equal(2, _explorador.LecturasDelCatalogo);
    }

    [Fact]
    public async Task SiFallaElCatalogo_IgualSugierePalabrasClave()
    {
        _explorador.DebeFallar = true;
        _analizador.Contexto = new ContextoDeAutocompletado(TipoDeContexto.General, "", null, []);

        var sugerencias = await SugerirAsync();

        Assert.Contains(sugerencias, sugerencia => sugerencia.Texto == "SELECT");
    }

    private Task<IReadOnlyList<Sugerencia>> SugerirAsync() =>
        _servicio.ObtenerSugerenciasAsync(_servidor, "Ventas", "texto", 0, CancellationToken.None);

    private sealed class AnalizadorFalso : IAnalizadorDeContextoSql
    {
        public ContextoDeAutocompletado Contexto { get; set; } = ContextoDeAutocompletado.SinSugerencias;

        public IReadOnlyList<string> PalabrasClave => ["SELECT", "FROM"];

        public IReadOnlyList<string> Funciones => ["COUNT"];

        public ContextoDeAutocompletado Analizar(string textoCompleto, int posicionDelCursor) => Contexto;
    }

    private sealed class ExploradorFalso : IExploradorDeEsquema
    {
        public int LecturasDelCatalogo { get; private set; }

        public bool DebeFallar { get; set; }

        public Task<IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasTablasAsync(
            DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion)
        {
            LecturasDelCatalogo++;

            if (DebeFallar)
            {
                throw new InvalidOperationException("Servidor no disponible");
            }

            IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>> columnas = new Dictionary<Tabla, IReadOnlyList<Columna>>
            {
                [Clientes] = [CrearColumna("Id"), CrearColumna("Nombre")],
                [Empleados] = [CrearColumna("Id")],
                [TablaConEspacios] = [CrearColumna("Cantidad")]
            };

            return Task.FromResult(columnas);
        }

        public Task<IReadOnlyDictionary<Vista, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasVistasAsync(
            DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) =>
            Task.FromResult<IReadOnlyDictionary<Vista, IReadOnlyList<Columna>>>(new Dictionary<Vista, IReadOnlyList<Columna>>());

        private static Columna CrearColumna(string nombre) =>
            new(nombre, new TipoDeDato("int"), AdmiteNulos: false, EsLlavePrimaria: false, EsIdentidad: false, Posicion: 1);

        public Task<Servidor> ObtenerServidorAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<BaseDeDatos>> ObtenerBasesDeDatosAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<Tabla>> ObtenerTablasAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<Vista>> ObtenerVistasAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProcedimientoAlmacenado>> ObtenerProcedimientosAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<Funcion>> ObtenerFuncionesAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<Columna>> ObtenerColumnasAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, ObjetoDeEsquema objeto, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<string?> ObtenerDefinicionAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, ObjetoDeEsquema objeto, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<Indice>> ObtenerIndicesAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, Tabla tabla, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<LlaveForanea>> ObtenerLlavesForaneasAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<YoshiSQL.Dominio.Seguridad.InicioDeSesion>> ObtenerIniciosDeSesionAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<YoshiSQL.Dominio.Seguridad.UsuarioDeBaseDeDatos>> ObtenerUsuariosAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
        public Task<IReadOnlyList<YoshiSQL.Dominio.Seguridad.RolDeBaseDeDatos>> ObtenerRolesAsync(DatosDeAcceso datosDeAcceso, string baseDeDatos, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
    }

    private sealed class ProveedorFalso(IAnalizadorDeContextoSql analizador, IExploradorDeEsquema explorador) : IProveedorDeBaseDeDatos
    {
        public string NombreDelMotor => "Falso";
        public IExploradorDeEsquema Explorador => explorador;
        public IAnalizadorDeContextoSql AnalizadorDeContexto => analizador;
        public IEjecutorDeConsultas Ejecutor => throw new NotSupportedException();
        public IDivisorDeLotes DivisorDeLotes => throw new NotSupportedException();
        public IGeneradorDeScripts GeneradorDeScripts => throw new NotSupportedException();
        public IFormateadorDeSql Formateador => throw new NotSupportedException();
        public IReadOnlyList<string> TiposDeDatoSugeridos => [];
        public IMonitorDeActividad Monitor => throw new NotSupportedException();
        public IAdministradorDeRespaldos Respaldos => throw new NotSupportedException();
        public IAnalizadorDePlanes AnalizadorDePlanes => throw new NotSupportedException();
        public Task ProbarConexionAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion) => throw new NotSupportedException();
    }
}
