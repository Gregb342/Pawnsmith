using System.Text.Json.Nodes;

using Microsoft.Extensions.DependencyInjection;

using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>
/// Puts a project in a known state through the running host's own repository,
/// for what the API cannot do yet — a cut-out candidate needs T5.
/// </summary>
internal static class ProjectSeed
{
    /// <summary>Creates a project through the API and returns its folder.</summary>
    public static async Task<string> CreateAsync(ApiHarness api, string name = "Donjon")
    {
        JsonNode created = await api.SendJsonAsync(HttpMethod.Post, "/api/projects", new
        {
            name,
            universe = "Fantasy",
            geometry = "TabAndSocket",
            paperFormat = "A4",
        });

        return created["folder"]!.GetValue<string>();
    }

    /// <summary>Adds a blueprint through the API and returns its identifier.</summary>
    public static async Task<Guid> AddBlueprintAsync(ApiHarness api, string folder, int quantity = 6, string size = "Medium")
    {
        JsonNode added = await api.SendJsonAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints", new
        {
            race = "goblin",
            characterClass = "skirmisher",
            size,
            optionalParameters = new[] { new { key = "weapon", value = "spear" } },
            details = "one ear torn",
            quantity,
        });

        return added["blueprint"]!["id"]!.GetValue<Guid>();
    }

    /// <summary>Adds a blueprint through the repository of the running host, bypassing the routes.</summary>
    /// <remarks>For tests about something else than the blueprint routes, so that they do not depend on them.</remarks>
    public static async Task<Guid> AddBlueprintDirectlyAsync(ApiHarness api, string folder)
    {
        IProjectRepository repository = api.Services.GetRequiredService<IProjectRepository>();
        Calibration calibration = api.Services.GetRequiredService<Calibration>();
        string directory = Path.Combine(api.ProjectsRoot, folder);

        LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, CancellationToken.None);
        var blueprint = new Blueprint(
            Id: Guid.NewGuid(),
            Race: "goblin",
            CharacterClass: "skirmisher",
            Size: Size.Medium,
            OptionalParameters: new Dictionary<string, string>(StringComparer.Ordinal),
            Details: string.Empty,
            SubjectClause: "a goblin skirmisher",
            Quantity: 6,
            Candidates: [],
            ElectedCandidateId: null);

        await repository.SaveAsync(directory, loaded.Project with { Blueprints = [.. loaded.Project.Blueprints, blueprint] }, CancellationToken.None);

        return blueprint.Id;
    }

    /// <summary>
    /// Gives a blueprint a candidate whose image files really exist, through the
    /// repository of the running host.
    /// </summary>
    /// <param name="cutOut">Whether the candidate has its two cut-outs, as T5 will produce them.</param>
    /// <param name="framing">The framing clause the candidate froze.</param>
    public static async Task<Guid> AddCandidateAsync(
        ApiHarness api,
        string folder,
        Guid blueprintId,
        bool cutOut,
        string framing,
        bool elect = false,
        byte[]? pairedPng = null)
    {
        IProjectRepository repository = api.Services.GetRequiredService<IProjectRepository>();
        Calibration calibration = api.Services.GetRequiredService<Calibration>();
        string directory = Path.Combine(api.ProjectsRoot, folder);

        LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, CancellationToken.None);
        Blueprint blueprint = loaded.Project.Blueprints.Single(each => each.Id == blueprintId);

        var candidateId = Guid.NewGuid();
        string images = Path.Combine(directory, "images");
        Directory.CreateDirectory(images);

        await File.WriteAllBytesAsync(Path.Combine(images, $"{candidateId}-pair.png"), pairedPng ?? TestPng.Create(24, 16));

        if (cutOut)
        {
            await File.WriteAllBytesAsync(Path.Combine(images, $"{candidateId}-front.png"), TestPng.Create(12, 16));
            await File.WriteAllBytesAsync(Path.Combine(images, $"{candidateId}-back.png"), TestPng.Create(12, 16));
        }

        var candidate = new Candidate(
            Id: candidateId,
            Seed: 18446744073709551615UL,
            FramingClauseUsed: framing,
            SubjectClauseUsed: blueprint.SubjectClause,
            StyleClauseUsed: loaded.Project.Style.StyleClause,
            Status: CandidateStatus.Draft,
            PairedImageFile: $"images/{candidateId}-pair.png",
            FrontImageFile: cutOut ? $"images/{candidateId}-front.png" : null,
            BackImageFile: cutOut ? $"images/{candidateId}-back.png" : null,
            GeneratedAt: new DateTimeOffset(2026, 10, 3, 12, 0, 0, TimeSpan.Zero));

        Blueprint grown = blueprint with
        {
            Candidates = [.. blueprint.Candidates, candidate],
            ElectedCandidateId = elect ? candidateId : blueprint.ElectedCandidateId,
        };

        Project project = loaded.Project with
        {
            Blueprints = [.. loaded.Project.Blueprints.Select(each => each.Id == blueprintId ? grown : each)],
        };

        await repository.SaveAsync(directory, project, CancellationToken.None);

        return candidateId;
    }
}
