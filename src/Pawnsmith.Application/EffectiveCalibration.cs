using Pawnsmith.Domain;

namespace Pawnsmith.Application;

/// <summary>
/// Merges a project's overrides into the machine's calibration, once, before
/// anything asks the layout engine for a sheet.
/// </summary>
/// <remarks>
/// <para>
/// <b>The domain never sees an override.</b> It sees resolved values. This is
/// the exact counterpart of "the renderer decides nothing" in B.6: the engine
/// receives an already-effective calibration and its signature does not change,
/// so <b>not one line of T1 is modified by this slice</b> (C.4.3, DEC-053).
/// That is the outcome B.2 was written to obtain when it required the engine to
/// read physical values without knowing them.
/// </para>
/// <para>
/// The merge lives here, in Application, and in exactly one place. Two copies
/// of it would be two chances to resolve a sheet against the calibration file
/// while the capacity indicator resolves against the project, or the reverse.
/// </para>
/// <para>
/// <b>This merge validates nothing.</b> It copies fields. Whether a tab is
/// wider than the pawn is checked where DEC-038 put that truth — in the cut
/// outline, when the sheet is calculated — and putting a second copy of the
/// check here is what DEC-056 removed. Loading a project therefore never fails
/// on account of the local calibration: a project is a user's data, a
/// calibration is a machine's, and one never rejects the other.
/// </para>
/// </remarks>
public static class EffectiveCalibration
{
    /// <summary>
    /// The calibration to hand the engine for this project.
    /// </summary>
    /// <remarks>
    /// A null member of <see cref="CalibrationOverrides"/> means "use the
    /// calibration's value"; a set member wins. Everything else in the
    /// calibration is copied through unchanged.
    /// <para>
    /// A consequence to make visible rather than discover, already announced by
    /// DEC-040: <see cref="CalibrationOverrides.TabHeightMm"/> feeds the cell
    /// height of B.5.2, so <b>the capacity of a page now depends on the
    /// project</b>. Two otherwise identical projects with different bases do not
    /// fit the same number of pawns per page, and the capacity indicator of T6
    /// must be computed on the result of this method, never on the calibration
    /// file.
    /// </para>
    /// </remarks>
    /// <param name="calibration">The machine's calibration, as read from disk.</param>
    /// <param name="overrides">The project's overrides. <see cref="CalibrationOverrides.None"/> is the normal case.</param>
    /// <returns>A calibration with the two tab dimensions resolved.</returns>
    public static Calibration Resolve(Calibration calibration, CalibrationOverrides overrides)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(overrides);

        TabAndSocketSettings tab = calibration.Geometry.TabAndSocket;

        TabAndSocketSettings resolvedTab = new(
            TabWidthMm: overrides.TabWidthMm ?? tab.TabWidthMm,
            TabHeightMm: overrides.TabHeightMm ?? tab.TabHeightMm);

        // Only the tab settings can move. Rebuilding the record by hand rather
        // than using a `with` expression is deliberate: a `with` would silently
        // carry through any member added to GeometrySettings later, and the
        // whole point of DEC-053 is that the overridable list is closed and
        // visible. Adding a third overridable dimension must be a visible edit
        // here, not something that happens by itself.
        GeometrySettings resolvedGeometry = new(
            FoldedTent: calibration.Geometry.FoldedTent,
            TabAndSocket: resolvedTab);

        return new Calibration(
            VersionSchema: calibration.VersionSchema,
            Paper: calibration.Paper,
            Sizes: calibration.Sizes,
            Geometry: resolvedGeometry,
            Layout: calibration.Layout,
            Print: calibration.Print,
            Strokes: calibration.Strokes,
            PaperFormats: calibration.PaperFormats);
    }

    /// <summary>The calibration to hand the engine for <paramref name="project"/>.</summary>
    /// <remarks>
    /// The overload that reads best at a call site, and the only one Application
    /// use cases should need.
    /// </remarks>
    public static Calibration Resolve(Calibration calibration, Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return Resolve(calibration, project.CalibrationOverrides);
    }
}
