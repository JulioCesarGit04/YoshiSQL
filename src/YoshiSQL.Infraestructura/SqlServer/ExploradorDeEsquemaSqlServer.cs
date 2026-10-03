using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;

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

    public async Task<IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>>> ObtenerColumnasDeTodasLasTablasAsync(
        DatosDeAcceso datosDeAcceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        // Las dos primeras columnas de la consulta son el esquema y la tabla
        const int PosicionDeLaPrimeraColumnaDeDatos = 2;

        var filas = await LeerFilasAsync(
            datosDeAcceso, baseDeDatos, "ListarColumnasDeTodasLasTablas", parametros: [],
            lector => (
                Tabla: new Tabla(lector.GetString(0), lector.GetString(1)),
                Columna: LeerColumna(lector, PosicionDeLaPrimeraColumnaDeDatos)),
            tokenDeCancelacion);

        return filas
            .GroupBy(fila => fila.Tabla)
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

    private static async Task<IReadOnlyList<T>> LeerFilasAsync<T>(
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
