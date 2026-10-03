using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Pawnsmith.Api.Hosting;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>
/// The real host, started on a free local port with its own folders, and an
/// HTTP client pointed at it (§G.13).
/// </summary>
/// <remarks>
/// <para>
/// <b>No test package.</b> <c>Microsoft.AspNetCore.Mvc.Testing</c> would host
/// the application in memory; this starts it for real, on <c>127.0.0.1</c> and
/// a port the system chooses, which is twenty lines and one dependency fewer —
/// the trade §3 of CLAUDE.md asks for when in doubt. It also means the tests go
/// through Kestrel, the host filtering and the middleware exactly as a browser
/// would.
/// </para>
/// <para>
/// Each harness gets a temporary folder holding a projects root and a copy of
/// the repository's <c>config/</c> files, and deletes it on disposal.
/// </para>
/// </remarks>
internal sealed class ApiHarness : IAsyncDisposable
{
    private readonly WebApplication app;

    private ApiHarness(WebApplication app, string root, HttpClient client)
    {
        this.app = app;
        Root = root;
        Client = client;
    }

    /// <summary>The temporary folder of this harness.</summary>
    public string Root { get; }

    /// <summary>The projects root the host was given.</summary>
    public string ProjectsRoot => Path.Combine(Root, "projects");

    /// <summary>The folder the host writes its log files to.</summary>
    public string LogsDirectory => Path.Combine(Root, "logs");

    /// <summary>Every event the host has logged so far, oldest first.</summary>
    public IReadOnlyList<JsonNode> LogEvents() => LogFiles.Events(LogsDirectory);

    /// <summary>A client pointed at the host, with no proxy and no cookie.</summary>
    public HttpClient Client { get; }

    /// <summary>The services of the running host, to reach the write gate or the repository in a test.</summary>
    public IServiceProvider Services => app.Services;

    /// <summary>Starts a host.</summary>
    /// <param name="generator">The generator to compose in, or null for "not configured".</param>
    /// <param name="settings">Extra settings, as <c>key=value</c> command-line arguments.</param>
    public static async Task<ApiHarness> StartAsync(GeneratorSetup? generator = null, params string[] settings)
    {
        string root = Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", Guid.NewGuid().ToString("N"));
        string config = Path.Combine(root, "config");
        Directory.CreateDirectory(Path.Combine(root, "projects"));
        Directory.CreateDirectory(config);

        string repositoryConfig = Path.Combine(RepositoryRoot(), "config");

        foreach (string file in new[] { "calibration.json", "catalog.fantasy.json", "prompt-template.fantasy.json" })
        {
            File.Copy(Path.Combine(repositoryConfig, file), Path.Combine(config, file));
        }

        string[] args =
        [
            "--urls=http://127.0.0.1:0",
            $"--contentRoot={root}",
            $"--Pawnsmith:ProjectsRoot={Path.Combine(root, "projects")}",
            $"--Pawnsmith:ConfigDirectory={config}",
            $"--Pawnsmith:Logs:Directory={Path.Combine(root, "logs")}",
            .. settings.Select(setting => $"--{setting}"),
        ];

        WebApplication app = await ApiHost.BuildAsync(args, services =>
        {
            if (generator is not null)
            {
                services.AddSingleton(generator);
            }
        });

        await app.StartAsync();

        // Port 0 asked the system for a free port; the address now names it.
        var client = new HttpClient(new SocketsHttpHandler { UseProxy = false })
        {
            BaseAddress = new Uri(app.Urls.Single()),
        };

        return new ApiHarness(app, root, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();

        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temporary folder is not worth failing a test over.
        }
    }

    /// <summary>GET, expecting success, as a JSON tree.</summary>
    public async Task<JsonNode> GetJsonAsync(string path)
    {
        using HttpResponseMessage response = await Client.GetAsync(path);
        return await SuccessAsync(response);
    }

    /// <summary>Sends a JSON body, expecting success, as a JSON tree.</summary>
    public async Task<JsonNode> SendJsonAsync(HttpMethod method, string path, object? body)
    {
        using HttpResponseMessage response = await SendAsync(method, path, body);
        return await SuccessAsync(response);
    }

    /// <summary>Sends a JSON body and returns the raw response, for a test about statuses.</summary>
    public Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body),
        };

        return Client.SendAsync(request);
    }

    /// <summary>The status and the code of an error response, after checking its body is that code and nothing else.</summary>
    public static async Task<(HttpStatusCode Status, string Code)> ErrorOf(HttpResponseMessage response)
    {
        string text = await response.Content.ReadAsStringAsync();
        var body = (JsonObject)JsonNode.Parse(text)!;

        // DEC-084: one key, and no message anywhere.
        body.Select(property => property.Key).ShouldBe(["code"]);

        return (response.StatusCode, body["code"]!.GetValue<string>());
    }

    private static async Task<JsonNode> SuccessAsync(HttpResponseMessage response)
    {
        string text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Expected success, got {(int)response.StatusCode}: {text}");
        }

        return JsonNode.Parse(Encoding.UTF8.GetBytes(text))!;
    }

    private static string RepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Pawnsmith.sln not found above the test binary.");
    }
}
