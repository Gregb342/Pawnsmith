using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Gives a new project a folder of its own under the projects root.
/// </summary>
/// <remarks>
/// <para>
/// This is the half of C.3.2 that <see cref="ProjectFolderName"/> deliberately
/// left out. That one is a pure function: it turns a display name into a safe
/// folder name and touches nothing. Resolving a collision and checking the
/// result really sits under the root both need a file system, so they live here,
/// where a file system is allowed.
/// </para>
/// <para>
/// <b>A collision is suffixed here and refused at import, and the difference is
/// intentional.</b> Creating a second project called "Donjon" is an ordinary
/// thing to do and <c>donjon-2</c> is the obvious answer. Importing an archive
/// onto an existing folder is not: something is already there, somebody meant
/// something by it, and quietly landing beside it would hide the fact that the
/// restore did not go where the user thought (C.9.2, DEC-051).
/// </para>
/// </remarks>
public sealed class ProjectCreator
{
    /// <summary>How many suffixed names to try before giving up.</summary>
    /// <remarks>
    /// A bound rather than a loop that cannot end. A thousand projects sharing
    /// one name is not a situation to keep searching through — it means
    /// something is wrong upstream, and saying so beats spinning.
    /// </remarks>
    private const int MaxCollisionAttempts = 1_000;

    private readonly ProjectRepositoryOptions options;
    private readonly ProjectSaver saver;
    private readonly TimeProvider clock;

    public ProjectCreator(ProjectRepositoryOptions options, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        this.clock = clock ?? TimeProvider.System;
        saver = new ProjectSaver(this.clock);
    }

    /// <summary>Creates the folder of a new, empty project and writes its file.</summary>
    /// <returns>The project as written, and the folder it was written to.</returns>
    public async Task<CreatedProject> CreateAsync(
        string name,
        Universe universe,
        Geometry geometry,
        string paperFormatName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrEmpty(paperFormatName);

        string root = Path.GetFullPath(options.ProjectsRoot);
        Directory.CreateDirectory(root);

        string directory = FreeFolder(root, ProjectFolderName.From(name));

        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "images"));
        Directory.CreateDirectory(Path.Combine(directory, "exports"));

        Project project = NewProject.Create(
            name,
            universe,
            geometry,
            paperFormatName,
            clock.GetUtcNow());

        Project written = await saver
            .SaveAsync(directory, project, cancellationToken)
            .ConfigureAwait(false);

        return new CreatedProject(written, directory);
    }

    /// <summary>The first of <c>name</c>, <c>name-2</c>, <c>name-3</c>… that is free.</summary>
    /// <remarks>
    /// Each candidate is resolved and checked against the root before it is even
    /// considered, rather than only the one finally chosen. The suffix cannot
    /// make a safe name unsafe, so this can never fire today — and that is
    /// exactly why it is written where the loop is rather than after it: a check
    /// placed only on the winner would quietly stop covering the others the day
    /// the naming rules change.
    /// </remarks>
    private static string FreeFolder(string root, string folder)
    {
        for (int attempt = 1; attempt <= MaxCollisionAttempts; attempt++)
        {
            string candidate = attempt == 1
                ? folder
                : $"{folder}-{attempt}";

            string directory = Path.GetFullPath(Path.Combine(root, candidate));

            if (!IsUnder(directory, root))
            {
                throw new ProjectException(
                    ProjectErrorCode.PathEscape,
                    $"'{candidate}' resolves outside the projects root '{root}'.");
            }

            if (!Directory.Exists(directory) && !File.Exists(directory))
            {
                return directory;
            }
        }

        throw new ProjectException(
            ProjectErrorCode.Invalid,
            $"There are already {MaxCollisionAttempts} folders named '{folder}' or a suffix of it. " +
            "Give the project a different name.");
    }

    private static bool IsUnder(string path, string root)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        string trimmedRoot = root.TrimEnd(Path.DirectorySeparatorChar);

        return string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), trimmedRoot, comparison)
            || path.StartsWith(trimmedRoot + Path.DirectorySeparatorChar, comparison);
    }
}

/// <summary>A project that has just been created, and where it lives.</summary>
public sealed record CreatedProject(Project Project, string Directory);
