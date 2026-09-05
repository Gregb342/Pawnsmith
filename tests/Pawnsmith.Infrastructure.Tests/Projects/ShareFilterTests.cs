using System.Text;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The <c>project.json</c> half of tests 34 and 35 of C.12: what a
/// <c>Share</c> archive carries, and what it refuses to carry.
/// </summary>
public class ShareFilterTests
{
    private static ProjectDocument Document() => ProjectSample.Rich().ToDocument();

    // --- C.12 n° 35 : les rejetés partent, les brouillons restent ---------

    [Fact]
    public void RejectedCandidatesAreRemovedAndTheOthersKept()
    {
        ProjectDocument shared = ShareFilter.Apply(Document());

        IReadOnlyList<CandidateDocument> candidates = shared.Blueprints[0].Candidates;

        candidates.ShouldNotContain(c => c.Status == nameof(CandidateStatus.Rejected));
        candidates.ShouldContain(c => c.Status == nameof(CandidateStatus.Valid));
        candidates.Count.ShouldBe(1);
    }

    [Fact]
    public void DraftCandidatesTravel()
    {
        // A project shared while the arbitration is still open needs its drafts.
        // It is often the very reason for sharing.
        ProjectDocument document = Document();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument draft = blueprint.Candidates[0] with
        {
            Status = nameof(CandidateStatus.Draft),
        };

        ProjectDocument shared = ShareFilter.Apply(document with
        {
            Blueprints = [blueprint with { Candidates = [draft], ElectedCandidateId = null }],
        });

        shared.Blueprints[0].Candidates.ShouldHaveSingleItem()
            .Status.ShouldBe(nameof(CandidateStatus.Draft));
    }

    [Fact]
    public void EveryBlueprintIsKeptIncludingTheEmptyOnes()
    {
        // An empty blueprint says what the sender intends to make. Dropping it
        // would lose intent, not weight.
        ProjectDocument shared = ShareFilter.Apply(Document());

        shared.Blueprints.Count.ShouldBe(2);
        shared.Blueprints[1].Candidates.ShouldBeEmpty();
    }

    // --- C.12 n° 34 : plus aucune image jumelée ---------------------------

    [Fact]
    public void ThePairedImageIsDroppedFromEveryCandidateThatRemains()
    {
        ProjectDocument shared = ShareFilter.Apply(Document());

        shared.Blueprints
            .SelectMany(blueprint => blueprint.Candidates)
            .ShouldAllBe(candidate => candidate.PairedImageFile == null);
    }

    [Fact]
    public void TheCutOutImagesAreUntouched()
    {
        ProjectDocument shared = ShareFilter.Apply(Document());
        CandidateDocument kept = shared.Blueprints[0].Candidates[0];

        kept.FrontImageFile.ShouldNotBeNull();
        kept.BackImageFile.ShouldNotBeNull();
    }

    // --- L'invariant : aucune référence pendante --------------------------

    [Fact]
    public void NoReferenceIsLeftDangling()
    {
        // The whole reason the filter touches project.json rather than merely
        // omitting files: an archive always holds a project.json consistent with
        // what it contains.
        ProjectDocument shared = ShareFilter.Apply(Document());

        IReadOnlyList<string> referenced = ShareFilter.ReferencedImages(shared);
        referenced.ShouldNotContain(path => path.Contains("-pair.png", StringComparison.Ordinal));

        foreach (BlueprintDocument blueprint in shared.Blueprints)
        {
            if (blueprint.ElectedCandidateId is { } elected)
            {
                blueprint.Candidates.ShouldContain(c => c.Id == elected);
            }
        }
    }

    [Fact]
    public void AnElectedCandidateThatWouldBeFilteredOutMakesTheExportRefuse()
    {
        // Impossible under the business rules expected of T3, and the file can
        // have been edited by hand. Producing a dangling reference is worse than
        // refusing, so the export refuses.
        ProjectDocument document = Document();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument rejectedElect = blueprint.Candidates[0] with
        {
            Status = nameof(CandidateStatus.Rejected),
        };

        ProjectException error = Should.Throw<ProjectException>(() => ShareFilter.Apply(
            document with
            {
                Blueprints =
                [
                    blueprint with
                    {
                        Candidates = [rejectedElect],
                        ElectedCandidateId = rejectedElect.Id,
                    },
                ],
            }));

        error.Code.ShouldBe(ProjectErrorCode.ArchiveExportFailed);
        error.WireCode.ShouldBe("ARCHIVE_EXPORT_FAILED");
    }

    [Fact]
    public void TheFilteredDocumentIsStillValid()
    {
        // Filtering must not produce something the import would then refuse.
        Should.NotThrow(() => ProjectValidation.Validate(ShareFilter.Apply(Document())));
    }

    [Fact]
    public void EverythingOutsideTheCandidatesIsUntouched()
    {
        ProjectDocument source = Document();
        ProjectDocument shared = ShareFilter.Apply(source);

        shared.ProjectId.ShouldBe(source.ProjectId);
        shared.Name.ShouldBe(source.Name);
        shared.Style.ShouldBe(source.Style);
        shared.CalibrationOverrides.ShouldBe(source.CalibrationOverrides);
        shared.CreatedAt.ShouldBe(source.CreatedAt);
        shared.ModifiedAt.ShouldBe(source.ModifiedAt);
    }

    // --- archive.json ------------------------------------------------------

    [Fact]
    public void TheManifestSaysWhichProfileItIs()
    {
        // Without it a Share cannot be told from a Backup whose author never
        // produced a PDF and kept no paired image - and re-exporting the first
        // as the second would claim a completeness it does not have.
        ArchiveManifest manifest = ArchiveManifestFile.For(
            ArchiveProfile.Share,
            new DateTimeOffset(2026, 9, 1, 16, 4, 12, TimeSpan.Zero));

        manifest.Profile.ShouldBe("Share");
        manifest.ArchiveVersion.ShouldBe(1);
        manifest.CreatedAt.ShouldBe("2026-09-01T16:04:12Z");
    }

    [Fact]
    public void TheManifestNamesTheBuildThatWroteIt()
    {
        ArchiveManifest manifest = ArchiveManifestFile.For(ArchiveProfile.Backup, DateTimeOffset.UtcNow);

        manifest.ProducedBy.ShouldStartWith("Pawnsmith ");
        manifest.ProducedBy.ShouldContain("0.3.0");
    }

    [Fact]
    public void NoCommitIdEverReachesAnArchive()
    {
        // This is what guards the build property rather than a runtime strip.
        // The SDK appends "+3f9a1c..." to the informational version by default;
        // Directory.Build.props turns it off. Stripping it again in code would
        // make this test pass even if that property were removed, so nothing
        // strips it (DEC-058).
        ArchiveManifest manifest = ArchiveManifestFile.For(ArchiveProfile.Backup, DateTimeOffset.UtcNow);

        manifest.ProducedBy.ShouldNotContain("+");
    }

    [Fact]
    public void TheManifestRoundTrips()
    {
        ArchiveManifest manifest = ArchiveManifestFile.For(ArchiveProfile.Share, DateTimeOffset.UtcNow);

        byte[] bytes = ArchiveManifestFile.Serialize(manifest);

        ArchiveManifestFile.Deserialize(bytes).ShouldBe(manifest);
        Encoding.UTF8.GetString(bytes).ShouldContain("\"archiveVersion\": 1");
    }

    [Fact]
    public void TheManifestCarriesNeitherIdentityNorName()
    {
        // Duplication is an opportunity to diverge, and project.json already
        // holds both.
        string[] members = [.. typeof(ArchiveManifest).GetProperties().Select(p => p.Name)];

        members.ShouldNotContain("ProjectId");
        members.ShouldNotContain("Name");
    }
}
