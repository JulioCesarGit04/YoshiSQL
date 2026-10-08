using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Dominio.Salud;
using YoshiSQL.Dominio.Seguridad;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class ExploradorDeEsquemaSqlServer : IExploradorDeEsquema
{
    public async Task<Servidor> ObtenerServidorAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion)
    {
        var servidores = await LeerFilasAsync(
            datosDeAcceso,
            PerfilDeConexion.BaseDeDatosDelSistema,
            "ObtenerServidor",
            parametros: [],
            lector => new Servidor(lector.GetString(0), lector.GetString(1), lector.GetString(2)),
            tokenDeCancelacion);

        return servidores[0];
    }

    public Task<IReadOnlyList<BaseDeDatos>> ObtenerBasesDeDatosAsync(
        DatosDeAcceso datosDeAcceso,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso,
            PerfilDeConexion.BaseDeDatosDelSistema,
            "ListarBasesDeDatos",
            parametros: [],
            lector => new BaseDeDatos(lector.GetString(0), lector.GetBoolean(1), lector.GetBoolean(2)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<Tabla>> ObtenerTablasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarTablas", parametros: [],
            lector => new Tabla(lector.GetString(0), lector.GetString(1)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<Vista>> ObtenerVistasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarVistas", parametros: [],
            lector => new Vista(lector.GetString(0), lector.GetString(1)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<ProcedimientoAlmacenado>> ObtenerProcedimientosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarProcedimientos", parametros: [],
            lector => new ProcedimientoAlmacenado(lector.GetString(0), lector.GetString(1)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<Funcion>> ObtenerFuncionesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarFunciones", parametros: [],
            lector => new Funcion(lector.GetString(0), lector.GetString(1)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<Columna>> ObtenerColumnasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarColumnas",
            CrearParametrosDelObjeto(objeto),
            lector => LeerColumna(lector, posicionInicial: 0),
            tokenDeCancelacion);

    public Task<IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasTablasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        ObtenerColumnasAgrupadasAsync(
            datosDeAcceso, baseDeDatos, "ListarColumnasDeTodasLasTablas",
            (esquema, nombre) => new Tabla(esquema, nombre),
            tokenDeCancelacion);

    public Task<IReadOnlyDictionary<Vista, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasVistasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        ObtenerColumnasAgrupadasAsync(
            datosDeAcceso, baseDeDatos, "ListarColumnasDeTodasLasVistas",
            (esquema, nombre) => new Vista(esquema, nombre),
            tokenDeCancelacion);

    /// <summary>
    /// Lee una consulta cuyas dos primeras columnas son esquema y objeto, seguidas de los datos de cada columna.
    /// </summary>
    private static async Task<IReadOnlyDictionary<TObjeto, IReadOnlyList<Columna>>> ObtenerColumnasAgrupadasAsync<TObjeto>(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        string nombreDeLaConsulta,
        Func<string, string, TObjeto> crearObjeto,
        CancellationToken tokenDeCancelacion)
        where TObjeto : ObjetoDeEsquema
    {
        const int PosicionDeLaPrimeraColumnaDeDatos = 2;

        var filas = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, nombreDeLaConsulta, parametros: [],
            lector => (
                Objeto: crearObjeto(lector.GetString(0), lector.GetString(1)),
                Columna: LeerColumna(lector, PosicionDeLaPrimeraColumnaDeDatos)),
            tokenDeCancelacion);

        return filas
            .GroupBy(fila => fila.Objeto)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<Columna>)grupo.Select(fila => fila.Columna).ToList());
    }

    public async Task<string?> ObtenerDefinicionAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion)
    {
        var definiciones = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ObtenerDefinicion",
            CrearParametrosDelObjeto(objeto),
            lector => lector.IsDBNull(0) ? null : lector.GetString(0),
            tokenDeCancelacion);

        return definiciones.Count == 0 ? null : definiciones[0];
    }

    public async Task<IReadOnlyList<Indice>> ObtenerIndicesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion)
    {
        var columnasDeIndices = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarIndices",
            CrearParametrosDelObjeto(tabla),
            lector => new
            {
                NombreDelIndice = lector.GetString(0),
                EsUnico = lector.GetBoolean(1),
                EsLlavePrimaria = lector.GetBoolean(2),
                EsAgrupado = lector.GetBoolean(3),
                NombreDeColumna = lector.GetString(4)
            },
            tokenDeCancelacion);

        return columnasDeIndices
            .GroupBy(fila => fila.NombreDelIndice)
            .Select(grupo =>
            {
                var primeraFila = grupo.First();
                return new Indice(
                    grupo.Key,
                    primeraFila.EsUnico,
                    primeraFila.EsLlavePrimaria,
                    primeraFila.EsAgrupado,
                    grupo.Select(fila => fila.NombreDeColumna).ToList());
            })
            .ToList();
    }

    public async Task<PropiedadesDeTabla> ObtenerPropiedadesDeTablaAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion)
    {
        var propiedades = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ObtenerPropiedadesDeTabla",
            CrearParametrosDelObjeto(tabla),
            lector => new PropiedadesDeTabla(
                Creacion: lector.GetDateTime(0),
                Modificacion: lector.GetDateTime(1),
                Filas: lector.GetInt64(2),
                EspacioTotalKb: lector.GetInt64(3),
                EspacioUsadoKb: lector.GetInt64(4),
                Columnas: lector.GetInt32(5),
                Indices: lector.GetInt32(6)),
            tokenDeCancelacion);

        return propiedades.Count > 0
            ? propiedades[0]
            : throw new InvalidOperationException($"No se encontraron las propiedades de la tabla {tabla.NombreCompleto}.");
    }

    public async Task<PropiedadesDeBaseDeDatos> ObtenerPropiedadesDeBaseDeDatosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var propiedades = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ObtenerPropiedadesDeBaseDeDatos",
            [new SqlParameter("@nombre", baseDeDatos)],
            lector => new PropiedadesDeBaseDeDatos(
                Nombre: lector.GetString(0),
                Estado: lector.GetString(1),
                ModeloDeRecuperacion: lector.GetString(2),
                Intercalacion: lector.IsDBNull(3) ? null : lector.GetString(3),
                NivelDeCompatibilidad: lector.GetByte(4),
                Propietario: lector.IsDBNull(5) ? null : lector.GetString(5),
                Creacion: lector.GetDateTime(6),
                TamanoKb: lector.GetInt64(7)),
            tokenDeCancelacion);

        return propiedades.Count > 0
            ? propiedades[0]
            : throw new InvalidOperationException($"No se encontraron las propiedades de la base de datos {baseDeDatos}.");
    }

    public Task<IReadOnlyList<DependenciaDeObjeto>> ObtenerDependenciasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ObtenerDependencias",
            CrearParametrosDelObjeto(objeto),
            lector => new DependenciaDeObjeto(
                Esquema: lector.IsDBNull(0) ? null : lector.GetString(0),
                Nombre: lector.GetString(1),
                Relacion: lector.GetString(2)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<Disparador>> ObtenerDisparadoresAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        Tabla tabla,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarDisparadores",
            CrearParametrosDelObjeto(tabla),
            lector => new Disparador(lector.GetString(0), lector.GetBoolean(1)),
            tokenDeCancelacion);

    public async Task<SaludDeLaBaseDeDatos> ObtenerSaludAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var resumen = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "SaludResumen", parametros: [],
            lector => new ResumenDeSalud(lector.GetInt32(0), lector.GetInt64(1), lector.GetInt64(2), lector.GetInt64(3)),
            tokenDeCancelacion);

        var tablasPesadas = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "SaludTablasPesadas", parametros: [],
            lector => new TablaPesada(lector.GetString(0), lector.GetInt64(1), lector.GetInt64(2)),
            tokenDeCancelacion);

        var indicesFragmentados = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "SaludIndicesFragmentados", parametros: [],
            lector => new IndiceFragmentado(lector.GetString(0), lector.GetString(1), lector.GetDouble(2), lector.GetInt64(3)),
            tokenDeCancelacion);

        return new SaludDeLaBaseDeDatos(
            resumen.Count > 0 ? resumen[0] : new ResumenDeSalud(0, 0, 0, 0),
            tablasPesadas,
            indicesFragmentados);
    }

    public Task<IReadOnlyList<ParametroDeProcedimiento>> ObtenerParametrosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        ObjetoDeEsquema procedimiento,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarParametros",
            CrearParametrosDelObjeto(procedimiento),
            lector => new ParametroDeProcedimiento(
                Nombre: lector.GetString(0),
                TipoDeDato: new TipoDeDato(
                    lector.GetString(1),
                    LeerEnteroOpcional(lector, 2),
                    LeerEnteroOpcional(lector, 3),
                    LeerEnteroOpcional(lector, 4)),
                EsSalida: lector.GetBoolean(5)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<InicioDeSesion>> ObtenerIniciosDeSesionAsync(
        DatosDeAcceso datosDeAcceso,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, PerfilDeConexion.BaseDeDatosDelSistema, "ListarIniciosDeSesion", parametros: [],
            lector => new InicioDeSesion(lector.GetString(0), lector.GetString(1), lector.GetBoolean(2)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<UsuarioDeBaseDeDatos>> ObtenerUsuariosAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarUsuarios", parametros: [],
            lector => new UsuarioDeBaseDeDatos(lector.GetString(0), lector.GetString(1), lector.IsDBNull(2) ? null : lector.GetString(2)),
            tokenDeCancelacion);

    public Task<IReadOnlyList<RolDeBaseDeDatos>> ObtenerRolesAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion) =>
        LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarRoles", parametros: [],
            lector => new RolDeBaseDeDatos(lector.GetString(0), lector.GetBoolean(1)),
            tokenDeCancelacion);

    public async Task<IReadOnlyList<LlaveForanea>> ObtenerLlavesForaneasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var columnasDeLlaves = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarLlavesForaneas", parametros: [],
            lector => new
            {
                NombreDeLaLlave = lector.GetString(0),
                TablaOrigen = new Tabla(lector.GetString(1), lector.GetString(2)),
                ColumnaOrigen = lector.GetString(3),
                TablaDestino = new Tabla(lector.GetString(4), lector.GetString(5)),
                ColumnaDestino = lector.GetString(6)
            },
            tokenDeCancelacion);

        return columnasDeLlaves
            .GroupBy(fila => (fila.NombreDeLaLlave, fila.TablaOrigen))
            .Select(grupo =>
            {
                var primeraFila = grupo.First();
                return new LlaveForanea(
                    primeraFila.NombreDeLaLlave,
                    primeraFila.TablaOrigen,
                    grupo.Select(fila => fila.ColumnaOrigen).ToList(),
                    primeraFila.TablaDestino,
                    grupo.Select(fila => fila.ColumnaDestino).ToList());
            })
            .ToList();
    }

    /// <param name="posicionInicial">Posición en la fila donde empiezan los datos de la columna.</param>
    private static Columna LeerColumna(SqlDataReader lector, int posicionInicial)
    {
        var tipoDeDato = new TipoDeDato(
            Nombre: lector.GetString(posicionInicial + 1),
            Longitud: LeerEnteroOpcional(lector, posicionInicial + 2),
            Precision: LeerEnteroOpcional(lector, posicionInicial + 3),
            Escala: LeerEnteroOpcional(lector, posicionInicial + 4));

        return new Columna(
            Nombre: lector.GetString(posicionInicial),
            TipoDeDato: tipoDeDato,
            AdmiteNulos: lector.GetBoolean(posicionInicial + 5),
            EsLlavePrimaria: lector.GetBoolean(posicionInicial + 6),
            EsIdentidad: lector.GetBoolean(posicionInicial + 7),
            Posicion: lector.GetInt32(posicionInicial + 8));
    }

    private static int? LeerEnteroOpcional(SqlDataReader lector, int posicion) =>
        lector.IsDBNull(posicion) ? null : lector.GetInt32(posicion);

    private static SqlParameter[] CrearParametrosDelObjeto(ObjetoDeEsquema objeto) =>
    [
        new SqlParameter("@esquema", objeto.Esquema),
        new SqlParameter("@nombre", objeto.Nombre)
    ];

    internal static async Task<IReadOnlyList<T>> LeerFilasAsync<T>(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        string nombreDeLaConsulta,
        SqlParameter[] parametros,
        Func<SqlDataReader, T> convertirFila,
        CancellationToken tokenDeCancelacion)
    {
        try
        {
            await using var conexion = new SqlConnection(
                ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, baseDeDatos, usarPoolDeConexiones: true));
            await conexion.OpenAsync(tokenDeCancelacion);

            await using var comando = new SqlCommand(LectorDeConsultasDeCatalogo.Leer(nombreDeLaConsulta), conexion);
            comando.Parameters.AddRange(parametros);

            await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);
            var filas = new List<T>();

            while (await lector.ReadAsync(tokenDeCancelacion))
            {
                filas.Add(convertirFila(lector));
            }

            return filas;
        }
        catch (SqlException excepcion)
        {
            throw TraductorDeErroresSqlServer.TraducirErrorDeConexion(excepcion);
        }
    }
}
