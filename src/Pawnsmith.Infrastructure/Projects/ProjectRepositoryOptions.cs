namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Where projects live, and the resource bounds the repository refuses to go
/// past.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the single named place the default values live</b> (DEC-057). No
/// bound appears as a literal in the code that applies it, and no configuration
/// file is created: T2 has neither an API nor a host able to read one, so
/// writing <c>limits.json</c> today would mean a file nothing reads, a
/// configuration binding with no host, and a file-search path to harden — three
/// abstractions for zero consumers. The file arrives in T6, with the ASP.NET
/// host that can read it.
/// </para>
/// <para>
/// <c>calibration.json</c> is the wrong home for them and would stay so. It is
/// reserved for physical values, and putting a compression-ratio ceiling in it
/// would blur the one distinction that file carries — the one DEC-040 spent a
/// whole card establishing.
/// </para>
/// <para>
/// <b>These values are arbitrated, not measured.</b> The rule that physical
/// values are never invented does not apply to them, and marking them
/// <c>À CALIBRER</c> would be wrong: no ream of paper decides how many entries
/// an archive may hold.
/// </para>
/// <para>
/// A nuance not to smooth over: the projects root and the bounds are not of the
/// same nature. The root is a deployment decision, already embodied by the
/// <c>/app/data/projects</c> volume of A.6; the bounds are security limits and
/// belong to chapter 9. They travel together here out of convenience, and
/// nothing obliges them to share a file in T6.
/// </para>
/// </remarks>
/// <param name="ProjectsRoot">Absolute path of the folder holding every project.</param>
public sealed record ProjectRepositoryOptions(string ProjectsRoot)
{
    /// <summary>Largest <c>project.json</c> that will be read at all.</summary>
    /// <remarks>
    /// Checked <b>before</b> deserialising (C.7.1 step 2): the file is read into
    /// memory whole, so the bound has to come first or it protects nothing.
    /// Thirty-two mebibytes is far beyond any real project and far below
    /// anything that would hurt.
    /// </remarks>
    public long MaxProjectFileBytes { get; init; } = 32L * 1024 * 1024;

    /// <summary>Most entries an archive may contain.</summary>
    /// <remarks>A project of 200 candidates holds about 600 files.</remarks>
    public int MaxArchiveEntryCount { get; init; } = 10_000;

    /// <summary>Largest total size an archive may expand to.</summary>
    /// <remarks>
    /// A <c>Backup</c> of a large project is legitimately big; this is a safety
    /// bound, not a comfort one.
    /// </remarks>
    public long MaxArchiveUncompressedBytes { get; init; } = 4L * 1024 * 1024 * 1024;

    /// <summary>Highest compression ratio tolerated, overall and per entry.</summary>
    /// <remarks>
    /// A PNG does not compress. A high ratio is the signature of a decompression
    /// bomb, which MEN-005 bounds for images and nothing bounded for archives.
    /// </remarks>
    public int MaxArchiveCompressionRatio { get; init; } = 100;

    /// <summary>Deepest path an archive entry may carry, in segments.</summary>
    /// <remarks>The whitelist of C.8.2 allows two, so three is already generous.</remarks>
    public int MaxArchivePathDepth { get; init; } = 3;

    /// <summary>Longest path an archive entry may carry, in characters.</summary>
    public int MaxArchivePathLength { get; init; } = 255;
}
