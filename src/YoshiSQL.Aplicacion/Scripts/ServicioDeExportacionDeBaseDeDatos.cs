using System.Text;
using YoshiSQL.Aplicacion.Conexiones;
using YoshiSQL.Dominio.Contratos;
using YoshiSQL.Dominio.Esquema;

namespace YoshiSQL.Aplicacion.Scripts;

/// <summary>
/// Arma un único script .sql con la estructura y (opcionalmente) los datos de toda una base de datos,
/// para recrearla en cualquier servidor sin depender de un respaldo .bak.
/// </summary>
public sealed class ServicioDeExportacionDeBaseDeDatos
{
    private readonly IExploradorDeEsquema _explorador;
    private readonly IEjecutorDeConsultas _ejecutor;
    private readonly IDivisorDeLotes _divisorDeLotes;
    private readonly IGeneradorDeScripts _generador;
    private readonly TimeProvider _reloj;

    public ServicioDeExportacionDeBaseDeDatos(IProveedorDeBaseDeDatos proveedor, TimeProvider reloj)
    {
        _explorador = proveedor.Explorador;
        _ejecutor = proveedor.Ejecutor;
        _divisorDeLotes = proveedor.DivisorDeLotes;
        _generador = proveedor.GeneradorDeScripts;
        _reloj = reloj;
    }

    public async Task<string> GenerarScriptCompletoAsync(
        ServidorConectado servidor,
        string baseDeDatos,
        OpcionesDeExportacion opciones,
        CancellationToken tokenDeCancelacion)
    {
        var acceso = servidor.DatosDeAcceso;

        var script = new StringBuilder();
        script.AppendLine($"-- Script de la base de datos {baseDeDatos}");
        script.AppendLine($"-- Generado por YoshiSQL el {_reloj.GetLocalNow():yyyy-MM-dd HH:mm}");
        script.AppendLine();
        AgregarLote(script, _generador.GenerarUso(baseDeDatos));

        var tablas = await _explorador.ObtenerTablasAsync(acceso, baseDeDatos, tokenDeCancelacion);

        var columnasPorTabla = new Dictionary<Tabla, IReadOnlyList<Columna>>();
        if (opciones.EstructuraDeTablas || opciones.DatosDeTablas)
        {
            foreach (var tabla in tablas)
            {
                columnasPorTabla[tabla] = await _explorador.ObtenerColumnasAsync(acceso, baseDeDatos, tabla, tokenDeCancelacion);
            }
        }

        if (opciones.EstructuraDeTablas)
        {
            script.AppendLine("-- Estructura de las tablas");
            script.AppendLine();
            foreach (var tabla in tablas)
            {
                AgregarLote(script, _generador.GenerarCreacionDeTablaSinEnvolver(tabla, columnasPorTabla[tabla]));
            }
        }

        if (opciones.DatosDeTablas)
        {
            await AgregarDatosAsync(script, servidor, baseDeDatos, tablas, columnasPorTabla, tokenDeCancelacion);
        }

        if (opciones.EstructuraDeTablas)
        {
            await AgregarLlavesForaneasAsync(script, acceso, baseDeDatos, tokenDeCancelacion);
        }

        // Funciones antes que vistas: una vista puede referenciar funciones
        if (opciones.Funciones)
        {
            var funciones = await _explorador.ObtenerFuncionesAsync(acceso, baseDeDatos, tokenDeCancelacion);
            await AgregarDefinicionesAsync(script, acceso, baseDeDatos, "Funciones", funciones, tokenDeCancelacion);
        }

        if (opciones.Vistas)
        {
            var vistas = await _explorador.ObtenerVistasAsync(acceso, baseDeDatos, tokenDeCancelacion);
            await AgregarDefinicionesAsync(script, acceso, baseDeDatos, "Vistas", vistas, tokenDeCancelacion);
        }

        if (opciones.Procedimientos)
        {
            var procedimientos = await _explorador.ObtenerProcedimientosAsync(acceso, baseDeDatos, tokenDeCancelacion);
            await AgregarDefinicionesAsync(script, acceso, baseDeDatos, "Procedimientos almacenados", procedimientos, tokenDeCancelacion);
        }

        return script.ToString();
    }

    private async Task AgregarDefinicionesAsync(
        StringBuilder script,
        Dominio.Conexiones.DatosDeAcceso acceso,
        string baseDeDatos,
        string titulo,
        IReadOnlyList<ObjetoDeEsquema> objetos,
        CancellationToken tokenDeCancelacion)
    {
        if (objetos.Count == 0)
        {
            return;
        }

        script.AppendLine($"-- {titulo}");
        script.AppendLine();

        foreach (var objeto in objetos)
        {
            var definicion = await _explorador.ObtenerDefinicionAsync(acceso, baseDeDatos, objeto, tokenDeCancelacion);

            if (string.IsNullOrWhiteSpace(definicion))
            {
                script.AppendLine($"-- {objeto.NombreCompleto}: cifrado o sin permiso para ver su definición; se omite.");
                script.AppendLine();
            }
            else
            {
                AgregarLote(script, definicion.Trim());
            }
        }
    }

    private async Task AgregarDatosAsync(
        StringBuilder script,
        ServidorConectado servidor,
        string baseDeDatos,
        IReadOnlyList<Tabla> tablas,
        IReadOnlyDictionary<Tabla, IReadOnlyList<Columna>> columnasPorTabla,
        CancellationToken tokenDeCancelacion)
    {
        script.AppendLine("-- Datos");
        script.AppendLine();

        await using var sesion = await _ejecutor.AbrirSesionAsync(servidor.DatosDeAcceso, baseDeDatos, tokenDeCancelacion);

        foreach (var tabla in tablas)
        {
            var lotes = _divisorDeLotes.DividirEnLotes(_generador.GenerarSeleccionCompleta(tabla));
            var resultado = await sesion.EjecutarLotesAsync(lotes, tokenDeCancelacion);
            var filas = resultado.ConjuntosDeResultados.Count > 0 ? resultado.ConjuntosDeResultados[0].Filas : [];

            var inserciones = _generador.GenerarInsertDeFilas(tabla, columnasPorTabla[tabla], filas);

            if (!string.IsNullOrEmpty(inserciones))
            {
                script.AppendLine($"-- {tabla.NombreCompleto} ({filas.Count} filas)");
                AgregarLote(script, inserciones);
            }
        }
    }

    private async Task AgregarLlavesForaneasAsync(
        StringBuilder script,
        Dominio.Conexiones.DatosDeAcceso acceso,
        string baseDeDatos,
        CancellationToken tokenDeCancelacion)
    {
        var llaves = await _explorador.ObtenerLlavesForaneasAsync(acceso, baseDeDatos, tokenDeCancelacion);

        if (llaves.Count == 0)
        {
            return;
        }

        script.AppendLine("-- Llaves foráneas");
        script.AppendLine();
        foreach (var llave in llaves)
        {
            script.AppendLine(_generador.GenerarLlaveForanea(llave));
        }

        script.AppendLine("GO");
        script.AppendLine();
    }

    private static void AgregarLote(StringBuilder script, string instruccion)
    {
        script.AppendLine(instruccion);
        script.AppendLine("GO");
        script.AppendLine();
    }
}
