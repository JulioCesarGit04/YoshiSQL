using System.Text;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;
using static YoshiSQL.Infraestructura.SqlServer.DelimitadorDeIdentificadores;

namespace YoshiSQL.Infraestructura.SqlServer;

public sealed class GeneradorDeScriptsSqlServer : IGeneradorDeScripts
{
    private const string SangriaDeColumna = "    ";

    public string GenerarSeleccionDeFilas(string baseDeDatos, ObjetoDeEsquema objeto, int cantidadDeFilas) =>
        $"""
        USE {Delimitar(baseDeDatos)};
        GO

        SELECT TOP ({cantidadDeFilas}) *
        FROM {Delimitar(objeto.Esquema, objeto.Nombre)};
        """;

    public string GenerarCreacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        $"""
        CREATE DATABASE {Delimitar(nombreDeLaBaseDeDatos)};
        GO
        """;

    public string GenerarCreacionDeTabla(string baseDeDatos, Tabla tabla, IReadOnlyList<Columna> columnas)
    {
        var definiciones = columnas
            .OrderBy(columna => columna.Posicion)
            .Select(DefinirColumna)
            .ToList();

        var columnasDeLaLlave = columnas.Where(columna => columna.EsLlavePrimaria).ToList();

        if (columnasDeLaLlave.Count > 0)
        {
            var nombreDeLaLlave = Delimitar($"PK_{tabla.Nombre}");
            var listaDeColumnas = string.Join(", ", columnasDeLaLlave.Select(columna => Delimitar(columna.Nombre)));
            definiciones.Add($"CONSTRAINT {nombreDeLaLlave} PRIMARY KEY ({listaDeColumnas})");
        }

        var script = new StringBuilder()
            .AppendLine($"USE {Delimitar(baseDeDatos)};")
            .AppendLine("GO")
            .AppendLine()
            .AppendLine($"CREATE TABLE {Delimitar(tabla.Esquema, tabla.Nombre)}")
            .AppendLine("(")
            .AppendLine(string.Join($",{Environment.NewLine}", definiciones.Select(linea => SangriaDeColumna + linea)))
            .AppendLine(");")
            .Append("GO");

        return script.ToString();
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

    public string GenerarEliminacionDeBaseDeDatos(string nombreDeLaBaseDeDatos) =>
        $"""
        USE [master];
        GO

        ALTER DATABASE {Delimitar(nombreDeLaBaseDeDatos)} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE {Delimitar(nombreDeLaBaseDeDatos)};
        GO
        """;

    private static string DefinirColumna(Columna columna)
    {
        var definicion = new StringBuilder($"{Delimitar(columna.Nombre)} {columna.TipoDeDato.Describir()}");

        if (columna.EsIdentidad)
        {
            definicion.Append(" IDENTITY(1,1)");
        }

        definicion.Append(columna.AdmiteNulos ? " NULL" : " NOT NULL");

        return definicion.ToString();
    }
}
