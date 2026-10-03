namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// Where the ComfyUI server is, and the bounds the client refuses to go past
/// (§E.9, DEC-080).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the single named place the default values live</b>, as DEC-057
/// asks for the bounds of T2: no bound appears as a literal in the code that
/// applies it. They are arbitrated, not measured — the rule that physical values
/// are never invented does not apply to them.
/// </para>
/// <para>
/// The batch-size cap is not here but in <c>GenerationOptions</c>, in
/// Application: it is a rule of the application, whereas these are properties
/// of this adapter. A remote provider (EVO-002) would have other timeouts and
/// the same cap.
/// </para>
/// </remarks>
/// <param name="GeneratorUrl">
/// The server's address, as the operator wrote it. Checked when the client is
/// built, never at the first batch (DEC-081). A deployment setting: the API
/// never changes it.
/// </param>
public sealed record ComfyUiOptions(string GeneratorUrl)
{
    /// <summary>The longest a whole generation may take, queue included.</summary>
    /// <remarks>
    /// A generation measures 34 to 42 s (DEC-043), but the <b>first</b> batch
    /// after ComfyUI starts loads a 12-billion-parameter model. Ten minutes cover
    /// a slow load without letting a stuck generator hold a batch forever.
    /// </remarks>
    public TimeSpan GenerationTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>The longest a single HTTP call may take.</summary>
    /// <remarks>No answer of ComfyUI is slow to produce: the wait is in the queue, not in the call.</remarks>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>The longest a health check may take.</summary>
    /// <remarks>A state of health that takes more than five seconds to answer is an answer.</remarks>
    public TimeSpan CheckTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>The pause between two looks at the history.</summary>
    /// <remarks>A second of delay on forty is invisible; a hundred requests a second would not be, to ComfyUI.</remarks>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>The heaviest image accepted.</summary>
    /// <remarks>
    /// A 1216 × 832 RGB PNG weighs 1 to 2 MB (DEC-050). Forty times that covers
    /// a fourfold resolution without letting a hostile server fill the memory.
    /// </remarks>
    public long MaxImageBytes { get; init; } = 64L * 1024 * 1024;

    /// <summary>The longest side accepted, checked on the header before anything is decoded.</summary>
    /// <remarks>MEN-005, first layer. An 8192 × 8192 image decoded in RGBA already weighs 256 MiB.</remarks>
    public int MaxImageDimensionPx { get; init; } = 8192;

    /// <summary>The largest JSON answer accepted.</summary>
    /// <remarks>
    /// The history of a task holds the whole graph; it is the largest JSON
    /// ComfyUI returns, and rarely passes a few hundred kilobytes.
    /// </remarks>
    public long MaxResponseJsonBytes { get; init; } = 16L * 1024 * 1024;
}
