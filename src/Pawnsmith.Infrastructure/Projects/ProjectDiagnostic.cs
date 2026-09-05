namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Something worth telling the user about a project that loaded perfectly well.
/// </summary>
/// <remarks>
/// <para>
/// <b>A diagnostic is not an error code, and the distinction is the whole of
/// DEC-056.</b> Errors are returned <i>instead of</i> a project; diagnostics are
/// returned <i>with</i> one. Routing them through
/// <see cref="ProjectErrorCode"/> would turn them into failures, which is
/// exactly what the card removed.
/// </para>
/// <para>
/// What lands here is <b>relational</b>: it confronts a project with the machine
/// it happens to be sitting on. An image that is not on this disk, a paper
/// format this calibration does not declare, a tab that does not fit these
/// pawns. None of it is wrong in the file — take the same file to another
/// machine and it may all be fine. <b>A project opens to be corrected, not to
/// crash.</b>
/// </para>
/// <para>
/// T6 will need a proper representation of these for the interface. Until then
/// they are handed to the caller as they are, which is enough for a command line
/// and for a test.
/// </para>
/// </remarks>
/// <param name="Kind">What sort of mismatch this is.</param>
/// <param name="Field">Where it was found, down to the index, or the empty string for a project-wide one.</param>
/// <param name="Message">What to tell the user, in terms they can act on.</param>
public sealed record ProjectDiagnostic(ProjectDiagnosticKind Kind, string Field, string Message);

/// <summary>The kinds of mismatch a load can report without refusing.</summary>
public enum ProjectDiagnosticKind
{
    /// <summary>
    /// A referenced image is not on this disk (C.7.2).
    /// </summary>
    /// <remarks>
    /// A deliberate departure from the manifest of T1, where a missing image is
    /// a validity error. The two files do not have the same contract: a manifest
    /// is the input of a render, so a missing image makes the render impossible
    /// and failing at once is the right answer. A project is a workspace — a
    /// missing image makes one candidate unusable, not the project, and refusing
    /// to open it would make it <b>unrepairable</b>, since the only way to drop
    /// the offending candidate would be to hand-edit the JSON.
    /// </remarks>
    MissingImageFile,

    /// <summary>
    /// The paper format is not one this calibration declares.
    /// </summary>
    /// <remarks>
    /// A project created where the calibration declares <c>A4Paysage</c>
    /// (DEC-036) has to open on a machine that has no such entry — to be
    /// corrected, not to crash. It becomes an error when someone asks for a
    /// sheet, in the engine of T1, which already raises it.
    /// </remarks>
    UnknownPaperFormat,

    /// <summary>
    /// A tab override wider than the pawns of a size this project uses.
    /// </summary>
    /// <remarks>
    /// <b>Only ever reported for <c>TabAndSocket</c>.</b> It is the one geometry
    /// with a tab: <c>FoldedTent</c> lays a flap across the full pawn width and
    /// <c>NoSupport</c> lays nothing at all (DEC-039), so neither ever reads the
    /// value and neither can ever be hurt by it. Reporting it there would leave a
    /// permanent warning that <i>no value could clear</i> — the exact false
    /// positive C.5.4 names as the failure mode to avoid, since a warning that
    /// fires wrongly ends up ignored, which amounts to not having it.
    /// </remarks>
    OverrideExceedsPawnWidth,
}
