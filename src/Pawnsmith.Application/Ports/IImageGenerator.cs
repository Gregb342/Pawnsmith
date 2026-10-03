namespace Pawnsmith.Application.Ports;

/// <summary>
/// Turns a prompt and a seed into an image: a ComfyUI server in v1, a remote
/// provider some day (EVO-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>The only port of the generation</b> (DEC-078). Chapter 7 also sketched an
/// <c>IPawnPairProducer</c>, as the substitution point if T0a showed the model
/// could not draw a rotation sheet. T0a showed it can (DEC-043); the pair is
/// produced by a use case on top of this port, and this port is where a
/// provider is substituted.
/// </para>
/// <para>
/// <b>An absent generator is a normal state, not an exception.</b>
/// <see cref="CheckAsync"/> returns it as a value. <see cref="GenerateAsync"/>
/// does throw — a <see cref="GeneratorException"/> with a code — and the batch
/// turns that into the <c>Failed</c> state of its job, so that seen from outside
/// the batch, an unreachable generator still is not an exception (§E.7.1).
/// </para>
/// <para>
/// <b>The prompt is sent exactly as given</b> (DEC-049, DEC-077): no
/// substitution, truncation or rewriting. Whoever calls this sends
/// <c>ResolvedPrompt.From</c> of the clauses it freezes, and the implementation
/// must not make that a lie.
/// </para>
/// </remarks>
public interface IImageGenerator
{
    /// <summary>Whether the generator answers, and answers like a generator.</summary>
    /// <remarks>Never throws because the generator is absent; only cancellation propagates.</remarks>
    Task<GeneratorAvailability> CheckAsync(CancellationToken cancellationToken);

    /// <summary>Generates one paired image.</summary>
    /// <exception cref="GeneratorException">The generator is unreachable, slow, refuses, fails, or returns something unusable.</exception>
    /// <exception cref="OperationCanceledException">The caller cancelled. The implementation has asked the generator to drop the work first.</exception>
    Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken);
}

/// <summary>What to generate.</summary>
/// <param name="Prompt">The resolved prompt, sent verbatim.</param>
/// <param name="NegativePrompt">The project's negative clause. Sent if the workflow has a place for it; not frozen (DEC-077).</param>
/// <param name="Seed">The seed. Unsigned 64-bit, as on the candidate.</param>
public sealed record GenerationRequest(string Prompt, string NegativePrompt, ulong Seed);

/// <summary>A paired image, checked but not decoded.</summary>
/// <remarks>
/// The dimensions come from the PNG header, read before anything was written
/// (MEN-005, first layer). Nothing in T4 decodes the pixels.
/// </remarks>
/// <param name="Png">The bytes of the PNG, exactly as the generator returned them.</param>
/// <param name="WidthPx">Width declared by the header.</param>
/// <param name="HeightPx">Height declared by the header.</param>
public sealed record GeneratedImage(byte[] Png, int WidthPx, int HeightPx);

/// <summary>What <see cref="IImageGenerator.CheckAsync"/> found.</summary>
public enum GeneratorAvailability
{
    /// <summary>It answered, like a generator.</summary>
    Available,

    /// <summary>Nothing answered: connection refused, name unknown, or no answer in time. The ordinary state of a machine where ComfyUI is not running.</summary>
    Unreachable,

    /// <summary>Something answered, but not like a generator: an error page, another service on the same port.</summary>
    Unhealthy,
}

/// <summary>Why a generation failed, as a code (§E.11).</summary>
/// <remarks>
/// Declared in Application, beside the port, because the use case has to catch
/// it and Application cannot see Infrastructure (A.3). The wire strings are
/// written out, so renaming a member cannot change what an API returns.
/// </remarks>
public enum GeneratorErrorCode
{
    /// <summary>Connection refused, unknown name, connection cut.</summary>
    Unreachable,

    /// <summary>A call, or the whole generation, ran out of time.</summary>
    Timeout,

    /// <summary>The generator refused the request: invalid graph, unknown node, missing model.</summary>
    Rejected,

    /// <summary>The execution ended in error, or the generator answered with a server error or a redirect.</summary>
    Failed,

    /// <summary>What came back cannot be used: unreadable or oversized answer, zero or several images, not a PNG, too large, not splittable.</summary>
    OutputInvalid,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class GeneratorErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in E.11.</summary>
    public static string ToWireCode(this GeneratorErrorCode code) => code switch
    {
        GeneratorErrorCode.Unreachable => "GENERATOR_UNREACHABLE",
        GeneratorErrorCode.Timeout => "GENERATOR_TIMEOUT",
        GeneratorErrorCode.Rejected => "GENERATOR_REJECTED",
        GeneratorErrorCode.Failed => "GENERATOR_FAILED",
        GeneratorErrorCode.OutputInvalid => "GENERATOR_OUTPUT_INVALID",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A generation that failed, with its code and message.</summary>
public sealed class GeneratorException : Exception, ICodedException
{
    public GeneratorException(GeneratorErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public GeneratorException(GeneratorErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Why it failed, as a code rather than as text.</summary>
    public GeneratorErrorCode Code { get; }

    /// <summary>The code in the form E.11 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
