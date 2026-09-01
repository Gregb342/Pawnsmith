namespace Pawnsmith.Domain;

/// <summary>
/// The printer correction of B.5.5, applied about the centre of the page.
/// </summary>
/// <remarks>
/// <para>
/// If the printer shrinks by 2%, the PDF is enlarged by 2%, and a mark drawn at
/// 102 mm comes out at 100 mm on paper. <b>The calibration mark is scaled like
/// everything else</b> — excluding it would make the measurement meaningless,
/// since the mark is the only judge of whether the print came out to scale.
/// </para>
/// <para>
/// <b>The centre is the anchor</b>, which the specification does not state. A
/// printer that rescales a page does so about its centre, so compensating about
/// the centre keeps the content where the printer expects to find it. Anchoring
/// at a corner would shift everything towards the opposite one, and the shift
/// would grow with the correction.
/// </para>
/// <para>
/// The whole correction lives here, in one type, applied by one method. It is
/// never spread through the geometry code.
/// </para>
/// </remarks>
/// <param name="Factor">Multiplier. 1.0 leaves the content untouched.</param>
/// <param name="PageWidthMm">Page width, used to find the centre.</param>
/// <param name="PageHeightMm">Page height, used to find the centre.</param>
public sealed record PageScale(double Factor, double PageWidthMm, double PageHeightMm)
{
    /// <summary>Scales a length. Positions are unaffected by this overload.</summary>
    public double Length(double lengthMm)
    {
        return lengthMm * Factor;
    }

    /// <summary>Scales a point about the centre of the page.</summary>
    public PointMm Point(PointMm point)
    {
        ArgumentNullException.ThrowIfNull(point);

        double centreXMm = PageWidthMm / 2;
        double centreYMm = PageHeightMm / 2;

        return new PointMm(
            centreXMm + ((point.XMm - centreXMm) * Factor),
            centreYMm + ((point.YMm - centreYMm) * Factor));
    }

    /// <summary>Scales a point given by its two coordinates.</summary>
    public PointMm Point(double xMm, double yMm)
    {
        return Point(new PointMm(xMm, yMm));
    }
}
