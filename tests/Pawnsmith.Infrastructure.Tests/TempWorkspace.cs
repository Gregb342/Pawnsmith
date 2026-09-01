namespace Pawnsmith.Infrastructure.Tests;

/// <summary>
/// A throwaway directory for one test, deleted when the test ends.
/// </summary>
/// <remarks>
/// These are integration tests, so they do touch the file system — unlike the
/// domain tests, which B.8 forbids from doing so. Each test gets its own
/// directory so they can run in parallel without colliding.
/// </remarks>
internal sealed class TempWorkspace : IDisposable
{
    public TempWorkspace()
    {
        Root = Path.Combine(Path.GetTempPath(), "pawnsmith-tests", Guid.NewGuid().ToString("N"));
        ImagesDirectory = Path.Combine(Root, "images");
        Directory.CreateDirectory(ImagesDirectory);
    }

    public string Root { get; }

    public string ImagesDirectory { get; }

    /// <summary>Writes a file at the root and returns its full path.</summary>
    public string WriteFile(string name, string content)
    {
        string path = Path.Combine(Root, name);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Writes a minimal but genuine PNG of the given pixel size.
    /// </summary>
    /// <remarks>
    /// Only the signature and the IHDR chunk are real; the rest is absent. That
    /// is enough for a reader that measures without decoding, and it keeps the
    /// fixture readable — a real PNG in a test would be an opaque blob.
    /// </remarks>
    public string WritePng(string name, int widthPx, int heightPx)
    {
        byte[] header =
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            0x49, 0x48, 0x44, 0x52,
            .. BigEndian(widthPx),
            .. BigEndian(heightPx),
        ];

        string path = Path.Combine(ImagesDirectory, name);
        File.WriteAllBytes(path, header);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }

    private static byte[] BigEndian(int value)
    {
        return
        [
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF),
        ];
    }
}
