namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The reasons a project operation can refuse, as codes rather than messages.
/// </summary>
/// <remarks>
/// <para>
/// Chapter 10 of the bible requires the API to return <b>codes, never
/// translated text</b>, so that the API stays ignorant of language and the
/// translations live in one place. T2 has no API, but the errors are born here:
/// giving them a code now is what stops T6 having to invent one afterwards by
/// reading messages.
/// </para>
/// <para>
/// The wire strings are those of C.11 and they are a <b>contract</b>. They are
/// written out in <see cref="ToWireCode"/> rather than derived from the member
/// names, so that renaming a member cannot quietly change what an API returns.
/// The switch is exhaustive on purpose: adding a member without a code fails the
/// build rather than falling through to a default.
/// </para>
/// <para>
/// Four of these have no thrower yet — they belong to the archive, which is
/// still to be written. They are declared now because C.11 is one table and
/// splitting it across three commits would mean editing it three times, not
/// because anything is being anticipated.
/// </para>
/// </remarks>
public enum ProjectErrorCode
{
    /// <summary>A schema newer than this build understands (C.6.1).</summary>
    SchemaTooRecent,

    /// <summary>Missing field, unknown field, unknown enumeration member, bad quantity, unreadable timestamp.</summary>
    Invalid,

    /// <summary>An image path that is absolute, outside <c>images/</c>, or carrying <c>..</c> or a backslash.</summary>
    PathEscape,

    /// <summary>A calibration override that is intrinsically wrong: null, negative, infinite, not a number.</summary>
    OverrideInvalid,

    /// <summary>No folder, or no <c>project.json</c> in it.</summary>
    NotFound,

    /// <summary>A <c>project.json</c> past the size bound of C.9.3.</summary>
    TooLarge,

    /// <summary>Any violation of C.9.1 steps 3 to 6. Deliberately says no more — see below.</summary>
    ArchiveRejected,

    /// <summary>A resource bound of C.9.3 exceeded.</summary>
    ArchiveLimitExceeded,

    /// <summary>A symbolic link met, a referenced file missing, a forbidden destination.</summary>
    ArchiveExportFailed,

    /// <summary>The destination folder already exists, even empty.</summary>
    ImportDestinationExists,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class ProjectErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in C.11.</summary>
    public static string ToWireCode(this ProjectErrorCode code) => code switch
    {
        ProjectErrorCode.SchemaTooRecent => "PROJECT_SCHEMA_TOO_RECENT",
        ProjectErrorCode.Invalid => "PROJECT_INVALID",
        ProjectErrorCode.PathEscape => "PROJECT_PATH_ESCAPE",
        ProjectErrorCode.OverrideInvalid => "PROJECT_OVERRIDE_INVALID",
        ProjectErrorCode.NotFound => "PROJECT_NOT_FOUND",
        ProjectErrorCode.TooLarge => "PROJECT_TOO_LARGE",
        ProjectErrorCode.ArchiveRejected => "ARCHIVE_REJECTED",
        ProjectErrorCode.ArchiveLimitExceeded => "ARCHIVE_LIMIT_EXCEEDED",
        ProjectErrorCode.ArchiveExportFailed => "ARCHIVE_EXPORT_FAILED",
        ProjectErrorCode.ImportDestinationExists => "IMPORT_DESTINATION_EXISTS",

        // The compiler insists on this arm: an enumeration can hold a value
        // that has no name, because any integer can be cast to it. Adding it
        // costs the compile-time check that every *named* member is handled -
        // the two warnings go away together - so a test walks Enum.GetValues
        // and asserts each one has a wire form. The guarantee moves from the
        // compiler to the test suite rather than disappearing.
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

// On ARCHIVE_REJECTED saying nothing about what went wrong: that is deliberate,
// and it is the one place in this file where a vague code is the right answer.
// The log message is precise (chapter 8). Telling the caller that a particular
// entry failed the case-collision check is useful to whoever is building the
// malicious archive and to nobody else. The reasoning holds only for the
// archive, whose content can be hostile - PROJECT_INVALID names the offending
// field, because a malformed project is almost always your own.
