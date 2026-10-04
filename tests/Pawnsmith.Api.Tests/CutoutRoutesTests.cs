using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The cut-out through the API: on demand, and in a batch. Covers test 22 of F.9.
/// </summary>
public class CutoutRoutesTests
{
    private static string CutoutRoute(string folder, Guid blueprint, Guid candidate) =>
        $"/api/projects/{folder}/blueprints/{blueprint}/candidates/{candidate}/cutout";

    // --- F.9 n° 22 : la route détoure ---------------------------------------------------------------------

    [Fact]
    public async Task ACandidateIsCutOutOnDemandAndBecomesElectable()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: false, FakeGenerator.Framing, pairedPng: TestScene.PairPng());

        JsonNode edited = await api.SendJsonAsync(HttpMethod.Post, CutoutRoute(folder, blueprint, candidate), body: null);

        JsonNode cutOut = edited["blueprint"]!["candidates"]!.AsArray().Single()!;
        cutOut["frontImage"]!.GetValue<string>().ShouldBe($"images/{candidate}-front.png");
        cutOut["backImage"]!.GetValue<string>().ShouldBe($"images/{candidate}-back.png");

        // The cut-out is served, like any referenced image (DEC-088)...
        using HttpResponseMessage image = await api.Client.GetAsync($"/api/projects/{folder}/images/{candidate}-front.png");
        image.StatusCode.ShouldBe(HttpStatusCode.OK);

        // ...and the candidate can now be elected (DEC-071).
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{blueprint}/election", new { candidateId = candidate });
    }

    [Fact]
    public async Task AnImageWithNoSubjectIsRefusedWithItsCode()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        // The default seeded image is plain grey: nothing to cut out.
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: false, FakeGenerator.Framing);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, CutoutRoute(folder, blueprint, candidate), body: null);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "CUTOUT_SUBJECT_NOT_FOUND"));
    }

    [Fact]
    public async Task AnUnknownCandidateIsNotFound()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, CutoutRoute(folder, blueprint, Guid.NewGuid()), body: null);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "CANDIDATE_NOT_FOUND"));
    }

    [Theory]
    [InlineData("CUTOUT_IMAGE_INVALID")]
    [InlineData("CUTOUT_IMAGE_TOO_LARGE")]
    [InlineData("CUTOUT_BACKGROUND_NOT_UNIFORM")]
    [InlineData("CUTOUT_SUBJECT_NOT_FOUND")]
    [InlineData("CANDIDATE_NO_PAIRED_IMAGE")]
    public void EveryCutOutCodeIsUnprocessable(string code)
    {
        Pawnsmith.Api.Errors.ErrorStatus.For(code).ShouldBe(422);
    }

    // --- Dans un lot : les échecs de détourage sont rendus ----------------------------------------------

    [Fact]
    public async Task ABatchWhoseImagesCannotBeCutOutCompletesAndListsThem()
    {
        // The default fake image is plain grey: every cut-out is refused.
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode job = await JobsHelper.RunToEndAsync(api, folder, blueprint, count: 2);

        job["state"]!.GetValue<string>().ShouldBe("Completed");
        JsonArray failures = job["cutoutFailures"]!.AsArray();
        failures.Count.ShouldBe(2);
        failures[0]!["code"]!.GetValue<string>().ShouldBe("CUTOUT_SUBJECT_NOT_FOUND");

        // The code, never the message (DEC-084).
        failures[0]!.AsObject().Select(property => property.Key).Order(StringComparer.Ordinal).ShouldBe(["candidateId", "code"]);
    }

    [Fact]
    public async Task ABatchOfRealScenesComesOutCutOut()
    {
        var generator = new FakeGenerator { Png = TestScene.PairPng() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode job = await JobsHelper.RunToEndAsync(api, folder, blueprint, count: 1);

        job["cutoutFailures"]!.AsArray().ShouldBeEmpty();
        JsonNode project = await api.GetJsonAsync($"/api/projects/{folder}");
        project["blueprints"]![0]!["candidates"]![0]!["frontImage"].ShouldNotBeNull();
    }
}
