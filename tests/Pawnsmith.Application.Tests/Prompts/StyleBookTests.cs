using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Prompts;

/// <summary>The style library's rules (§I.7.2, DEC-110).</summary>
public class StyleBookTests
{
    private sealed class MemoryStore : IPersonalStyleStore
    {
        public IReadOnlyList<StylePreset> Stored { get; set; } = [];

        public Task<IReadOnlyList<StylePreset>> ReadAsync(Universe universe, CancellationToken cancellationToken) => Task.FromResult(Stored);

        public Task WriteAsync(Universe universe, IReadOnlyList<StylePreset> personal, CancellationToken cancellationToken)
        {
            Stored = personal;
            return Task.CompletedTask;
        }
    }

    private static readonly StylePreset Ink = new("ink", PromptFixture.Labels("Ink"), "Ink drawing.", "colour");

    private static Task<StyleBook> Book(MemoryStore store) =>
        StyleBook.LoadAsync(Universe.Fantasy, [Ink], store, CancellationToken.None);

    [Fact]
    public async Task APersonalStyleIsSavedAfterTheShippedOnesUnderItsOneName()
    {
        var store = new MemoryStore();
        StyleBook book = await Book(store);

        IReadOnlyList<StylePreset> styles = await book.AddAsync(" Mon aquarelle ", " Watercolour, pale. ", "", CancellationToken.None);

        styles.Select(style => style.Origin).ShouldBe([StyleOrigin.Shipped, StyleOrigin.Personal]);
        StylePreset mine = styles[1];
        mine.Names.Values.Distinct().ShouldBe(["Mon aquarelle"]);
        mine.StyleClause.ShouldBe("Watercolour, pale.");
        store.Stored.ShouldHaveSingleItem().Id.ShouldBe(mine.Id);
    }

    [Theory]
    [InlineData("", "Watercolour.")]
    [InlineData("Mine", "  ")]
    public async Task ANameAndAStyleClauseAreRequired(string name, string clause)
    {
        StyleBook book = await Book(new MemoryStore());

        (await Should.ThrowAsync<StyleRuleException>(() => book.AddAsync(name, clause, "", CancellationToken.None)))
            .WireCode.ShouldBe("STYLE_INVALID");
    }

    [Fact]
    public async Task AShippedStyleStaysAndAnUnknownOneIsNotFound()
    {
        StyleBook book = await Book(new MemoryStore());

        (await Should.ThrowAsync<StyleRuleException>(() => book.RemoveAsync("ink", CancellationToken.None))).WireCode.ShouldBe("STYLE_SHIPPED");
        (await Should.ThrowAsync<StyleRuleException>(() => book.RemoveAsync("nope", CancellationToken.None))).WireCode.ShouldBe("STYLE_NOT_FOUND");
    }

    [Fact]
    public async Task APersonalStyleIsRemoved()
    {
        var store = new MemoryStore();
        StyleBook book = await Book(store);
        string id = (await book.AddAsync("Mine", "Watercolour.", "", CancellationToken.None))[1].Id;

        (await book.RemoveAsync(id, CancellationToken.None)).ShouldHaveSingleItem().Id.ShouldBe("ink");
        store.Stored.ShouldBeEmpty();
    }
}
