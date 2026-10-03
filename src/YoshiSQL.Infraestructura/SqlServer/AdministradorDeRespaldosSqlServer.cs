using System.Text;
using Microsoft.Data.SqlClient;
using YoshiSQL.Dominio.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Respaldos;
using static YoshiSQL.Infraestructura.SqlServer.DelimitadorDeIdentificadores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class AdministradorDeRespaldosSqlServer : IAdministradorDeRespaldos
{
    // Informa el avance cada 10 %, que aparece en la pestaña de mensajes
    private const int PorcentajeDeAvance = 10;
    private const char TipoDeArchivoDeRegistro = 'L';

    public async Task<CarpetasDelServidor> ObtenerCarpetasDelServidorAsync(DatosDeAcceso datosDeAcceso, CancellationToken tokenDeCancelacion)
    {
        var carpetas = await ExploradorDeEsquemaSqlServer.LeerFilasAsync(
            datosDeAcceso,
            PerfilDeConexion.BaseDeDatosDelSistema,
            "ObtenerCarpetasDelServidor",
            parametros: [],
            lector => new CarpetasDelServidor(
                lector.IsDBNull(0) ? string.Empty : lector.GetString(0),
                lector.IsDBNull(1) ? string.Empty : lector.GetString(1),
                lector.IsDBNull(2) ? string.Empty : lector.GetString(2)),
            tokenDeCancelacion);

        return carpetas[0];
    }

    public async Task<InformacionDelRespaldo> LeerRespaldoAsync(DatosDeAcceso datosDeAcceso, string rutaDelArchivo, CancellationToken tokenDeCancelacion)
    {
        try
        {
            await using var conexion = new SqlConnection(
                ConstructorDeCadenaDeConexion.Construir(datosDeAcceso, PerfilDeConexion.BaseDeDatosDelSistema, usarPoolDeConexiones: true));
            await conexion.OpenAsync(tokenDeCancelacion);

            var (baseDeDatos, fecha) = await LeerEncabezadoAsync(conexion, rutaDelArchivo, tokenDeCancelacion);
            var archivos = await LeerArchivosAsync(conexion, rutaDelArchivo, tokenDeCancelacion);

            return new InformacionDelRespaldo(baseDeDatos, fecha, archivos);
        }
        catch (SqlException excepcion)
        {
            throw new ErrorDeEjecucion($"No se pudo leer el respaldo \"{rutaDelArchivo}\": {excepcion.Message}", causa: excepcion);
        }
    }

    public string GenerarScriptDeRespaldo(OpcionesDeRespaldo opciones)
    {
        var opcionesDelBackup = new List<string>
        {
            "INIT",
            $"NAME = {EscribirTexto($"{opciones.BaseDeDatos} - respaldo completo")}",
            $"STATS = {PorcentajeDeAvance}"
        };

        if (opciones.SoloCopia)
        {
            opcionesDelBackup.Insert(0, "COPY_ONLY");
        }

        if (opciones.Comprimir)
        {
            opcionesDelBackup.Add("COMPRESSION");
        }

        var script = new StringBuilder()
            .AppendLine($"BACKUP DATABASE {Delimitar(opciones.BaseDeDatos)}")
            .AppendLine($"    TO DISK = {EscribirTexto(opciones.RutaDelArchivo)}")
            .AppendLine($"    WITH {string.Join(", ", opcionesDelBackup)};")
            .AppendLine("GO");

        if (opciones.Verificar)
        {
            script.AppendLine()
                .AppendLine($"RESTORE VERIFYONLY FROM DISK = {EscribirTexto(opciones.RutaDelArchivo)};")
                .AppendLine("GO");
        }

        return script.ToString().TrimEnd();
    }

    public string GenerarScriptDeRestauracion(OpcionesDeRestauracion opciones)
    {
        var destino = Delimitar(opciones.BaseDeDatosDestino);
        var nombreDelDestino = EscribirTexto(opciones.BaseDeDatosDestino);
        var script = new StringBuilder().AppendLine("USE [master];").AppendLine("GO").AppendLine();

        if (opciones.CerrarConexionesExistentes)
        {
            script.AppendLine($"IF DB_ID({nombreDelDestino}) IS NOT NULL")
                .AppendLine($"    ALTER DATABASE {destino} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;")
                .AppendLine("GO")
                .AppendLine();
        }

        var opcionesDelRestore = UbicarArchivos(opciones)
            .Select(archivo => $"MOVE {EscribirTexto(archivo.NombreLogico)} TO {EscribirTexto(archivo.RutaNueva)}")
            .ToList();

        if (opciones.Reemplazar)
        {
            opcionesDelRestore.Add("REPLACE");
        }

        opcionesDelRestore.Add($"STATS = {PorcentajeDeAvance}");

        script.AppendLine($"RESTORE DATABASE {destino}")
            .AppendLine($"    FROM DISK = {EscribirTexto(opciones.RutaDelArchivo)}")
            .AppendLine($"    WITH {string.Join($",{Environment.NewLine}         ", opcionesDelRestore)};")
            .AppendLine("GO");

        if (opciones.CerrarConexionesExistentes)
        {
            script.AppendLine().AppendLine($"ALTER DATABASE {destino} SET MULTI_USER;").AppendLine("GO");
        }

        return script.ToString().TrimEnd();
    }

    /// <summary>
    /// Cada archivo se renombra según la base de destino (Ventas.mdf, Ventas_2.ndf, Ventas_log.ldf...)
    /// para no chocar con los archivos de la base original si sigue existiendo.
    /// </summary>
    private static IEnumerable<(string NombreLogico, string RutaNueva)> UbicarArchivos(OpcionesDeRestauracion opciones)
    {
        var archivosDeDatos = opciones.Archivos.Where(archivo => !archivo.EsDeRegistro).ToList();
        var archivosDeRegistro = opciones.Archivos.Where(archivo => archivo.EsDeRegistro).ToList();

        for (var indice = 0; indice < archivosDeDatos.Count; indice++)
        {
            var nombre = indice == 0 ? $"{opciones.BaseDeDatosDestino}.mdf" : $"{opciones.BaseDeDatosDestino}_{indice + 1}.ndf";
            yield return (archivosDeDatos[indice].NombreLogico, UnirRuta(opciones.CarpetaDeDatos, nombre));
        }

        for (var indice = 0; indice < archivosDeRegistro.Count; indice++)
        {
            var sufijo = indice == 0 ? string.Empty : $"{indice + 1}";
            yield return (archivosDeRegistro[indice].NombreLogico, UnirRuta(opciones.CarpetaDeRegistros, $"{opciones.BaseDeDatosDestino}_log{sufijo}.ldf"));
        }
    }

    /// <summary>
    /// Usa el separador del sistema del servidor: "/" en Linux y "\" en Windows.
    /// </summary>
    private static string UnirRuta(string carpeta, string nombreDelArchivo)
    {
        var separador = carpeta.Contains('\\') ? '\\' : '/';
        return $"{carpeta.TrimEnd('/', '\\')}{separador}{nombreDelArchivo}";
    }

    private static async Task<(string BaseDeDatos, DateTime Fecha)> LeerEncabezadoAsync(
        SqlConnection conexion,
        string rutaDelArchivo,
        CancellationToken tokenDeCancelacion)
    {
        await using var comando = CrearComandoDeLectura(conexion, "RESTORE HEADERONLY FROM DISK = @ruta;", rutaDelArchivo);
        await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);

        if (!await lector.ReadAsync(tokenDeCancelacion))
        {
            throw new ErrorDeYoshiSql("El archivo no contiene ningún respaldo.");
        }

        return (lector.GetString(lector.GetOrdinal("DatabaseName")), lector.GetDateTime(lector.GetOrdinal("BackupFinishDate")));
    }

    private static async Task<List<ArchivoDelRespaldo>> LeerArchivosAsync(
        SqlConnection conexion,
        string rutaDelArchivo,
        CancellationToken tokenDeCancelacion)
    {
        await using var comando = CrearComandoDeLectura(conexion, "RESTORE FILELISTONLY FROM DISK = @ruta;", rutaDelArchivo);
        await using var lector = await comando.ExecuteReaderAsync(tokenDeCancelacion);
        var archivos = new List<ArchivoDelRespaldo>();

        while (await lector.ReadAsync(tokenDeCancelacion))
        {
            archivos.Add(new ArchivoDelRespaldo(
                lector.GetString(lector.GetOrdinal("LogicalName")),
                lector.GetString(lector.GetOrdinal("PhysicalName")),
                lector.GetString(lector.GetOrdinal("Type"))[0] == TipoDeArchivoDeRegistro));
        }

        return archivos;
    }

    // RESTORE acepta la ruta como parámetro, así que nunca se concatena al texto
    private static SqlCommand CrearComandoDeLectura(SqlConnection conexion, string texto, string rutaDelArchivo)
    {
        var comando = new SqlCommand(texto, conexion);
        comando.Parameters.AddWithValue("@ruta", rutaDelArchivo);
        return comando;
    }

    private static string EscribirTexto(string texto) => $"N'{texto.Replace("'", "''", StringComparison.Ordinal)}'";
}
