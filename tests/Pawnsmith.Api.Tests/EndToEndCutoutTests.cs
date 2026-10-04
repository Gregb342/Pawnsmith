using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The whole path T5 opens, through the API: generate, cut out on the way,
/// elect, print. Covers tests 23 and 24 of F.9.
/// </summary>
public class EndToEndCutoutTests
{
    [Fact]
    public async Task GenerateCutOutElectAndPrintAPdfThatCarriesTheTransparency()
    {
        var generator = new FakeGenerator { Png = TestScene.PairPng() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder, quantity: 3);

        // Generate: the candidate comes out cut out (DEC-101).
        JsonNode job = await JobsHelper.RunToEndAsync(api, folder, blueprint, count: 1);
        job["state"]!.GetValue<string>().ShouldBe("Completed");
        Guid candidate = job["produced"]![0]!.GetValue<Guid>();

        // Elect: possible straight away, both cut-outs exist (DEC-071).
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{blueprint}/election", new { candidateId = candidate });

        // The sheet report now has a page, with the three copies on it.
        JsonNode page = (await api.GetJsonAsync($"/api/projects/{folder}/sheet/report"))["pages"]!.AsArray().Single()!;
        page["used"]!.GetValue<int>().ShouldBe(3);

        // Print: PDFsharp accepts the RGBA cut-outs, and keeps their alpha as
        // a soft mask - the transparent background stays transparent on paper.
        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf?culture=fr");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        byte[] pdf = await response.Content.ReadAsByteArrayAsync();
        string text = Encoding.Latin1.GetString(pdf);
        text.ShouldStartWith("%PDF-");
        text.ShouldContain("/SMask");
    }
}
