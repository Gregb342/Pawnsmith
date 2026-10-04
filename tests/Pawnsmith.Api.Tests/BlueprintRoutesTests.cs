using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The routes of blueprints, candidates and images. Covers tests 10 to 16 of
/// G.13.
/// </summary>
public class BlueprintRoutesTests
{
    private static object Fields(string race = "goblin", string weapon = "spear", int quantity = 6) => new
    {
        race,
        characterClass = "skirmisher",
        size = "Medium",
        optionalParameters = new[] { new { key = "weapon", value = weapon }, new { key = "armour", value = "leather" } },
        details = "one ear torn",
        quantity,
    };

    // --- G.13 n° 10 : ajouter un gabarit compose sa clause ------------------------------

    [Fact]
    public async Task AddingABlueprintComposesItsClauseAndReportsWhatTheCatalogueDidNotKnow()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);

        JsonNode added = await api.SendJsonAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints", Fields(weapon: "halberd"));

        string clause = added["blueprint"]!["subjectClause"]!.GetValue<string>();
        clause.ShouldStartWith("a goblin skirmisher");
        clause.ShouldContain("halberd");

        // An unknown value is inserted and reported, never refused (DEC-056).
        JsonNode diagnostic = added["compositionDiagnostics"]!.AsArray().Single()!;
        diagnostic["key"]!.GetValue<string>().ShouldBe("weapon");
        diagnostic["value"]!.GetValue<string>().ShouldBe("halberd");
        ((JsonObject)diagnostic).Select(property => property.Key).ShouldBe(["key", "value"]);
    }

    [Fact]
    public async Task ARepeatedParameterKeyIsRequestInvalid()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints", new
        {
            race = "goblin",
            characterClass = "skirmisher",
            size = "Medium",
            optionalParameters = new[] { new { key = "weapon", value = "spear" }, new { key = "weapon", value = "axe" } },
            details = "",
            quantity = 1,
        });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    [Fact]
    public async Task AQuantityOfZeroIsRefusedByTheSaverWithItsCode()
    {
        // The API checks no rule of its own: the saver does, and names it.
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints", Fields(quantity: 0));

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "PROJECT_INVALID"));
        (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]!.AsArray().ShouldBeEmpty();
    }

    // --- G.13 n° 11 : recomposition tant que non éditée ----------------------------------

    [Fact]
    public async Task AnUneditedClauseFollowsItsFieldsAndAnEditedOneStays()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode recomposed = await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}", Fields(race: "orc"));
        // The article comes from the catalogue's race entry (DEC-106).
        recomposed["blueprint"]!["subjectClause"]!.GetValue<string>().ShouldStartWith("an orc skirmisher");

        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/subject-clause", new { clause = "a scarred orc with a notched axe" });

        JsonNode kept = await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}", Fields(race: "troll"));
        kept["blueprint"]!["subjectClause"]!.GetValue<string>().ShouldBe("a scarred orc with a notched axe");
        kept["blueprint"]!["race"]!.GetValue<string>().ShouldBe("troll");
    }

    // --- G.13 n° 12 : statuer ne change ni l'élection ni les fichiers ---------------------

    [Fact]
    public async Task JudgingACandidateMovesNeitherTheElectionNorTheFiles()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: true, FakeGenerator.Framing, elect: true);

        JsonNode judged = await api.SendJsonAsync(
            HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/candidates/{candidate}/status", new { status = "Rejected" });

        JsonNode blueprint = judged["blueprint"]!;
        blueprint["candidates"]![0]!["status"]!.GetValue<string>().ShouldBe("Rejected");
        blueprint["electedCandidateId"]!.GetValue<Guid>().ShouldBe(candidate);
        File.Exists(Path.Combine(api.ProjectsRoot, folder, "images", $"{candidate}-front.png")).ShouldBeTrue();
    }

    [Fact]
    public async Task AnUnknownCandidateIsNotFound()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage response = await api.SendAsync(
            HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/candidates/{Guid.NewGuid()}/status", new { status = "Valid" });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "CANDIDATE_NOT_FOUND"));
    }

    // --- G.13 n° 13 : élire sans détourage -------------------------------------------------

    [Fact]
    public async Task ElectingACandidateWithoutCutOutsIsRefused()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: false, FakeGenerator.Framing);

        using HttpResponseMessage response = await api.SendAsync(
            HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/election", new { candidateId = candidate });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "CANDIDATE_NOT_CUT_OUT"));
    }

    [Fact]
    public async Task ACandidateIsElectedAndUnelected()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: true, FakeGenerator.Framing);

        JsonNode elected = await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/election", new { candidateId = candidate });
        elected["blueprint"]!["electedCandidateId"]!.GetValue<Guid>().ShouldBe(candidate);

        JsonNode unelected = await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}/election", new { candidateId = (Guid?)null });
        unelected["blueprint"]!["electedCandidateId"].ShouldBeNull();
    }

    // --- G.13 n° 14 : supprimer un gabarit supprime ses fichiers ----------------------------

    [Fact]
    public async Task RemovingABlueprintRemovesItsFilesAndOnlyThem()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: true, FakeGenerator.Framing, elect: true);
        string images = Path.Combine(api.ProjectsRoot, folder, "images");
        await File.WriteAllBytesAsync(Path.Combine(images, "unrelated.png"), TestPng.Create(2, 2));

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Delete, $"/api/projects/{folder}/blueprints/{id}", body: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Directory.GetFiles(images).Select(Path.GetFileName).ShouldBe(["unrelated.png"]);
        (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]!.AsArray().ShouldBeEmpty();
        candidate.ShouldNotBe(Guid.Empty);
    }

    // --- G.13 n° 15 : seule une image référencée est servie --------------------------------

    [Fact]
    public async Task AReferencedImageIsServed()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: false, FakeGenerator.Framing);

        JsonNode project = await api.GetJsonAsync($"/api/projects/{folder}");
        string path = project["blueprints"]![0]!["candidates"]![0]!["pairedImage"]!.GetValue<string>();
        path.ShouldBe($"images/{candidate}-pair.png");

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/{path}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await response.Content.ReadAsByteArrayAsync()).ShouldBe(TestPng.Create(24, 16));
    }

    [Theory]
    [InlineData("unrelated.png")]
    [InlineData("../project.json")]
    [InlineData("%2E%2E%2Fproject.json")]
    public async Task AnImageNoCandidateReferencesIsNotFoundEvenWhenItIsOnDisk(string fileName)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: false, FakeGenerator.Framing);
        await File.WriteAllBytesAsync(Path.Combine(api.ProjectsRoot, folder, "images", "unrelated.png"), TestPng.Create(2, 2));

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/images/{fileName}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("versionSchema");
    }

    // --- G.13 n° 16 : paramètres triés, graine en chaîne ------------------------------------

    [Fact]
    public async Task ParametersAreSortedByKeyAndTheSeedIsAString()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid id = await ProjectSeed.AddBlueprintAsync(api, folder);
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{id}", Fields());
        await ProjectSeed.AddCandidateAsync(api, folder, id, cutOut: false, FakeGenerator.Framing);

        JsonNode blueprint = (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]![0]!;

        // Sent as weapon then armour; returned sorted, ordinal.
        blueprint["optionalParameters"]!.AsArray().Select(parameter => parameter!["key"]!.GetValue<string>())
            .ShouldBe(["armour", "weapon"]);

        // 2^64 - 1, which a JSON number would lose in a browser.
        JsonNode seed = blueprint["candidates"]![0]!["seed"]!;
        seed.GetValueKind().ShouldBe(System.Text.Json.JsonValueKind.String);
        seed.GetValue<string>().ShouldBe("18446744073709551615");
    }
}
