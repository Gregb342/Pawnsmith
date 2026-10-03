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
        string? errorCode)
    {
        Configuration = configuration;
        Generator = generator;
        FramingClause = framingClause;
        Address = address;
        ErrorCode = errorCode;
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

    /// <summary>A generator that is set up, for the host and for the tests.</summary>
    public static GeneratorSetup Configured(IImageGenerator generator, string framingClause, string address) =>
        new(GeneratorConfiguration.Configured, generator, framingClause, address, errorCode: null);

    /// <summary>No generator at all.</summary>
    public static GeneratorSetup NotConfigured() =>
        new(GeneratorConfiguration.NotConfigured, generator: null, framingClause: null, address: null, errorCode: null);

    /// <summary>Reads the workflow and checks the address, once.</summary>
    public static async Task<GeneratorSetup> LoadAsync(PawnsmithSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.GeneratorUrl is null)
        {
            return NotConfigured();
        }

        WorkflowTemplate workflow;

        try
        {
            workflow = await WorkflowTemplateReader.ReadAsync(settings.WorkflowFile, cancellationToken).ConfigureAwait(false);
        }
        catch (GeneratorConfigException error)
        {
            return new GeneratorSetup(GeneratorConfiguration.Misconfigured, null, null, settings.GeneratorUrl, error.WireCode);
        }

        try
        {
            var generator = new ComfyUiImageGenerator(new ComfyUiOptions(settings.GeneratorUrl), workflow);
            return Configured(generator, workflow.FramingClause, settings.GeneratorUrl);
        }
        catch (GeneratorConfigException error)
        {
            // The workflow was read, so its framing clause is known even though
            // the address is refused: misalignment stays computable.
            return new GeneratorSetup(GeneratorConfiguration.Misconfigured, null, workflow.FramingClause, address: null, error.WireCode);
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
