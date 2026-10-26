using YoshiSQL.Infraestructura.Persistencia;

namespace YoshiSQL.Infraestructura.Pruebas.Persistencia;

public sealed class RepositorioDeScriptsEnDiscoPruebas : IDisposable
{
    private static readonly byte[] MarcaUtf8 = [0xEF, 0xBB, 0xBF];

    private readonly string _rutaDelArchivo = Path.Combine(Path.GetTempPath(), $"yoshisql-pruebas-{Guid.NewGuid()}.sql");
    private readonly RepositorioDeScriptsEnDisco _repositorio = new();

    [Fact]
    public async Task Guardar_SoloAscii_NoAgregaBom()
    {
        await _repositorio.GuardarAsync(_rutaDelArchivo, "SELECT 1;", CancellationToken.None);

        Assert.False((await File.ReadAllBytesAsync(_rutaDelArchivo)).AsSpan().StartsWith(MarcaUtf8));
    }

    [Fact]
    public async Task Guardar_ConTildes_AgregaBomYSeLeeIgual()
    {
        const string Script = "INSERT INTO dbo.T VALUES (N'año', N'canción');";

        await _repositorio.GuardarAsync(_rutaDelArchivo, Script, CancellationToken.None);

        Assert.True((await File.ReadAllBytesAsync(_rutaDelArchivo)).AsSpan().StartsWith(MarcaUtf8));
        Assert.Equal(Script, await _repositorio.LeerAsync(_rutaDelArchivo, CancellationToken.None));
    }

    public void Dispose() => File.Delete(_rutaDelArchivo);
}
