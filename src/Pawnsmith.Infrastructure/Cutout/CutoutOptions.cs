namespace Pawnsmith.Infrastructure.Cutout;

/// <summary>
/// The arbitrated values of the cut-out (§F.3.7, DEC-100).
/// </summary>
/// <remarks>
/// <para>
/// These values are <b>arbitrated, not measured</b> (DEC-057): they bound an
/// algorithm, they describe nothing physical. Each is declared once, here,
/// with its reason, and never written as a literal where it is applied. They
/// are meant to be tuned on the real images of T0a with the CLI's
/// <c>cutout</c> command (§F.8).
/// </para>
/// <para>
/// Colour distances are on the 0-255 scale of one channel: a distance of 24
/// means "no channel is more than 24 away".
/// </para>
/// </remarks>
public sealed record CutoutOptions
{
    /// <summary>Longest side decoded: the generator's and the sheet's bound (MEN-005).</summary>
    public int MaxImageDimensionPx { get; init; } = 8192;

    /// <summary>
    /// Heaviest paired image cut out, in bytes: the generator's own bound
    /// (§E.9). A paired image comes from the generator, so one heavier than
    /// that did not; an imported archive could otherwise hand the cut-out a
    /// file of gigabytes, read whole before its header is checked (MEN-005).
    /// </summary>
    public long MaxImageBytes { get; init; } = 64L * 1024 * 1024;

    /// <summary>
    /// Distance under which a pixel looks like the background. Absorbs the
    /// noise of a generated "uniform" grey without eating a light garment.
    /// </summary>
    public int BackgroundTolerance { get; init; } = 24;

    /// <summary>
    /// Stricter distance for background enclosed by the subject — between an
    /// arm and the body. Half the tolerance: a grey garment close to the
    /// background must not vanish.
    /// </summary>
    public int HoleTolerance { get; init; } = 12;

    /// <summary>Smallest enclosed hole removed, as a share of the half's pixels. Smaller ones are reflections.</summary>
    public double MinHoleFraction { get; init; } = 0.0005;

    /// <summary>
    /// Share of the top, left and right border pixels that must look like the
    /// background. T0a measured 83 % at worst; under 60 %, the border is
    /// scenery, not a uniform background.
    /// </summary>
    public double MinBorderUniformity { get; init; } = 0.60;

    /// <summary>
    /// Share of a bottom row that must differ from the background for the row
    /// to be ground. A narrow standing silhouette never covers it.
    /// </summary>
    public double GroundBandMinCoverage { get; init; } = 0.90;

    /// <summary>
    /// Most of the height removed as ground: four times the band T0a saw
    /// (8 to 10 pixels on 832), and the subject stays whole.
    /// </summary>
    public double GroundBandMaxFraction { get; init; } = 0.05;

    /// <summary>Smallest subject accepted, as a share of the half's pixels. Below, it is not a character.</summary>
    public double MinSubjectFraction { get; init; } = 0.01;
}
