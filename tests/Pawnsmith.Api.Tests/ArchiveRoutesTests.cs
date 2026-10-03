using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// Export and import of archives. Covers tests 26 and 27 of G.13.
/// </summary>
public class ArchiveRoutesTests
{
    private static async Task<byte[]> ExportAsync(ApiHarness api, string folder, string profile)
    {
        using HttpResponseMessage response = await api.Client.PostAsync($"/api/projects/{folder}/archives?profile={profile}", content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/zip");
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static Task<HttpResponseMessage> ImportAsync(ApiHarness api, byte[] archive, string name, string mediaType = "application/zip")
    {
        var content = new ByteArrayContent(archive);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

        return api.Client.PostAsync($"/api/projects/import?name={Uri.EscapeDataString(name)}", content);
    }

    // --- G.13 n° 26 : export puis import -------------------------------------------------

    [Fact]
    public async Task AProjectExportedAndImportedComesBack()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: true, FakeGenerator.Framing, elect: true);
        JsonNode original = await api.GetJsonAsync($"/api/projects/{folder}");

        byte[] archive = await ExportAsync(api, folder, "Backup");

        using HttpResponseMessage response = await ImportAsync(api, archive, "Donjon restauré");
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldBe("/api/projects/donjon-restaure");

        JsonNode imported = await api.GetJsonAsync("/api/projects/donjon-restaure");
        imported["projectId"]!.GetValue<Guid>().ShouldBe(original["projectId"]!.GetValue<Guid>());
        imported["blueprints"]![0]!["electedCandidateId"]!.GetValue<Guid>().ShouldBe(candidate);

        // And its images came with it.
        using HttpResponseMessage image = await api.Client.GetAsync($"/api/projects/donjon-restaure/images/{candidate}-front.png");
        image.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AShareArchiveCarriesNoPairedImage()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: true, FakeGenerator.Framing);

        byte[] archive = await ExportAsync(api, folder, "Share");

        using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
        zip.Entries.Select(entry => entry.FullName).ShouldNotContain(name => name.EndsWith("-pair.png", StringComparison.Ordinal));
        zip.Entries.Select(entry => entry.FullName).ShouldContain("project.json");
    }

    [Fact]
    public async Task NoTemporaryFileOutlivesAnExport()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        string staging = Path.Combine(Path.GetTempPath(), "pawnsmith-exports");
        int before = Directory.Exists(staging) ? Directory.GetDirectories(staging).Length : 0;

        await ExportAsync(api, folder, "Backup");
        await Task.Delay(100);

        (Directory.Exists(staging) ? Directory.GetDirectories(staging).Length : 0).ShouldBeLessThanOrEqualTo(before);
    }

    [Theory]
    [InlineData("")]
    [InlineData("?profile=backup")]
    [InlineData("?profile=Everything")]
    public async Task AnUnknownProfileIsRequestInvalid(string query)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);

        using HttpResponseMessage response = await api.Client.PostAsync($"/api/projects/{folder}/archives{query}", content: null);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    [Fact]
    public async Task ImportingOverAnExistingFolderIsAConflict()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        byte[] archive = await ExportAsync(api, folder, "Backup");

        using HttpResponseMessage response = await ImportAsync(api, archive, "Donjon");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.Conflict, "IMPORT_DESTINATION_EXISTS"));
    }

    [Fact]
    public async Task SomethingThatIsNotAnArchiveIsRejected()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await ImportAsync(api, "not a zip at all"u8.ToArray(), "Nope");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "ARCHIVE_REJECTED"));
        Directory.GetDirectories(api.ProjectsRoot).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("application/octet-stream")]
    [InlineData("multipart/form-data")]
    public async Task AnotherMediaTypeIsRequestInvalid(string mediaType)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        byte[] archive = await ExportAsync(api, folder, "Backup");

        using HttpResponseMessage response = await ImportAsync(api, archive, "Elsewhere", mediaType);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    // --- G.13 n° 27 : une archive trop grosse --------------------------------------------------

    [Fact]
    public async Task AnArchiveLargerThanTheBoundIsRefusedWhileItArrives()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(generator: null, "Pawnsmith:MaxUploadBytes=1000");
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: true, FakeGenerator.Framing);
        byte[] archive = await ExportAsync(api, folder, "Backup");
        archive.Length.ShouldBeGreaterThan(1000);

        using HttpResponseMessage response = await ImportAsync(api, archive, "Too big");

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        Directory.GetDirectories(api.ProjectsRoot).Length.ShouldBe(1);
    }

    [Fact]
    public async Task AnArchiveLargerThanAnOrdinaryRequestIsStillAccepted()
    {
        // The import raises the bound for itself alone: two mebibytes is past
        // MaxRequestBytes and well under MaxUploadBytes.
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: true, FakeGenerator.Framing);

        // A large, incompressible paired image.
        byte[] noise = new byte[2 * 1024 * 1024];
        new Random(7).NextBytes(noise);
        byte[] png = TestPng.Create(4, 4);
        await File.WriteAllBytesAsync(Path.Combine(api.ProjectsRoot, folder, "images", $"{candidate}-pair.png"), [.. png, .. noise]);

        byte[] archive = await ExportAsync(api, folder, "Backup");
        archive.Length.ShouldBeGreaterThan(1024 * 1024);

        using HttpResponseMessage response = await ImportAsync(api, archive, "Large");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }
}
