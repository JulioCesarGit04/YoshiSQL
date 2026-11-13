using YoshiSQL.Dominio.Contratos;

namespace YoshiSQL.Aplicacion.Consultas;

public sealed class ServicioDeFormatoSql
{
    private readonly IFormateadorDeSql _formateador;

    public ServicioDeFormatoSql(IProveedorDeBaseDeDatos proveedor)
    {
        _formateador = proveedor.Formateador;
    }

    public string Formatear(string textoSql) => _formateador.Formatear(textoSql);

    public IReadOnlyList<Dominio.Consultas.ErrorDeSintaxis> BuscarErroresDeSintaxis(string textoSql) =>
        _formateador.BuscarErroresDeSintaxis(textoSql);
}
