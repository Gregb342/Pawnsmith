namespace Pawnsmith.Domain;

/// <summary>
/// A horizontal band of an unfolded unit, positioned along the unit's own
/// vertical axis.
/// </summary>
/// <remarks>
/// <b>Axis convention, used everywhere in the domain:</b> Y is measured from
/// the <i>top</i> of the unit and grows <i>downwards</i>. The specification
/// describes the bands top to bottom (B.4.1), so reading the code in the order
/// the document reads is worth more than matching the mathematical convention
/// where Y grows upwards.
/// <para>
/// Coordinates are relative to the unit, not to the page. Placing units on a
/// page is a separate concern.
/// </para>
/// </remarks>
/// <param name="TopMm">Distance from the top of the unit to the top of the band.</param>
/// <param name="HeightMm">Height of the band.</param>
public sealed record UnitBand(double TopMm, double HeightMm)
{
    /// <summary>Distance from the top of the unit to the bottom of the band.</summary>
    public double BottomMm => TopMm + HeightMm;
}
