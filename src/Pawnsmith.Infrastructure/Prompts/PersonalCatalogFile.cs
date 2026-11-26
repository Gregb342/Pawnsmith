using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Json;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// The personal catalogue of a universe, as a file of the user directory:
/// <c>catalog.{universe}.json</c> (§I.4.2, DEC-107).
/// </summary>
/// <remarks>
/// <para>
/// <b>The same format as the shipped catalogue</b>, schema 2, with only the
/// personal entries in it. A user who wants to move an entry into the shipped
/// file can copy it as it is.
/// </para>
/// <para>
/// The labels of a parameter are not written: the shipped catalogue names its
/// keys. Reading checks the shape of the file; whether its entries fit the
/// shipped catalogue is decided by the merge, in the Application.
/// </para>
/// </remarks>
public sealed class PersonalCatalogFile(string userDirectory) : IPersonalCatalogStore
{
    public async Task<IReadOnlyList<CatalogParameter>> ReadAsync(Universe universe, CancellationToken cancellationToken)
    {
        string path = PathOf(universe);

        // Absent until the user adds a first entry; that is the ordinary case.
        if (!File.Exists(path))
        {
            return [];
        }

        (_, List<CatalogParameter> parameters) = await CatalogReader.ReadParametersAsync(path, universe, cancellationToken).ConfigureAwait(false);

        return parameters;
    }

    public Task WriteAsync(Universe universe, IReadOnlyList<CatalogParameter> personal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(personal);

        var document = new CatalogReader.CatalogDocument
        {
            VersionSchema = CatalogReader.SupportedVersionSchema,
            Universe = universe.ToString(),
            Parameters = [.. personal.Select(parameter => new CatalogReader.ParameterDocument
            {
                Key = parameter.Key,
                Entries = [.. parameter.Entries.Select(entry => new CatalogReader.EntryDocument
                {
                    Value = entry.Value,

                    // Sorted by culture name: a dictionary has no order of its
                    // own, and the same entries must give the same file.
                    Labels = entry.Labels
                        .OrderBy(label => label.Key, StringComparer.Ordinal)
                        .ToDictionary(label => label.Key, label => label.Value, StringComparer.Ordinal),
                    Fragment = entry.Fragment,
                })],
            })],
        };

        return UserFile.WriteJsonAsync(PathOf(universe), document, cancellationToken);
    }

    private string PathOf(Universe universe) =>
        Path.Combine(userDirectory, $"catalog.{universe.ToString().ToLowerInvariant()}.json");
}
