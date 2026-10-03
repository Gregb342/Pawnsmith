using Pawnsmith.Infrastructure.Generation;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>The wire codes of E.11 that the configuration readers raise.</summary>
public class GeneratorConfigErrorCodeTests
{
    [Fact]
    public void EveryCodeHasItsWireString()
    {
        // Walks the enumeration, so a member added without a wire string fails
        // here rather than at the first API call that meets it.
        foreach (GeneratorConfigErrorCode code in Enum.GetValues<GeneratorConfigErrorCode>())
        {
            code.ToWireCode().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData(GeneratorConfigErrorCode.WorkflowInvalid, "WORKFLOW_INVALID")]
    [InlineData(GeneratorConfigErrorCode.WorkflowUnknownToken, "WORKFLOW_UNKNOWN_TOKEN")]
    [InlineData(GeneratorConfigErrorCode.WorkflowSchemaTooRecent, "WORKFLOW_SCHEMA_TOO_RECENT")]
    [InlineData(GeneratorConfigErrorCode.GeneratorUrlInvalid, "GENERATOR_URL_INVALID")]
    public void TheWireStringsAreThoseOfE11(GeneratorConfigErrorCode code, string wire)
    {
        code.ToWireCode().ShouldBe(wire);
    }
}
