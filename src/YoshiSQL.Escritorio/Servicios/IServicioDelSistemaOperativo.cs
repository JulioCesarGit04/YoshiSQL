namespace YoshiSQL.Escritorio.Servicios;

public interface IServicioDelSistemaOperativo
{
    string CarpetaDeRegistros { get; }

    string CarpetaDeConfiguracion { get; }

    void AbrirCarpetaDeRegistros();

    Task CopiarAlPortapapelesAsync(string texto);
}
