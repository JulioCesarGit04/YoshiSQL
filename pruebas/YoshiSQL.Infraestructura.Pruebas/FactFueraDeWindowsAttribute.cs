namespace YoshiSQL.Infraestructura.Pruebas;

/// <summary>
/// Prueba que solo tiene sentido en Linux o macOS (ej. permisos de archivo Unix); en Windows se omite
/// en lugar de fallar, porque allí la protección equivalente la da DPAPI.
/// </summary>
public sealed class FactFueraDeWindowsAttribute : FactAttribute
{
    public FactFueraDeWindowsAttribute()
    {
        if (OperatingSystem.IsWindows())
        {
            Skip = "Los permisos de archivo Unix no existen en Windows.";
        }
    }
}
