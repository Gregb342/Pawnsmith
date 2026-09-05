using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// Covers tests 1 to 3 of C.12: the assembly rule of C.5.3.
/// </summary>
/// <remarks>
/// These assertions look trivial and are not. The assembly rule is a
/// compatibility surface: changing the order or the separator misaligns every
/// candidate of every project at once (C.5.5). These tests are what make such a
/// change fail loudly instead of silently.
/// </remarks>
public class ResolvedPromptTests
{
    // --- C.12 n° 1 : ordre, séparateur, Trim -------------------------------

    [Fact]
    public void ClausesAreJoinedInFramingSubjectStyleOrder()
    {
        string prompt = ResolvedPrompt.From("framing", "subject", "style");

        prompt.ShouldBe("framing\nsubject\nstyle");
    }

    [Fact]
    public void ClausesAreTrimmedBeforeBeingJoined()
    {
        string prompt = ResolvedPrompt.From("  framing\t", "\n subject ", " style\n\n");

        prompt.ShouldBe("framing\nsubject\nstyle");
    }

    [Fact]
    public void TheSeparatorIsASingleLineFeedAndNeverThePlatformOne()
    {
        string prompt = ResolvedPrompt.From("a", "b", "c");

        // Written as an explicit character so the assertion still means
        // something if someone reaches for Environment.NewLine in the code.
        prompt.ShouldBe("a\u000Ab\u000Ac");
        prompt.ShouldNotContain("\r");
    }

    // --- C.12 n° 2 : une clause vide est omise -----------------------------

    [Fact]
    public void AnEmptyStyleClauseLeavesNoTrailingSeparator()
    {
        string prompt = ResolvedPrompt.From("framing", "subject", string.Empty);

        prompt.ShouldBe("framing\nsubject");
    }

    [Fact]
    public void AnEmptyFramingClauseLeavesNoLeadingSeparator()
    {
        string prompt = ResolvedPrompt.From(string.Empty, "subject", "style");

        prompt.ShouldBe("subject\nstyle");
    }

    [Fact]
    public void AClauseMadeOnlyOfWhitespaceCountsAsEmpty()
    {
        string prompt = ResolvedPrompt.From("framing", "   \n\t ", "style");

        prompt.ShouldBe("framing\nstyle");
    }

    [Fact]
    public void AnEmptyClauseAndAnAbsentClauseGiveTheSamePrompt()
    {
        // The assembly is deliberately not injective: an empty style and no
        // style at all mean the same thing, so they must produce the same
        // prompt (C.5.3).
        ResolvedPrompt.From("framing", "subject", string.Empty)
            .ShouldBe(ResolvedPrompt.From("framing", "subject", "   "));
    }

    [Fact]
    public void ThreeEmptyClausesGiveAnEmptyPrompt()
    {
        ResolvedPrompt.From(string.Empty, string.Empty, string.Empty).ShouldBe(string.Empty);
    }

    // --- C.12 n° 3 : les fins de ligne Windows ne changent rien ------------

    [Fact]
    public void WindowsLineEndingsInsideAClauseGiveTheSameResultAsUnixOnes()
    {
        string windows = ResolvedPrompt.From("first\r\nsecond", "subject", "style");
        string unix = ResolvedPrompt.From("first\nsecond", "subject", "style");

        windows.ShouldBe(unix);
        windows.ShouldBe("first\nsecond\nsubject\nstyle");
    }

    [Fact]
    public void ALoneCarriageReturnBecomesOneLineFeedAndNotTwo()
    {
        // The order of the two replacements is what this pins down: replacing
        // "\r" before "\r\n" would turn every Windows line ending into a blank
        // line, and the prompt sent would silently differ from the one stored.
        ResolvedPrompt.Normalize("first\rsecond").ShouldBe("first\nsecond");
        ResolvedPrompt.Normalize("first\r\nsecond").ShouldBe("first\nsecond");
    }

    [Fact]
    public void NormalizeIsIdempotent()
    {
        // C.5.3 relies on this: the clause is normalised at write time and
        // again at assembly time, and applying it twice must cost nothing.
        string once = ResolvedPrompt.Normalize("  first\r\nsecond \r\n");

        ResolvedPrompt.Normalize(once).ShouldBe(once);
    }
}
