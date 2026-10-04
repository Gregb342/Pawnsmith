using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>The style library through the API (§I.7.2), test 14 of §I.12.</summary>
public class StyleRoutesTests
{
    private const string Styles = "/api/universes/Fantasy/styles";

    [Fact]
    public async Task ShippedStylesComeFirstAndAPersonalOneIsAddedThenRemoved()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        JsonArray shipped = (await api.GetJsonAsync(Styles)).AsArray();
        shipped.ShouldNotBeEmpty();
        shipped.ShouldAllBe(style => style!["origin"]!.GetValue<string>() == "Shipped");
        shipped[0]!["names"]!["fr"]!.GetValue<string>().ShouldNotBeNullOrWhiteSpace();

        using HttpResponseMessage created = await api.SendAsync(HttpMethod.Post, Styles, new { name = "Mon style", styleClause = "Pale watercolour.", negativeClause = "" });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        JsonArray withMine = (await api.GetJsonAsync(Styles)).AsArray();
        JsonNode mine = withMine[^1]!;
        mine["origin"]!.GetValue<string>().ShouldBe("Personal");
        mine["names"]!["en"]!.GetValue<string>().ShouldBe("Mon style");

        JsonArray after = (await api.SendJsonAsync(HttpMethod.Delete, $"{Styles}/{mine["id"]!.GetValue<string>()}", null)).AsArray();
        after.Count.ShouldBe(shipped.Count);
    }

    [Fact]
    public async Task AShippedStyleIsNotRemovedAndAnEmptyClauseIsRefused()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string shippedId = (await api.GetJsonAsync(Styles)).AsArray()[0]!["id"]!.GetValue<string>();

        using HttpResponseMessage shipped = await api.SendAsync(HttpMethod.Delete, $"{Styles}/{shippedId}", null);
        (await ApiHarness.ErrorOf(shipped)).ShouldBe((HttpStatusCode.Conflict, "STYLE_SHIPPED"));

        using HttpResponseMessage empty = await api.SendAsync(HttpMethod.Post, Styles, new { name = "Mine", styleClause = " ", negativeClause = "" });
        (await ApiHarness.ErrorOf(empty)).ShouldBe((HttpStatusCode.UnprocessableEntity, "STYLE_INVALID"));
    }
}
