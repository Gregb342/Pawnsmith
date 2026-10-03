using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Api.Endpoints;

/// <summary>
/// Turns the folder name of a URL into the folder of a project, or into
/// <c>PROJECT_NOT_FOUND</c> (§G.5, DEC-083).
/// </summary>
/// <remarks>
/// <para>
/// <b>The name arrives from a client, so it is MEN-002 and MEN-009 at once.</b>
/// It is accepted only if it is already in its canonical form —
/// <see cref="ProjectFolderName.From"/> returns it unchanged — which is a
/// whitelist: lower-case ASCII letters, digits and dashes, no dot, no
/// separator, no reserved name. Then the resolved path must be a direct child
/// of the projects root, which is the second barrier, and must not be a link.
/// </para>
/// <para>
/// Every refusal is the same <c>PROJECT_NOT_FOUND</c>, whether the name was
/// malformed or the folder absent: a legitimate caller has no use for the
/// difference, and the other kind has no business learning it.
/// </para>
/// </remarks>
public static class ProjectAccess
{
    /// <summary>The full path of the project folder named by <paramref name="folder"/>.</summary>
    /// <exception cref="ApiException"><c>PROJECT_NOT_FOUND</c>.</exception>
    public static string Directory(PawnsmithSettings settings, string folder)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrEmpty(folder) || !string.Equals(ProjectFolderName.From(folder), folder, StringComparison.Ordinal))
        {
            throw new ApiException(ApiCodes.ProjectNotFound);
        }

        string root = Path.GetFullPath(settings.ProjectsRoot);
        string directory = Path.GetFullPath(Path.Combine(root, folder));

        bool directChild = string.Equals(Path.GetDirectoryName(directory), root.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal);
        var info = new DirectoryInfo(directory);

        if (!directChild || !info.Exists || info.LinkTarget is not null)
        {
            throw new ApiException(ApiCodes.ProjectNotFound);
        }

        return directory;
    }
}
