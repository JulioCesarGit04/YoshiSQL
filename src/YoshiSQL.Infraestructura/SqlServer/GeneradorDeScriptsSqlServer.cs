using System.Text;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Diseno;
using YoshiSQL.Dominio.Edicion;
using YoshiSQL.Dominio.Errores;
using YoshiSQL.Dominio.Esquema;
using YoshiSQL.Dominio.Seguridad;
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

    public string GenerarCreacionDeTablaSinEnvolver(
        Tabla tabla,
        IReadOnlyList<Columna> columnas,
        Indice? llavePrimaria = null,
        IReadOnlyList<ColumnaCalculada>? columnasCalculadas = null,
        IReadOnlyList<IdentidadDeColumna>? identidades = null)
    {
        var formulas = (columnasCalculadas ?? []).ToDictionary(calculada => calculada.Columna, StringComparer.OrdinalIgnoreCase);
        var identidadesPorColumna = (identidades ?? []).ToDictionary(identidad => identidad.Columna, StringComparer.OrdinalIgnoreCase);

        var definicionesDeColumnas = columnas
            .OrderBy(columna => columna.Posicion)
            .Select(columna => formulas.TryGetValue(columna.Nombre, out var formula)
                ? DefinirColumnaCalculada(columna, formula)
                : DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos, identidadesPorColumna.GetValueOrDefault(columna.Nombre)))
            .ToList();

        return CrearInstruccionCreateTable(tabla, definicionesDeColumnas, DescribirLlavePrimaria(tabla, columnas, llavePrimaria));
    }

    // Una columna calculada se define solo con su fórmula: el tipo lo deduce SQL Server.
    // NOT NULL solo es válido (y necesario, si es parte de una llave) cuando está persistida.
    private static string DefinirColumnaCalculada(Columna columna, ColumnaCalculada formula)
    {
        var definicion = $"{Delimitar(columna.Nombre)} AS {formula.Definicion}";

        if (!formula.EsPersistida)
        {
            return definicion;
        }

        return columna.AdmiteNulos ? $"{definicion} PERSISTED" : $"{definicion} PERSISTED NOT NULL";
    }

    public string GenerarCreacionDeTipoDeTabla(TipoDeTabla tipo)
    {
        var lineas = tipo.Columnas
            .OrderBy(columna => columna.Posicion)
            .Select(columna => DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos))
            .ToList();

        var columnasDeLaLlave = tipo.Columnas
            .Where(columna => columna.EsLlavePrimaria)
            .OrderBy(columna => columna.Posicion)
            .Select(columna => columna.Nombre)
            .ToList();

        if (columnasDeLaLlave.Count > 0)
        {
            lineas.Add($"PRIMARY KEY ({UnirNombres(columnasDeLaLlave)})");
        }

        return new StringBuilder()
            .AppendLine($"IF TYPE_ID({EscribirTexto(Delimitar(tipo.Esquema, tipo.Nombre))}) IS NULL")
            .AppendLine($"    CREATE TYPE {Delimitar(tipo.Esquema, tipo.Nombre)} AS TABLE")
            .AppendLine("    (")
            .AppendLine(string.Join($",{Environment.NewLine}", lineas.Select(linea => SangriaDeColumna + SangriaDeColumna + linea)))
            .Append("    );")
            .ToString();
    }

    public string GenerarCreacionDeSecuencia(Secuencia secuencia)
    {
        var cache = secuencia switch
        {
            { UsaCache: false } => "NO CACHE",
            { TamanoDeCache: int tamano } => $"CACHE {tamano}",
            _ => "CACHE"
        };

        // Solo si no existe: así el script también sirve para completar una base que ya la tiene
        return $"IF OBJECT_ID({EscribirTexto(Delimitar(secuencia.Esquema, secuencia.Nombre))}, N'SO') IS NULL{Environment.NewLine}"
            + $"    CREATE SEQUENCE {Delimitar(secuencia.Esquema, secuencia.Nombre)} AS {secuencia.TipoDeDato} "
            + $"START WITH {secuencia.SiguienteValor} INCREMENT BY {secuencia.Incremento} "
            + $"MINVALUE {secuencia.ValorMinimo} MAXVALUE {secuencia.ValorMaximo} "
            + $"{(secuencia.EsCiclica ? "CYCLE" : "NO CYCLE")} {cache};";
    }

    public string GenerarDeshabilitacionDeDisparadorDeBaseDeDatos(DisparadorDeBaseDeDatos disparador) =>
        $"DISABLE TRIGGER {Delimitar(disparador.Nombre)} ON DATABASE;";

    public string GenerarPropiedadExtendida(PropiedadExtendida propiedad)
    {
        var instruccion = new StringBuilder("EXEC sys.sp_addextendedproperty ")
            .Append($"@name = {EscribirTexto(propiedad.Nombre)}, @value = {EscribirTexto(propiedad.Valor)}, ")
            .Append($"@level0type = N'SCHEMA', @level0name = {EscribirTexto(propiedad.Esquema)}, ")
            .Append($"@level1type = N'{propiedad.TipoDelObjeto}', @level1name = {EscribirTexto(propiedad.Objeto)}");

        if (propiedad.Columna is not null)
        {
            instruccion.Append($", @level2type = N'COLUMN', @level2name = {EscribirTexto(propiedad.Columna)}");
        }

        return instruccion.Append(';').ToString();
    }

    public string GenerarCreacionDeSinonimo(Sinonimo sinonimo) =>
        $"IF OBJECT_ID({EscribirTexto(Delimitar(sinonimo.Esquema, sinonimo.Nombre))}, N'SN') IS NULL{Environment.NewLine}"
        + $"    CREATE SYNONYM {Delimitar(sinonimo.Esquema, sinonimo.Nombre)} FOR {sinonimo.ObjetoBase};";

    public string GenerarCreacionDeIndice(IndiceDeTabla indice)
    {
        var tabla = Delimitar(indice.Tabla.Esquema, indice.Tabla.Nombre);
        var agrupado = indice.EsAgrupado ? "CLUSTERED" : "NONCLUSTERED";

        if (indice.EsColumnar)
        {
            return CrearIndiceColumnar(indice, tabla, agrupado);
        }
        var columnas = string.Join(", ", indice.Columnas.Select(columna => columna.EsDescendente ? $"{Delimitar(columna.Nombre)} DESC" : Delimitar(columna.Nombre)));

        if (indice.EsRestriccionUnica)
        {
            return $"ALTER TABLE {tabla} ADD CONSTRAINT {Delimitar(indice.Nombre)} UNIQUE {agrupado} ({columnas});";
        }

        var instruccion = new StringBuilder($"CREATE {(indice.EsUnico ? "UNIQUE " : string.Empty)}{agrupado} INDEX {Delimitar(indice.Nombre)} ON {tabla} ({columnas})");

        if (indice.ColumnasIncluidas.Count > 0)
        {
            instruccion.Append($" INCLUDE ({UnirNombres(indice.ColumnasIncluidas)})");
        }

        if (!string.IsNullOrWhiteSpace(indice.Filtro))
        {
            instruccion.Append($" WHERE {indice.Filtro}");
        }

        return instruccion.Append(';').ToString();
    }

    private static string CrearIndiceColumnar(IndiceDeTabla indice, string tabla, string agrupado)
    {
        var instruccion = new StringBuilder($"CREATE {agrupado} COLUMNSTORE INDEX {Delimitar(indice.Nombre)} ON {tabla}");

        // El agrupado abarca toda la tabla y no admite lista de columnas
        if (!indice.EsAgrupado && indice.ColumnasIncluidas.Count > 0)
        {
            instruccion.Append($" ({UnirNombres(indice.ColumnasIncluidas)})");
        }

        if (!string.IsNullOrWhiteSpace(indice.Filtro))
        {
            instruccion.Append($" WHERE {indice.Filtro}");
        }

        return instruccion.Append(';').ToString();
    }

    public string GenerarCreacionDeUsuarioExistente(UsuarioDeBaseDeDatos usuario)
    {
        var nombre = Delimitar(usuario.Nombre);
        var sinInicioDeSesion = $"CREATE USER {nombre} WITHOUT LOGIN;";
        var condicion = $"IF DATABASE_PRINCIPAL_ID({EscribirTexto(usuario.Nombre)}) IS NULL";

        if (string.IsNullOrWhiteSpace(usuario.InicioDeSesion))
        {
            return $"{condicion}{Environment.NewLine}    {sinInicioDeSesion}";
        }

        return string.Join(
            Environment.NewLine,
            condicion,
            "BEGIN",
            $"    IF SUSER_ID({EscribirTexto(usuario.InicioDeSesion)}) IS NOT NULL",
            $"        CREATE USER {nombre} FOR LOGIN {Delimitar(usuario.InicioDeSesion)};",
            "    ELSE",
            $"        {sinInicioDeSesion}",
            "END;");
    }

    public string GenerarCreacionDeRol(RolDeBaseDeDatos rol) =>
        $"IF DATABASE_PRINCIPAL_ID({EscribirTexto(rol.Nombre)}) IS NULL{Environment.NewLine}    CREATE ROLE {Delimitar(rol.Nombre)};";

    public string GenerarMiembroDeRol(MiembroDeRol miembro) =>
        $"ALTER ROLE {Delimitar(miembro.Rol)} ADD MEMBER {Delimitar(miembro.Miembro)};";

    public string GenerarPermiso(PermisoDeBaseDeDatos permiso)
    {
        var instruccion = new StringBuilder(permiso.EsDenegacion ? "DENY " : "GRANT ").Append(permiso.Permiso);

        if (permiso.Objeto is not null && permiso.Esquema is not null)
        {
            instruccion.Append($" ON OBJECT::{Delimitar(permiso.Esquema, permiso.Objeto)}");

            if (permiso.Columna is not null)
            {
                instruccion.Append($" ({Delimitar(permiso.Columna)})");
            }
        }
        else if (permiso.Esquema is not null)
        {
            instruccion.Append($" ON SCHEMA::{Delimitar(permiso.Esquema)}");
        }

        instruccion.Append($" TO {Delimitar(permiso.Beneficiario)}");

        if (permiso.PuedeOtorgarlo)
        {
            instruccion.Append(" WITH GRANT OPTION");
        }

        return instruccion.Append(';').ToString();
    }

    public string GenerarValorPredeterminado(ValorPredeterminado valor)
    {
        var restriccion = valor.Nombre is null ? string.Empty : $"CONSTRAINT {Delimitar(valor.Nombre)} ";
        return $"ALTER TABLE {Delimitar(valor.Tabla.Esquema, valor.Tabla.Nombre)} ADD {restriccion}DEFAULT {valor.Definicion} FOR {Delimitar(valor.Columna)};";
    }

    public string GenerarRestriccionDeVerificacion(RestriccionDeVerificacion restriccion)
    {
        var tabla = Delimitar(restriccion.Tabla.Esquema, restriccion.Tabla.Nombre);
        var nombre = restriccion.Nombre is null ? string.Empty : $"CONSTRAINT {Delimitar(restriccion.Nombre)} ";

        // Una restricción deshabilitada puede tener filas que no la cumplen: se crea sin validar y se deja apagada.
        // Sin nombre no se puede apagar después; ese caso (rarísimo) se crea sin validar las filas existentes.
        if (!restriccion.EstaDeshabilitada)
        {
            return $"ALTER TABLE {tabla} WITH CHECK ADD {nombre}CHECK {restriccion.Definicion};";
        }

        var creacion = $"ALTER TABLE {tabla} WITH NOCHECK ADD {nombre}CHECK {restriccion.Definicion};";

        return restriccion.Nombre is null
            ? creacion
            : $"{creacion}{Environment.NewLine}ALTER TABLE {tabla} NOCHECK CONSTRAINT {Delimitar(restriccion.Nombre)};";
    }

    public string GenerarDeshabilitacionDeDisparador(DisparadorDeTabla disparador) =>
        $"DISABLE TRIGGER {Delimitar(disparador.Esquema, disparador.Nombre)} ON {Delimitar(disparador.EsquemaDelObjeto, disparador.NombreDelObjeto)};";

    // CREATE SCHEMA debe ser la única instrucción de su lote; dentro de EXEC se puede condicionar a que no exista
    public string GenerarCreacionDeEsquema(string esquema) =>
        $"IF SCHEMA_ID({EscribirTexto(esquema)}) IS NULL EXEC({EscribirTexto($"CREATE SCHEMA {Delimitar(esquema)}")});";

    public string GenerarCreacionDeTipo(TipoDeUsuario tipo) =>
        $"IF TYPE_ID({EscribirTexto($"{Delimitar(tipo.Esquema)}.{Delimitar(tipo.Nombre)}")}) IS NULL{Environment.NewLine}"
        + $"    CREATE TYPE {Delimitar(tipo.Esquema, tipo.Nombre)} FROM {tipo.TipoBase.Describir()} {(tipo.AdmiteNulos ? "NULL" : "NOT NULL")};";

    /// <summary>
    /// Con la PK real se conservan su nombre (los nombres de restricción son únicos por base: "PK_Tabla" chocaba
    /// entre dbo.Clientes y ventas.Clientes), el orden de sus columnas y si es agrupada.
    /// </summary>
    private static LlavePrimariaParaCrear? DescribirLlavePrimaria(Tabla tabla, IReadOnlyList<Columna> columnas, Indice? llavePrimaria)
    {
        if (llavePrimaria is not null)
        {
            return new LlavePrimariaParaCrear(llavePrimaria.Nombre, llavePrimaria.Columnas, llavePrimaria.EsAgrupado);
        }

        var columnasDeLaLlave = columnas
            .Where(columna => columna.EsLlavePrimaria)
            .OrderBy(columna => columna.Posicion)
            .Select(columna => columna.Nombre)
            .ToList();

        return columnasDeLaLlave.Count == 0 ? null : new LlavePrimariaParaCrear($"PK_{tabla.Nombre}", columnasDeLaLlave, EsAgrupada: null);
    }

    public string GenerarLlaveForanea(LlaveForanea llave) =>
        $"ALTER TABLE {Delimitar(llave.TablaOrigen.Esquema, llave.TablaOrigen.Nombre)} "
        + $"ADD CONSTRAINT {Delimitar(llave.Nombre)} FOREIGN KEY ({UnirNombres(llave.ColumnasOrigen)}) "
        + $"REFERENCES {Delimitar(llave.TablaDestino.Esquema, llave.TablaDestino.Nombre)} ({UnirNombres(llave.ColumnasDestino)})"
        + $"{DescribirAccionReferencial("ON DELETE", llave.AlEliminar)}{DescribirAccionReferencial("ON UPDATE", llave.AlActualizar)};";

    // Sin esto, una llave con ON DELETE CASCADE se recreaba sin la cascada y borrar un padre empezaba a fallar.
    // El catálogo escribe las acciones con guion bajo (SET_NULL); NO_ACTION es la predeterminada y no se escribe.
    private static string DescribirAccionReferencial(string evento, string accion) =>
        string.IsNullOrEmpty(accion) || accion.Equals(LlaveForanea.SinAccion, StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : $" {evento} {accion.Replace('_', ' ')}";

    public string GenerarInsertDeFilas(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<object?[]> filas)
    {
        if (filas.Count == 0)
        {
            return string.Empty;
        }

        var ordenadas = columnas.OrderBy(columna => columna.Posicion).ToList();

        // SQL Server rechaza todo el INSERT si incluye una columna calculada o rowversion: se omiten
        var posicionesInsertables = Enumerable.Range(0, ordenadas.Count)
            .Where(posicion => !ordenadas[posicion].EsCalculada && !EsRowversion(ordenadas[posicion]))
            .ToList();

        var nombreDeLaTabla = Delimitar(tabla.Esquema, tabla.Nombre);
        var listaDeColumnas = UnirNombres(posicionesInsertables.Select(posicion => ordenadas[posicion].Nombre));
        var tieneIdentidad = ordenadas.Any(columna => columna.EsIdentidad);

        var texto = new StringBuilder();

        if (tieneIdentidad)
        {
            texto.AppendLine($"SET IDENTITY_INSERT {nombreDeLaTabla} ON;");
        }

        foreach (var fila in filas)
        {
            var valores = string.Join(", ", posicionesInsertables.Select(posicion => EscribirLiteralDeColumna(fila[posicion], ordenadas[posicion])));
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

    // Fechas en ISO 8601 con "T": sin la T, una columna datetime con idioma español (DATEFORMAT dmy)
    // lee "2026-01-05" como año-día-mes y guarda otra fecha o falla con "fuera de rango"
    // Formatos con los 7 decimales de segundo de datetime2 y datetimeoffset. Solo se usan en columnas de esos tipos:
    // una columna datetime rechaza un texto con más de 3 decimales.
    private const string FormatoDeFechaPrecisa = "yyyy-MM-dd'T'HH:mm:ss.fffffff";
    private const string FormatoDeFechaConZonaPrecisa = "yyyy-MM-dd'T'HH:mm:ss.fffffffzzz";

    /// <summary>
    /// Igual que <see cref="EscribirLiteralSql"/>, pero conociendo la columna: antes una columna datetime2(7)
    /// se exportaba con 3 decimales y la copia perdía las fracciones de milisegundo.
    /// </summary>
    private static string EscribirLiteralDeColumna(object? valor, Columna columna) => (valor, columna.TipoDeDato.Nombre.ToLowerInvariant()) switch
    {
        (DateTime fecha, "datetime2") => $"'{fecha.ToString(FormatoDeFechaPrecisa, System.Globalization.CultureInfo.InvariantCulture)}'",
        (DateTimeOffset fecha, "datetimeoffset") => $"'{fecha.ToString(FormatoDeFechaConZonaPrecisa, System.Globalization.CultureInfo.InvariantCulture)}'",
        _ => EscribirLiteralSql(valor)
    };

    private static string EscribirLiteralSql(object? valor) => valor switch
    {
        null => "NULL",
        bool booleano => booleano ? "1" : "0",
        byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
        DateTime fecha => $"'{fecha.ToString("yyyy-MM-dd'T'HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture)}'",
        DateTimeOffset fecha => $"'{fecha.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", System.Globalization.CultureInfo.InvariantCulture)}'",
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

    public string GenerarCreacionDeTabla(string baseDeDatos, Tabla tabla, IReadOnlyList<Columna> columnas, Indice? llavePrimaria = null) =>
        new StringBuilder()
            .AppendLine($"USE {Delimitar(baseDeDatos)};")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine(GenerarCreacionDeTablaSinEnvolver(tabla, columnas, llavePrimaria))
            .Append("GO")
            .ToString();

    public string GenerarCambiosDeTabla(string baseDeDatos, DefinicionDeTabla? original, DefinicionDeTabla nueva)
    {
        var instrucciones = original is null
            ? [CrearInstruccionCreateTable(
                nueva.Tabla,
                nueva.Columnas.Select(DefinirColumnaConValorPredeterminado).ToList(),
                nueva.ColumnasDeLaLlavePrimaria.Any()
                    ? new LlavePrimariaParaCrear($"PK_{nueva.Nombre}", nueva.ColumnasDeLaLlavePrimaria.Select(columna => columna.Nombre).ToList(), EsAgrupada: null)
                    : null)]
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

        // En un lote aparte para que se ejecute aunque el anterior falle: si no, la pestaña donde se abra el script
        // se quedaría con XACT_ABORT activo y las consultas siguientes se comportarían distinto
        return script
            .AppendLine("COMMIT TRANSACTION;")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine("SET XACT_ABORT OFF;")
            .Append("GO")
            .ToString();
    }

    public IReadOnlyList<ComandoSql> GenerarComandosDeEdicion(Tabla tabla, IReadOnlyList<Columna> columnas, IReadOnlyList<CambioDeFila> cambios)
    {
        var posicionesDeLaLlave = Enumerable.Range(0, columnas.Count).Where(posicion => columnas[posicion].EsLlavePrimaria).ToList();

        if (posicionesDeLaLlave.Count == 0)
        {
            throw new ErrorDeYoshiSql($"La tabla {tabla.NombreCompleto} no tiene llave primaria; no se puede identificar cada fila de forma segura.");
        }

        // Una fila editada y devuelta a su valor original sigue "modificada" pero no cambia nada:
        // generaría "UPDATE ... SET  WHERE" (SQL inválido) y la transacción descartaría todos los demás cambios
        return cambios
            .Where(cambio => cambio.Tipo != EstadoDeFila.Modificada || ObtenerPosicionesModificadas(columnas, cambio).Count > 0)
            .Select(cambio => cambio.Tipo switch
            {
                EstadoDeFila.Nueva => CrearInsert(tabla, columnas, cambio),
                EstadoDeFila.Modificada => CrearUpdate(tabla, columnas, cambio, posicionesDeLaLlave),
                EstadoDeFila.Eliminada => CrearDelete(tabla, columnas, cambio, posicionesDeLaLlave),
                _ => throw new ArgumentOutOfRangeException(nameof(cambios), cambio.Tipo, "Una fila sin cambios no genera instrucciones.")
            })
            .ToList();
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

    public string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos, OpcionesDeEliminacionDeBaseDeDatos opciones)
    {
        var nombre = Delimitar(nombreDeLaBaseDeDatos);
        var script = new StringBuilder();

        script.AppendLine("USE [master];");
        script.AppendLine("GO");
        script.AppendLine();

        if (opciones.EliminarHistorialDeRespaldos)
        {
            script.AppendLine($"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = {EscribirTexto(nombreDeLaBaseDeDatos)};");
            script.AppendLine("GO");
            script.AppendLine();
        }

        if (opciones.CerrarConexionesExistentes)
        {
            // Si el DROP falla después de SINGLE_USER (otra sesión tomó el único lugar), la base quedaría
            // inaccesible para todos: por eso se devuelve a MULTI_USER antes de informar el error
            script.AppendLine(
                $"""
                BEGIN TRY
                    ALTER DATABASE {nombre} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE {nombre};
                END TRY
                BEGIN CATCH
                    IF DB_ID({EscribirTexto(nombreDeLaBaseDeDatos)}) IS NOT NULL
                        ALTER DATABASE {nombre} SET MULTI_USER;
                    THROW;
                END CATCH;
                """);
        }
        else
        {
            script.AppendLine($"DROP DATABASE {nombre};");
        }

        script.Append("GO");
        return script.ToString();
    }

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

    private static string CrearInstruccionCreateTable(Tabla tabla, IReadOnlyList<string> definicionesDeColumnas, LlavePrimariaParaCrear? llavePrimaria)
    {
        var lineas = definicionesDeColumnas.ToList();

        if (llavePrimaria is not null)
        {
            var tipoDeIndice = llavePrimaria.EsAgrupada switch
            {
                true => " CLUSTERED",
                false => " NONCLUSTERED",
                null => string.Empty
            };

            lineas.Add($"CONSTRAINT {Delimitar(llavePrimaria.Nombre)} PRIMARY KEY{tipoDeIndice} ({UnirNombres(llavePrimaria.Columnas)})");
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

        // SQL Server no deja eliminar una columna mientras tenga un valor predeterminado: primero se quita.
        // Su nombre no se conoce de antemano (suele ser DF__Tabla__Col__1A2B3C), así que se busca al ejecutar.
        instrucciones.AddRange(cambios.ColumnasEliminadas.Select((columna, indice) =>
            CrearEliminacionDeValorPredeterminado(nueva.Tabla, columna.Nombre, indice)
            + Environment.NewLine
            + $"ALTER TABLE {nombreDeLaTabla} DROP COLUMN {Delimitar(columna.Nombre)};"));

        instrucciones.AddRange(cambios.ColumnasRenombradas.Select(columna =>
            $"EXEC sp_rename {EscribirTexto($"{nombreDeLaTabla}.{Delimitar(columna.NombreOriginal!)}")}, {EscribirTexto(columna.Nombre)}, N'COLUMN';"));

        instrucciones.AddRange(cambios.ColumnasModificadas.Select(columna =>
            $"ALTER TABLE {nombreDeLaTabla} ALTER COLUMN {Delimitar(columna.Nombre)} {columna.TipoDeDato.Describir()} {(columna.AdmiteNulos ? "NULL" : "NOT NULL")};"));

        instrucciones.AddRange(cambios.ColumnasNuevas.Select(columna => CrearAdicionDeColumna(nombreDeLaTabla, columna)));

        // Cambiar un valor predeterminado es quitar la restricción anterior (si había) y crear otra.
        // Va después de renombrar, así que se usa el nombre nuevo de la columna.
        instrucciones.AddRange(cambios.ColumnasConOtroValorPredeterminado.Select((columna, indice) =>
            CrearCambioDeValorPredeterminado(nueva.Tabla, columna, cambios.ColumnasEliminadas.Count + indice)));

        var columnasDeLaLlave = nueva.ColumnasDeLaLlavePrimaria.Select(columna => columna.Nombre).ToList();

        if (cambios.CambiaLaLlavePrimaria && columnasDeLaLlave.Count > 0)
        {
            var nombreDeLaLlave = original.NombreDeLaLlavePrimaria ?? $"PK_{nueva.Nombre}";
            instrucciones.Add($"ALTER TABLE {nombreDeLaTabla} ADD CONSTRAINT {Delimitar(nombreDeLaLlave)} PRIMARY KEY ({UnirNombres(columnasDeLaLlave)});");
        }

        return instrucciones.Count > 0 ? instrucciones : ["-- No hay cambios que aplicar."];
    }

    private static string DefinirColumnaConValorPredeterminado(DefinicionDeColumna columna)
    {
        var definicion = DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos);
        return columna.TieneValorPredeterminado ? $"{definicion} DEFAULT {EntreParentesis(columna.ValorPredeterminado!)}" : definicion;
    }

    private static string CrearCambioDeValorPredeterminado(Tabla tabla, DefinicionDeColumna columna, int indice)
    {
        var eliminacion = CrearEliminacionDeValorPredeterminado(tabla, columna.Nombre, indice);

        return columna.TieneValorPredeterminado
            ? eliminacion + Environment.NewLine
                + $"ALTER TABLE {Delimitar(tabla.Esquema, tabla.Nombre)} ADD DEFAULT {EntreParentesis(columna.ValorPredeterminado!)} FOR {Delimitar(columna.Nombre)};"
            : eliminacion;
    }

    // El usuario escribe "0" o "getdate()"; SQL Server lo guarda como "((0))". Si ya viene entre paréntesis se deja igual.
    private static string EntreParentesis(string expresion)
    {
        var texto = expresion.Trim();
        return texto.StartsWith('(') && texto.EndsWith(')') ? texto : $"({texto})";
    }

    private static string CrearEliminacionDeValorPredeterminado(Tabla tabla, string columna, int indice)
    {
        var nombreDeLaTabla = Delimitar(tabla.Esquema, tabla.Nombre);
        var variable = $"@quitarPredeterminado{indice + 1}";

        var nombreDentroDeTexto = nombreDeLaTabla.Replace("'", "''", StringComparison.Ordinal);

        return string.Join(
            Environment.NewLine,
            $"DECLARE {variable} nvarchar(max) = N'';",
            $"SELECT {variable} += N'ALTER TABLE {nombreDentroDeTexto} DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'",
            "FROM sys.default_constraints AS dc",
            "INNER JOIN sys.columns AS c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id",
            $"WHERE dc.parent_object_id = OBJECT_ID({EscribirTexto(nombreDeLaTabla)}) AND c.name = {EscribirTexto(columna)};",
            $"EXEC sys.sp_executesql {variable};");
    }

    /// <summary>
    /// Una columna NOT NULL no se puede agregar a una tabla que ya tiene filas: SQL Server no sabe qué valor ponerles.
    /// Se agrega con un valor inicial (0, texto vacío...) solo para las filas existentes, y se quita enseguida
    /// para que la columna quede sin valor predeterminado, tal como se diseñó.
    /// </summary>
    private static string CrearAdicionDeColumna(string nombreDeLaTabla, DefinicionDeColumna columna)
    {
        // Con un valor predeterminado propio, las filas existentes lo reciben y no hace falta uno temporal
        if (columna.TieneValorPredeterminado)
        {
            return $"ALTER TABLE {nombreDeLaTabla} ADD {DefinirColumnaConValorPredeterminado(columna)};";
        }

        var definicion = DefinirColumna(columna.Nombre, columna.TipoDeDato, columna.EsIdentidad, columna.AdmiteNulos);
        var valorInicial = columna.AdmiteNulos || columna.EsIdentidad ? null : ObtenerValorInicial(columna.TipoDeDato);

        if (valorInicial is null)
        {
            return $"ALTER TABLE {nombreDeLaTabla} ADD {definicion};";
        }

        var restriccionTemporal = Delimitar($"DF_YoshiSQL_Temporal_{columna.Nombre}");

        return $"-- Las filas que ya existen reciben {valorInicial} en la columna nueva"
            + Environment.NewLine
            + $"ALTER TABLE {nombreDeLaTabla} ADD {definicion} CONSTRAINT {restriccionTemporal} DEFAULT ({valorInicial});"
            + Environment.NewLine
            + $"ALTER TABLE {nombreDeLaTabla} DROP CONSTRAINT {restriccionTemporal};";
    }

    /// <returns>El valor "vacío" del tipo, o nulo si no hay uno razonable (xml, tipos de usuario...).</returns>
    private static string? ObtenerValorInicial(TipoDeDato tipo) => tipo.Nombre.ToLowerInvariant() switch
    {
        "int" or "bigint" or "smallint" or "tinyint" or "bit" or "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "0",
        "char" or "varchar" or "nchar" or "nvarchar" or "text" or "ntext" => "''",
        "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "'19000101'",
        "time" => "'00:00:00'",
        "uniqueidentifier" => "'00000000-0000-0000-0000-000000000000'",
        "binary" or "varbinary" => "0x",
        _ => null
    };

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
            .Select((posicion, indice) => CrearParametro($"@v{indice}", ConvertidorDeValoresDeEdicion.Convertir(cambio.ValoresNuevos[posicion], columnas[posicion]), columnas[posicion]))
            .ToList();

        var texto = $"INSERT INTO {Delimitar(tabla.Esquema, tabla.Nombre)} ({UnirNombres(posiciones.Select(posicion => columnas[posicion].Nombre))}) "
            + $"VALUES ({string.Join(", ", parametros.Select(parametro => parametro.Nombre))});";

        return new ComandoSql(texto, parametros, FilasEsperadas: 1);
    }

    private static ComandoSql CrearUpdate(Tabla tabla, IReadOnlyList<Columna> columnas, CambioDeFila cambio, IReadOnlyList<int> posicionesDeLaLlave)
    {
        var asignaciones = ObtenerPosicionesModificadas(columnas, cambio)
            .Select((posicion, indice) => (Texto: $"{Delimitar(columnas[posicion].Nombre)} = @v{indice}",
                Parametro: CrearParametro($"@v{indice}", ConvertidorDeValoresDeEdicion.Convertir(cambio.ValoresNuevos[posicion], columnas[posicion]), columnas[posicion])))
            .ToList();

        var (condicion, parametrosDeLaLlave) = CrearCondicionPorLlave(columnas, cambio, posicionesDeLaLlave);
        var (condicionDeOriginales, parametrosDeOriginales) = CrearCondicionDeValoresOriginales(columnas, cambio, posicionesDeLaLlave);
        var texto = $"UPDATE {Delimitar(tabla.Esquema, tabla.Nombre)} SET {string.Join(", ", asignaciones.Select(asignacion => asignacion.Texto))} WHERE {condicion}{condicionDeOriginales};";

        return new ComandoSql(
            texto,
            [.. asignaciones.Select(asignacion => asignacion.Parametro), .. parametrosDeLaLlave, .. parametrosDeOriginales],
            FilasEsperadas: 1);
    }

    /// <summary>
    /// Además de la llave, se exige que las columnas que se van a cambiar sigan teniendo el valor que el usuario vio.
    /// Si otra persona las modificó mientras tanto, el UPDATE no encuentra la fila y se avisa, en vez de pisar su cambio.
    /// </summary>
    private static (string Condicion, List<ParametroSql> Parametros) CrearCondicionDeValoresOriginales(
        IReadOnlyList<Columna> columnas,
        CambioDeFila cambio,
        IReadOnlyList<int> posicionesDeLaLlave)
    {
        var condicion = new StringBuilder();
        var parametros = new List<ParametroSql>();

        foreach (var posicion in ObtenerPosicionesModificadas(columnas, cambio))
        {
            // La llave ya está en la condición, y hay tipos que SQL Server no deja comparar con "=" (xml, text, geography...)
            if (posicionesDeLaLlave.Contains(posicion) || !TiposQueSePuedenComparar.Contains(columnas[posicion].TipoDeDato.Nombre))
            {
                continue;
            }

            var nombre = Delimitar(columnas[posicion].Nombre);

            if (cambio.ValoresOriginales[posicion] is null)
            {
                condicion.Append($" AND {nombre} IS NULL");
                continue;
            }

            var parametro = CrearParametro($"@o{parametros.Count}", cambio.ValoresOriginales[posicion], columnas[posicion]);
            condicion.Append($" AND {nombre} = {parametro.Nombre}");
            parametros.Add(parametro);
        }

        return (condicion.ToString(), parametros);
    }

    private static readonly HashSet<string> TiposQueSePuedenComparar = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "bigint", "smallint", "tinyint", "bit", "decimal", "numeric", "money", "smallmoney", "float", "real",
        "char", "varchar", "nchar", "nvarchar", "date", "datetime", "datetime2", "smalldatetime", "datetimeoffset", "time",
        "uniqueidentifier", "binary", "varbinary"
    };

    private static ParametroSql CrearParametro(string nombre, object? valor, Columna columna) =>
        new(nombre, valor, columna.TipoDeDato.Nombre);

    private static List<int> ObtenerPosicionesModificadas(IReadOnlyList<Columna> columnas, CambioDeFila cambio) =>
        Enumerable.Range(0, columnas.Count)
            .Where(posicion => EsEditable(columnas[posicion]) && !Equals(cambio.ValoresNuevos[posicion], cambio.ValoresOriginales[posicion]))
            .ToList();

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
            .Select((posicion, indice) => CrearParametro($"@k{indice}", cambio.ValoresOriginales[posicion], columnas[posicion]))
            .ToList();

        var condicion = string.Join(" AND ", posicionesDeLaLlave.Select((posicion, indice) => $"{Delimitar(columnas[posicion].Nombre)} = @k{indice}"));
        return (condicion, parametros);
    }

    // Identidad, rowversion y columnas calculadas las asigna SQL Server; no se pueden escribir
    private static bool EsEditable(Columna columna) =>
        !columna.EsIdentidad && !columna.EsCalculada && !EsRowversion(columna);

    private static bool EsRowversion(Columna columna) => columna.TipoDeDato.Nombre is "timestamp" or "rowversion";

    private static string UnirNombres(IEnumerable<string> nombres) => string.Join(", ", nombres.Select(nombre => Delimitar(nombre)));

    /// <param name="EsAgrupada">Nulo si no se conoce: SQL Server la crea agrupada por defecto.</param>
    private sealed record LlavePrimariaParaCrear(string Nombre, IReadOnlyList<string> Columnas, bool? EsAgrupada);

    private static string EscribirTexto(string texto) => $"N'{texto.Replace("'", "''", StringComparison.Ordinal)}'";

    /// <param name="identidad">Semilla e incremento reales; si no se conocen se usa IDENTITY(1,1).</param>
    private static string DefinirColumna(string nombre, TipoDeDato tipoDeDato, bool esIdentidad, bool admiteNulos, IdentidadDeColumna? identidad = null)
    {
        var definicion = new StringBuilder($"{Delimitar(nombre)} {tipoDeDato.Describir()}");

        if (esIdentidad)
        {
            definicion.Append(identidad is null ? " IDENTITY(1,1)" : $" IDENTITY({identidad.Semilla},{identidad.Incremento})");
        }

        definicion.Append(admiteNulos ? " NULL" : " NOT NULL");

        return definicion.ToString();
    }
}
