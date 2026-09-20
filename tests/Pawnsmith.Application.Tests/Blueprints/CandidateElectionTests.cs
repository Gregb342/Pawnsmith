using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Blueprints;

/// <summary>
/// Covers tests 31 and 32 of D.11: DEC-071 and DEC-068.
/// </summary>
public class CandidateElectionTests
{
    private static readonly Guid First = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");
    private static readonly Guid Second = new("0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740");

    private static Project WithTwoCandidates(bool secondCutOut = true, CandidateStatus firstStatus = CandidateStatus.Valid)
    {
        Blueprint blueprint = ProjectFixture.Blueprint(
            candidates:
            [
                ProjectFixture.Candidate(id: First, status: firstStatus),
                ProjectFixture.Candidate(id: Second, status: CandidateStatus.Draft, cutOut: secondCutOut),
            ],
            electedCandidateId: First);

        return ProjectFixture.Project(blueprints: [blueprint]);
    }

    // --- D.11 n° 32 : l'ancien élu garde son statut ------------------------

    [Fact]
    public void ElectingAnotherCandidateLeavesThePreviousOnesStatusUntouched()
    {
        EditedProject edited = CandidateElection.Elect(WithTwoCandidates(), ProjectFixture.BlueprintId, Second);

        edited.Blueprint.ElectedCandidateId.ShouldBe(Second);

        Candidate previous = edited.Blueprint.Candidates.Single(candidate => candidate.Id == First);
        previous.Status.ShouldBe(CandidateStatus.Valid);

        // The new one keeps its status too: election is not validation.
        edited.Blueprint.Candidates.Single(candidate => candidate.Id == Second).Status.ShouldBe(CandidateStatus.Draft);
    }

    [Fact]
    public void ARejectedCandidateCanStillBeElected()
    {
        // Two independent axes, in both directions. Whether the interface
        // should offer it is T6's question; the model does not forbid it.
        Project project = WithTwoCandidates(firstStatus: CandidateStatus.Rejected);

        EditedProject edited = CandidateElection.Elect(project, ProjectFixture.BlueprintId, First);

        edited.Blueprint.ElectedCandidateId.ShouldBe(First);
    }

    // --- D.11 n° 31 : sans détourage, refus codé ---------------------------

    [Fact]
    public void ACandidateWithoutItsCutOutsCannotBeElected()
    {
        Project project = WithTwoCandidates(secondCutOut: false);

        BlueprintRuleException error = Should.Throw<BlueprintRuleException>(() =>
            CandidateElection.Elect(project, ProjectFixture.BlueprintId, Second));

        error.Code.ShouldBe(BlueprintRuleCode.CandidateNotCutOut);
        error.WireCode.ShouldBe("CANDIDATE_NOT_CUT_OUT");
    }

    [Fact]
    public void ACandidateWithOnlyOneCutOutCannotBeElectedEither()
    {
        Candidate halfDone = ProjectFixture.Candidate(id: Second) with { BackImageFile = null };
        Blueprint blueprint = ProjectFixture.Blueprint(candidates: [halfDone]);
        Project project = ProjectFixture.Project(blueprints: [blueprint]);

        BlueprintRuleException error = Should.Throw<BlueprintRuleException>(() =>
            CandidateElection.Elect(project, ProjectFixture.BlueprintId, Second));

        error.Code.ShouldBe(BlueprintRuleCode.CandidateNotCutOut);
        error.Message.ShouldContain("back cut-out");
    }

    [Fact]
    public void StatusNeverRequiredACutOutInTheFirstPlace()
    {
        // DEC-071's other half: a Valid candidate with no cut-out is a
        // perfectly formed candidate. The constraint is on election only.
        Candidate judged = ProjectFixture.Candidate(status: CandidateStatus.Valid, cutOut: false);

        judged.Status.ShouldBe(CandidateStatus.Valid);
        judged.FrontImageFile.ShouldBeNull();
    }

    // --- Identifiants inconnus ---------------------------------------------

    [Fact]
    public void ACandidateOfAnotherBlueprintIsNotFound()
    {
        Project project = WithTwoCandidates();

        BlueprintRuleException error = Should.Throw<BlueprintRuleException>(() =>
            CandidateElection.Elect(project, ProjectFixture.BlueprintId, Guid.NewGuid()));

        error.Code.ShouldBe(BlueprintRuleCode.CandidateNotFound);
        error.WireCode.ShouldBe("CANDIDATE_NOT_FOUND");
    }

    [Fact]
    public void UnelectingClearsTheElectionAndNothingElse()
    {
        EditedProject edited = CandidateElection.Unelect(WithTwoCandidates(), ProjectFixture.BlueprintId);

        edited.Blueprint.ElectedCandidateId.ShouldBeNull();
        edited.Blueprint.Candidates.Count.ShouldBe(2);
        edited.Blueprint.Candidates[0].Status.ShouldBe(CandidateStatus.Valid);
    }
}
