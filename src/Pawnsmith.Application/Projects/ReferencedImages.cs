using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Projects;

/// <summary>
/// Which image files a project references — the only ones the API serves
/// (§G.6.2, DEC-088).
/// </summary>
/// <remarks>
/// This is the whitelist of the export (DEC-050) applied to reading. The API is
/// not a static file server on <c>images/</c>: a file dropped there by hand, an
/// orphan left by a crash, a temporary file of T4 all stay invisible, and a
/// name that arrives in a URL never designates anything the project does not
/// declare. The rule lives here rather than in the endpoint because an endpoint
/// decides nothing (§G.0).
/// </remarks>
public static class ReferencedImages
{
    /// <summary>
    /// The stored path of <paramref name="fileName"/> if a candidate of the
    /// project references <c>images/{fileName}</c>, or null.
    /// </summary>
    /// <param name="project">The project as loaded.</param>
    /// <param name="fileName">A bare file name, as it arrives from a URL — no folder.</param>
    public static string? Find(Project project, string fileName)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        // Compared to the stored strings exactly, ordinal: a name that differs
        // by case or by an encoded separator is simply not referenced.
        string wanted = $"images/{fileName}";

        bool referenced = project.Blueprints
            .SelectMany(blueprint => blueprint.Candidates)
            .SelectMany(candidate => new[] { candidate.PairedImageFile, candidate.FrontImageFile, candidate.BackImageFile })
            .Any(path => string.Equals(path, wanted, StringComparison.Ordinal));

        return referenced ? wanted : null;
    }
}
