using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Blueprints;

/// <summary>
/// Covers tests 23 to 26 and 28 of D.11: the recomposition rule of DEC-067.
/// Test 27 — that <c>SaveAsync</c> composes nothing — sits with the saver, in
/// the Infrastructure tests.
/// </summary>
public class BlueprintEditorTests
{
    private static readonly IPromptComposer Composer = PromptFixture.Composer();

    /// <summary>The fixture blueprint, whose stored clause is exactly its composition.</summary>
    private static Project Unedited() => ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint()]);

    private static BlueprintFields Fields(string race = "goblin", string details = "one ear torn") =>
        new(race, "skirmisher", Size.Medium,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["weapon"] = "spear" },
            details, 6);

    // --- D.11 n° 23 : non éditée, elle suit -----------------------------------

    [Fact]
    public void ChangingTheRaceOfAnUneditedBlueprintRecomposesTheClause()
    {
        EditedProject edited = BlueprintEditor.UpdateFields(
            Unedited(), ProjectFixture.BlueprintId, Fields(race: "orc"), Composer);

        edited.Blueprint.Race.ShouldBe("orc");
        edited.Blueprint.SubjectClause.ShouldBe("a orc skirmisher, wielding a short spear, one ear torn");
        edited.Diagnostics.ShouldBeEmpty();
    }

    // --- D.11 n° 24 : éditée, elle reste -------------------------------------

    [Fact]
    public void ChangingTheRaceOfAnEditedBlueprintLeavesTheClauseAlone()
    {
        const string handwritten = "a wiry goblin scout, spear in hand, one ear torn off in a brawl";
        Project project = ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint(subjectClause: handwritten)]);

        EditedProject edited = BlueprintEditor.UpdateFields(
            project, ProjectFixture.BlueprintId, Fields(race: "orc"), Composer);

        edited.Blueprint.Race.ShouldBe("orc");
        edited.Blueprint.SubjectClause.ShouldBe(handwritten);
    }

    [Fact]
    public void AnExplicitEditIsStoredNormalisedAndComposesNothing()
    {
        EditedProject edited = BlueprintEditor.EditSubjectClause(
            Unedited(), ProjectFixture.BlueprintId, "  a wiry goblin scout\r\n");

        edited.Blueprint.SubjectClause.ShouldBe("a wiry goblin scout");
        edited.Diagnostics.ShouldBeEmpty();
    }

    // --- D.11 n° 25 : remise à l'identique = non éditée ------------------------

    [Fact]
    public void AnEditThatEqualsTheCompositionCountsAsNoEdit()
    {
        // The user typed exactly what the composer would have produced. There
        // is no way to tell, and no reason to: the next field change produces
        // exactly what they would have typed anyway.
        Project project = ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint(subjectClause: "handwritten")]);

        EditedProject reset = BlueprintEditor.EditSubjectClause(project, ProjectFixture.BlueprintId, ProjectFixture.Subject);
        EditedProject edited = BlueprintEditor.UpdateFields(
            reset.Project, ProjectFixture.BlueprintId, Fields(race: "orc"), Composer);

        edited.Blueprint.SubjectClause.ShouldStartWith("a orc skirmisher");
    }

    // --- D.11 n° 26 : catalogue modifié, rien n'est écrasé --------------------

    [Fact]
    public void AChangedCatalogueFailsTheComparisonAndOverwritesNothing()
    {
        // Same blueprint, but the composer now phrases the head differently:
        // what it produces for the old fields no longer matches the stored
        // clause, so the clause is treated as edited. Wrong on the safe side.
        IPromptComposer changed = PromptFixture.Composer(PromptFixture.Template(head: "one {race} {characterClass}"));

        EditedProject edited = BlueprintEditor.UpdateFields(
            Unedited(), ProjectFixture.BlueprintId, Fields(race: "orc"), changed);

        edited.Blueprint.Race.ShouldBe("orc");
        edited.Blueprint.SubjectClause.ShouldBe(ProjectFixture.Subject);
    }

    // --- D.11 n° 28 : le désalignement suit la recomposition -------------------

    [Fact]
    public void CandidatesAreMisalignedOnTheSubjectAfterARecomposition()
    {
        Candidate frozen = ProjectFixture.Candidate(subjectClauseUsed: ProjectFixture.Subject);
        Blueprint blueprint = ProjectFixture.Blueprint(candidates: [frozen], electedCandidateId: frozen.Id);
        Project project = ProjectFixture.Project(blueprints: [blueprint]);

        EditedProject edited = BlueprintEditor.UpdateFields(
            project, ProjectFixture.BlueprintId, Fields(race: "orc"), Composer);

        IReadOnlySet<ClauseKind> drifted = Misalignment.Of(
            frozen, edited.Blueprint, edited.Project.Style, frozen.FramingClauseUsed);

        drifted.ShouldBe([ClauseKind.Subject]);

        // And nothing else moved: the candidate and the election are intact.
        edited.Blueprint.Candidates.ShouldHaveSingleItem().Status.ShouldBe(CandidateStatus.Valid);
        edited.Blueprint.ElectedCandidateId.ShouldBe(frozen.Id);
    }

    // --- Ajout ------------------------------------------------------------------

    [Fact]
    public void AddingABlueprintComposesItsClauseAndAppendsItLast()
    {
        Project project = Unedited();

        EditedProject edited = BlueprintEditor.Add(project, Fields(race: "ogre", details: ""), Composer);

        edited.Project.Blueprints.Count.ShouldBe(2);
        edited.Project.Blueprints[1].ShouldBe(edited.Blueprint);
        edited.Blueprint.SubjectClause.ShouldBe("a ogre skirmisher, wielding a short spear");
        edited.Blueprint.Candidates.ShouldBeEmpty();
        edited.Blueprint.ElectedCandidateId.ShouldBeNull();
    }

    [Fact]
    public void AddingWithAnUnknownValueSurfacesTheDiagnostic()
    {
        BlueprintFields fields = Fields() with
        {
            OptionalParameters = new Dictionary<string, string>(StringComparer.Ordinal) { ["weapon"] = "halberd" },
        };

        EditedProject edited = BlueprintEditor.Add(Unedited(), fields, Composer);

        edited.Diagnostics.ShouldHaveSingleItem().Value.ShouldBe("halberd");
        edited.Blueprint.SubjectClause.ShouldContain("halberd");
    }

    // --- Position et identité ---------------------------------------------------

    [Fact]
    public void EditingKeepsTheBlueprintAtItsPosition()
    {
        var second = Guid.NewGuid();
        Project project = ProjectFixture.Project(blueprints:
        [
            ProjectFixture.Blueprint(),
            ProjectFixture.Blueprint(id: second, race: "ogre"),
        ]);

        EditedProject edited = BlueprintEditor.UpdateFields(project, second, Fields(race: "troll"), Composer);

        edited.Project.Blueprints.Select(blueprint => blueprint.Id).ShouldBe([ProjectFixture.BlueprintId, second]);
        edited.Project.Blueprints[1].Race.ShouldBe("troll");
    }

    [Fact]
    public void AnUnknownBlueprintIsRefusedWithACode()
    {
        BlueprintRuleException error = Should.Throw<BlueprintRuleException>(() =>
            BlueprintEditor.UpdateFields(Unedited(), Guid.NewGuid(), Fields(), Composer));

        error.Code.ShouldBe(BlueprintRuleCode.BlueprintNotFound);
        error.WireCode.ShouldBe("BLUEPRINT_NOT_FOUND");
    }

    // --- DEC-109 : revenir au texte automatique ------------------------------

    [Fact]
    public void ResettingRecomposesAnEditedClauseAndItFollowsTheFieldsAgain()
    {
        Project project = ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint()]);
        EditedProject edited = BlueprintEditor.EditSubjectClause(project, ProjectFixture.BlueprintId, "a scarred goblin");
        BlueprintEditor.IsSubjectClauseEdited(edited.Blueprint, project.Universe, Composer).ShouldBeTrue();

        EditedProject reset = BlueprintEditor.ResetSubjectClause(edited.Project, ProjectFixture.BlueprintId, Composer);

        reset.Blueprint.SubjectClause.ShouldBe(Composer.ComposeSubject(reset.Blueprint, project.Universe).Clause);
        BlueprintEditor.IsSubjectClauseEdited(reset.Blueprint, project.Universe, Composer).ShouldBeFalse();
    }
}

