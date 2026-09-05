using Pawnsmith.Application.PhysicalValues;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.PhysicalValues;

/// <summary>
/// Covers tests 11 and 13 of C.12: the resolution of project overrides, and the
/// coupling between a tab dimension and page capacity.
/// </summary>
public class EffectiveCalibrationTests
{
    // --- C.12 n° 11 : null prend la calibration, renseigné prend le projet --

    [Fact]
    public void NoOverrideLeavesTheTabDimensionsAlone()
    {
        Calibration resolved = EffectiveCalibration.Resolve(
            CalibrationFixture.Calibration(),
            CalibrationOverrides.None);

        resolved.Geometry.TabAndSocket.TabWidthMm.ShouldBe(CalibrationFixture.TabWidthMm);
        resolved.Geometry.TabAndSocket.TabHeightMm.ShouldBe(CalibrationFixture.TabHeightMm);
    }

    [Fact]
    public void ASetMemberWinsOverTheCalibration()
    {
        Calibration resolved = EffectiveCalibration.Resolve(
            CalibrationFixture.Calibration(),
            new CalibrationOverrides(TabWidthMm: 9.5, TabHeightMm: 14.0));

        resolved.Geometry.TabAndSocket.TabWidthMm.ShouldBe(9.5);
        resolved.Geometry.TabAndSocket.TabHeightMm.ShouldBe(14.0);
    }

    [Fact]
    public void OneMemberCanBeOverriddenWithoutTheOther()
    {
        // The everyday case: someone knows the slot width of the bases they
        // own and has no opinion at all on the depth.
        Calibration resolved = EffectiveCalibration.Resolve(
            CalibrationFixture.Calibration(),
            new CalibrationOverrides(TabWidthMm: 9.5, TabHeightMm: null));

        resolved.Geometry.TabAndSocket.TabWidthMm.ShouldBe(9.5);
        resolved.Geometry.TabAndSocket.TabHeightMm.ShouldBe(CalibrationFixture.TabHeightMm);
    }

    [Fact]
    public void EverythingElseIsCopiedThroughUnchanged()
    {
        Calibration baseline = CalibrationFixture.Calibration();

        Calibration resolved = EffectiveCalibration.Resolve(
            baseline,
            new CalibrationOverrides(TabWidthMm: 9.5, TabHeightMm: 14.0));

        resolved.VersionSchema.ShouldBe(baseline.VersionSchema);
        resolved.Paper.ShouldBe(baseline.Paper);
        resolved.Sizes.ShouldBe(baseline.Sizes);
        resolved.Layout.ShouldBe(baseline.Layout);
        resolved.Print.ShouldBe(baseline.Print);
        resolved.Strokes.ShouldBe(baseline.Strokes);
        resolved.PaperFormats.ShouldBe(baseline.PaperFormats);

        // The other geometry is untouched: the closed list of DEC-053 holds two
        // members, and the flap of the folded tent is not one of them.
        resolved.Geometry.FoldedTent.ShouldBe(baseline.Geometry.FoldedTent);
    }

    [Fact]
    public void TheOriginalCalibrationIsNotMutated()
    {
        // Records are immutable, so this cannot fail today. It is here because
        // the merge is the one place where a later edit might reach for a
        // `with` expression on a shared instance — and the same calibration
        // object is handed to every project, so that is the day it would bite.
        Calibration baseline = CalibrationFixture.Calibration();

        EffectiveCalibration.Resolve(baseline, new CalibrationOverrides(9.5, 14.0));

        baseline.Geometry.TabAndSocket.TabWidthMm.ShouldBe(CalibrationFixture.TabWidthMm);
        baseline.Geometry.TabAndSocket.TabHeightMm.ShouldBe(CalibrationFixture.TabHeightMm);
    }

    [Fact]
    public void TheProjectOverloadResolvesTheProjectsOwnOverrides()
    {
        Project project = ProjectFixture.Project(
            new CalibrationOverrides(TabWidthMm: 9.5, TabHeightMm: null));

        Calibration resolved = EffectiveCalibration.Resolve(CalibrationFixture.Calibration(), project);

        resolved.Geometry.TabAndSocket.TabWidthMm.ShouldBe(9.5);
        resolved.Geometry.TabAndSocket.TabHeightMm.ShouldBe(CalibrationFixture.TabHeightMm);
    }

    // --- C.12 n° 13 : la capacité d'une page dépend désormais du projet ----

    [Fact]
    public void ATallerTabCostsRowsOnThePage()
    {
        // Locks the coupling DEC-040 announced and C.4.3 repeats.
        //
        // An A4 page holds twelve Medium tab-and-socket pawns with a 10 mm tab:
        // the unfolded unit is 2 x (50 + 10) = 120 mm tall, and two rows fit in
        // the 263 mm of usable height (297 - 2 x 10 margin - 14 calibration
        // zone). Six columns of 25.4 mm fit in the 190 mm of usable width.
        //
        // Take the tab to 25 mm and the unit becomes 2 x (50 + 25) = 150 mm, so
        // a single row fits and half the page is gone. Same size, same paper,
        // same geometry: the project alone made the difference.
        Calibration baseline = CalibrationFixture.Calibration();
        Calibration deeper = EffectiveCalibration.Resolve(
            baseline,
            new CalibrationOverrides(TabWidthMm: null, TabHeightMm: 25.0));

        CalibrationFixture.CapacityOnA4(baseline, Size.Medium, Geometry.TabAndSocket).ShouldBe(12);
        CalibrationFixture.CapacityOnA4(deeper, Size.Medium, Geometry.TabAndSocket).ShouldBe(6);
    }

    [Fact]
    public void OverridingTheTabChangesNothingForTheOtherTwoGeometries()
    {
        // Not asked for by C.12, and worth asserting anyway: it is the fact
        // C.4.4 leans on when it emits the relational diagnostic for
        // TabAndSocket only. FoldedTent lays a flap across the full pawn width
        // and NoSupport lays nothing at all (DEC-039), so neither ever reads a
        // tab dimension — and a warning about one would be a warning that no
        // value could ever clear.
        Calibration baseline = CalibrationFixture.Calibration();
        Calibration overridden = EffectiveCalibration.Resolve(
            baseline,
            new CalibrationOverrides(TabWidthMm: 9.5, TabHeightMm: 25.0));

        foreach (Geometry geometry in new[] { Geometry.FoldedTent, Geometry.NoSupport })
        {
            CalibrationFixture.CapacityOnA4(overridden, Size.Medium, geometry)
                .ShouldBe(CalibrationFixture.CapacityOnA4(baseline, Size.Medium, geometry));
        }
    }
}
