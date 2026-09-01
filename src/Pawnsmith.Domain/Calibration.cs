namespace Pawnsmith.Domain;

/// <summary>
/// Every physical value the engine uses, read from <c>config/calibration.json</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>No value in this graph may ever appear as a constant in code</b> (B.2).
/// The code reads these values; it does not know them. They are replaced after
/// the T0b control print, and that replacement must require no code change.
/// </para>
/// <para>
/// These are plain records with no serialisation attributes: Domain references
/// nothing, so reading the file is Infrastructure's job.
/// </para>
/// </remarks>
/// <param name="VersionSchema">Schema version of the calibration file.</param>
/// <param name="Paper">Documentary only — see <see cref="PaperNote"/>.</param>
/// <param name="Sizes">Dimensions per size. A size absent here is a validation error, not a default.</param>
/// <param name="Geometry">Appendix dimensions, per geometry.</param>
/// <param name="Layout">Page layout values.</param>
/// <param name="Print">Printer correction.</param>
/// <param name="Strokes">Stroke widths and colour of the printing marks.</param>
/// <param name="PaperFormats">Known paper sizes, by name.</param>
public sealed record Calibration(
    int VersionSchema,
    PaperNote Paper,
    IReadOnlyDictionary<Size, PawnDimensions> Sizes,
    GeometrySettings Geometry,
    LayoutSettings Layout,
    PrintSettings Print,
    StrokeSettings Strokes,
    IReadOnlyDictionary<string, PaperFormat> PaperFormats);

/// <summary>
/// The paper stock the other values were measured on. <b>No calculation reads
/// this.</b> It exists because every other value in the file depends on the
/// stock it was measured against: the day the ream changes and the pawns warp,
/// this is the only record of what used to work.
/// </summary>
/// <param name="GrammageGsm">Paper weight, in grams per square metre.</param>
/// <param name="Note">Free-text reminder carried in the file itself.</param>
public sealed record PaperNote(int GrammageGsm, string Note);

/// <summary>
/// The dimensions of one pawn size.
/// </summary>
/// <remarks>
/// <b>The trap of this project.</b> <paramref name="GridFootprintMm"/> is what
/// the pawn occupies on the game grid; <paramref name="PawnHeightMm"/> is how
/// tall it stands. They are independent dimensions — never derive one from the
/// other. A medium humanoid occupies a 25.4 mm square and stands about twice
/// that.
/// <para>
/// Footprints are documented facts (chapter 14 of the bible). Heights are
/// provisional markers, arbitrated in T0b (DEC-032).
/// </para>
/// </remarks>
/// <param name="GridFootprintMm">Footprint on the game grid, in millimetres.</param>
/// <param name="PawnWidthMm">Width of the printed pawn, in millimetres.</param>
/// <param name="PawnHeightMm">Standing height of the printed pawn, in millimetres.</param>
public sealed record PawnDimensions(
    double GridFootprintMm,
    double PawnWidthMm,
    double PawnHeightMm);

/// <summary>Appendix dimensions, one entry per geometry.</summary>
/// <param name="FoldedTent">Flap settings for the folded tent.</param>
/// <param name="TabAndSocket">Tab settings for the tab-and-socket pawn.</param>
public sealed record GeometrySettings(
    FoldedTentSettings FoldedTent,
    TabAndSocketSettings TabAndSocket);

/// <summary>Folded tent: the appendix spans the full pawn width.</summary>
/// <param name="FlapHeightMm">Height of one flap, in millimetres.</param>
public sealed record FoldedTentSettings(double FlapHeightMm);

/// <summary>Tab and socket: the appendix is a horizontally centred tab.</summary>
/// <param name="TabWidthMm">Tab width, in millimetres.</param>
/// <param name="TabHeightMm">Tab height, in millimetres.</param>
public sealed record TabAndSocketSettings(double TabWidthMm, double TabHeightMm);

/// <summary>Values governing how units are laid out on a page.</summary>
/// <remarks>
/// <paramref name="PageMarginMm"/> is a single value applied to all four sides.
/// That uniformity is an arbitration, not an oversight (DEC-035): do not turn
/// it into four independent margins without reopening the decision.
/// </remarks>
/// <param name="PageMarginMm">Margin on every side of the page, in millimetres.</param>
/// <param name="GutterMm">Space between two neighbouring cut outlines, in millimetres.</param>
/// <param name="SilhouetteMarginMm">Safety margin around the artwork, in millimetres.</param>
/// <param name="CalibrationZoneHeightMm">Height reserved at the page bottom for the calibration mark.</param>
public sealed record LayoutSettings(
    double PageMarginMm,
    double GutterMm,
    double SilhouetteMarginMm,
    double CalibrationZoneHeightMm);

/// <summary>Printer correction.</summary>
/// <remarks>
/// <paramref name="ScaleCorrectionFactor"/> applies to the whole page content,
/// <b>the calibration mark included</b> (B.5.5). That is deliberate: if the
/// printer shrinks by 2%, the PDF is enlarged by 2%, and a mark drawn at
/// 102 mm comes out at 100 mm on paper. Excluding the mark from the correction
/// would make the measurement meaningless.
/// </remarks>
/// <param name="ScaleCorrectionFactor">Multiplier applied to all page content. 1.0 means no correction.</param>
public sealed record PrintSettings(double ScaleCorrectionFactor);

/// <summary>Stroke widths and colour of the printing marks (B.5.4).</summary>
/// <param name="CutWidthMm">Cut outline stroke width, in millimetres.</param>
/// <param name="FoldWidthMm">Fold line stroke width, in millimetres.</param>
/// <param name="ColorHex">Stroke colour, as a hexadecimal RGB string.</param>
/// <param name="FoldDashPatternMm">Dash pattern of fold lines, in millimetres.</param>
public sealed record StrokeSettings(
    double CutWidthMm,
    double FoldWidthMm,
    string ColorHex,
    IReadOnlyList<double> FoldDashPatternMm);
