namespace Pawnsmith.Application.Prompts;

/// <summary>Why a change to the personal catalogue was refused (§I.4.3).</summary>
public enum CatalogRuleCode
{
    /// <summary>An unknown key, or an empty value, fragment or label: the entry is not complete.</summary>
    EntryInvalid,

    /// <summary>The key already has this value, shipped or personal.</summary>
    EntryDuplicate,

    /// <summary>The key has no such value.</summary>
    EntryNotFound,

    /// <summary>A shipped entry, which the interface does not remove.</summary>
    EntryShipped,
}

public static class CatalogRuleCodeExtensions
{
    public static string ToWireCode(this CatalogRuleCode code) => code switch
    {
        CatalogRuleCode.EntryInvalid => "CATALOG_ENTRY_INVALID",
        CatalogRuleCode.EntryDuplicate => "CATALOG_ENTRY_DUPLICATE",
        CatalogRuleCode.EntryNotFound => "CATALOG_ENTRY_NOT_FOUND",
        CatalogRuleCode.EntryShipped => "CATALOG_ENTRY_SHIPPED",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

public sealed class CatalogRuleException : Exception, ICodedException
{
    public CatalogRuleException(CatalogRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public CatalogRuleCode Code { get; }

    public string WireCode => Code.ToWireCode();
}
