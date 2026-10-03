using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The routes of projects. Covers tests 6 to 9 of G.13, and the refusals of a
/// malformed request.
/// </summary>
public class ProjectRoutesTests
{
    // --- G.13 n° 6 : créer, relire, lister ---------------------------------------------

    [Fact]
    public async Task AProjectIsCreatedReadBackAndListed()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, "/api/projects", new
        {
            name = "Donjon de la Griffe Noire",
            universe = "Fantasy",
            geometry = "NoSupport",
            paperFormat = "A4",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldBe("/api/projects/donjon-de-la-griffe-noire");

        JsonNode project = await api.GetJsonAsync("/api/projects/donjon-de-la-griffe-noire");
        project["name"]!.GetValue<string>().ShouldBe("Donjon de la Griffe Noire");
        project["geometry"]!.GetValue<string>().ShouldBe("NoSupport");
        project["blueprints"]!.AsArray().ShouldBeEmpty();
        project["diagnostics"]!.AsArray().ShouldBeEmpty();

        JsonNode listed = await api.GetJsonAsync("/api/projects");
        listed.AsArray().Single()!["folder"]!.GetValue<string>().ShouldBe("donjon-de-la-griffe-noire");
        listed.AsArray().Single()!["projectId"]!.GetValue<Guid>().ShouldBe(project["projectId"]!.GetValue<Guid>());
    }

    [Fact]
    public async Task AnUnknownPaperFormatIsADiagnosticNotARefusal()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonNode created = await api.SendJsonAsync(HttpMethod.Post, "/api/projects", new
        {
            name = "Tabloid",
            universe = "Fantasy",
            geometry = "TabAndSocket",
            paperFormat = "Tabloid",
        });

        // DEC-056: a machine's calibration never rejects a user's project.
        JsonNode diagnostic = created["diagnostics"]!.AsArray().Single()!;
        diagnostic["kind"]!.GetValue<string>().ShouldBe("UnknownPaperFormat");
        ((JsonObject)diagnostic).Select(property => property.Key).ShouldBe(["kind", "field"]);
    }

    // --- G.13 n° 7 : un nom de dossier non canonique -------------------------------------

    [Theory]
    [InlineData("Donjon")]
    [InlineData("donjon.")]
    [InlineData("..%2Fprojects")]
    [InlineData("con")]
    [InlineData("absent")]
    public async Task ANonCanonicalOrAbsentFolderIsNotFound(string folder)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        await ProjectSeed.CreateAsync(api);

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "PROJECT_NOT_FOUND"));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("%2E%2E")]
    public async Task DotSegmentsAreRemovedBeforeRoutingAndReachNoProject(string folder)
    {
        // The HTTP stack resolves "/api/projects/.." to "/api/" before any
        // route sees it - encoded or not - so the request never reaches the
        // projects at all. The canonical-name check behind it is the second
        // barrier, tested above with names that do reach it.
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "ROUTE_NOT_FOUND"));
    }

    [Fact]
    public async Task AFolderWithoutAProjectFileIsNotFound()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Directory.CreateDirectory(Path.Combine(api.ProjectsRoot, "empty"));

        using HttpResponseMessage response = await api.Client.GetAsync("/api/projects/empty");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "PROJECT_NOT_FOUND"));
    }

    // --- G.13 n° 8 : un projet illisible reste dans la liste ------------------------------

    [Fact]
    public async Task ABrokenProjectIsListedWithItsCodeAndReadsAsItsCode()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        await File.WriteAllTextAsync(Path.Combine(api.ProjectsRoot, folder, "project.json"), "{ \"versionSchema\": 1 }");

        JsonNode listed = (await api.GetJsonAsync("/api/projects")).AsArray().Single()!;
        listed["errorCode"]!.GetValue<string>().ShouldBe("PROJECT_INVALID");
        listed["name"].ShouldBeNull();

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}");
        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "PROJECT_INVALID"));
    }

    // --- G.13 n° 9 : modifier le style désaligne, et la réponse le montre ------------------

    [Fact]
    public async Task ChangingTheStyleMisalignsTheCandidatesAndTheAnswerShowsIt()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintDirectlyAsync(api, folder);
        await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: false, FakeGenerator.Framing);

        JsonNode before = await api.GetJsonAsync($"/api/projects/{folder}");
        before["misalignmentKnown"]!.GetValue<bool>().ShouldBeTrue();
        Candidates(before).Single()!["misalignedClauses"]!.AsArray().ShouldBeEmpty();

        JsonNode after = await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/settings", new
        {
            name = "Donjon",
            universe = "Fantasy",
            geometry = "FoldedTent",
            paperFormat = "A4",
            style = new { name = "Oil", styleClause = "oil painting, heavy impasto", negativeClause = "", palette = "" },
            calibrationOverrides = new { tabWidthMm = (double?)null, tabHeightMm = (double?)null },
        });

        Candidates(after).Single()!["misalignedClauses"]!.AsArray().Select(clause => clause!.GetValue<string>())
            .ShouldBe(["Style"]);
        after["geometry"]!.GetValue<string>().ShouldBe("FoldedTent");
        after["blueprints"]![0]!["resolvedPrompt"]!.GetValue<string>().ShouldEndWith("oil painting, heavy impasto");
    }

    [Fact]
    public async Task WithoutAWorkflowTheMisalignmentIsUnknownNotEmpty()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintDirectlyAsync(api, folder);
        await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: false, FakeGenerator.Framing);

        JsonNode project = await api.GetJsonAsync($"/api/projects/{folder}");

        project["misalignmentKnown"]!.GetValue<bool>().ShouldBeFalse();
        Candidates(project).Single()!["misalignedClauses"].ShouldBeNull();
        project["blueprints"]![0]!["resolvedPrompt"].ShouldBeNull();
    }

    // --- Une requête mal formée -----------------------------------------------------------

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "name": "x", "universe": "Fantasy", "geometry": "TabAndSocket" }""")]
    [InlineData("""{ "name": null, "universe": "Fantasy", "geometry": "TabAndSocket", "paperFormat": "A4" }""")]
    [InlineData("""{ "name": "x", "universe": "Steampunk", "geometry": "TabAndSocket", "paperFormat": "A4" }""")]
    [InlineData("""{ "name": "x", "universe": 0, "geometry": "TabAndSocket", "paperFormat": "A4" }""")]
    public async Task AMalformedRequestIsRequestInvalid(string body)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await api.Client.PostAsync("/api/projects", content);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
        Directory.GetDirectories(api.ProjectsRoot).ShouldBeEmpty();
    }

    private static JsonArray Candidates(JsonNode project) => project["blueprints"]![0]!["candidates"]!.AsArray();
}
