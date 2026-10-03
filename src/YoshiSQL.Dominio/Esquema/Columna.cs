namespace YoshiSQL.Dominio.Esquema;

public sealed record Columna(
    string Nombre,
    TipoDeDato TipoDeDato,
    bool AdmiteNulos,
    bool EsLlavePrimaria,
    bool EsIdentidad,
    int Posicion)
{
    /// <summary>
    /// Texto como lo muestra el explorador: "Id (PK, int, not null)".
    /// </summary>
    public string DescribirParaExplorador()
    {
        var detalles = new List<string>();

        if (EsLlavePrimaria)
        {
            detalles.Add("PK");
        }

        detalles.Add(TipoDeDato.Describir());
        detalles.Add(AdmiteNulos ? "null" : "not null");

        return $"{Nombre} ({string.Join(", ", detalles)})";
    }
}
