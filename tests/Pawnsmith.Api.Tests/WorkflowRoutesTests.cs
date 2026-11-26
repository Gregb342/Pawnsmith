using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The workflow imported from the interface (§I.16, DEC-119, DEC-120).
/// Covers tests 6 to 8 of §I.16.5.
/// </summary>
public class WorkflowRoutesTests
{
    // Port 9 is "discard": nothing listens there, so a configured generator
    // is reported Unreachable, quickly.
    private const string Address = "http://127.0.0.1:9/";

    /// <summary>A raw ComfyUI API export, without a token anywhere.</summary>
    private const string RawExport = """
        {
          "4": { "class_type": "CLIPTextEncode", "inputs": { "text": "a goblin", "clip": ["2", 0] } },
          "5": { "class_type": "CLIPTextEncode", "inputs": { "text": "blurry", "clip": ["2", 0] } },
          "7": { "class_type": "KSampler", "inputs": { "seed": 5, "steps": 8, "positive": ["4", 0], "negative": ["5", 0] } },
          "9": { "class_type": "SaveImage", "inputs": { "filename_prefix": "ComfyUI", "images": ["8", 0] } }
        }
        """;

    private static string NewUserDirectory() =>
        Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", "user-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// An address, and a configuration workflow that does not exist: until
    /// something is imported, the generator is misconfigured.
    /// </summary>
    private static string[] Settings(string user) =>
    [
        $"Pawnsmith:UserDirectory={user}",
        $"Pawnsmith:Generator:Url={Address}",
        $"Pawnsmith:Generator:WorkflowFile={Path.Combine(user, "absent.json")}",
    ];

    private static Task<HttpResponseMessage> Import(ApiHarness api, string body, string contentType = "application/json") =>
        api.Client.PutAsync("/api/generator/workflow", new StringContent(body, Encoding.UTF8, contentType));

    private static async Task<JsonNode> ImportOk(ApiHarness api, string body)
    {
        using HttpResponseMessage response = await Import(api, body);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    // --- §I.16.5 n° 6 : l'import configure, et survit à un redémarrage --------------------------

    [Fact]
    public async Task ARawExportImportedConfiguresTheGeneratorAndARestartReadsIt()
    {
        string user = NewUserDirectory();

        try
        {
            await using (ApiHarness first = await ApiHarness.StartAsync(settings: Settings(user)))
            {
                JsonNode before = await first.GetJsonAsync("/api/generator");
                before["state"]!.GetValue<string>().ShouldBe("Misconfigured");
                before["workflowSource"]!.GetValue<string>().ShouldBe("None");

                JsonNode after = await ImportOk(first, RawExport);

                // Configured on the same address; the fake address answers nothing.
                after["state"]!.GetValue<string>().ShouldBe("Unreachable");
                after["workflowSource"]!.GetValue<string>().ShouldBe("Imported");
                after["framingClause"]!.GetValue<string>().ShouldEndWith("Subject:");
            }

            string saved = await File.ReadAllTextAsync(Path.Combine(user, "workflow.comfyui.json"));
            saved.ShouldContain("{{POSITIVE}}");
            saved.ShouldContain("{{SEED}}");

            await using ApiHarness second = await ApiHarness.StartAsync(settings: Settings(user));
            JsonNode restarted = await second.GetJsonAsync("/api/generator");

            restarted["state"]!.GetValue<string>().ShouldBe("Unreachable");
            restarted["workflowSource"]!.GetValue<string>().ShouldBe("Imported");
        }
        finally
        {
            Directory.Delete(user, recursive: true);
        }
    }

    // --- §I.16.5 n° 7 : un import refusé n'écrit rien ---------------------------------------------

    [Theory]
    [InlineData("""{ "nodes": [], "links": [] }""", "WORKFLOW_IMPORT_INVALID")]
    [InlineData("""{ "4": { "class_type": "CLIPTextEncode", "inputs": { "text": "a" } } }""", "WORKFLOW_IMPORT_NO_OUTPUT")]
    [InlineData("not json", "WORKFLOW_IMPORT_INVALID")]
    public async Task ARefusedImportSavesNothingAndChangesNothing(string body, string code)
    {
        string user = NewUserDirectory();

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(settings: Settings(user));

            using HttpResponseMessage response = await Import(api, body);

            (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, code));
            File.Exists(Path.Combine(user, "workflow.comfyui.json")).ShouldBeFalse();
            (await api.GetJsonAsync("/api/generator"))["state"]!.GetValue<string>().ShouldBe("Misconfigured");
        }
        finally
        {
            if (Directory.Exists(user))
            {
                Directory.Delete(user, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AnImportThatIsNotJsonByItsTypeIsRequestInvalid()
    {
        // A form from another site could only send text/plain or a form type:
        // refused before the file is even read (§G.11.1).
        string user = NewUserDirectory();

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(settings: Settings(user));

            using HttpResponseMessage response = await Import(api, RawExport, "text/plain");

            (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
        }
        finally
        {
            if (Directory.Exists(user))
            {
                Directory.Delete(user, recursive: true);
            }
        }
    }

    // --- §I.16.5 n° 8 : le retour au fichier de la configuration ----------------------------------

    [Fact]
    public async Task RemovingTheImportedWorkflowGoesBackToTheConfiguration()
    {
        string user = NewUserDirectory();

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(settings: Settings(user));
            await ImportOk(api, RawExport);

            using HttpResponseMessage response = await api.Client.DeleteAsync("/api/generator/workflow");
            JsonNode back = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            back["state"]!.GetValue<string>().ShouldBe("Misconfigured");
            back["workflowSource"]!.GetValue<string>().ShouldBe("None");
            File.Exists(Path.Combine(user, "workflow.comfyui.json")).ShouldBeFalse();
        }
        finally
        {
            if (Directory.Exists(user))
            {
                Directory.Delete(user, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AWorkflowImportedWithoutAnAddressWaitsForOne()
    {
        // No address yet: the import is kept, and the generator becomes
        // configured as soon as an address is given.
        string user = NewUserDirectory();

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(settings:
            [
                $"Pawnsmith:UserDirectory={user}",
                $"Pawnsmith:Generator:WorkflowFile={Path.Combine(user, "absent.json")}",
            ]);

            (await ImportOk(api, RawExport))["state"]!.GetValue<string>().ShouldBe("NotConfigured");

            JsonNode configured = await api.SendJsonAsync(HttpMethod.Put, "/api/generator", new { address = Address });
            configured["state"]!.GetValue<string>().ShouldBe("Unreachable");
            configured["workflowSource"]!.GetValue<string>().ShouldBe("Imported");
        }
        finally
        {
            if (Directory.Exists(user))
            {
                Directory.Delete(user, recursive: true);
            }
        }
    }
}
