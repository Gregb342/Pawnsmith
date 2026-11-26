using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Generation;

namespace Pawnsmith.Api.Hosting;

/// <summary>Whether generation can work on this machine, decided once at start-up.</summary>
public enum GeneratorConfiguration
{
    /// <summary>No generator address: generation is simply not set up.</summary>
    NotConfigured,

    /// <summary>An address or a workflow that was refused, with its code.</summary>
    Misconfigured,

    /// <summary>Address and workflow both accepted.</summary>
    Configured,
}

/// <summary>
/// The generator as the host found it at start-up: configured, not configured,
/// or refused with a code (§G.2.2, DEC-087).
/// </summary>
/// <remarks>
/// <para>
/// <b>A misconfigured generator does not stop the application.</b> It is
/// DEC-056 applied to the machine: a calibration that cannot be read stops
/// everything, because no project loads without it; a generator that cannot be
/// reached, or a workflow that is wrong, only stops generation — the user can
/// still work on their projects, and the refusal is shown by
/// <c>GET /api/generator</c> with its code.
/// </para>
/// <para>
/// The refusal is still read <b>at start-up</b>, as §E.10.1 requires, and not at
/// the first batch: this type is built once.
/// </para>
/// </remarks>
public sealed class GeneratorSetup : IDisposable
{
    private GeneratorSetup(
        GeneratorConfiguration configuration,
        IImageGenerator? generator,
        string? framingClause,
        string? address,
        string? errorCode,
        string? errorMessage = null)
    {
        Configuration = configuration;
        Generator = generator;
        FramingClause = framingClause;
        Address = address;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public GeneratorConfiguration Configuration { get; }

    /// <summary>The generator, when configured.</summary>
    public IImageGenerator? Generator { get; }

    /// <summary>The framing clause of the workflow, when the workflow was read.</summary>
    /// <remarks>Null means "unknown", and misalignment is then reported as unknown (§G.4).</remarks>
    public string? FramingClause { get; }

    /// <summary>The address as configured — shown, never changed by the API (DEC-081).</summary>
    public string? Address { get; }

    /// <summary>Why it was refused, when misconfigured.</summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// The message of the refusal, for the log only (§H.4.1): it names a file
    /// path or the refused address, which no response carries (DEC-084).
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>A generator that is set up, for the host and for the tests.</summary>
    public static GeneratorSetup Configured(IImageGenerator generator, string framingClause, string address) =>
        new(GeneratorConfiguration.Configured, generator, framingClause, address, errorCode: null);

    /// <summary>No generator at all.</summary>
    public static GeneratorSetup NotConfigured() =>
        new(GeneratorConfiguration.NotConfigured, generator: null, framingClause: null, address: null, errorCode: null);

    /// <summary>Reads the workflow and checks the address, once.</summary>
    public static Task<GeneratorSetup> LoadAsync(PawnsmithSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return LoadAsync(settings.GeneratorUrl, settings.WorkflowFile, cancellationToken);
    }

    /// <summary>The setup for this address and workflow file; no address means not configured.</summary>
    /// <remarks>
    /// The address may come from the configuration or from the user directory
    /// (DEC-108); this method does not care which.
    /// </remarks>
    public static async Task<GeneratorSetup> LoadAsync(string? url, string workflowFile, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(workflowFile);

        if (url is null)
        {
            return NotConfigured();
        }

        WorkflowTemplate workflow;

        try
        {
            workflow = await WorkflowTemplateReader.ReadAsync(workflowFile, cancellationToken).ConfigureAwait(false);
        }
        catch (GeneratorConfigException error)
        {
            // The address was never checked on this path: it is shown, and
            // logged, only if it would itself be accepted. One refused for
            // carrying credentials must not be repeated (DEC-081).
            return new GeneratorSetup(GeneratorConfiguration.Misconfigured, null, null, ShownAddress(url), error.WireCode, error.Message);
        }

        try
        {
            var generator = new ComfyUiImageGenerator(new ComfyUiOptions(url), workflow);
            return Configured(generator, workflow.FramingClause, url);
        }
        catch (GeneratorConfigException error)
        {
            // The workflow was read, so its framing clause is known even though
            // the address is refused: misalignment stays computable.
            return new GeneratorSetup(GeneratorConfiguration.Misconfigured, null, workflow.FramingClause, address: null, error.WireCode, error.Message);
        }
    }

    /// <summary>The address if it passes the rules of DEC-081, null otherwise.</summary>
    private static string? ShownAddress(string url)
    {
        try
        {
            GeneratorAddress.Parse(url);
            return url;
        }
        catch (GeneratorConfigException)
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (Generator is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
