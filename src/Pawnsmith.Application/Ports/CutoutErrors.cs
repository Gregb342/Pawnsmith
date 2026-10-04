namespace Pawnsmith.Application.Ports;

/// <summary>Why a cut-out failed, as a code (§F.6, DEC-103).</summary>
/// <remarks>
/// Declared in Application, like <see cref="GeneratorErrorCode"/>: the batch
/// has to catch it, the on-demand use case raises one of them itself, and
/// Application cannot see Infrastructure (A.3). The wire strings are written
/// out, so renaming a member cannot change what an API returns.
/// </remarks>
public enum CutoutErrorCode
{
    /// <summary>Not a PNG, a PNG outside the subset read (§F.2.1), or a damaged one.</summary>
    ImageInvalid,

    /// <summary>A side beyond the bound, read on the header (MEN-005).</summary>
    ImageTooLarge,

    /// <summary>The border of the image is not a uniform background (§F.3.1).</summary>
    BackgroundNotUniform,

    /// <summary>Nothing, or almost nothing, is left once the background is removed (§F.3.6).</summary>
    SubjectNotFound,

    /// <summary>The candidate has no paired image to cut out (§F.5.3).</summary>
    NoPairedImage,
}

/// <summary>Turns a code into the string an API returns.</summary>
public static class CutoutErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in §F.6.</summary>
    public static string ToWireCode(this CutoutErrorCode code) => code switch
    {
        CutoutErrorCode.ImageInvalid => "CUTOUT_IMAGE_INVALID",
        CutoutErrorCode.ImageTooLarge => "CUTOUT_IMAGE_TOO_LARGE",
        CutoutErrorCode.BackgroundNotUniform => "CUTOUT_BACKGROUND_NOT_UNIFORM",
        CutoutErrorCode.SubjectNotFound => "CUTOUT_SUBJECT_NOT_FOUND",
        CutoutErrorCode.NoPairedImage => "CANDIDATE_NO_PAIRED_IMAGE",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A cut-out that failed, with its code and message.</summary>
public sealed class CutoutException : Exception, ICodedException
{
    public CutoutException(CutoutErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public CutoutException(CutoutErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Why it failed, as a code rather than as text.</summary>
    public CutoutErrorCode Code { get; }

    /// <summary>The code in the form §F.6 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
