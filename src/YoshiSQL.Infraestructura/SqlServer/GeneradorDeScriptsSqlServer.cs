using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;
using static YoshiSQL.Infraestructura.SqlServer.DelimitadorDeIdentificadores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class GeneradorDeScriptsSqlServer : IGeneradorDeScripts
{
    private const string SangriaDeColumna = "    ";

    public string GenerarSeleccionDeFilas(
        string baseDeDatos,
        ObjetoDeEsquema objeto,
        int cantidadDeFilas,
        string? filtroWhere = null,
        string? ordenarPor = null)
    {
        var consulta = new StringBuilder()
            .AppendLine($"USE {Delimitar(baseDeDatos)};")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine($"SELECT TOP ({cantidadDeFilas}) *")
            .Append($"FROM {Delimitar(objeto.Esquema, objeto.Nombre)}");

        if (!string.IsNullOrWhiteSpace(filtroWhere))
        {
            consulta.AppendLine().Append($"WHERE {filtroWhere.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(ordenarPor))
        {
            consulta.AppendLine().Append($"ORDER BY {ordenarPor.Trim()}");
        }

        return consulta.Append(';').ToString();
    }

    public string GenerarInstruccionDml(string baseDeDatos, ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas, TipoDeScriptDml tipo)
    {
        var ordenadas = columnas.OrderBy(columna => columna.Posicion).ToList();

        var instruccion = tipo switch
        {
            TipoDeScriptDml.Seleccion => CrearSeleccionExplicita(objeto, ordenadas),
            TipoDeScriptDml.Insercion => CrearInsercion(objeto, ordenadas),
            TipoDeScriptDml.Actualizacion => CrearActualizacion(objeto, ordenadas),
            TipoDeScriptDml.Eliminacion => CrearEliminacionDeFilas(objeto, ordenadas),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo))
        };

        return EnvolverEnBaseDeDatos(baseDeDatos, instruccion);
    }

    // Marcador tipo SSMS que el usuario reemplaza por un valor: <Nombre, tipo,>
    private static string MarcadorDeValor(Columna columna) => $"<{columna.Nombre}, {columna.TipoDeDato.Describir()},>";

    private static string CrearSeleccionExplicita(ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas)
    {
        var lista = columnas.Count == 0
            ? "*"
            : string.Join($",{Environment.NewLine}       ", columnas.Select(columna => Delimitar(columna.Nombre)));

        return $"SELECT {lista}{Environment.NewLine}FROM {Delimitar(objeto.Esquema, objeto.Nombre)};";
    }

    private static string CrearInsercion(ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas)
    {
        // La identidad la asigna SQL Server; no se incluye en el INSERT
        var insertables = columnas.Where(columna => !columna.EsIdentidad).ToList();

        return $"INSERT INTO {Delimitar(objeto.Esquema, objeto.Nombre)} ({UnirNombres(insertables.Select(columna => columna.Nombre))}){Environment.NewLine}"
            + $"VALUES ({string.Join(", ", insertables.Select(MarcadorDeValor))});";
    }

    private static string CrearActualizacion(ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas)
    {
        var modificables = columnas.Where(columna => !columna.EsIdentidad).ToList();
        var asignaciones = string.Join(
            $",{Environment.NewLine}    ",
            modificables.Select(columna => $"{Delimitar(columna.Nombre)} = {MarcadorDeValor(columna)}"));

        return $"UPDATE {Delimitar(objeto.Esquema, objeto.Nombre)}{Environment.NewLine}"
            + $"SET {asignaciones}{Environment.NewLine}"
            + $"WHERE {CondicionDeBusqueda(columnas)};";
    }

    private static string CrearEliminacionDeFilas(ObjetoDeEsquema objeto, IReadOnlyList<Columna> columnas) =>
        $"DELETE FROM {Delimitar(objeto.Esquema, objeto.Nombre)}{Environment.NewLine}WHERE {CondicionDeBusqueda(columnas)};";

    private static string CondicionDeBusqueda(IReadOnlyList<Columna> columnas)
    {
        var llave = columnas.Where(columna => columna.EsLlavePrimaria).ToList();

        return llave.Count == 0
            ? "<condición de búsqueda, ,>"
            : string.Join(" AND ", llave.Select(columna => $"{Delimitar(columna.Nombre)} = {MarcadorDeValor(columna)}"));
    }

    public string GenerarRenombrado(string baseDeDatos, ObjetoDeEsquema objeto, string nuevoNombre) =>
        EnvolverEnBaseDeDatos(
            baseDeDatos,
            $"EXEC sys.sp_rename {EscribirTexto(Delimitar(objeto.Esquema, objeto.Nombre))}, {EscribirTexto(nuevoNombre)};");

    public string GenerarCreacionDeTablaSinEnvolver(Tabla tabla, IReadOnlyList<Columna> columnas)
    {
        var definicionesDeColumnas = columnas
            .OrderBy(columna => columna.Posicion)
            .Select(columna => DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos))
            .ToList();

        var columnasDeLaLlave = columnas.Where(columna => columna.EsLlavePrimaria).Select(columna => columna.Nombre).ToList();

        return CrearInstruccionCreateTable(tabla, definicionesDeColumnas, columnasDeLaLlave);
    }

    public string GenerarLlaveForanea(LlaveForanea llave) =>
        $"ALTER TABLE {Delimitar(llave.TablaOrigen.Esquema, llave.TablaOrigen.Nombre)} "
        + $"ADD CONSTRAINT {Delimitar(llave.Nombre)} FOREIGN KEY ({UnirNombres(llave.ColumnasOrigen)}) "
        + $"REFERENCES {Delimitar(llave.TablaDestino.Esquema, llave.TablaDestino.Nombre)} ({UnirNombres(llave.ColumnasDestino)});";

    public string GenerarInsertDeFilas(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<object?[]> filas)
    {
        if (filas.Count == 0)
        {
            return string.Empty;
        }

        var ordenadas = columnas.OrderBy(columna => columna.Posicion).ToList();
        var nombreDeLaTabla = Delimitar(tabla.Esquema, tabla.Nombre);
        var listaDeColumnas = UnirNombres(ordenadas.Select(columna => columna.Nombre));
        var tieneIdentidad = ordenadas.Any(columna => columna.EsIdentidad);

        var texto = new StringBuilder();

        if (tieneIdentidad)
        {
            texto.AppendLine($"SET IDENTITY_INSERT {nombreDeLaTabla} ON;");
        }

        foreach (var fila in filas)
        {
            var valores = string.Join(", ", fila.Select(EscribirLiteralSql));
            texto.AppendLine($"INSERT INTO {nombreDeLaTabla} ({listaDeColumnas}) VALUES ({valores});");
        }

        if (tieneIdentidad)
        {
            texto.Append($"SET IDENTITY_INSERT {nombreDeLaTabla} OFF;");
        }

        return texto.ToString();
    }

    public string GenerarEjecucionDeProcedimiento(string baseDeDatos, ObjetoDeEsquema procedimiento, IReadOnlyList<ParametroDeProcedimiento> parametros)
    {
        var salidas = parametros.Where(parametro => parametro.EsSalida).ToList();
        var cuerpo = new StringBuilder();

        // Las variables para los parámetros OUTPUT se declaran antes del EXEC
        foreach (var salida in salidas)
        {
            cuerpo.AppendLine($"DECLARE {salida.Nombre} {salida.TipoDeDato.Describir()};");
        }

        if (salidas.Count > 0)
        {
            cuerpo.AppendLine();
        }

        cuerpo.Append($"EXEC {Delimitar(procedimiento.Esquema, procedimiento.Nombre)}");

        if (parametros.Count > 0)
        {
            var lineas = parametros.Select(parametro => parametro.EsSalida
                ? $"    {parametro.Nombre} = {parametro.Nombre} OUTPUT"
                : $"    {parametro.Nombre} = <{parametro.Nombre.TrimStart('@')}, {parametro.TipoDeDato.Describir()},>");

            cuerpo.Append(Environment.NewLine).Append(string.Join($",{Environment.NewLine}", lineas));
        }

        cuerpo.Append(';');

        // Al final se muestran los valores de salida para poder verlos
        foreach (var salida in salidas)
        {
            cuerpo.Append(Environment.NewLine).Append($"SELECT {salida.Nombre} AS {Delimitar(salida.Nombre.TrimStart('@'))};");
        }

        return EnvolverEnBaseDeDatos(baseDeDatos, cuerpo.ToString());
    }

    public string GenerarUso(string baseDeDatos) => $"USE {Delimitar(baseDeDatos)};";

    public string GenerarSeleccionCompleta(Tabla tabla) => $"SELECT * FROM {Delimitar(tabla.Esquema, tabla.Nombre)};";

    private static string EscribirLiteralSql(object? valor) => valor switch
    {
        null => "NULL",
        bool booleano => booleano ? "1" : "0",
        byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
        DateTime fecha => $"'{fecha.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)}'",
        DateTimeOffset fecha => $"'{fecha.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", System.Globalization.CultureInfo.InvariantCulture)}'",
        TimeSpan hora => $"'{hora.ToString("c", System.Globalization.CultureInfo.InvariantCulture)}'",
        Guid identificador => $"'{identificador}'",
        byte or short or int or long or decimal or double or float => Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL",
        _ => $"N'{valor.ToString()!.Replace("'", "''", StringComparison.Ordinal)}'"
    };

    public string GenerarConsultaDeFragmentacion(string baseDeDatos, Tabla tabla) =>
        EnvolverEnBaseDeDatos(
            baseDeDatos,
            $"""
            SELECT
                i.name AS Indice,
                i.type_desc AS Tipo,
                estadisticas.avg_fragmentation_in_percent AS FragmentacionPorcentaje,
                estadisticas.page_count AS Paginas
            FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID({EscribirTexto(Delimitar(tabla.Esquema, tabla.Nombre))}), NULL, NULL, 'LIMITED') AS estadisticas
            JOIN sys.indexes i ON i.object_id = estadisticas.object_id AND i.index_id = estadisticas.index_id
            WHERE i.index_id > 0
            ORDER BY estadisticas.avg_fragmentation_in_percent DESC;
            """);

    public string GenerarMantenimientoDeIndice(string baseDeDatos, Tabla tabla, string nombreDelIndice, bool reconstruir) =>
        EnvolverEnBaseDeDatos(
            baseDeDatos,
            $"ALTER INDEX {Delimitar(nombreDelIndice)} ON {Delimitar(tabla.Esquema, tabla.Nombre)} {(reconstruir ? "REBUILD" : "REORGANIZE")};");

    public string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        $"""
        CREATE DATABASE {Delimitar(nombreDeLaBaseDeDatos)};
        GO
        """;

    public string GenerarCreacionDeTabla(string baseDeDatos, Tabla tabla, IReadOnlyList<Columna> columnas)
    {
        var definicionesDeColumnas = columnas
            .OrderBy(columna => columna.Posicion)
            .Select(columna => DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos))
            .ToList();

        var columnasDeLaLlave = columnas.Where(columna => columna.EsLlavePrimaria).Select(columna => columna.Nombre).ToList();

        return new StringBuilder()
            .AppendLine($"USE {Delimitar(baseDeDatos)};")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine(CrearInstruccionCreateTable(tabla, definicionesDeColumnas, columnasDeLaLlave))
            .Append("GO")
            .ToString();
    }

    public string GenerarCambiosDeTabla(string baseDeDatos, DefinicionDeTabla? original, DefinicionDeTabla nueva)
    {
        var instrucciones = original is null
            ? [CrearInstruccionCreateTable(
                nueva.Tabla,
                nueva.Columnas.Select(columna => DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos)).ToList(),
                nueva.ColumnasDeLaLlavePrimaria.Select(columna => columna.Nombre).ToList())]
            : CrearInstruccionesDeModificacion(original, nueva);

        var script = new StringBuilder()
            .AppendLine($"USE {Delimitar(baseDeDatos)};")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine("-- Si una instrucción falla, se deshacen todas")
            .AppendLine("SET XACT_ABORT ON;")
            .AppendLine("BEGIN TRANSACTION;")
            .AppendLine();

        foreach (var instruccion in instrucciones)
        {
            script.AppendLine(instruccion).AppendLine();
        }

        return script.AppendLine("COMMIT TRANSACTION;").Append("GO").ToString();
    }

    public IReadOnlyList<ComandoSql> GenerarComandosDeEdicion(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<CambioDeFila> cambios)
    {
        var posicionesDeLaLlave = Enumerable.Range(0, columnas.Count).Where(posicion => columnas[posicion].EsLlavePrimaria).ToList();

        if (posicionesDeLaLlave.Count == 0)
        {
            throw new ErrorDeYoshiSql($"La tabla {tabla.NombreCompleto} no tiene llave primaria; no se puede identificar cada fila de forma segura.");
        }

        return cambios.Select(cambio => cambio.Tipo switch
        {
            EstadoDeFila.Nueva => CrearInsert(tabla, columnas, cambio),
            EstadoDeFila.Modificada => CrearUpdate(tabla, columnas, cambio, posicionesDeLaLlave),
            EstadoDeFila.Eliminada => CrearDelete(tabla, columnas, cambio, posicionesDeLaLlave),
            _ => throw new ArgumentOutOfRangeException(nameof(cambios), cambio.Tipo, "Una fila sin cambios no genera instrucciones.")
        }).ToList();
    }

    public string GenerarEliminacion(string baseDeDatos, ObjetoDeEsquema objeto)
    {
        var tipoEnSql = objeto.Tipo switch
        {
            TipoDeObjeto.Tabla => "TABLE",
            TipoDeObjeto.Vista => "VIEW",
            TipoDeObjeto.ProcedimientoAlmacenado => "PROCEDURE",
            TipoDeObjeto.Funcion => "FUNCTION",
            _ => throw new ArgumentOutOfRangeException(nameof(objeto), objeto.Tipo, "Tipo de objeto no eliminable.")
        };

        return $"""
            USE {Delimitar(baseDeDatos)};
            GO

            DROP {tipoEnSql} IF EXISTS {Delimitar(objeto.Esquema, objeto.Nombre)};
            GO
            """;
    }

    public string GenerarCreacionDeInicioDeSesion() =>
        """
        USE [master];
        GO

        -- Cambia el nombre y usa una contraseña segura (mínimo 8 caracteres con mayúsculas, minúsculas, números y símbolos)
        CREATE LOGIN [NuevoInicioDeSesion]
            WITH PASSWORD = N'Cambia$EstaClave1',
                 CHECK_POLICY = ON,
                 DEFAULT_DATABASE = [master];
        GO
        """;

    public string GenerarEliminacionDeInicioDeSesion(string nombre) =>
        $"""
        USE [master];
        GO

        DROP LOGIN {Delimitar(nombre)};
        GO
        """;

    public string GenerarCreacionDeUsuario(string baseDeDatos) =>
        $"""
        USE {Delimitar(baseDeDatos)};
        GO

        -- El usuario se asocia a un inicio de sesión que ya exista en el servidor
        CREATE USER [NuevoUsuario] FOR LOGIN [NuevoInicioDeSesion];

        -- Permisos de lectura y escritura sobre todas las tablas
        ALTER ROLE [db_datareader] ADD MEMBER [NuevoUsuario];
        ALTER ROLE [db_datawriter] ADD MEMBER [NuevoUsuario];
        GO
        """;

    public string GenerarEliminacionDeUsuario(string baseDeDatos, string nombre) =>
        $"""
        USE {Delimitar(baseDeDatos)};
        GO

        DROP USER {Delimitar(nombre)};
        GO
        """;

    public string GenerarCreacionDesdeDefinicion(string baseDeDatos, string definicion) =>
        EnvolverEnBaseDeDatos(baseDeDatos, definicion);

    public string GenerarModificacionDesdeDefinicion(string baseDeDatos, string definicion) =>
        EnvolverEnBaseDeDatos(baseDeDatos, CambiarCreatePorAlter(definicion));

    public string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        $"""
        USE [master];
        GO

        ALTER DATABASE {Delimitar(nombreDeLaBaseDeDatos)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE {Delimitar(nombreDeLaBaseDeDatos)};
        GO
        """;

    private static string EnvolverEnBaseDeDatos(string baseDeDatos, string definicion) =>
        $"USE {Delimitar(baseDeDatos)};{Environment.NewLine}GO{Environment.NewLine}{Environment.NewLine}{definicion.Trim()}{Environment.NewLine}GO";

    /// <summary>
    /// Reemplaza la primera palabra CREATE del código por ALTER, respetando comentarios previos.
    /// Si la definición ya dice CREATE OR ALTER, se deja igual porque sirve para modificar.
    /// </summary>
    private static string CambiarCreatePorAlter(string definicion)
    {
        var tokens = new TSql170Parser(initialQuotedIdentifiers: true).GetTokenStream(new StringReader(definicion), out _);
        var tokensSignificativos = tokens?
            .Where(token => token.TokenType is not (TSqlTokenType.WhiteSpace or TSqlTokenType.SingleLineComment or TSqlTokenType.MultilineComment))
            .Take(2)
            .ToList();

        if (tokensSignificativos is not [{ TokenType: TSqlTokenType.Create } tokenCreate, var siguienteToken]
            || siguienteToken.TokenType == TSqlTokenType.Or)
        {
            return definicion;
        }

        return string.Concat(definicion.AsSpan(0, tokenCreate.Offset), "ALTER", definicion.AsSpan(tokenCreate.Offset + tokenCreate.Text.Length));
    }

    private static string CrearInstruccionCreateTable(Tabla tabla, IReadOnlyList<string> definicionesDeColumnas, IReadOnlyList<string> columnasDeLaLlave)
    {
        var lineas = definicionesDeColumnas.ToList();

        if (columnasDeLaLlave.Count > 0)
        {
            lineas.Add($"CONSTRAINT {Delimitar($"PK_{tabla.Nombre}")} PRIMARY KEY ({UnirNombres(columnasDeLaLlave)})");
        }

        return new StringBuilder()
            .AppendLine($"CREATE TABLE {Delimitar(tabla.Esquema, tabla.Nombre)}")
            .AppendLine("(")
            .AppendLine(string.Join($",{Environment.NewLine}", lineas.Select(linea => SangriaDeColumna + linea)))
            .Append(");")
            .ToString();
    }

    /// <summary>
    /// El orden importa: primero se quita la llave y las columnas eliminadas, luego se renombra,
    /// se modifica y se agrega, y al final se vuelve a crear la llave primaria.
    /// </summary>
    private static List<string> CrearInstruccionesDeModificacion(DefinicionDeTabla original, DefinicionDeTabla nueva)
    {
        var cambios = CambiosDeTabla.Calcular(original, nueva);
        var nombreDeLaTabla = Delimitar(nueva.Esquema, nueva.Nombre);
        var instrucciones = new List<string>();

        if (cambios.CambiaLaLlavePrimaria && original.NombreDeLaLlavePrimaria is not null)
        {
            instrucciones.Add($"ALTER TABLE {nombreDeLaTabla} DROP CONSTRAINT {Delimitar(original.NombreDeLaLlavePrimaria)};");
        }

        instrucciones.AddRange(cambios.ColumnasEliminadas.Select(columna =>
            $"ALTER TABLE {nombreDeLaTabla} DROP COLUMN {Delimitar(columna.Nombre)};"));

        instrucciones.AddRange(cambios.ColumnasRenombradas.Select(columna =>
            $"EXEC sp_rename {EscribirTexto($"{nombreDeLaTabla}.{Delimitar(columna.NombreOriginal!)}")}, {EscribirTexto(columna.Nombre)}, N'COLUMN';"));

        instrucciones.AddRange(cambios.ColumnasModificadas.Select(columna =>
            $"ALTER TABLE {nombreDeLaTabla} ALTER COLUMN {Delimitar(columna.Nombre)} {columna.TipoDeDato.Describir()} {(columna.AdmiteNulos ? "NULL" : "NOT NULL")};"));

        instrucciones.AddRange(cambios.ColumnasNuevas.Select(columna =>
            $"ALTER TABLE {nombreDeLaTabla} ADD {DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos)};"));

        var columnasDeLaLlave = nueva.ColumnasDeLaLlavePrimaria.Select(columna => columna.Nombre).ToList();

        if (cambios.CambiaLaLlavePrimaria && columnasDeLaLlave.Count > 0)
        {
            var nombreDeLaLlave = original.NombreDeLaLlavePrimaria ?? $"PK_{nueva.Nombre}";
            instrucciones.Add($"ALTER TABLE {nombreDeLaTabla} ADD CONSTRAINT {Delimitar(nombreDeLaLlave)} PRIMARY KEY ({UnirNombres(columnasDeLaLlave)});");
        }

        return instrucciones.Count > 0 ? instrucciones : ["-- No hay cambios que aplicar."];
    }

    private static ComandoSql CrearInsert(Tabla tabla, IReadOnlyList<Columna> columnas, CambioDeFila cambio)
    {
        // Las celdas vacías no se envían, para que se apliquen los valores predeterminados de la tabla
        var posiciones = Enumerable.Range(0, columnas.Count)
            .Where(posicion => EsEditable(columnas[posicion]) && cambio.ValoresNuevos[posicion] is not null)
            .ToList();

        if (posiciones.Count == 0)
        {
            return new ComandoSql($"INSERT INTO {Delimitar(tabla.Esquema, tabla.Nombre)} DEFAULT VALUES;", []);
        }

        var parametros = posiciones
            .Select((posicion, indice) => new ParametroSql($"@v{indice}", ConvertidorDeValoresDeEdicion.Convertir(cambio.ValoresNuevos[posicion], columnas[posicion])))
            .ToList();

        var texto = $"INSERT INTO {Delimitar(tabla.Esquema, tabla.Nombre)} ({UnirNombres(posiciones.Select(posicion => columnas[posicion].Nombre))}) "
            + $"VALUES ({string.Join(", ", parametros.Select(parametro => parametro.Nombre))});";

        return new ComandoSql(texto, parametros, FilasEsperadas: 1);
    }

    private static ComandoSql CrearUpdate(Tabla tabla, IReadOnlyList<Columna> columnas, CambioDeFila cambio, IReadOnlyList<int> posicionesDeLaLlave)
    {
        var posicionesModificadas = Enumerable.Range(0, columnas.Count)
            .Where(posicion => EsEditable(columnas[posicion]) && !Equals(cambio.ValoresNuevos[posicion], cambio.ValoresOriginales[posicion]))
            .ToList();

        var asignaciones = posicionesModificadas
            .Select((posicion, indice) => (Texto: $"{Delimitar(columnas[posicion].Nombre)} = @v{indice}",
                Parametro: new ParametroSql($"@v{indice}", ConvertidorDeValoresDeEdicion.Convertir(cambio.ValoresNuevos[posicion], columnas[posicion]))))
            .ToList();

        var (condicion, parametrosDeLaLlave) = CrearCondicionPorLlave(columnas, cambio, posicionesDeLaLlave);
        var texto = $"UPDATE {Delimitar(tabla.Esquema, tabla.Nombre)} SET {string.Join(", ", asignaciones.Select(asignacion => asignacion.Texto))} WHERE {condicion};";

        return new ComandoSql(texto, [.. asignaciones.Select(asignacion => asignacion.Parametro), .. parametrosDeLaLlave], FilasEsperadas: 1);
    }

    private static ComandoSql CrearDelete(Tabla tabla, IReadOnlyList<Columna> columnas, CambioDeFila cambio, IReadOnlyList<int> posicionesDeLaLlave)
    {
        var (condicion, parametrosDeLaLlave) = CrearCondicionPorLlave(columnas, cambio, posicionesDeLaLlave);
        return new ComandoSql($"DELETE FROM {Delimitar(tabla.Esquema, tabla.Nombre)} WHERE {condicion};", parametrosDeLaLlave, FilasEsperadas: 1);
    }

    /// <summary>
    /// La fila se ubica por los valores originales de su llave primaria, aunque el usuario los haya editado.
    /// </summary>
    private static (string Condicion, List<ParametroSql> Parametros) CrearCondicionPorLlave(
        IReadOnlyList<Columna> columnas,
        CambioDeFila cambio,
        IReadOnlyList<int> posicionesDeLaLlave)
    {
        var parametros = posicionesDeLaLlave
            .Select((posicion, indice) => new ParametroSql($"@k{indice}", cambio.ValoresOriginales[posicion]))
            .ToList();

        var condicion = string.Join(" AND ", posicionesDeLaLlave.Select((posicion, indice) => $"{Delimitar(columnas[posicion].Nombre)} = @k{indice}"));
        return (condicion, parametros);
    }

    // Identidad y rowversion las asigna SQL Server; no se pueden escribir
    private static bool EsEditable(Columna columna) =>
        !columna.EsIdentidad && columna.TipoDeDato.Nombre is not ("timestamp" or "rowversion");

    private static string UnirNombres(IEnumerable<string> nombres) => string.Join(", ", nombres.Select(nombre => Delimitar(nombre)));

    private static string EscribirTexto(string texto) => $"N'{texto.Replace("'", "''", StringComparison.Ordinal)}'";

    private static string DefinirColumna(string nombre, TipoDeDato tipoDeDato, bool esIdentidad, bool admiteNulos)
    {
        var definicion = new StringBuilder($"{Delimitar(nombre)} {tipoDeDato.Describir()}");

        if (esIdentidad)
        {
            definicion.Append(" IDENTITY(1,1)");
        }

        definicion.Append(admiteNulos ? " NULL" : " NOT NULL");

        return definicion.ToString();
    }
}
