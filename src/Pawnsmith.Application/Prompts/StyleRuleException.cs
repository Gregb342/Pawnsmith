namespace Pawnsmith.Application.Prompts;

/// <summary>Why a change to the style library was refused (§I.7.2).</summary>
public enum StyleRuleCode
{
    /// <summary>An empty name or style clause.</summary>
    StyleInvalid,

    /// <summary>No style has this identifier.</summary>
    StyleNotFound,

    /// <summary>A shipped style, which the interface does not remove.</summary>
    StyleShipped,
}

public static class StyleRuleCodeExtensions
{
    public static string ToWireCode(this StyleRuleCode code) => code switch
    {
        StyleRuleCode.StyleInvalid => "STYLE_INVALID",
        StyleRuleCode.StyleNotFound => "STYLE_NOT_FOUND",
        StyleRuleCode.StyleShipped => "STYLE_SHIPPED",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

public sealed class StyleRuleException : Exception, ICodedException
{
    public StyleRuleException(StyleRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public StyleRuleCode Code { get; }

    public string WireCode => Code.ToWireCode();
}
