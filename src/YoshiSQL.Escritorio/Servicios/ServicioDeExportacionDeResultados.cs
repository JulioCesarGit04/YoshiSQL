using YoshiSQL.Aplicacion.Consultas;
using YoshiSQL.Aplicacion.Errores;
using YoshiSQL.Dominio.Consultas;

namespace YoshiSQL.Escritorio.Servicios;

public sealed class ServicioDeExportacionDeResultados : IServicioDeExportacionDeResultados
{
    private const string NombreSugeridoDelArchivo = "Resultados";

    private readonly ServicioDeExportacion _servicioDeExportacion;
    private readonly IServicioDeDialogos _servicioDeDialogos;
    private readonly IServicioDelSistemaOperativo _sistemaOperativo;
    private readonly IServicioDeErrores _servicioDeErrores;

    public ServicioDeExportacionDeResultados(
        ServicioDeExportacion servicioDeExportacion,
        IServicioDeDialogos servicioDeDialogos,
        IServicioDelSistemaOperativo sistemaOperativo,
        IServicioDeErrores servicioDeErrores)
    {
        _servicioDeExportacion = servicioDeExportacion;
        _servicioDeDialogos = servicioDeDialogos;
        _sistemaOperativo = sistemaOperativo;
        _servicioDeErrores = servicioDeErrores;
    }

    public async Task ExportarAsync(ConjuntoDeResultados conjunto, FormatoDeExportacion formato)
    {
        try
        {
            var tipoDeArchivo = new TipoDeArchivo(DescribirFormato(formato), _servicioDeExportacion.ObtenerExtension(formato));
            var rutaDelArchivo = await _servicioDeDialogos.SeleccionarArchivoParaGuardarAsync(NombreSugeridoDelArchivo, tipoDeArchivo);

            if (rutaDelArchivo is not null)
            {
                await _servicioDeExportacion.ExportarAsync(conjunto, formato, rutaDelArchivo, CancellationToken.None);
            }
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError($"Exportar resultados a {DescribirFormato(formato)}"));
        }
    }

    public async Task CopiarAlPortapapelesAsync(ConjuntoDeResultados conjunto)
    {
        try
        {
            var texto = await _servicioDeExportacion.CrearTextoParaCopiarAsync(conjunto, CancellationToken.None);
            await _sistemaOperativo.CopiarAlPortapapelesAsync(texto);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Copiar resultados"));
        }
    }

    public async Task CopiarComoInsertAsync(ConjuntoDeResultados conjunto)
    {
        try
        {
            var texto = await _servicioDeExportacion.CrearTextoAsync(conjunto, FormatoDeExportacion.Insert, CancellationToken.None);
            await _sistemaOperativo.CopiarAlPortapapelesAsync(texto);
        }
        catch (Exception error)
        {
            await _servicioDeErrores.RegistrarYMostrarAsync(error, new ContextoDeError("Copiar resultados como INSERT"));
        }
    }

    private static string DescribirFormato(FormatoDeExportacion formato) => formato switch
    {
        FormatoDeExportacion.Csv => "CSV",
        FormatoDeExportacion.Json => "JSON",
        FormatoDeExportacion.Excel => "Excel",
        _ => "texto"
    };
}
