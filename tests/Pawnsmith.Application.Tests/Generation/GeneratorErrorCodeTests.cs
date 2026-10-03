using Pawnsmith.Application;
using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Ports;

namespace Pawnsmith.Application.Tests.Generation;

/// <summary>The generation codes of E.11, and the interface that carries a code across layers.</summary>
public class GeneratorErrorCodeTests
{
    [Fact]
    public void EveryCodeHasItsWireString()
    {
        foreach (GeneratorErrorCode code in Enum.GetValues<GeneratorErrorCode>())
        {
            code.ToWireCode().ShouldStartWith("GENERATOR_");
        }
    }

    [Theory]
    [InlineData(GeneratorErrorCode.Unreachable, "GENERATOR_UNREACHABLE")]
    [InlineData(GeneratorErrorCode.Timeout, "GENERATOR_TIMEOUT")]
    [InlineData(GeneratorErrorCode.Rejected, "GENERATOR_REJECTED")]
    [InlineData(GeneratorErrorCode.Failed, "GENERATOR_FAILED")]
    [InlineData(GeneratorErrorCode.OutputInvalid, "GENERATOR_OUTPUT_INVALID")]
    public void TheWireStringsAreThoseOfE11(GeneratorErrorCode code, string wire)
    {
        code.ToWireCode().ShouldBe(wire);
    }

    [Fact]
    public void ACodedExceptionIsReadableWithoutItsType()
    {
        // What the batch does with an exception from a layer it cannot see.
        Exception[] errors =
        [
            new GeneratorException(GeneratorErrorCode.Timeout, "slow"),
            new BlueprintRuleException(BlueprintRuleCode.BlueprintNotFound, "gone"),
        ];

        errors.Select(error => ((ICodedException)error).WireCode)
            .ShouldBe(["GENERATOR_TIMEOUT", "BLUEPRINT_NOT_FOUND"]);
    }
}
