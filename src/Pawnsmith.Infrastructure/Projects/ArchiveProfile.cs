namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// What an archive is for, which decides what goes into it.
/// </summary>
/// <remarks>
/// <para>
/// Two profiles rather than one, and it is not a gratuitous complication
/// (DEC-050). MEN-006 already forbids secrets and logs; the question the two
/// profiles answer is what to do with the things the threat does not cover —
/// <c>exports/</c> and the raw paired images.
/// </para>
/// <para>
/// The figures decide it. A paired image is 1 to 2 MB <b>per candidate,
/// rejected ones included</b>, and is only ever useful for diagnosing its own
/// split. The PDFs in <c>exports/</c> are entirely reproducible from the project
/// and the calibration. Neither has any value for a recipient, and both have
/// value for yourself.
/// </para>
/// </remarks>
public enum ArchiveProfile
{
    /// <summary>
    /// Back yourself up, or move to another machine.
    /// </summary>
    /// <remarks>
    /// Everything the whitelist allows, <c>exports/</c> and paired images
    /// included. The acceptance criterion "round trip without loss" is measured
    /// on this profile, to the byte.
    /// </remarks>
    Backup,

    /// <summary>
    /// Send the project to someone.
    /// </summary>
    /// <remarks>
    /// A filtered <c>project.json</c>, the cut-out images of the candidates that
    /// were kept, and nothing else. The criterion becomes: no loss of anything
    /// that was not deliberately removed, and an imported project that is
    /// coherent and renderable.
    /// </remarks>
    Share,
}
