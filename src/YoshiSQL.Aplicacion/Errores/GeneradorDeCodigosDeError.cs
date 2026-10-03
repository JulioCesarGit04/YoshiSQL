using System.Security.Cryptography;

namespace YoshiSQL.Aplicacion.Errores;

public static class GeneradorDeCodigosDeError
{
    private const int CaracteresAleatorios = 4;

    /// <summary>
    /// Crea un código corto y fácil de dictar, ej. E-1003-7F3A (mes, día y cuatro caracteres al azar).
    /// </summary>
    public static string Generar(DateTimeOffset momento)
    {
        var parteAleatoria = Convert.ToHexString(RandomNumberGenerator.GetBytes(CaracteresAleatorios / 2));
        return $"E-{momento:MMdd}-{parteAleatoria}";
    }
}
