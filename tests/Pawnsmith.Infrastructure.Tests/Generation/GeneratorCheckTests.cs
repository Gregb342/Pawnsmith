using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// Covers tests 20, 21 and 31 of E.12: the health check, and the address rules
/// of DEC-081.
/// </summary>
public class GeneratorCheckTests
{
    private static ComfyUiImageGenerator Client(string address) =>
        new(new ComfyUiOptions(address) { CheckTimeout = TimeSpan.FromSeconds(2) },
            WorkflowTemplateReader.Parse(WorkflowFixture.File(), "workflow.test.json"));

    // --- E.12 n° 20 : un serveur qui répond ------------------------------------

    [Fact]
    public async Task AServerThatAnswersLikeComfyUiIsAvailable()
    {
        await using var server = new FakeHttpServer(request =>
            request.Path == "/system_stats"
                ? FakeResponse.Json("""{ "system": { "os": "posix" }, "devices": [] }""")
                : FakeResponse.Json("{}", status: 404));

        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        (await client.CheckAsync(CancellationToken.None)).ShouldBe(GeneratorAvailability.Available);
        server.Requests.Single().Target.ShouldBe("/system_stats");
    }

    // --- E.12 n° 21 : un port fermé, sans lever --------------------------------

    [Fact]
    public async Task AClosedPortIsUnreachableAndNothingIsThrown()
    {
        using ComfyUiImageGenerator client = Client(FakeHttpServer.ClosedAddress());

        GeneratorAvailability availability = await client.CheckAsync(CancellationToken.None);

        availability.ShouldBe(GeneratorAvailability.Unreachable);
    }

    [Fact]
    public async Task AServerThatNeverAnswersInTimeIsUnreachable()
    {
        await using var server = new FakeHttpServer(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return FakeResponse.Json("{}");
        });

        using var client = new ComfyUiImageGenerator(
            new ComfyUiOptions(server.BaseAddress) { CheckTimeout = TimeSpan.FromMilliseconds(200) },
            WorkflowTemplateReader.Parse(WorkflowFixture.File(), "workflow.test.json"));

        (await client.CheckAsync(CancellationToken.None)).ShouldBe(GeneratorAvailability.Unreachable);
    }

    [Theory]
    [InlineData(500, """{ "error": "boom" }""")]
    [InlineData(200, "<html>not a generator</html>")]
    [InlineData(200, "[1, 2, 3]")]
    public async Task AServerThatAnswersLikeSomethingElseIsUnhealthy(int status, string body)
    {
        await using var server = new FakeHttpServer(_ => FakeResponse.Json(body, status));

        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        (await client.CheckAsync(CancellationToken.None)).ShouldBe(GeneratorAvailability.Unhealthy);
    }

    [Fact]
    public async Task ARedirectedHealthCheckIsNotFollowed()
    {
        await using var target = new FakeHttpServer(_ => FakeResponse.Json("{}"));
        await using var server = new FakeHttpServer(_ => FakeResponse.Redirect(target.BaseAddress + "system_stats"));

        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        (await client.CheckAsync(CancellationToken.None)).ShouldBe(GeneratorAvailability.Unhealthy);
        target.Requests.ShouldBeEmpty();
    }

    // --- E.12 n° 31 : l'adresse --------------------------------------------------

    [Theory]
    [InlineData("ftp://127.0.0.1:8188/")]
    [InlineData("file:///etc/passwd")]
    [InlineData("http://user:secret@127.0.0.1:8188/")]
    [InlineData("http://127.0.0.1:8188/?next=http://169.254.169.254/")]
    [InlineData("http://127.0.0.1:8188/#fragment")]
    [InlineData("127.0.0.1:8188")]
    [InlineData("")]
    public void AnAddressOutsideTheRulesIsRefused(string address)
    {
        GeneratorConfigException error = Should.Throw<GeneratorConfigException>(() => GeneratorAddress.Parse(address));

        error.Code.ShouldBe(GeneratorConfigErrorCode.GeneratorUrlInvalid);
        error.WireCode.ShouldBe("GENERATOR_URL_INVALID");
    }

    [Fact]
    public void ARefusedAddressDoesNotRepeatItsCredentials()
    {
        GeneratorConfigException error = Should.Throw<GeneratorConfigException>(() =>
            GeneratorAddress.Parse("http://user:secret@127.0.0.1:8188/"));

        error.Message.ShouldNotContain("secret");
    }

    [Fact]
    public void TheClientRefusesABadAddressWhenItIsBuiltNotAtTheFirstBatch()
    {
        Should.Throw<GeneratorConfigException>(() => Client("ftp://127.0.0.1/"))
            .Code.ShouldBe(GeneratorConfigErrorCode.GeneratorUrlInvalid);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8188", "http://127.0.0.1:8188/")]
    [InlineData("http://comfy.local:9000/comfy", "http://comfy.local:9000/comfy/")]
    [InlineData("https://comfy.local/comfy/", "https://comfy.local/comfy/")]
    public void AnAcceptedAddressEndsWithASlashSoThatCallsAppendToIt(string address, string expected)
    {
        GeneratorAddress.Parse(address).ToString().ShouldBe(expected);
    }

    [Fact]
    public void AnyPortIsAccepted()
    {
        // No port whitelist (DEC-081): the operator writes this address.
        GeneratorAddress.Parse("http://192.168.1.20:31337/").Port.ShouldBe(31337);
    }
}
