using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Blueprints;

/// <summary>
/// The judgement of §G.6.1: the status moves, the election and the files do
/// not (DEC-068, DEC-071). Supports test 12 of G.13.
/// </summary>
public class CandidateJudgementTests
{
    private static readonly Guid First = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");
    private static readonly Guid Second = new("0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740");

    private static Project TwoCandidates() => ProjectFixture.Project(blueprints:
    [
        ProjectFixture.Blueprint(
            candidates:
            [
                ProjectFixture.Candidate(id: First, status: CandidateStatus.Valid),
                ProjectFixture.Candidate(id: Second, status: CandidateStatus.Draft, cutOut: false),
            ],
            electedCandidateId: First),
    ]);

    [Fact]
    public void OnlyTheStatusOfThatCandidateMoves()
    {
        EditedProject judged = CandidateJudgement.SetStatus(TwoCandidates(), ProjectFixture.BlueprintId, Second, CandidateStatus.Valid);

        judged.Blueprint.Candidates.Select(candidate => candidate.Status).ShouldBe([CandidateStatus.Valid, CandidateStatus.Valid]);
        judged.Blueprint.Candidates.Select(candidate => candidate.Id).ShouldBe([First, Second]);
        judged.Blueprint.ElectedCandidateId.ShouldBe(First);
    }

    [Fact]
    public void RejectingTheElectedCandidateLeavesItElected()
    {
        // Two independent axes (DEC-068): the next election is the user's act.
        EditedProject judged = CandidateJudgement.SetStatus(TwoCandidates(), ProjectFixture.BlueprintId, First, CandidateStatus.Rejected);

        judged.Blueprint.ElectedCandidateId.ShouldBe(First);
        judged.Blueprint.Candidates[0].Status.ShouldBe(CandidateStatus.Rejected);
    }

    [Fact]
    public void AStatusRequiresNoFile()
    {
        // The second candidate has no cut-out; validating it is allowed (DEC-071).
        EditedProject judged = CandidateJudgement.SetStatus(TwoCandidates(), ProjectFixture.BlueprintId, Second, CandidateStatus.Valid);

        judged.Blueprint.Candidates[1].FrontImageFile.ShouldBeNull();
        judged.Blueprint.Candidates[1].Status.ShouldBe(CandidateStatus.Valid);
    }

    [Fact]
    public void AnUnknownCandidateIsRefusedWithItsCode()
    {
        BlueprintRuleException error = Should.Throw<BlueprintRuleException>(() =>
            CandidateJudgement.SetStatus(TwoCandidates(), ProjectFixture.BlueprintId, Guid.NewGuid(), CandidateStatus.Valid));

        error.WireCode.ShouldBe("CANDIDATE_NOT_FOUND");
    }

    [Fact]
    public void AnUnknownBlueprintIsRefusedWithItsCode()
    {
        Should.Throw<BlueprintRuleException>(() =>
            CandidateJudgement.SetStatus(TwoCandidates(), Guid.NewGuid(), First, CandidateStatus.Valid))
            .WireCode.ShouldBe("BLUEPRINT_NOT_FOUND");
    }

    [Fact]
    public void AValueThatIsNotAStatusIsRefused()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            CandidateJudgement.SetStatus(TwoCandidates(), ProjectFixture.BlueprintId, First, (CandidateStatus)42));
    }
}
