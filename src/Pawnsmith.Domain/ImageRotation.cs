namespace Pawnsmith.Domain;

/// <summary>
/// How an image is turned before being drawn.
/// </summary>
/// <remarks>
/// Only two values exist, and that is the whole point: a half turn is not a
/// quantity to be parameterised, it is a structural property of the back panel
/// (B.4.1). An enumeration rather than a boolean, so that reading the drawing
/// code answers "rotated by how much?" without looking it up.
/// </remarks>
public enum ImageRotation
{
    /// <summary>Drawn as-is. The front panel.</summary>
    None,

    /// <summary>
    /// Turned by 180° about the centre of its own rectangle. The back panel.
    /// </summary>
    HalfTurn,
}
