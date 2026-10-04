using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>The personal catalogue through the API (§I.4.3), tests 4 to 8 of §I.12.</summary>
public class CatalogRoutesTests
{
    private const string Entries = "/api/universes/Fantasy/catalog/entries";

    private static object Halberd(string value = "halberd", string french = "hallebarde", string fragment = "holding a halberd upright against the shoulder") => new
    {
        key = "weapon",
        value,
        labels = new Dictionary<string, string> { ["en"] = "halberd", ["fr"] = french },
        fragment,
    };

    private static JsonNode? Entry(JsonNode catalog, string key, string value) =>
        catalog["parameters"]!.AsArray()
            .Single(parameter => parameter!["key"]!.GetValue<string>() == key)!["entries"]!.AsArray()
            .SingleOrDefault(entry => entry!["value"]!.GetValue<string>() == value);

    [Fact]
    public async Task ACompleteEntryIsAddedAndServedAsPersonal()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage created = await api.SendAsync(HttpMethod.Post, Entries, Halberd());
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        JsonNode catalog = await api.GetJsonAsync("/api/universes/Fantasy/catalog");
        JsonNode halberd = Entry(catalog, "weapon", "halberd")!;
        halberd["origin"]!.GetValue<string>().ShouldBe("Personal");
        halberd["labels"]!["fr"]!.GetValue<string>().ShouldBe("hallebarde");
    }

    [Theory]
    [InlineData("", "holding a halberd")]
    [InlineData("halberd", "")]
    public async Task AnIncompleteEntryIsRefused(string french, string fragment)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, Entries, Halberd(french: french, fragment: fragment));

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "CATALOG_ENTRY_INVALID"));
    }

    [Fact]
    public async Task AShippedValueIsADuplicateAndAShippedEntryStays()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage duplicate = await api.SendAsync(HttpMethod.Post, Entries, Halberd(value: "axe"));
        (await ApiHarness.ErrorOf(duplicate)).ShouldBe((HttpStatusCode.Conflict, "CATALOG_ENTRY_DUPLICATE"));

        using HttpResponseMessage shipped = await api.SendAsync(HttpMethod.Delete, $"{Entries}/weapon/axe", null);
        (await ApiHarness.ErrorOf(shipped)).ShouldBe((HttpStatusCode.Conflict, "CATALOG_ENTRY_SHIPPED"));
    }

    [Fact]
    public async Task APersonalEntryIsRemoved()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        await api.SendJsonAsync(HttpMethod.Post, Entries, Halberd());

        JsonNode after = await api.SendJsonAsync(HttpMethod.Delete, $"{Entries}/weapon/halberd", null);

        Entry(after, "weapon", "halberd").ShouldBeNull();
    }

    [Fact]
    public async Task ThePersonalCatalogueSurvivesARestart()
    {
        string user = Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", "user-" + Guid.NewGuid().ToString("N"));

        try
        {
            await using (ApiHarness first = await ApiHarness.StartAsync(settings: $"Pawnsmith:UserDirectory={user}"))
            {
                await first.SendJsonAsync(HttpMethod.Post, Entries, Halberd());
            }

            await using ApiHarness second = await ApiHarness.StartAsync(settings: $"Pawnsmith:UserDirectory={user}");
            Entry(await second.GetJsonAsync("/api/universes/Fantasy/catalog"), "weapon", "halberd").ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(user, recursive: true);
        }
    }

    [Fact]
    public async Task ABlueprintComposesWithAPersonalRace()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        await api.SendJsonAsync(HttpMethod.Post, Entries, new
        {
            key = "race",
            value = "dragonborn",
            labels = new Dictionary<string, string> { ["en"] = "dragonborn", ["fr"] = "drakéide" },
            fragment = "a dragonborn",
        });
        string folder = await ProjectSeed.CreateAsync(api);

        JsonNode added = await api.SendJsonAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints", new
        {
            race = "dragonborn",
            characterClass = "warrior",
            size = "Medium",
            optionalParameters = Array.Empty<object>(),
            details = "",
            quantity = 1,
        });

        added["blueprint"]!["subjectClause"]!.GetValue<string>().ShouldBe("a dragonborn warrior");
        added["compositionDiagnostics"]!.AsArray().ShouldBeEmpty();
    }
}
