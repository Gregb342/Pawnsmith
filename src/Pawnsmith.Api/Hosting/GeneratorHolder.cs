using Pawnsmith.Api.Errors;
using Pawnsmith.Infrastructure.Generation;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// The generator in force, replaceable while the application runs (§I.5,
/// DEC-108).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a holder.</b> Until T6's front the generator was built once, at
/// start-up, and every service held it. The address can now change from the
/// interface. Every reader asks this holder for <see cref="Current"/> at the
/// moment it needs it, and a change replaces it in one assignment.
/// </para>
/// <para>
/// <b>A queued batch keeps its generator.</b> The start route captures
/// <see cref="Current"/> into the job (§I.5.2). Changing the address is
/// therefore for the next batch, and a generator replaced while a batch still
/// uses it must stay alive: replaced generators are kept, and disposed with
/// the holder when the host stops.
/// </para>
/// </remarks>
public sealed class GeneratorHolder(GeneratorSetup initial, PawnsmithSettings settings) : IDisposable
{
    private readonly SemaphoreSlim changes = new(1, 1);
    private readonly List<GeneratorSetup> built = [];
    private GeneratorSetup current = initial;

    public GeneratorSetup Current => Volatile.Read(ref current);

    /// <summary>Validates, saves and applies a new address; <c>null</c> means no generator.</summary>
    /// <returns>The new setup — configured, or misconfigured when the workflow is the problem.</returns>
    /// <exception cref="ApiException"><c>GENERATOR_ADDRESS_INVALID</c>: nothing was saved or changed.</exception>
    public async Task<GeneratorSetup> ReplaceAsync(string? address, CancellationToken cancellationToken)
    {
        string? trimmed = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (trimmed is not null)
        {
            try
            {
                GeneratorAddress.Parse(trimmed);
            }
            catch (GeneratorConfigException)
            {
                // A code of its own rather than GENERATOR_URL_INVALID, which
                // is a deployment fault (503): this one is a request the user
                // can correct (422). The address is not repeated anywhere.
                throw new ApiException(ApiCodes.GeneratorAddressInvalid);
            }
        }

        await changes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            GeneratorSetup next = trimmed is null
                ? GeneratorSetup.NotConfigured()
                : await GeneratorSetup.LoadAsync(trimmed, settings.WorkflowFile, cancellationToken).ConfigureAwait(false);

            // Saved before it is applied: an address in force that a restart
            // would forget is worse than one refused.
            await new GeneratorAddressFile(settings.UserDirectory).WriteAsync(trimmed, cancellationToken).ConfigureAwait(false);

            built.Add(next);
            Volatile.Write(ref current, next);

            return next;
        }
        finally
        {
            changes.Release();
        }
    }

    /// <summary>
    /// Disposes the setups this holder built. The initial one belongs to the
    /// container, which disposes it.
    /// </summary>
    public void Dispose()
    {
        foreach (GeneratorSetup setup in built)
        {
            setup.Dispose();
        }

        changes.Dispose();
    }
}
