using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 24 to 29 of C.12: what a <c>project.json</c> can contradict on
/// its own, and therefore what blocks a load.
/// </summary>
public class ProjectValidationTests
{
    private static ProjectDocument Document() => ProjectSample.Rich().ToDocument();

    private static ProjectException Refuse(ProjectDocument document) =>
        Should.Throw<ProjectException>(() => ProjectValidation.Validate(document));

    [Fact]
    public void AWellFormedProjectPasses()
    {
        Should.NotThrow(() => ProjectValidation.Validate(Document()));
    }

    // --- C.12 n° 24 : un schéma plus récent -------------------------------

    [Fact]
    public void ASchemaFromTheFutureIsRefusedWithoutBeingPartlyRead()
    {
        // The motive is data loss, not caution: opening a v2 project with a v1
        // writer and saving it would drop everything v2 added, and the loss only
        // surfaces on reopening with the newer build (C.6.1).
        ProjectException error = Refuse(Document() with { VersionSchema = 2 });

        error.Code.ShouldBe(ProjectErrorCode.SchemaTooRecent);
        error.WireCode.ShouldBe("PROJECT_SCHEMA_TOO_RECENT");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ASchemaThatCannotExistIsRefusedAsInvalid(int version)
    {
        // Not a migration engine: there is one version, so there is nothing to
        // migrate (C.6.2). Refusing a number no writer ever produced is simply
        // refusing to interpret a file we cannot read.
        Refuse(Document() with { VersionSchema = version }).Code.ShouldBe(ProjectErrorCode.Invalid);
    }

    // --- C.12 n° 26 : un champ obligatoire absent -------------------------

    [Fact]
    public void AnAbsentRootFieldIsRefusedAndNamed()
    {
        // The deserialiser leaves an absent member at null whatever the type
        // annotation says, so null is checked explicitly - the compiler has no
        // idea a file was involved.
        ProjectException error = Refuse(Document() with { Name = null! });

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("name");
    }

    [Fact]
    public void AnAbsentNestedFieldIsNamedDownToItsIndex()
    {
        ProjectDocument document = Document();
        BlueprintDocument broken = document.Blueprints[0] with { Race = null! };

        ProjectException error = Refuse(document with { Blueprints = [broken] });

        error.Message.ShouldContain("blueprints[0].race");
    }

    [Fact]
    public void AnAbsentStyleMemberIsRefused()
    {
        ProjectDocument document = Document();

        Refuse(document with { Style = document.Style with { Palette = null! } })
            .Message.ShouldContain("style.palette");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AnEmptyNameIsRefused(string name)
    {
        Refuse(Document() with { Name = name }).Message.ShouldContain("name");
    }

    [Fact]
    public void AnOverlongNameIsRefused()
    {
        Refuse(Document() with { Name = new string('a', ProjectValidation.MaxNameLength + 1) })
            .Message.ShouldContain("name");
    }

    // --- C.12 n° 25 sur les valeurs, et les identifiants ------------------

    [Theory]
    [InlineData("Steampunk")]
    [InlineData("fantasy")]
    [InlineData("0")]
    public void AnUnknownUniverseIsRefusedAndTheKnownOnesListed(string universe)
    {
        ProjectException error = Refuse(Document() with { Universe = universe });

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("Fantasy");
    }

    [Fact]
    public void TheThirdGeometryIsAccepted()
    {
        // DEC-039 added NoSupport and the tables of the bible took a while to
        // catch up. The schema accepts three.
        Should.NotThrow(() => ProjectValidation.Validate(Document() with { Geometry = "NoSupport" }));
    }

    [Theory]
    [InlineData("not-a-uuid")]
    [InlineData("{8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833}")]
    [InlineData("8f1a3c2e5b474d90a1e672c9f0d4b833")]
    public void AnIdentifierInAnyOtherSpellingIsRefused(string id)
    {
        Refuse(Document() with { ProjectId = id }).Message.ShouldContain("projectId");
    }

    [Theory]
    [InlineData("2026-09-01 14:22:07")]
    [InlineData("2026-09-01T14:22:07+02:00")]
    [InlineData("hier")]
    public void AnUnreadableTimestampIsRefused(string instant)
    {
        // The offset form is refused on purpose: a project has no time zone, and
        // accepting two spellings of one instant would let two files differ byte
        // for byte while meaning the same thing.
        Refuse(Document() with { CreatedAt = instant }).Message.ShouldContain("createdAt");
    }

    [Theory]
    [InlineData("18446744073709551616")]
    [InlineData("-1")]
    [InlineData("1e9")]
    public void ASeedThatIsNotAnUnsignedSixtyFourBitIntegerIsRefused(string seed)
    {
        ProjectDocument document = Document();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument broken = blueprint.Candidates[0] with { Seed = seed };

        ProjectException error = Refuse(document with
        {
            Blueprints = [blueprint with { Candidates = [broken], ElectedCandidateId = broken.Id }],
        });

        error.Message.ShouldContain("seed");
    }

    // --- C.12 n° 27 : la quantité ------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AQuantityBelowOneIsRefused(int quantity)
    {
        ProjectDocument document = Document();

        ProjectException error = Refuse(document with
        {
            Blueprints = [document.Blueprints[0] with { Quantity = quantity }],
        });

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("quantity");
    }

    // --- C.12 n° 28 : l'élu doit appartenir à ce gabarit -------------------

    [Fact]
    public void AnElectedCandidateThatExistsNowhereIsRefused()
    {
        ProjectDocument document = Document();

        ProjectException error = Refuse(document with
        {
            Blueprints =
            [
                document.Blueprints[0] with
                {
                    ElectedCandidateId = "11111111-2222-3333-4444-555555555555",
                },
            ],
        });

        error.Message.ShouldContain("electedCandidateId");
    }

    [Fact]
    public void AnElectedCandidateBelongingToAnotherBlueprintIsRefused()
    {
        // "Of this blueprint", not "of this project" - referential integrity is
        // what T2 owes. What becomes of the previous elect when a new one is
        // chosen is a business rule, so question B of chapter 16, so T3.
        ProjectDocument document = Document();
        string otherBlueprintsCandidate = document.Blueprints[0].Candidates[0].Id;

        ProjectException error = Refuse(document with
        {
            Blueprints =
            [
                document.Blueprints[1] with { ElectedCandidateId = otherBlueprintsCandidate },
            ],
        });

        error.Message.ShouldContain("electedCandidateId");
    }

    [Fact]
    public void ABlueprintWithNoCandidateAndNoElectIsPerfectlyNormal()
    {
        ProjectDocument document = Document();

        Should.NotThrow(() => ProjectValidation.Validate(
            document with { Blueprints = [document.Blueprints[1]] }));
    }

    // --- C.12 n° 29 : les chemins d'image ---------------------------------

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Users/Serge/a.png")]
    [InlineData("images/../../secret.png")]
    [InlineData("images\\front.png")]
    [InlineData("exports/front.png")]
    [InlineData("front.png")]
    [InlineData("./images/front.png")]
    public void AnImagePathThatEscapesIsRefusedWithItsOwnCode(string path)
    {
        ProjectDocument document = Document();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument broken = blueprint.Candidates[0] with { FrontImageFile = path };

        ProjectException error = Refuse(document with
        {
            Blueprints = [blueprint with { Candidates = [broken], ElectedCandidateId = broken.Id }],
        });

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
        error.WireCode.ShouldBe("PROJECT_PATH_ESCAPE");
    }

    [Fact]
    public void ANullImagePathIsAccepted()
    {
        // Between generation (T4) and cut-out (T5) a candidate has a paired
        // image and no cut-outs. Refusing null would force T4 to write paths to
        // files that do not exist, which is worse.
        ProjectDocument document = Document();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument pending = blueprint.Candidates[0] with
        {
            FrontImageFile = null,
            BackImageFile = null,
        };

        Should.NotThrow(() => ProjectValidation.Validate(document with
        {
            Blueprints = [blueprint with { Candidates = [pending], ElectedCandidateId = pending.Id }],
        }));
    }

    // --- C.4.4 : la moitié intrinsèque des surcharges ---------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AnIntrinsicallyWrongOverrideIsRefusedAndNamed(double value)
    {
        ProjectException error = Refuse(Document() with
        {
            CalibrationOverrides = new CalibrationOverridesDocument(value, null),
        });

        error.Code.ShouldBe(ProjectErrorCode.OverrideInvalid);
        error.WireCode.ShouldBe("PROJECT_OVERRIDE_INVALID");
        error.Message.ShouldContain("tabWidthMm");
    }

    [Fact]
    public void AnOverrideTooWideForThePawnIsNotRefusedHere()
    {
        // This is DEC-056 in one assertion. The value is perfectly valid in
        // itself; only this machine's calibration says otherwise, and a
        // machine's data never refuses a user's. The v1.0 specification blocked
        // here, which made a coherent archive unopenable because its author owns
        // different bases - the scenario the Share profile exists to make
        // pleasant. It becomes a diagnostic at load, and an error when someone
        // asks for a sheet.
        Should.NotThrow(() => ProjectValidation.Validate(Document() with
        {
            CalibrationOverrides = new CalibrationOverridesDocument(TabWidthMm: 900.0, TabHeightMm: null),
        }));
    }

    [Fact]
    public void NoOverrideAtAllIsTheNormalCase()
    {
        Should.NotThrow(() => ProjectValidation.Validate(Document() with
        {
            CalibrationOverrides = new CalibrationOverridesDocument(null, null),
        }));
    }

    // --- Les codes eux-mêmes ------------------------------------------------

    [Fact]
    public void EveryCodeHasAWireForm()
    {
        // The compiler cannot enforce this: a switch over an enumeration needs a
        // catch-all arm, because any integer can be cast to the type, and adding
        // that arm silences the missing-member check too. The guarantee lives
        // here instead.
        foreach (ProjectErrorCode code in Enum.GetValues<ProjectErrorCode>())
        {
            code.ToWireCode().ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void TheWireFormsAreThoseOfTheSpecification()
    {
        ProjectErrorCode.SchemaTooRecent.ToWireCode().ShouldBe("PROJECT_SCHEMA_TOO_RECENT");
        ProjectErrorCode.Invalid.ToWireCode().ShouldBe("PROJECT_INVALID");
        ProjectErrorCode.PathEscape.ToWireCode().ShouldBe("PROJECT_PATH_ESCAPE");
        ProjectErrorCode.OverrideInvalid.ToWireCode().ShouldBe("PROJECT_OVERRIDE_INVALID");
        ProjectErrorCode.NotFound.ToWireCode().ShouldBe("PROJECT_NOT_FOUND");
        ProjectErrorCode.TooLarge.ToWireCode().ShouldBe("PROJECT_TOO_LARGE");
        ProjectErrorCode.ArchiveRejected.ToWireCode().ShouldBe("ARCHIVE_REJECTED");
        ProjectErrorCode.ArchiveLimitExceeded.ToWireCode().ShouldBe("ARCHIVE_LIMIT_EXCEEDED");
        ProjectErrorCode.ArchiveExportFailed.ToWireCode().ShouldBe("ARCHIVE_EXPORT_FAILED");
        ProjectErrorCode.ImportDestinationExists.ToWireCode().ShouldBe("IMPORT_DESTINATION_EXISTS");
    }
}
