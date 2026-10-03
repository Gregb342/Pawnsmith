namespace Pawnsmith.Application.Blueprints;

/// <summary>
/// The business rules a blueprint operation can refuse on, as codes.
/// </summary>
/// <remarks>
/// These are Application's own, not Infrastructure's: nothing here is about a
/// file. A rule of chapter 16 that refuses — electing a candidate without its
/// cut-outs — refuses <i>here</i>, where the rule lives, and T6 gets a code to
/// translate rather than a message to parse (chapter 10).
/// </remarks>
public enum BlueprintRuleCode
{
    /// <summary>No blueprint of the project carries this identifier.</summary>
    BlueprintNotFound,

    /// <summary>No candidate of this blueprint carries this identifier.</summary>
    CandidateNotFound,

    /// <summary>Electing a candidate that lacks a front or back cut-out (DEC-071).</summary>
    CandidateNotCutOut,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class BlueprintRuleCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in D.10.</summary>
    public static string ToWireCode(this BlueprintRuleCode code) => code switch
    {
        BlueprintRuleCode.BlueprintNotFound => "BLUEPRINT_NOT_FOUND",
        BlueprintRuleCode.CandidateNotFound => "CANDIDATE_NOT_FOUND",
        BlueprintRuleCode.CandidateNotCutOut => "CANDIDATE_NOT_CUT_OUT",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A blueprint operation that refuses, with its code and message.</summary>
public sealed class BlueprintRuleException : Exception, ICodedException
{
    public BlueprintRuleException(BlueprintRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Which rule refused.</summary>
    public BlueprintRuleCode Code { get; }

    /// <summary>The code in the form D.10 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
