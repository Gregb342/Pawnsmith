namespace Pawnsmith.Application.Generation;

/// <summary>The reasons a batch is refused before it starts, as codes (§E.11).</summary>
/// <remarks>
/// One member today. It is an enumeration all the same, for the reason every
/// code of the repository is one: the wire string is written out once, and an
/// API maps a code, never a message.
/// </remarks>
public enum GenerationRuleCode
{
    /// <summary>An empty batch, or one larger than <see cref="GenerationOptions.MaxBatchSize"/> (MEN-007).</summary>
    BatchSizeInvalid,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class GenerationRuleCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in E.11.</summary>
    public static string ToWireCode(this GenerationRuleCode code) => code switch
    {
        GenerationRuleCode.BatchSizeInvalid => "BATCH_SIZE_INVALID",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A batch refused before any job exists, with its code and message.</summary>
public sealed class GenerationRuleException : Exception, ICodedException
{
    public GenerationRuleException(GenerationRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>Which rule refused.</summary>
    public GenerationRuleCode Code { get; }

    /// <summary>The code in the form E.11 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
