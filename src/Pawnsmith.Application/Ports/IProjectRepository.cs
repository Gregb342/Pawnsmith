using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Ports;

/// <summary>
/// Everything the application can do with a project folder.
/// </summary>
/// <remarks>
/// <para>
/// The port chapter 7 of the bible declares, assembled here because C.1 puts it
/// in the scope of T2. It is not written ahead of its need: the five operations
/// all exist, in Infrastructure, and this is what stops a use case having to
/// know which of five classes to reach for — and what stops T6 inventing its own
/// answer to that question.
/// </para>
/// <para>
/// <b>Three departures from the signatures chapter 7 sketched, each with a
/// reason that arrived after it was written.</b>
/// </para>
/// <para>
/// <b>Every operation takes a path.</b> Chapter 7 wrote
/// <c>SaveAsync(Project, CancellationToken)</c>, with the project alone. That
/// only works if a project knows where it lives, and DEC-047 forbids exactly
/// that: the folder name has no meaning, so a project that carried its own
/// location would make renaming a folder a way of changing the project. The
/// repository is therefore stateless and told where to work each time
/// (DEC-062).
/// </para>
/// <para>
/// <b><see cref="LoadAsync"/> takes the calibration, and returns diagnostics
/// beside the project.</b> DEC-053 added the parameter; DEC-056 decided what it
/// is for. It never refuses a project — a project is a user's data and a
/// calibration is a machine's, and one never rejects the other — it reports what
/// does not match. A project opens to be corrected, not to crash.
/// </para>
/// <para>
/// <b>Import takes a name, not a destination.</b> The folder is derived from it
/// through the transliteration of MEN-009, so that a name arriving inside a
/// third-party archive can never decide where anything is written.
/// </para>
/// <para>
/// What the port deliberately does <b>not</b> carry: any notion of the "current"
/// project, any locking, any merge. The first is state and this repository has
/// none; the second waits for a concurrent caller, which arrives with the API in
/// T6; the third is refused outright (DEC-051).
/// </para>
/// </remarks>
public interface IProjectRepository
{
    /// <summary>Creates the folder of a new, empty project and writes its file.</summary>
    /// <returns>The project as written, and the folder it went to.</returns>
    Task<CreatedProjectResult> CreateAsync(
        string name,
        Universe universe,
        Geometry geometry,
        string paperFormatName,
        CancellationToken cancellationToken);

    /// <summary>Reads the project held in <paramref name="projectDirectory"/>.</summary>
    /// <exception cref="Exception">
    /// An implementation refuses with its own exception type carrying an error
    /// code. The port does not name it, because naming it here would drag an
    /// Infrastructure type into Application — the wrong direction (A.3).
    /// </exception>
    Task<LoadedProjectResult> LoadAsync(
        string projectDirectory,
        Calibration calibration,
        CancellationToken cancellationToken);

    /// <summary>Writes <paramref name="project"/> into an existing folder.</summary>
    /// <remarks>
    /// It never receives the project as it was loaded, and must never grow a
    /// parameter for it (DEC-055). No field is locked after creation, so there is
    /// no transition to arbitrate; a repository that compared before and after
    /// would become the keeper of a business rule. If a later slice wants one, it
    /// belongs to a use case, which holds both states.
    /// </remarks>
    /// <returns>The project as written, with its new <c>modifiedAt</c>.</returns>
    Task<Project> SaveAsync(
        string projectDirectory,
        Project project,
        CancellationToken cancellationToken);

    /// <summary>Writes the paired image of a new candidate into the project folder.</summary>
    /// <remarks>
    /// <para>
    /// Added in T4, for the batch (DEC-075): the image is written <b>before</b>
    /// the candidate that references it is saved, so that a <c>project.json</c>
    /// never points to a file that does not exist yet.
    /// </para>
    /// <para>
    /// The repository chooses the name, <c>images/{candidateId}-pair.png</c>,
    /// and returns it: the file layout of C.3.1 is the repository's to know, and
    /// nothing the generator said about its own file reaches the disk.
    /// </para>
    /// </remarks>
    /// <returns>The path to store in <c>pairedImageFile</c>, relative to the project folder.</returns>
    Task<string> WritePairedImageAsync(
        string projectDirectory,
        Guid candidateId,
        byte[] png,
        CancellationToken cancellationToken);

    /// <summary>Writes the two cut-outs of a candidate, replacing existing ones (§F.5, DEC-101).</summary>
    /// <remarks>
    /// Added in T5. The repository chooses the names,
    /// <c>images/{candidateId}-front.png</c> and <c>-back.png</c>, as it does
    /// for the paired image. Unlike the paired image, an existing cut-out is
    /// replaced: cutting a candidate out again is ordinary.
    /// </remarks>
    /// <returns>The paths to store in <c>frontImageFile</c> and <c>backImageFile</c>.</returns>
    Task<CutoutFiles> WriteCutoutImagesAsync(
        string projectDirectory,
        Guid candidateId,
        byte[] frontPng,
        byte[] backPng,
        CancellationToken cancellationToken);

    /// <summary>Deletes image files of a project, exactly those listed (DEC-070).</summary>
    /// <remarks>
    /// Added in T6, so that the API can finish what <c>BlueprintRemoval</c>
    /// starts without reaching into Infrastructure; the command line of T3
    /// calls the same code directly. Every path is checked before the first
    /// file is deleted.
    /// </remarks>
    /// <returns>How many files were actually removed.</returns>
    Task<int> DeleteImagesAsync(
        string projectDirectory,
        IReadOnlyList<string> relativePaths,
        CancellationToken cancellationToken);

    /// <summary>Opens an image file of a project for reading, or returns null if it is not there.</summary>
    /// <remarks>
    /// The path must pass the rules of C.3.5; whether the project
    /// <i>references</i> it is not the repository's question but the caller's
    /// (DEC-088).
    /// </remarks>
    Task<Stream?> OpenImageAsync(
        string projectDirectory,
        string relativePath,
        CancellationToken cancellationToken);

    /// <summary>Every project under the projects root, including those that do not load.</summary>
    /// <remarks>
    /// Added in T6 (DEC-083). A project that fails to load is listed with its
    /// code rather than left out: it opens to be corrected, it does not vanish
    /// (DEC-056). Sorted by folder name, ordinal, so that two calls give the
    /// same order.
    /// </remarks>
    Task<IReadOnlyList<ProjectListing>> ListAsync(
        Calibration calibration,
        CancellationToken cancellationToken);

    /// <summary>Writes an archive of <paramref name="projectDirectory"/>.</summary>
    /// <returns>The full path of the archive written.</returns>
    Task<string> ExportArchiveAsync(
        string projectDirectory,
        ArchiveProfileKind profile,
        string destinationDirectory,
        CancellationToken cancellationToken);

    /// <summary>Imports an archive under the projects root, or nothing at all.</summary>
    /// <param name="name">Display name the folder is derived from (MEN-009).</param>
    Task<ImportedProjectResult> ImportArchiveAsync(
        string archivePath,
        string name,
        Calibration calibration,
        CancellationToken cancellationToken);
}

/// <summary>What an archive is for, which decides what goes into it.</summary>
/// <remarks>
/// Declared in Application rather than reused from Infrastructure, because the
/// dependency rule of A.3 has no arrow in that direction. Two members, and a
/// mapping of four lines on the other side — which is what DEC-021 asks for
/// anyway: a mapping one can read.
/// </remarks>
public enum ArchiveProfileKind
{
    /// <summary>Back yourself up, or move machine. Everything the whitelist allows.</summary>
    Backup,

    /// <summary>Send the project to someone. Filtered, and coherent with what it carries.</summary>
    Share,
}

/// <summary>A project that has just been created, and where it lives.</summary>
public sealed record CreatedProjectResult(Project Project, string Directory);

/// <summary>A project as it came off the disk, with whatever it has to say about itself.</summary>
/// <param name="Project">The project. Always usable — a load either succeeds or throws.</param>
/// <param name="Diagnostics">
/// What does not match this machine, as text. Empty in the ordinary case, and
/// <b>never</b> a reason the load failed (DEC-056).
/// </param>
public sealed record LoadedProjectResult(Project Project, IReadOnlyList<ProjectMismatch> Diagnostics);

/// <summary>A project that has just arrived from an archive.</summary>
public sealed record ImportedProjectResult(
    Project Project,
    string Directory,
    IReadOnlyList<ProjectMismatch> Diagnostics);

/// <summary>Something worth telling the user about a project that opened perfectly well.</summary>
/// <remarks>
/// <b>Not an error code, and the distinction is the whole of DEC-056.</b> An
/// error is returned <i>instead of</i> a project; this is returned <i>with</i>
/// one. T6 will want a richer representation for the interface; until there is
/// an interface, the kind and the sentence are what a command line and a test
/// need.
/// </remarks>
/// <param name="Kind">What sort of mismatch this is, as the name of the infrastructure kind.</param>
/// <param name="Field">Where it was found, down to the index.</param>
/// <param name="Message">What to tell the user, in terms they can act on.</param>
public sealed record ProjectMismatch(string Kind, string Field, string Message);

/// <summary>One folder under the projects root, and what it holds.</summary>
/// <param name="Folder">The folder's name, which is how the API addresses it (DEC-083).</param>
/// <param name="Directory">The folder's full path.</param>
/// <param name="Project">The project, when it loads.</param>
/// <param name="ErrorCode">The code that stopped it loading, when it does not.</param>
public sealed record ProjectListing(string Folder, string Directory, Project? Project, string? ErrorCode);

/// <summary>Where the two cut-outs of a candidate were written, relative to the project folder.</summary>
public sealed record CutoutFiles(string Front, string Back);
