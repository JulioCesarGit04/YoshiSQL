using System.Text.Encodings.Web;
using System.Text.Json;
using YoshiSQL.Dominio.Consultas;
using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Infraestructura.Exportacion;

/// <summary>
/// Arreglo JSON con un objeto por fila. Números, booleanos y nulos conservan su tipo.
/// </summary>
public sealed class ExportadorJson : IExportadorDeResultados
{
    private static readonly JsonWriterOptions OpcionesDeEscritura = new()
    {
        Indented = true,
        // Permite tildes y ñ legibles en el archivo en lugar de ñ
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public FormatoDeExportacion Formato => FormatoDeExportacion.Json;

    public string Extension => "json";

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, Stream destino, CancellationToken tokenDeCancelacion)
    {
        await using var escritor = new Utf8JsonWriter(destino, OpcionesDeEscritura);
        var nombresDePropiedades = CrearNombresUnicos(conjunto.Columnas);

        escritor.WriteStartArray();

        foreach (var fila in conjunto.Filas)
        {
            tokenDeCancelacion.ThrowIfCancellationRequested();
            escritor.WriteStartObject();

            for (var posicion = 0; posicion < fila.Length; posicion++)
            {
                escritor.WritePropertyName(nombresDePropiedades[posicion]);
                EscribirValor(escritor, fila[posicion]);
            }

            escritor.WriteEndObject();
        }

        escritor.WriteEndArray();
        await escritor.FlushAsync(tokenDeCancelacion);
    }

    private static void EscribirValor(Utf8JsonWriter escritor, object? valor)
    {
        switch (valor)
        {
            case null:
                escritor.WriteNullValue();
                break;
            case bool booleano:
                escritor.WriteBooleanValue(booleano);
                break;
            case byte or short or int or long:
                escritor.WriteNumberValue(Convert.ToInt64(valor));
                break;
            case decimal numeroDecimal:
                escritor.WriteNumberValue(numeroDecimal);
                break;
            case double or float when double.IsFinite(Convert.ToDouble(valor)):
                escritor.WriteNumberValue(Convert.ToDouble(valor));
                break;
            default:
                escritor.WriteStringValue(ConvertidorDeValoresATexto.Convertir(valor));
                break;
        }
    }

    /// <summary>
    /// Una consulta puede devolver dos columnas con el mismo nombre; en JSON se numeran (Id, Id_2).
    /// </summary>
    private static string[] CrearNombresUnicos(IReadOnlyList<ColumnaDeResultado> columnas)
    {
        var nombresUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return columnas.Select(columna =>
        {
            var nombre = columna.Nombre;

            for (var numero = 2; !nombresUsados.Add(nombre); numero++)
            {
                nombre = $"{columna.Nombre}_{numero}";
            }

            return nombre;
        }).ToArray();
    }
}
