using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Tests.Fixtures;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// Covers tests 4 to 10 of C.12: the comparison rule of C.5.4, and the two
/// things misalignment is not.
/// </summary>
public class MisalignmentTests
{
    private static IReadOnlySet<ClauseKind> Drifted(
        Candidate candidate,
        Blueprint? blueprint = null,
        Style? style = null,
        string framingClause = ProjectFixture.Framing)
    {
        return Misalignment.Of(
            candidate,
            blueprint ?? ProjectFixture.Blueprint(),
            style ?? ProjectFixture.Style(),
            framingClause);
    }

    // --- C.12 n° 4 : trois clauses identiques, aucun désalignement ---------

    [Fact]
    public void ACandidateWhoseThreeClausesStillMatchIsAligned()
    {
        Candidate candidate = ProjectFixture.Candidate();

        Drifted(candidate).ShouldBeEmpty();
        Misalignment.IsMisaligned(
            candidate,
            ProjectFixture.Blueprint(),
            ProjectFixture.Style(),
            ProjectFixture.Framing).ShouldBeFalse();
    }

    // --- C.12 n° 5, 6, 7 : le jeu rendu nomme exactement la clause fautive -

    [Fact]
    public void ChangingTheProjectStyleDriftsTheStyleClauseAndOnlyThat()
    {
        Style edited = ProjectFixture.Style("a completely different rendering");

        Drifted(ProjectFixture.Candidate(), style: edited)
            .ShouldBeExactly(ClauseKind.Style);
    }

    [Fact]
    public void EditingTheSubjectClauseDriftsTheSubjectClauseAndOnlyThat()
    {
        Blueprint edited = ProjectFixture.Blueprint(subjectClause: "a goblin skirmisher with a bow");

        Drifted(ProjectFixture.Candidate(), blueprint: edited)
            .ShouldBeExactly(ClauseKind.Subject);
    }

    [Fact]
    public void EditingTheWorkflowTemplateDriftsTheFramingClauseAndOnlyThat()
    {
        // This is the case of the advanced user adjusting their ComfyUI
        // workflow template, which DEC-029 leaves open on purpose. It misaligns
        // their whole library, in every project, at once — which is correct,
        // and only bearable because the interface can name the culprit.
        Drifted(ProjectFixture.Candidate(), framingClause: "three-quarter view, single figure")
            .ShouldBeExactly(ClauseKind.Framing);
    }

    [Fact]
    public void ThreeChangesAtOnceAreAllReported()
    {
        IReadOnlySet<ClauseKind> drifted = Drifted(
            ProjectFixture.Candidate(),
            blueprint: ProjectFixture.Blueprint(subjectClause: "an ogre"),
            style: ProjectFixture.Style("charcoal sketch"),
            framingClause: "three-quarter view");

        drifted.ShouldBeExactly(ClauseKind.Framing, ClauseKind.Subject, ClauseKind.Style);
    }

    // --- C.12 n° 8 : le calcul ne touche à rien ----------------------------

    [Fact]
    public void AValidCandidateThatBecomesMisalignedStaysValid()
    {
        // The natural mistake at this spot is to fold misalignment into the
        // status enumeration. Doing so would make "rejected by the user"
        // indistinguishable from "produced under a style that is no longer the
        // project's" (DEC-030).
        Candidate candidate = ProjectFixture.Candidate(status: CandidateStatus.Valid);
        Style edited = ProjectFixture.Style("charcoal sketch");

        Drifted(candidate, style: edited).ShouldContain(ClauseKind.Style);
        candidate.Status.ShouldBe(CandidateStatus.Valid);
    }

    [Fact]
    public void TheCalculationLeavesTheBlueprintElectionAlone()
    {
        Blueprint blueprint = ProjectFixture.Blueprint();
        Candidate elected = blueprint.Candidates[0];

        Misalignment.Of(elected, blueprint, ProjectFixture.Style("something else"), ProjectFixture.Framing);

        blueprint.ElectedCandidateId.ShouldBe(elected.Id);
        blueprint.Candidates[0].ShouldBe(elected);
    }

    // --- C.12 n° 9 : les paramètres de rendu ne désalignent rien -----------

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    [InlineData(Geometry.NoSupport)]
    public void ChangingTheGeometryMisalignsNothing(Geometry geometry)
    {
        Project project = ProjectFixture.Project(geometry: geometry);

        DriftedIn(project).ShouldBeEmpty();
    }

    [Fact]
    public void ChangingThePaperFormatMisalignsNothing()
    {
        Project project = ProjectFixture.Project(paperFormatName: "Letter");

        DriftedIn(project).ShouldBeEmpty();
    }

    [Fact]
    public void ChangingTheQuantityMisalignsNothing()
    {
        // Geometry, paper format and quantity are rendering parameters
        // (DEC-030, DEC-004): they decide how a pawn is printed, never what the
        // model was asked to draw.
        Project project = ProjectFixture.Project(blueprint: ProjectFixture.Blueprint(quantity: 40));

        DriftedIn(project).ShouldBeEmpty();
    }

    // --- C.12 n° 10 : la seule tolérance est l'espace de bord --------------

    [Fact]
    public void ATrailingSpaceAloneDoesNotMisalign()
    {
        // Sound, because the assembly trims it too: the prompt actually sent is
        // identical either way (C.5.4).
        Candidate candidate = ProjectFixture.Candidate(
            styleClauseUsed: ProjectFixture.StyleClause + "  \n");

        Drifted(candidate).ShouldBeEmpty();
    }

    [Fact]
    public void ADifferenceOfCaseMisaligns()
    {
        Candidate candidate = ProjectFixture.Candidate(
            styleClauseUsed: ProjectFixture.StyleClause.ToUpperInvariant());

        Drifted(candidate).ShouldBeExactly(ClauseKind.Style);
    }

    [Fact]
    public void AnInternalSpaceMisaligns()
    {
        // A diffusion model is not indifferent to either, so neither is the
        // comparison. No leniency beyond the edge whitespace above.
        Candidate candidate = ProjectFixture.Candidate(
            styleClauseUsed: ProjectFixture.StyleClause.Replace(" with ", "  with  ", StringComparison.Ordinal));

        Drifted(candidate).ShouldBeExactly(ClauseKind.Style);
    }

    /// <summary>Every drifted clause of every candidate of a project.</summary>
    private static IReadOnlySet<ClauseKind> DriftedIn(Project project)
    {
        HashSet<ClauseKind> drifted = [];

        foreach (Blueprint blueprint in project.Blueprints)
        {
            foreach (Candidate candidate in blueprint.Candidates)
            {
                drifted.UnionWith(
                    Misalignment.Of(candidate, blueprint, project.Style, ProjectFixture.Framing));
            }
        }

        return drifted;
    }
}

/// <summary>Set assertions for the clause tests.</summary>
internal static class ClauseSetAssertions
{
    /// <summary>Asserts the exact content of a set, whatever its enumeration order.</summary>
    /// <remarks>
    /// Shouldly's <c>ShouldBe</c> compares collections in order, and the
    /// enumeration order of a <c>HashSet</c> is not guaranteed. Comparing as
    /// sets makes the assertion say what it means, and stops a passing test
    /// from depending on an implementation detail of the framework.
    /// </remarks>
    public static void ShouldBeExactly(
        this IReadOnlySet<ClauseKind> actual,
        params ClauseKind[] expected)
    {
        actual.SetEquals(expected).ShouldBeTrue(
            $"Expected exactly [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}].");
    }
}
