using System.Text;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers test 22 of C.12: an interrupted write leaves the previous
/// <c>project.json</c> intact and readable.
/// </summary>
public class ProjectFileWriterTests
{
    private static byte[] Bytes(Project project) => ProjectJson.Serialize(project.ToDocument());

    [Fact]
    public async Task WritingCreatesTheFileAndLeavesNoTemporaryBehind()
    {
        using TempWorkspace workspace = new();

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(ProjectSample.Rich()), CancellationToken.None);

        string path = Path.Combine(workspace.Root, ProjectFileWriter.FileName);
        File.Exists(path).ShouldBeTrue();
        File.Exists(Path.Combine(workspace.Root, ProjectFileWriter.TemporaryFileName)).ShouldBeFalse();

        ProjectJson.Deserialize(await File.ReadAllBytesAsync(path)).ToDomain().Name
            .ShouldBe(ProjectSample.Rich().Name);
    }

    [Fact]
    public async Task WritingTwiceReplacesTheFileRatherThanAppendingToIt()
    {
        using TempWorkspace workspace = new();
        Project first = ProjectSample.Rich();
        Project second = first with { Name = "Autre projet" };

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(first), CancellationToken.None);
        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(second), CancellationToken.None);

        byte[] written = await File.ReadAllBytesAsync(Path.Combine(workspace.Root, ProjectFileWriter.FileName));

        written.ShouldBe(Bytes(second));
    }

    [Fact]
    public async Task TwoSavesOfAnUnchangedProjectProduceTheSameBytesOnDisk()
    {
        // Test 19 of C.12, asserted where it actually matters — on the file,
        // not only on the serialiser. `modifiedAt` is set by the save use case
        // of C.7.3, not by this writer, so nothing here moves between the two.
        using TempWorkspace workspace = new();
        Project project = ProjectSample.Rich();
        string path = Path.Combine(workspace.Root, ProjectFileWriter.FileName);

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(project), CancellationToken.None);
        byte[] first = await File.ReadAllBytesAsync(path);

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(project), CancellationToken.None);
        byte[] second = await File.ReadAllBytesAsync(path);

        second.ShouldBe(first);
    }

    // --- C.12 n° 22 : l'écriture atomique ---------------------------------

    [Fact]
    public async Task AFailedWriteLeavesThePreviousFileIntactAndReadable()
    {
        // The failure is forced by putting a *directory* where the staged file
        // has to go: writing it then throws, before anything has touched the
        // real file. That is the whole point of staging - the project folder is
        // the user's only copy, and a half-written project.json loses the work.
        using TempWorkspace workspace = new();
        Project original = ProjectSample.Rich();
        string path = Path.Combine(workspace.Root, ProjectFileWriter.FileName);

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(original), CancellationToken.None);
        byte[] before = await File.ReadAllBytesAsync(path);

        Directory.CreateDirectory(Path.Combine(workspace.Root, ProjectFileWriter.TemporaryFileName));

        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => ProjectFileWriter.WriteAsync(
                workspace.Root,
                Bytes(original with { Name = "Écriture interrompue" }),
                CancellationToken.None));

        // Intact, and still readable — the two halves of the criterion.
        (await File.ReadAllBytesAsync(path)).ShouldBe(before);
        ProjectJson.Deserialize(await File.ReadAllBytesAsync(path)).ToDomain().Name
            .ShouldBe(original.Name);
    }

    [Fact]
    public async Task TheFileIsNeverLeftTruncated()
    {
        // A file half of whose bytes reached the disk would still parse as
        // nothing at all. Asserting the file always ends with the closing brace
        // and a newline is the cheapest way to say "complete".
        using TempWorkspace workspace = new();

        await ProjectFileWriter.WriteAsync(workspace.Root, Bytes(ProjectSample.Rich()), CancellationToken.None);

        string content = await File.ReadAllTextAsync(
            Path.Combine(workspace.Root, ProjectFileWriter.FileName),
            Encoding.UTF8);

        content.ShouldEndWith("}\n");
    }
}
