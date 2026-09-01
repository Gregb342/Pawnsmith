namespace Pawnsmith.Domain;

/// <summary>
/// A paper size, in millimetres. Named entries come from the calibration file:
/// no format is hard-coded, and adding one is a configuration change, never a
/// code change (DEC-016).
/// </summary>
/// <remarks>
/// Landscape is expressed the same way — an entry whose width and height are
/// swapped — and never as an engine parameter or a UI toggle (DEC-036).
/// </remarks>
/// <param name="Name">The key this format is listed under in the calibration file.</param>
/// <param name="WidthMm">Page width, in millimetres.</param>
/// <param name="HeightMm">Page height, in millimetres.</param>
public sealed record PaperFormat(string Name, double WidthMm, double HeightMm);
