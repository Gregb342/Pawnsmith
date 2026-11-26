using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Projects;

/// <summary>Universe and style frozen at the first proposal (§I.8.1, DEC-112).</summary>
public class ProjectSettingsEditorTests
{
    private static ProjectSettings SettingsOf(Project project) => new(
        project.Name, project.Universe, project.Geometry, project.PaperFormatName, project.Style, project.CalibrationOverrides);

    private static Project WithProposal() => ProjectFixture.Project() with
    {
        Blueprints = [ProjectFixture.Blueprint(candidates: [ProjectFixture.Candidate()])],
    };

    [Fact]
    public void WithoutProposalsEverythingChanges()
    {
        Project project = ProjectFixture.Project() with { Blueprints = [ProjectFixture.Blueprint()] };

        Project changed = ProjectSettingsEditor.Apply(project, SettingsOf(project) with { Style = ProjectFixture.Style("ink drawing") });

        changed.Style.StyleClause.ShouldBe("ink drawing");
        ProjectSettingsEditor.IsFrozen(project).ShouldBeFalse();
    }

    [Fact]
    public void WithAProposalTheStyleIsRefused()
    {
        Project project = WithProposal();

        ProjectRuleException error = Should.Throw<ProjectRuleException>(() =>
            ProjectSettingsEditor.Apply(project, SettingsOf(project) with { Style = project.Style with { Palette = "red" } }));

        error.WireCode.ShouldBe("STYLE_FROZEN");
    }

    [Fact]
    public void WithAProposalTheNameGeometryPaperAndTabsStillChange()
    {
        Project project = WithProposal();

        Project changed = ProjectSettingsEditor.Apply(project, SettingsOf(project) with
        {
            Name = "Renamed",
            Geometry = Geometry.FoldedTent,
            PaperFormatName = "Letter",
            CalibrationOverrides = new CalibrationOverrides(14, 9),
        });

        changed.Geometry.ShouldBe(Geometry.FoldedTent);
        changed.Name.ShouldBe("Renamed");
        changed.CalibrationOverrides.TabWidthMm.ShouldBe(14);
    }

    [Fact]
    public void SendingBackTheSameStyleIsNotAChange()
    {
        // The front sends every field at each save (DEC-111); an unchanged
        // style must pass, field by field, palette included.
        Project project = WithProposal();

        Should.NotThrow(() => ProjectSettingsEditor.Apply(project, SettingsOf(project) with
        {
            Style = new Style(project.Style.Name, project.Style.StyleClause, project.Style.NegativeClause, project.Style.Palette),
        }));
    }
}
