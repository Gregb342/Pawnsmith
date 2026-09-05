using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Writes a project back to its folder, in the order C.7.3 fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is written until the project has been validated</b>, and that is
/// the first step for a reason: the project folder is the user's only copy, so
/// an invalid entity is never serialised over a valid file.
/// </para>
/// <para>
/// <b>No previous state, ever.</b> <c>SaveAsync</c> does not receive the project
/// as it was loaded, and must not grow a parameter for it (DEC-055). No field is
/// locked after creation, so there is no transition to arbitrate, and a
/// repository that compared before and after would become the keeper of a
/// business rule. If a later slice wants one — confirm before a geometry change,
/// refuse a universe change while candidates exist — it belongs to a use case in
/// Application, which holds both states. Same boundary as "the renderer decides
/// nothing".
/// </para>
/// <para>
/// <b>No <c>.bak</c>, and no version history</b> (C.7.3). The user's backup is
/// the archive of C.8, and adding a second mechanism beside it would give two
/// half-reliable ones instead of one.
/// </para>
/// </remarks>
public sealed class ProjectSaver
{
    private readonly TimeProvider clock;

    /// <param name="clock">
    /// Where "now" comes from. <c>TimeProvider</c> is the abstraction .NET ships
    /// for exactly this — it needs no package, and it lets a test pin the
    /// instant instead of asserting that a timestamp is "roughly now", which is
    /// the kind of assertion that fails once a month on a slow machine.
    /// </param>
    public ProjectSaver(TimeProvider? clock = null)
    {
        this.clock = clock ?? TimeProvider.System;
    }

    /// <summary>Saves <paramref name="project"/> into <paramref name="projectDirectory"/>.</summary>
    /// <remarks>
    /// The folder is a parameter rather than something read off the project,
    /// because <b>the folder name has no semantics</b> (DEC-047): the identity is
    /// <c>projectId</c>, and the application never parses a path to learn
    /// anything. How the caller knows the folder is its own business, and how
    /// <c>IProjectRepository</c> will express that is settled when it is
    /// assembled — see the note in the commit.
    /// </remarks>
    /// <returns>The project as it was written, with its new <c>modifiedAt</c>.</returns>
    /// <exception cref="ProjectException">The project is not in a state worth writing.</exception>
    public async Task<Project> SaveAsync(
        string projectDirectory,
        Project project,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(project);

        // Step 3 first in the code, because the stamped project is what gets
        // validated and written - validating one object and writing another
        // would be a gap wide enough to drive a defect through.
        Project stamped = project with { ModifiedAt = Now() };

        ProjectDocument document = stamped.ToDocument();

        // Steps 1 and 2. The same intrinsic rules as the load: what is wrong
        // here is wrong on every machine, so refusing it costs nobody anything
        // (DEC-056).
        ProjectValidation.Validate(document);
        RequireNormalisedClauses(stamped);

        if (!Directory.Exists(projectDirectory))
        {
            throw new ProjectException(
                ProjectErrorCode.NotFound,
                $"The project folder '{projectDirectory}' does not exist. " +
                "Saving creates a file, never a folder.");
        }

        // Steps 4 and 5, both inside the writer.
        await ProjectFileWriter
            .WriteAsync(projectDirectory, ProjectJson.Serialize(document), cancellationToken)
            .ConfigureAwait(false);

        return stamped;
    }

    /// <summary>The instant to stamp, always in UTC.</summary>
    /// <remarks>
    /// Truncated to the second, because that is the precision the file records
    /// (C.3.3). Without the truncation the project held in memory would carry
    /// milliseconds the file does not, and the very next comparison between the
    /// two would disagree for a reason nobody could see.
    /// </remarks>
    private DateTimeOffset Now()
    {
        DateTimeOffset instant = clock.GetUtcNow();

        return new DateTimeOffset(
            instant.Year, instant.Month, instant.Day,
            instant.Hour, instant.Minute, instant.Second,
            TimeSpan.Zero);
    }

    /// <summary>
    /// Refuses a clause that is not in the canonical form of C.5.3.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Checked rather than repaired, and the difference matters. Normalising
    /// silently on the way out would make the file disagree with the project the
    /// caller still holds in memory, and the next misalignment calculation would
    /// then compare a normalised stored clause against an unnormalised current
    /// one — a phantom misalignment, on the one mechanism the product relies on
    /// for safety.
    /// </para>
    /// <para>
    /// A clause that reaches this point unnormalised is a defect in whoever built
    /// the project, not a bad file, so it is refused loudly.
    /// </para>
    /// </remarks>
    private static void RequireNormalisedClauses(Project project)
    {
        Require(project.Style.StyleClause, "style.styleClause");

        for (int blueprintIndex = 0; blueprintIndex < project.Blueprints.Count; blueprintIndex++)
        {
            Blueprint blueprint = project.Blueprints[blueprintIndex];
            string where = $"blueprints[{blueprintIndex}]";

            Require(blueprint.SubjectClause, $"{where}.subjectClause");

            for (int candidateIndex = 0; candidateIndex < blueprint.Candidates.Count; candidateIndex++)
            {
                Candidate candidate = blueprint.Candidates[candidateIndex];
                string candidateField = $"{where}.candidates[{candidateIndex}]";

                Require(candidate.FramingClauseUsed, $"{candidateField}.framingClauseUsed");
                Require(candidate.SubjectClauseUsed, $"{candidateField}.subjectClauseUsed");
                Require(candidate.StyleClauseUsed, $"{candidateField}.styleClauseUsed");
            }
        }

        static void Require(string clause, string field)
        {
            if (!string.Equals(clause, ResolvedPrompt.Normalize(clause), StringComparison.Ordinal))
            {
                throw new ProjectException(
                    ProjectErrorCode.Invalid,
                    $"The clause at {field} is not normalised: it carries a carriage return, " +
                    "or leading or trailing whitespace. Clauses are normalised when they are " +
                    "stored, not on the way to the disk, so that the file and the project in " +
                    "memory never disagree.");
            }
        }
    }
}
