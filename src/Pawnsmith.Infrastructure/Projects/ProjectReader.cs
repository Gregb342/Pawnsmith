using System.Text.Json;

using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>A project as it came off the disk, with whatever it has to say about itself.</summary>
/// <param name="Project">The project. Always usable — a load either succeeds or throws.</param>
/// <param name="Diagnostics">What does not match this machine. Empty in the ordinary case.</param>
public sealed record LoadedProject(Project Project, IReadOnlyList<ProjectDiagnostic> Diagnostics);

/// <summary>
/// Reads a project folder, in the order C.7.1 fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>The order is not a matter of taste.</b> Each step assumes the one before
/// it succeeded, and swapping two of them opens a hole: the size bound has to
/// come before the file is read into memory or it protects nothing, and the
/// paths have to be checked before anything is opened or the check is the thing
/// that opens them.
/// </para>
/// <para>
/// Steps 1 to 7 fail the load. Step 9 never does, and the line between them is
/// DEC-056: <b>what the file contradicts on its own blocks, what only this
/// machine contradicts informs.</b>
/// </para>
/// </remarks>
public sealed class ProjectReader
{
    private readonly ProjectRepositoryOptions options;

    public ProjectReader(ProjectRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    /// <summary>Loads the project held in <paramref name="projectDirectory"/>.</summary>
    /// <remarks>
    /// The calibration is a parameter because two things need it, and only two:
    /// producing the relational diagnostics, and — later, in Application —
    /// building the effective calibration. Validating the file itself does not
    /// need it at all (DEC-053, DEC-056).
    /// </remarks>
    /// <exception cref="ProjectException">Any of steps 1 to 7 refuses.</exception>
    public async Task<LoadedProject> LoadAsync(
        string projectDirectory,
        Calibration calibration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(calibration);

        string resolvedDirectory = RequireUnderProjectsRoot(projectDirectory);
        byte[] content = await ReadFileAsync(resolvedDirectory, cancellationToken).ConfigureAwait(false);

        ProjectDocument document = Deserialize(content, resolvedDirectory);

        // Steps 4 to 7, all of them intrinsic and all of them blocking.
        ProjectValidation.Validate(document);

        // Step 8: build the entities. This recalculates nothing - the resolved
        // prompt and the misalignment are computed when someone asks for them,
        // from the current clauses, never at load time (C.3.6).
        Project project = document.ToDomain();

        // Step 9, and only now: confront the project with this machine.
        List<ProjectDiagnostic> diagnostics = [];
        CollectImageDiagnostics(project, resolvedDirectory, diagnostics);
        CollectPaperFormatDiagnostic(project, calibration, diagnostics);
        CollectOverrideDiagnostics(project, calibration, diagnostics);

        return new LoadedProject(project, diagnostics);
    }

    // ---- Étape 1 : le dossier est-il bien sous la racine ? ----------------

    private string RequireUnderProjectsRoot(string projectDirectory)
    {
        string root = Path.GetFullPath(options.ProjectsRoot);
        string candidate = Path.GetFullPath(projectDirectory);

        // Resolved first, compared second. Comparing the strings as given would
        // be defeated by a "..", and following the link matters because a
        // symbolic link is how a folder that looks inside the root actually
        // lives outside it - the read-side cousin of MEN-008.
        // Guarded by Exists because resolving the link of a folder that is not
        // there throws, and a path that escapes the root has to be refused as an
        // escape whether or not anything sits at the end of it. Step 2 is what
        // reports a folder that is legitimately located and simply absent.
        if (Directory.Exists(candidate)
            && Directory.ResolveLinkTarget(candidate, returnFinalTarget: true) is { } target)
        {
            candidate = Path.GetFullPath(target.FullName);
        }

        if (!IsUnder(candidate, root))
        {
            throw new ProjectException(
                ProjectErrorCode.PathEscape,
                $"The project folder '{projectDirectory}' resolves outside the projects root " +
                $"'{options.ProjectsRoot}'. Nothing is read from outside it.");
        }

        return candidate;
    }

    /// <summary>Whether <paramref name="path"/> is the root itself or sits inside it.</summary>
    /// <remarks>
    /// The trailing separator is what stops <c>/data/projects-evil</c> passing
    /// for a child of <c>/data/projects</c> — a plain <c>StartsWith</c> on the
    /// two strings says yes, and it is wrong. The comparison follows the
    /// platform: case matters on Linux and does not on Windows, and pretending
    /// otherwise would refuse legitimate folders on one side or accept escapes on
    /// the other.
    /// </remarks>
    private static bool IsUnder(string path, string root)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        string rootWithSeparator = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        return string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), root.TrimEnd(Path.DirectorySeparatorChar), comparison)
            || path.StartsWith(rootWithSeparator, comparison);
    }

    // ---- Étapes 2 et 3 : lire, borner, désérialiser -----------------------

    private async Task<byte[]> ReadFileAsync(string projectDirectory, CancellationToken cancellationToken)
    {
        string path = Path.Combine(projectDirectory, ProjectFileWriter.FileName);

        if (!Directory.Exists(projectDirectory) || !File.Exists(path))
        {
            throw new ProjectException(
                ProjectErrorCode.NotFound,
                $"No '{ProjectFileWriter.FileName}' in '{projectDirectory}'.");
        }

        // Bounded before being read, never after: the whole file goes into
        // memory, so a check that came afterwards would protect nothing (C.9.3).
        long length = new FileInfo(path).Length;

        if (length > options.MaxProjectFileBytes)
        {
            throw new ProjectException(
                ProjectErrorCode.TooLarge,
                $"'{path}' is {length} bytes, past the {options.MaxProjectFileBytes} byte bound.");
        }

        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private static ProjectDocument Deserialize(byte[] content, string projectDirectory)
    {
        try
        {
            return ProjectJson.Deserialize(content);
        }
        catch (JsonException error)
        {
            // An unknown member arrives here, because the serialiser is the one
            // refusing it (C.6.3). Its message names the member, which is what
            // C.11 asks of PROJECT_INVALID: a malformed project is almost always
            // your own, so it says which field.
            throw new ProjectException(
                ProjectErrorCode.Invalid,
                $"The project file in '{projectDirectory}' cannot be read: {error.Message}",
                error);
        }
    }

    // ---- Étape 9 : les diagnostics, qui n'échouent jamais le chargement ---

    private static void CollectImageDiagnostics(
        Project project,
        string projectDirectory,
        List<ProjectDiagnostic> diagnostics)
    {
        for (int blueprintIndex = 0; blueprintIndex < project.Blueprints.Count; blueprintIndex++)
        {
            Blueprint blueprint = project.Blueprints[blueprintIndex];

            for (int candidateIndex = 0; candidateIndex < blueprint.Candidates.Count; candidateIndex++)
            {
                Candidate candidate = blueprint.Candidates[candidateIndex];
                string where = $"blueprints[{blueprintIndex}].candidates[{candidateIndex}]";

                CheckImage(candidate.PairedImageFile, $"{where}.pairedImageFile");
                CheckImage(candidate.FrontImageFile, $"{where}.frontImageFile");
                CheckImage(candidate.BackImageFile, $"{where}.backImageFile");
            }
        }

        void CheckImage(string? relativePath, string field)
        {
            if (relativePath is null)
            {
                return;
            }

            string absolute = Path.GetFullPath(Path.Combine(projectDirectory, relativePath));

            // The lexical check of ImagePathRules ran before anything was opened;
            // this is the other half C.3.5 asks for, on the resolved path. Both
            // are needed: the first stops a "..", the second stops a link.
            if (!IsUnder(absolute, projectDirectory))
            {
                throw new ProjectException(
                    ProjectErrorCode.PathEscape,
                    $"The image path at {field} resolves outside the project folder.");
            }

            if (!File.Exists(absolute))
            {
                diagnostics.Add(new ProjectDiagnostic(
                    ProjectDiagnosticKind.MissingImageFile,
                    field,
                    $"The file '{relativePath}' is referenced but is not on this disk. " +
                    "The candidate is unusable; the project is not."));
                return;
            }

            if (File.ResolveLinkTarget(absolute, returnFinalTarget: true) is { } target
                && !IsUnder(Path.GetFullPath(target.FullName), projectDirectory))
            {
                throw new ProjectException(
                    ProjectErrorCode.PathEscape,
                    $"The image at {field} is a link pointing outside the project folder.");
            }
        }
    }

    private static void CollectPaperFormatDiagnostic(
        Project project,
        Calibration calibration,
        List<ProjectDiagnostic> diagnostics)
    {
        if (calibration.PaperFormats.ContainsKey(project.PaperFormatName))
        {
            return;
        }

        diagnostics.Add(new ProjectDiagnostic(
            ProjectDiagnosticKind.UnknownPaperFormat,
            "paperFormat",
            $"'{project.PaperFormatName}' is not a paper format this calibration declares. " +
            $"Known formats are: {string.Join(", ", calibration.PaperFormats.Keys)}. " +
            "The project opens all the same; producing a sheet will not."));
    }

    private static void CollectOverrideDiagnostics(
        Project project,
        Calibration calibration,
        List<ProjectDiagnostic> diagnostics)
    {
        // Conditioned on the geometry, and this is the whole point of the check.
        // Only TabAndSocket has a tab, so only there can the value be read and
        // only there can it hurt. Warning a NoSupport project would be a warning
        // nothing could ever clear (C.4.4).
        if (project.Geometry != Geometry.TabAndSocket)
        {
            return;
        }

        if (project.CalibrationOverrides.TabWidthMm is not { } tabWidthMm)
        {
            return;
        }

        // One diagnostic per size actually used by a blueprint, not per size the
        // calibration declares: a project holding only Medium pawns has no
        // business being warned about Gargantuan ones.
        foreach (Size size in project.Blueprints.Select(blueprint => blueprint.Size).Distinct())
        {
            if (!calibration.Sizes.TryGetValue(size, out PawnDimensions? dimensions)
                || tabWidthMm <= dimensions.PawnWidthMm)
            {
                continue;
            }

            diagnostics.Add(new ProjectDiagnostic(
                ProjectDiagnosticKind.OverrideExceedsPawnWidth,
                "calibrationOverrides.tabWidthMm",
                $"The tab is {tabWidthMm} mm wide and a {size} pawn is {dimensions.PawnWidthMm} mm. " +
                "The project opens; calculating a sheet from it will fail, because a tab wider " +
                "than the pawn turns the cut outline inside out."));
        }
    }
}
