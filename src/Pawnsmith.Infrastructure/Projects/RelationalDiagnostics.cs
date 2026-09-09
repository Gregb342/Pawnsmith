using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The mismatches that need a calibration to see, and a disk to see none of.
/// </summary>
/// <remarks>
/// <para>
/// These are the relational half of DEC-056: they confront a project with the
/// machine it happens to be sitting on, and <b>none of them refuses anything.</b>
/// A paper format this calibration does not declare, a tab wider than these
/// pawns — take the same file to another machine and both may be fine. <b>A
/// project opens to be corrected, not to crash.</b>
/// </para>
/// <para>
/// <b>Why they live here rather than in the reader that first needed them.</b>
/// A load and an import both have to produce them, and the two have nothing else
/// in common: one is handed a folder, the other a ZIP. Leaving them in
/// <see cref="ProjectReader"/> would have meant either an importer that reads
/// folders — which it does not, it reads an archive — or the same rule written
/// twice, which is how two answers to one question start to drift.
/// </para>
/// <para>
/// The third diagnostic, a referenced image that is not on this disk, is
/// deliberately <b>not</b> here. It is the one that needs a file system, and it
/// belongs to the load alone: an import cannot produce it, because C.9.1 step 6
/// has already refused any archive whose project names a file the archive does
/// not carry. What is a tolerated hole in a folder is a refusal in an archive,
/// and that is the same asymmetry the export carries in the other direction.
/// </para>
/// </remarks>
public static class RelationalDiagnostics
{
    /// <summary>Everything this machine has to say about a project it did not produce.</summary>
    public static IReadOnlyList<ProjectDiagnostic> For(Project project, Calibration calibration)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(calibration);

        List<ProjectDiagnostic> diagnostics = [];

        CollectPaperFormat(project, calibration, diagnostics);
        CollectOverrides(project, calibration, diagnostics);

        return diagnostics;
    }

    /// <summary>Adds the same diagnostics to a list the caller is already building.</summary>
    /// <remarks>
    /// The load collects its image diagnostics in the same pass and wants one
    /// list in the end, so it appends rather than concatenating two.
    /// </remarks>
    public static void AddTo(List<ProjectDiagnostic> diagnostics, Project project, Calibration calibration)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(calibration);

        CollectPaperFormat(project, calibration, diagnostics);
        CollectOverrides(project, calibration, diagnostics);
    }

    private static void CollectPaperFormat(
        Project project,
        Calibration calibration,
        List<ProjectDiagnostic> diagnostics)
    {
        if (calibration.PaperFormats.ContainsKey(project.PaperFormatName))
        {
            return;
        }

        diagnostics.Add(new ProjectDiagnostic(
            ProjectDiagnosticKind.UnknownPaperFormat,
            "paperFormat",
            $"'{project.PaperFormatName}' is not a paper format this calibration declares. " +
            $"Known formats are: {string.Join(", ", calibration.PaperFormats.Keys)}. " +
            "The project opens all the same; producing a sheet will not."));
    }

    private static void CollectOverrides(
        Project project,
        Calibration calibration,
        List<ProjectDiagnostic> diagnostics)
    {
        // Conditioned on the geometry, and this is the whole point of the check.
        // Only TabAndSocket has a tab, so only there can the value be read and
        // only there can it hurt. Warning a NoSupport project would be a warning
        // nothing could ever clear (C.4.4).
        if (project.Geometry != Geometry.TabAndSocket)
        {
            return;
        }

        if (project.CalibrationOverrides.TabWidthMm is not { } tabWidthMm)
        {
            return;
        }

        // One diagnostic per size actually used by a blueprint, not per size the
        // calibration declares: a project holding only Medium pawns has no
        // business being warned about Gargantuan ones.
        foreach (Size size in project.Blueprints.Select(blueprint => blueprint.Size).Distinct())
        {
            if (!calibration.Sizes.TryGetValue(size, out PawnDimensions? dimensions)
                || tabWidthMm <= dimensions.PawnWidthMm)
            {
                continue;
            }

            diagnostics.Add(new ProjectDiagnostic(
                ProjectDiagnosticKind.OverrideExceedsPawnWidth,
                "calibrationOverrides.tabWidthMm",
                $"The tab is {tabWidthMm} mm wide and a {size} pawn is {dimensions.PawnWidthMm} mm. " +
                "The project opens; calculating a sheet from it will fail, because a tab wider " +
                "than the pawn turns the cut outline inside out."));
        }
    }
}
