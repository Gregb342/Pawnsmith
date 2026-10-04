using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Tests.Fixtures;
using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Sheets;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Projects;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The host itself: errors, reference routes, generator state, and the guards
/// of MEN-010. Covers tests 1 to 5, 28 and 29 of G.13.
/// </summary>
public class HostTests
{
    // --- G.13 n° 1 : une erreur est un code, et rien d'autre --------------------------

    [Fact]
    public async Task AnErrorIsACodeAndNothingElse()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.Client.GetAsync("/api/universes/Steampunk/catalog");

        (HttpStatusCode status, string code) = await ApiHarness.ErrorOf(response);
        status.ShouldBe(HttpStatusCode.NotFound);
        code.ShouldBe("UNIVERSE_NOT_FOUND");
    }

    [Fact]
    public async Task AnUnknownApiRouteAnswersWithACodeNotWithThePageOfTheFront()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.Client.GetAsync("/api/does/not/exist");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "ROUTE_NOT_FOUND"));
    }

    // --- G.13 n° 2 : chaque code connu a son statut -------------------------------------

    /// <summary>Every code that can reach a request, from every layer.</summary>
    /// <remarks>
    /// Two families are left out, each for a stated reason. The codes of the
    /// prompt files (<c>CATALOG_*</c>, <c>TEMPLATE_*</c>, <c>UNIVERSE_MISMATCH</c>)
    /// stop the start-up and never reach a request (§G.2.2). The
    /// <c>GENERATOR_*</c> codes of a generation end a job, never a request: the
    /// request that launched it has already had its 202 (§G.3.2).
    /// </remarks>
    public static TheoryData<string> KnownCodes()
    {
        IEnumerable<string> codes =
        [
            .. Enum.GetValues<ProjectErrorCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<BlueprintRuleCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<GenerationRuleCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<SheetRuleCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<GeneratorConfigErrorCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<CutoutErrorCode>().Select(code => code.ToWireCode()),
            .. Enum.GetValues<CatalogRuleCode>().Select(code => code.ToWireCode()),
            .. typeof(ApiCodes).GetFields().Select(field => (string)field.GetValue(null)!),
        ];

        return [.. codes.Where(code => code != ApiCodes.InternalError)];
    }

    [Theory]
    [MemberData(nameof(KnownCodes))]
    public void EveryKnownCodeHasAStatusOtherThan500(string code)
    {
        ErrorStatus.For(code).ShouldNotBe(500);
    }

    [Fact]
    public void AnUnknownCodeIs500()
    {
        ErrorStatus.For("SOMETHING_NEW").ShouldBe(500);
        ErrorStatus.For(ApiCodes.InternalError).ShouldBe(500);
    }

    // --- G.13 n° 3 et 4 : configuration et catalogue -------------------------------------

    [Fact]
    public async Task TheConfigurationListsFormatsSizesGeometriesAndUniverses()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonNode configuration = await api.GetJsonAsync("/api/configuration");

        configuration["paperFormats"]!.AsArray().Select(format => format!["name"]!.GetValue<string>()).ShouldContain("A4");
        configuration["sizes"]!.AsArray().Select(size => size!["size"]!.GetValue<string>())
            .ShouldBe(["Small", "Medium", "Large", "Huge", "Gargantuan"]);
        configuration["geometries"]!.AsArray().Select(geometry => geometry!.GetValue<string>())
            .ShouldBe(["FoldedTent", "TabAndSocket", "NoSupport"]);
        configuration["universes"]!.AsArray().Select(universe => universe!.GetValue<string>()).ShouldBe(["Fantasy"]);
        configuration["cultures"]!.AsArray().Select(culture => culture!.GetValue<string>()).ShouldBe(["en", "fr"]);
    }

    [Fact]
    public async Task TheCatalogueComesInTheOrderOfItsFile()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonNode catalog = await api.GetJsonAsync("/api/universes/Fantasy/catalog");

        string[] keys = [.. catalog["parameters"]!.AsArray().Select(parameter => parameter!["key"]!.GetValue<string>())];
        string shipped = await File.ReadAllTextAsync(Path.Combine(api.Root, "config", "catalog.fantasy.json"));

        // The order of the file, not an alphabetical one: each key appears in
        // the file before the next.
        int[] positions = [.. keys.Select(key => shipped.IndexOf($"\"{key}\"", StringComparison.Ordinal))];
        positions.ShouldBe(positions.Order());
        keys.Length.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task TheCatalogueCarriesItsLabelsAndTheOriginOfEachEntry()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonNode catalog = await api.GetJsonAsync("/api/universes/Fantasy/catalog");
        JsonNode weapon = catalog["parameters"]!.AsArray().Single(parameter => parameter!["key"]!.GetValue<string>() == "weapon")!;
        JsonNode spear = weapon["entries"]!.AsArray().Single(entry => entry!["value"]!.GetValue<string>() == "spear")!;

        weapon["labels"]!["fr"]!.GetValue<string>().ShouldBe("Arme");
        spear["labels"]!["en"]!.GetValue<string>().ShouldBe("short spear");
        spear["origin"]!.GetValue<string>().ShouldBe("Shipped");
    }

    [Theory]
    [InlineData("fantasy")]
    [InlineData("0")]
    [InlineData("Steampunk")]
    public async Task AUniverseIsNamedExactly(string universe)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/universes/{universe}/catalog");

        (await ApiHarness.ErrorOf(response)).Code.ShouldBe("UNIVERSE_NOT_FOUND");
    }

    // --- G.13 n° 5 : les états du générateur, sans exception --------------------------------

    [Fact]
    public async Task WithoutAnAddressTheGeneratorIsNotConfigured()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonNode generator = await api.GetJsonAsync("/api/generator");

        generator["state"]!.GetValue<string>().ShouldBe("NotConfigured");
        generator["framingClause"].ShouldBeNull();
    }

    [Fact]
    public async Task AWorkflowThatCannotBeReadMakesItMisconfiguredWithItsCodeAndTheAppStillStarts()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(
            generator: null,
            "Pawnsmith:Generator:Url=http://127.0.0.1:8188",
            "Pawnsmith:Generator:WorkflowFile=does-not-exist.json");

        JsonNode generator = await api.GetJsonAsync("/api/generator");

        generator["state"]!.GetValue<string>().ShouldBe("Misconfigured");
        generator["code"]!.GetValue<string>().ShouldBe("WORKFLOW_INVALID");

        // And the rest of the application works (DEC-087).
        (await api.GetJsonAsync("/api/configuration"))["universes"].ShouldNotBeNull();
    }

    [Fact]
    public async Task ARefusedAddressMakesItMisconfiguredWithoutRepeatingTheAddress()
    {
        string workflow = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        File.Copy(Path.Combine(RepositoryConfig(), "workflow.comfyui.example.json"), workflow);

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(
                generator: null,
                "Pawnsmith:Generator:Url=http://user:secret@127.0.0.1:8188",
                $"Pawnsmith:Generator:WorkflowFile={workflow}");

            JsonNode generator = await api.GetJsonAsync("/api/generator");

            generator["state"]!.GetValue<string>().ShouldBe("Misconfigured");
            generator["code"]!.GetValue<string>().ShouldBe("GENERATOR_URL_INVALID");
            generator["address"].ShouldBeNull();
            generator.ToJsonString().ShouldNotContain("secret");

            // The workflow was read: its framing clause is known.
            generator["framingClause"]!.GetValue<string>().ShouldStartWith("Character rotation sheet");
        }
        finally
        {
            File.Delete(workflow);
        }
    }

    [Theory]
    [InlineData(GeneratorAvailability.Available, "Available")]
    [InlineData(GeneratorAvailability.Unreachable, "Unreachable")]
    [InlineData(GeneratorAvailability.Unhealthy, "Unhealthy")]
    public async Task AConfiguredGeneratorReportsItsLiveState(GeneratorAvailability availability, string state)
    {
        var fake = new FakeGenerator { Availability = availability };
        await using ApiHarness api = await ApiHarness.StartAsync(fake.Setup());

        JsonNode generator = await api.GetJsonAsync("/api/generator");

        generator["state"]!.GetValue<string>().ShouldBe(state);
        generator["address"]!.GetValue<string>().ShouldBe("http://comfy.test:8188/");
        generator["framingClause"]!.GetValue<string>().ShouldBe(FakeGenerator.Framing);
    }

    // --- G.13 n° 28 : une requête d'une autre origine ---------------------------------------

    [Theory]
    [InlineData("http://evil.example")]
    [InlineData("null")]
    public async Task AStateChangingRequestFromAnotherOriginIsRefused(string origin)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/projects");
        request.Headers.Add("Origin", origin);
        using HttpResponseMessage response = await api.Client.SendAsync(request);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.Forbidden, "CROSS_ORIGIN_REFUSED"));
    }

    [Fact]
    public async Task TheSameOriginAndNoOriginAreLetThrough()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var sameOrigin = new HttpRequestMessage(HttpMethod.Post, "/api/does-not-exist");
        sameOrigin.Headers.Add("Origin", api.Client.BaseAddress!.GetLeftPart(UriPartial.Authority));
        using HttpResponseMessage same = await api.Client.SendAsync(sameOrigin);

        using HttpResponseMessage none = await api.Client.PostAsync("/api/does-not-exist", content: null);

        // Past the guard, both reach routing and find no route.
        (await ApiHarness.ErrorOf(same)).Code.ShouldBe("ROUTE_NOT_FOUND");
        (await ApiHarness.ErrorOf(none)).Code.ShouldBe("ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task AReadFromAnotherOriginIsNotRefused()
    {
        // GET changes nothing, and without CORS headers the browser does not
        // let the other page read the answer anyway.
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/configuration");
        request.Headers.Add("Origin", "http://evil.example");
        using HttpResponseMessage response = await api.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }

    // --- G.13 n° 29 : un Host étranger ------------------------------------------------------

    [Fact]
    public async Task AForeignHostIsRefused()
    {
        // DNS rebinding: the page and the API share an origin, but the Host
        // header names the attacker's domain.
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/configuration");
        request.Headers.Host = "evil.example";
        using HttpResponseMessage response = await api.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TheHostsAreRestrictedEvenWithoutASettingsFile()
    {
        // The harness points the content root at a folder with no
        // appsettings.json: the restriction comes from the code (MEN-010).
        await using ApiHarness api = await ApiHarness.StartAsync();

        using var local = new HttpRequestMessage(HttpMethod.Get, "/api/configuration");
        local.Headers.Host = "localhost";
        using HttpResponseMessage response = await api.Client.SendAsync(local);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        File.Exists(Path.Combine(api.Root, "appsettings.json")).ShouldBeFalse();
    }

    private static string RepositoryConfig()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return Path.Combine(directory!, "config");
    }
}
