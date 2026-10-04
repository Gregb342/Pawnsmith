using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Prompts;

/// <summary>
/// The personal catalogue: complete entries only, shipped entries untouched
/// (§I.4, DEC-107). Tests 4 to 7 of §I.12 on the rule side; the routes and
/// the file have their own.
/// </summary>
public class CatalogBookTests
{
    private sealed class MemoryStore : IPersonalCatalogStore
    {
        public IReadOnlyList<CatalogParameter> Stored { get; set; } = [];

        public int Writes { get; private set; }

        public Task<IReadOnlyList<CatalogParameter>> ReadAsync(Universe universe, CancellationToken cancellationToken) =>
            Task.FromResult(Stored);

        public Task WriteAsync(Universe universe, IReadOnlyList<CatalogParameter> personal, CancellationToken cancellationToken)
        {
            Stored = personal;
            Writes++;
            return Task.CompletedTask;
        }
    }

    private static Task<CatalogBook> Book(MemoryStore store) =>
        CatalogBook.LoadAsync(PromptFixture.Catalog(), store, CancellationToken.None);

    private static Dictionary<string, string> Both(string english, string french) =>
        new(StringComparer.Ordinal) { ["en"] = english, ["fr"] = french };

    [Fact]
    public async Task ACompleteEntryJoinsItsKeyAsPersonalAndIsWritten()
    {
        var store = new MemoryStore();
        CatalogBook book = await Book(store);

        Catalog updated = await book.AddEntryAsync(
            "weapon", " halberd ", Both("halberd", "hallebarde"), " holding a halberd upright against the shoulder ", CancellationToken.None);

        CatalogEntry added = updated.Find("weapon")!.Entries[^1];
        added.Value.ShouldBe("halberd");
        added.Fragment.ShouldBe("holding a halberd upright against the shoulder");
        added.Origin.ShouldBe(CatalogEntryOrigin.Personal);
        book.Current.ShouldBeSameAs(updated);
        store.Writes.ShouldBe(1);
        store.Stored.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem().Value.ShouldBe("halberd");
    }

    [Theory]
    [InlineData("mount", "horse", "riding a horse")]
    [InlineData("weapon", "  ", "holding something")]
    [InlineData("weapon", "halberd", "   ")]
    public async Task AnIncompleteEntryIsRefusedAndNothingIsWritten(string key, string value, string fragment)
    {
        var store = new MemoryStore();
        CatalogBook book = await Book(store);

        CatalogRuleException error = await Should.ThrowAsync<CatalogRuleException>(() =>
            book.AddEntryAsync(key, value, Both("x", "x"), fragment, CancellationToken.None));

        error.WireCode.ShouldBe("CATALOG_ENTRY_INVALID");
        store.Writes.ShouldBe(0);
    }

    [Fact]
    public async Task AnEntryWithoutAFrenchLabelIsRefused()
    {
        var store = new MemoryStore();
        CatalogBook book = await Book(store);
        var englishOnly = new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "halberd" };

        CatalogRuleException error = await Should.ThrowAsync<CatalogRuleException>(() =>
            book.AddEntryAsync("weapon", "halberd", englishOnly, "holding a halberd", CancellationToken.None));

        error.WireCode.ShouldBe("CATALOG_ENTRY_INVALID");
        error.Message.ShouldContain("'fr'");
    }

    [Fact]
    public async Task AValueTheKeyAlreadyHasIsADuplicate()
    {
        CatalogBook book = await Book(new MemoryStore());

        CatalogRuleException shipped = await Should.ThrowAsync<CatalogRuleException>(() =>
            book.AddEntryAsync("weapon", "axe", Both("axe", "hache"), "wielding an axe", CancellationToken.None));
        shipped.WireCode.ShouldBe("CATALOG_ENTRY_DUPLICATE");

        await book.AddEntryAsync("weapon", "halberd", Both("halberd", "hallebarde"), "holding a halberd", CancellationToken.None);

        CatalogRuleException personal = await Should.ThrowAsync<CatalogRuleException>(() =>
            book.AddEntryAsync("weapon", "halberd", Both("halberd", "hallebarde"), "holding a halberd", CancellationToken.None));
        personal.WireCode.ShouldBe("CATALOG_ENTRY_DUPLICATE");
    }

    [Fact]
    public async Task AShippedEntryIsNeverRemovedAndAnUnknownOneIsNotFound()
    {
        CatalogBook book = await Book(new MemoryStore());

        (await Should.ThrowAsync<CatalogRuleException>(() => book.RemoveEntryAsync("weapon", "axe", CancellationToken.None)))
            .WireCode.ShouldBe("CATALOG_ENTRY_SHIPPED");
        (await Should.ThrowAsync<CatalogRuleException>(() => book.RemoveEntryAsync("weapon", "halberd", CancellationToken.None)))
            .WireCode.ShouldBe("CATALOG_ENTRY_NOT_FOUND");
    }

    [Fact]
    public async Task APersonalEntryIsRemovedAndTheStoreRewritten()
    {
        var store = new MemoryStore();
        CatalogBook book = await Book(store);
        await book.AddEntryAsync("weapon", "halberd", Both("halberd", "hallebarde"), "holding a halberd", CancellationToken.None);

        Catalog updated = await book.RemoveEntryAsync("weapon", "halberd", CancellationToken.None);

        updated.TryGetFragment("weapon", "halberd", out _).ShouldBeFalse();
        store.Stored.ShouldBeEmpty();
    }

    [Fact]
    public async Task AtStartUpAPersonalEntryTheShippedCatalogueNowHasIsSetAside()
    {
        // An upgrade ships "axe" with its own fragment: the shipped one wins,
        // and the start-up can say what was set aside.
        var store = new MemoryStore
        {
            Stored =
            [
                PromptFixture.Param("weapon", PromptFixture.Entry("axe", "my own axe"), PromptFixture.Entry("halberd", "holding a halberd")),
                PromptFixture.Param("mount", PromptFixture.Entry("horse", "riding a horse")),
            ],
        };

        CatalogBook book = await Book(store);

        book.Current.TryGetFragment("weapon", "axe", out string axe).ShouldBeTrue();
        axe.ShouldBe("wielding a large battle axe");
        book.Current.TryGetFragment("weapon", "halberd", out _).ShouldBeTrue();
        book.Dropped.ShouldBe(["weapon: axe", "mount: horse"]);
    }

    [Fact]
    public async Task TheComposerSeesAnEntryAddedAfterItWasBuilt()
    {
        CatalogBook book = await Book(new MemoryStore());
        var composer = new TemplatePromptComposer(PromptFixture.Template(), () => book.Current);
        Blueprint blueprint = ProjectFixture.Blueprint() with
        {
            OptionalParameters = new Dictionary<string, string>(StringComparer.Ordinal) { ["weapon"] = "halberd" },
        };

        composer.ComposeSubject(blueprint, Universe.Fantasy).Diagnostics.ShouldHaveSingleItem();

        await book.AddEntryAsync("weapon", "halberd", Both("halberd", "hallebarde"), "holding a halberd", CancellationToken.None);

        ComposedSubject composed = composer.ComposeSubject(blueprint, Universe.Fantasy);
        composed.Diagnostics.ShouldBeEmpty();
        composed.Clause.ShouldContain("holding a halberd");
    }
}
