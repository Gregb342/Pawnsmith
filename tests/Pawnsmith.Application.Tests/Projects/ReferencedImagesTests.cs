using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Projects;

/// <summary>The whitelist of DEC-088. Supports test 15 of G.13.</summary>
public class ReferencedImagesTests
{
    private static readonly Project Project = ProjectFixture.Project(blueprints:
        [ProjectFixture.Blueprint(candidates: [ProjectFixture.Candidate()])]);

    [Theory]
    [InlineData("pair")]
    [InlineData("front")]
    [InlineData("back")]
    public void AnImageACandidateReferencesIsFound(string role)
    {
        string fileName = $"{ProjectFixture.CandidateId}-{role}.png";

        ReferencedImages.Find(Project, fileName).ShouldBe($"images/{fileName}");
    }

    [Theory]
    [InlineData("orphan.png")]
    [InlineData("../project.json")]
    [InlineData("")]
    public void AnythingElseIsNotFound(string fileName)
    {
        ReferencedImages.Find(Project, fileName).ShouldBeNull();
    }

    [Fact]
    public void TheComparisonIsExactCaseIncluded()
    {
        string upper = $"{ProjectFixture.CandidateId}-PAIR.png";

        ReferencedImages.Find(Project, upper).ShouldBeNull();
    }
}
