using System.Net;
using System.Text.Json.Nodes;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>Starts a batch through the API and waits for it to end, as a client would.</summary>
internal static class JobsHelper
{
    public static async Task<JsonNode> RunToEndAsync(ApiHarness api, string folder, Guid blueprint, int count)
    {
        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { count });
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        Guid id = JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<Guid>();

        for (int attempt = 0; attempt < 500; attempt++)
        {
            JsonNode job = await api.GetJsonAsync($"/api/jobs/{id}");

            if (job["state"]!.GetValue<string>() is "Completed" or "Failed" or "Cancelled")
            {
                return job;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"Job {id} never ended.");
    }
}
