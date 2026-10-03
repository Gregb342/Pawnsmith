namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>Finds the repository, so a test can read a file the repository ships.</summary>
internal static class RepositoryRoot
{
    /// <summary>The folder holding <c>Pawnsmith.sln</c>, found by walking up from the test binary.</summary>
    public static string Path()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(System.IO.Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = System.IO.Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Pawnsmith.sln not found above the test binary.");
    }
}
