namespace Pawnsmith.Domain;

/// <summary>
/// The pixel dimensions of a source PNG.
/// </summary>
/// <remarks>
/// Only the ratio between the two matters to the domain — the placement scales
/// the image to fit its box while preserving that ratio. Pixels are taken
/// rather than a bare ratio so the caller has no arithmetic to do, and so a
/// swapped width and height stay visible at the call site.
/// </remarks>
/// <param name="WidthPx">Width of the source image, in pixels.</param>
/// <param name="HeightPx">Height of the source image, in pixels.</param>
public sealed record SourceImageSize(double WidthPx, double HeightPx);
