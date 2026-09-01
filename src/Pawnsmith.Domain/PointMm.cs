namespace Pawnsmith.Domain;

/// <summary>
/// A point in millimetres, relative to the unit it belongs to.
/// </summary>
/// <remarks>
/// X is measured from the left edge of the unit and grows rightwards; Y is
/// measured from the top and grows downwards, the convention documented on
/// <see cref="UnitBand"/>.
/// </remarks>
/// <param name="XMm">Distance from the left edge of the unit.</param>
/// <param name="YMm">Distance from the top of the unit.</param>
public sealed record PointMm(double XMm, double YMm);
