namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Whether this machine lets a test create a symbolic link, and a helper to do
/// it.
/// </summary>
/// <remarks>
/// <para>
/// It is not a given. Linux and macOS allow it to anyone; Windows requires the
/// <c>SeCreateSymbolicLink</c> privilege, which in practice means Developer Mode
/// or an elevated prompt. The continuous integration runs on Ubuntu, so the
/// MEN-008 test is exercised for real there; on a Windows workstation without
/// the privilege it cannot be.
/// </para>
/// <para>
/// <b>What the test does when it cannot create a link is written in the test
/// itself</b>, not hidden here: it falls back to the second layer of the same
/// countermeasure — the whitelist of DEC-050, which lets nothing but referenced
/// <c>.png</c> and <c>.pdf</c> files through — and says so. That is weaker, and
/// it is why the CI run is the one that certifies this threat.
/// </para>
/// </remarks>
internal static class SymbolicLinks
{
    private static readonly Lazy<bool> Supported = new(Probe);

    /// <summary>Whether <see cref="Create"/> will work on this machine.</summary>
    public static bool AreSupported => Supported.Value;

    /// <summary>Creates a symbolic link at <paramref name="linkPath"/>.</summary>
    public static void Create(string linkPath, string targetPath) =>
        File.CreateSymbolicLink(linkPath, targetPath);

    private static bool Probe()
    {
        string folder = Path.Combine(Path.GetTempPath(), "pawnsmith-symlink-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        try
        {
            string target = Path.Combine(folder, "target");
            File.WriteAllText(target, "probe");
            File.CreateSymbolicLink(Path.Combine(folder, "link"), target);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
